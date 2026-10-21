using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using FluentPass.Services;
using FluentPass.Tests.Mocks;
using FluentPass.ViewModels;
using KeePass;
using KeePass.App;
using KeePass.Forms;
using NUnit.Framework;

namespace FluentPass.Tests
{
    [TestFixture, Apartment(ApartmentState.STA), Category("Integration")]
    public class AutoTypeIntegrationTests
    {
        private static readonly int[] AutoTypeHotkeys =
        {
            AppDefs.GlobalHotKeyId.AutoTypeSelected,
            AppDefs.GlobalHotKeyId.AutoType,
            AppDefs.GlobalHotKeyId.AutoTypePassword
        };

        [Test]
        public void ActivityScope_BlocksNestedPipelinesAndResetsAfterException()
        {
            var service = new AutoTypeService(new TestPluginHost());
            int nested = 0;
            Assert.Throws<InvalidOperationException>(() => service.RunNative(() =>
            {
                Assert.That(service.IsBusy, Is.True);
                service.RunNative(() => nested++);
                throw new InvalidOperationException("fixture");
            }));
            Assert.That(nested, Is.Zero);
            Assert.That(service.IsBusy, Is.False);
            service.RunNative(() => nested++);
            Assert.That(nested, Is.EqualTo(1));
        }

        [Test]
        public void HiddenHost_SelectedMessageUsesShellEvenWithoutASelection()
        {
            using (var host = new MainForm())
            {
                var service = new AutoTypeService(new TestPluginHost { MainWindow = host });
                int selected = 0;
                using (var bridge = CreateBridge(host, service, () => true, () => selected++))
                {
                    SendToBridge(bridge, host, 0x0312, AppDefs.GlobalHotKeyId.AutoTypeSelected);
                    Assert.That(selected, Is.EqualTo(1));
                    Assert.That(host.Visible, Is.False, "No native stale-selection fallback may show the host.");
                }
            }
        }

        [Test]
        public void BusyPipeline_ConsumesAllAutoTypeMessagesBeforeVisibilityOrOwnershipChecks()
        {
            using (var host = new MainForm())
            {
                var service = new AutoTypeService(new TestPluginHost { MainWindow = host });
                int ownershipChecks = 0;
                int selected = 0;
                using (var bridge = CreateBridge(host, service, () => { ownershipChecks++; return true; }, () => selected++))
                {
                    service.RunNative(() =>
                    {
                        foreach (var command in AutoTypeHotkeys) SendToBridge(bridge, host, 0x0312, command);
                        if (Program.ApplicationMessage != 0)
                        {
                            foreach (var command in new[] { Program.AppMessage.AutoType, Program.AppMessage.AutoTypeSelected, Program.AppMessage.AutoTypePassword })
                                SendToBridge(bridge, host, Program.ApplicationMessage, (int)command);
                        }
                    });
                    Assert.That(ownershipChecks, Is.Zero);
                    Assert.That(selected, Is.Zero);
                }
            }
        }

        [Test]
        public void BrowsingWithHiddenHost_DoesNotTouchNativeEntrySelection()
        {
            using (var host = new MainForm())
            {
                var pluginHost = new TestPluginHost { MainWindow = host };
                var entry = TestPluginHost.CreateTestEntry("Fixture", "user", "fixture");
                pluginHost.Database.RootGroup.AddEntry(entry, true);
                var vm = new MainViewModel(pluginHost);
                var list = (ListView)typeof(MainForm).GetField("m_lvEntries", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(host)!;
                var handle = host.Handle;
                var window = new System.Windows.Window();
                try
                {
                    vm.AttachWindow(window);
                    vm.SelectedEntry = null;
                    vm.SelectedEntry = vm.FilteredEntries[0];
                    vm.RefreshDatabaseState();

                    Assert.That(list.Items.Count, Is.Zero);
                    Assert.That(host.Visible, Is.False);
                    Assert.That(vm.SelectedEntry!.Model, Is.SameAs(entry));
                }
                finally { window.Close(); }
            }
        }

        [Test]
        public void ForegroundInKeePassProcess_IsRejectedAsAutoTypeTarget()
        {
            var check = typeof(AutoTypeService).GetMethod("IsExternalForeground", BindingFlags.Static | BindingFlags.NonPublic)!;
            var window = new System.Windows.Window { Width = 200, Height = 200 };
            try
            {
                window.Show();
                window.Activate();
                Assert.That(check.Invoke(null, null), Is.False);
            }
            finally { window.Close(); }
        }

        [Test]
        public void Bridge_DisposeUnsubscribesHandleLifecycle()
        {
            using (var host = new MainForm())
            {
                var service = new AutoTypeService(new TestPluginHost { MainWindow = host });
                var bridge = CreateBridge(host, service, () => true, () => { });
                var nativeWindow = (NativeWindow)bridge;
                var handle = host.Handle;
                Assert.That(nativeWindow.Handle, Is.EqualTo(handle));
                bridge.Dispose();
                Assert.That(nativeWindow.Handle, Is.EqualTo(IntPtr.Zero));
                typeof(Control).GetMethod("RecreateHandle", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(host, null);
                Assert.That(nativeWindow.Handle, Is.EqualTo(IntPtr.Zero));
            }
        }

        private static IDisposable CreateBridge(MainForm host, AutoTypeService service, Func<bool> ownsSelection, Action selected)
        {
            var type = typeof(FluentPassExt).Assembly.GetType("FluentPass.Common.HostAutoTypeWindow")!;
            return (IDisposable)Activator.CreateInstance(type, host, service, ownsSelection, selected)!;
        }

        private static void SendToBridge(IDisposable bridge, MainForm host, int messageId, int command)
        {
            var message = Message.Create(host.Handle, messageId, (IntPtr)command, IntPtr.Zero);
            bridge.GetType().GetMethod("WndProc", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(bridge, new object[] { message });
        }
    }
}
