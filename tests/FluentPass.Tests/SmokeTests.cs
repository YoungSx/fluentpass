using System;
using System.Threading;
using System.Windows;
using FluentPass.Tests.Mocks;
using FluentPass.ViewModels;
using FluentPass.Views;
using NUnit.Framework;
using Wpf.Ui.Markup;

namespace FluentPass.Tests
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
            var plugin = new FluentPassExt();
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
    }
}
