using System.Windows.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Forms.Integration;
using KeePass.Plugins;
using FluentPass.Common;
using FluentPass.Services;
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
            PreviewMouseWheel += OnUserActivityOccurred;

            // 监听全局滚动事件：当主窗口中任意 ScrollViewer 发生滚动时，若用户名建议列表处于展开状态，立即收起
            AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnAnyScrollViewerScrollChanged));

            _viewModel.QuickAccessRequested += OnQuickAccessRequested;
            _viewModel.Update.RestartRequested += OnUpdateRestartRequested;
            _viewModel.Update.CloseFlyoutRequested += OnUpdateCloseFlyoutRequested;

            Loaded += OnWindowLoaded;
        }

        private QuickAccessWindow? _quickAccessWindow;

        private void OnUpdateCloseFlyoutRequested(object? sender, EventArgs e)
        {
            if (UpdateFlyout != null)
            {
                UpdateFlyout.IsOpen = false;
            }
        }

        /// <summary>titlebar 更新提醒按钮：弹出（或收起）更新 Flyout，并将其精准锚定在更新按钮正下方右对齐，消除漂移。</summary>
        private void OnUpdateButtonClick(object sender, RoutedEventArgs e)
        {
            if (UpdateFlyout == null) return;
            ConfigureUpdateFlyoutPlacement(sender as FrameworkElement ?? UpdateButton);
            UpdateFlyout.IsOpen = !UpdateFlyout.IsOpen;
        }

        private void ConfigureUpdateFlyoutPlacement(FrameworkElement? target = null)
        {
            if (UpdateFlyout == null) return;
            var anchor = target ?? UpdateButton;
            if (anchor == null) return;

            UpdateFlyout.ApplyTemplate();
            if (UpdateFlyout.Template?.FindName("PART_Popup", UpdateFlyout) is Popup popup)
            {
                popup.PlacementTarget = anchor;
                popup.Placement = PlacementMode.Custom;
                popup.CustomPopupPlacementCallback = (popupSize, targetSize, offset) =>
                {
                    // 优选：Flyout 右边缘与 UpdateButton 右边缘对齐，纵向置于按钮正下方 4px
                    var x = targetSize.Width - popupSize.Width;
                    var y = targetSize.Height + 4;
                    return new[]
                    {
                        new CustomPopupPlacement(new Point(x, y), PopupPrimaryAxis.Horizontal),
                        new CustomPopupPlacement(new Point(0, y), PopupPrimaryAxis.Horizontal)
                    };
                };
            }
        }

        private void OnQuickAccessRequested(object? sender, EventArgs e)
        {
            try
            {
                // 安全守卫：自动输入进行中或宿主存在模态/密码窗体时不呼出面板，
                // 否则会抢走焦点并可能把后续击键投递到错误窗口。
                if (_viewModel.AutoType.IsBusy || HostWindowGuard.HasModalDialog(_host)) return;

                // 单例浮层：已打开时仅重新聚焦
                if (_quickAccessWindow != null && _quickAccessWindow.IsLoaded)
                {
                    _quickAccessWindow.Activate();
                    return;
                }

                _quickAccessWindow = new QuickAccessWindow(_host, _viewModel);
                ElementHost.EnableModelessKeyboardInterop(_quickAccessWindow);
                _quickAccessWindow.PositionOver(this);
                _quickAccessWindow.Closed += (_, __) => _quickAccessWindow = null;
                _quickAccessWindow.Show();
            }
            catch
            {
                // 浮层创建失败时静默降级（不影响主窗口）
            }
        }

        /// <summary>双击条目 = 复制密码（与 docs 落地页承诺一致；拖拽路径已由 Preview 链路隔离，不冲突）。</summary>
        private void OnEntryListDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.ListViewItem item && item.DataContext is EntryItemViewModel vm)
            {
                _viewModel.CopySelectedPasswordCommand.Execute(vm);
            }
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (RootSnackbarPresenter != null)
            {
                NotificationService.SetPresenter(RootSnackbarPresenter);
            }

            ConfigureUpdateFlyoutPlacement();

            HookSearchIconSubmit();

            EnsureThemeWatcher();

            // 后台更新检查：窗口显示后才发起，绝不阻塞启动。
            _viewModel.StartBackgroundUpdateCheck();

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

        /// <summary>
        /// 更新换装完成后重启 KeePass：先关闭本 WPF 窗口，再走宿主原生「文件 → 退出」管线
        /// （脏库保存确认与「关闭按钮最小化」配置都由宿主处理），最后宿主重启新实例加载新程序集。
        /// </summary>
        private void OnUpdateRestartRequested(object? sender, EventArgs e)
        {
            if (HostWindowGuard.HasModalDialog(_host))
            {
                MessageService.Show(Loc.Get("Update_Restart_Blocked"), Loc.Get("Update_Failed_Title"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ForceClose();

            if (!HostRestartService.TryRestart(_host, out var error))
            {
                // 换装已经完成，重启失败不影响下次启动生效，如实告知即可。
                MessageService.Show(Loc.Get("Update_RestartFailedFormat", error ?? string.Empty),
                    Loc.Get("Update_Failed_Title"), MessageBoxButton.OK, MessageBoxImage.Warning);
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

            if (e is MouseWheelEventArgs)
            {
                // 当在主窗口外部区域（如页面空白处）滚动滚轮时，若建议列表处于展开状态，立即收起建议列表，防止弹出层悬空脱离输入框
                CloseUserNameSuggestions();
            }
            else if (e is MouseButtonEventArgs mb && mb.ButtonState == MouseButtonState.Pressed)
            {
                // 当在主窗口外部空白处点击时，若建议列表处于展开状态且点击不在输入框内，立即收起
                if (EditUserNameBox != null && EditUserNameBox.IsSuggestionListOpen && !EditUserNameBox.IsMouseOver)
                {
                    CloseUserNameSuggestions();
                }
            }
        }

        private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is GroupItemViewModel groupVm)
            {
                _viewModel.SelectedGroup = groupVm;
            }
        }

        private void OnSearchQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            // 对齐 KeePass 原生 OnQuickFindKeyDown：Enter 无条件重跑快速搜索（以控件当前文本为准）
            _viewModel.SubmitSearch(args.QueryText);
        }

        private void OnSearchBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // WPF-UI 4.3.0 的 AutoSuggestBox.Text 依赖属性不随用户输入回写（未注册双向绑定，
            // 内部 TextBox 仅抛出 TextChanged 事件），官方消费方式就是在该事件中取 args.Text
            args.Handled = true; // 本产品不使用自动建议列表（对齐原生快速搜索语义），同时跳过内部默认过滤（其会对空 OriginalItemsSource 抛异常）
            _viewModel.SearchText = args.Text;

            // 控件在用户输入后总会自动打开建议弹层；在下一次渲染前关闭，避免标题栏下方出现空弹层残影
            sender.Dispatcher.BeginInvoke(() => sender.SetCurrentValue(AutoSuggestBox.IsSuggestionListOpenProperty, false));
        }

        private void OnEditUserNameTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (sender.OriginalItemsSource == null)
            {
                sender.OriginalItemsSource = Array.Empty<string>();
            }

            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput ||
                args.Reason == AutoSuggestionBoxTextChangeReason.SuggestionChosen)
            {
                if (sender.DataContext is EntryDetailViewModel vm)
                {
                    vm.EditUserName = args.Text;
                }
            }
        }

        private void OnEditUserNameSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is string chosen && sender.DataContext is EntryDetailViewModel vm)
            {
                vm.EditUserName = chosen;
            }
        }

        private void OnEditUserNameQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (sender.DataContext is EntryDetailViewModel vm && !string.IsNullOrEmpty(args.QueryText))
            {
                vm.EditUserName = args.QueryText;
            }
            sender.SetCurrentValue(AutoSuggestBox.IsSuggestionListOpenProperty, false);
        }

        private void OnEditUserNamePreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down && sender is AutoSuggestBox box && !box.IsSuggestionListOpen)
            {
                box.SetCurrentValue(AutoSuggestBox.IsSuggestionListOpenProperty, true);
                e.Handled = true;
            }
        }

        private void OnAnyScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (Math.Abs(e.VerticalChange) > 0.001 || Math.Abs(e.HorizontalChange) > 0.001)
            {
                CloseUserNameSuggestions();
            }
        }

        private void OnDetailScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (Math.Abs(e.VerticalChange) > 0.001 || Math.Abs(e.HorizontalChange) > 0.001)
            {
                CloseUserNameSuggestions();
            }
        }

        private void OnDetailScrollViewerPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            CloseUserNameSuggestions();
        }

        private void CloseUserNameSuggestions()
        {
            if (EditUserNameBox != null && EditUserNameBox.IsSuggestionListOpen)
            {
                EditUserNameBox.SetCurrentValue(AutoSuggestBox.IsSuggestionListOpenProperty, false);
            }
        }

        private void OnTreeViewPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 对齐 KeePass 原生 OnGroupsNodeMouseClick：重复点击“已选中”的分组节点时也强制重建条目列表，
            // 使处于搜索结果视图时单击当前分组即可退出搜索结果、回到该分组条目
            if (!_viewModel.IsSearchResultsActive) return;

            var treeViewItem = FindVisualAncestor<System.Windows.Controls.TreeViewItem>(e.OriginalSource as DependencyObject);
            if (treeViewItem?.DataContext is GroupItemViewModel groupVm && ReferenceEquals(groupVm, _viewModel.SelectedGroup))
            {
                _viewModel.LoadGroupEntries(groupVm);
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

        private void TrySelectAllInSearchBox()
        {
            // AutoSuggestBox 未公开 SelectAll；借模板部件 PART_TextBox 实现与原生 Ctrl+E 一致的全选体验
            try
            {
                (SearchBox?.Template?.FindName("PART_TextBox", SearchBox) as System.Windows.Controls.TextBox)?.SelectAll();
            }
            catch { }
        }

        private ContentPresenter? _hookedSearchIconHost;

        /// <summary>
        /// 解析搜索图标宿主。WPF-UI 4.3.0 的 Icon 是纯视觉 ContentPresenter（非按钮，无事件与命令），
        /// 且位于内部 TextBox（PART_TextBox）的模板命名域中——必须经内层模板 FindName 才能取到。
        /// </summary>
        private ContentPresenter? FindSearchIconHost(AutoSuggestBox? box)
        {
            try
            {
                var inner = box?.Template?.FindName("PART_TextBox", box) as System.Windows.Controls.TextBox;
                return inner?.Template?.FindName("ControlIconRight", inner) as ContentPresenter
                    ?? inner?.Template?.FindName("ControlIconLeft", inner) as ContentPresenter;
            }
            catch { return null; }
        }

        private void HookSearchIconSubmit()
        {
            // WPF-UI 的搜索图标无内置点击行为；把提交动作直接挂到图标宿主元素上
            // （PreviewMouseLeftButtonUp 为 Direct 路由：鼠标指针位于图标区域即在本元素触发），
            // 使“点击放大镜 = 提交搜索”等价于 Enter。若主题切换重建模板，下次窗口加载时重新挂接。
            var host = FindSearchIconHost(SearchBox);
            if (host == null || ReferenceEquals(host, _hookedSearchIconHost)) return;

            host.Cursor = System.Windows.Input.Cursors.Hand;
            host.PreviewMouseLeftButtonUp -= OnSearchIconClick;
            host.PreviewMouseLeftButtonUp += OnSearchIconClick;
            _hookedSearchIconHost = host;
        }

        private void OnSearchIconClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            SearchBox?.Focus();
            _viewModel.SubmitSearch();
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

        private const string DragDropFormatEntryList = "FluentPass.EntryItems";
        private Point _dragStartPoint;
        private bool _isPotentialDrag;
        private EntryItemViewModel? _draggedCandidate;
        private System.Windows.Controls.TreeViewItem? _currentDragOverItem;

        private void SetDragHighlight(System.Windows.Controls.TreeViewItem? item)
        {
            if (_currentDragOverItem == item) return;

            ClearDragHighlight();
            if (item != null)
            {
                _currentDragOverItem = item;
                DragDropHelper.SetIsDropTarget(item, true);
            }
        }

        private void ClearDragHighlight()
        {
            if (_currentDragOverItem != null)
            {
                DragDropHelper.SetIsDropTarget(_currentDragOverItem, false);
                _currentDragOverItem = null;
            }
        }

        private void OnListViewItemPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(this);
            _isPotentialDrag = true;
            if (sender is FrameworkElement fe && fe.DataContext is EntryItemViewModel vm)
            {
                _draggedCandidate = vm;
            }
        }

        private void OnListViewItemPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPotentialDrag || e.LeftButton != MouseButtonState.Pressed)
            {
                _isPotentialDrag = false;
                return;
            }

            Point currentPoint = e.GetPosition(this);
            Vector diff = _dragStartPoint - currentPoint;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                _isPotentialDrag = false;

                var selected = EntriesListView.SelectedItems.OfType<EntryItemViewModel>().ToList();
                List<EntryItemViewModel> toDrag;

                if (_draggedCandidate != null && selected.Contains(_draggedCandidate))
                {
                    toDrag = selected;
                }
                else if (_draggedCandidate != null)
                {
                    toDrag = new List<EntryItemViewModel> { _draggedCandidate };
                }
                else
                {
                    toDrag = selected;
                }

                if (toDrag.Count == 0) return;

                var data = new DataObject(DragDropFormatEntryList, toDrag);
                try
                {
                    DragDrop.DoDragDrop(EntriesListView, data, DragDropEffects.Move);
                }
                catch
                {
                    // Drag-drop aborted or interop issue
                }
                finally
                {
                    ClearDragHighlight();
                    _isPotentialDrag = false;
                    _draggedCandidate = null;
                }
            }
        }

        private void OnListViewItemPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isPotentialDrag = false;
            _draggedCandidate = null;
        }

        private void OnTreeViewItemDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DragDropFormatEntryList))
            {
                var treeViewItem = FindVisualAncestor<System.Windows.Controls.TreeViewItem>(e.OriginalSource as DependencyObject)
                                   ?? sender as System.Windows.Controls.TreeViewItem;

                if (treeViewItem?.DataContext is GroupItemViewModel targetGroupVm && targetGroupVm.Model != null)
                {
                    var draggedEntries = e.Data.GetData(DragDropFormatEntryList) as List<EntryItemViewModel>;
                    if (draggedEntries != null && draggedEntries.Count > 0)
                    {
                        bool allAlreadyInTarget = draggedEntries.All(entry => entry.Model.ParentGroup == targetGroupVm.Model);
                        if (allAlreadyInTarget)
                        {
                            ClearDragHighlight();
                            e.Effects = DragDropEffects.None;
                        }
                        else
                        {
                            SetDragHighlight(treeViewItem);
                            e.Effects = DragDropEffects.Move;
                        }

                        e.Handled = true;
                        return;
                    }
                }
            }

            ClearDragHighlight();
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void OnTreeViewItemDragLeave(object sender, DragEventArgs e)
        {
            if (sender is System.Windows.Controls.TreeViewItem item && item == _currentDragOverItem)
            {
                var ancestor = FindVisualAncestor<System.Windows.Controls.TreeViewItem>(e.OriginalSource as DependencyObject);
                if (ancestor != item)
                {
                    ClearDragHighlight();
                }
            }
        }

        private void OnTreeViewItemDrop(object sender, DragEventArgs e)
        {
            ClearDragHighlight();
            if (e.Data.GetDataPresent(DragDropFormatEntryList))
            {
                var treeViewItem = FindVisualAncestor<System.Windows.Controls.TreeViewItem>(e.OriginalSource as DependencyObject)
                                   ?? sender as System.Windows.Controls.TreeViewItem;

                if (treeViewItem?.DataContext is GroupItemViewModel targetGroupVm)
                {
                    var draggedEntries = e.Data.GetData(DragDropFormatEntryList) as List<EntryItemViewModel>;
                    if (draggedEntries != null && draggedEntries.Count > 0)
                    {
                        _viewModel.MoveEntriesToGroup(draggedEntries, targetGroupVm);
                        e.Handled = true;
                        return;
                    }
                }
            }
        }

        private void OnGroupsTreeViewDragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DragDropFormatEntryList))
            {
                ClearDragHighlight();
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            var treeViewItem = FindVisualAncestor<System.Windows.Controls.TreeViewItem>(e.OriginalSource as DependencyObject);
            if (treeViewItem == null)
            {
                ClearDragHighlight();
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }
        }

        private void OnGroupsTreeViewDragLeave(object sender, DragEventArgs e)
        {
            if (sender is FrameworkElement fe)
            {
                Point pos = e.GetPosition(fe);
                if (pos.X < 0 || pos.Y < 0 || pos.X >= fe.ActualWidth || pos.Y >= fe.ActualHeight)
                {
                    ClearDragHighlight();
                }
            }
        }

        private void OnGroupsTreeViewDrop(object sender, DragEventArgs e)
        {
            ClearDragHighlight();
        }

        private void OnListViewItemPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.ListViewItem item)
            {
                if (!item.IsSelected)
                {
                    item.IsSelected = true;
                }
                item.Focus();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // 命令面板快捷键（Ctrl+Shift+P）：文本输入态也放行（避开原生 Ctrl+K=复制条目的高冲突组合）
            if (e.Key == Key.P && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                _viewModel.OpenQuickAccessCommand.Execute(null);
                e.Handled = true;
                return;
            }

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
                    TrySelectAllInSearchBox();
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
                else if (e.Key == Key.D)
                {
                    _viewModel.DuplicateEntryCommand.Execute(_viewModel.SelectedEntry);
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
            _viewModel.Update.RestartRequested -= OnUpdateRestartRequested;
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
