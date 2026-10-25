using System;
using System.Collections.Generic;
using System.IO;

namespace FluentPass.Services
{
    /// <summary>
    /// 附件临时文件的登记与清理。
    /// “打开附件”会把条目二进制落盘到临时目录以交给系统默认程序，
    /// 进程退出或数据库切换时尽力删除；被外部程序占用的文件允许残留由系统回收。
    /// </summary>
    public static class TempAttachmentStore
    {
        private static readonly object SyncRoot = new object();
        private static readonly List<string> TrackedFiles = new List<string>();

        /// <summary>把二进制数据写入临时文件并登记。返回完整路径；失败返回 null。</summary>
        public static string? WriteTempFile(string attachmentName, byte[] data)
        {
            if (data == null) return null;

            try
            {
                string safeName = SanitizeFileName(string.IsNullOrWhiteSpace(attachmentName) ? "attachment.bin" : attachmentName);
                string path = Path.Combine(Path.GetTempPath(), $"FluentPass_{Guid.NewGuid():N}_{safeName}");
                File.WriteAllBytes(path, data);
                lock (SyncRoot)
                {
                    TrackedFiles.Add(path);
                }
                return path;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>尽力删除单个已登记临时文件（被占用时静默跳过）。</summary>
        public static void TryDelete(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            lock (SyncRoot)
            {
                TrackedFiles.Remove(path!);
            }

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch { /* 文件被外部程序占用时留给系统临时目录回收 */ }
        }

        /// <summary>进程退出/插件卸载时清理全部登记文件。</summary>
        public static void CleanupAll()
        {
            string[] snapshot;
            lock (SyncRoot)
            {
                snapshot = TrackedFiles.ToArray();
                TrackedFiles.Clear();
            }

            foreach (var path in snapshot)
            {
                try
                {
                    if (File.Exists(path)) File.Delete(path);
                }
                catch { }
            }
        }

        public static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }
    }
}
