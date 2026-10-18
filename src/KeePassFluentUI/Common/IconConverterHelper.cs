using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KeePass.Forms;
using KeePassLib;

namespace KeePassFluentUI.Common
{
    /// <summary>
    /// 提供 KeePass 内置图标 (ClientIcons) 与自定义图标 (PwCustomIcon) 到 WPF ImageSource 的无缝转换与缓存，
    /// 纯托管流式转换，杜绝 Win32 GDI 句柄泄漏。
    /// </summary>
    public static class IconConverterHelper
    {
        private static readonly ConcurrentDictionary<int, ImageSource> _standardIconCache = new ConcurrentDictionary<int, ImageSource>();
        private static readonly ConcurrentDictionary<string, ImageSource> _customIconCache = new ConcurrentDictionary<string, ImageSource>();

        public static ImageSource? GetIconSource(MainForm? mainForm, PwDatabase? database, PwIcon iconId, PwUuid? customIconUuid)
        {
            // 1. 优先尝试加载数据库自定义图标
            if (database != null && customIconUuid != null && !customIconUuid.Equals(PwUuid.Zero))
            {
                string customKey = customIconUuid.ToHexString();
                if (_customIconCache.TryGetValue(customKey, out var cachedCustom))
                {
                    return cachedCustom;
                }

                try
                {
                    var customImg = database.GetCustomIcon(customIconUuid, 16, 16);
                    if (customImg != null)
                    {
                        var imageSource = ConvertImageToBitmapSource(customImg);
                        if (imageSource != null)
                        {
                            _customIconCache[customKey] = imageSource;
                            return imageSource;
                        }
                    }
                }
                catch
                {
                    // 自定义图标解析失败时平滑降级到内置图标
                }
            }

            // 2. 加载 KeePass 内置标准图标
            int index = (int)iconId;
            if (_standardIconCache.TryGetValue(index, out var cachedStandard))
            {
                return cachedStandard;
            }

            if (mainForm?.ClientIcons != null && index >= 0 && index < mainForm.ClientIcons.Images.Count)
            {
                try
                {
                    var img = mainForm.ClientIcons.Images[index];
                    if (img != null)
                    {
                        var imageSource = ConvertImageToBitmapSource(img);
                        if (imageSource != null)
                        {
                            _standardIconCache[index] = imageSource;
                            return imageSource;
                        }
                    }
                }
                catch
                {
                    // 忽略加载异常
                }
            }

            return null;
        }

        public static BitmapSource? ConvertImageToBitmapSource(Image image)
        {
            if (image == null) return null;

            using (var ms = new MemoryStream())
            {
                // 以 PNG 格式导出确保透明通道保留
                image.Save(ms, ImageFormat.Png);
                ms.Position = 0;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze(); // 冻结以支持跨线程安全使用

                return bitmap;
            }
        }

        public static BitmapSource? LoadFromBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;

            using (var ms = new MemoryStream(bytes))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();

                return bitmap;
            }
        }

        public static void ClearCustomCache()
        {
            _customIconCache.Clear();
        }
    }
}
