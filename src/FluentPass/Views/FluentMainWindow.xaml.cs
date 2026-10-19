using System.Windows.Media;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using KeePass.Plugins;
using FluentPass.Common;
using FluentPass.ViewModels;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxResult = System.Windows.MessageBoxResult;
using MessageBoxImage = System.Windows.MessageBoxImage;
using FormWindowState = System.Windows.Forms.FormWindowState;

namespace FluentPass.Views
{
    public partial class FluentMainWindow : FluentWindow
    {
        private readonly IPluginHost _host;
        private readonly MainViewModel _viewModel;
        private readonly IdleNotificationThrottle _idleThrottle;
        private readonly object _themeWatcherLock = new object();

        private void SafeInvokeOnHost(Action action)
        {
            if (_host.MainWindow == null) return;
            var mw = _host.MainWindow;
            if (mw.IsDisposed || !mw.IsHandleCreated) return;

            if (mw.InvokeRequired)
            {
                mw.Invoke(action);
            }
            else
            {
                action();
            }
        }
        private bool _isThemeWatcherAttached = false;
        private bool _isExplicitExit = false;

        public FluentMainWindow(IPluginHost host, MainViewModel viewModel)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            FluentWindowManager.EnsureWpfApplication();
            try
            {
                if (System.Windows.Application.Current != null && System.Windows.Application.Current.Dispatcher.CheckAccess())
                {
                    System.Windows.Application.Current.MainWindow = this;
                }
            }
            catch { }

            try
            {
                if (Wpf.Ui.UiApplication.Current != null)
                {
                    Wpf.Ui.UiApplication.Current.MainWindow = this;
                }
            }
            catch { }

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

            try
            {
                var theme = ApplicationThemeManager.GetAppTheme();
                WindowBackgroundManager.UpdateBackground(this, theme, WindowBackdropType.Mica);
            }
            catch
            {
                // 安全降级
            }
        }

        public void EnsureThemeWatcher()
        {
            lock (_themeWatcherLock)
            {
                if (_isThemeWatcherAttached) return;
                if (!IsLoaded) return;

                try
                {
                    SystemThemeWatcher.Watch(this, WindowBackdropType.Mica, true);
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

        private void OnTreeViewPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var treeViewItem = FindVisualAncestor<System.Windows.Controls.TreeViewItem>(e.OriginalSource as DependencyObject);
            if (treeViewItem != null)
            {
                treeViewItem.IsSelected = true;
                treeViewItem.Focus();
            }
        }

        private void OnTreeViewContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            _viewModel.RefreshGroupActionStates();
        }

        private static T? FindVisualAncestor<T>(DependencyObject? current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T match)
                    return match;

                if (current is Visual or System.Windows.Media.Media3D.Visual3D)
                {
                    current = VisualTreeHelper.GetParent(current);
                }
                else if (current is FrameworkContentElement fce)
                {
                    current = fce.Parent;
                }
                else
                {
                    break;
                }
            }
            return null;
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
                    else if (e.Key == Key.OemComma)
                    {
                        _viewModel.IsSettingsOpen = !_viewModel.IsSettingsOpen;
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
                else if (e.Key == Key.OemComma)
                {
                    _viewModel.IsSettingsOpen = !_viewModel.IsSettingsOpen;
                    e.Handled = true;
                }
            }
            else if (Keyboard.Modifiers == ModifierKeys.None)
            {
                if (e.Key == Key.Escape)
                {
                    if (_viewModel.IsSettingsOpen)
                    {
                        _viewModel.IsSettingsOpen = false;
                        e.Handled = true;
                    }
                }
                else if (e.Key == Key.Delete)
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

            // 🛡️ 守卫：如果宿主已不存在或已 Disposed，直接放行关闭，杜绝孤儿进程
            if (_host.MainWindow == null || _host.MainWindow.IsDisposed)
            {
                ForceClose();
                return;
            }

            // 🛡️ 守卫：先提交正在内联编辑的临时草稿，确保脏状态同步至 KeePass 数据库
            if (_viewModel.CurrentDetail != null && _viewModel.CurrentDetail.IsEditing)
            {
                _viewModel.CurrentDetail.SaveEditInternal(notify: false);
            }

            var closeAction = SettingsService.GetCloseAction(_host);

            if (closeAction == WindowCloseAction.ExitKeePass)
            {
                // 将退出决策交还给宿主管线统一执行全局保存检查、插件反初始化与安全退出
                e.Cancel = true;
                SafeInvokeOnHost(() =>
                {
                    _host.MainWindow?.Close();
                });
                return;
            }

            // 避免误触关闭直接杀死或丢失未保存数据库
            if (_host.Database != null && _host.Database.IsOpen && _host.Database.Modified)
            {
                var result = MessageService.Show(
                    Loc.Get("Dialog_UnsavedChanges_Prompt"),
                    Loc.Get("Dialog_UnsavedChanges_Title"),
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

            e.Cancel = true;
            Hide();

            if (closeAction == WindowCloseAction.MinimizeToTray)
            {
                SafeInvokeOnHost(() =>
                {
                    var mw = _host.MainWindow;
                    if (mw == null) return;
                    bool hasTray = mw.MainNotifyIcon != null && mw.MainNotifyIcon.Visible;
                    if (!hasTray)
                    {
                        // 托盘不可用时自动安全降级为恢复原版窗口，避免沦为无UI孤儿进程
                        mw.Show();
                        mw.WindowState = FormWindowState.Normal;
                        mw.Activate();
                    }
                    else
                    {
                        if (mw.Visible) mw.Hide();
                    }
                });
            }
            else
            {
                // 默认策略：恢复原版窗口
                SafeInvokeOnHost(() =>
                {
                    var mw = _host.MainWindow;
                    if (mw != null && !mw.Visible)
                    {
                        mw.Show();
                        mw.WindowState = FormWindowState.Normal;
                        mw.Activate();
                    }
                });
            }
        }
    }
}
