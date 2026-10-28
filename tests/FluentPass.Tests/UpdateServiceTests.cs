using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentPass.Common;
using FluentPass.Services;
using FluentPass.Tests.Mocks;
using FluentPass.ViewModels;
using NUnit.Framework;

namespace FluentPass.Tests
{
    /// <summary>
    /// 在线更新全链路离线验证：官方 version.txt 解析、SHA256SUMS 校验、暂存换装与
    /// 状态机。网络经 <see cref="IUpdateTransport"/> 注入假实现，任何用例都不出网。
    /// </summary>
    [TestFixture, Apartment(ApartmentState.STA), Category("Unit")]
    public class UpdateServiceTests
    {
        private const string PayloadText = "new-plugin-payload";

        private static readonly byte[] Payload = Encoding.UTF8.GetBytes(PayloadText);

        private string _root = null!;
        private string _targetPath = null!;
        private string _stagingRoot = null!;

        [SetUp]
        public void Setup()
        {
            _root = Path.Combine(Path.GetTempPath(), "fp-update-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);

            _targetPath = Path.Combine(_root, "plugin", UpdateService.PluginFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(_targetPath)!);
            File.WriteAllText(_targetPath, "old-plugin-payload");

            _stagingRoot = Path.Combine(_root, "staging");
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_root, true); }
            catch { }
        }

        // ---------- 版本文件解析 ----------

        [Test]
        public void TryParseVersionFile_ParsesOfficialFormat()
        {
            var text = ":\r\nFluentPass:1.6.0.0\r\n:\r\n";

            Assert.That(UpdateService.TryParseVersionFile(text), Is.EqualTo(new Version(1, 6, 0, 0)));
        }

        [Test]
        public void TryParseVersionFile_IgnoresOtherPluginsAndNoise()
        {
            var text = ":\nOtherPlugin:9.9.9.9\nFluentPass: 1.7.2 \n:\n";

            Assert.That(UpdateService.TryParseVersionFile(text), Is.EqualTo(new Version(1, 7, 2, 0)));
        }

        [Test]
        public void TryParseVersionFile_ReturnsNull_ForEmptyOrMalformed()
        {
            Assert.That(UpdateService.TryParseVersionFile(null), Is.Null);
            Assert.That(UpdateService.TryParseVersionFile(""), Is.Null);
            Assert.That(UpdateService.TryParseVersionFile(":\r\n:\r\n"), Is.Null);
            Assert.That(UpdateService.TryParseVersionFile("OtherPlugin:1.0.0.0"), Is.Null);
            Assert.That(UpdateService.TryParseVersionFile("FluentPass:not-a-version"), Is.Null);
        }

        // ---------- 版本比较 ----------

        [Test]
        public void IsNewer_ComparesFourSegments()
        {
            Assert.That(UpdateService.IsNewer(new Version(1, 5, 1, 0), new Version(1, 5, 2, 0)), Is.True);
            Assert.That(UpdateService.IsNewer(new Version(1, 5, 1, 0), new Version(1, 5, 1, 1)), Is.True);
            Assert.That(UpdateService.IsNewer(new Version(1, 5, 1, 0), new Version(1, 6, 0, 0)), Is.True);
            Assert.That(UpdateService.IsNewer(new Version(1, 5, 1, 0), new Version(1, 5, 1, 0)), Is.False);
            Assert.That(UpdateService.IsNewer(new Version(1, 5, 1, 0), new Version(1, 5, 0, 9)), Is.False);
        }

        [Test]
        public void IsNewer_NormalizesThreeSegmentVersions()
        {
            // 发布侧常写 1.5.1，程序集却是 1.5.1.0：不得误判为更新。
            Assert.That(UpdateService.IsNewer(new Version(1, 5, 1, 0), new Version(1, 5, 1)), Is.False);
            Assert.That(UpdateService.IsNewer(new Version(1, 5, 1), new Version(1, 5, 2)), Is.True);
        }

        // ---------- 校验和解析 ----------

        [Test]
        public void FindChecksum_ParsesGetFileHashOutput()
        {
            var text = "ABCDEF0123  FluentPass.zip\r\n0011223344  FluentPass.dll\r\n";

            Assert.That(UpdateService.FindChecksum(text, "FluentPass.dll"), Is.EqualTo("0011223344"));
            Assert.That(UpdateService.FindChecksum(text, "fluentpass.DLL"), Is.EqualTo("0011223344"));
        }

        [Test]
        public void FindChecksum_AcceptsTabAndAsteriskPrefixedBinaryNames()
        {
            Assert.That(UpdateService.FindChecksum("0011\t*FluentPass.dll", "FluentPass.dll"),
                Is.EqualTo("0011"));
        }

        [Test]
        public void FindChecksum_ReturnsNull_WhenAbsent()
        {
            Assert.That(UpdateService.FindChecksum(null, "FluentPass.dll"), Is.Null);
            Assert.That(UpdateService.FindChecksum("ABCD  Other.dll", "FluentPass.dll"), Is.Null);
            Assert.That(UpdateService.FindChecksum("ABCD  FluentPass.dll", ""), Is.Null);
        }

        // ---------- 哈希 ----------

        [Test]
        public void ComputeSha256_MatchesKnownVector()
        {
            // SHA256("abc")
            var expected = "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD";

            Assert.That(UpdateService.ComputeSha256(Encoding.ASCII.GetBytes("abc")), Is.EqualTo(expected));
        }

        [Test]
        public void VerifyFileHash_DetectsMismatch()
        {
            var actual = UpdateService.ComputeSha256(File.ReadAllBytes(_targetPath));

            Assert.That(UpdateService.VerifyFileHash(_targetPath, actual), Is.True);
            Assert.That(UpdateService.VerifyFileHash(_targetPath, new string('0', 64)), Is.False);
            Assert.That(UpdateService.VerifyFileHash(_targetPath + ".missing", actual), Is.False);
        }

        // ---------- 检查 + 安装 ----------

        [Test]
        public async Task CheckAndInstall_UpToDate_DoesNotDownloadPackage()
        {
            var transport = FakeTransport.WithRelease(new Version(1, 0, 0, 0), Payload);

            var result = await new UpdateService(transport, _targetPath, _stagingRoot)
                .CheckAndInstallAsync(new Version(1, 0, 0, 0), null, CancellationToken.None);

            Assert.That(result.Outcome, Is.EqualTo(UpdateOutcome.UpToDate));
            Assert.That(transport.StringRequests, Has.Count.EqualTo(1));
            Assert.That(transport.ByteRequests, Is.Empty);
            Assert.That(File.ReadAllText(_targetPath), Is.EqualTo("old-plugin-payload"));
        }

        [Test]
        public async Task CheckAndInstall_SkippedVersion_IsTreatedAsUpToDate()
        {
            var transport = FakeTransport.WithRelease(new Version(2, 0, 0, 0), Payload);

            var result = await new UpdateService(transport, _targetPath, _stagingRoot)
                .CheckAndInstallAsync(new Version(1, 0, 0, 0), "2.0.0.0", CancellationToken.None);

            Assert.That(result.Outcome, Is.EqualTo(UpdateOutcome.UpToDate));
            Assert.That(transport.ByteRequests, Is.Empty);
        }

        [Test]
        public async Task CheckAndInstall_Installed_SwapsTargetAndKeepsBackup()
        {
            var transport = FakeTransport.WithRelease(new Version(2, 0, 0, 0), Payload);

            var result = await new UpdateService(transport, _targetPath, _stagingRoot)
                .CheckAndInstallAsync(new Version(1, 0, 0, 0), null, CancellationToken.None);

            Assert.That(result.Outcome, Is.EqualTo(UpdateOutcome.Installed));
            Assert.That(result.RemoteVersion, Is.EqualTo(new Version(2, 0, 0, 0)));
            Assert.That(File.ReadAllText(_targetPath), Is.EqualTo(PayloadText));
            Assert.That(File.ReadAllText(_targetPath + ".old"), Is.EqualTo("old-plugin-payload"));

            // 暂存文件已被移入目标位，不留副本
            Assert.That(File.Exists(Path.Combine(_stagingRoot, "2.0.0.0", UpdateService.PluginFileName)), Is.False);
        }

        [Test]
        public async Task CheckAndInstall_HashMismatch_FailsWithoutTouchingTarget()
        {
            var transport = FakeTransport.WithRelease(new Version(2, 0, 0, 0), Payload);
            transport.Checksums[UpdateService.ChecksumsUrl] =
                UpdateService.ComputeSha256(Encoding.UTF8.GetBytes("something else")) + "  " + UpdateService.PluginFileName;

            var result = await new UpdateService(transport, _targetPath, _stagingRoot)
                .CheckAndInstallAsync(new Version(1, 0, 0, 0), null, CancellationToken.None);

            Assert.That(result.Outcome, Is.EqualTo(UpdateOutcome.Failed));
            Assert.That(File.ReadAllText(_targetPath), Is.EqualTo("old-plugin-payload"));
            Assert.That(File.Exists(_targetPath + ".old"), Is.False);
        }

        [Test]
        public async Task CheckAndInstall_MissingChecksumEntry_FailsWithoutDownloadingPackage()
        {
            var transport = FakeTransport.WithRelease(new Version(2, 0, 0, 0), Payload);
            transport.Checksums[UpdateService.ChecksumsUrl] = "ABCD  Other.dll";

            var result = await new UpdateService(transport, _targetPath, _stagingRoot)
                .CheckAndInstallAsync(new Version(1, 0, 0, 0), null, CancellationToken.None);

            Assert.That(result.Outcome, Is.EqualTo(UpdateOutcome.Failed));
            Assert.That(transport.ByteRequests, Is.Empty);
            Assert.That(File.ReadAllText(_targetPath), Is.EqualTo("old-plugin-payload"));
        }

        [Test]
        public async Task CheckAndInstall_TransportFailure_FailsWithoutThrowing()
        {
            var transport = FakeTransport.WithRelease(new Version(2, 0, 0, 0), Payload);
            transport.Failure = new TimeoutException("net down");

            var result = await new UpdateService(transport, _targetPath, _stagingRoot)
                .CheckAndInstallAsync(new Version(1, 0, 0, 0), null, CancellationToken.None);

            Assert.That(result.Outcome, Is.EqualTo(UpdateOutcome.Failed));
            Assert.That(result.Error, Does.Contain("net down"));
            Assert.That(File.ReadAllText(_targetPath), Is.EqualTo("old-plugin-payload"));
        }

        [Test]
        public async Task CheckAndInstall_Cancellation_Propagates()
        {
            var transport = FakeTransport.WithRelease(new Version(2, 0, 0, 0), Payload);
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();

                Assert.ThrowsAsync<OperationCanceledException>(async () =>
                    await new UpdateService(transport, _targetPath, _stagingRoot)
                        .CheckAndInstallAsync(new Version(1, 0, 0, 0), null, cts.Token));
            }
        }

        [Test]
        public async Task CheckAndInstall_UnwritableTarget_ReportsNotWritableAndKeepsFolder()
        {
            var transport = FakeTransport.WithRelease(new Version(2, 0, 0, 0), Payload);
            var pluginDir = Path.GetDirectoryName(_targetPath)!;

            using (var deny = new DenyWriteScope(pluginDir))
            {
                if (!deny.Applied)
                {
                    Assert.Ignore("当前环境无法设置目录 ACL 拒绝规则，跳过只读目录用例");
                }

                var result = await new UpdateService(transport, _targetPath, _stagingRoot)
                    .CheckAndInstallAsync(new Version(1, 0, 0, 0), null, CancellationToken.None);

                Assert.That(result.Outcome, Is.EqualTo(UpdateOutcome.NotWritable));
                Assert.That(result.DownloadedPath, Is.Not.Null.And.Not.Empty);
                Assert.That(File.Exists(result.DownloadedPath!), Is.True, "暂存文件必须保留以便人工替换");
                Assert.That(File.ReadAllText(_targetPath), Is.EqualTo("old-plugin-payload"));
                Assert.That(File.Exists(_targetPath + ".old"), Is.False);
            }
        }

        // ---------- 换装 ----------

        [Test]
        public void ApplyStagedUpdate_ReplacesStaleBackup()
        {
            var staged = Path.Combine(_root, "staged.dll");
            File.WriteAllText(staged, PayloadText);
            File.WriteAllText(_targetPath + ".old", "stale-backup");

            var result = UpdateService.ApplyStagedUpdate(staged, _targetPath, out var error);

            Assert.That(result, Is.EqualTo(UpdateApplyResult.Applied));
            Assert.That(error, Is.Null);
            Assert.That(File.ReadAllText(_targetPath), Is.EqualTo(PayloadText));
            Assert.That(File.ReadAllText(_targetPath + ".old"), Is.EqualTo("old-plugin-payload"));
        }

        [Test]
        public void ApplyStagedUpdate_RollsBack_WhenStagedMoveFails()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                Assert.Ignore("当前环境为非 Windows NT，文件独占锁无法阻止 File.Move");
            }

            var staged = Path.Combine(_root, "staged.dll");
            File.WriteAllText(staged, PayloadText);

            using (new FileStream(staged, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                var result = UpdateService.ApplyStagedUpdate(staged, _targetPath, out var error);

                Assert.That(result, Is.EqualTo(UpdateApplyResult.Failed));
                Assert.That(error, Is.Not.Null.And.Not.Empty);
                Assert.That(File.ReadAllText(_targetPath), Is.EqualTo("old-plugin-payload"), "必须回滚到旧文件");
                Assert.That(File.Exists(_targetPath + ".old"), Is.False);
            }
        }

        [Test]
        public void ApplyStagedUpdate_MissingStagedFile_FailsWithoutTouchingTarget()
        {
            var result = UpdateService.ApplyStagedUpdate(
                Path.Combine(_root, "nope.dll"), _targetPath, out var error);

            Assert.That(result, Is.EqualTo(UpdateApplyResult.Failed));
            Assert.That(error, Does.Contain("暂存文件缺失"));
            Assert.That(File.ReadAllText(_targetPath), Is.EqualTo("old-plugin-payload"));
        }

        [Test]
        public void CleanupLeftoverFiles_RemovesOldAndNewSiblings()
        {
            File.WriteAllText(_targetPath + ".old", "old");
            File.WriteAllText(_targetPath + ".new", "new");

            UpdateService.CleanupLeftoverFiles(_targetPath);

            Assert.That(File.Exists(_targetPath + ".old"), Is.False);
            Assert.That(File.Exists(_targetPath + ".new"), Is.False);
            Assert.That(File.Exists(_targetPath), Is.True);
        }

        // ---------- 视图状态机 ----------

        /// <summary>相对当前程序集版本的下一个版本，避免测试与真实版本号耦合。</summary>
        private static Version NextVersion()
        {
            var current = UpdateService.CurrentVersion;
            return new Version(current.Major, current.Minor, current.Build + 1, 0);
        }

        [Test]
        public async Task ViewModel_Installed_ShowsBarAndAllowsRestart()
        {
            var (vm, _) = CreateViewModel(NextVersion(), Payload, hashMatches: true);

            await vm.RunCheckAsync(automatic: true);

            Assert.That(vm.IsBarVisible, Is.True);
            Assert.That(vm.CanRestart, Is.True);
            Assert.That(vm.CanOpenFolder, Is.False);
            Assert.That(vm.BarSeverity, Is.EqualTo(UpdateBarSeverity.Success));
            Assert.That(vm.BarTitle, Is.Not.Empty);
            Assert.That(vm.IsChecking, Is.False);
        }

        [Test]
        public async Task ViewModel_UpToDate_HidesTitleBarButton()
        {
            var (vm, _) = CreateViewModel(UpdateService.CurrentVersion, Payload, hashMatches: true);

            await vm.RunCheckAsync(automatic: true);

            // 已是最新：titlebar 提醒按钮不出现，结论只在设置页摘要可见。
            Assert.That(vm.IsBarVisible, Is.False);
            Assert.That(vm.CanRestart, Is.False);
            Assert.That(vm.CanOpenFolder, Is.False);
            Assert.That(vm.CheckStatusText, Is.Not.Empty);
        }

        [Test]
        public async Task ViewModel_AutomaticFailure_StaysSilentButReportsInSettings()
        {
            var (vm, _) = CreateViewModel(NextVersion(), Payload, hashMatches: true,
                failure: new TimeoutException("offline"));

            await vm.RunCheckAsync(automatic: true);

            Assert.That(vm.IsBarVisible, Is.False, "自动检查失败不应打扰用户");
            Assert.That(vm.CheckStatusText, Does.Contain("offline"));
        }

        [Test]
        public async Task ViewModel_CheckFailure_HidesBar_EvenWhenManual()
        {
            var (vm, _) = CreateViewModel(NextVersion(), Payload, hashMatches: true,
                failure: new TimeoutException("offline"));

            await vm.RunCheckAsync(automatic: false);

            Assert.That(vm.IsBarVisible, Is.False, "检测更新失败绝不显示 TitleBar 图标");
            Assert.That(vm.CanDownloadManually, Is.False);
            Assert.That(vm.CanRestart, Is.False);
            Assert.That(vm.CheckStatusText, Does.Contain("offline"));
        }

        [Test]
        public async Task ViewModel_DownloadFailure_ShowsBar_AndEnablesManualDownload()
        {
            var remote = NextVersion();
            var (vm, _) = CreateViewModel(remote, Payload, hashMatches: false);

            await vm.RunCheckAsync(automatic: true);

            Assert.That(vm.IsBarVisible, Is.True, "下载失败应显示 TitleBar 图标");
            Assert.That(vm.CanDownloadManually, Is.True, "下载失败应提供手动下载入口");
            Assert.That(vm.CanRestart, Is.False);
            Assert.That(vm.CanOpenFolder, Is.False);
            Assert.That(vm.BarSeverity, Is.EqualTo(UpdateBarSeverity.Error));
            Assert.That(vm.BarTitle, Does.Contain(remote.ToString(3)));
            Assert.That(vm.CheckStatusText, Does.Contain(remote.ToString(3)));
        }

        [Test]
        public async Task ViewModel_DismissOnDownloadFailure_HidesBar_AndRemembersVersion()
        {
            var host = new TestPluginHost();
            var remote = NextVersion();
            var (vm, _) = CreateViewModel(remote, Payload, hashMatches: false, host: host);

            await vm.RunCheckAsync(automatic: true);
            Assert.That(vm.IsBarVisible, Is.True);
            Assert.That(vm.CanDownloadManually, Is.True);

            vm.DismissCommand.Execute(null);

            Assert.That(vm.IsBarVisible, Is.False);
            Assert.That(vm.CanDownloadManually, Is.False);
            Assert.That(SettingsService.GetSkippedUpdateVersion(host), Is.EqualTo(remote.ToString()));
        }

        [Test]
        public async Task ViewModel_NotWritable_OffersManualInstall()
        {
            var (vm, transport) = CreateViewModel(NextVersion(), Payload, hashMatches: true);
            var pluginDir = Path.GetDirectoryName(_targetPath)!;

            using (var deny = new DenyWriteScope(pluginDir))
            {
                if (!deny.Applied)
                {
                    Assert.Ignore("当前环境无法设置目录 ACL 拒绝规则，跳过只读目录用例");
                }

                await vm.RunCheckAsync(automatic: false);

                Assert.That(vm.IsBarVisible, Is.True);
                Assert.That(vm.BarSeverity, Is.EqualTo(UpdateBarSeverity.Warning));
                Assert.That(vm.CanRestart, Is.False);
                Assert.That(vm.CanOpenFolder, Is.True);
                Assert.That(transport.ByteRequests, Has.Count.EqualTo(1));
            }
        }

        [Test]
        public async Task ViewModel_Dismiss_RemembersSkippedVersion()
        {
            var host = new TestPluginHost();
            var remote = NextVersion();
            var (vm, _) = CreateViewModel(remote, Payload, hashMatches: true, host: host);

            await vm.RunCheckAsync(automatic: true);
            Assert.That(vm.IsBarVisible, Is.True);

            vm.DismissCommand.Execute(null);

            Assert.That(vm.IsBarVisible, Is.False);
            Assert.That(vm.CanRestart, Is.False);
            Assert.That(SettingsService.GetSkippedUpdateVersion(host), Is.EqualTo(remote.ToString()));
        }

        [Test]
        public async Task ViewModel_IgnoresReentrantChecks()
        {
            var (vm, transport) = CreateViewModel(NextVersion(), Payload, hashMatches: true);
            transport.Delay = TimeSpan.FromMilliseconds(100);

            var first = vm.RunCheckAsync(automatic: true);
            var second = vm.RunCheckAsync(automatic: true);
            await Task.WhenAll(first, second);

            Assert.That(transport.ByteRequests, Has.Count.EqualTo(1));
        }

        [Test]
        public void ViewModel_Dispose_CancelsInFlightCheck()
        {
            var (vm, transport) = CreateViewModel(NextVersion(), Payload, hashMatches: true);
            transport.Delay = TimeSpan.FromSeconds(5);

            var pending = vm.RunCheckAsync(automatic: true);
            vm.Dispose();

            Assert.That(async () => await pending, Throws.Nothing);
        }

        // ---------- 测试脚手架 ----------

        private (UpdateViewModel Vm, FakeTransport Transport) CreateViewModel(
            Version remoteVersion, byte[] payload, bool hashMatches,
            Exception? failure = null, TestPluginHost? host = null)
        {
            var transport = FakeTransport.WithRelease(remoteVersion, payload);
            if (!hashMatches)
            {
                transport.Checksums[UpdateService.ChecksumsUrl] =
                    UpdateService.ComputeSha256(Encoding.UTF8.GetBytes("other")) + "  " + UpdateService.PluginFileName;
            }
            transport.Failure = failure;

            var service = new UpdateService(transport, _targetPath, _stagingRoot);
            var vm = new UpdateViewModel(host ?? new TestPluginHost(), service);
            return (vm, transport);
        }

        private sealed class FakeTransport : IUpdateTransport
        {
            public Dictionary<string, string> Strings { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
            public Dictionary<string, string> Checksums { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
            public Dictionary<string, byte[]> Bytes { get; } = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            public List<string> StringRequests { get; } = new List<string>();
            public List<string> ByteRequests { get; } = new List<string>();
            public Exception? Failure { get; set; }
            public TimeSpan Delay { get; set; } = TimeSpan.Zero;

            public static FakeTransport WithRelease(Version version, byte[] payload)
            {
                var transport = new FakeTransport();
                transport.Strings[UpdateService.VersionFileUrl] =
                    ":\nFluentPass:" + version + "\n:\n";
                transport.Checksums[UpdateService.ChecksumsUrl] =
                    UpdateService.ComputeSha256(payload) + "  " + UpdateService.PluginFileName;
                transport.Bytes[UpdateService.DownloadUrl] = payload;
                return transport;
            }

            public async Task<string> DownloadStringAsync(string url, CancellationToken cancellationToken)
            {
                StringRequests.Add(url);
                await DelayAsync(cancellationToken).ConfigureAwait(false);

                if (Strings.TryGetValue(url, out var text)) return text;
                if (Checksums.TryGetValue(url, out var checksums)) return checksums;

                throw new FileNotFoundException("unexpected url: " + url);
            }

            public async Task<byte[]> DownloadBytesAsync(string url, CancellationToken cancellationToken)
            {
                ByteRequests.Add(url);
                await DelayAsync(cancellationToken).ConfigureAwait(false);

                if (Bytes.TryGetValue(url, out var bytes)) return bytes;

                throw new FileNotFoundException("unexpected url: " + url);
            }

            private async Task DelayAsync(CancellationToken cancellationToken)
            {
                if (Delay > TimeSpan.Zero)
                {
                    await Task.Delay(Delay, cancellationToken).ConfigureAwait(false);
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (Failure != null) throw Failure;
            }
        }

        /// <summary>
        /// 在指定目录上叠加 DENY 写/删 ACE，模拟「插件装在 Program Files、宿主未提权」的换装失败。
        /// 若环境不允许改 ACL（例如以 SYSTEM 运行），<see cref="Applied"/> 为 false，调用方跳过用例。
        /// </summary>
        private sealed class DenyWriteScope : IDisposable
        {
            private readonly DirectoryInfo _directory;
            private readonly FileSystemAccessRule _rule;
            private bool _aclSet;
            private bool _applied;

            public DenyWriteScope(string directory)
            {
                _directory = new DirectoryInfo(directory);
                var identity = WindowsIdentity.GetCurrent().Name;
                _rule = new FileSystemAccessRule(identity,
                    FileSystemRights.WriteData | FileSystemRights.CreateFiles |
                    FileSystemRights.Delete | FileSystemRights.DeleteSubdirectoriesAndFiles,
                    AccessControlType.Deny);

                try
                {
                    var security = _directory.GetAccessControl();
                    security.AddAccessRule(_rule);
                    _directory.SetAccessControl(security);
                    _aclSet = true;
                    _applied = !WriteStillSucceeds();
                }
                catch
                {
                    _aclSet = false;
                    _applied = false;
                }
            }

            /// <summary>持有备份/还原权限的运行环境会绕过 DENY ACE，此时换装不会失败，用例无意义。</summary>
            private bool WriteStillSucceeds()
            {
                var probe = Path.Combine(_directory.FullName, "acl-probe-" + Guid.NewGuid().ToString("N") + ".tmp");
                try
                {
                    File.WriteAllText(probe, "probe");
                    return true;
                }
                catch (UnauthorizedAccessException)
                {
                    return false;
                }
            }

            public bool Applied => _applied;

            public void Dispose()
            {
                if (!_aclSet) return;

                try
                {
                    var security = _directory.GetAccessControl();
                    security.RemoveAccessRule(_rule);
                    _directory.SetAccessControl(security);
                }
                catch { }
            }
        }
    }
}