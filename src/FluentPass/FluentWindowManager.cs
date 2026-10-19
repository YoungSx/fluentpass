using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms.Integration;
using KeePass.Forms;
using KeePass.Plugins;
using FluentPass.ViewModels;
using FluentPass.Views;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Markup;
using FluentPass.Common;

namespace FluentPass
{
    public static class FluentWindowManager
    {
        private static IPluginHost? _host;
        private static FluentMainWindow? _fluentWindow;
        private static MainViewModel? _viewModel;
        private static bool _hasAutoTakenOver = false;
        private static EventHandler? _shownHandler = null;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Initialize(IPluginHost host)
        {
            _host = host;

            EnsureWpfApplication();
            _viewModel = new MainViewModel(_host);

            if (_host?.MainWindow != null)
            {
                _host.MainWindow.FileOpened += OnHostFileOpened;
                _host.MainWindow.FileClosed += OnHostFileClosed;
                _host.MainWindow.UIStateUpdated += OnHostUIStateUpdated;

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

            if (!SettingsService.GetAutoTakeoverUI(_host)) return;

            // 1. 模态窗口守卫：检测是否有模态窗体或密码输入框开启，或主窗口因系统/Win32模态对话框被禁用
            bool hasModal = !mw.Enabled;
            if (!hasModal)
            {
                try
                {
                    foreach (System.Windows.Forms.Form form in System.Windows.Forms.Application.OpenForms)
                    {
                        if (form.Modal || form.GetType().Name.IndexOf("Key", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            hasModal = true;
                            break;
                        }
                    }
                }
                catch { }
            }

            if (hasModal)
            {
                // 等待数据库打开或模态对话框结束后，在 OnHostFileOpened 中再次接管
                return;
            }

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
            if (_host == null || _viewModel == null) return;

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

        private static void OnHostFileOpened(object sender, FileOpenedEventArgs e)
        {
            _fluentWindow?.Dispatcher.InvokeAsync(() =>
            {
                _viewModel?.RefreshDatabaseState();
            });

            if (!_hasAutoTakenOver && _host != null && SettingsService.GetAutoTakeoverUI(_host))
            {
                _host.MainWindow?.BeginInvoke(new Action(TryPerformAutoTakeover));
            }
        }

        private static void OnHostFileClosed(object sender, FileClosedEventArgs e)
        {
            _fluentWindow?.Dispatcher.InvokeAsync(() =>
            {
                _viewModel?.RefreshDatabaseState();
            });
        }

        private static void OnHostUIStateUpdated(object sender, EventArgs e)
        {
            _fluentWindow?.Dispatcher.InvokeAsync(() =>
            {
                _viewModel?.RefreshDatabaseState();
            });

            if (!_hasAutoTakenOver && _host != null && SettingsService.GetAutoTakeoverUI(_host))
            {
                _host.MainWindow?.BeginInvoke(new Action(TryPerformAutoTakeover));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Shutdown()
        {
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
