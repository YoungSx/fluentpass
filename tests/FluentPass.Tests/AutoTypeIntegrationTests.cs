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

        /// <summary>
        /// 真实 Win32 链路冒烟：对宿主自身进程的真实窗口句柄，前台判定必须拒绝。
        /// 不依赖“谁抢到焦点”，因此可稳定运行于 CI 与交互桌面（见 issue #41）；
        /// 纯分支覆盖由 AutoTypeForegroundProbeUnitTests 承担。
        /// </summary>
        [Test]
        public void ForegroundInKeePassProcess_IsRejectedAsAutoTypeTarget()
        {
            var window = new System.Windows.Window { Width = 200, Height = 200 };
            try
            {
                window.Show();
                var handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
                Assert.That(handle, Is.Not.EqualTo(IntPtr.Zero), "窗口句柄应已创建");

                // 真实 GetWindowThreadProcessId + 真实“合法 Auto-Type 目标”判定，
                // 仅把“当前前台句柄”固定为自身窗口，解除对桌面焦点的依赖。
                var probe = new OwnProcessForegroundProbe(handle);
                var service = new AutoTypeService(new TestPluginHost(), probe);

                Assert.That(probe.GetProcessId(handle), Is.EqualTo((uint)System.Diagnostics.Process.GetCurrentProcess().Id));
                Assert.That(service.IsExternalForeground(), Is.False);
            }
            finally { window.Close(); }
        }

        /// <summary>前台句柄固定为指定窗口；其余判定走真实 Win32 / KeePass 官方实现。</summary>
        private sealed class OwnProcessForegroundProbe : IForegroundWindowProbe
        {
            private readonly IntPtr _handle;
            private readonly IForegroundWindowProbe _real = Win32ForegroundWindowProbe.Instance;

            public OwnProcessForegroundProbe(IntPtr handle) => _handle = handle;

            public IntPtr GetForegroundWindow() => _handle;
            public uint GetProcessId(IntPtr window) => _real.GetProcessId(window);
            public bool IsValidAutoTypeTarget(IntPtr window) => true;
        }

        /// <summary>
        /// 生产探针真机冒烟：确认 user32 链路真的能取到前台窗口与进程归属。
        /// 无头/服务会话中 GetForegroundWindow 可能返回 Zero，故必须在 CI 中排除
        /// （过滤器 Category!=RequiresInteractiveDesktop），仅供本地/真机验证。
        /// </summary>
        [Test, Category("RequiresInteractiveDesktop")]
        public void ProductionProbe_ResolvesRealForegroundWindow()
        {
            var probe = Win32ForegroundWindowProbe.Instance;
            var foreground = probe.GetForegroundWindow();

            Assert.That(foreground, Is.Not.EqualTo(IntPtr.Zero), "真实桌面会话应存在前台窗口");
            Assert.That(probe.GetProcessId(foreground), Is.Not.Zero, "应能解析前台窗口的进程归属");
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
