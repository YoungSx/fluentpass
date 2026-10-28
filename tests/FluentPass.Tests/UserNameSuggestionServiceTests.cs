using System.Collections.Generic;
using FluentPass.Services;
using FluentPass.Tests.Mocks;
using KeePass;
using KeePass.App.Configuration;
using KeePassLib;
using KeePassLib.Security;
using NUnit.Framework;

namespace FluentPass.Tests
{
    [TestFixture]
    public class UserNameSuggestionServiceTests
    {
        [Test]
        public void GetSuggestions_NullOrClosedDatabase_ReturnsEmpty()
        {
            var resNull = UserNameSuggestionService.GetSuggestions(null, checkAsterisksMask: false);
            Assert.That(resNull, Is.Not.Null);
            Assert.That(resNull, Is.Empty);

            var dbClosed = new PwDatabase();
            var resClosed = UserNameSuggestionService.GetSuggestions(dbClosed, checkAsterisksMask: false);
            Assert.That(resClosed, Is.Not.Null);
            Assert.That(resClosed, Is.Empty);
        }

        [Test]
        public void GetSuggestions_EmptyDatabaseWithDefaultUser_ReturnsDefaultUser()
        {
            var host = new TestPluginHost();
            host.Database.DefaultUserName = "admin@example.com";

            var suggestions = UserNameSuggestionService.GetSuggestions(host.Database, checkAsterisksMask: false);

            Assert.That(suggestions, Has.Count.EqualTo(1));
            Assert.That(suggestions[0], Is.EqualTo("admin@example.com"));
        }

        [Test]
        public void GetSuggestions_MultipleEntries_DeduplicatesCaseInsensitivelyAndSorts()
        {
            var host = new TestPluginHost();
            var root = host.Database.RootGroup;

            // Root group entries
            var entry1 = TestPluginHost.CreateTestEntry("Entry 1", "alice", "pass1");
            var entry2 = TestPluginHost.CreateTestEntry("Entry 2", "Bob", "pass2");
            var entry3 = TestPluginHost.CreateTestEntry("Entry 3", "ALICE", "pass3"); // Duplicate case-insensitive
            var entry4 = TestPluginHost.CreateTestEntry("Entry 4", "  charlie  ", "pass4"); // Whitespace
            var entryEmpty = TestPluginHost.CreateTestEntry("Empty User", "", "pass5");

            root.AddEntry(entry1, true);
            root.AddEntry(entry2, true);
            root.AddEntry(entry3, true);
            root.AddEntry(entry4, true);
            root.AddEntry(entryEmpty, true);

            // Subgroup entries
            var subGroup = new PwGroup(true, true, "SubGroup", PwIcon.Folder);
            root.AddGroup(subGroup, true);

            var entrySub1 = TestPluginHost.CreateTestEntry("Sub Entry 1", "david", "pass6");
            var entrySub2 = TestPluginHost.CreateTestEntry("Sub Entry 2", "bob", "pass7"); // Duplicate with entry2
            subGroup.AddEntry(entrySub1, true);
            subGroup.AddEntry(entrySub2, true);

            var suggestions = UserNameSuggestionService.GetSuggestions(host.Database, checkAsterisksMask: false);

            Assert.That(suggestions, Is.Not.Null);
            // Expected: alice, Bob (or bob), charlie, david
            Assert.That(suggestions, Has.Count.EqualTo(4));
            Assert.That(suggestions, Does.Contain("alice").IgnoreCase);
            Assert.That(suggestions, Does.Contain("Bob").IgnoreCase);
            Assert.That(suggestions, Does.Contain("charlie").IgnoreCase);
            Assert.That(suggestions, Does.Contain("david").IgnoreCase);

            // Sorted order check
            Assert.That(string.Compare(suggestions[0], suggestions[1], true), Is.LessThanOrEqualTo(0));
            Assert.That(string.Compare(suggestions[1], suggestions[2], true), Is.LessThanOrEqualTo(0));
            Assert.That(string.Compare(suggestions[2], suggestions[3], true), Is.LessThanOrEqualTo(0));
        }

        [Test]
        public void GetSuggestions_DefaultUserAlreadyInEntries_DoesNotDuplicate()
        {
            var host = new TestPluginHost();
            host.Database.DefaultUserName = "admin";
            var root = host.Database.RootGroup;

            var entry = TestPluginHost.CreateTestEntry("Admin Entry", "ADMIN", "pass");
            root.AddEntry(entry, true);

            var suggestions = UserNameSuggestionService.GetSuggestions(host.Database, checkAsterisksMask: false);

            Assert.That(suggestions, Has.Count.EqualTo(1));
            Assert.That(suggestions[0], Is.EqualTo("admin").IgnoreCase);
        }

        [Test]
        public void GetSuggestions_WhenHideWithAsterisksConfigured_ReturnsEmpty()
        {
            var host = new TestPluginHost();
            var root = host.Database.RootGroup;
            root.AddEntry(TestPluginHost.CreateTestEntry("Entry 1", "alice", "pass1"), true);

            if (Program.Config?.MainWindow != null)
            {
                var col = Program.Config.MainWindow.FindColumn(AceColumnType.UserName);
                bool added = false;
                bool origHide = false;
                if (col == null)
                {
                    col = new AceColumn(AceColumnType.UserName);
                    Program.Config.MainWindow.EntryListColumns.Add(col);
                    added = true;
                }
                else
                {
                    origHide = col.HideWithAsterisks;
                }

                try
                {
                    col.HideWithAsterisks = true;
                    var suggestions = UserNameSuggestionService.GetSuggestions(host.Database, checkAsterisksMask: true);
                    Assert.That(suggestions, Is.Empty);
                }
                finally
                {
                    if (added)
                    {
                        Program.Config.MainWindow.EntryListColumns.Remove(col);
                    }
                    else
                    {
                        col.HideWithAsterisks = origHide;
                    }
                }
            }
        }
    }
}
