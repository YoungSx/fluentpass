using System;
using FluentPass.Tests.Mocks;
using FluentPass.ViewModels;
using KeePassLib;
using NUnit.Framework;

namespace FluentPass.Tests
{
    [TestFixture, Category("Unit")]
    public class EntryItemViewModelUnitTests
    {
        [Test, Category("Unit")]
        public void Constructor_ValidEntry_MapsPropertiesCorrectly()
        {
            var entry = TestPluginHost.CreateTestEntry("My Service", "admin@domain.com", "pass123", "https://example.com", "My note");
            var vm = new EntryItemViewModel(entry, null, null);

            Assert.That(vm.Title, Is.EqualTo("My Service"));
            Assert.That(vm.UserName, Is.EqualTo("admin@domain.com"));
            Assert.That(vm.Url, Is.EqualTo("https://example.com"));
            Assert.That(vm.NotesSnippet, Is.EqualTo("My note"));
            Assert.That(vm.HasPassword, Is.True);
            Assert.That(vm.LastModifiedDisplay, Is.Not.Null.And.Not.Empty);
        }

        [Test, Category("Unit")]
        public void Constructor_EmptyTitle_SetsFallbackNoTitle()
        {
            var entry = TestPluginHost.CreateTestEntry("", "admin", "pass");
            var vm = new EntryItemViewModel(entry, null, null);

            Assert.That(vm.Title, Is.EqualTo("(无标题)"));
        }

        [Test, Category("Unit")]
        public void Constructor_WhitespaceTitle_SetsFallbackNoTitle()
        {
            var entry = TestPluginHost.CreateTestEntry("   \t", "admin", "pass");
            var vm = new EntryItemViewModel(entry, null, null);

            Assert.That(vm.Title, Is.EqualTo("(无标题)"));
        }

        [Test, Category("Unit")]
        public void Constructor_LongNotes_TruncatesTo80CharsWithEllipsis()
        {
            string longNote = new string('A', 100);
            var entry = TestPluginHost.CreateTestEntry("Entry", "admin", "pass", notes: longNote);
            var vm = new EntryItemViewModel(entry, null, null);

            Assert.That(vm.NotesSnippet.Length, Is.EqualTo(83)); // 80 chars + "..."
            Assert.That(vm.NotesSnippet, Does.EndWith("..."));
            Assert.That(vm.NotesSnippet.StartsWith(new string('A', 80)), Is.True);
        }

        [Test, Category("Unit")]
        public void Constructor_ShortNotes_DoesNotTruncate()
        {
            string shortNote = new string('B', 80);
            var entry = TestPluginHost.CreateTestEntry("Entry", "admin", "pass", notes: shortNote);
            var vm = new EntryItemViewModel(entry, null, null);

            Assert.That(vm.NotesSnippet, Is.EqualTo(shortNote));
            Assert.That(vm.NotesSnippet, Does.Not.EndWith("..."));
        }

        [Test, Category("Unit")]
        public void Constructor_EmptyPassword_SetsHasPasswordFalse()
        {
            var entry = TestPluginHost.CreateTestEntry("No Pass Entry", "user", "");
            var vm = new EntryItemViewModel(entry, null, null);

            Assert.That(vm.HasPassword, Is.False);
        }

        [Test, Category("Unit")]
        public void Equals_And_HashCode_BasedOnUuid()
        {
            var entry1 = TestPluginHost.CreateTestEntry("Title 1", "u1", "p1");
            var entry2 = TestPluginHost.CreateTestEntry("Title 2", "u2", "p2");

            var vm1 = new EntryItemViewModel(entry1, null, null);
            var vm1Duplicate = new EntryItemViewModel(entry1, null, null);
            var vm2 = new EntryItemViewModel(entry2, null, null);

            Assert.That(vm1.Equals(vm1Duplicate), Is.True);
            Assert.That(vm1.Equals(vm2), Is.False);
            EntryItemViewModel? nullVm = null;
            Assert.That(vm1.Equals(nullVm), Is.False);
            object? nullObj = null;
            Assert.That(vm1.Equals(nullObj), Is.False);
            Assert.That(vm1.Equals((object)"some string"), Is.False);
            Assert.That(vm1.GetHashCode(), Is.EqualTo(vm1Duplicate.GetHashCode()));
        }

        [Test, Category("Unit")]
        public void Constructor_NullEntry_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new EntryItemViewModel(null!, null, null));
        }
    }
}
