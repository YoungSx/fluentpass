using System;
using System.IO;
using System.Reflection;
using KeePass.Forms;
using KeePass.Plugins;
using NUnit.Framework;

namespace FluentPass.Tests
{
    [TestFixture, Apartment(System.Threading.ApartmentState.STA), Category("Smoke"), Category("Integration")]
    public class PluginLoadingDiagnosticsTests
    {
        [Test, Category("Smoke"), Category("Integration")]
        public void DiagnoseKeePassPluginLoading()
        {
            var pluginPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FluentPass.dll");
            if (!File.Exists(pluginPath))
            {
                var installedPath = @"C:\Program Files\KeePass Password Safe 2\Plugins\FluentPass.dll";
                if (File.Exists(installedPath))
                {
                    pluginPath = installedPath;
                }
            }

            Assert.That(File.Exists(pluginPath), Is.True, $"FluentPass.dll not found at: {pluginPath}");
            var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(pluginPath);
            Console.WriteLine("Plugin file: " + pluginPath);
            Console.WriteLine("ProductName: " + fvi.ProductName);
            Assert.That(fvi.ProductName, Is.EqualTo("KeePass Plugin"));

            // 实例化并初始化插件
            var handle = Activator.CreateInstanceFrom(pluginPath, "FluentPass.FluentPassExt");
            var plugin = (Plugin)handle.Unwrap()!;
            Assert.That(plugin, Is.Not.Null);
            Console.WriteLine("Created instance: " + plugin.GetType().FullName);

            // 获取菜单项测试
            var menuItem = plugin.GetMenuItem(PluginMenuType.Main);
            Assert.That(menuItem, Is.Not.Null);
            Console.WriteLine("Menu Item Text: " + menuItem.Text);
            var asm = typeof(MainForm).Assembly;
            var pmType = asm.GetType("KeePass.Plugins.PluginManager")!;

            // 验证 CheckCompatibilityPriv 对该插件通过
            var checkCompatPrivMethod = pmType.GetMethod("CheckCompatibilityPriv", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (checkCompatPrivMethod != null)
            {
                Assert.DoesNotThrow(() => checkCompatPrivMethod.Invoke(null, new object[] { plugin }));
                Console.WriteLine("CheckCompatibilityPriv passed cleanly!");
            }
        }
    }
}
