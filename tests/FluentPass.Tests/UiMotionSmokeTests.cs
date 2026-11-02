using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using FluentPass.Common;
using FluentPass.Tests.Mocks;
using FluentPass.ViewModels;
using FluentPass.Views;
using NUnit.Framework;

namespace FluentPass.Tests
{
    [TestFixture, Apartment(ApartmentState.STA), Category("Smoke")]
    public class UiMotionSmokeTests
    {
        private TestPluginHost _host = null!;
        private MainViewModel _vm = null!;
        private FluentMainWindow _window = null!;
        private FrameworkElement[] _hosts = null!;

        [SetUp]
        public void SetUp()
        {
            MessageService.EnableDialogs = false;
            _host = new TestPluginHost();
            SettingsService.SetAutoCheckUpdates(_host, false);
            SettingsService.SetAnimationsEnabled(_host, false);
            _host.Database.RootGroup.AddEntry(TestPluginHost.CreateTestEntry("Entry", "user", "secret"), true);
            _vm = new MainViewModel(_host);
            _window = new FluentMainWindow(_host, _vm);
            _window.Show();
            _window.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() => { }));
            Assert.That(_window.IsLoaded, Is.True);
            _vm.SelectedEntry = _vm.FilteredEntries[0];
            _vm.Settings.AnimationsEnabled = true;
            _hosts = new[] { "WorkspaceHost", "SettingsHost", "DetailScrollViewer", "DetailEmptyState" }
                .Select(name => (FrameworkElement)_window.FindName(name)).ToArray();
        }

        [TearDown]
        public void TearDown()
        {
            _window.ForceClose();
            _vm.Dispose();
            _host.Database.Close();
        }

        // Seed a running fade even on software-rendered CI, where WPF-UI skips animations.
        private void SeedFades()
        {
            foreach (var host in _hosts)
                host.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.2, 0.2, Duration.Forever));
        }

        private static void AssertNoFade(params FrameworkElement[] hosts)
        {
            foreach (var host in hosts)
            {
                Assert.That(host.HasAnimatedProperties, Is.False, host.Name);
                Assert.That(host.Opacity, Is.EqualTo(1), host.Name);
            }
        }

        [Test]
        public void LockingDatabase_ImmediatelyStopsAllTransitions()
        {
            SeedFades();
            _vm.IsLocked = true;
            AssertNoFade(_hosts);
        }

        [Test]
        public void ClosingDatabase_PublishesClosedStateBeforeClearingDetails()
        {
            SeedFades();
            bool detailCleared = false;
            _vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(MainViewModel.CurrentDetail)) return;
                detailCleared = true;
                Assert.That(_vm.IsDatabaseOpen, Is.False);
                AssertNoFade(_hosts);
            };

            _host.Database.Close();
            _vm.RefreshDatabaseState();

            Assert.That(detailCleared, Is.True);
            Assert.That(_vm.CurrentDetail, Is.Null);
            AssertNoFade(_hosts);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SettingsSwitch_WhenLockedOrClosed_DoesNotAnimate(bool locked)
        {
            if (locked) _vm.IsLocked = true;
            else
            {
                _host.Database.Close();
                _vm.RefreshDatabaseState();
            }

            foreach (bool open in new[] { true, false })
            {
                SeedFades();
                _vm.IsSettingsOpen = open;
                AssertNoFade(_hosts);
            }
        }

        [Test]
        public void DetailsCoveredBySettings_DoNotAnimate()
        {
            _vm.IsSettingsOpen = true;
            foreach (bool selected in new[] { false, true })
            {
                SeedFades();
                _vm.SelectedEntry = selected ? _vm.FilteredEntries[0] : null;
                AssertNoFade(_hosts[2], _hosts[3]);
            }
        }

        [Test]
        public void DisablingAnimations_ImmediatelyStopsAllTransitions()
        {
            SeedFades();
            _vm.Settings.AnimationsEnabled = false;
            AssertNoFade(_hosts);
        }

        [Test]
        public void SettingsSwitch_UsesCurrentAnimationPreference()
        {
            bool expected = SystemParameters.ClientAreaAnimation && (RenderCapability.Tier >> 16) >= 1;
            _vm.IsSettingsOpen = true;
            Assert.That(_hosts[1].HasAnimatedProperties, Is.EqualTo(expected));
            AssertNoFade(_hosts[0], _hosts[2], _hosts[3]);

            _vm.Settings.AnimationsEnabled = false;
            _vm.IsSettingsOpen = false;
            AssertNoFade(_hosts);
        }

        [Test]
        public void DetailChanges_AnimateOnlyCurrentPane()
        {
            bool expected = SystemParameters.ClientAreaAnimation && (RenderCapability.Tier >> 16) >= 1;
            _vm.SelectedEntry = null;
            Assert.That(_hosts[3].HasAnimatedProperties, Is.EqualTo(expected));
            AssertNoFade(_hosts[2]);

            _vm.SelectedEntry = _vm.FilteredEntries[0];
            Assert.That(_hosts[2].HasAnimatedProperties, Is.EqualTo(expected));
            AssertNoFade(_hosts[3]);
        }
    }
}
