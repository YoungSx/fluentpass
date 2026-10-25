using System;
using System.Linq;
using FluentPass.Tests.Mocks;
using FluentPass.ViewModels;
using KeePassLib;
using KeePassLib.Security;
using NUnit.Framework;

namespace FluentPass.Tests
{
    [TestFixture, Category("Integration")]
    public class CustomFieldEditIntegrationTests
    {
        private TestPluginHost _host = null!;
        private MainViewModel _mainVm = null!;
        private PwEntry _entry = null!;
        private EntryDetailViewModel _detail = null!;

        [SetUp]
        public void Setup()
        {
            Common.MessageService.EnableDialogs = false;
            _host = new TestPluginHost();

            _entry = TestPluginHost.CreateTestEntry("GitHub", "octocat", "pass");
            _entry.Strings.Set("ApiKey", new ProtectedString(false, "plain-key"));
            _entry.Strings.Set("OtpSeed", new ProtectedString(true, "seed-secret"));
            _host.Database.RootGroup.AddEntry(_entry, true);

            _mainVm = new MainViewModel(_host);
            _mainVm.LoadGroups();
            _mainVm.SelectedEntry = _mainVm.FilteredEntries.First();
            _detail = _mainVm.CurrentDetail!;
        }

        [Test]
        public void RefreshData_LoadsCustomFields_WithProtectionFlags()
        {
            Assert.That(_detail.CustomFields.Count, Is.EqualTo(2));
            var apiKey = _detail.CustomFields.First(f => f.Key == "ApiKey");
            var otp = _detail.CustomFields.First(f => f.Key == "OtpSeed");

            Assert.That(apiKey.IsProtected, Is.False);
            Assert.That(apiKey.DisplayValue, Is.EqualTo("plain-key"));
            Assert.That(otp.IsProtected, Is.True);
            Assert.That(otp.DisplayValue, Is.EqualTo("••••••••••••"));
        }

        [Test]
        public void EditValueChange_ProtectedFlagChange_SavePersistsAll()
        {
            _detail.BeginEdit();

            var apiKey = _detail.CustomFields.First(f => f.Key == "ApiKey");
            Assert.That(apiKey.EditValue, Is.EqualTo("plain-key"), "BeginEdit 必须把当前值复制进草稿");

            // 修改值 + 切换保护
            apiKey.EditValue = "rotated-key";
            apiKey.ToggleEditProtection();
            Assert.That(apiKey.IsProtected, Is.True);

            // 重命名字段
            var otp = _detail.CustomFields.First(f => f.Key == "OtpSeed");
            otp.EditKey = "OtpSeedRenamed";

            _detail.SaveEditInternal();

            Assert.That(_entry.Strings.ReadSafe("OtpSeed"), Is.Empty, "被重命名的旧键必须已被移除");
            Assert.That(_entry.Strings.ReadSafe("ApiKey"), Is.EqualTo("rotated-key"));
            Assert.That(_entry.Strings.GetSafe("ApiKey").IsProtected, Is.True);
            Assert.That(_entry.Strings.ReadSafe("OtpSeedRenamed"), Is.EqualTo("seed-secret"), "重命名后原值保留");
            Assert.That(_entry.Strings.GetSafe("OtpSeedRenamed").IsProtected, Is.True, "未改保护标志的字段保持原保护状态");
            Assert.That(_host.Database.Modified, Is.True);
            Assert.That(_entry.History.UCount, Is.GreaterThan(0), "保存前必须创建历史备份");
        }

        [Test]
        public void AddField_Save_WritesToEntry()
        {
            _detail.BeginEdit();
            _detail.AddCustomField();
            var added = _detail.CustomFields.Single(f => f.IsNew);
            added.EditKey = "Server";
            added.EditValue = "host-01";
            added.ToggleEditProtection();

            _detail.SaveEditInternal();

            Assert.That(_entry.Strings.ReadSafe("Server"), Is.EqualTo("host-01"));
            Assert.That(_entry.Strings.GetSafe("Server").IsProtected, Is.True);
            Assert.That(_detail.CustomFields.Any(f => f.IsNew), Is.False, "保存后 RefreshData 重建集合，不再有草稿行");
        }

        [Test]
        public void RemoveField_Save_DeletesFromEntry()
        {
            _detail.BeginEdit();
            var otp = _detail.CustomFields.First(f => f.Key == "OtpSeed");
            _detail.RemoveCustomField(otp);

            _detail.SaveEditInternal();

            Assert.That(_entry.Strings.ReadSafe("OtpSeed"), Is.Empty);
            Assert.That(_entry.Strings.ReadSafe("ApiKey"), Is.EqualTo("plain-key"), "其余字段不受影响");
        }

        [Test]
        public void CancelEdit_DiscardsDrafts_AndDropsBlankNewRows()
        {
            _detail.BeginEdit();
            _detail.CustomFields.First(f => f.Key == "ApiKey").EditValue = "modified";
            _detail.AddCustomField();

            _detail.CancelEdit();

            Assert.That(_detail.CustomFields.Count, Is.EqualTo(2), "新增空行被丢弃，原有字段保留");
            Assert.That(_detail.CustomFields.All(f => !f.IsNew), Is.True);
            Assert.That(_detail.CustomFields.First(f => f.Key == "ApiKey").EditValue, Is.Empty, "草稿明文必须清空");
            Assert.That(_entry.Strings.ReadSafe("ApiKey"), Is.EqualTo("plain-key"), "条目模型不受取消影响");
        }

        [Test]
        public void AddField_Cancel_DropsNewRowFromEntry()
        {
            _detail.BeginEdit();
            _detail.AddCustomField();
            var added = _detail.CustomFields.Single(f => f.IsNew);
            added.EditKey = "Ghost";
            added.EditValue = "value";

            _detail.CancelEdit();

            Assert.That(_entry.Strings.ReadSafe("Ghost"), Is.Empty, "未保存的新增字段绝不能写入条目");
        }

        [Test]
        public void Save_WithEmptyKey_ShowsValidationAndDoesNotTouchModel()
        {
            _detail.BeginEdit();
            _detail.CustomFields.First(f => f.Key == "ApiKey").EditKey = "  ";

            _detail.SaveEditInternal();

            Assert.That(_detail.IsEditing, Is.True, "校验失败必须停留在编辑态");
            Assert.That(_entry.Strings.ReadSafe("ApiKey"), Is.EqualTo("plain-key"), "非法草稿绝不能触发写入");
            Assert.That(_entry.History.UCount, Is.EqualTo(0), "校验先于备份：不得为非法草稿产生历史版本");
        }

        [Test]
        public void Save_WithStandardFieldName_IsRejected()
        {
            _detail.BeginEdit();
            _detail.CustomFields.First(f => f.Key == "ApiKey").EditKey = "Notes";

            _detail.SaveEditInternal();

            Assert.That(_detail.IsEditing, Is.True);
            Assert.That(_entry.Strings.ReadSafe("Notes"), Is.EqualTo(string.Empty), "标准字段冲突必须被拒绝");
        }

        [Test]
        public void Save_WithDuplicateKeys_IsRejected()
        {
            _detail.BeginEdit();
            _detail.AddCustomField();
            var added = _detail.CustomFields.Single(f => f.IsNew);
            added.EditKey = "ApiKey"; // 与现有字段重名

            _detail.SaveEditInternal();

            Assert.That(_detail.IsEditing, Is.True);
            Assert.That(_entry.Strings.ReadSafe("ApiKey"), Is.EqualTo("plain-key"));
        }

        [Test]
        public void Save_WithBlankNewRow_SilentlyDropsIt()
        {
            _detail.BeginEdit();
            _detail.AddCustomField(); // 名称与值均留空

            _detail.SaveEditInternal();

            Assert.That(_detail.IsEditing, Is.False, "全空白新增行应被静默丢弃而非阻塞保存");
            Assert.That(_entry.Strings.ReadSafe("ApiKey"), Is.EqualTo("plain-key"));
        }

        [Test]
        public void IsCustomFieldsSectionVisible_EditingAlwaysShowsSection()
        {
            var entry = TestPluginHost.CreateTestEntry("NoCustomFields", "u", "p");
            _host.Database.RootGroup.AddEntry(entry, true);
            _mainVm.RefreshDatabaseState();
            _mainVm.SelectedGroup = _mainVm.RootGroups[0].Children.Count > 0
                ? _mainVm.RootGroups[0].Children[0]
                : _mainVm.RootGroups[0];
            _mainVm.SelectedEntry = _mainVm.FilteredEntries.First(e => ReferenceEquals(e.Model, entry));
            var detail = _mainVm.CurrentDetail!;

            Assert.That(detail.CustomFields, Is.Empty);
            Assert.That(detail.IsCustomFieldsSectionVisible, Is.False, "查看态且无字段时隐藏区块");

            detail.BeginEdit();
            Assert.That(detail.IsCustomFieldsSectionVisible, Is.True, "编辑态必须显示区块以提供添加入口");
        }

        [Test]
        public void ExternalRefresh_RebuildsCustomFields()
        {
            // 模拟原版窗口/其他端修改了自定义字段
            _entry.Strings.Remove("ApiKey");
            _entry.Strings.Set("NewFromNative", new ProtectedString(true, "x"));
            _detail.RefreshData();

            Assert.That(_detail.CustomFields.Any(f => f.Key == "ApiKey"), Is.False);
            Assert.That(_detail.CustomFields.Any(f => f.Key == "NewFromNative"), Is.True);
        }
    }
}
