using System;
using System.Reflection;
using System.Threading;
using System.Windows.Threading;
using FluentPass.Common;
using FluentPass.Tests.Mocks;
using NUnit.Framework;

namespace FluentPass.Tests
{
    [TestFixture, Apartment(ApartmentState.STA), Category("Integration")]
    public class HostRefreshTests
    {
        [Test]
        public void HostEvents_CoalesceAndRemainPendingUntilAutoTypeFinishes()
        {
            MessageService.EnableDialogs = false;
            var host = new TestPluginHost();
            SettingsService.SetAutoTakeoverUI(host, false);
            FluentWindowManager.Initialize(host);
            var manager = typeof(FluentWindowManager);
            var flags = BindingFlags.Static | BindingFlags.NonPublic;
            var pending = manager.GetField("_refreshPending", flags)!;
            var timer = (DispatcherTimer)manager.GetField("_refreshTimer", flags)!.GetValue(null)!;
            var vm = (ViewModels.MainViewModel)manager.GetField("_viewModel", flags)!.GetValue(null)!;
            var request = manager.GetMethod("OnHostUIStateUpdated", flags)!;
            var tick = manager.GetMethod("OnRefreshTick", flags)!;
            try
            {
                for (int i = 0; i < 50; i++) request.Invoke(null, new object[] { host, EventArgs.Empty });
                Assert.That(timer.IsEnabled, Is.True);
                vm.AutoType.RunNative(() =>
                {
                    tick.Invoke(null, new object?[] { null, EventArgs.Empty });
                    Assert.That(pending.GetValue(null), Is.True);
                    Assert.That(timer.IsEnabled, Is.True);
                });

                tick.Invoke(null, new object?[] { null, EventArgs.Empty });
                Assert.That(pending.GetValue(null), Is.False);
                Assert.That(timer.IsEnabled, Is.False);
            }
            finally
            {
                FluentWindowManager.Shutdown();
            }
            Assert.That(timer.IsEnabled, Is.False);
        }
    }
}
