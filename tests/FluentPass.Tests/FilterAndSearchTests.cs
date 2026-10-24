using System.Linq;
using FluentPass.Tests.Mocks;
using FluentPass.ViewModels;
using KeePassLib;
using NUnit.Framework;

namespace FluentPass.Tests
{
    [TestFixture, Apartment(System.Threading.ApartmentState.STA), Category("Integration")]
    public class FilterAndSearchTests
    {
        private TestPluginHost _host = null!;
        private MainViewModel _mainVm = null!;

        [Test]
        public void ReapplySearch_PreservesSelectedInstanceWithoutCollectionReset()
        {
            var first = TestPluginHost.CreateTestEntry("Match First", "first", "fixture");
            var second = TestPluginHost.CreateTestEntry("Match Second", "second", "fixture");
            _host.Database.RootGroup.AddEntry(first, true);
            _host.Database.RootGroup.AddEntry(second, true);
            _mainVm.LoadGroups();
            _mainVm.SearchText = "Match";
            _mainVm.ApplyFilter();
            var selected = _mainVm.FilteredEntries.Single(e => e.Model == second);
            _mainVm.SelectedEntry = selected;
            int resets = 0;
            _mainVm.FilteredEntries.CollectionChanged += (_, e) =>
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset) resets++;
            };

            _mainVm.ApplyFilter();

            Assert.That(_mainVm.SelectedEntry, Is.SameAs(selected));
            Assert.That(resets, Is.Zero);
        }

        [SetUp]
        public void Setup()
        {
            Common.MessageService.EnableDialogs = false;
            _host = new TestPluginHost();

            var e1 = TestPluginHost.CreateTestEntry("GitHub Account", "octocat", "pass1", "https://github.com", "My code repository");
            var e2 = TestPluginHost.CreateTestEntry("Google Mail", "john.doe@gmail.com", "pass2", "https://mail.google.com", "Primary email");
            var e3 = TestPluginHost.CreateTestEntry("Microsoft Azure", "admin@company.onmicrosoft.com", "pass3", "https://portal.azure.com", "Cloud infrastructure");
            var e4 = TestPluginHost.CreateTestEntry("Server SSH Key", "root", "pass4", "", "Production linux host 192.168.1.100");

            _host.Database.RootGroup.AddEntry(e1, true);
            _host.Database.RootGroup.AddEntry(e2, true);
            _host.Database.RootGroup.AddEntry(e3, true);
            _host.Database.RootGroup.AddEntry(e4, true);

            _mainVm = new MainViewModel(_host);
            _mainVm.LoadGroups();
        }

        [Test, Category("Integration")]
        public void Filter_WithEmptySearch_ShouldReturnAllEntries()
        {
            _mainVm.SearchText = string.Empty;
            _mainVm.ApplyFilter();
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(4));
        }

        [Test, Category("Integration")]
        public void Filter_ByTitleKeyword_ShouldReturnMatchingEntries()
        {
            _mainVm.SearchText = "git";
            _mainVm.ApplyFilter();
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));
            Assert.That(_mainVm.FilteredEntries[0].Title, Is.EqualTo("GitHub Account"));
        }

        [Test, Category("Integration")]
        public void Filter_ByUserName_ShouldReturnMatchingEntries()
        {
            _mainVm.SearchText = "john.doe";
            _mainVm.ApplyFilter();
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));
            Assert.That(_mainVm.FilteredEntries[0].Title, Is.EqualTo("Google Mail"));
        }

        [Test, Category("Integration")]
        public void Filter_ByUrl_ShouldReturnMatchingEntries()
        {
            _mainVm.SearchText = "azure.com";
            _mainVm.ApplyFilter();
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));
            Assert.That(_mainVm.FilteredEntries[0].Title, Is.EqualTo("Microsoft Azure"));
        }

        [Test, Category("Integration")]
        public void Filter_ByNotes_ShouldReturnMatchingEntries()
        {
            _mainVm.SearchText = "linux host";
            _mainVm.ApplyFilter();
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));
            Assert.That(_mainVm.FilteredEntries[0].Title, Is.EqualTo("Server SSH Key"));
        }

        [Test, Category("Integration")]
        public void ShowSubgroupEntries_DefaultsToTrue_ShouldIncludeNestedEntries_ExcludingRecycleBin()
        {
            var subGroup = new PwGroup(true, true, "Work Subgroup", PwIcon.Folder);
            var subEntry = TestPluginHost.CreateTestEntry("Nested Entry", "worker", "pass");
            subGroup.AddEntry(subEntry, true);
            _host.Database.RootGroup.AddGroup(subGroup, true);

            // Configure Recycle Bin
            var recycleBin = new PwGroup(true, true, "Recycle Bin", PwIcon.TrashBin);
            var trashEntry = TestPluginHost.CreateTestEntry("Trashed Entry", "trash", "pass");
            recycleBin.AddEntry(trashEntry, true);
            _host.Database.RootGroup.AddGroup(recycleBin, true);
            _host.Database.RecycleBinUuid = recycleBin.Uuid;
            _host.Database.RecycleBinEnabled = true;

            _mainVm.LoadGroups();

            // Default: ShowSubgroupEntries is true, so nested entry is included (4 + 1 = 5), excluding trashEntry
            Assert.That(_mainVm.ShowSubgroupEntries, Is.True);
            Assert.That(_mainVm.Entries.Count, Is.EqualTo(5));
            Assert.That(_mainVm.Entries.Any(e => e.Title == "Nested Entry"), Is.True);
            Assert.That(_mainVm.Entries.Any(e => e.Title == "Trashed Entry"), Is.False);

            // When disabled, only direct entries are returned (4)
            _mainVm.ShowSubgroupEntries = false;
            Assert.That(_mainVm.Entries.Count, Is.EqualTo(4));
            Assert.That(_mainVm.Entries.Any(e => e.Title == "Nested Entry"), Is.False);
        }

        [Test, Category("Integration")]
        public void Search_DefaultsToDatabaseWide_WhenChildGroupSelected()
        {
            var childGroup = new PwGroup(true, true, "Child Group", PwIcon.Folder);
            var childEntry = TestPluginHost.CreateTestEntry("Child Special Token", "user", "pass");
            childGroup.AddEntry(childEntry, true);
            _host.Database.RootGroup.AddGroup(childGroup, true);

            _mainVm.LoadGroups();
            var childVm = _mainVm.RootGroups[0].Children.First(g => g.Model.Uuid.Equals(childGroup.Uuid));
            _mainVm.SelectedGroup = childVm;

            // Direct + subgroup entries in Child Group is 1
            Assert.That(_mainVm.Entries.Count, Is.EqualTo(1));

            // Searching for an entry outside Child Group (e.g., "GitHub") should still match via database-wide search
            _mainVm.SearchText = "GitHub";
            _mainVm.ApplyFilter();
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));
            Assert.That(_mainVm.FilteredEntries[0].Title, Is.EqualTo("GitHub Account"));
        }

        [Test, Category("Integration")]
        public void Search_UsesKeePassNativeEngine_IncludingTags()
        {
            // 标签检索是 KeePassLib 原生 SearchParameters 的能力（原生快速搜索启用 SearchInTags），
            // 旧的 Title/UserName/Url/Notes 手写 Contains 引擎无法命中标签
            var tagged = TestPluginHost.CreateTestEntry("Tagged Entry", "user", "pass");
            tagged.Tags.Add("work");
            _host.Database.RootGroup.AddEntry(tagged, true);

            _mainVm.SearchText = "work";
            _mainVm.ApplyFilter();

            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));
            Assert.That(_mainVm.FilteredEntries[0].Title, Is.EqualTo("Tagged Entry"));
        }

        [Test, Category("Integration")]
        public void Search_UsesKeePassNativeEngine_RegexSyntax()
        {
            // //正则// 语法为 KeePass 2.61 原生快速搜索内建能力
            _mainVm.SearchText = "//Git.*//";
            _mainVm.ApplyFilter();

            Assert.That(_mainVm.IsSearchResultsActive, Is.True);
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));
            Assert.That(_mainVm.FilteredEntries[0].Title, Is.EqualTo("GitHub Account"));
        }

        [Test, Category("Integration")]
        public void GroupSelection_AfterSearch_ExitsSearchResultsView_AndRetainsSearchText()
        {
            var group = new PwGroup(true, true, "Target Group", PwIcon.Folder);
            group.AddEntry(TestPluginHost.CreateTestEntry("Target Entry A", "a", "p"), true);
            group.AddEntry(TestPluginHost.CreateTestEntry("Target Entry B", "b", "p"), true);
            _host.Database.RootGroup.AddGroup(group, true);
            _mainVm.LoadGroups();

            _mainVm.SearchText = "git";
            _mainVm.SubmitSearch();
            Assert.That(_mainVm.IsSearchResultsActive, Is.True);
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));

            // 模拟用户点击左侧分组
            var targetVm = _mainVm.RootGroups[0].Children.Single(g => g.Name == "Target Group");
            _mainVm.SelectedGroup = targetVm;

            Assert.That(_mainVm.IsSearchResultsActive, Is.False, "分组点击必须退出搜索结果视图（对齐原生 SelectGroup）");
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(2), "中栏必须显示最新点击分组的条目");
            Assert.That(_mainVm.FilteredEntries.All(e => e.Title.StartsWith("Target")), Is.True);
            Assert.That(_mainVm.SearchText, Is.EqualTo("git"), "搜索框文本保留是 KeePass 原生行为（仅锁定/关闭数据库才清空）");
        }

        [Test, Category("Integration")]
        public void ReClickSelectedGroup_ExitsSearchResultsView()
        {
            _mainVm.SearchText = "git";
            _mainVm.SubmitSearch();
            Assert.That(_mainVm.IsSearchResultsActive, Is.True);
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));

            // WPF TreeView 重复点击同一节点不触发 SelectedItemChanged；
            // 视图层以 LoadGroupEntries 模拟原生 OnGroupsNodeMouseClick 的强制刷新
            _mainVm.LoadGroupEntries(_mainVm.SelectedGroup);

            Assert.That(_mainVm.IsSearchResultsActive, Is.False);
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(4), "重复点击当前分组也应回到该分组的全部条目");
        }

        [Test, Category("Integration")]
        public void Search_AfterExitingResults_CanBeReactivated()
        {
            _mainVm.SearchText = "git";
            _mainVm.LoadGroupEntries(_mainVm.SelectedGroup); // 退出搜索结果视图
            Assert.That(_mainVm.IsSearchResultsActive, Is.False);

            // 用户修改搜索词再次输入 → 重新进入搜索结果视图
            _mainVm.SearchText = "azure.com";
            _mainVm.ApplyFilter();

            Assert.That(_mainVm.IsSearchResultsActive, Is.True);
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));
            Assert.That(_mainVm.FilteredEntries[0].Title, Is.EqualTo("Microsoft Azure"));
        }

        [Test, Category("Integration")]
        public void SubmitSearch_AfterGroupExit_RearmsSearchResultsView_WithoutTextChange()
        {
            _mainVm.SearchText = "git";
            _mainVm.SubmitSearch();
            Assert.That(_mainVm.IsSearchResultsActive, Is.True);

            // 点击分组退出搜索结果视图（搜索框文本保留，对齐原生）
            _mainVm.LoadGroupEntries(_mainVm.SelectedGroup);
            Assert.That(_mainVm.IsSearchResultsActive, Is.False);
            Assert.That(_mainVm.SearchText, Is.EqualTo("git"));

            // 回到搜索框直接按 Enter（文本未变化）：必须重新进入搜索结果视图
            _mainVm.SubmitSearch("git");

            Assert.That(_mainVm.IsSearchResultsActive, Is.True,
                "Enter 提交必须无条件重跑搜索（对齐原生 PerformSearchQuick），不应依赖文本变化");
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));
            Assert.That(_mainVm.FilteredEntries[0].Title, Is.EqualTo("GitHub Account"));
        }

        [Test, Category("Integration")]
        public void ClearData_ClearsSearchText_AndSearchState()
        {
            _mainVm.SearchText = "git";
            Assert.That(_mainVm.IsSearchResultsActive, Is.True);

            _mainVm.ClearData();

            Assert.That(_mainVm.SearchText, Is.Empty, "锁定/关闭数据库时清空快速搜索框（对齐原生 CloseDocument）");
            Assert.That(_mainVm.IsSearchResultsActive, Is.False);
        }

        [Test, Category("Integration")]
        public void SubmitSearch_RunsImmediately()
        {
            _mainVm.SearchText = "azure.com";
            _mainVm.SubmitSearch(); // Enter 提交路径：不等待 200ms 防抖

            Assert.That(_mainVm.IsSearchResultsActive, Is.True);
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));
            Assert.That(_mainVm.FilteredEntries[0].Title, Is.EqualTo("Microsoft Azure"));
        }

        [Test, Category("Integration")]
        public void Entries_DefaultOrder_ShouldBeReversedPhysicalOrder()
        {
            Assert.That(_mainVm.Entries.Count, Is.EqualTo(4));
            Assert.That(_mainVm.Entries[0].Title, Is.EqualTo("Server SSH Key"));
            Assert.That(_mainVm.Entries[1].Title, Is.EqualTo("Microsoft Azure"));
            Assert.That(_mainVm.Entries[2].Title, Is.EqualTo("Google Mail"));
            Assert.That(_mainVm.Entries[3].Title, Is.EqualTo("GitHub Account"));
        }

        [Test, Category("Integration")]
        public void SyncCurrentGroupEntries_ExternalAdd_PlacesNewEntryAtTop()
        {
            var e5 = TestPluginHost.CreateTestEntry("Z Newest Entry", "admin", "pass5");
            _host.Database.RootGroup.AddEntry(e5, true);
            _mainVm.RefreshDatabaseState();

            Assert.That(_mainVm.Entries.Count, Is.EqualTo(5));
            Assert.That(_mainVm.Entries[0].Title, Is.EqualTo("Z Newest Entry"));
        }

        public void InlineEdit_PasswordMask_ToggleShouldWorkSafely()
        {
            var entry = _host.Database.RootGroup.Entries.GetAt(0);
            var detailVm = new EntryDetailViewModel(entry, _host, null, () => { });

            detailVm.BeginEdit();
            Assert.That(detailVm.IsEditing, Is.True);
            Assert.That(detailVm.IsEditPasswordRevealed, Is.False, "Initial edit mode must default to masked");

            detailVm.ToggleEditPasswordVisibility();
            Assert.That(detailVm.IsEditPasswordRevealed, Is.True);

            detailVm.CancelEdit();
            Assert.That(detailVm.IsEditing, Is.False);
            Assert.That(detailVm.EditPassword, Is.Empty, "Password must be wiped from memory upon cancel");
            Assert.That(detailVm.IsEditPasswordRevealed, Is.False);
        }
    }
}
