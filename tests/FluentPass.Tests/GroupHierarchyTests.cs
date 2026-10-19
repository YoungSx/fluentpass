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

        [Test, Category("Integration")]
        public void GroupItemViewModel_ExpandAllAndCollapseAll_ShouldUpdateExpandedStateCorrectly()
        {
            var host = new TestPluginHost();

            var subGroup1 = new PwGroup(true, true, "Work Accounts", PwIcon.Folder);
            var subSubGroup = new PwGroup(true, true, "Servers", PwIcon.Folder);
            subGroup1.AddGroup(subSubGroup, true);
            host.Database.RootGroup.AddGroup(subGroup1, true);

            var rootVm = new GroupItemViewModel(host.Database.RootGroup, null, null, host.Database);
            var workVm = rootVm.Children[0];
            var serverVm = workVm.Children[0];

            // Initially, root is expanded, children are collapsed
            Assert.That(rootVm.IsExpanded, Is.True);
            Assert.That(workVm.IsExpanded, Is.False);
            Assert.That(serverVm.IsExpanded, Is.False);

            // Expand all
            rootVm.ExpandAll();
            Assert.That(rootVm.IsExpanded, Is.True);
            Assert.That(workVm.IsExpanded, Is.True);
            Assert.That(serverVm.IsExpanded, Is.True);

            // Collapse all: root remains expanded because parent is null
            rootVm.CollapseAll();
            Assert.That(rootVm.IsExpanded, Is.True);
            Assert.That(workVm.IsExpanded, Is.False);
            Assert.That(serverVm.IsExpanded, Is.False);
        }

        [Test, Category("Integration")]
        public void GroupItemViewModel_ReloadChildren_ShouldPreserveViewModelsAndReflectNewOrder()
        {
            var host = new TestPluginHost();

            var groupB = new PwGroup(true, true, "Beta", PwIcon.Folder);
            var groupA = new PwGroup(true, true, "Alpha", PwIcon.Folder);
            host.Database.RootGroup.AddGroup(groupB, true);
            host.Database.RootGroup.AddGroup(groupA, true);

            var rootVm = new GroupItemViewModel(host.Database.RootGroup, null, null, host.Database);
            Assert.That(rootVm.Children[0].Name, Is.EqualTo("Beta"));
            Assert.That(rootVm.Children[1].Name, Is.EqualTo("Alpha"));

            var origBetaVm = rootVm.Children[0];
            var origAlphaVm = rootVm.Children[1];

            // Reorder in KeePass model
            host.Database.RootGroup.SortSubGroups(false);

            // Reload children
            rootVm.ReloadChildren();

            Assert.That(rootVm.Children[0].Name, Is.EqualTo("Alpha"));
            Assert.That(rootVm.Children[1].Name, Is.EqualTo("Beta"));
            Assert.That(rootVm.Children[0], Is.SameAs(origAlphaVm));
            Assert.That(rootVm.Children[1], Is.SameAs(origBetaVm));
        }

        [Test, Category("Integration")]
        public void MainViewModel_GroupActionStates_ShouldReflectHierarchyAndRecycleBin()
        {
            var host = new TestPluginHost();
            var mainVm = new MainViewModel(host);
            mainVm.LoadGroups();

            // Root group selected initially
            Assert.That(mainVm.SelectedGroup, Is.Not.Null);
            Assert.That(mainVm.CanDuplicateSelectedGroup, Is.False, "Cannot duplicate root group");
            Assert.That(mainVm.CanDeleteSelectedGroup, Is.False, "Cannot delete root group");
            Assert.That(mainVm.CanMoveSelectedGroupUp, Is.False, "Cannot move root group");
            Assert.That(mainVm.CanMoveSelectedGroupDown, Is.False, "Cannot move root group");

            // Add sub-groups
            mainVm.NewGroup();
            mainVm.NewGroup();
            mainVm.RefreshGroupActionStates();

            var rootVm = mainVm.RootGroups[0];
            Assert.That(rootVm.Children.Count, Is.EqualTo(2));
            Assert.That(mainVm.CanSortSelectedSubgroups, Is.True, "Root has 2 children, can sort");

            // Select first child
            mainVm.SelectedGroup = rootVm.Children[0];
            Assert.That(mainVm.CanDuplicateSelectedGroup, Is.True);
            Assert.That(mainVm.CanDeleteSelectedGroup, Is.True);
            Assert.That(mainVm.CanMoveSelectedGroupUp, Is.False, "First child cannot move up");
            Assert.That(mainVm.CanMoveSelectedGroupDown, Is.True, "First child can move down");

            // Select second child
            mainVm.SelectedGroup = rootVm.Children[1];
            Assert.That(mainVm.CanMoveSelectedGroupUp, Is.True, "Second child can move up");
            Assert.That(mainVm.CanMoveSelectedGroupDown, Is.False, "Second child cannot move down");

            // Move second child up
            mainVm.MoveGroupUp();
            Assert.That(rootVm.Children[0], Is.SameAs(mainVm.SelectedGroup));
        }
    }
}
