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

namespace FluentPass
{
    public static class FluentWindowManager
    {
        private static IPluginHost? _host;
        private static FluentMainWindow? _fluentWindow;
        private static MainViewModel? _viewModel;

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
            }
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
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Shutdown()
        {
            if (_host?.MainWindow != null)
            {
                _host.MainWindow.FileOpened -= OnHostFileOpened;
                _host.MainWindow.FileClosed -= OnHostFileClosed;
                _host.MainWindow.UIStateUpdated -= OnHostUIStateUpdated;
            }

            if (_fluentWindow != null)
            {
                try
                {
                    _fluentWindow.Close();
                }
                catch { }
                _fluentWindow = null;
            }

            _viewModel = null;
            _host = null;
        }
    }
}
