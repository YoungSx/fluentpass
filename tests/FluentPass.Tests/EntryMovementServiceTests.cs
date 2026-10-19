using System;
using System.Collections.Generic;
using System.Linq;
using FluentPass.Services;
using FluentPass.Tests.Mocks;
using KeePassLib;
using NUnit.Framework;

namespace FluentPass.Tests
{
    [TestFixture, Category("Unit")]
    public class EntryMovementServiceTests
    {
        [Test, Category("Unit")]
        public void MoveEntries_NullParameters_ReturnsZeroAndDoesNotModifyDatabase()
        {
            var host = new TestPluginHost();
            var db = host.Database;
            db.Modified = false;

            var targetGroup = new PwGroup(true, true, "Target", PwIcon.Folder);
            db.RootGroup.AddGroup(targetGroup, true);

            var entry = TestPluginHost.CreateTestEntry("Entry", "user", "pass");
            db.RootGroup.AddEntry(entry, true);

            // Null entries
            int res1 = EntryMovementService.MoveEntries(null, targetGroup, db);
            Assert.That(res1, Is.EqualTo(0));
            Assert.That(db.Modified, Is.False);

            // Null target group
            int res2 = EntryMovementService.MoveEntries(new[] { entry }, null, db);
            Assert.That(res2, Is.EqualTo(0));
            Assert.That(db.Modified, Is.False);

            // Null database
            int res3 = EntryMovementService.MoveEntries(new[] { entry }, targetGroup, null);
            Assert.That(res3, Is.EqualTo(0));
        }

        [Test, Category("Unit")]
        public void MoveEntries_EmptyList_ReturnsZeroAndDoesNotModifyDatabase()
        {
            var host = new TestPluginHost();
            var db = host.Database;
            db.Modified = false;

            var targetGroup = new PwGroup(true, true, "Target", PwIcon.Folder);
            db.RootGroup.AddGroup(targetGroup, true);

            int moved = EntryMovementService.MoveEntries(new List<PwEntry>(), targetGroup, db);
            Assert.That(moved, Is.EqualTo(0));
            Assert.That(db.Modified, Is.False);
        }

        [Test, Category("Unit")]
        public void MoveEntries_SingleEntryToNewGroup_MovesEntryRemovesFromOldGroupSetsOwnershipAndUpdatesLocation()
        {
            var host = new TestPluginHost();
            var db = host.Database;
            db.Modified = false;

            var groupA = new PwGroup(true, true, "Group A", PwIcon.Folder);
            var groupB = new PwGroup(true, true, "Group B", PwIcon.Folder);
            db.RootGroup.AddGroup(groupA, true);
            db.RootGroup.AddGroup(groupB, true);

            var entry = TestPluginHost.CreateTestEntry("Secret Item", "admin", "pass123");
            groupA.AddEntry(entry, true);

            DateTime beforeMove = DateTime.UtcNow.AddSeconds(-1);
            bool callbackInvoked = false;

            int moved = EntryMovementService.MoveEntries(
                new[] { entry },
                groupB,
                db,
                onDatabaseModified: () => callbackInvoked = true);

            Assert.That(moved, Is.EqualTo(1));
            Assert.That(groupA.Entries.UCount, Is.EqualTo(0u), "Entry should be removed from source group");
            Assert.That(groupB.Entries.UCount, Is.EqualTo(1u), "Entry should be added to target group");
            Assert.That(groupB.Entries.GetAt(0), Is.SameAs(entry));
            Assert.That(entry.ParentGroup, Is.SameAs(groupB), "Entry ParentGroup should be target group");
            Assert.That(entry.LocationChanged, Is.GreaterThanOrEqualTo(beforeMove), "LocationChanged timestamp should be updated");
            Assert.That(db.Modified, Is.True, "Database Modified flag should be set to true");
            Assert.That(callbackInvoked, Is.True, "Database modified callback should be invoked");
        }

        [Test, Category("Unit")]
        public void MoveEntries_EntryAlreadyInTargetGroup_SkipsAndDoesNotDuplicateOrCorrupt()
        {
            var host = new TestPluginHost();
            var db = host.Database;
            db.Modified = false;

            var targetGroup = new PwGroup(true, true, "Target", PwIcon.Folder);
            db.RootGroup.AddGroup(targetGroup, true);

            var entry = TestPluginHost.CreateTestEntry("Existing Item", "user", "pass");
            targetGroup.AddEntry(entry, true);

            bool callbackInvoked = false;
            int moved = EntryMovementService.MoveEntries(
                new[] { entry },
                targetGroup,
                db,
                onDatabaseModified: () => callbackInvoked = true);

            Assert.That(moved, Is.EqualTo(0), "No items should be moved when already in target group");
            Assert.That(targetGroup.Entries.UCount, Is.EqualTo(1u), "Target group should not have duplicate entries");
            Assert.That(db.Modified, Is.False, "Database Modified flag should remain false");
            Assert.That(callbackInvoked, Is.False, "Callback should not be invoked when no-op");
        }

        [Test, Category("Unit")]
        public void MoveEntries_MultipleEntries_AllMovedToTargetGroupSuccessfully()
        {
            var host = new TestPluginHost();
            var db = host.Database;
            db.Modified = false;

            var groupSource = new PwGroup(true, true, "Source", PwIcon.Folder);
            var groupTarget = new PwGroup(true, true, "Target", PwIcon.Folder);
            db.RootGroup.AddGroup(groupSource, true);
            db.RootGroup.AddGroup(groupTarget, true);

            var entry1 = TestPluginHost.CreateTestEntry("Entry 1", "u1", "p1");
            var entry2 = TestPluginHost.CreateTestEntry("Entry 2", "u2", "p2");
            var entry3 = TestPluginHost.CreateTestEntry("Entry 3", "u3", "p3");
            groupSource.AddEntry(entry1, true);
            groupSource.AddEntry(entry2, true);
            groupSource.AddEntry(entry3, true);

            int moved = EntryMovementService.MoveEntries(
                new[] { entry1, entry2, entry3 },
                groupTarget,
                db);

            Assert.That(moved, Is.EqualTo(3));
            Assert.That(groupSource.Entries.UCount, Is.EqualTo(0u));
            Assert.That(groupTarget.Entries.UCount, Is.EqualTo(3u));
            Assert.That(entry1.ParentGroup, Is.SameAs(groupTarget));
            Assert.That(entry2.ParentGroup, Is.SameAs(groupTarget));
            Assert.That(entry3.ParentGroup, Is.SameAs(groupTarget));
            Assert.That(db.Modified, Is.True);
        }

        [Test, Category("Unit")]
        public void MoveEntries_MixedEntries_SomeAlreadyInTarget_OnlyMovesOthers()
        {
            var host = new TestPluginHost();
            var db = host.Database;
            db.Modified = false;

            var groupA = new PwGroup(true, true, "Group A", PwIcon.Folder);
            var groupTarget = new PwGroup(true, true, "Target", PwIcon.Folder);
            db.RootGroup.AddGroup(groupA, true);
            db.RootGroup.AddGroup(groupTarget, true);

            var entryA1 = TestPluginHost.CreateTestEntry("Entry A1", "u1", "p1");
            var entryA2 = TestPluginHost.CreateTestEntry("Entry A2", "u2", "p2");
            var entryTarget = TestPluginHost.CreateTestEntry("Target Entry", "ut", "pt");

            groupA.AddEntry(entryA1, true);
            groupA.AddEntry(entryA2, true);
            groupTarget.AddEntry(entryTarget, true);

            // Attempt to move all three to groupTarget
            int moved = EntryMovementService.MoveEntries(
                new[] { entryA1, entryTarget, entryA2 },
                groupTarget,
                db);

            Assert.That(moved, Is.EqualTo(2), "Only the 2 entries not in groupTarget should be moved");
            Assert.That(groupA.Entries.UCount, Is.EqualTo(0u));
            Assert.That(groupTarget.Entries.UCount, Is.EqualTo(3u));
            Assert.That(groupTarget.Entries.Any(e => e == entryTarget), Is.True);
            Assert.That(groupTarget.Entries.Count(e => e == entryTarget), Is.EqualTo(1), "Target entry should not be duplicated");
        }

        [Test, Category("Unit")]
        public void MoveEntries_NullOrDuplicateReferencesInCollection_HandledSafely()
        {
            var host = new TestPluginHost();
            var db = host.Database;
            db.Modified = false;

            var groupA = new PwGroup(true, true, "Group A", PwIcon.Folder);
            var groupB = new PwGroup(true, true, "Group B", PwIcon.Folder);
            db.RootGroup.AddGroup(groupA, true);
            db.RootGroup.AddGroup(groupB, true);

            var entry1 = TestPluginHost.CreateTestEntry("Entry 1", "u1", "p1");
            groupA.AddEntry(entry1, true);

            // Pass collection containing null and duplicates of entry1
            var input = new List<PwEntry?> { null, entry1, null, entry1 };

            int moved = EntryMovementService.MoveEntries(input!, groupB, db);

            Assert.That(moved, Is.EqualTo(1));
            Assert.That(groupB.Entries.UCount, Is.EqualTo(1u));
            Assert.That(groupA.Entries.UCount, Is.EqualTo(0u));
        }
    }
}
