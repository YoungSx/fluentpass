using System;
using System.Linq;
using FluentPass.Tests.Mocks;
using FluentPass.ViewModels;
using KeePassLib;
using KeePassLib.Security;
using NUnit.Framework;

namespace FluentPass.Tests
{
    [TestFixture, Apartment(System.Threading.ApartmentState.STA), Category("Integration")]
    public class StateSyncAndSaveTests
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

        [Test, Category("Integration")]
        public void StatusMessage_WhenDatabaseIsOpen_ShouldBeReadyNotUnopened()
        {
            // Initial state with open database
            Assert.That(_host.Database.IsOpen, Is.True);
            _mainVm.RefreshDatabaseState();

            // Status message must NOT be "未打开数据库"
            Assert.That(_mainVm.StatusMessage, Is.Not.EqualTo("未打开数据库"));
            Assert.That(_mainVm.StatusMessage, Does.StartWith("就绪"));
        }

        [Test, Category("Integration")]
        public void StatusMessage_WhenDatabaseModified_ShouldReflectUnsavedChanges()
        {
            _host.Database.Modified = true;
            _mainVm.StatusMessage = "未打开数据库"; // Simulate stale status before refresh
            _mainVm.RefreshDatabaseState();

            Assert.That(_mainVm.IsModified, Is.True);
            Assert.That(_mainVm.StatusMessage, Is.EqualTo("就绪 (存在未保存的修改)"));
            Assert.That(_mainVm.WindowTitle, Does.EndWith("*"));
        }

        [Test, Category("Integration")]
        public void SaveDatabase_WhenEditingEntry_ShouldAutoCommitEntryAndSaveDatabase()
        {
            var entry = TestPluginHost.CreateTestEntry("Initial Title", "testuser", "secret");
            _host.Database.RootGroup.AddEntry(entry, true);
            _mainVm.LoadGroups();

            _mainVm.SelectedEntry = _mainVm.FilteredEntries.First(e => e.Title == "Initial Title");
            Assert.That(_mainVm.CurrentDetail, Is.Not.Null);

            // Enter edit mode on entry
            _mainVm.CurrentDetail!.BeginEdit();
            _mainVm.CurrentDetail.EditTitle = "Updated Title Via Save";
            _mainVm.CurrentDetail.EditUserName = "newuser";
            Assert.That(_mainVm.CurrentDetail.IsEditing, Is.True);

            // Execute Save Database
            _mainVm.SaveDatabase();

            // 1. Entry edit should be automatically committed
            Assert.That(_mainVm.CurrentDetail.IsEditing, Is.False);
            Assert.That(entry.Strings.ReadSafe(PwDefs.TitleField), Is.EqualTo("Updated Title Via Save"));
            Assert.That(entry.Strings.ReadSafe(PwDefs.UserNameField), Is.EqualTo("newuser"));

            // 2. Status message should report saved
            Assert.That(_mainVm.StatusMessage, Is.EqualTo("数据库已保存"));
        }

        [Test, Category("Integration")]
        public void Database_CloseAndReopen_StatusMessageTransitionsCleanly()
        {
            // Simulate database close
            _host.Database.Close();
            _mainVm.RefreshDatabaseState();

            Assert.That(_mainVm.IsDatabaseOpen, Is.False);
            Assert.That(_mainVm.StatusMessage, Is.EqualTo("未打开数据库"));

            // Re-open database
            var newHost = new TestPluginHost();
            _mainVm = new MainViewModel(newHost);
            _mainVm.RefreshDatabaseState();

            Assert.That(_mainVm.IsDatabaseOpen, Is.True);
            Assert.That(_mainVm.StatusMessage, Is.EqualTo("就绪"));
        }
    }
}
