using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPass.Common;
using FluentPass.Services;
using KeePass.Plugins;

namespace FluentPass.ViewModels
{
    /// <summary>更新提示条当前表达的语义状态。</summary>
    internal enum UpdatePhase
    {
        None,
        Installed,
        ManualInstallRequired,
        Failed
    }

    /// <summary>
    /// 在线更新状态机（视图无关）：后台检查 → 下载校验 → 进程内换装 → 提示重启。
    /// 网络与文件 IO 全部在 <see cref="UpdateService"/>，本类只维护可绑定状态；
    /// 重启宿主属于视图协同（要先关掉 WPF 窗口），通过 <see cref="RestartRequested"/> 交由窗口执行。
    /// </summary>
    public partial class UpdateViewModel : ObservableObject, IDisposable
    {
        private readonly IPluginHost _host;
        private readonly UpdateService _service;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        /// <summary>
        /// 构造时快照令牌：<see cref="Dispose"/> 释放 CTS 后，仍在线程池里排队的检查
        /// 读取 <c>_cts.Token</c> 会抛 <see cref="ObjectDisposedException"/>，用快照即无此竞态。
        /// </summary>
        private readonly CancellationToken _token;

        private bool _isRunning;
        private bool _isDisposed;
        private UpdatePhase _phase = UpdatePhase.None;
        private Version? _remoteVersion;
        private string? _lastError;
        private string? _downloadedPath;

        /// <summary>请求关闭 WPF 窗口并通过宿主原生管线重启 KeePass。</summary>
        public event EventHandler? RestartRequested;

        [ObservableProperty]
        private bool _isBarVisible;

        [ObservableProperty]
        private string _barTitle = string.Empty;

        [ObservableProperty]
        private string _barMessage = string.Empty;

        [ObservableProperty]
        private UpdateBarSeverity _barSeverity = UpdateBarSeverity.Informational;

        [ObservableProperty]
        private bool _canRestart;

        [ObservableProperty]
        private bool _canOpenFolder;

        [ObservableProperty]
        private bool _isChecking;

        /// <summary>设置页里的一行结果摘要（检查中 / 已是最新 / 失败原因）。</summary>
        [ObservableProperty]
        private string _checkStatusText = string.Empty;

        public bool IsNotChecking => !IsChecking;

        partial void OnIsCheckingChanged(bool value)
        {
            OnPropertyChanged(nameof(IsNotChecking));
        }

        public UpdateViewModel(IPluginHost host)
            : this(host, new UpdateService(
                // 预算归 MirrorAwareTransport 裁定（元数据 20s / 载荷 120s），内层只留同额兜底，
                // 避免内层 30s 默认值抢先把载荷下载杀死。
                new MirrorAwareTransport(
                    new WebClientUpdateTransport(MirrorAwareTransport.DefaultPayloadTimeoutMs),
                    UpdateService.MirrorPrefixes),
                typeof(UpdateService).Assembly.Location))
        {
        }

        /// <summary>测试接缝：注入传输与目标路径后，整个状态机可离线驱动。</summary>
        internal UpdateViewModel(IPluginHost host, UpdateService service)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _token = _cts.Token;
        }

        /// <summary>
        /// 执行一次「检查 + 按需安装」。<paramref name="automatic"/> 为 true 时：
        /// 尊重「忽略此版本」，且网络失败静默不打扰用户。
        /// </summary>
        public async Task RunCheckAsync(bool automatic)
        {
            if (_isDisposed || _isRunning) return;

            _isRunning = true;
            IsChecking = true;
            CheckStatusText = Loc.Get("Update_Checking");

            try
            {
                var skipped = automatic ? SettingsService.GetSkippedUpdateVersion(_host) : null;

                UpdateCheckResult result;
                try
                {
                    result = await Task.Run(
                        () => _service.CheckAndInstallAsync(UpdateService.CurrentVersion, skipped, _token),
                        _token).ConfigureAwait(true);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    result = new UpdateCheckResult(UpdateOutcome.Failed, error: ex.Message);
                }

                Present(result, automatic);
            }
            finally
            {
                _isRunning = false;
                IsChecking = false;
            }
        }

        private void Present(UpdateCheckResult result, bool automatic)
        {
            _remoteVersion = result.RemoteVersion;
            _downloadedPath = result.DownloadedPath;
            _lastError = result.Error;

            switch (result.Outcome)
            {
                case UpdateOutcome.UpToDate:
                    _phase = UpdatePhase.None;
                    IsBarVisible = false;
                    CanRestart = false;
                    CanOpenFolder = false;
                    CheckStatusText = Loc.Get("Update_UpToDateFormat", FormatVersion(UpdateService.CurrentVersion));
                    break;

                case UpdateOutcome.Installed:
                    _phase = UpdatePhase.Installed;
                    CanRestart = true;
                    CanOpenFolder = false;
                    IsBarVisible = true;
                    CheckStatusText = Loc.Get("Update_Ready_Title", FormatVersion(result.RemoteVersion));
                    break;

                case UpdateOutcome.NotWritable:
                    _phase = UpdatePhase.ManualInstallRequired;
                    CanRestart = false;
                    CanOpenFolder = !string.IsNullOrEmpty(_downloadedPath);
                    IsBarVisible = true;
                    CheckStatusText = Loc.Get("Update_Manual_Title", FormatVersion(result.RemoteVersion));
                    break;

                default:
                    _phase = UpdatePhase.Failed;
                    CanRestart = false;
                    CanOpenFolder = false;
                    // 自动检查失败（离线、代理、限流）不应打扰用户，只更新设置页摘要。
                    IsBarVisible = !automatic;
                    CheckStatusText = Loc.Get("Update_FailedFormat", result.Error ?? string.Empty);
                    break;
            }

            RefreshLocalizedText();
        }

        /// <summary>语言切换后重建提示文案；不改变状态机。</summary>
        public void RefreshLocalizedText()
        {
            var remote = FormatVersion(_remoteVersion);
            var current = FormatVersion(UpdateService.CurrentVersion);

            switch (_phase)
            {
                case UpdatePhase.Installed:
                    BarTitle = Loc.Get("Update_Ready_Title", remote);
                    BarMessage = Loc.Get("Update_Ready_Message", remote, current);
                    BarSeverity = UpdateBarSeverity.Success;
                    break;

                case UpdatePhase.ManualInstallRequired:
                    BarTitle = Loc.Get("Update_Manual_Title", remote);
                    BarMessage = Loc.Get("Update_Manual_Message", remote);
                    BarSeverity = UpdateBarSeverity.Warning;
                    break;

                case UpdatePhase.Failed:
                    BarTitle = Loc.Get("Update_Failed_Title");
                    BarMessage = Loc.Get("Update_FailedFormat", _lastError ?? string.Empty);
                    BarSeverity = UpdateBarSeverity.Error;
                    break;

                default:
                    BarTitle = string.Empty;
                    BarMessage = string.Empty;
                    break;
            }
        }

        [RelayCommand]
        private async Task CheckNow()
        {
            await RunCheckAsync(automatic: false).ConfigureAwait(true);
        }

        [RelayCommand]
        private void Restart()
        {
            RestartRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void OpenDownloadFolder()
        {
            var path = _downloadedPath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "/select,\"" + path + "\"",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        /// <summary>忽略此版本：记住版本号并收起提示条，后续同版本不再下载。</summary>
        [RelayCommand]
        private void Dismiss()
        {
            if (_remoteVersion != null)
            {
                SettingsService.SetSkippedUpdateVersion(_host, _remoteVersion.ToString());
            }

            IsBarVisible = false;
            CanRestart = false;
            CanOpenFolder = false;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try { _cts.Cancel(); } catch { }
            _cts.Dispose();
        }

        /// <summary>统一显示为三段式版本号（1.5.1），避免与 4 段内部版本混用造成认知负担。</summary>
        private static string FormatVersion(Version? version)
        {
            if (version == null) return "?";
            return version.Build >= 0 && version.Revision >= 0
                ? $"{version.Major}.{version.Minor}.{version.Build}"
                : version.ToString();
        }
    }
}
