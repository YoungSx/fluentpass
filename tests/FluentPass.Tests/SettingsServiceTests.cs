using System;
using FluentPass.Common;
using FluentPass.Tests.Mocks;
using FluentPass.ViewModels;
using NUnit.Framework;

namespace FluentPass.Tests
{
    [TestFixture, Apartment(System.Threading.ApartmentState.STA), Category("Unit")]
    public class SettingsServiceTests
    {
        private TestPluginHost _host = null!;

        [SetUp]
        public void Setup()
        {
            _host = new TestPluginHost();
        }

        [Test]
        public void SettingsService_DefaultValues_WhenCustomConfigIsEmpty()
        {
            Assert.That(SettingsService.GetAutoTakeoverUI(_host), Is.True);
            Assert.That(SettingsService.GetCloseAction(_host), Is.EqualTo(WindowCloseAction.RestoreOriginalWindow));
            Assert.That(SettingsService.GetAppTheme(_host), Is.EqualTo("System"));
            Assert.That(SettingsService.GetAutoMaskSeconds(_host), Is.EqualTo(10));
        }

        [Test]
        public void SettingsService_DefaultValues_WhenHostIsNull()
        {
            Assert.That(SettingsService.GetAutoTakeoverUI(null), Is.True);
            Assert.That(SettingsService.GetCloseAction(null), Is.EqualTo(WindowCloseAction.RestoreOriginalWindow));
            Assert.That(SettingsService.GetAppTheme(null), Is.EqualTo("System"));
            Assert.That(SettingsService.GetAutoMaskSeconds(null), Is.EqualTo(10));
        }

        [Test]
        public void SettingsService_ReadAndWrite_AutoTakeoverUI()
        {
            SettingsService.SetAutoTakeoverUI(_host, false);
            Assert.That(SettingsService.GetAutoTakeoverUI(_host), Is.False);

            SettingsService.SetAutoTakeoverUI(_host, true);
            Assert.That(SettingsService.GetAutoTakeoverUI(_host), Is.True);
        }

        [Test]
        public void SettingsService_ReadAndWrite_CloseAction()
        {
            SettingsService.SetCloseAction(_host, WindowCloseAction.MinimizeToTray);
            Assert.That(SettingsService.GetCloseAction(_host), Is.EqualTo(WindowCloseAction.MinimizeToTray));

            SettingsService.SetCloseAction(_host, WindowCloseAction.ExitKeePass);
            Assert.That(SettingsService.GetCloseAction(_host), Is.EqualTo(WindowCloseAction.ExitKeePass));

            SettingsService.SetCloseAction(_host, WindowCloseAction.RestoreOriginalWindow);
            Assert.That(SettingsService.GetCloseAction(_host), Is.EqualTo(WindowCloseAction.RestoreOriginalWindow));
        }

        [Test]
        public void SettingsService_ReadAndWrite_AppTheme()
        {
            SettingsService.SetAppTheme(_host, "Dark");
            Assert.That(SettingsService.GetAppTheme(_host), Is.EqualTo("Dark"));

            SettingsService.SetAppTheme(_host, "Light");
            Assert.That(SettingsService.GetAppTheme(_host), Is.EqualTo("Light"));

            SettingsService.SetAppTheme(_host, "InvalidThemeName");
            Assert.That(SettingsService.GetAppTheme(_host), Is.EqualTo("System"));
        }

        [Test]
        public void SettingsService_ReadAndWrite_AutoCheckUpdates()
        {
            Assert.That(SettingsService.GetAutoCheckUpdates(_host), Is.True);
            Assert.That(SettingsService.GetAutoCheckUpdates(null), Is.True);

            SettingsService.SetAutoCheckUpdates(_host, false);
            Assert.That(SettingsService.GetAutoCheckUpdates(_host), Is.False);

            SettingsService.SetAutoCheckUpdates(_host, true);
            Assert.That(SettingsService.GetAutoCheckUpdates(_host), Is.True);
        }

        [Test]
        public void SettingsService_ReadAndWrite_SkippedUpdateVersion()
        {
            Assert.That(SettingsService.GetSkippedUpdateVersion(_host), Is.Empty);
            Assert.That(SettingsService.GetSkippedUpdateVersion(null), Is.Empty);

            SettingsService.SetSkippedUpdateVersion(_host, "1.6.0.0");
            Assert.That(SettingsService.GetSkippedUpdateVersion(_host), Is.EqualTo("1.6.0.0"));

            SettingsService.SetSkippedUpdateVersion(_host, null);
            Assert.That(SettingsService.GetSkippedUpdateVersion(_host), Is.Empty);
        }

        [Test]
        public void SettingsService_CorruptedAutoCheckUpdates_FallsBackToEnabled()
        {
            _host.CustomConfig.SetString(SettingsService.AutoCheckUpdatesKey, "not_a_bool");
            Assert.That(SettingsService.GetAutoCheckUpdates(_host), Is.True);
        }

        [Test]
        public void SettingsService_ReadAndWrite_AutoMaskSeconds()
        {
            SettingsService.SetAutoMaskSeconds(_host, 15);
            Assert.That(SettingsService.GetAutoMaskSeconds(_host), Is.EqualTo(15));

            SettingsService.SetAutoMaskSeconds(_host, 30);
            Assert.That(SettingsService.GetAutoMaskSeconds(_host), Is.EqualTo(30));

            // Test boundary fallback
            SettingsService.SetAutoMaskSeconds(_host, -1);
            Assert.That(SettingsService.GetAutoMaskSeconds(_host), Is.EqualTo(10));

            SettingsService.SetAutoMaskSeconds(_host, 9999);
            Assert.That(SettingsService.GetAutoMaskSeconds(_host), Is.EqualTo(10));
        }

        [Test]
        public void SettingsViewModel_TwoWayBinding_UpdatesService()
        {
            string? changedTheme = null;
            bool closed = false;

            var vm = new SettingsViewModel(_host, () => closed = true, theme => changedTheme = theme);

            // 1. Toggle AutoTakeoverUI
            vm.AutoTakeoverUI = false;
            Assert.That(SettingsService.GetAutoTakeoverUI(_host), Is.False);

            // 2. Change CloseActionIndex
            vm.CloseActionIndex = (int)WindowCloseAction.ExitKeePass;
            Assert.That(SettingsService.GetCloseAction(_host), Is.EqualTo(WindowCloseAction.ExitKeePass));

            // 3. Change Theme
            vm.SelectedTheme = "Dark";
            Assert.That(SettingsService.GetAppTheme(_host), Is.EqualTo("Dark"));
            Assert.That(changedTheme, Is.EqualTo("Dark"));

            // 4. Change AutoMaskSecondsValue (NumberBox 绑定口径)
            vm.AutoMaskSecondsValue = 45;
            Assert.That(vm.AutoMaskSeconds, Is.EqualTo(45));
            Assert.That(SettingsService.GetAutoMaskSeconds(_host), Is.EqualTo(45));

            // 5. Toggle AutoCheckUpdates（在线更新分组）
            vm.AutoCheckUpdates = false;
            Assert.That(SettingsService.GetAutoCheckUpdates(_host), Is.False);

            // 6. Close Command
            vm.CloseSettingsCommand.Execute(null);
            Assert.That(closed, Is.True);
        }

        [Test]
        public void SettingsService_CorruptedConfigValues_FallbackToDefaults()
        {
            _host.CustomConfig.SetString(SettingsService.CloseActionKey, "invalid_enum_val");
            Assert.That(SettingsService.GetCloseAction(_host), Is.EqualTo(WindowCloseAction.RestoreOriginalWindow));

            _host.CustomConfig.SetString(SettingsService.CloseActionKey, "999");
            Assert.That(SettingsService.GetCloseAction(_host), Is.EqualTo(WindowCloseAction.RestoreOriginalWindow));

            _host.CustomConfig.SetString(SettingsService.AutoTakeoverKey, "not_a_bool");
            Assert.That(SettingsService.GetAutoTakeoverUI(_host), Is.True);

            _host.CustomConfig.SetString(SettingsService.AutoMaskSecondsKey, "not_a_number");
            Assert.That(SettingsService.GetAutoMaskSeconds(_host), Is.EqualTo(10));

            _host.CustomConfig.SetString(SettingsService.AppThemeKey, "HackerNeonGreen");
            Assert.That(SettingsService.GetAppTheme(_host), Is.EqualTo("System"));
        }

        [Test]
        public void SettingsViewModel_IsSettingsOpen_DefaultsFalse_And_CloseCommandSetsFalse()
        {
            bool closed = false;
            var vm = new SettingsViewModel(_host, () => closed = true);

            Assert.That(vm.IsSettingsOpen, Is.False);

            vm.IsSettingsOpen = true;
            Assert.That(vm.IsSettingsOpen, Is.True);

            vm.CloseSettingsCommand.Execute(null);
            Assert.That(vm.IsSettingsOpen, Is.False);
            Assert.That(closed, Is.True);
        }

        [Test]
        public void SettingsViewModel_ResetSettings_RestoresDefaults()
        {
            var vm = new SettingsViewModel(_host);
            vm.AutoTakeoverUI = false;
            vm.CloseActionIndex = (int)WindowCloseAction.ExitKeePass;
            vm.SelectedTheme = "Dark";
            vm.AutoMaskSecondsValue = 5;
            vm.AutoCheckUpdates = false;

            vm.ResetSettingsCommand.Execute(null);

            Assert.That(vm.AutoTakeoverUI, Is.True);
            Assert.That(vm.CloseActionIndex, Is.EqualTo((int)WindowCloseAction.RestoreOriginalWindow));
            Assert.That(vm.SelectedTheme, Is.EqualTo("System"));
            Assert.That(vm.AutoMaskSeconds, Is.EqualTo(10));
            Assert.That(vm.AutoCheckUpdates, Is.True);
        }
    }
}
