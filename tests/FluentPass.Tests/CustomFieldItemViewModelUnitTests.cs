using FluentPass.ViewModels;
using KeePassLib.Security;
using NUnit.Framework;

namespace FluentPass.Tests
{
    [TestFixture, Category("Unit")]
    public class CustomFieldItemViewModelUnitTests
    {
        [Test, Category("Unit")]
        public void Constructor_WithPlainString_SetsDisplayValueDirectly()
        {
            var plainVal = new ProtectedString(false, "VisibleValue123");
            var vm = new CustomFieldItemViewModel("ApiKey", plainVal);

            Assert.That(vm.Key, Is.EqualTo("ApiKey"));
            Assert.That(vm.IsProtected, Is.False);
            Assert.That(vm.IsRevealed, Is.False);
            Assert.That(vm.DisplayValue, Is.EqualTo("VisibleValue123"));
        }

        [Test, Category("Unit")]
        public void Constructor_WithProtectedString_MasksValueByDefault()
        {
            var secretVal = new ProtectedString(true, "SuperSecretToken");
            var vm = new CustomFieldItemViewModel("SecretKey", secretVal);

            Assert.That(vm.Key, Is.EqualTo("SecretKey"));
            Assert.That(vm.IsProtected, Is.True);
            Assert.That(vm.IsRevealed, Is.False);
            Assert.That(vm.DisplayValue, Is.EqualTo("••••••••••••"));
        }

        [Test, Category("Unit")]
        public void ToggleReveal_WhenProtected_TogglesBetweenMaskAndSecret()
        {
            var secretVal = new ProtectedString(true, "MyPINCode");
            var vm = new CustomFieldItemViewModel("PIN", secretVal);

            Assert.That(vm.IsRevealed, Is.False);
            Assert.That(vm.DisplayValue, Is.EqualTo("••••••••••••"));

            vm.ToggleReveal();
            Assert.That(vm.IsRevealed, Is.True);
            Assert.That(vm.DisplayValue, Is.EqualTo("MyPINCode"));

            vm.ToggleReveal();
            Assert.That(vm.IsRevealed, Is.False);
            Assert.That(vm.DisplayValue, Is.EqualTo("••••••••••••"));
        }

        [Test, Category("Unit")]
        public void HideValue_WhenRevealed_ResetsToMasked()
        {
            var secretVal = new ProtectedString(true, "Confidential");
            var vm = new CustomFieldItemViewModel("ConfField", secretVal);

            vm.ToggleReveal();
            Assert.That(vm.IsRevealed, Is.True);

            vm.HideValue();
            Assert.That(vm.IsRevealed, Is.False);
            Assert.That(vm.DisplayValue, Is.EqualTo("••••••••••••"));
        }

        [Test, Category("Unit")]
        public void ToggleReveal_WhenNotProtected_KeepsOriginalValue()
        {
            var plainVal = new ProtectedString(false, "StaticText");
            var vm = new CustomFieldItemViewModel("Normal", plainVal);

            vm.ToggleReveal();
            Assert.That(vm.IsRevealed, Is.False);
            Assert.That(vm.DisplayValue, Is.EqualTo("StaticText"));
        }
    }
}
