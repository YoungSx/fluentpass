using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;
using FluentPass.Tests.Mocks;
using FluentPass.ViewModels;
using FluentPass.Views;
using KeePass.Plugins;
using NUnit.Framework;
using Wpf.Ui.Markup;

namespace FluentPass.Tests
{
    [TestFixture, Category("Smoke")]
    public class SmokeTests
    {
        [SetUp]
        public void Setup()
        {
            Common.MessageService.EnableDialogs = false;
        }

        [Test, Category("Smoke")]
        public void PluginMetadata_ShouldBeValid()
        {
            var plugin = new FluentPassExt();
            Assert.That(plugin.UpdateUrl, Is.Not.Null.And.Not.Empty);
            // SmallIcon 可以安全读取，且严格符合 KeePass 官方 16x16 像素规范
            var icon = plugin.SmallIcon;
            Assert.That(icon, Is.Not.Null);
            Assert.That(icon!.Width, Is.EqualTo(16));
            Assert.That(icon.Height, Is.EqualTo(16));
        }

        [Test, Category("Smoke")]
        public void WpfApplication_And_FluentMainWindow_CanBeInstantiatedInSTAThread()
        {
            Exception? threadException = null;

            var thread = new Thread(() =>
            {
                try
                {
                    if (Application.Current == null)
                    {
                        var app = new Application
                        {
                            ShutdownMode = ShutdownMode.OnExplicitShutdown
                        };
                        app.Resources.MergedDictionaries.Add(new ThemesDictionary());
                        app.Resources.MergedDictionaries.Add(new ControlsDictionary());
                    }

                    var host = new TestPluginHost();
                    var vm = new MainViewModel(host);
                    var win = new FluentMainWindow(host, vm);
                    Assert.That(win, Is.Not.Null);
                }
                catch (Exception ex)
                {
                    threadException = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join(10000);

            Assert.That(threadException, Is.Null, $"STA thread initialization threw: {threadException}");
        }

        [Test, Category("Smoke")]
        public void PluginLifecycle_InitializeAndTerminate_ExecutesWithoutException()
        {
            var plugin = new FluentPassExt();
            var host = new TestPluginHost();

            bool initResult = false;
            Assert.DoesNotThrow(() =>
            {
                initResult = plugin.Initialize(host);
            });
            Assert.That(initResult, Is.True);

            var menuItem = plugin.GetMenuItem(PluginMenuType.Main);
            Assert.That(menuItem, Is.Not.Null);
            Assert.That(menuItem!.Text, Does.Contain("Fluent Pass"));

            Assert.DoesNotThrow(() =>
            {
                plugin.Terminate();
            });
        }

        [Test, Category("Smoke")]
        public void CrossPlatformGuard_GracefulFallback_DoesNotThrowUnhandledException()
        {
            var plugin = new FluentPassExt();
            var host = new TestPluginHost();
            plugin.Initialize(host);

            // 无论在 Windows 还是非 Windows (Mono) 环境下，ShowFluentWindow 都不应抛出未捕获的 TypeLoadException
            Assert.DoesNotThrow(() =>
            {
                plugin.ShowFluentWindow();
            });

            plugin.Terminate();
        }

        [Test, Category("Smoke")]
        public void SmallIcon_EmbeddedIconResource_Resolves16x16Bitmap()
        {
            var asm = typeof(FluentPassExt).Assembly;
            using (var stream = asm.GetManifestResourceStream("FluentPass.Resources.AppIcon.ico"))
            {
                Assert.That(stream, Is.Not.Null, "FluentPass.Resources.AppIcon.ico embedded resource must exist!");
                using (var icon = new Icon(stream, 16, 16))
                {
                    using (var bmp = icon.ToBitmap())
                    {
                        Assert.That(bmp.Width, Is.EqualTo(16));
                        Assert.That(bmp.Height, Is.EqualTo(16));
                    }
                }
            }
        }
    }
}
