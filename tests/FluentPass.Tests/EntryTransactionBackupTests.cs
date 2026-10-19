using FluentPass.Tests.Mocks;
using FluentPass.ViewModels;
using KeePassLib;
using NUnit.Framework;

namespace FluentPass.Tests
{
    [TestFixture, Apartment(System.Threading.ApartmentState.STA), Category("Integration")]
    public class EntryTransactionBackupTests
    {
        [SetUp]
        public void Setup()
        {
            Common.MessageService.EnableDialogs = false;
        }

        [Test, Category("Integration")]
        public void SaveEdit_MustCreateHistoryBackup_AndSetDatabaseModified()
        {
            // 规范 4: 数据修改、历史记录与状态回写标准流程
            var host = new TestPluginHost();
            const string initialPassword = "OldPassword111";
            const string newPassword = "NewUpdatedPassword222";

            var entry = TestPluginHost.CreateTestEntry("Original Title", "user1", initialPassword);
            host.Database.RootGroup.AddEntry(entry, true);
            host.Database.Modified = false;

            Assert.That(entry.History.UCount, Is.EqualTo(0u), "Initial history should be empty");

            bool modifiedCallbackFired = false;
            var detailVm = new EntryDetailViewModel(entry, host, null, () => { modifiedCallbackFired = true; });

            // 进入编辑模式并修改字段
            detailVm.BeginEdit();
            detailVm.EditTitle = "Updated Title";
            detailVm.EditPassword = newPassword;
            detailVm.SaveEdit();

            // 1. 验证历史记录已备份
            Assert.That(entry.History.UCount, Is.EqualTo(1u), "CreateBackup must create exactly 1 history item before updating");
            var backupVersion = entry.History.GetAt(0);
            Assert.That(backupVersion.Strings.ReadSafe(PwDefs.TitleField), Is.EqualTo("Original Title"));
            Assert.That(backupVersion.Strings.ReadSafe(PwDefs.PasswordField), Is.EqualTo(initialPassword));

            // 2. 验证当前条目已更新为新数据
            Assert.That(entry.Strings.ReadSafe(PwDefs.TitleField), Is.EqualTo("Updated Title"));
            Assert.That(entry.Strings.ReadSafe(PwDefs.PasswordField), Is.EqualTo(newPassword));

            // 3. 验证数据库被标记为 Modified
            Assert.That(host.Database.Modified, Is.True);
            Assert.That(modifiedCallbackFired, Is.True);
        }
    }
}
