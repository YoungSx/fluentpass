using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms.Integration;
using KeePass.Forms;
using KeePass.Plugins;
using KeePassFluentUI.ViewModels;
using KeePassFluentUI.Views;
using Wpf.Ui.Markup;

namespace KeePassFluentUI
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

            InitializeWpfApplication();
            _viewModel = new MainViewModel(_host);

            _host.MainWindow.FileOpened += OnHostFileOpened;
            _host.MainWindow.FileClosed += OnHostFileClosed;
            _host.MainWindow.UIStateUpdated += OnHostUIStateUpdated;
        }

        private static void InitializeWpfApplication()
        {
            if (System.Windows.Application.Current == null)
            {
                var app = new System.Windows.Application
                {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown
                };

                app.Resources.MergedDictionaries.Add(new ThemesDictionary());
                app.Resources.MergedDictionaries.Add(new ControlsDictionary());
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ShowFluentWindow()
        {
            if (_host == null || _viewModel == null) return;

            if (_fluentWindow == null || !_fluentWindow.IsLoaded)
            {
                _fluentWindow = new FluentMainWindow(_host, _viewModel);
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
