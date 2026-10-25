using System;
using FluentPass.Common;
using FluentPass.Tests.Mocks;
using FluentPass.ViewModels;
using KeePassLib;
using NUnit.Framework;

namespace FluentPass.Tests
{
    /// <summary>
    /// 过期警示（对齐宿主 ExpirySoonDays 口径）、详情元数据展示与面包屑导航。
    /// </summary>
    [TestFixture, Apartment(System.Threading.ApartmentState.STA), Category("Integration")]
    public class ExpiryBreadcrumbMetadataTests
    {
        private TestPluginHost _host = null!;

        [SetUp]
        public void Setup()
        {
            Common.MessageService.EnableDialogs = false;
            _host = new TestPluginHost();
            Loc.Instance.SetLanguagePreference("en-US"); // 断言文本用确定语言
        }

        private EntryDetailViewModel CreateDetail(PwEntry entry)
        {
            _host.Database.RootGroup.AddEntry(entry, true);
            return new EntryDetailViewModel(entry, _host, null, () => { });
        }

        // ===== 过期警示 =====

        [Test]
        public void Expiry_NonExpiringEntry_HidesWarning()
        {
            var entry = TestPluginHost.CreateTestEntry("Eternal", "u", "p", "", "");
            entry.Expires = false;
            var vm = CreateDetail(entry);

            Assert.That(vm.IsExpiryWarningVisible, Is.False);
            Assert.That(vm.ExpiryWarningText, Is.Empty);
            Assert.That(vm.ExpiryDisplay, Is.EqualTo("Never expires"));
        }

        [Test]
        public void Expiry_AlreadyExpiredEntry_ShowsExpiredWarning()
        {
            var entry = TestPluginHost.CreateTestEntry("Stale", "u", "p", "", "");
            entry.Expires = true;
            entry.ExpiryTime = DateTime.UtcNow.AddDays(-3);
            var vm = CreateDetail(entry);

            Assert.That(vm.IsExpiryWarningVisible, Is.True);
            Assert.That(vm.ExpiryWarningText, Does.Contain("expired"));
        }

        [Test]
        public void Expiry_SoonExpiringEntry_ShowsSoonWarning()
        {
            var entry = TestPluginHost.CreateTestEntry("Doomed", "u", "p", "", "");
            entry.Expires = true;
            entry.ExpiryTime = DateTime.UtcNow.AddDays(3); // 默认 7 天窗口内
            var vm = CreateDetail(entry);

            Assert.That(vm.IsExpiryWarningVisible, Is.True);
            Assert.That(vm.ExpiryWarningText, Does.Contain("expires"));
            Assert.That(vm.ExpiryWarningText, Does.Not.Contain("expired"));
        }

        [Test]
        public void Expiry_FarFutureEntry_HidesWarning()
        {
            var entry = TestPluginHost.CreateTestEntry("Safe", "u", "p", "", "");
            entry.Expires = true;
            entry.ExpiryTime = DateTime.UtcNow.AddYears(5);
            var vm = CreateDetail(entry);

            Assert.That(vm.IsExpiryWarningVisible, Is.False);
            Assert.That(vm.ExpiryDisplay, Does.Contain(entry.ExpiryTime.ToLocalTime().ToString("yyyy/MM/dd")));
        }

        [Test]
        public void Expiry_HonorsHostExpirySoonDaysConfiguration()
        {
            var config = KeePass.Program.Config.Application;
            int saved = config.ExpirySoonDays;
            try
            {
                config.ExpirySoonDays = 1; // 明天以后都不算“即将过期”
                var entry = TestPluginHost.CreateTestEntry("Boundary", "u", "p", "", "");
                entry.Expires = true;
                entry.ExpiryTime = DateTime.UtcNow.AddDays(3);
                var vm = CreateDetail(entry);
                Assert.That(vm.IsExpiryWarningVisible, Is.False, "3 天后过期不应触发 1 天窗口的即将过期警示");

                config.ExpirySoonDays = 30;
                var vm2 = CreateDetail(entry);
                Assert.That(vm2.IsExpiryWarningVisible, Is.True, "窗口放宽到 30 天后应触发警示");
            }
            finally
            {
                config.ExpirySoonDays = saved;
            }
        }

        // ===== 元数据 =====

        [Test]
        public void Metadata_ShowsUsageCountAndTimes()
        {
            var entry = TestPluginHost.CreateTestEntry("Meta", "u", "p", "", "");
            entry.UsageCount = 42;
            var vm = CreateDetail(entry);

            Assert.That(vm.UsageCountDisplay, Does.Contain("42"));
            Assert.That(vm.LastAccessDisplay, Is.Not.Empty);
            Assert.That(vm.MetadataHeader, Is.Not.Empty);
            Assert.That(vm.UsageCountDisplay, Is.Not.EqualTo("42"), "展示文本应含格式化文案而非裸数字");
        }

        // ===== 面包屑 =====

        [Test]
        public void Breadcrumb_RootOnly_HidesBar()
        {
            var mainVm = new MainViewModel(_host);
            mainVm.LoadGroups();

            Assert.That(mainVm.BreadcrumbItems.Count, Is.EqualTo(1));
            Assert.That(mainVm.HasBreadcrumb, Is.False);
        }

        [Test]
        public void Breadcrumb_NestedGroup_BuildsAncestorChain()
        {
            var root = _host.Database.RootGroup;
            var level1 = new PwGroup(true, true, "L1", PwIcon.Folder);
            var level2 = new PwGroup(true, true, "L2", PwIcon.Folder);
            root.AddGroup(level1, true);
            level1.AddGroup(level2, true);

            var mainVm = new MainViewModel(_host);
            mainVm.LoadGroups();
            var l2Vm = mainVm.RootGroups[0].Children[0].Children[0];
            mainVm.SelectedGroup = l2Vm;

            Assert.That(mainVm.BreadcrumbItems.Count, Is.EqualTo(3));
            Assert.That(mainVm.BreadcrumbItems[0].Name, Is.EqualTo(root.Name));
            Assert.That(mainVm.BreadcrumbItems[1].Name, Is.EqualTo("L1"));
            Assert.That(mainVm.BreadcrumbItems[2].Name, Is.EqualTo("L2"));
            Assert.That(mainVm.BreadcrumbItems[2].IsCurrent, Is.True);
            Assert.That(mainVm.BreadcrumbItems[0].IsCurrent, Is.False);
            Assert.That(mainVm.HasBreadcrumb, Is.True);
        }

        [Test]
        public void Breadcrumb_NavigateToAncestor_ChangesSelection()
        {
            var root = _host.Database.RootGroup;
            var level1 = new PwGroup(true, true, "Target", PwIcon.Folder);
            root.AddGroup(level1, true);

            var mainVm = new MainViewModel(_host);
            mainVm.LoadGroups();
            var l1Vm = mainVm.RootGroups[0].Children[0];
            mainVm.SelectedGroup = l1Vm;

            var rootCrumb = mainVm.BreadcrumbItems[0];
            Assert.That(rootCrumb.IsCurrent, Is.False);
            mainVm.NavigateToBreadcrumbCommand.Execute(rootCrumb);

            Assert.That(mainVm.SelectedGroup, Is.SameAs(mainVm.RootGroups[0]));
            Assert.That(mainVm.HasBreadcrumb, Is.False, "回到根后面包屑隐藏");
        }

        [Test]
        public void Breadcrumb_ClickingCurrentNode_NoOp()
        {
            var mainVm = new MainViewModel(_host);
            mainVm.LoadGroups();
            var current = mainVm.BreadcrumbItems[0];

            Assert.That(() => mainVm.NavigateToBreadcrumb(current), Throws.Nothing);
            Assert.That(mainVm.SelectedGroup, Is.SameAs(mainVm.RootGroups[0]));
        }

        [Test]
        public void Breadcrumb_ClearedOnDatabaseClose()
        {
            var mainVm = new MainViewModel(_host);
            mainVm.LoadGroups();
            mainVm.ClearData();

            Assert.That(mainVm.BreadcrumbItems, Is.Empty);
            Assert.That(mainVm.HasBreadcrumb, Is.False);
        }
    }
}
