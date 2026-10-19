using System;
using System.Linq;
using System.Reflection;
using FluentPass.Tests.Mocks;
using FluentPass.ViewModels;
using KeePassLib;
using KeePassLib.Security;
using NUnit.Framework;

namespace FluentPass.Tests
{
    [TestFixture, Apartment(System.Threading.ApartmentState.STA), Category("Integration")]
    public class EntrySecurityTests
    {
        [SetUp]
        public void Setup()
        {
            Common.MessageService.EnableDialogs = false;
        }

        [Test, Category("Integration")]
        public void EntryItemViewModel_ShouldNeverExposeDecryptedPasswordInProperties()
        {
            // 规范 3: 内存安全零妥协 - 列表项绝对不解密密码
            const string secretPassword = "SuperSecretPlainTextPassword123!";
            var entry = TestPluginHost.CreateTestEntry("Test Account", "user@domain.com", secretPassword);

            var vm = new EntryItemViewModel(entry, null, null);

            // 1. 验证公开属性中绝无明文密码
            var stringProperties = typeof(EntryItemViewModel)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == typeof(string));

            foreach (var prop in stringProperties)
            {
                var val = prop.GetValue(vm) as string;
                Assert.That(val, Does.Not.Contain(secretPassword), 
                    $"Property '{prop.Name}' exposed the plain password string on managed heap!");
            }

            // 2. 验证私有字段中绝无明文密码字符串
            var stringFields = typeof(EntryItemViewModel)
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.FieldType == typeof(string));

            foreach (var field in stringFields)
            {
                var val = field.GetValue(vm) as string;
                Assert.That(val, Does.Not.Contain(secretPassword),
                    $"Field '{field.Name}' retained the plain password string on managed heap!");
            }

            Assert.That(vm.HasPassword, Is.True);
        }

        [Test, Category("Integration")]
        public void EntryDetailViewModel_ShouldOnlyRevealPasswordOnDemand_AndClearOnHide()
        {
            const string secretPassword = "TopSecretPassword456$";
            var entry = TestPluginHost.CreateTestEntry("Bank Account", "admin", secretPassword);
            var host = new TestPluginHost();

            var detailVm = new EntryDetailViewModel(entry, host, null, () => { });

            // 初始状态：明文为空，展示固定掩码
            Assert.That(detailVm.RevealedPassword, Is.Null);
            Assert.That(detailVm.IsPasswordRevealed, Is.False);
            Assert.That(detailVm.PasswordMasked, Is.EqualTo("••••••••••••"));

            // 用户触发查看
            detailVm.TogglePasswordVisibility();
            Assert.That(detailVm.IsPasswordRevealed, Is.True);
            Assert.That(detailVm.RevealedPassword, Is.EqualTo(secretPassword));
            Assert.That(detailVm.PasswordRevealSecondsRemaining, Is.EqualTo(10));

            // 用户手动隐藏或超时隐藏
            detailVm.HidePassword();
            Assert.That(detailVm.IsPasswordRevealed, Is.False);
            Assert.That(detailVm.RevealedPassword, Is.Null, "Revealed password must be cleared to null upon hide!");
        }

        [Test, Category("Integration")]
        public void CustomFieldItemViewModel_ShouldProtectSensitiveFieldsByDefault()
        {
            const string secretPin = "987654";
            var protectedStr = new ProtectedString(true, secretPin);
            var customFieldVm = new CustomFieldItemViewModel("SecurityPIN", protectedStr);

            Assert.That(customFieldVm.IsProtected, Is.True);
            Assert.That(customFieldVm.IsRevealed, Is.False);
            Assert.That(customFieldVm.DisplayValue, Is.EqualTo("••••••••••••"));

            // 展开
            customFieldVm.ToggleReveal();
            Assert.That(customFieldVm.IsRevealed, Is.True);
            Assert.That(customFieldVm.DisplayValue, Is.EqualTo(secretPin));

            // 隐藏
            customFieldVm.HideValue();
            Assert.That(customFieldVm.IsRevealed, Is.False);
            Assert.That(customFieldVm.DisplayValue, Is.EqualTo("••••••••••••"));
        }
    }
}
