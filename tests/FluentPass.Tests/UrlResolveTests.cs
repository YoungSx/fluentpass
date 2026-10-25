using FluentPass.Services;
using FluentPass.Tests.Mocks;
using KeePassLib;
using NUnit.Framework;

namespace FluentPass.Tests
{
    /// <summary>
    /// UrlService 与 KeePass 原生“打开 URL”管线的对齐验证：
    /// 条目级 OverrideUrl → 全局 URL Override → Scheme Overrides → URL 原文占位符编译。
    /// </summary>
    [TestFixture, Category("Unit")]
    public class UrlResolveTests
    {
        [Test]
        public void Resolve_NoUrl_ReturnsNull()
        {
            var host = new TestPluginHost();
            var entry = TestPluginHost.CreateTestEntry("NoUrl", "u", "p", "", "");
            host.Database.RootGroup.AddEntry(entry, true);

            Assert.That(UrlService.ResolveOpenUrl(entry, host.Database), Is.Null);
        }

        [Test]
        public void Resolve_NullEntry_ReturnsNull()
        {
            Assert.That(UrlService.ResolveOpenUrl(null, new TestPluginHost().Database), Is.Null);
        }

        [Test]
        public void Resolve_PlainUrl_NoOverrides_PassesThrough()
        {
            var host = new TestPluginHost();
            const string url = "https://example.com/login?user=x";
            var entry = TestPluginHost.CreateTestEntry("Plain", "u", "p", url, "");
            host.Database.RootGroup.AddEntry(entry, true);

            Assert.That(UrlService.ResolveOpenUrl(entry, host.Database), Is.EqualTo(url));
        }

        [Test]
        public void Resolve_EntryOverrideWithPlaceholder_ReplacesUrl()
        {
            var host = new TestPluginHost();
            const string url = "https://example.com/secret";
            var entry = TestPluginHost.CreateTestEntry("Override", "u", "p", url, "");
            entry.OverrideUrl = "cmd://myapp --open \"{URL}\"";
            host.Database.RootGroup.AddEntry(entry, true);

            var resolved = UrlService.ResolveOpenUrl(entry, host.Database);
            Assert.That(resolved, Does.StartWith("cmd://myapp --open"));
            Assert.That(resolved, Does.Contain(url));
        }

        [Test]
        public void Resolve_EntryOverrideWithoutPlaceholder_UsedVerbatim()
        {
            var host = new TestPluginHost();
            var entry = TestPluginHost.CreateTestEntry("Override2", "u", "p", "https://ignored.example", "");
            entry.OverrideUrl = "mstsc://direct-target";
            host.Database.RootGroup.AddEntry(entry, true);

            Assert.That(UrlService.ResolveOpenUrl(entry, host.Database), Is.EqualTo("mstsc://direct-target"));
        }

        [Test]
        public void Resolve_GlobalOverride_UsedWhenEnabled()
        {
            var host = new TestPluginHost();
            var entry = TestPluginHost.CreateTestEntry("Global", "u", "p", "https://global.example/path", "");
            host.Database.RootGroup.AddEntry(entry, true);

            var integration = KeePass.Program.Config.Integration;
            bool savedEnabled = integration.UrlOverrideEnabled;
            string savedOverride = integration.UrlOverride;
            try
            {
                integration.UrlOverrideEnabled = true;
                integration.UrlOverride = "custom://global/{URL}";

                var resolved = UrlService.ResolveOpenUrl(entry, host.Database);
                Assert.That(resolved, Does.StartWith("custom://global/"));
                Assert.That(resolved, Does.Contain("https://global.example/path"));
            }
            finally
            {
                integration.UrlOverrideEnabled = savedEnabled;
                integration.UrlOverride = savedOverride;
            }
        }

        [Test]
        public void Resolve_GlobalOverrideDisabled_EntryUrlWins()
        {
            var host = new TestPluginHost();
            const string url = "https://disabled.example";
            var entry = TestPluginHost.CreateTestEntry("Disabled", "u", "p", url, "");
            host.Database.RootGroup.AddEntry(entry, true);

            var integration = KeePass.Program.Config.Integration;
            bool savedEnabled = integration.UrlOverrideEnabled;
            string savedOverride = integration.UrlOverride;
            try
            {
                integration.UrlOverrideEnabled = false;
                integration.UrlOverride = "custom://should-not-apply/{URL}";

                Assert.That(UrlService.ResolveOpenUrl(entry, host.Database), Is.EqualTo(url));
            }
            finally
            {
                integration.UrlOverrideEnabled = savedEnabled;
                integration.UrlOverride = savedOverride;
            }
        }

        [Test]
        public void Resolve_EntryOverrideTakesPrecedenceOverGlobal()
        {
            var host = new TestPluginHost();
            var entry = TestPluginHost.CreateTestEntry("Precedence", "u", "p", "https://base.example", "");
            entry.OverrideUrl = "entry://wins";
            host.Database.RootGroup.AddEntry(entry, true);

            var integration = KeePass.Program.Config.Integration;
            bool savedEnabled = integration.UrlOverrideEnabled;
            string savedOverride = integration.UrlOverride;
            try
            {
                integration.UrlOverrideEnabled = true;
                integration.UrlOverride = "global://loses/{URL}";

                Assert.That(UrlService.ResolveOpenUrl(entry, host.Database), Is.EqualTo("entry://wins"));
            }
            finally
            {
                integration.UrlOverrideEnabled = savedEnabled;
                integration.UrlOverride = savedOverride;
            }
        }

        [Test]
        public void Resolve_UrlWithSprPlaceholder_Compiles()
        {
            var host = new TestPluginHost();
            // {URL:RMVSCM} 组件占位符引用条目 URL 本身（原生 SprEngine 能力）
            var entry = TestPluginHost.CreateTestEntry("Spr", "u", "p", "{URL:RMVSCM}https://example.com/x", "");
            host.Database.RootGroup.AddEntry(entry, true);

            var resolved = UrlService.ResolveOpenUrl(entry, host.Database);
            Assert.That(resolved, Is.Not.Null);
            Assert.That(resolved, Does.Contain("https://example.com/x"));
            Assert.That(resolved, Does.Not.Contain("{URL"));
        }
    }
}
