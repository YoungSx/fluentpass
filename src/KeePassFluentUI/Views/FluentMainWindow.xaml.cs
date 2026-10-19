using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using KeePass.Plugins;
using KeePassFluentUI.Common;
using KeePassFluentUI.ViewModels;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxResult = System.Windows.MessageBoxResult;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace KeePassFluentUI.Views
{
    public partial class FluentMainWindow : FluentWindow
    {
        private readonly IPluginHost _host;
        private readonly MainViewModel _viewModel;
        private readonly IdleNotificationThrottle _idleThrottle;
        private readonly object _themeWatcherLock = new object();
        private bool _isThemeWatcherAttached = false;
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

            Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (RootSnackbarPresenter != null)
            {
                NotificationService.SetPresenter(RootSnackbarPresenter);
            }

            EnsureThemeWatcher();
        }

        public void EnsureThemeWatcher()
        {
            lock (_themeWatcherLock)
            {
                if (_isThemeWatcherAttached) return;
                if (!IsLoaded) return;

                try
                {
                    SystemThemeWatcher.Watch(this);
                    _isThemeWatcherAttached = true;
                }
                catch
                {
                    // 平台不支持或无句柄时的安全降级
                }
            }
        }

        public void CleanupThemeWatcher()
        {
            lock (_themeWatcherLock)
            {
                if (!_isThemeWatcherAttached) return;

                try
                {
                    SystemThemeWatcher.UnWatch(this);
                }
                catch
                {
                    // 窗口句柄已失效或已解挂时的安全忽略
                }
                finally
                {
                    _isThemeWatcherAttached = false;
                }
            }
        }

        private void OnUserActivityOccurred(object sender, RoutedEventArgs e)
        {
            _idleThrottle.RecordActivity();
        }

        private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is GroupItemViewModel groupVm)
            {
                _viewModel.SelectedGroup = groupVm;
            }
        }

        private void OnListViewItemPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.ListViewItem item)
            {
                item.IsSelected = true;
                item.Focus();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // 文本框焦点守卫：如果焦点在输入框内，放行所有原生编辑键 (Ctrl+C, Ctrl+V, Del 等)
            if (Keyboard.FocusedElement is TextBoxBase ||
                Keyboard.FocusedElement is System.Windows.Controls.PasswordBox)
            {
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
                return;
            }

            // 非文本输入状态下的桌面交互快捷键
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
                else if (e.Key == Key.F)
                {
                    SearchBox?.Focus();
                    SearchBox?.SelectAll();
                    e.Handled = true;
                }
                else if (e.Key == Key.N)
                {
                    _viewModel.NewEntryCommand.Execute(null);
                    e.Handled = true;
                }
                else if (e.Key == Key.C)
                {
                    _viewModel.CopySelectedPasswordCommand.Execute(_viewModel.SelectedEntry);
                    e.Handled = true;
                }
                else if (e.Key == Key.B)
                {
                    _viewModel.CopySelectedUsernameCommand.Execute(_viewModel.SelectedEntry);
                    e.Handled = true;
                }
                else if (e.Key == Key.U)
                {
                    _viewModel.OpenSelectedUrlCommand.Execute(_viewModel.SelectedEntry);
                    e.Handled = true;
                }
                else if (e.Key == Key.V)
                {
                    _viewModel.AutoTypeSelectedCommand.Execute(_viewModel.SelectedEntry);
                    e.Handled = true;
                }
            }
            else if (Keyboard.Modifiers == ModifierKeys.None)
            {
                if (e.Key == Key.Delete)
                {
                    if (_viewModel.SelectedEntry != null)
                    {
                        _viewModel.DeleteEntryCommand.Execute(_viewModel.SelectedEntry);
                        e.Handled = true;
                    }
                }
                else if (e.Key == Key.Enter)
                {
                    if (_viewModel.SelectedEntry != null && _viewModel.CurrentDetail != null && !_viewModel.CurrentDetail.IsEditing)
                    {
                        _viewModel.CurrentDetail.BeginEdit();
                        e.Handled = true;
                    }
                }
            }
        }

        public void ForceClose()
        {
            _isExplicitExit = true;
            CleanupThemeWatcher();
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            CleanupThemeWatcher();
            base.OnClosed(e);
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
                var result = MessageService.Show(
                    "当前数据库有未保存的更改。是否立即保存？",
                    "KeePass Fluent UI",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _viewModel.SaveDatabaseCommand.Execute(null);
                }
                else if (result == MessageBoxResult.Cancel)
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
            }
        }
    }
}
