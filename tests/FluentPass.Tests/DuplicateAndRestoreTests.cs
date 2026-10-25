using System.Linq;
using FluentPass.Tests.Mocks;
using FluentPass.ViewModels;
using KeePassLib;
using NUnit.Framework;

namespace FluentPass.Tests
{
    /// <summary>
    /// 复制条目（官方 PwEntry.Duplicate + DuplicationForm.ApplyTo 语义）与
    /// 回收站还原（对齐原生 Restore：目标为回收站父分组，复用移动管线）。
    /// </summary>
    [TestFixture, Apartment(System.Threading.ApartmentState.STA), Category("Integration")]
    public class DuplicateAndRestoreTests
    {
        private TestPluginHost _host = null!;
        private MainViewModel _mainVm = null!;

        [SetUp]
        public void Setup()
        {
            Common.MessageService.EnableDialogs = false;
            _host = new TestPluginHost();
            _mainVm = new MainViewModel(_host);
            _mainVm.LoadGroups();
        }

        // ===== 复制条目 =====

        [Test]
        public void DuplicateEntry_CreatesIndependentCopy_WithNewUuid()
        {
            var original = TestPluginHost.CreateTestEntry("Source", "user1", "pass1", "https://src.example", "note");
            original.Tags.Add("keepme");
            _host.Database.RootGroup.AddEntry(original, true);
            _mainVm.LoadGroupEntries(_mainVm.SelectedGroup);
            var vm = _mainVm.Entries.First(e => e.Model == original);

            _mainVm.DuplicateEntry(vm);

            Assert.That(_host.Database.Modified, Is.True);
            var copies = _host.Database.RootGroup.Entries.Where(e => e.Uuid != original.Uuid).ToList();
            Assert.That(copies.Count, Is.EqualTo(1), "应产生恰好一个副本");
            var copy = copies[0];
            Assert.That(copy.Strings.ReadSafe(PwDefs.TitleField), Is.EqualTo("Source"));
            Assert.That(copy.Strings.ReadSafe(PwDefs.UserNameField), Is.EqualTo("user1"));
            CollectionAssert.Contains(copy.Tags.ToArray(), "keepme");
        }

        [Test]
        public void DuplicateEntry_RefreshesList_IncludingCopy()
        {
            var original = TestPluginHost.CreateTestEntry("ListSource", "u", "p", "", "");
            _host.Database.RootGroup.AddEntry(original, true);
            _mainVm.LoadGroupEntries(_mainVm.SelectedGroup);

            _mainVm.DuplicateEntry(_mainVm.Entries.First(e => e.Model == original));

            Assert.That(_mainVm.Entries.Count, Is.EqualTo(2));
            Assert.That(_mainVm.Entries.Any(e => e.Model.Uuid != original.Uuid), Is.True);
        }

        [Test]
        public void DuplicateEntry_NullOrNoSelection_NoOp()
        {
            Assert.That(() => _mainVm.DuplicateEntry(null), Throws.Nothing);
        }

        // ===== 回收站还原 =====

        private PwGroup SetupRecycleBin()
        {
            _host.Database.RecycleBinEnabled = true;
            var rb = new PwGroup(true, true, "Recycle Bin", PwIcon.TrashBin);
            _host.Database.RootGroup.AddGroup(rb, true);
            _host.Database.RecycleBinUuid = rb.Uuid;
            return rb;
        }

        [Test]
        public void RestoreEntry_MovesOutOfRecycleBin_ToRootGroup()
        {
            var rb = SetupRecycleBin();
            var entry = TestPluginHost.CreateTestEntry("Deleted", "u", "p", "", "");
            rb.AddEntry(entry, true);

            _mainVm.RefreshDatabaseState();
            var rbVm = _mainVm.RootGroups[0].Children.FirstOrDefault(c => c.Model == rb);
            Assert.That(rbVm, Is.Not.Null, "测试前提：回收站分组应出现在树中");
            _mainVm.SelectedGroup = rbVm;
            Assert.That(_mainVm.IsRecycleBinSelected, Is.True);
            Assert.That(_mainVm.CanRestoreSelectedEntry, Is.True);

            var entryVm = _mainVm.Entries.First(e => e.Model == entry);
            _mainVm.RestoreEntryFromRecycleBin(entryVm);

            Assert.That(ReferenceEquals(entry.ParentGroup, _host.Database.RootGroup), Is.True, "对齐原生 Restore：还原到回收站父分组（根）");
            Assert.That(rb.Entries.Contains(entry), Is.False);
            Assert.That(_host.Database.Modified, Is.True);
        }

        [Test]
        public void RestoreEntry_EntryOutsideRecycleBin_NoOp()
        {
            SetupRecycleBin();
            var entry = TestPluginHost.CreateTestEntry("Normal", "u", "p", "", "");
            _host.Database.RootGroup.AddEntry(entry, true);
            _mainVm.RefreshDatabaseState();

            _mainVm.RestoreEntryFromRecycleBin(null);
            Assert.That(ReferenceEquals(entry.ParentGroup, _host.Database.RootGroup), Is.True);
        }

        [Test]
        public void RestoreEntry_RecycleBinDisabled_NoOp()
        {
            _host.Database.RecycleBinEnabled = false;
            var entry = TestPluginHost.CreateTestEntry("Orphan", "u", "p", "", "");
            _host.Database.RootGroup.AddEntry(entry, true);
            _mainVm.RefreshDatabaseState();

            Assert.That(() => _mainVm.RestoreEntryFromRecycleBin(null), Throws.Nothing);
        }
    }
}
