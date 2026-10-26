using System;
using System.Runtime.InteropServices;
using KeePass.Util;

namespace FluentPass.Services
{
    /// <summary>
    /// 前台窗口探针。抽象为接口是为了让 Auto-Type 目标判定可注入，
    /// 从而在无头/自动化会话中不依赖真实桌面焦点（可测试性），
    /// 同时把 Win32 调用隔离在唯一适配器中。
    /// </summary>
    public interface IForegroundWindowProbe
    {
        /// <summary>当前前台窗口句柄；无前台窗口时返回 <see cref="IntPtr.Zero"/>。</summary>
        IntPtr GetForegroundWindow();

        /// <summary>窗口所属进程 ID；无法解析时返回 0。</summary>
        uint GetProcessId(IntPtr window);

        /// <summary>窗口是否为合法 Auto-Type 目标（排除宿主自身、系统窗口等）。</summary>
        bool IsValidAutoTypeTarget(IntPtr window);
    }

    /// <summary>基于 user32 + KeePass 官方判定逻辑的生产探针。</summary>
    public sealed class Win32ForegroundWindowProbe : IForegroundWindowProbe
    {
        public static readonly Win32ForegroundWindowProbe Instance = new Win32ForegroundWindowProbe();

        private Win32ForegroundWindowProbe() { }

        public IntPtr GetForegroundWindow() => NativeMethods.GetForegroundWindow();

        public uint GetProcessId(IntPtr window)
        {
            NativeMethods.GetWindowThreadProcessId(window, out uint processId);
            return processId;
        }

        public bool IsValidAutoTypeTarget(IntPtr window) => AutoType.IsValidAutoTypeWindow(window, false);

        private static class NativeMethods
        {
            [DllImport("user32.dll")]
            internal static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll")]
            internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
        }
    }
}
