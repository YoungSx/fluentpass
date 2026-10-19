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
        private static readonly int[] AvailableAutoMaskSeconds = { 5, 10, 15, 30 };

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

        [ObservableProperty]
        private int _autoMaskSeconds;

        [ObservableProperty]
        private int _autoMaskSecondsIndex;

        [ObservableProperty]
        private string _versionDisplay = "1.0.0";

        public string VersionInfoSummary => Loc.Get("Settings_VersionFormat", VersionDisplay);

        public SettingsViewModel(IPluginHost? host, Action? onCloseSettings = null, Action<string>? onThemeChanged = null)
        {
            _host = host;
            _onCloseSettings = onCloseSettings;
            _onThemeChanged = onThemeChanged;

            _autoTakeoverUI = SettingsService.GetAutoTakeoverUI(_host);
            _closeActionIndex = (int)SettingsService.GetCloseAction(_host);
            _selectedTheme = SettingsService.GetAppTheme(_host);
            _selectedLanguage = SettingsService.GetLanguage(_host);
            _autoMaskSeconds = SettingsService.GetAutoMaskSeconds(_host);
            _autoMaskSecondsIndex = MapSecondsToIndex(_autoMaskSeconds);

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

        private static int MapSecondsToIndex(int seconds)
        {
            for (int i = 0; i < AvailableAutoMaskSeconds.Length; i++)
            {
                if (AvailableAutoMaskSeconds[i] == seconds)
                    return i;
            }
            return 1; // Default: 10 seconds
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
        }

        partial void OnAutoMaskSecondsIndexChanged(int value)
        {
            if (value >= 0 && value < AvailableAutoMaskSeconds.Length)
            {
                AutoMaskSeconds = AvailableAutoMaskSeconds[value];
                SettingsService.SetAutoMaskSeconds(_host, AutoMaskSeconds);
            }
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
        public void CheckUpdate()
        {
            OpenGitHub();
        }

        [RelayCommand]
        public void ResetSettings()
        {
            AutoTakeoverUI = SettingsService.DefaultAutoTakeoverUI;
            CloseActionIndex = (int)SettingsService.DefaultCloseAction;
            SelectedTheme = SettingsService.DefaultAppTheme;
            SelectedLanguage = SettingsService.DefaultLanguage;
            AutoMaskSeconds = SettingsService.DefaultAutoMaskSeconds;
            AutoMaskSecondsIndex = MapSecondsToIndex(AutoMaskSeconds);
        }
    }
}
