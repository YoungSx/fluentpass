using System;
using FluentPass.Tests.Mocks;
using FluentPass.ViewModels;
using KeePassLib;
using NUnit.Framework;

namespace FluentPass.Tests
{
    [TestFixture, Category("Unit")]
    public class EntryHistoryItemViewModelUnitTests
    {
        [Test, Category("Unit")]
        public void Constructor_ValidHistoryEntry_SetsPropertiesCorrectly()
        {
            var entry = TestPluginHost.CreateTestEntry("History 1", "testuser", "pass");
            bool callbackFired = false;
            PwEntry? restoredEntry = null;

            var vm = new EntryHistoryItemViewModel(entry, 1, e =>
            {
                callbackFired = true;
                restoredEntry = e;
            });

            Assert.That(vm.Model, Is.SameAs(entry));
            Assert.That(vm.RevisionIndex, Is.EqualTo(1));
            Assert.That(vm.Title, Is.EqualTo("History 1"));
            Assert.That(vm.UserName, Is.EqualTo("testuser"));
            Assert.That(vm.LastModifiedDisplay, Is.Not.Null.And.Not.Empty);

            vm.Restore();
            Assert.That(callbackFired, Is.True);
            Assert.That(restoredEntry, Is.SameAs(entry));
        }

        [Test, Category("Unit")]
        public void Constructor_EmptyTitle_SetsFallbackNoTitle()
        {
            var entry = TestPluginHost.CreateTestEntry("", "user2", "pass");
            var vm = new EntryHistoryItemViewModel(entry, 0, _ => { });

            Assert.That(vm.Title, Is.EqualTo("(无标题)"));
        }

        [Test, Category("Unit")]
        public void Constructor_WhitespaceTitle_SetsFallbackNoTitle()
        {
            var entry = TestPluginHost.CreateTestEntry("   ", "user3", "pass");
            var vm = new EntryHistoryItemViewModel(entry, 0, _ => { });

            Assert.That(vm.Title, Is.EqualTo("(无标题)"));
        }

        [Test, Category("Unit")]
        public void Constructor_NullEntry_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new EntryHistoryItemViewModel(null!, 0, _ => { }));
        }

        [Test, Category("Unit")]
        public void Constructor_NullCallback_ThrowsArgumentNullException()
        {
            var entry = TestPluginHost.CreateTestEntry("Title", "user", "pass");
            Assert.Throws<ArgumentNullException>(() => new EntryHistoryItemViewModel(entry, 0, null!));
        }
    }
}
