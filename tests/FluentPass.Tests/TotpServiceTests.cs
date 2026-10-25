using System.Text.RegularExpressions;
using FluentPass.Services;
using FluentPass.Tests.Mocks;
using FluentPass.ViewModels;
using KeePassLib;
using KeePassLib.Security;
using NUnit.Framework;

namespace FluentPass.Tests
{
    /// <summary>
    /// TOTP 能力验证：种子字段检测（不解密）、宿主 SprEngine {TIMEOTP} 编译、
    /// 周期解析与详情面板倒计时联动。全部复用官方实现，测试断言输出契约。
    /// </summary>
    [TestFixture, Apartment(System.Threading.ApartmentState.STA), Category("Integration")]
    public class TotpServiceTests
    {
        private static PwEntry CreateTotpEntry(string secretField, string secretValue, string? period = null)
        {
            var entry = TestPluginHost.CreateTestEntry("Totp Entry", "user", "pass", "", "");
            entry.Strings.Set(secretField, new ProtectedString(true, secretValue));
            if (period != null)
            {
                entry.Strings.Set("TimeOtp-Period", new ProtectedString(false, period));
            }
            return entry;
        }

        [Test]
        public void HasTotpSeed_NoSeed_ReturnsFalse()
        {
            var entry = TestPluginHost.CreateTestEntry("Plain", "u", "p", "", "");
            Assert.That(TotpService.HasTotpSeed(entry), Is.False);
            Assert.That(TotpService.HasTotpSeed(null), Is.False);
        }

        [TestCase("TimeOtp-Secret-Base32")]
        [TestCase("TimeOtp-Secret-Hex")]
        [TestCase("TimeOtp-Secret-Base64")]
        public void HasTotpSeed_KnownSeedFields_ReturnsTrue(string fieldName)
        {
            var entry = CreateTotpEntry(fieldName, "JBSWY3DPEHPK3PXP");
            Assert.That(TotpService.HasTotpSeed(entry), Is.True);
        }

        [Test]
        public void HasTotpSeed_HotpSeedOnly_ReturnsFalse()
        {
            // HOTP（HmacOtp-*）为计数型，不属于 {TIMEOTP} 范畴
            var entry = CreateTotpEntry("HmacOtp-Secret-Hex", "ABCD");
            Assert.That(TotpService.HasTotpSeed(entry), Is.False);
        }

        [Test]
        public void Compile_Base32Seed_ProducesSixDigitCode()
        {
            var host = new TestPluginHost();
            var entry = CreateTotpEntry("TimeOtp-Secret-Base32", "JBSWY3DPEHPK3PXP");
            host.Database.RootGroup.AddEntry(entry, true);

            var code = TotpService.CompileCurrentCode(entry, host.Database);
            Assert.That(code, Is.Not.Null);
            Assert.That(code, Does.Match(@"^\d{6}$"));
        }

        [Test]
        public void Compile_SameWindow_CodeIsStable()
        {
            var host = new TestPluginHost();
            var entry = CreateTotpEntry("TimeOtp-Secret-Base32", "JBSWY3DPEHPK3PXP");
            host.Database.RootGroup.AddEntry(entry, true);

            var first = TotpService.CompileCurrentCode(entry, host.Database);
            var second = TotpService.CompileCurrentCode(entry, host.Database);
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.EqualTo(first), "同一时间窗内两次编译应产出相同验证码");
        }

        [Test]
        public void Compile_InvalidHexSeed_ReturnsNullOrValidFormat_NoCrash()
        {
            var host = new TestPluginHost();
            var entry = CreateTotpEntry("TimeOtp-Secret-Hex", "ZZZZ-not-hex!");
            host.Database.RootGroup.AddEntry(entry, true);

            var code = TotpService.CompileCurrentCode(entry, host.Database);
            // 官方引擎对非法种子的容错：返回 null（占位符未替换/为空）或合法格式
            Assert.That(code == null || Regex.IsMatch(code, @"^\d{6}$"), Is.True);
        }

        [Test]
        public void Compile_EntryWithoutDatabaseContext_StillCompiles()
        {
            // 详情面板在 db 关闭时不应崩溃（Compile 委派宿主，db 仅用于 {REF} 解析）
            var entry = CreateTotpEntry("TimeOtp-Secret-Base32", "JBSWY3DPEHPK3PXP");
            Assert.That(() => TotpService.CompileCurrentCode(entry, null), Throws.Nothing);
        }

        [Test]
        public void GetPeriod_ValidAndInvalidValues()
        {
            Assert.That(TotpService.GetPeriodSeconds(CreateTotpEntry("TimeOtp-Secret-Base32", "JBSWY3DPEHPK3PXP", "60")), Is.EqualTo(60));
            Assert.That(TotpService.GetPeriodSeconds(CreateTotpEntry("TimeOtp-Secret-Base32", "JBSWY3DPEHPK3PXP", "abc")), Is.EqualTo(30));
            Assert.That(TotpService.GetPeriodSeconds(CreateTotpEntry("TimeOtp-Secret-Base32", "JBSWY3DPEHPK3PXP", "99999")), Is.EqualTo(30));
            Assert.That(TotpService.GetPeriodSeconds(CreateTotpEntry("TimeOtp-Secret-Base32", "JBSWY3DPEHPK3PXP")), Is.EqualTo(30));
        }

        [Test]
        public void GetExpectedDigits_DefaultsToSix_AndHonorsLengthField()
        {
            Assert.That(TotpService.GetExpectedDigits(CreateTotpEntry("TimeOtp-Secret-Base32", "JBSWY3DPEHPK3PXP")), Is.EqualTo(6));

            var entry = CreateTotpEntry("TimeOtp-Secret-Base32", "JBSWY3DPEHPK3PXP");
            entry.Strings.Set("TimeOtp-Length", new ProtectedString(false, "8"));
            Assert.That(TotpService.GetExpectedDigits(entry), Is.EqualTo(8));
        }

        [Test]
        public void DetailViewModel_WithSeed_ShowsTotpCard()
        {
            var host = new TestPluginHost();
            var entry = CreateTotpEntry("TimeOtp-Secret-Base32", "JBSWY3DPEHPK3PXP");
            host.Database.RootGroup.AddEntry(entry, true);

            var vm = new EntryDetailViewModel(entry, host, null, () => { });
            Assert.That(vm.HasTotp, Is.True);
            Assert.That(vm.TotpCode, Does.Match(@"^\d{6}$"));
            Assert.That(vm.TotpCountdownPercent, Is.InRange(0.0, 100.0));
            Assert.That(vm.TotpSecondsRemaining, Is.InRange(1, 30));
        }

        [Test]
        public void DetailViewModel_WithoutSeed_HidesTotpCard()
        {
            var host = new TestPluginHost();
            var entry = TestPluginHost.CreateTestEntry("NoTotp", "u", "p", "", "");
            host.Database.RootGroup.AddEntry(entry, true);

            var vm = new EntryDetailViewModel(entry, host, null, () => { });
            Assert.That(vm.HasTotp, Is.False);
            Assert.That(vm.TotpCode, Is.Empty);
        }

        [Test]
        public void DetailViewModel_InvalidSeed_HidesCardInsteadOfHanging()
        {
            var host = new TestPluginHost();
            var entry = CreateTotpEntry("TimeOtp-Secret-Hex", "not-hex-at-all!!");
            host.Database.RootGroup.AddEntry(entry, true);

            var vm = new EntryDetailViewModel(entry, host, null, () => { });
            // 种子损坏：卡片隐藏（HasTotp=false）或展示合法验证码，绝不能残留占位符文本
            if (vm.HasTotp)
            {
                Assert.That(vm.TotpCode, Does.Match(@"^\d{6}$"));
            }
            else
            {
                Assert.That(vm.TotpCode, Is.Empty);
            }
        }

        [Test]
        public void DetailViewModel_ClearSensitiveData_StopsCountdownAndClearsCode()
        {
            var host = new TestPluginHost();
            var entry = CreateTotpEntry("TimeOtp-Secret-Base32", "JBSWY3DPEHPK3PXP");
            host.Database.RootGroup.AddEntry(entry, true);

            var vm = new EntryDetailViewModel(entry, host, null, () => { });
            Assert.That(vm.HasTotp, Is.True);

            vm.ClearSensitiveData();
            Assert.That(vm.HasTotp, Is.False);
            Assert.That(vm.TotpCode, Is.Empty);
            Assert.That(vm.TotpSecondsRemaining, Is.EqualTo(0));
        }
    }
}
