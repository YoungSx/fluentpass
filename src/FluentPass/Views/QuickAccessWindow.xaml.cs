using System;
using System.Windows;
using System.Windows.Input;
using KeePass.Plugins;
using FluentPass.ViewModels;
using Wpf.Ui.Controls;

namespace FluentPass.Views
{
    /// <summary>
    /// 命令面板浮层（1Password Quick Access 风格）：失焦自动关闭、Enter 执行默认动作、
    /// Esc 关闭、上下键移动选择。默认动作 = 条目复制密码 / 命令执行。
    /// </summary>
    public partial class QuickAccessWindow : FluentWindow
    {
        private readonly QuickAccessViewModel _viewModel;

        public QuickAccessWindow(IPluginHost host, MainViewModel main)
        {
            InitializeComponent();

            _viewModel = new QuickAccessViewModel(host, main);
            DataContext = _viewModel;

            _viewModel.CloseRequested += OnCloseRequested;
            Loaded += OnWindowLoaded;
        }

        /// <summary>视图模型请求关闭（行内 Auto-Type 前必须让出前台）。</summary>
        private void OnCloseRequested(object? sender, EventArgs e)
        {
            if (IsLoaded) Close();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel.ApplyFilter();
            QueryBox.Focus();
        }

        /// <summary>把浮层定位到宿主窗口顶部居中偏下的位置。</summary>
        public void PositionOver(Window owner)
        {
            if (owner == null) return;

            try
            {
                // Show 之前布局未完成，ActualWidth 可能为 0，退回声明的 Width
                double width = ActualWidth > 0 ? ActualWidth : Width;
                Left = owner.Left + Math.Max(0, (owner.ActualWidth - width) / 2);
                Top = owner.Top + Math.Max(0, Math.Min(120, owner.ActualHeight * 0.15));
            }
            catch { }
        }

        private void OnQueryTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            args.Handled = true;
            _viewModel.SearchText = args.Text;
        }

        private void OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            ExecuteDefault();
        }

        private void OnResultListDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ExecuteDefault();
        }

        private void ExecuteDefault()
        {
            var result = _viewModel.SelectedResult ?? (_viewModel.Results.Count > 0 ? _viewModel.Results[0] : null);
            if (result == null) return;

            _viewModel.ExecuteDefault(result);
            Close();
        }

        private void OnWindowDeactivated(object sender, EventArgs e)
        {
            Close();
        }

        private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
                return;
            }

            if (_viewModel.Results.Count == 0) return;

            int currentIndex = _viewModel.SelectedResult == null
                ? -1
                : _viewModel.Results.IndexOf(_viewModel.SelectedResult);

            if (e.Key == Key.Down)
            {
                int next = Math.Min(_viewModel.Results.Count - 1, currentIndex + 1);
                _viewModel.SelectedResult = _viewModel.Results[next];
                ResultsList.ScrollIntoView(_viewModel.SelectedResult);
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                int prev = Math.Max(0, currentIndex - 1);
                _viewModel.SelectedResult = _viewModel.Results[prev];
                ResultsList.ScrollIntoView(_viewModel.SelectedResult);
                e.Handled = true;
            }
        }
    }
}
