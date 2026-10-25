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

        [Test, Category("Unit")]
        public void BeginEditDraft_CopiesKeyAndPlaintextIntoDraft()
        {
            var secretVal = new ProtectedString(true, "DraftSecret");
            var vm = new CustomFieldItemViewModel("ApiKey", secretVal);

            vm.BeginEditDraft();

            Assert.That(vm.EditKey, Is.EqualTo("ApiKey"));
            Assert.That(vm.EditValue, Is.EqualTo("DraftSecret"));
            Assert.That(vm.IsEditValueRevealed, Is.False);
            Assert.That(vm.IsEditValueMaskedBoxVisible, Is.True, "受保护字段编辑态默认显示掩码输入框");
            Assert.That(vm.IsEditValuePlainBoxVisible, Is.False);
        }

        [Test, Category("Unit")]
        public void ClearEditDraft_WipesPlaintextDraft()
        {
            var vm = new CustomFieldItemViewModel("K", new ProtectedString(true, "Secret"));
            vm.BeginEditDraft();
            vm.ToggleEditValueVisibility();
            Assert.That(vm.EditValue, Is.EqualTo("Secret"));
            Assert.That(vm.IsEditValueRevealed, Is.True);

            vm.ClearEditDraft();

            Assert.That(vm.EditValue, Is.Empty, "退出编辑必须清空明文草稿（内存零残留）");
            Assert.That(vm.IsEditValueRevealed, Is.False);
        }

        [Test, Category("Unit")]
        public void ToggleEditProtection_SwitchesMaskingDefaults()
        {
            var vm = new CustomFieldItemViewModel("K", new ProtectedString(false, "plain"));
            vm.BeginEditDraft();

            Assert.That(vm.IsEditValuePlainBoxVisible, Is.True);

            vm.ToggleEditProtection();
            Assert.That(vm.IsProtected, Is.True);
            Assert.That(vm.IsEditValueMaskedBoxVisible, Is.True);
            Assert.That(vm.IsEditValueRevealed, Is.False, "切换保护后必须复位为掩码呈现");

            vm.ToggleEditProtection();
            Assert.That(vm.IsProtected, Is.False);
            Assert.That(vm.IsEditValuePlainBoxVisible, Is.True);
        }

        [Test, Category("Unit")]
        public void IsNew_IsExclusiveProperty()
        {
            var existing = new CustomFieldItemViewModel("K", new ProtectedString(false, "v"));
            var added = new CustomFieldItemViewModel("K2", new ProtectedString(false, string.Empty), isNew: true);

            Assert.That(existing.IsNew, Is.False);
            Assert.That(added.IsNew, Is.True);
        }
    }
}
