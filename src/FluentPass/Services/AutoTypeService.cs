using System;
using System.Diagnostics;
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
        private readonly IForegroundWindowProbe _foregroundProbe;
        private readonly uint _currentProcessId;
        public bool IsActive { get; private set; }
        public bool IsBusy => IsActive || SendInputEx.IsSending;
        private PwDatabase? _selectedDatabase;

        public AutoTypeService(IPluginHost host, IForegroundWindowProbe? foregroundProbe = null)
        {
            _host = host;
            _foregroundProbe = foregroundProbe ?? Win32ForegroundWindowProbe.Instance;
            _currentProcessId = (uint)Process.GetCurrentProcess().Id;
        }

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

            _selectedDatabase = db;
            try
            {
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
            finally { _selectedDatabase = null; }
        }

        private static bool IsLiveEntry(PwEntry? entry, PwDatabase? db) =>
            entry != null && db != null && db.IsOpen && entry.GetAutoTypeEnabled() &&
            ReferenceEquals(db.RootGroup?.FindEntry(entry.Uuid, true), entry);

        private void ValidateBeforeSend(object sender, AutoTypeEventArgs e)
        {
            // KeePass pumps messages after compiling; validate again before any keystroke.
            if ((_selectedDatabase != null && !ReferenceEquals(_host.Database, _selectedDatabase)) ||
                !IsLiveEntry(e.Entry, e.Database) || _host.MainWindow?.Enabled != true ||
                GlobalWindowManager.WindowCount != 0 || !IsExternalForeground()) e.Sequence = string.Empty;
        }

        internal bool IsExternalForeground()
        {
            var foreground = _foregroundProbe.GetForegroundWindow();
            if (foreground == IntPtr.Zero) return false;

            uint processId = _foregroundProbe.GetProcessId(foreground);
            return processId != 0 && processId != _currentProcessId &&
                _foregroundProbe.IsValidAutoTypeTarget(foreground);
        }
    }
}
