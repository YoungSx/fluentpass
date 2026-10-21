using System;
using System.Linq;
using FluentPass.Common;
using FluentPass.Tests.Mocks;
using FluentPass.ViewModels;
using KeePassLib;
using KeePassLib.Security;
using NUnit.Framework;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace FluentPass.Tests
{
    [TestFixture, Apartment(System.Threading.ApartmentState.STA), Category("Integration")]
    public class ExternalSyncTests
    {
        private TestPluginHost _host = null!;
        private MainViewModel _mainVm = null!;

        [Test]
        public void HostRefresh_PreservesSelectedChildInBoundWpfTree()
        {
            var child = new PwGroup(true, true, "Selected Child", PwIcon.Folder);
            _host.Database.RootGroup.AddGroup(child, true);
            _mainVm.LoadGroups();
            var tree = new TreeView
            {
                ItemsSource = _mainVm.RootGroups,
                ItemTemplate = new HierarchicalDataTemplate(typeof(GroupItemViewModel))
                {
                    ItemsSource = new Binding(nameof(GroupItemViewModel.Children))
                }
            };
            tree.SelectedItemChanged += (_, e) => _mainVm.SelectedGroup = (GroupItemViewModel)e.NewValue;
            var window = new Window { Content = tree, Width = 300, Height = 300, ShowInTaskbar = false };
            try
            {
                window.Show();
                window.UpdateLayout();
                var rootItem = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromIndex(0);
                rootItem.IsExpanded = true;
                window.UpdateLayout();
                var childItem = (TreeViewItem)rootItem.ItemContainerGenerator.ContainerFromIndex(0);
                childItem.IsSelected = true;

                _mainVm.RefreshDatabaseState();
                window.UpdateLayout();

                Assert.That(tree.SelectedItem, Is.SameAs(_mainVm.RootGroups[0].Children[0]));
                Assert.That(_mainVm.SelectedGroup!.Model, Is.SameAs(child));
                Assert.That(childItem.IsSelected, Is.True);
            }
            finally
            {
                window.Close();
            }
        }

        [SetUp]
        public void Setup()
        {
            Common.MessageService.EnableDialogs = false;
            Loc.Instance.SetLanguagePreference("zh-CN");
            _host = new TestPluginHost();
            _mainVm = new MainViewModel(_host);
        }

        [Test]
        public void ReplacedEntryWithSameUuid_UsesLiveModelAndDetail()
        {
            var original = TestPluginHost.CreateTestEntry("Original", "user", "fixture");
            _host.Database.RootGroup.AddEntry(original, true);
            _mainVm.LoadGroups();
            var replacement = original.CloneDeep();
            replacement.Strings.Set(PwDefs.TitleField, new ProtectedString(false, "Replacement"));
            _host.Database.RootGroup.Entries.Remove(original);
            _host.Database.RootGroup.AddEntry(replacement, true);

            _mainVm.RefreshDatabaseState();

            Assert.That(_mainVm.Entries[0].Model, Is.SameAs(replacement));
            Assert.That(_mainVm.FilteredEntries[0].Model, Is.SameAs(replacement));
            Assert.That(_mainVm.SelectedEntry!.Model, Is.SameAs(replacement));
            Assert.That(_mainVm.CurrentDetail!.Model, Is.SameAs(replacement));
        }

        [Test]
        public void SwitchingGroups_ClearsDraftInDetachedDetail()
        {
            _host.Database.RootGroup.AddEntry(TestPluginHost.CreateTestEntry("Fixture", "user", "fixture"), true);
            _host.Database.RootGroup.AddGroup(new PwGroup(true, true, "Empty", PwIcon.Folder), true);
            _mainVm.LoadGroups();
            var detail = _mainVm.CurrentDetail!;
            detail.BeginEdit();
            detail.EditPassword = "unsaved fixture";

            _mainVm.SelectedGroup = _mainVm.RootGroups[0].Children[0];

            Assert.That(detail.EditPassword, Is.Empty);
            Assert.That(detail.IsEditing, Is.False);
        }

        [Test]
        public void ClearData_ClearsHeldEditingAndRevealedSecrets()
        {
            var entry = TestPluginHost.CreateTestEntry("Fixture", "user", "fixture");
            _host.Database.RootGroup.AddEntry(entry, true);
            _mainVm.LoadGroups();
            var detail = _mainVm.CurrentDetail!;
            detail.BeginEdit();
            detail.EditPassword = "unsaved fixture";
            detail.IsEditPasswordRevealed = true;
            detail.TogglePasswordVisibility();

            _host.Database.Close();
            _mainVm.ClearData();

            Assert.That(detail.EditPassword, Is.Empty);
            Assert.That(detail.IsEditing, Is.False);
            Assert.That(detail.IsEditPasswordRevealed, Is.False);
            Assert.That(detail.RevealedPassword, Is.Null);
            Assert.That(_mainVm.CurrentDetail, Is.Null);
        }

        [TearDown]
        public void TearDown()
        {
            Loc.Instance.SetLanguagePreference(SettingsService.DefaultLanguage);
        }

        [Test]
        public void ExternalChange_UrlUpdatedInNativeKeePass_ReflectsInEntryItemAndDetail()
        {
            var entry = TestPluginHost.CreateTestEntry("My Service", "user1", "pass1", "https://old.example.com", "my notes");
            _host.Database.RootGroup.AddEntry(entry, true);
            _mainVm.LoadGroups();

            _mainVm.SelectedEntry = _mainVm.FilteredEntries.First(e => e.Title == "My Service");
            Assert.That(_mainVm.CurrentDetail, Is.Not.Null);
            Assert.That(_mainVm.SelectedEntry.Url, Is.EqualTo("https://old.example.com"));
            Assert.That(_mainVm.CurrentDetail!.Url, Is.EqualTo("https://old.example.com"));

            // Simulate user editing entry URL in KeePass native window
            entry.Strings.Set(PwDefs.UrlField, new ProtectedString(false, "https://new.example.com"));

            // Native KeePass triggers UIStateUpdated -> RefreshDatabaseState()
            _mainVm.RefreshDatabaseState();

            // Verify both list item and detail view model are updated in real time
            Assert.That(_mainVm.SelectedEntry.Url, Is.EqualTo("https://new.example.com"));
            Assert.That(_mainVm.CurrentDetail.Url, Is.EqualTo("https://new.example.com"));
        }

        [Test]
        public void ExternalChange_TitleAndUsernameUpdated_ReflectsInEntryItemAndDetail()
        {
            var entry = TestPluginHost.CreateTestEntry("Original Title", "orig_user", "pass");
            _host.Database.RootGroup.AddEntry(entry, true);
            _mainVm.LoadGroups();

            _mainVm.SelectedEntry = _mainVm.FilteredEntries.First(e => e.Title == "Original Title");
            Assert.That(_mainVm.CurrentDetail, Is.Not.Null);

            // Simulate KeePass native edit
            entry.Strings.Set(PwDefs.TitleField, new ProtectedString(false, "Updated Native Title"));
            entry.Strings.Set(PwDefs.UserNameField, new ProtectedString(false, "updated_user"));

            _mainVm.RefreshDatabaseState();

            Assert.That(_mainVm.SelectedEntry.Title, Is.EqualTo("Updated Native Title"));
            Assert.That(_mainVm.SelectedEntry.UserName, Is.EqualTo("updated_user"));
            Assert.That(_mainVm.CurrentDetail!.Title, Is.EqualTo("Updated Native Title"));
            Assert.That(_mainVm.CurrentDetail.UserName, Is.EqualTo("updated_user"));
        }

        [Test]
        public void ExternalChange_EntryAddedInNativeKeePass_ReflectsInEntriesList()
        {
            _mainVm.LoadGroups();
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(0));

            // Add entry in native KeePass
            var entry = TestPluginHost.CreateTestEntry("Added In KeePass", "admin", "12345");
            _host.Database.RootGroup.AddEntry(entry, true);

            _mainVm.RefreshDatabaseState();

            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));
            Assert.That(_mainVm.FilteredEntries[0].Title, Is.EqualTo("Added In KeePass"));
        }

        [Test]
        public void ExternalChange_EntryDeletedInNativeKeePass_RemovesFromListAndClearsDetail()
        {
            var entry = TestPluginHost.CreateTestEntry("To Be Deleted", "user", "pwd");
            _host.Database.RootGroup.AddEntry(entry, true);
            _mainVm.LoadGroups();

            _mainVm.SelectedEntry = _mainVm.FilteredEntries.First(e => e.Title == "To Be Deleted");
            Assert.That(_mainVm.CurrentDetail, Is.Not.Null);

            // Delete entry in native KeePass
            _host.Database.RootGroup.Entries.Remove(entry);

            _mainVm.RefreshDatabaseState();

            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(0));
            Assert.That(_mainVm.SelectedEntry, Is.Null);
            Assert.That(_mainVm.CurrentDetail, Is.Null);
        }

        [Test]
        public void ExternalChange_SelectedGroupDeletedInNativeKeePass_FallsBackToRoot()
        {
            var childGroup = new PwGroup(true, true, "SubGroupToDelete", PwIcon.Folder);
            _host.Database.RootGroup.AddGroup(childGroup, true);
            _mainVm.LoadGroups();

            // Select child group
            var rootVm = _mainVm.RootGroups.First();
            var childVm = rootVm.Children.First(c => c.Name == "SubGroupToDelete");
            _mainVm.SelectedGroup = childVm;
            Assert.That(_mainVm.SelectedGroup.Name, Is.EqualTo("SubGroupToDelete"));

            // Delete child group from native KeePass
            _host.Database.RootGroup.Groups.Remove(childGroup);

            _mainVm.RefreshDatabaseState();

            // Selected group should safely fall back to root group
            Assert.That(_mainVm.SelectedGroup, Is.Not.Null);
            Assert.That(_mainVm.SelectedGroup.Model, Is.EqualTo(_host.Database.RootGroup));
        }

        [Test]
        public void ExternalChange_WhileEditingDetail_DoesNotOverwriteUnsavedDraft()
        {
            var entry = TestPluginHost.CreateTestEntry("Draft Title", "draft_user", "pwd", "https://site.com");
            _host.Database.RootGroup.AddEntry(entry, true);
            _mainVm.LoadGroups();

            _mainVm.SelectedEntry = _mainVm.FilteredEntries.First(e => e.Title == "Draft Title");
            Assert.That(_mainVm.CurrentDetail, Is.Not.Null);

            // Start editing in FluentPass UI
            _mainVm.CurrentDetail!.BeginEdit();
            _mainVm.CurrentDetail.EditTitle = "User Is Typing Something New";
            _mainVm.CurrentDetail.EditUrl = "https://custom-typing.com";
            Assert.That(_mainVm.CurrentDetail.IsEditing, Is.True);

            // Native KeePass changes background data
            entry.Strings.Set(PwDefs.NotesField, new ProtectedString(false, "New notes from native"));

            // Background event triggers RefreshDatabaseState()
            _mainVm.RefreshDatabaseState();

            // Should remain in editing mode without destroying the user's active inputs
            Assert.That(_mainVm.CurrentDetail.IsEditing, Is.True);
            Assert.That(_mainVm.CurrentDetail.EditTitle, Is.EqualTo("User Is Typing Something New"));
            Assert.That(_mainVm.CurrentDetail.EditUrl, Is.EqualTo("https://custom-typing.com"));
        }
    }
}
