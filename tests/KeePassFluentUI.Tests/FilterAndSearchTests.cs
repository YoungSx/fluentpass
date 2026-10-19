using System.Linq;
using KeePassFluentUI.Tests.Mocks;
using KeePassFluentUI.ViewModels;
using KeePassLib;
using NUnit.Framework;

namespace KeePassFluentUI.Tests
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
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(4));
        }

        [Test]
        public void Filter_ByTitleKeyword_ShouldReturnMatchingEntries()
        {
            _mainVm.SearchText = "git";
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));
            Assert.That(_mainVm.FilteredEntries[0].Title, Is.EqualTo("GitHub Account"));
        }

        [Test]
        public void Filter_ByUserName_ShouldReturnMatchingEntries()
        {
            _mainVm.SearchText = "john.doe";
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));
            Assert.That(_mainVm.FilteredEntries[0].Title, Is.EqualTo("Google Mail"));
        }

        [Test]
        public void Filter_ByUrl_ShouldReturnMatchingEntries()
        {
            _mainVm.SearchText = "azure.com";
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));
            Assert.That(_mainVm.FilteredEntries[0].Title, Is.EqualTo("Microsoft Azure"));
        }

        [Test]
        public void Filter_ByNotes_ShouldReturnMatchingEntries()
        {
            _mainVm.SearchText = "linux host";
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));
            Assert.That(_mainVm.FilteredEntries[0].Title, Is.EqualTo("Server SSH Key"));
        }

        [Test]
        public void ShowSubgroupEntries_ShouldIncludeNestedEntries_ExcludingRecycleBin()
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

            // Default: ShowSubgroupEntries = false, root group only has 4 direct entries
            Assert.That(_mainVm.Entries.Count, Is.EqualTo(4));

            // Enable ShowSubgroupEntries: should include subEntry (4 + 1 = 5), but exclude trashEntry
            _mainVm.ShowSubgroupEntries = true;
            Assert.That(_mainVm.Entries.Count, Is.EqualTo(5));
            Assert.That(_mainVm.Entries.Any(e => e.Title == "Nested Entry"), Is.True);
            Assert.That(_mainVm.Entries.Any(e => e.Title == "Trashed Entry"), Is.False);
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
