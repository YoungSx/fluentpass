using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using FluentPass.Common;
using NUnit.Framework;

namespace FluentPass.Tests
{
    [TestFixture, Apartment(System.Threading.ApartmentState.STA), Category("Unit")]
    public class IconConverterHelperUnitTests
    {
        [Test, Category("Unit")]
        public void ConvertImageToBitmapSource_NullImage_ReturnsNull()
        {
            var res = IconConverterHelper.ConvertImageToBitmapSource(null!);
            Assert.That(res, Is.Null);
        }

        [Test, Category("Unit")]
        public void ConvertImageToBitmapSource_ValidImage_ReturnsFrozenBitmapSource()
        {
            using (var bmp = new Bitmap(16, 16))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Blue);
                }

                var source = IconConverterHelper.ConvertImageToBitmapSource(bmp);
                Assert.That(source, Is.Not.Null);
                Assert.That(source!.PixelWidth, Is.EqualTo(16));
                Assert.That(source.PixelHeight, Is.EqualTo(16));
                Assert.That(source.IsFrozen, Is.True);
            }
        }

        [Test, Category("Unit")]
        public void LoadFromBytes_NullOrEmptyBytes_ReturnsNull()
        {
            Assert.That(IconConverterHelper.LoadFromBytes(null!), Is.Null);
            Assert.That(IconConverterHelper.LoadFromBytes(new byte[0]), Is.Null);
        }

        [Test, Category("Unit")]
        public void LoadFromBytes_ValidPngBytes_ReturnsFrozenBitmapSource()
        {
            byte[] pngBytes;
            using (var bmp = new Bitmap(16, 16))
            {
                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Png);
                    pngBytes = ms.ToArray();
                }
            }

            var source = IconConverterHelper.LoadFromBytes(pngBytes);
            Assert.That(source, Is.Not.Null);
            Assert.That(source!.PixelWidth, Is.EqualTo(16));
            Assert.That(source.PixelHeight, Is.EqualTo(16));
            Assert.That(source.IsFrozen, Is.True);
        }

        [Test, Category("Unit")]
        public void ClearCustomCache_ClearsWithoutException()
        {
            Assert.DoesNotThrow(() => IconConverterHelper.ClearCustomCache());
        }
    }
}
