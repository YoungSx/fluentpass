using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Forms.Integration;
using KeePass.Forms;
using KeePass.Plugins;
using FluentPass.ViewModels;
using FluentPass.Views;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Markup;
using FluentPass.Common;
using FluentPass.Services;

namespace FluentPass
{
    public static class FluentWindowManager
    {
        private static IPluginHost? _host;
        private static FluentMainWindow? _fluentWindow;
        private static MainViewModel? _viewModel;
        private static bool _hasAutoTakenOver = false;
        private static EventHandler? _shownHandler = null;
        private static HostAutoTypeWindow? _autoTypeWindow;
        private static DispatcherTimer? _refreshTimer;
        private static bool _refreshPending;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Initialize(IPluginHost host)
        {
            _host = host;

            Loc.Instance.Initialize(_host);

            // 上次在线更新换装留下的 .old/.new：本进程已不再映射这些文件，此时清理必然成功。
            UpdateService.CleanupLeftoverFiles();

            EnsureWpfApplication();
            _viewModel = new MainViewModel(_host);
            _refreshTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _refreshTimer.Tick += OnRefreshTick;

            if (_host?.MainWindow != null)
            {
                _autoTypeWindow = new HostAutoTypeWindow(_host.MainWindow, _viewModel.AutoType,
                    () => _fluentWindow?.IsLoaded == true, _viewModel.AutoTypeSelectedIntoCurrentWindow);
                _host.MainWindow.FileOpened += OnHostFileOpened;
                _host.MainWindow.FileClosed += OnHostFileClosed;
                _host.MainWindow.UIStateUpdated += OnHostUIStateUpdated;
                _host.MainWindow.FileSaved += OnHostFileSaved;

                if (SettingsService.GetAutoTakeoverUI(_host))
                {
                    HookAutoTakeover(_host.MainWindow);
                }
            }
        }

        private static void HookAutoTakeover(MainForm mw)
        {
            if (_hasAutoTakenOver) return;

            _shownHandler = (s, e) =>
            {
                if (_shownHandler != null)
                {
                    mw.Shown -= _shownHandler;
                    _shownHandler = null;
                }
                mw.BeginInvoke(new Action(TryPerformAutoTakeover));
            };

            mw.Shown += _shownHandler;
        }

        private static void TryPerformAutoTakeover()
        {
            if (_hasAutoTakenOver || _host == null || _host.MainWindow == null) return;
            var mw = _host.MainWindow;
            if (mw.IsDisposed) return;

            if (_viewModel?.AutoType.IsBusy == true) { QueueHostRefresh(); return; }

            if (!SettingsService.GetAutoTakeoverUI(_host)) return;

            // 1. 模态窗口守卫：主窗被禁用、存在模态窗体或原生密码框时不接管
            if (HostWindowGuard.HasModalDialog(_host)) return;

            _hasAutoTakenOver = true;

            bool isMinimized = mw.WindowState == System.Windows.Forms.FormWindowState.Minimized;

            ShowFluentWindow();

            if (isMinimized && _fluentWindow != null)
            {
                _fluentWindow.WindowState = WindowState.Minimized;
            }

            mw.Hide();
        }

        public static void EnsureWpfApplication()
        {
            var app = System.Windows.Application.Current;
            if (app == null)
            {
                app = new System.Windows.Application
                {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown
                };
            }

            EnsureWpfUiDictionaries(app.Resources);
            Loc.Instance.UpdateApplicationResources();
        }

        private static void EnsureWpfUiDictionaries(ResourceDictionary resources)
        {
            bool hasThemes = false;
            bool hasControls = false;

            foreach (var dict in resources.MergedDictionaries)
            {
                if (dict is ThemesDictionary || (dict.Source != null && dict.Source.ToString().IndexOf("/Theme/", StringComparison.OrdinalIgnoreCase) >= 0))
                    hasThemes = true;
                if (dict is ControlsDictionary || (dict.Source != null && dict.Source.ToString().IndexOf("controls.xaml", StringComparison.OrdinalIgnoreCase) >= 0))
                    hasControls = true;
            }

            if (!hasThemes)
            {
                resources.MergedDictionaries.Add(new ThemesDictionary());
            }
            if (!hasControls)
            {
                resources.MergedDictionaries.Add(new ControlsDictionary());
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ShowFluentWindow()
        {
            if (_host == null || _viewModel == null || _viewModel.AutoType.IsBusy) return;

            EnsureWpfApplication();

            if (_fluentWindow == null || !_fluentWindow.IsLoaded)
            {
                _fluentWindow = new FluentMainWindow(_host, _viewModel);
                try
                {
                    if (System.Windows.Application.Current != null && System.Windows.Application.Current.Dispatcher.CheckAccess())
                    {
                        System.Windows.Application.Current.MainWindow = _fluentWindow;
                    }
                }
                catch { }

                try
                {
                    if (UiApplication.Current != null)
                    {
                        UiApplication.Current.MainWindow = _fluentWindow;
                    }
                }
                catch { }

                ElementHost.EnableModelessKeyboardInterop(_fluentWindow);
            }

            _viewModel.RefreshDatabaseState();

            if (!_fluentWindow.IsVisible)
            {
                _fluentWindow.Show();
            }

            _fluentWindow.Activate();

            if (_fluentWindow.WindowState == WindowState.Minimized)
            {
                _fluentWindow.WindowState = WindowState.Normal;
            }
        }

        private static void QueueHostRefresh()
        {
            _refreshPending = true;
            _refreshTimer?.Start();
        }

        private static void OnRefreshTick(object? sender, EventArgs e)
        {
            if (_host == null || _viewModel == null) { _refreshTimer?.Stop(); return; }
            if (_viewModel.AutoType.IsBusy) return;

            _refreshPending = false;
            if (_fluentWindow != null) _viewModel.RefreshDatabaseState();
            TryPerformAutoTakeover();
            if (!_refreshPending) _refreshTimer?.Stop();
        }

        private static void OnHostFileOpened(object sender, FileOpenedEventArgs e) => QueueHostRefresh();

        private static void OnHostFileClosed(object sender, FileClosedEventArgs e)
        {
            // Never defer plaintext cleanup behind an Auto-Type message pump.
            _viewModel?.ClearData();
            QueueHostRefresh();
        }

        private static void OnHostUIStateUpdated(object sender, EventArgs e) => QueueHostRefresh();
        private static void OnHostFileSaved(object sender, FileSavedEventArgs e) => QueueHostRefresh();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Shutdown()
        {
            _autoTypeWindow?.Dispose();
            _autoTypeWindow = null;
            // 附件“打开”功能落盘的临时文件随插件卸载统一清理
            Services.TempAttachmentStore.CleanupAll();
            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
                _refreshTimer.Tick -= OnRefreshTick;
                _refreshTimer = null;
            }
            _refreshPending = false;
            _viewModel?.Dispose();
            if (_host?.MainWindow != null)
            {
                if (_shownHandler != null)
                {
                    _host.MainWindow.Shown -= _shownHandler;
                    _shownHandler = null;
                }
                _host.MainWindow.FileOpened -= OnHostFileOpened;
                _host.MainWindow.FileClosed -= OnHostFileClosed;
                _host.MainWindow.UIStateUpdated -= OnHostUIStateUpdated;
                _host.MainWindow.FileSaved -= OnHostFileSaved;
            }

            _hasAutoTakenOver = false;

            if (_fluentWindow != null)
            {
                try
                {
                    _fluentWindow.ForceClose();
                }
                catch { }
                _fluentWindow = null;
            }

            _viewModel = null;
            _host = null;
        }
    }
}
