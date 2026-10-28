using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace FluentPass.Services
{
    /// <summary>
    /// 镜像回退传输层（装饰器）：官方源优先，遇到<b>网络层</b>失败时按顺序改走 GitHub 反代镜像重试。
    ///
    /// 只对网络层失败回退：HTTP 404/403 是官方给出的确定性答复，镜像代理的是同一份内容，
    /// 重试只会放大延迟并掩盖真实错误。
    ///
    /// 每次尝试都有独立预算（<see cref="PerAttemptTimeoutMs"/>），由本层持有而非内层传输：
    /// 重试策略属于本层，超时预算自然也该由本层裁定。官方 CDN 慢到超预算时即改走镜像，
    /// 这正是「官方可达但下载龟速」场景的解法。
    ///
    /// 粘滞策略：某个镜像一旦取数成功，同一实例的后续请求先走该镜像，避免每个请求都重新
    /// 等一轮官方超时。常见情形（GitHub 可达、Release 资产 CDN 缓慢）下 version.txt 与
    /// SHA256SUMS.txt 仍取自官方源，完整性锚点留在 GitHub；只有官方完全不可达时整条链路
    /// 才会落到镜像。
    ///
    /// <b>信任边界</b>：本层不参与任何信任判断，下载字节仍由 <see cref="UpdateService"/> 按
    /// <c>SHA256SUMS.txt</c> 逐字节校验，校验不过一律放弃，绝不落盘替换。但需明确：
    /// 若校验和与 DLL 来自同一个镜像，该校验只能防传输损坏，不能防该镜像作恶——
    /// 因此官方可达时校验和始终优先取自官方（见 <see cref="UpdateService.MirrorPrefixes"/> 的调用顺序）。
    /// </summary>
    public sealed class MirrorAwareTransport : IUpdateTransport
    {
        /// <summary>单次尝试的默认预算（毫秒）。</summary>
        public const int DefaultPerAttemptTimeoutMs = 20000;

        private readonly IUpdateTransport _inner;
        private readonly IReadOnlyList<string> _mirrorPrefixes;
        private readonly int _perAttemptTimeoutMs;

        public MirrorAwareTransport(
            IUpdateTransport inner,
            IReadOnlyList<string>? mirrorPrefixes = null,
            int perAttemptTimeoutMs = DefaultPerAttemptTimeoutMs)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _mirrorPrefixes = mirrorPrefixes ?? Array.Empty<string>();
            _perAttemptTimeoutMs = perAttemptTimeoutMs > 0 ? perAttemptTimeoutMs : DefaultPerAttemptTimeoutMs;
        }

        /// <summary>最近一次取数成功的镜像前缀；同一实例的后续请求优先复用它。</summary>
        public string? PreferredMirror { get; private set; }

        public Task<string> DownloadStringAsync(string url, CancellationToken cancellationToken)
        {
            return ExecuteAsync((candidate, token) => _inner.DownloadStringAsync(candidate, token),
                url, cancellationToken);
        }

        public Task<byte[]> DownloadBytesAsync(string url, CancellationToken cancellationToken)
        {
            return ExecuteAsync((candidate, token) => _inner.DownloadBytesAsync(candidate, token),
                url, cancellationToken);
        }

        private async Task<T> ExecuteAsync<T>(
            Func<string, CancellationToken, Task<T>> fetch, string url, CancellationToken cancellationToken)
        {
            Exception? firstError = null;

            foreach (var candidate in BuildCandidates(url))
            {
                using (var attempt = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    attempt.CancelAfter(_perAttemptTimeoutMs);

                    try
                    {
                        var value = await fetch(candidate.Url, attempt.Token).ConfigureAwait(false);

                        if (candidate.MirrorPrefix != null)
                        {
                            PreferredMirror = candidate.MirrorPrefix;
                        }

                        return value;
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        // 用户取消（关闭窗口 / 释放视图模型）必须原样上抛，绝不改道重试。
                        throw;
                    }
                    catch (Exception ex)
                    {
                        if (candidate.MirrorPrefix == null && !IsRetryable(ex, attempt))
                        {
                            // 官方给出的确定性答复（如 404）不是网络问题，换镜像也拿不到别的东西。
                            throw;
                        }

                        firstError ??= ex;
                    }
                }
            }

            if (firstError != null)
            {
                // 保留原始异常类型与堆栈，供状态机区分超时 / 拒绝连接等原因。
                ExceptionDispatchInfo.Capture(firstError).Throw();
            }

            throw new InvalidOperationException("没有可用的更新源: " + url);
        }

        /// <summary>
        /// 候选地址顺序：
        /// 无粘滞镜像时为 [官方, 镜像1, 镜像2…]；
        /// 已有粘滞镜像时为 [粘滞镜像, 其余镜像…, 官方]，官方放在最后兜底。
        /// </summary>
        private IEnumerable<Candidate> BuildCandidates(string url)
        {
            var preferred = PreferredMirror;

            if (preferred == null)
            {
                yield return new Candidate(url, null);

                foreach (var prefix in _mirrorPrefixes)
                {
                    yield return new Candidate(prefix + url, prefix);
                }

                yield break;
            }

            yield return new Candidate(preferred + url, preferred);

            foreach (var prefix in _mirrorPrefixes)
            {
                if (!string.Equals(prefix, preferred, StringComparison.OrdinalIgnoreCase))
                {
                    yield return new Candidate(prefix + url, prefix);
                }
            }

            yield return new Candidate(url, null);
        }

        /// <summary>
        /// 是否值得换源重试。本层预算耗尽（<paramref name="attempt"/> 已取消但用户未取消）
        /// 与「根本没拿到答复」都算；<c>ProtocolError</c>（HTTP 4xx/5xx）是服务器明确表态，
        /// 镜像代理同一份内容，重试没有意义。
        /// </summary>
        private static bool IsRetryable(Exception ex, CancellationTokenSource attempt)
        {
            if (attempt.IsCancellationRequested) return true;

            if (ex is TimeoutException) return true;

            // WebClient 会把套接字层错误包装成 WebException，少数路径直接抛 IOException。
            if (ex is IOException) return true;

            if (ex is WebException web)
            {
                switch (web.Status)
                {
                    case WebExceptionStatus.NameResolutionFailure:
                    case WebExceptionStatus.ProxyNameResolutionFailure:
                    case WebExceptionStatus.ConnectFailure:
                    case WebExceptionStatus.ConnectionClosed:
                    case WebExceptionStatus.KeepAliveFailure:
                    case WebExceptionStatus.ReceiveFailure:
                    case WebExceptionStatus.SendFailure:
                    case WebExceptionStatus.Timeout:
                    case WebExceptionStatus.SecureChannelFailure:
                        return true;
                    default:
                        return false;
                }
            }

            return false;
        }

        private readonly struct Candidate
        {
            public Candidate(string url, string? mirrorPrefix)
            {
                Url = url;
                MirrorPrefix = mirrorPrefix;
            }

            public string Url { get; }

            /// <summary>镜像前缀；<c>null</c> 表示这是官方源。</summary>
            public string? MirrorPrefix { get; }
        }
    }
}
