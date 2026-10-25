using System;
using System.IO;
using FluentPass.Services;
using FluentPass.Tests.Mocks;
using FluentPass.ViewModels;
using KeePassLib;
using KeePassLib.Security;
using NUnit.Framework;

namespace FluentPass.Tests
{
    /// <summary>
    /// 附件管理：添加（备份+防重名）、删除（确认+备份）、另存（字节一致+内存清零）、
    /// 打开（临时文件登记与清理）。确认对话框经 EnableDialogs=false 关闭以支持无头测试。
    /// </summary>
    [TestFixture, Apartment(System.Threading.ApartmentState.STA), Category("Integration")]
    public class AttachmentManagementTests
    {
        private TestPluginHost _host = null!;
        private EntryDetailViewModel _vm = null!;
        private PwEntry _entry = null!;
        private string _tempDir = null!;

        [SetUp]
        public void Setup()
        {
            Common.MessageService.EnableDialogs = false;
            _host = new TestPluginHost();
            _entry = TestPluginHost.CreateTestEntry("WithAttach", "u", "p", "", "");
            _host.Database.RootGroup.AddEntry(_entry, true);
            _vm = new EntryDetailViewModel(_entry, _host, null, () => { });
            _tempDir = Path.Combine(Path.GetTempPath(), "fluentpass-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            TempAttachmentStore.CleanupAll();
            try { Directory.Delete(_tempDir, true); } catch { }
        }

        // ===== 添加 =====

        [Test]
        public void AddAttachmentData_CreatesBackup_AndStoresProtectedBinary()
        {
            var data = new byte[] { 1, 2, 3, 4, 5 };

            var finalName = _vm.AddAttachmentData("report.pdf", data);

            Assert.That(finalName, Is.EqualTo("report.pdf"));
            Assert.That(_entry.Binaries.Get("report.pdf"), Is.Not.Null);
            Assert.That(_entry.History.UCount, Is.EqualTo(1u), "添加前必须 CreateBackup");
            Assert.That(_host.Database.Modified, Is.True);
            Assert.That(_vm.Attachments.Count, Is.EqualTo(1));
        }

        [Test]
        public void AddAttachmentData_NameCollision_AutoSuffixes()
        {
            _vm.AddAttachmentData("file.txt", new byte[] { 1 });
            var second = _vm.AddAttachmentData("file.txt", new byte[] { 2 });

            Assert.That(second, Is.EqualTo("file (2).txt"));
            Assert.That(_entry.Binaries.Get("file.txt"), Is.Not.Null);
            Assert.That(_entry.Binaries.Get("file (2).txt"), Is.Not.Null);
        }

        [Test]
        public void AddAttachmentData_TripleCollision_SuffixIncrements()
        {
            _vm.AddAttachmentData("a.bin", new byte[] { 1 });
            _vm.AddAttachmentData("a.bin", new byte[] { 2 });
            var third = _vm.AddAttachmentData("a.bin", new byte[] { 3 });
            Assert.That(third, Is.EqualTo("a (3).bin"));
        }

        [Test]
        public void AddAttachmentData_EmptyData_Throws()
        {
            Assert.That(() => _vm.AddAttachmentData("empty.bin", Array.Empty<byte>()), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void AddAttachmentData_EmptyName_GetsFallbackName()
        {
            var name = _vm.AddAttachmentData("", new byte[] { 9 });
            Assert.That(name, Is.Not.Empty);
            Assert.That(_entry.Binaries.Get(name), Is.Not.Null);
        }

        // ===== 删除 =====

        [Test]
        public void DeleteAttachment_RemovesBinary_AndCreatesBackup()
        {
            _vm.AddAttachmentData("doomed.bin", new byte[] { 1, 2 });
            uint historyBefore = _entry.History.UCount;

            _vm.DeleteAttachmentCommand.Execute(_vm.Attachments[0]);

            Assert.That(_entry.Binaries.Get("doomed.bin"), Is.Null);
            Assert.That(_entry.History.UCount, Is.GreaterThan(historyBefore));
            Assert.That(_host.Database.Modified, Is.True);
            Assert.That(_vm.Attachments, Is.Empty);
        }

        [Test]
        public void DeleteAttachment_UnknownName_NoOp()
        {
            uint historyBefore = _entry.History.UCount;
            _vm.DeleteAttachment(new AttachmentItemViewModel("ghost.bin", 10));
            Assert.That(_entry.History.UCount, Is.EqualTo(historyBefore));
        }

        [Test]
        public void DeleteAttachment_Null_NoOp()
        {
            Assert.That(() => _vm.DeleteAttachment(null), Throws.Nothing);
        }

        // ===== 另存 =====

        [Test]
        public void SaveAttachmentData_WritesIdenticalBytes()
        {
            var payload = new byte[] { 10, 20, 30, 40, 50, 60 };
            _vm.AddAttachmentData("saved.bin", payload);
            var binary = _entry.Binaries.Get("saved.bin")!;

            string target = Path.Combine(_tempDir, "saved.bin");
            _vm.SaveAttachmentData(binary, target);

            Assert.That(File.Exists(target), Is.True);
            CollectionAssert.AreEqual(payload, File.ReadAllBytes(target));
        }

        [Test]
        public void SaveAttachmentData_UnprotectedBinary_AlsoWorks()
        {
            var binary = new ProtectedBinary(false, new byte[] { 7, 8 });
            string target = Path.Combine(_tempDir, "plain.bin");
            _vm.SaveAttachmentData(binary, target);
            CollectionAssert.AreEqual(new byte[] { 7, 8 }, File.ReadAllBytes(target));
        }

        // ===== 打开（临时文件） =====

        [Test]
        public void OpenAttachment_WritesRegisteredTempFile()
        {
            var payload = new byte[] { 3, 1, 4, 1, 5 };
            _vm.AddAttachmentData("viewme.txt", payload);

            // 直接走提取路径（Process.Start 部分不纳入无头测试）
            var binary = _entry.Binaries.Get("viewme.txt")!;
            var data = binary.ReadData();
            string? tempPath = TempAttachmentStore.WriteTempFile("viewme.txt", data);

            Assert.That(tempPath, Is.Not.Null);
            Assert.That(File.Exists(tempPath), Is.True);
            CollectionAssert.AreEqual(payload, File.ReadAllBytes(tempPath!));
            Assert.That(Path.GetFileName(tempPath!), Does.Contain("viewme.txt"));
        }

        [Test]
        public void TempAttachmentStore_SanitizesInvalidFileNameCharacters()
        {
            string safe = TempAttachmentStore.SanitizeFileName("bad<>:\"/\\|?*name.txt");
            Assert.That(safe.IndexOfAny(Path.GetInvalidFileNameChars()), Is.EqualTo(-1));
            Assert.That(safe, Does.EndWith("name.txt"));
        }

        [Test]
        public void TempAttachmentStore_TryDelete_RemovesFile()
        {
            string path = Path.Combine(_tempDir, "tracked.tmp");
            File.WriteAllBytes(path, new byte[] { 1 });
            TempAttachmentStore.WriteTempFile("tracked.tmp", new byte[] { 1 });

            TempAttachmentStore.TryDelete(path);
            Assert.That(File.Exists(path), Is.False);
        }

        [Test]
        public void TempAttachmentStore_TryDelete_MissingOrOccupiedFile_NoThrow()
        {
            Assert.That(() => TempAttachmentStore.TryDelete(null), Throws.Nothing);
            Assert.That(() => TempAttachmentStore.TryDelete(""), Throws.Nothing);
            Assert.That(() => TempAttachmentStore.TryDelete(Path.Combine(_tempDir, "never-existed.tmp")), Throws.Nothing);
        }

        [Test]
        public void TempAttachmentStore_CleanupAll_DeletesAllTracked()
        {
            var p1 = TempAttachmentStore.WriteTempFile("c1.tmp", new byte[] { 1 });
            var p2 = TempAttachmentStore.WriteTempFile("c2.tmp", new byte[] { 2 });
            Assert.That(p1, Is.Not.Null);
            Assert.That(p2, Is.Not.Null);

            TempAttachmentStore.CleanupAll();

            Assert.That(File.Exists(p1!), Is.False);
            Assert.That(File.Exists(p2!), Is.False);
        }

        [Test]
        public void TempAttachmentStore_WriteTempFile_NullData_ReturnsNull()
        {
            Assert.That(TempAttachmentStore.WriteTempFile("x.bin", null!), Is.Null);
        }

        [Test]
        public void Detail_ClearSensitiveData_AttemptsTempCleanup()
        {
            _vm.AddAttachmentData("temp-open.bin", new byte[] { 1 });
            // 模拟“打开”登记（不经 Process.Start）
            var binary = _entry.Binaries.Get("temp-open.bin")!;
            var data = binary.ReadData();
            string? tempPath = TempAttachmentStore.WriteTempFile("temp-open.bin", data);
            var field = typeof(EntryDetailViewModel).GetField("_openAttachmentTempPaths", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            (field!.GetValue(_vm) as System.Collections.Generic.List<string>)!.Add(tempPath!);

            _vm.ClearSensitiveData();

            Assert.That(File.Exists(tempPath!), Is.False, "条目切换时应清理该条目登记的临时附件");
        }
    }
}
