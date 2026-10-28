using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FluentPass.Services
{
    /// <summary>更新检查的终态。</summary>
    public enum UpdateOutcome
    {
        /// <summary>远端版本不高于当前版本（或该版本已被用户忽略）。</summary>
        UpToDate,

        /// <summary>新版本已下载、校验并替换落盘，重启宿主后生效。</summary>
        Installed,

        /// <summary>新版本已下载并校验，但当前账户无权写入插件目录，需人工替换。</summary>
        NotWritable,

        /// <summary>网络、格式或校验失败，未对现有插件做任何改动。</summary>
        Failed
    }

    public enum UpdateApplyResult
    {
        Applied,
        NotWritable,
        Failed
    }

    public sealed class UpdateCheckResult
    {
        public UpdateOutcome Outcome { get; }
        public Version? RemoteVersion { get; }
        public string? DownloadedPath { get; }
        public string? Error { get; }

        public bool IsCheckFailure => Outcome == UpdateOutcome.Failed && RemoteVersion == null;
        public bool IsDownloadFailure => Outcome == UpdateOutcome.Failed && RemoteVersion != null;

        public UpdateCheckResult(UpdateOutcome outcome, Version? remoteVersion = null,
            string? downloadedPath = null, string? error = null)
        {
            Outcome = outcome;
            RemoteVersion = remoteVersion;
            DownloadedPath = downloadedPath;
            Error = error;
        }
    }

    /// <summary>
    /// 网络传输接缝：生产实现走 <see cref="WebClient"/>，单元测试注入内存假实现，
    /// 从而让版本解析、校验与换装逻辑全部可离线验证。
    /// </summary>
    public interface IUpdateTransport
    {
        Task<string> DownloadStringAsync(string url, CancellationToken cancellationToken);
        Task<byte[]> DownloadBytesAsync(string url, CancellationToken cancellationToken);
    }

    /// <summary>
    /// 基于 <see cref="WebClient"/> 的最小传输实现（刻意不引入 <c>System.Net.Http</c> 包依赖，
    /// 保持单文件分发不新增程序集）。TLS 1.2 显式开启以兼容默认协议受限的旧系统。
    /// </summary>
    public sealed class WebClientUpdateTransport : IUpdateTransport
    {
        private readonly int _timeoutMs;

        public WebClientUpdateTransport(int timeoutMs = 30000)
        {
            _timeoutMs = timeoutMs > 0 ? timeoutMs : 30000;
        }

        static WebClientUpdateTransport()
        {
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
            catch
            {
                // 极旧系统不支持枚举值时的安全忽略（.NET 4.8 下必然可用）
            }
        }

        public Task<string> DownloadStringAsync(string url, CancellationToken cancellationToken)
        {
            return ExecuteAsync(client => client.DownloadStringTaskAsync(new Uri(url)), cancellationToken);
        }

        public Task<byte[]> DownloadBytesAsync(string url, CancellationToken cancellationToken)
        {
            return ExecuteAsync(client => client.DownloadDataTaskAsync(new Uri(url)), cancellationToken);
        }

        private async Task<T> ExecuteAsync<T>(Func<WebClient, Task<T>> action, CancellationToken cancellationToken)
        {
            using (var client = new WebClient())
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                client.Headers[HttpRequestHeader.UserAgent] = UpdateService.UserAgent;

                linked.CancelAfter(_timeoutMs);
                using (linked.Token.Register(() =>
                {
                    try { client.CancelAsync(); }
                    catch { }
                }))
                {
                    try
                    {
                        return await action(client).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (linked.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                    {
                        // 仅本层超时触发取消（用户未取消）：统一翻译为超时，绝不泄露
                        // OperationCanceledException——那会被上层镜像回退误判为用户取消而直接放弃。
                        throw new TimeoutException("更新请求超时", ex);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 在线更新核心（铁律 1 合规：没有任何跨进程数据通道，只在用户开启时发起
    /// 三次 HTTPS 只读请求并仅携带版本号/User-Agent，不上传任何凭据或数据库信息）。
    ///
    /// 版本通道复用 KeePass 官方插件更新契约（<c>Plugin.UpdateUrl</c> 指向的
    /// <c>releases/latest/download/version.txt</c>），因此没有 GitHub API 速率限制，
    /// 也不需要令牌。下载源为同一 Release 的 <c>FluentPass.dll</c> 与 <c>SHA256SUMS.txt</c>，
    /// 校验不过则一律放弃，绝不落盘替换。
    ///
    /// 换装策略（实证见 <see cref="ApplyStagedUpdate"/>）：Windows 允许重命名已被映射的
    /// 程序集文件（仅删除被拒），因此可在宿主进程内完成「旧 DLL 改名 → 新 DLL 就位」，
    /// 无需外挂替身进程，也不必等待 KeePass 退出。
    /// </summary>
    public sealed class UpdateService
    {
        public const string VersionFileUrl =
            "https://github.com/YoungSx/fluentpass/releases/latest/download/version.txt";

        public const string DownloadUrl =
            "https://github.com/YoungSx/fluentpass/releases/latest/download/FluentPass.dll";

        public const string ChecksumsUrl =
            "https://github.com/YoungSx/fluentpass/releases/latest/download/SHA256SUMS.txt";

        public const string ReleasesPageUrl =
            "https://github.com/YoungSx/fluentpass/releases/latest";

        /// <summary>
        /// GitHub 反代镜像（前缀拼接官方完整 URL）。仅在官方源发生<b>网络层</b>失败时按序回退，
        /// 用于绕过中国大陆常见的连接重置 / CDN 限速；官方可达时永不使用。
        ///
        /// 顺序即优先级：先选响应快的，再选存活久的。列表会随镜像站存亡变化，属可维护配置，
        /// 不承诺长期有效——镜像全挂等价于回到「只有官方源」的原行为。
        /// </summary>
        public static readonly IReadOnlyList<string> MirrorPrefixes = new[]
        {
            "https://gh-proxy.com/",
            "https://ghproxy.net/",
            "https://hk.gh-proxy.com/"
        };

        /// <summary>Release 包内插件文件名（与 SHA256SUMS.txt 中的条目一致）。</summary>
        public const string PluginFileName = "FluentPass.dll";

        private const string VersionEntryPrefix = "FluentPass:";

        private readonly IUpdateTransport _transport;
        private readonly string _targetPath;
        private readonly string _stagingRoot;

        public static string UserAgent =>
            "FluentPass/" + (typeof(UpdateService).Assembly.GetName().Version?.ToString(3) ?? "0.0.0");

        /// <summary>当前进程内加载的程序集版本（读内存中的 AssemblyName，换装后也不会被新文件误导）。</summary>
        public static Version CurrentVersion => typeof(UpdateService).Assembly.GetName().Version ?? new Version(0, 0);

        public UpdateService(IUpdateTransport transport, string targetPath, string? stagingRoot = null)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            if (string.IsNullOrWhiteSpace(targetPath)) throw new ArgumentException("targetPath", nameof(targetPath));

            _targetPath = targetPath;
            _stagingRoot = stagingRoot ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FluentPass", "updates");
        }

        public string TargetPath => _targetPath;

        /// <summary>
        /// 检查并按需完成「下载 → SHA256 校验 → 换装」。任何一步失败都不会碰触现有插件文件。
        /// </summary>
        /// <param name="currentVersion">当前运行版本。</param>
        /// <param name="skippedVersion">用户明确忽略的版本（同版本不再重复下载）。</param>
        public async Task<UpdateCheckResult> CheckAndInstallAsync(
            Version currentVersion, string? skippedVersion, CancellationToken cancellationToken)
        {
            string versionText;
            try
            {
                versionText = await _transport.DownloadStringAsync(VersionFileUrl, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new UpdateCheckResult(UpdateOutcome.Failed, error: ex.Message);
            }

            var remote = TryParseVersionFile(versionText);
            if (remote == null)
            {
                return new UpdateCheckResult(UpdateOutcome.Failed, error: "version.txt 格式无效");
            }

            if (!IsNewer(currentVersion, remote) ||
                (!string.IsNullOrEmpty(skippedVersion) &&
                 string.Equals(skippedVersion, remote.ToString(), StringComparison.OrdinalIgnoreCase)))
            {
                return new UpdateCheckResult(UpdateOutcome.UpToDate, remote);
            }

            string checksumsText;
            try
            {
                checksumsText = await _transport.DownloadStringAsync(ChecksumsUrl, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new UpdateCheckResult(UpdateOutcome.Failed, remote, error: ex.Message);
            }

            var expectedHash = FindChecksum(checksumsText, PluginFileName);
            if (string.IsNullOrEmpty(expectedHash))
            {
                return new UpdateCheckResult(UpdateOutcome.Failed, remote,
                    error: "SHA256SUMS.txt 中缺少 " + PluginFileName + " 条目");
            }

            byte[] payload;
            try
            {
                payload = await _transport.DownloadBytesAsync(DownloadUrl, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new UpdateCheckResult(UpdateOutcome.Failed, remote, error: ex.Message);
            }

            if (!string.Equals(ComputeSha256(payload), expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                return new UpdateCheckResult(UpdateOutcome.Failed, remote, error: "SHA256 校验不匹配");
            }

            string stagedPath;
            try
            {
                var versionDir = Path.Combine(_stagingRoot, remote.ToString());
                Directory.CreateDirectory(versionDir);
                stagedPath = Path.Combine(versionDir, PluginFileName);
                File.WriteAllBytes(stagedPath, payload);
            }
            catch (Exception ex)
            {
                return new UpdateCheckResult(UpdateOutcome.Failed, remote, error: ex.Message);
            }

            var apply = ApplyStagedUpdate(stagedPath, _targetPath, out var applyError);
            switch (apply)
            {
                case UpdateApplyResult.Applied:
                    return new UpdateCheckResult(UpdateOutcome.Installed, remote, stagedPath);
                case UpdateApplyResult.NotWritable:
                    return new UpdateCheckResult(UpdateOutcome.NotWritable, remote, stagedPath, applyError);
                default:
                    return new UpdateCheckResult(UpdateOutcome.Failed, remote, stagedPath, applyError);
            }
        }

        /// <summary>
        /// 官方插件版本文件格式（https://keepass.info/help/v2_dev/plg_index.html）：
        /// 首末行为分隔符 <c>:</c>，中间每行 <c>&lt;AssemblyTitle&gt;:&lt;AssemblyFileVersion&gt;</c>。
        /// 只认本插件条目，其余插件的行一律忽略。
        /// </summary>
        public static Version? TryParseVersionFile(string? content)
        {
            if (string.IsNullOrWhiteSpace(content)) return null;

            foreach (var rawLine in content!.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line == ":") continue;
                if (!line.StartsWith(VersionEntryPrefix, StringComparison.OrdinalIgnoreCase)) continue;

                var value = line.Substring(VersionEntryPrefix.Length).Trim();
                if (Version.TryParse(value, out var parsed)) return Normalize(parsed);
            }

            return null;
        }

        /// <summary>
        /// 严格大于才算新版本：KeePass 的版本判定同样只看数值段，
        /// 因此补齐到 4 段后比较，避免 "1.5.1" 与 "1.5.1.0" 产生虚假更新提示。
        /// </summary>
        public static bool IsNewer(Version current, Version remote)
        {
            return Normalize(remote) > Normalize(current);
        }

        /// <summary>
        /// 解析 <c>SHA256SUMS.txt</c>：每行 <c>&lt;HEX&gt;  &lt;FILENAME&gt;</c>（两空格分隔，
        /// 由发版流水线的 <c>Get-FileHash</c> 输出）。文件名不区分大小写。
        /// </summary>
        public static string? FindChecksum(string? checksumsContent, string fileName)
        {
            if (string.IsNullOrWhiteSpace(checksumsContent) || string.IsNullOrEmpty(fileName)) return null;

            foreach (var rawLine in checksumsContent!.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = rawLine.Trim();
                if (line.Length == 0) continue;

                var separator = line.IndexOfAny(new[] { ' ', '\t' });
                if (separator <= 0) continue;

                var hash = line.Substring(0, separator).Trim();
                var name = line.Substring(separator).Trim().TrimStart('*');
                if (hash.Length == 0) continue;

                if (string.Equals(name, fileName, StringComparison.OrdinalIgnoreCase))
                {
                    return hash;
                }
            }

            return null;
        }

        public static string ComputeSha256(byte[] payload)
        {
            using (var sha = SHA256.Create())
            {
                return ToHex(sha.ComputeHash(payload ?? Array.Empty<byte>()));
            }
        }

        /// <summary>
        /// 校验磁盘文件哈希。仅用于手工核对/测试，下载路径直接对内存字节校验，避免二次读盘。
        /// </summary>
        public static bool VerifyFileHash(string path, string expectedSha256Hex)
        {
            if (string.IsNullOrEmpty(expectedSha256Hex) || !File.Exists(path)) return false;

            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                return string.Equals(ToHex(sha.ComputeHash(stream)), expectedSha256Hex,
                    StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// 进程内换装。实证（.NET Framework 4.8 / Windows 11）：
        /// 已被 CLR 映射的程序集文件 <b>可以重命名</b>，但 <b>不可删除</b>
        /// （镜像节持有删除拒绝，但目录项改名不受限制）。
        /// 因此顺序固定为：旧文件改名备份 → 新文件就位；任何一步失败都回滚旧文件。
        /// 备份文件在宿主下次启动时由 <see cref=\"CleanupLeftoverFiles\"/> 清理。
        /// </summary>
        public static UpdateApplyResult ApplyStagedUpdate(string stagedPath, string targetPath, out string? error)
        {
            error = null;

            if (!File.Exists(stagedPath))
            {
                error = "暂存文件缺失: " + stagedPath;
                return UpdateApplyResult.Failed;
            }

            var backupPath = targetPath + ".old";

            // 上一次更新留下的备份：本进程未映射它，可以安全删除；删不掉说明另有实例仍持有，放弃本次换装。
            try
            {
                if (File.Exists(backupPath)) File.Delete(backupPath);
            }
            catch (Exception ex)
            {
                error = "旧备份无法清理（可能另有一个 KeePass 实例正在运行）: " + ex.Message;
                return UpdateApplyResult.Failed;
            }

            try
            {
                File.Move(targetPath, backupPath);
            }
            catch (UnauthorizedAccessException ex)
            {
                error = ex.Message;
                return UpdateApplyResult.NotWritable;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return UpdateApplyResult.Failed;
            }

            try
            {
                File.Move(stagedPath, targetPath);
                return UpdateApplyResult.Applied;
            }
            catch (Exception ex)
            {
                // 回滚：把旧文件放回原位，确保插件目录永远处于可加载状态。
                try
                {
                    File.Move(backupPath, targetPath);
                }
                catch
                {
                    error = ex.Message + "（回滚失败，请手动将 " + PluginFileName + ".old 改回原名）";
                    return UpdateApplyResult.Failed;
                }

                error = ex.Message;
                return UpdateApplyResult.Failed;
            }
        }

        /// <summary>
        /// 清理换装遗留（<c>.old</c> / <c>.new</c>）。新进程启动时旧镜像已卸载，删除必然成功；
        /// 任何异常都只做静默忽略，绝不影响插件加载。
        /// </summary>
        public static void CleanupLeftoverFiles(string? targetPath = null)
        {
            try
            {
                var path = targetPath ?? typeof(UpdateService).Assembly.Location;
                if (string.IsNullOrWhiteSpace(path)) return;

                foreach (var suffix in new[] { ".old", ".new" })
                {
                    var leftover = path + suffix;
                    try
                    {
                        if (File.Exists(leftover)) File.Delete(leftover);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static Version Normalize(Version version)
        {
            var major = Math.Max(version.Major, 0);
            var minor = Math.Max(version.Minor, 0);
            var build = Math.Max(version.Build, 0);
            var revision = Math.Max(version.Revision, 0);
            return new Version(major, minor, build, revision);
        }

        private static string ToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
