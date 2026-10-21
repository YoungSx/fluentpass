using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using KeePass.Plugins;
using KeePass.UI;
using KeePass.Util;
using KeePassLib;

namespace FluentPass.Services
{
    public sealed class AutoTypeService
    {
        private readonly IPluginHost _host;
        public bool IsActive { get; private set; }
        public bool IsBusy => IsActive || SendInputEx.IsSending;

        public AutoTypeService(IPluginHost host) => _host = host;

        public void RunNative(Action action)
        {
            if (IsBusy) return;
            IsActive = true;
            AutoType.FilterSendPre += ValidateBeforeSend;
            try { action(); }
            finally
            {
                AutoType.FilterSendPre -= ValidateBeforeSend;
                IsActive = false;
            }
        }

        public void Perform(PwEntry? entry, Window? window)
        {
            var db = _host.Database;
            var mw = _host.MainWindow;
            if (IsBusy || !IsLiveEntry(entry, db) || mw == null || mw.IsDisposed || !mw.Enabled ||
                GlobalWindowManager.WindowCount != 0) return;

            RunNative(() =>
            {
                if (window != null) window.WindowState = WindowState.Minimized;
                if (!ReferenceEquals(_host.Database, db) || !IsLiveEntry(entry, db) || mw.IsDisposed ||
                    !mw.Enabled || GlobalWindowManager.WindowCount != 0) return;

                if (window != null && mw.Visible)
                {
                    AutoType.PerformIntoPreviousWindow(mw, entry, db);
                }
                else if (IsExternalForeground())
                {
                    // The hidden native form is unrelated to the target window's Z order.
                    AutoType.PerformIntoCurrentWindow(entry, db);
                }
            });
        }

        private static bool IsLiveEntry(PwEntry? entry, PwDatabase? db) =>
            entry != null && db != null && db.IsOpen && entry.GetAutoTypeEnabled() &&
            ReferenceEquals(db.RootGroup?.FindEntry(entry.Uuid, true), entry);

        private void ValidateBeforeSend(object sender, AutoTypeEventArgs e)
        {
            // KeePass pumps messages after compiling; validate again before any keystroke.
            if (!IsLiveEntry(e.Entry, e.Database) || _host.MainWindow?.Enabled != true ||
                GlobalWindowManager.WindowCount != 0 || !IsExternalForeground()) e.Sequence = string.Empty;
        }

        internal static bool IsExternalForeground()
        {
            var foreground = GetForegroundWindow();
            if (foreground == IntPtr.Zero) return false;
            GetWindowThreadProcessId(foreground, out uint processId);
            return processId != 0 && processId != (uint)Process.GetCurrentProcess().Id &&
                AutoType.IsValidAutoTypeWindow(foreground, false);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    }
}
