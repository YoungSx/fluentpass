using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPass.Common;
using KeePass.Plugins;

namespace FluentPass.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        /// <summary>明文自动遮蔽秒数的合法区间（与 <see cref="SettingsService"/> 的持久化校验一致）。</summary>
        public const int MinAutoMaskSeconds = 3;
        public const int MaxAutoMaskSeconds = 300;

        private readonly IPluginHost? _host;
        private readonly Action? _onCloseSettings;
        private readonly Action<string>? _onThemeChanged;

        [ObservableProperty]
        private bool _isSettingsOpen = false;

        [ObservableProperty]
        private bool _autoTakeoverUI;

        [ObservableProperty]
        private int _closeActionIndex;

        [ObservableProperty]
        private string _selectedTheme;

        [ObservableProperty]
        private string _selectedLanguage;

        /// <summary>
        /// NumberBox 绑定值（控件 Value 为 double?）。写入前按 [3,300] 取整校验，
        /// 越界/非法输入不落盘，保持设置值始终可用。
        /// </summary>
        [ObservableProperty]
        private double _autoMaskSecondsValue;

        /// <summary>当前生效的自动遮蔽秒数（取整后的持久化口径）。</summary>
        public int AutoMaskSeconds => (int)Math.Round(AutoMaskSecondsValue);

        [ObservableProperty]
        private string _versionDisplay = "1.0.0";

        /// <summary>启动时后台检查更新（默认开启，可在设置中心关闭）。</summary>
        [ObservableProperty]
        private bool _autoCheckUpdates;

        /// <summary>在线更新状态机（共享实例，由 <see cref="MainViewModel"/> 持有）。</summary>
        public UpdateViewModel? Update { get; }

        public string VersionInfoSummary => Loc.Get("Settings_VersionFormat", VersionDisplay);

        public SettingsViewModel(IPluginHost? host, Action? onCloseSettings = null,
            Action<string>? onThemeChanged = null, UpdateViewModel? update = null)
        {
            _host = host;
            _onCloseSettings = onCloseSettings;
            _onThemeChanged = onThemeChanged;
            Update = update;

            _autoTakeoverUI = SettingsService.GetAutoTakeoverUI(_host);
            _closeActionIndex = (int)SettingsService.GetCloseAction(_host);
            _selectedTheme = SettingsService.GetAppTheme(_host);
            _selectedLanguage = SettingsService.GetLanguage(_host);
            _autoMaskSecondsValue = SettingsService.GetAutoMaskSeconds(_host);
            _autoCheckUpdates = SettingsService.GetAutoCheckUpdates(_host);

            try
            {
                var ver = typeof(SettingsViewModel).Assembly.GetName().Version;
                if (ver != null)
                {
                    _versionDisplay = $"{ver.Major}.{ver.Minor}.{ver.Build}";
                }
            }
            catch { }

            Loc.Instance.LanguageChanged += OnLocLanguageChanged;
        }

        private void OnLocLanguageChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(VersionInfoSummary));
        }

        partial void OnAutoTakeoverUIChanged(bool value)
        {
            SettingsService.SetAutoTakeoverUI(_host, value);
        }

        partial void OnCloseActionIndexChanged(int value)
        {
            if (Enum.IsDefined(typeof(WindowCloseAction), value))
            {
                SettingsService.SetCloseAction(_host, (WindowCloseAction)value);
            }
        }

        partial void OnSelectedThemeChanged(string value)
        {
            SettingsService.SetAppTheme(_host, value);
            _onThemeChanged?.Invoke(value);
        }

        partial void OnSelectedLanguageChanged(string value)
        {
            SettingsService.SetLanguage(_host, value);
            Loc.Instance.SetLanguagePreference(value);
            OnPropertyChanged(nameof(VersionInfoSummary));
            Update?.RefreshLocalizedText();
        }

        partial void OnAutoCheckUpdatesChanged(bool value)
        {
            SettingsService.SetAutoCheckUpdates(_host, value);
        }

        partial void OnAutoMaskSecondsValueChanged(double value)
        {
            int seconds = (int)Math.Round(value);
            if (seconds < MinAutoMaskSeconds || seconds > MaxAutoMaskSeconds) return;

            SettingsService.SetAutoMaskSeconds(_host, seconds);
            OnPropertyChanged(nameof(AutoMaskSeconds));
        }

        [RelayCommand]
        public void CloseSettings()
        {
            IsSettingsOpen = false;
            _onCloseSettings?.Invoke();
        }

        [RelayCommand]
        public void OpenGitHub()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/YoungSx/fluentpass",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        [RelayCommand]
        public void ResetSettings()
        {
            AutoTakeoverUI = SettingsService.DefaultAutoTakeoverUI;
            CloseActionIndex = (int)SettingsService.DefaultCloseAction;
            SelectedTheme = SettingsService.DefaultAppTheme;
            SelectedLanguage = SettingsService.DefaultLanguage;
            AutoMaskSecondsValue = SettingsService.DefaultAutoMaskSeconds;
            AutoCheckUpdates = SettingsService.DefaultAutoCheckUpdates;
        }
    }
}
