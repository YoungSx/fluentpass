using System;
using System.Windows;
using System.Windows.Interop;
using WinFormsIWin32Window = System.Windows.Forms.IWin32Window;

namespace KeePassFluentUI.Common
{
    /// <summary>
    /// 将 WPF Window 封装为 WinForms IWin32Window，用于作为原版 WinForms 对话框的模态 Owner，
    /// 彻底防止 WinForms/WPF 交叉模态对话框造成的焦点死锁与层叠错乱。
    /// </summary>
    public sealed class Wpf32Window : WinFormsIWin32Window
    {
        public IntPtr Handle { get; }

        public Wpf32Window(Window wpfWindow)
        {
            if (wpfWindow == null)
                throw new ArgumentNullException(nameof(wpfWindow));

            var helper = new WindowInteropHelper(wpfWindow);
            Handle = helper.EnsureHandle();
        }

        public Wpf32Window(IntPtr handle)
        {
            Handle = handle;
        }
    }
}
