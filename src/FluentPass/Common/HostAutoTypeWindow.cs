using System;
using System.Windows.Forms;
using FluentPass.Services;
using KeePass;
using KeePass.App;
using KeePass.Forms;
using KeePass.UI;

namespace FluentPass.Common
{
    internal sealed class HostAutoTypeWindow : NativeWindow, IDisposable
    {
        private readonly MainForm _host;
        private readonly AutoTypeService _autoType;
        private readonly Func<bool> _ownsSelection;
        private readonly Action _performSelected;

        public HostAutoTypeWindow(MainForm host, AutoTypeService autoType, Func<bool> ownsSelection, Action performSelected)
        {
            _host = host;
            _autoType = autoType;
            _ownsSelection = ownsSelection;
            _performSelected = performSelected;
            host.HandleCreated += OnHandleCreated;
            host.HandleDestroyed += OnHandleDestroyed;
            if (host.IsHandleCreated) AssignHandle(host.Handle);
        }

        private void OnHandleCreated(object? sender, EventArgs e) => AssignHandle(_host.Handle);
        private void OnHandleDestroyed(object? sender, EventArgs e) => ReleaseHandle();

        protected override void WndProc(ref Message m)
        {
            bool hotkey = m.Msg == 0x0312;
            bool appMessage = Program.ApplicationMessage != 0 && m.Msg == Program.ApplicationMessage;
            long command = m.WParam.ToInt64();
            bool selected = hotkey && command == AppDefs.GlobalHotKeyId.AutoTypeSelected ||
                appMessage && command == (int)Program.AppMessage.AutoTypeSelected;
            bool autoType = selected || hotkey && (command == AppDefs.GlobalHotKeyId.AutoType ||
                command == AppDefs.GlobalHotKeyId.AutoTypePassword) ||
                appMessage && (command == (int)Program.AppMessage.AutoType || command == (int)Program.AppMessage.AutoTypePassword);

            if (!autoType) { base.WndProc(ref m); return; }
            if (_autoType.IsBusy) { m.Result = IntPtr.Zero; return; }

            // Keep native hotkey recording and modal workflows in KeePass's own handler.
            if (!_host.Enabled || GlobalWindowManager.WindowCount != 0)
            {
                base.WndProc(ref m);
                return;
            }

            if (selected && !_host.Visible && _ownsSelection())
            {
                _host.NotifyUserActivity();
                _performSelected();
                m.Result = IntPtr.Zero;
                return;
            }

            if (!_autoType.IsExternalForeground()) { m.Result = IntPtr.Zero; return; }

            var message = m;
            _autoType.RunNative(() => base.WndProc(ref message));
            m = message;
        }

        public void Dispose()
        {
            _host.HandleCreated -= OnHandleCreated;
            _host.HandleDestroyed -= OnHandleDestroyed;
            ReleaseHandle();
        }
    }
}
