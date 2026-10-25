using System.Linq;
using FluentPass.Tests.Mocks;
using FluentPass.ViewModels;
using KeePassLib;
using NUnit.Framework;

namespace FluentPass.Tests
{
    /// <summary>
    /// 条目模板能力：EntryTemplatesGroup 枚举（原生语义）与“从模板新建”事务（Duplicate + 新 UUID + 无历史链）。
    /// </summary>
    [TestFixture, Apartment(System.Threading.ApartmentState.STA), Category("Integration")]
    public class TemplateEntryTests
    {
        private TestPluginHost _host = null!;
        private MainViewModel _mainVm = null!;
        private PwGroup _templateGroup = null!;

        [SetUp]
        public void Setup()
        {
            Common.MessageService.EnableDialogs = false;
            _host = new TestPluginHost();

            _templateGroup = new PwGroup(true, true, "Templates", PwIcon.Folder);
            var tpl1 = TestPluginHost.CreateTestEntry("Bank Template", "account", "", "https://bank.example", "IBAN goes here");
            var tpl2 = TestPluginHost.CreateTestEntry("Server Template", "root", "", "", "SSH server");
            _templateGroup.AddEntry(tpl1, true);
            _templateGroup.AddEntry(tpl2, true);
            _host.Database.RootGroup.AddGroup(_templateGroup, true);

            var workGroup = new PwGroup(true, true, "Work", PwIcon.Folder);
            _host.Database.RootGroup.AddGroup(workGroup, true);

            _mainVm = new MainViewModel(_host);
            _mainVm.LoadGroups();
            _mainVm.SelectedGroup = _mainVm.RootGroups[0].Children.First(g => g.Model == workGroup);
        }

        private void ConfigureTemplateGroup()
        {
            _host.Database.EntryTemplatesGroup = _templateGroup.Uuid;
            _mainVm.RefreshDatabaseState();
        }

        [Test]
        public void ReloadTemplates_NoTemplateGroupConfigured_HasTemplatesFalse()
        {
            Assert.That(_mainVm.HasTemplates, Is.False);
            Assert.That(_mainVm.Templates, Is.Empty);
        }

        [Test]
        public void ReloadTemplates_DanglingTemplateGroupUuid_HasTemplatesFalse_NoCrash()
        {
            _host.Database.EntryTemplatesGroup = new PwUuid(true);
            Assert.That(() => _mainVm.RefreshDatabaseState(), Throws.Nothing);
            Assert.That(_mainVm.HasTemplates, Is.False);
        }

        [Test]
        public void ReloadTemplates_ConfiguredGroup_ListsTopLevelEntries()
        {
            ConfigureTemplateGroup();

            Assert.That(_mainVm.HasTemplates, Is.True);
            Assert.That(_mainVm.Templates.Count, Is.EqualTo(2));
            CollectionAssert.AreEquivalent(
                new[] { "Bank Template", "Server Template" },
                _mainVm.Templates.Select(t => t.Title).ToArray());
        }

        [Test]
        public void NewEntryFromTemplate_CreatesFreshEntry_InSelectedGroup()
        {
            ConfigureTemplateGroup();
            var template = _mainVm.Templates.First(t => t.Title == "Bank Template").Model;

            int entriesBefore = _mainVm.Entries.Count;
            _mainVm.NewEntryFromTemplate(template);

            Assert.That(_mainVm.Entries.Count, Is.EqualTo(entriesBefore + 1));
            var created = _mainVm.Entries[0].Model;
            Assert.That(created.Uuid, Is.Not.EqualTo(template.Uuid), "必须生成全新 UUID");
            Assert.That(ReferenceEquals(created.ParentGroup, _mainVm.SelectedGroup!.Model), Is.True, "新条目应落入当前选中分组");
            Assert.That(created.Strings.ReadSafe(PwDefs.TitleField), Is.EqualTo("Bank Template"));
            Assert.That(created.Strings.ReadSafe(PwDefs.NotesField), Is.EqualTo("IBAN goes here"));
            Assert.That(created.History.UCount, Is.EqualTo(0u), "模板克隆不携带历史链");
            Assert.That(_host.Database.Modified, Is.True);
            Assert.That(_mainVm.SelectedEntry, Is.Not.Null);
            Assert.That(_mainVm.CurrentDetail, Is.Not.Null);
            Assert.That(_mainVm.CurrentDetail!.IsEditing, Is.True, "创建后应直接进入内联编辑");
        }

        [Test]
        public void NewEntryFromTemplate_NullTemplate_NoOp()
        {
            ConfigureTemplateGroup();
            int before = _mainVm.Entries.Count;
            _mainVm.NewEntryFromTemplate(null);
            Assert.That(_mainVm.Entries.Count, Is.EqualTo(before));
        }

        [Test]
        public void NewEntryFromTemplate_FieldsCopiedVerbatim_IncludingCustomFields()
        {
            ConfigureTemplateGroup();
            var template = _mainVm.Templates.First().Model;
            template.Strings.Set("IBAN", new KeePassLib.Security.ProtectedString(false, "DE89370400440532013000"));
            template.Strings.Set(PwDefs.PasswordField, new KeePassLib.Security.ProtectedString(true, "tpl-secret"));

            _mainVm.NewEntryFromTemplate(template);

            var created = _mainVm.Entries[0].Model;
            Assert.That(created.Strings.ReadSafe("IBAN"), Is.EqualTo("DE89370400440532013000"));
            Assert.That(created.Strings.ReadSafe(PwDefs.PasswordField), Is.EqualTo("tpl-secret"));
        }
    }
}
