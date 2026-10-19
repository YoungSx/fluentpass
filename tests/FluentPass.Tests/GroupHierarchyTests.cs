using FluentPass.Tests.Mocks;
using FluentPass.ViewModels;
using KeePassLib;
using NUnit.Framework;

namespace FluentPass.Tests
{
    [TestFixture, Apartment(System.Threading.ApartmentState.STA), Category("Integration")]
    public class GroupHierarchyTests
    {
        [SetUp]
        public void Setup()
        {
            Common.MessageService.EnableDialogs = false;
        }

        [Test, Category("Integration")]
        public void GroupItemViewModel_ShouldRecursivelyLoadChildren_AndCountEntries()
        {
            var host = new TestPluginHost();

            var subGroup1 = new PwGroup(true, true, "Work Accounts", PwIcon.Folder);
            var subGroup2 = new PwGroup(true, true, "Personal Accounts", PwIcon.Folder);

            var entry1 = TestPluginHost.CreateTestEntry("Work 1", "u1", "p1");
            var entry2 = TestPluginHost.CreateTestEntry("Work 2", "u2", "p2");
            subGroup1.AddEntry(entry1, true);
            subGroup1.AddEntry(entry2, true);

            var subSubGroup = new PwGroup(true, true, "Servers", PwIcon.Folder);
            subGroup1.AddGroup(subSubGroup, true);

            host.Database.RootGroup.AddGroup(subGroup1, true);
            host.Database.RootGroup.AddGroup(subGroup2, true);

            var rootVm = new GroupItemViewModel(host.Database.RootGroup, null, null, host.Database);

            Assert.That(rootVm.Children.Count, Is.EqualTo(2));
            var workVm = rootVm.Children[0];
            Assert.That(workVm.Name, Is.EqualTo("Work Accounts"));
            Assert.That(workVm.EntryCount, Is.EqualTo(2));
            Assert.That(workVm.Children.Count, Is.EqualTo(1));
            Assert.That(workVm.Children[0].Name, Is.EqualTo("Servers"));
        }

        [Test, Category("Integration")]
        public void GroupItemViewModel_RefreshSelfAndAncestors_ShouldUpdateCountsUpTheTree()
        {
            var host = new TestPluginHost();

            var subGroup1 = new PwGroup(true, true, "Work Accounts", PwIcon.Folder);
            var subSubGroup = new PwGroup(true, true, "Servers", PwIcon.Folder);
            subGroup1.AddGroup(subSubGroup, true);
            host.Database.RootGroup.AddGroup(subGroup1, true);

            var rootVm = new GroupItemViewModel(host.Database.RootGroup, null, null, host.Database);
            var workVm = rootVm.Children[0];
            var serverVm = workVm.Children[0];

            Assert.That(serverVm.EntryCount, Is.EqualTo(0));

            // Add entry into subSubGroup
            var entry = TestPluginHost.CreateTestEntry("Server 1", "root", "secret");
            subSubGroup.AddEntry(entry, true);

            serverVm.RefreshSelfAndAncestors();

            Assert.That(serverVm.EntryCount, Is.EqualTo(1));
        }
    }
}
