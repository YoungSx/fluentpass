using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace KeePassFluentUI.Common
{
    /// <summary>
    /// 记录 Toast 通知内容的数据契约，专供测试验证与历史回溯
    /// </summary>
    public sealed class NotificationRecord
    {
        public string Title { get; }
        public string Message { get; }
        public ControlAppearance Appearance { get; }
        public IconElement? Icon { get; }
        public TimeSpan Timeout { get; }
        public DateTime Timestamp { get; }

        public NotificationRecord(string title, string message, ControlAppearance appearance, IconElement? icon, TimeSpan timeout)
        {
            Title = title ?? string.Empty;
            Message = message ?? string.Empty;
            Appearance = appearance;
            Icon = icon;
            Timeout = timeout;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 符合 WPF-UI v4.0.0 规范的跨线程安全 Toast 通知服务。
    /// 严格对标 ISnackbarService.Show(title, message, appearance, icon, timeout) 官方契约，
    /// 内置 Dispatcher.CheckAccess 调度，保证在后台线程及无 UI 单元测试环境下的绝对健壮性。
    /// </summary>
    public class NotificationService : ISnackbarService
    {
        private static readonly Lazy<NotificationService> _lazyInstance = new Lazy<NotificationService>(() => new NotificationService());
        public static NotificationService Instance => _lazyInstance.Value;

        private readonly object _syncLock = new object();
        private readonly List<NotificationRecord> _history = new List<NotificationRecord>();

        private SnackbarPresenter? _presenter;
        private Snackbar? _snackbar;

        public TimeSpan DefaultTimeOut { get; set; } = TimeSpan.FromSeconds(3);

        public IReadOnlyList<NotificationRecord> Notifications
        {
            get
            {
                lock (_syncLock)
                {
                    return _history.ToArray();
                }
            }
        }

        public NotificationRecord? LastNotificationRecord
        {
            get
            {
                lock (_syncLock)
                {
                    return _history.Count > 0 ? _history[_history.Count - 1] : null;
                }
            }
        }

        public static IReadOnlyList<NotificationRecord> History => Instance.Notifications;

        public static NotificationRecord? LastNotification => Instance.LastNotificationRecord;

        public static void ClearHistory()
        {
            Instance.Clear();
        }

        public void Clear()
        {
            lock (_syncLock)
            {
                _history.Clear();
            }
        }

        public void SetSnackbarPresenter(SnackbarPresenter contentPresenter)
        {
            _presenter = contentPresenter;
            _snackbar = null;
        }

        public SnackbarPresenter? GetSnackbarPresenter()
        {
            return _presenter;
        }

        /// <summary>
        /// 显式实现 ISnackbarService.Show 官方契约接口。
        /// </summary>
        void ISnackbarService.Show(string title, string message, ControlAppearance appearance, IconElement? icon, TimeSpan timeout)
        {
            ShowInternal(title, message, appearance, icon, timeout);
        }

        /// <summary>
        /// 实例级通知投递方法
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

        // ================= 静态便利调用门面 =================

        public static void Show(string title, string message, ControlAppearance appearance = ControlAppearance.Secondary, IconElement? icon = null, TimeSpan duration = default)
        {
            Instance.ShowInternal(title, message, appearance, icon, duration);
        }

        public static void ShowSuccess(string title, string message, int durationSeconds = 3)
        {
            Show(title, message, ControlAppearance.Success, new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(durationSeconds));
        }

        public static void ShowSuccess(string title, string message, TimeSpan duration)
        {
            Show(title, message, ControlAppearance.Success, new SymbolIcon(SymbolRegular.CheckmarkCircle24), duration);
        }

        public static void ShowInformation(string title, string message, int durationSeconds = 3)
        {
            Show(title, message, ControlAppearance.Info, new SymbolIcon(SymbolRegular.Info24), TimeSpan.FromSeconds(durationSeconds));
        }

        public static void ShowInformation(string title, string message, TimeSpan duration)
        {
            Show(title, message, ControlAppearance.Info, new SymbolIcon(SymbolRegular.Info24), duration);
        }

        public static void ShowWarning(string title, string message, int durationSeconds = 3)
        {
            Show(title, message, ControlAppearance.Caution, new SymbolIcon(SymbolRegular.Warning24), TimeSpan.FromSeconds(durationSeconds));
        }

        public static void ShowWarning(string title, string message, TimeSpan duration)
        {
            Show(title, message, ControlAppearance.Caution, new SymbolIcon(SymbolRegular.Warning24), duration);
        }

        public static void ShowError(string title, string message, int durationSeconds = 5)
        {
            Show(title, message, ControlAppearance.Danger, new SymbolIcon(SymbolRegular.DismissCircle24), TimeSpan.FromSeconds(durationSeconds));
        }

        public static void ShowError(string title, string message, TimeSpan duration)
        {
            Show(title, message, ControlAppearance.Danger, new SymbolIcon(SymbolRegular.DismissCircle24), duration);
        }

        public static void SetPresenter(SnackbarPresenter? presenter)
        {
            Instance.SetSnackbarPresenter(presenter!);
        }
    }
}
