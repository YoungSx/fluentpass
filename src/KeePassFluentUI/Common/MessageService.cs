using System.Windows;

namespace KeePassFluentUI.Common
{
    /// <summary>
    /// 解耦对话框弹窗，支持自动化测试与非交互模式下禁用模态弹窗，杜绝测试执行死锁。
    /// </summary>
    public static class MessageService
    {
        public static bool EnableDialogs { get; set; } = true;

        public static MessageBoxResult Show(string message, string caption = "KeePass Fluent UI", 
            MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None)
        {
            if (!EnableDialogs)
            {
                // 测试模式下默认确认
                return button == MessageBoxButton.YesNo || button == MessageBoxButton.YesNoCancel 
                    ? MessageBoxResult.Yes 
                    : MessageBoxResult.OK;
            }

            return MessageBox.Show(message, caption, button, icon);
        }
    }
}
