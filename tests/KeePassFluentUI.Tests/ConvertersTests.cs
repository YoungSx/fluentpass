using System.Globalization;
using System.Windows;
using KeePassFluentUI.Common;
using NUnit.Framework;

namespace KeePassFluentUI.Tests
{
    [TestFixture]
    public class ConvertersTests
    {
        [Test]
        public void BooleanToVisibilityConverter_Standard_ShouldConvertCorrectly()
        {
            var conv = new BooleanToVisibilityConverter();
            var culture = CultureInfo.InvariantCulture;

            Assert.That(conv.Convert(true, typeof(Visibility), null!, culture), Is.EqualTo(Visibility.Visible));
            Assert.That(conv.Convert(false, typeof(Visibility), null!, culture), Is.EqualTo(Visibility.Collapsed));
            Assert.That(conv.Convert(null!, typeof(Visibility), null!, culture), Is.EqualTo(Visibility.Collapsed));
            Assert.That(conv.Convert(DependencyProperty.UnsetValue, typeof(Visibility), null!, culture), Is.EqualTo(Visibility.Collapsed));
        }

        [Test]
        public void BooleanToVisibilityConverter_InverseParameter_ShouldInvertCorrectly()
        {
            var conv = new BooleanToVisibilityConverter();
            var culture = CultureInfo.InvariantCulture;

            Assert.That(conv.Convert(true, typeof(Visibility), "Inverse", culture), Is.EqualTo(Visibility.Collapsed));
            Assert.That(conv.Convert(false, typeof(Visibility), "Inverse", culture), Is.EqualTo(Visibility.Visible));
            Assert.That(conv.Convert(null!, typeof(Visibility), "Inverse", culture), Is.EqualTo(Visibility.Visible));
            Assert.That(conv.Convert(DependencyProperty.UnsetValue, typeof(Visibility), "Inverse", culture), Is.EqualTo(Visibility.Visible));
        }

        [Test]
        public void BooleanToVisibilityConverter_IntegerCount_ShouldEvaluateGreaterThanZero()
        {
            var conv = new BooleanToVisibilityConverter();
            var culture = CultureInfo.InvariantCulture;

            Assert.That(conv.Convert(5, typeof(Visibility), null!, culture), Is.EqualTo(Visibility.Visible));
            Assert.That(conv.Convert(0, typeof(Visibility), null!, culture), Is.EqualTo(Visibility.Collapsed));
            Assert.That(conv.Convert(0, typeof(Visibility), "Inverse", culture), Is.EqualTo(Visibility.Visible));
            Assert.That(conv.Convert(3, typeof(Visibility), "Inverse", culture), Is.EqualTo(Visibility.Collapsed));
        }
    }
}
