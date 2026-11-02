using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using FluentPass.Common;
using NUnit.Framework;

namespace FluentPass.Tests
{
    [TestFixture, Apartment(ApartmentState.STA), Category("Unit")]
    public class UiMotionTests
    {
        [Test]
        public void IsAllowed_EnabledByApp_RespectsWindowsPreference()
        {
            Assert.That(UiMotion.IsAllowed(true), Is.EqualTo(SystemParameters.ClientAreaAnimation));
            Assert.That(UiMotion.IsAllowed(false), Is.False);
        }

        [Test]
        public void TryApply_EnabledByApp_UsesWpfUiHardwareFallback()
        {
            var element = new Grid();
            bool expected = SystemParameters.ClientAreaAnimation && (RenderCapability.Tier >> 16) >= 1;
            try
            {
                Assert.That(UiMotion.TryApply(element, true), Is.EqualTo(expected));
                Assert.That(element.HasAnimatedProperties, Is.EqualTo(expected));
                if (!expected) Assert.That(element.Opacity, Is.EqualTo(1));
            }
            finally
            {
                UiMotion.Stop(element);
            }
        }

        [Test]
        public void TryApply_DisabledByApp_RemovesPreviousFadeAndRestoresBaseOpacity()
        {
            var element = new Grid { Opacity = 0.8 };
            element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.2, 0.2, Duration.Forever));
            Assert.That(element.HasAnimatedProperties, Is.True);

            Assert.That(UiMotion.TryApply(element, false), Is.False);

            Assert.That(element.HasAnimatedProperties, Is.False);
            Assert.That(element.Opacity, Is.EqualTo(0.8));
        }
    }
}
