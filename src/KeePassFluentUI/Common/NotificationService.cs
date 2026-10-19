using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace KeePassFluentUI.Common
{
    /// <summary>
    /// 承载通知记录的数据对象
    /// </summary>
    public sealed class NotificationRecord
    {
        public string Title { get; }
        public string Message { get; }
        public ControlAppearance Appearance { get; }
        public IconElement? Icon { get; }
        public TimeSpan Timeout { get; }
        public DateTime TimestampUtc { get; }

        public NotificationRecord(string title, string message, ControlAppearance appearance, IconElement? icon, TimeSpan timeout)
        {
            Title = title;
            Message = message;
            Appearance = appearance;
            Icon = icon;
            Timeout = timeout;
            TimestampUtc = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 全局 Fluent UI 消息通知与 Toast 广播服务。
    /// 严格对齐官方 Wpf.Ui.ISnackbarService 规范，统一管理视窗全局 ui:SnackbarPresenter 与 ui:Snackbar。
    /// </summary>
    public sealed class NotificationService : ISnackbarService
    {
        private static readonly Lazy<NotificationService> _lazyInstance =
            new Lazy<NotificationService>(() => new NotificationService());

        public static NotificationService Instance => _lazyInstance.Value;

        private readonly object _syncLock = new object();
        private readonly List<NotificationRecord> _history = new List<NotificationRecord>();
        private SnackbarPresenter? _presenter;
        private Snackbar? _snackbar;

        public TimeSpan DefaultTimeOut { get; set; } = TimeSpan.FromSeconds(3);

        public static IReadOnlyList<NotificationRecord> History
        {
            get
            {
                lock (Instance._syncLock)
                {
                    return Instance._history.ToArray();
                }
            }
        }

        public static NotificationRecord? LastNotification
        {
            get
            {
                lock (Instance._syncLock)
                {
                    return Instance._history.Count > 0 ? Instance._history[Instance._history.Count - 1] : null;
                }
            }
        }

        public static void ClearHistory()
        {
            lock (Instance._syncLock)
            {
                Instance._history.Clear();
            }
        }

        // ================= ISnackbarService 规范实现 =================

        public void SetSnackbarPresenter(SnackbarPresenter contentPresenter)
        {
            _presenter = contentPresenter;
            _snackbar = null; // 重置绑定的 Snackbar 实例以适配新宿主
        }

        public SnackbarPresenter GetSnackbarPresenter()
        {
            return _presenter!;
        }

        void ISnackbarService.Show(string title, string message, ControlAppearance appearance, IconElement? icon, TimeSpan timeout)
        {
            ShowInternal(title, message, appearance, icon, timeout);
        }

        // ================= 内部调度与安全防护 =================

        /// <summary>
        /// 实例级通知传递方法
        /// </summary>
        public void ShowNotification(string title, string message, ControlAppearance appearance = ControlAppearance.Secondary, IconElement? icon = null, TimeSpan duration = default)
        {
            ShowInternal(title, message, appearance, icon, duration);
        }

        /// <summary>
        /// 严格对标 ISnackbarService.Show 官方签名的内部执行方法。
        /// 内置跨线程调度与无 UI 单元测试环境容错。
        /// </summary>
        private void ShowInternal(string title, string message, ControlAppearance appearance, IconElement? icon, TimeSpan timeout)
        {
            var duration = timeout.TotalSeconds <= 0 ? DefaultTimeOut : timeout;

            lock (_syncLock)
            {
                _history.Add(new NotificationRecord(title, message, appearance, icon, duration));
            }

            var app = Application.Current;
            if (app == null || app.Dispatcher == null)
            {
                // 无 UI / 单元测试环境直接安全返回
                return;
            }

            if (!app.Dispatcher.CheckAccess())
            {
                app.Dispatcher.BeginInvoke(new Action(() => ShowInternal(title, message, appearance, icon, duration)));
                return;
            }

            if (_presenter == null)
            {
                // Presenter 尚未挂载时不抛出未处理异常
                return;
            }

            try
            {
                if (_snackbar == null)
                {
                    _snackbar = new Snackbar(_presenter);
                }

                _snackbar.SetCurrentValue(Snackbar.TitleProperty, title);
                _snackbar.SetCurrentValue(ContentControl.ContentProperty, message);
                _snackbar.SetCurrentValue(Snackbar.AppearanceProperty, appearance);
                _snackbar.SetCurrentValue(Snackbar.IconProperty, icon);
                _snackbar.SetCurrentValue(Snackbar.TimeoutProperty, duration);
                _snackbar.Show(immediately: true);
            }
            catch
            {
                // 杜绝非致命 Toast 异常阻断主流流程
            }
        }

        // ================= 跨线程安全创建 UI 控件 =================

        private static IconElement? SafeCreateSymbolIcon(SymbolRegular symbol)
        {
            // 仅在 STA 线程中创建 UI 元素，杜绝在 MTA 背景线程抛出 InvalidOperationException
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                try
                {
                    return new SymbolIcon(symbol);
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        // ================= 静态便利调用门面 =================

        public static void Show(string title, string message, ControlAppearance appearance = ControlAppearance.Secondary, IconElement? icon = null, TimeSpan duration = default)
        {
            Instance.ShowInternal(title, message, appearance, icon, duration);
        }

        public static void ShowSuccess(string title, string message, int durationSeconds = 3)
        {
            Show(title, message, ControlAppearance.Success, SafeCreateSymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(durationSeconds));
        }

        public static void ShowSuccess(string title, string message, TimeSpan duration)
        {
            Show(title, message, ControlAppearance.Success, SafeCreateSymbolIcon(SymbolRegular.CheckmarkCircle24), duration);
        }

        public static void ShowInformation(string title, string message, int durationSeconds = 3)
        {
            Show(title, message, ControlAppearance.Info, SafeCreateSymbolIcon(SymbolRegular.Info24), TimeSpan.FromSeconds(durationSeconds));
        }

        public static void ShowInformation(string title, string message, TimeSpan duration)
        {
            Show(title, message, ControlAppearance.Info, SafeCreateSymbolIcon(SymbolRegular.Info24), duration);
        }

        public static void ShowWarning(string title, string message, int durationSeconds = 3)
        {
            Show(title, message, ControlAppearance.Caution, SafeCreateSymbolIcon(SymbolRegular.Warning24), TimeSpan.FromSeconds(durationSeconds));
        }

        public static void ShowWarning(string title, string message, TimeSpan duration)
        {
            Show(title, message, ControlAppearance.Caution, SafeCreateSymbolIcon(SymbolRegular.Warning24), duration);
        }

        public static void ShowError(string title, string message, int durationSeconds = 5)
        {
            Show(title, message, ControlAppearance.Danger, SafeCreateSymbolIcon(SymbolRegular.DismissCircle24), TimeSpan.FromSeconds(durationSeconds));
        }

        public static void ShowError(string title, string message, TimeSpan duration)
        {
            Show(title, message, ControlAppearance.Danger, SafeCreateSymbolIcon(SymbolRegular.DismissCircle24), duration);
        }

        public static void SetPresenter(SnackbarPresenter? presenter)
        {
            Instance.SetSnackbarPresenter(presenter!);
        }
    }
}
