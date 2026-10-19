using System;
using System.Linq;
using KeePassFluentUI.Tests.Mocks;
using KeePassFluentUI.ViewModels;
using KeePassLib;
using KeePassLib.Cryptography;
using KeePassLib.Security;
using NUnit.Framework;

namespace KeePassFluentUI.Tests
{
    [TestFixture, Apartment(System.Threading.ApartmentState.STA)]
    public class Phase3Tests
    {
        private TestPluginHost _host = null!;
        private MainViewModel _mainVm = null!;

        [SetUp]
        public void Setup()
        {
            Common.MessageService.EnableDialogs = false;
            _host = new TestPluginHost();
            _mainVm = new MainViewModel(_host);
        }

        [Test]
        public void PasswordQuality_EstimationAndClassification_ShouldBeAccurate()
        {
            var entry = TestPluginHost.CreateTestEntry("Test", "user", "123456");
            var detailVm = new EntryDetailViewModel(entry, _host, null, () => { });

            // "123456" is a very weak password (< 40 bits)
            Assert.That(detailVm.PasswordQualityBits, Is.LessThan(40));
            Assert.That(detailVm.PasswordQualityColor, Is.EqualTo("#D32F2F"));

            // Edit password to a very strong password
            detailVm.BeginEdit();
            detailVm.EditPassword = "Correct-Horse-Battery-Staple-2026!#$";
            Assert.That(detailVm.PasswordQualityBits, Is.GreaterThanOrEqualTo(80));
            Assert.That(detailVm.PasswordQualityColor, Is.EqualTo("#388E3C"));
            Assert.That(detailVm.PasswordQualityPercent, Is.GreaterThan(60));
        }

        [Test]
        public void EntryHistory_Restore_ShouldPreserveUuidAndBackupCurrentState()
        {
            var entry = TestPluginHost.CreateTestEntry("Original Title", "user1", "pass1");
            _host.Database.RootGroup.AddEntry(entry, true);
            var detailVm = new EntryDetailViewModel(entry, _host, null, () => { });

            // Create revision 1
            detailVm.BeginEdit();
            detailVm.EditTitle = "Modified Title V2";
            detailVm.SaveEdit();

            Assert.That(entry.History.UCount, Is.EqualTo(1));
            Assert.That(detailVm.HistoryRevisions.Count, Is.EqualTo(1));

            var originalSnapshot = entry.History.GetAt(0);
            Assert.That(originalSnapshot.Strings.ReadSafe(PwDefs.TitleField), Is.EqualTo("Original Title"));

            // Restore the historical revision
            var origUuid = entry.Uuid;
            detailVm.RestoreRevision(originalSnapshot);

            // Verified invariants:
            // 1. Title restored
            Assert.That(entry.Strings.ReadSafe(PwDefs.TitleField), Is.EqualTo("Original Title"));
            // 2. UUID untouched
            Assert.That(entry.Uuid, Is.EqualTo(origUuid));
            // 3. Current state backed up before restore (history now has 2 items)
            Assert.That(entry.History.UCount, Is.EqualTo(2));
            Assert.That(entry.History.GetAt(1).Strings.ReadSafe(PwDefs.TitleField), Is.EqualTo("Modified Title V2"));
        }

        [Test]
        public void RecycleBin_Emptying_ShouldRecordDeletedObjectsAndRetainContainer()
        {
            var recycleBin = new PwGroup(true, true, "Recycle Bin", PwIcon.TrashBin);
            _host.Database.RootGroup.AddGroup(recycleBin, true);
            _host.Database.RecycleBinUuid = recycleBin.Uuid;
            _host.Database.RecycleBinEnabled = true;

            var trash1 = TestPluginHost.CreateTestEntry("Trash Entry 1", "u1", "p1");
            var trash2 = TestPluginHost.CreateTestEntry("Trash Entry 2", "u2", "p2");
            var trashSubGroup = new PwGroup(true, true, "Trash Subgroup", PwIcon.Folder);

            recycleBin.AddEntry(trash1, true);
            recycleBin.AddEntry(trash2, true);
            recycleBin.AddGroup(trashSubGroup, true);

            _mainVm.LoadGroups();
            var binVm = _mainVm.RootGroups[0].Children.First(g => g.Model.Uuid.Equals(recycleBin.Uuid));
            _mainVm.SelectedGroup = binVm;

            Assert.That(_mainVm.IsRecycleBinSelected, Is.True);

            // Execute Empty Recycle Bin
            _mainVm.EmptyRecycleBin();

            // Verified invariants:
            // 1. Container still exists and is attached
            Assert.That(_host.Database.RootGroup.FindGroup(recycleBin.Uuid, true), Is.Not.Null);
            // 2. Entries and Groups emptied
            Assert.That(recycleBin.Entries.UCount, Is.EqualTo(0));
            Assert.That(recycleBin.Groups.UCount, Is.EqualTo(0));
            // 3. Tombstones created in DeletedObjects (2 entries + 1 subgroup = 3 tombstones)
            Assert.That(_host.Database.DeletedObjects.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(_host.Database.DeletedObjects.Any(d => d.Uuid.Equals(trash1.Uuid)), Is.True);
            Assert.That(_host.Database.DeletedObjects.Any(d => d.Uuid.Equals(trash2.Uuid)), Is.True);
            Assert.That(_host.Database.DeletedObjects.Any(d => d.Uuid.Equals(trashSubGroup.Uuid)), Is.True);
        }

        [Test]
        public void RecycleBin_Viewing_WithSubgroupsEnabled_ShouldNotFilterOutRecycleBinEntries()
        {
            var recycleBin = new PwGroup(true, true, "Recycle Bin", PwIcon.TrashBin);
            _host.Database.RootGroup.AddGroup(recycleBin, true);
            _host.Database.RecycleBinUuid = recycleBin.Uuid;
            _host.Database.RecycleBinEnabled = true;

            var trash = TestPluginHost.CreateTestEntry("Trash Entry", "u", "p");
            recycleBin.AddEntry(trash, true);

            _mainVm.LoadGroups();
            var binVm = _mainVm.RootGroups[0].Children.First(g => g.Model.Uuid.Equals(recycleBin.Uuid));
            _mainVm.SelectedGroup = binVm;

            // When viewing Recycle Bin with ShowSubgroupEntries = true, trash items MUST be visible!
            _mainVm.ShowSubgroupEntries = true;
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));
            Assert.That(_mainVm.FilteredEntries[0].Title, Is.EqualTo("Trash Entry"));
        }

        [Test]
        public void EntryItemViewModel_UuidEquality_ShouldPreserveSelectionOnFilter()
        {
            var e1 = TestPluginHost.CreateTestEntry("Alpha Entry", "a", "p");
            var e2 = TestPluginHost.CreateTestEntry("Beta Entry", "b", "p");
            _host.Database.RootGroup.AddEntry(e1, true);
            _host.Database.RootGroup.AddEntry(e2, true);

            _mainVm.LoadGroups();

            // Select Beta
            var betaVm = _mainVm.FilteredEntries.First(e => e.Title == "Beta Entry");
            _mainVm.SelectedEntry = betaVm;

            // Type filter matching both entries
            _mainVm.SearchText = "Entry";
            _mainVm.ApplyFilter();

            // Selection must be preserved on Beta Entry (same UUID)
            Assert.That(_mainVm.SelectedEntry, Is.Not.Null);
            Assert.That(_mainVm.SelectedEntry!.Title, Is.EqualTo("Beta Entry"));
        }
    }
}
