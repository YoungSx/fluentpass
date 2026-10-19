using System;
using System.IO;
using System.Reflection;
using KeePass.Forms;
using KeePass.Plugins;
using NUnit.Framework;

namespace KeePassFluentUI.Tests
{
    [TestFixture, Apartment(System.Threading.ApartmentState.STA)]
    public class PluginLoadingDiagnosticsTests
    {
        [Test]
        public void DiagnoseKeePassPluginLoading()
        {
            var pluginPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KeePassFluentUI.dll");
            if (!File.Exists(pluginPath))
            {
                var installedPath = @"C:\Program Files\KeePass Password Safe 2\Plugins\KeePassFluentUI.dll";
                if (File.Exists(installedPath))
                {
                    pluginPath = installedPath;
                }
            }

            Assert.That(File.Exists(pluginPath), Is.True, $"KeePassFluentUI.dll not found at: {pluginPath}");
            var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(pluginPath);
            Console.WriteLine("Plugin file: " + pluginPath);
            Console.WriteLine("ProductName: " + fvi.ProductName);
            Assert.That(fvi.ProductName, Is.EqualTo("KeePass Plugin"));

            // 实例化并初始化插件
            var handle = Activator.CreateInstanceFrom(pluginPath, "KeePassFluentUI.KeePassFluentUIExt");
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
