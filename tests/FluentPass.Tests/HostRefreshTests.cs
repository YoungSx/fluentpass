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
        public void HostFileClosed_PublishesClosedStateBeforeClearingDetailsDuringAutoType()
        {
            MessageService.EnableDialogs = false;
            var host = new TestPluginHost();
            SettingsService.SetAutoTakeoverUI(host, false);
            host.Database.RootGroup.AddEntry(TestPluginHost.CreateTestEntry("Entry", "user", "secret"), true);
            FluentWindowManager.Initialize(host);
            var flags = BindingFlags.Static | BindingFlags.NonPublic;
            var vm = (ViewModels.MainViewModel)typeof(FluentWindowManager).GetField("_viewModel", flags)!.GetValue(null)!;
            var closed = typeof(FluentWindowManager).GetMethod("OnHostFileClosed", flags)!;
            try
            {
                vm.SelectedEntry = vm.FilteredEntries[0];
                bool detailCleared = false;
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName != nameof(ViewModels.MainViewModel.CurrentDetail)) return;
                    detailCleared = true;
                    Assert.That(vm.IsDatabaseOpen, Is.False);
                };

                vm.AutoType.RunNative(() =>
                {
                    host.Database.Close();
                    closed.Invoke(null, new object?[] { host, null });
                    Assert.That(detailCleared, Is.True);
                    Assert.That(vm.CurrentDetail, Is.Null);
                });
            }
            finally
            {
                FluentWindowManager.Shutdown();
            }
        }

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
