using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using KeePass.Forms;
using KeePass.Plugins;
using KeePassFluentUI.ViewModels;
using KeePassFluentUI.Views;
using Wpf.Ui.Markup;

namespace KeePassFluentUI
{
    public sealed class KeePassFluentPlugin : Plugin
    {
        private IPluginHost? _host;
        private FluentMainWindow? _fluentWindow;
        private MainViewModel? _viewModel;

        public override string UpdateUrl => "https://github.com/fluentpass/fluentpass";

        public override Image? SmallIcon
        {
            get
            {
                try
                {
                    // 获取程序集内嵌图标作为 16x16 列表小图标
                    var icoStream = typeof(KeePassFluentPlugin).Assembly.GetManifestResourceStream("KeePassFluentUI.Resources.AppIcon.ico");
                    if (icoStream != null)
                    {
                        using (var ico = new Icon(icoStream, 16, 16))
                        {
                            return ico.ToBitmap();
                        }
                    }
                }
                catch { }
                return null;
            }
        }

        public override bool Initialize(IPluginHost host)
        {
            if (host == null) return false;
            _host = host;

            // 规范 1: 确保 WPF Application 单例与 WPF-UI 资源就绪
            InitializeWpfApplication();

            // 构建 ViewModel
            _viewModel = new MainViewModel(_host);

            // 监听 KeePass 原版事件
            _host.MainWindow.FileOpened += OnHostFileOpened;
            _host.MainWindow.FileClosed += OnHostFileClosed;
            _host.MainWindow.UIStateUpdated += OnHostUIStateUpdated;

            return true;
        }

        private void InitializeWpfApplication()
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

        public void ShowFluentWindow()
        {
            if (_host == null || _viewModel == null) return;

            if (_fluentWindow == null || !_fluentWindow.IsLoaded)
            {
                _fluentWindow = new FluentMainWindow(_host, _viewModel);
                
                // 规范 2: 关键互操作——为非模态 WPF 窗口注入 WinForms 键盘消息循环
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

        public override ToolStripMenuItem? GetMenuItem(PluginMenuType t)
        {
            if (t == PluginMenuType.Main)
            {
                var menuItem = new ToolStripMenuItem("KeePass Fluent UI");
                menuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.F;
                menuItem.Click += (s, e) => ShowFluentWindow();
                return menuItem;
            }

            return null;
        }

        private void OnHostFileOpened(object sender, FileOpenedEventArgs e)
        {
            _fluentWindow?.Dispatcher.InvokeAsync(() =>
            {
                _viewModel?.RefreshDatabaseState();
            });
        }

        private void OnHostFileClosed(object sender, FileClosedEventArgs e)
        {
            _fluentWindow?.Dispatcher.InvokeAsync(() =>
            {
                _viewModel?.RefreshDatabaseState();
            });
        }

        private void OnHostUIStateUpdated(object sender, EventArgs e)
        {
            _fluentWindow?.Dispatcher.InvokeAsync(() =>
            {
                _viewModel?.RefreshDatabaseState();
            });
        }

        public override void Terminate()
        {
            if (_host != null && _host.MainWindow != null)
            {
                _host.MainWindow.FileOpened -= OnHostFileOpened;
                _host.MainWindow.FileClosed -= OnHostFileClosed;
                _host.MainWindow.UIStateUpdated -= OnHostUIStateUpdated;
            }

            if (_fluentWindow != null)
            {
                _fluentWindow.Dispatcher.Invoke(() =>
                {
                    _fluentWindow.ForceClose();
                });
                _fluentWindow = null;
            }

            base.Terminate();
        }
    }
}
