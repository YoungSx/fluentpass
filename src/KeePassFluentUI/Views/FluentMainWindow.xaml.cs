using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using KeePass.Plugins;
using KeePassFluentUI.Common;
using KeePassFluentUI.ViewModels;
using Wpf.Ui.Controls;

namespace KeePassFluentUI.Views
{
    public partial class FluentMainWindow : FluentWindow
    {
        private readonly IPluginHost _host;
        private readonly MainViewModel _viewModel;
        private readonly IdleNotificationThrottle _idleThrottle;
        private bool _isExplicitExit = false;

        public FluentMainWindow(IPluginHost host, MainViewModel viewModel)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            InitializeComponent();

            DataContext = _viewModel;
            _viewModel.AttachWindow(this);

            _idleThrottle = new IdleNotificationThrottle(_host.MainWindow, throttleSeconds: 1.5);

            // 监听顶层输入，节流通知 KeePass 原生闲置计时器
            PreviewMouseDown += OnUserActivityOccurred;
            PreviewKeyDown += OnUserActivityOccurred;
        }

        private void OnUserActivityOccurred(object sender, RoutedEventArgs e)
        {
            _idleThrottle.RecordActivity();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // 快捷键映射
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.S)
                {
                    _viewModel.SaveDatabaseCommand.Execute(null);
                    e.Handled = true;
                }
                else if (e.Key == Key.L)
                {
                    _viewModel.LockDatabaseCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        public void ForceClose()
        {
            _isExplicitExit = true;
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_isExplicitExit)
            {
                base.OnClosing(e);
                return;
            }

            // 避免误触关闭直接杀死或丢失未保存数据库
            if (_host.Database != null && _host.Database.IsOpen && _host.Database.Modified)
            {
                var result = System.Windows.MessageBox.Show(
                    "当前数据库有未保存的更改。是否立即保存？",
                    "KeePass Fluent UI",
                    System.Windows.MessageBoxButton.YesNoCancel,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    _viewModel.SaveDatabaseCommand.Execute(null);
                }
                else if (result == System.Windows.MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }

            // 默认隐藏 Fluent 窗口并确保原版窗口可见，不粗暴退出宿主进程
            e.Cancel = true;
            Hide();

            if (_host.MainWindow != null && !_host.MainWindow.Visible)
            {
                _host.MainWindow.Show();
                _host.MainWindow.BringToFront();
                _host.MainWindow.Activate();
            }
        }
    }
}
