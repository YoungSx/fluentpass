using System.Diagnostics;
using NUnit.Framework;

namespace FluentPass.Tests
{
    /// <summary>
    /// 铁律 5（单文件预编译分发）的机器校验：
    /// 宿主只加载一个 FluentPass.dll，任何依赖缺失都会导致插件静默加载失败。
    /// 这里用反射核对 Costura 内嵌资源与 PE 产品标识，避免发版后再靠人工冒烟发现。
    /// </summary>
    [TestFixture, Category("Unit")]
    public class PackagingInvariantTests
    {
        private static readonly string[] RequiredEmbeddedDependencies =
        {
            "costura.wpf.ui.dll.compressed",
            "costura.wpf.ui.abstractions.dll.compressed",
            "costura.system.memory.dll.compressed",
        };

        [Test]
        public void PluginAssembly_DeclaresKeePassPluginIdentity()
        {
            var info = FileVersionInfo.GetVersionInfo(typeof(FluentPassExt).Assembly.Location);

            Assert.That(info.ProductName, Is.EqualTo("KeePass Plugin"),
                "KeePass 通过 PE Product 标识识别插件");
        }

        [Test]
        public void PluginAssembly_EmbedsAllRuntimeDependencies()
        {
            var resources = typeof(FluentPassExt).Assembly.GetManifestResourceNames();

            foreach (var expected in RequiredEmbeddedDependencies)
            {
                Assert.That(resources, Does.Contain(expected),
                    $"Costura 未内嵌 {expected}：分发出去的单文件插件将无法在宿主中加载");
            }
        }
    }
}
