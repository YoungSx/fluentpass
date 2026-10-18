using System;
using System.Threading;
using System.Windows;
using KeePassFluentUI.Tests.Mocks;
using KeePassFluentUI.ViewModels;
using KeePassFluentUI.Views;
using NUnit.Framework;
using Wpf.Ui.Markup;

namespace KeePassFluentUI.Tests
{
    [TestFixture]
    public class SmokeTests
    {
        [SetUp]
        public void Setup()
        {
            Common.MessageService.EnableDialogs = false;
        }
        [Test]
        public void PluginMetadata_ShouldBeValid()
        {
            var plugin = new KeePassFluentPlugin();
            Assert.That(plugin.UpdateUrl, Is.Not.Null.And.Not.Empty);
            // SmallIcon 可以安全读取
            var icon = plugin.SmallIcon;
            Assert.That(icon, Is.Not.Null);
        }

        [Test]
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
                    var window = new FluentMainWindow(host, vm);

                    Assert.That(window, Is.Not.Null);
                    Assert.That(window.DataContext, Is.SameAs(vm));
                }
                catch (Exception ex)
                {
                    threadException = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join(10000);

            if (threadException != null)
            {
                Assert.Fail($"Smoke test failed with exception: {threadException}");
            }
        }
    }
}
