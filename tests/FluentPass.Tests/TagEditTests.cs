using System;
using System.Linq;
using FluentPass.Tests.Mocks;
using FluentPass.ViewModels;
using KeePassLib;
using NUnit.Framework;

namespace FluentPass.Tests
{
    /// <summary>
    /// 标签体系：输入规范化（官方 StrUtil.StringToTags 语义 + 去空去重）、
    /// 详情查看/编辑双模式数据流、列表项标签徽标数据。
    /// </summary>
    [TestFixture, Apartment(System.Threading.ApartmentState.STA), Category("Integration")]
    public class TagEditTests
    {
        private TestPluginHost _host = null!;

        [SetUp]
        public void Setup()
        {
            Common.MessageService.EnableDialogs = false;
            _host = new TestPluginHost();
        }

        // ===== NormalizeTags（静态规范化） =====

        [Test]
        public void NormalizeTags_CommaAndSemicolonSeparators()
        {
            var tags = EntryDetailViewModel.NormalizeTags("work, finance; personal");
            CollectionAssert.AreEquivalent(new[] { "work", "finance", "personal" }, tags);
        }

        [Test]
        public void NormalizeTags_RemovesEmptyAndDuplicates()
        {
            var tags = EntryDetailViewModel.NormalizeTags("a, ,b;;a,A");
            // 语义断言：去空、去重（大小写不敏感）后恰为 {a, b}。
            // 保留哪个大小写由官方 StrUtil.StringToTags 决定（2.57.1 自行去重且保留后位、
            // 2.61.1 原样返回由本层去重），故不钉死大小写，保证跨宿主版本可移植。
            Assert.That(tags.Count, Is.EqualTo(2));
            Assert.That(tags.Any(t => t.Equals("a", StringComparison.OrdinalIgnoreCase)), Is.True);
            Assert.That(tags.Any(t => t.Equals("b", StringComparison.OrdinalIgnoreCase)), Is.True);
        }

        [Test]
        public void NormalizeTags_NullOrEmpty_ReturnsEmpty()
        {
            Assert.That(EntryDetailViewModel.NormalizeTags(null), Is.Empty);
            Assert.That(EntryDetailViewModel.NormalizeTags("   "), Is.Empty);
            Assert.That(EntryDetailViewModel.NormalizeTags(",,;;"), Is.Empty);
        }

        [Test]
        public void NormalizeTags_TrimsWhitespace()
        {
            var tags = EntryDetailViewModel.NormalizeTags("  alpha  ,  beta  ");
            CollectionAssert.AreEquivalent(new[] { "alpha", "beta" }, tags);
        }

        // ===== 详情 VM 数据流 =====

        private EntryDetailViewModel CreateDetail(out PwEntry entry, params string[] initialTags)
        {
            entry = TestPluginHost.CreateTestEntry("Tagged", "u", "p", "", "");
            foreach (var t in initialTags)
            {
                entry.Tags.Add(t);
            }
            _host.Database.RootGroup.AddEntry(entry, true);
            return new EntryDetailViewModel(entry, _host, null, () => { });
        }

        [Test]
        public void Detail_ShowsExistingTags_InViewMode()
        {
            var vm = CreateDetail(out _, "work", "bank");
            Assert.That(vm.HasTags, Is.True);
            CollectionAssert.AreEquivalent(new[] { "work", "bank" }, vm.Tags.ToArray());
            Assert.That(vm.IsTagsSectionVisible, Is.True);
        }

        [Test]
        public void Detail_NoTags_HiddenInViewMode_VisibleInEditMode()
        {
            var vm = CreateDetail(out _);
            Assert.That(vm.HasTags, Is.False);
            Assert.That(vm.IsTagsSectionVisible, Is.False);

            vm.BeginEdit();
            Assert.That(vm.IsTagsSectionVisible, Is.True, "编辑模式必须暴露标签编辑入口");
        }

        [Test]
        public void Detail_BeginEdit_LoadsTagsIntoDraft()
        {
            var vm = CreateDetail(out _, "one", "two");
            vm.BeginEdit();
            Assert.That(vm.EditTagsText, Does.Contain("one"));
            Assert.That(vm.EditTagsText, Does.Contain("two"));
        }

        [Test]
        public void SaveEdit_WritesNormalizedTags_AndCreatesBackup()
        {
            var vm = CreateDetail(out var entry, "old");
            vm.BeginEdit();
            vm.EditTagsText = "new1, new2,new1";

            vm.SaveEdit();

            Assert.That(entry.Tags.Count, Is.EqualTo(2));
            CollectionAssert.AreEquivalent(new[] { "new1", "new2" }, entry.Tags.ToArray());
            Assert.That(entry.History.UCount, Is.EqualTo(1u), "保存前必须 CreateBackup（铁律 3）");
            Assert.That(_host.Database.Modified, Is.True);
        }

        [Test]
        public void SaveEdit_EmptyTagsText_RemovesAllTags()
        {
            var vm = CreateDetail(out var entry, "stale1", "stale2");
            vm.BeginEdit();
            vm.EditTagsText = "";

            vm.SaveEdit();

            Assert.That(entry.Tags, Is.Empty);
        }

        [Test]
        public void SaveEdit_CancelEdit_DoesNotTouchModelTags()
        {
            var vm = CreateDetail(out var entry, "keep");
            vm.BeginEdit();
            vm.EditTagsText = "changed";
            vm.CancelEdit();

            Assert.That(entry.Tags.Count, Is.EqualTo(1));
            Assert.That(entry.Tags[0], Is.EqualTo("keep"));
        }

        // ===== 列表项徽标数据 =====

        [Test]
        public void EntryItemViewModel_ExposesTagsForBadges()
        {
            var entry = TestPluginHost.CreateTestEntry("Badges", "u", "p", "", "");
            entry.Tags.Add("prod");
            entry.Tags.Add("Prod"); // 大小写重复应去重
            entry.Tags.Add(" ");

            var vm = new EntryItemViewModel(entry, null, _host.Database);
            Assert.That(vm.HasTags, Is.True);
            CollectionAssert.AllItemsAreUnique(vm.Tags.ToArray());
            Assert.That(vm.Tags.Contains("prod"), Is.True);
            Assert.That(vm.Tags.Contains(" "), Is.False);
        }

        [Test]
        public void EntryItemViewModel_NoTags_HasTagsFalse()
        {
            var entry = TestPluginHost.CreateTestEntry("NoBadges", "u", "p", "", "");
            var vm = new EntryItemViewModel(entry, null, _host.Database);
            Assert.That(vm.HasTags, Is.False);
            Assert.That(vm.Tags, Is.Empty);
        }
    }
}
