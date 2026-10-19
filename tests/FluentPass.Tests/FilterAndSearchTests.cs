using System.Linq;
using FluentPass.Tests.Mocks;
using FluentPass.ViewModels;
using KeePassLib;
using NUnit.Framework;

namespace FluentPass.Tests
{
    [TestFixture, Apartment(System.Threading.ApartmentState.STA)]
    public class FilterAndSearchTests
    {
        private TestPluginHost _host = null!;
        private MainViewModel _mainVm = null!;

        [SetUp]
        public void Setup()
        {
            Common.MessageService.EnableDialogs = false;
            _host = new TestPluginHost();

            var e1 = TestPluginHost.CreateTestEntry("GitHub Account", "octocat", "pass1", "https://github.com", "My code repository");
            var e2 = TestPluginHost.CreateTestEntry("Google Mail", "john.doe@gmail.com", "pass2", "https://mail.google.com", "Primary email");
            var e3 = TestPluginHost.CreateTestEntry("Microsoft Azure", "admin@company.onmicrosoft.com", "pass3", "https://portal.azure.com", "Cloud infrastructure");
            var e4 = TestPluginHost.CreateTestEntry("Server SSH Key", "root", "pass4", "", "Production linux host 192.168.1.100");

            _host.Database.RootGroup.AddEntry(e1, true);
            _host.Database.RootGroup.AddEntry(e2, true);
            _host.Database.RootGroup.AddEntry(e3, true);
            _host.Database.RootGroup.AddEntry(e4, true);

            _mainVm = new MainViewModel(_host);
            _mainVm.LoadGroups();
        }

        [Test]
        public void Filter_WithEmptySearch_ShouldReturnAllEntries()
        {
            _mainVm.SearchText = string.Empty;
            _mainVm.ApplyFilter();
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(4));
        }

        [Test]
        public void Filter_ByTitleKeyword_ShouldReturnMatchingEntries()
        {
            _mainVm.SearchText = "git";
            _mainVm.ApplyFilter();
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));
            Assert.That(_mainVm.FilteredEntries[0].Title, Is.EqualTo("GitHub Account"));
        }

        [Test]
        public void Filter_ByUserName_ShouldReturnMatchingEntries()
        {
            _mainVm.SearchText = "john.doe";
            _mainVm.ApplyFilter();
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));
            Assert.That(_mainVm.FilteredEntries[0].Title, Is.EqualTo("Google Mail"));
        }

        [Test]
        public void Filter_ByUrl_ShouldReturnMatchingEntries()
        {
            _mainVm.SearchText = "azure.com";
            _mainVm.ApplyFilter();
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));
            Assert.That(_mainVm.FilteredEntries[0].Title, Is.EqualTo("Microsoft Azure"));
        }

        [Test]
        public void Filter_ByNotes_ShouldReturnMatchingEntries()
        {
            _mainVm.SearchText = "linux host";
            _mainVm.ApplyFilter();
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));
            Assert.That(_mainVm.FilteredEntries[0].Title, Is.EqualTo("Server SSH Key"));
        }

        [Test]
        public void ShowSubgroupEntries_DefaultsToTrue_ShouldIncludeNestedEntries_ExcludingRecycleBin()
        {
            var subGroup = new PwGroup(true, true, "Work Subgroup", PwIcon.Folder);
            var subEntry = TestPluginHost.CreateTestEntry("Nested Entry", "worker", "pass");
            subGroup.AddEntry(subEntry, true);
            _host.Database.RootGroup.AddGroup(subGroup, true);

            // Configure Recycle Bin
            var recycleBin = new PwGroup(true, true, "Recycle Bin", PwIcon.TrashBin);
            var trashEntry = TestPluginHost.CreateTestEntry("Trashed Entry", "trash", "pass");
            recycleBin.AddEntry(trashEntry, true);
            _host.Database.RootGroup.AddGroup(recycleBin, true);
            _host.Database.RecycleBinUuid = recycleBin.Uuid;
            _host.Database.RecycleBinEnabled = true;

            _mainVm.LoadGroups();

            // Default: ShowSubgroupEntries is true, so nested entry is included (4 + 1 = 5), excluding trashEntry
            Assert.That(_mainVm.ShowSubgroupEntries, Is.True);
            Assert.That(_mainVm.Entries.Count, Is.EqualTo(5));
            Assert.That(_mainVm.Entries.Any(e => e.Title == "Nested Entry"), Is.True);
            Assert.That(_mainVm.Entries.Any(e => e.Title == "Trashed Entry"), Is.False);

            // When disabled, only direct entries are returned (4)
            _mainVm.ShowSubgroupEntries = false;
            Assert.That(_mainVm.Entries.Count, Is.EqualTo(4));
            Assert.That(_mainVm.Entries.Any(e => e.Title == "Nested Entry"), Is.False);
        }

        [Test]
        public void Search_DefaultsToDatabaseWide_WhenChildGroupSelected()
        {
            var childGroup = new PwGroup(true, true, "Child Group", PwIcon.Folder);
            var childEntry = TestPluginHost.CreateTestEntry("Child Special Token", "user", "pass");
            childGroup.AddEntry(childEntry, true);
            _host.Database.RootGroup.AddGroup(childGroup, true);

            _mainVm.LoadGroups();
            var childVm = _mainVm.RootGroups[0].Children.First(g => g.Model.Uuid.Equals(childGroup.Uuid));
            _mainVm.SelectedGroup = childVm;

            // Direct + subgroup entries in Child Group is 1
            Assert.That(_mainVm.Entries.Count, Is.EqualTo(1));

            // Searching for an entry outside Child Group (e.g., "GitHub") should still match via database-wide search
            _mainVm.SearchText = "GitHub";
            _mainVm.ApplyFilter();
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));
            Assert.That(_mainVm.FilteredEntries[0].Title, Is.EqualTo("GitHub Account"));
        }

        [Test]
        public void InlineEdit_PasswordMask_ToggleShouldWorkSafely()
        {
            var entry = _host.Database.RootGroup.Entries.GetAt(0);
            var detailVm = new EntryDetailViewModel(entry, _host, null, () => { });

            detailVm.BeginEdit();
            Assert.That(detailVm.IsEditing, Is.True);
            Assert.That(detailVm.IsEditPasswordRevealed, Is.False, "Initial edit mode must default to masked");

            detailVm.ToggleEditPasswordVisibility();
            Assert.That(detailVm.IsEditPasswordRevealed, Is.True);

            detailVm.CancelEdit();
            Assert.That(detailVm.IsEditing, Is.False);
            Assert.That(detailVm.EditPassword, Is.Empty, "Password must be wiped from memory upon cancel");
            Assert.That(detailVm.IsEditPasswordRevealed, Is.False);
        }
    }
}
