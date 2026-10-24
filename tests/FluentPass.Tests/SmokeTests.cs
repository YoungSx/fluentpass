using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using FluentPass.Tests.Mocks;
using FluentPass.ViewModels;
using FluentPass.Views;
using KeePass.Plugins;
using NUnit.Framework;
using Wpf.Ui.Controls;
using Wpf.Ui.Markup;

namespace FluentPass.Tests
{
    [TestFixture, Apartment(ApartmentState.STA), Category("Smoke")]
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
            FluentWindowManager.EnsureWpfApplication();

            var host = new TestPluginHost();
            var vm = new MainViewModel(host);
            var win = new FluentMainWindow(host, vm);
            Assert.That(win, Is.Not.Null);
            win.ForceClose();
        }

        [Test, Category("Smoke")]
        public void SearchBox_UserInput_UpdatesSearchTextAndFiltersEntries()
        {
            FluentWindowManager.EnsureWpfApplication();

            var host = new TestPluginHost();
            host.Database.RootGroup.AddEntry(
                TestPluginHost.CreateTestEntry("GitHub Account", "octocat", "pass1", "https://github.com"), true);
            host.Database.RootGroup.AddEntry(
                TestPluginHost.CreateTestEntry("Google Mail", "john.doe", "pass2"), true);

            var vm = new MainViewModel(host);
            vm.LoadGroups();
            var win = new FluentMainWindow(host, vm);
            win.Show();

            try
            {
                var box = (AutoSuggestBox)win.FindName("SearchBox");
                Assert.That(box, Is.Not.Null);
                box.ApplyTemplate();
                var inner = box.Template.FindName("PART_TextBox", box) as System.Windows.Controls.TextBox;
                Assert.That(inner, Is.Not.Null, "AutoSuggestBox 模板必须包含 PART_TextBox 输入框");

                // 模拟用户键入：直接修改内部 TextBox 文本（WPF-UI 4.3.0 的 Text 依赖属性
                // 不随输入回写，输入只能经 TextChanged 路由事件进入 ViewModel —— 此测试固定该接线）
                inner.Text = "git";

                Assert.That(vm.SearchText, Is.EqualTo("git"),
                    "用户在搜索框输入的内容必须经 TextChanged 事件同步进 ViewModel");
                Assert.That(vm.IsSearchResultsActive, Is.True);

                // 泵 dispatcher 直至 200ms 输入防抖触发搜索（上限 3 秒）
                var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(3);
                while (DateTime.UtcNow < deadline && vm.FilteredEntries.Count != 1)
                {
                    PumpDispatcher();
                }

                Assert.That(vm.IsSearchResultsActive, Is.True);
                Assert.That(vm.FilteredEntries.Count, Is.EqualTo(1));
                Assert.That(vm.FilteredEntries[0].Title, Is.EqualTo("GitHub Account"));
            }
            finally
            {
                win.ForceClose();
            }
        }

        private static void PumpDispatcher()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }

        [Test, Category("Smoke")]
        public void SearchBox_IconClick_SubmitsSearch()
        {
            FluentWindowManager.EnsureWpfApplication();

            var host = new TestPluginHost();
            host.Database.RootGroup.AddEntry(
                TestPluginHost.CreateTestEntry("GitHub Account", "octocat", "pass1", "https://github.com"), true);
            host.Database.RootGroup.AddEntry(
                TestPluginHost.CreateTestEntry("Google Mail", "john.doe", "pass2"), true);

            var vm = new MainViewModel(host);
            vm.LoadGroups();
            var win = new FluentMainWindow(host, vm);
            win.Show();

            try
            {
                var box = (AutoSuggestBox)win.FindName("SearchBox");
                box.ApplyTemplate();
                var inner = box.Template.FindName("PART_TextBox", box) as System.Windows.Controls.TextBox;
                Assert.That(inner, Is.Not.Null);
                inner.ApplyTemplate();

                inner.Text = "git";
                Assert.That(vm.SearchText, Is.EqualTo("git"));

                // 模拟点击分组退出搜索结果视图（文本保留）
                vm.LoadGroupEntries(vm.SelectedGroup);
                Assert.That(vm.IsSearchResultsActive, Is.False);

                // 点击搜索图标（内部 TextBox 模板中的图标宿主 ContentPresenter，Direct 路由事件）：必须等价于 Enter 提交
                var iconHost = inner.Template.FindName("ControlIconRight", inner) as System.Windows.Controls.ContentPresenter
                    ?? inner.Template.FindName("ControlIconLeft", inner) as System.Windows.Controls.ContentPresenter;
                Assert.That(iconHost, Is.Not.Null, "内部 TextBox 模板应包含图标宿主元素");
                iconHost!.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(
                    System.Windows.Input.Mouse.PrimaryDevice, 0, System.Windows.Input.MouseButton.Left)
                {
                    RoutedEvent = System.Windows.UIElement.PreviewMouseLeftButtonUpEvent
                });

                Assert.That(vm.IsSearchResultsActive, Is.True, "点击搜索图标必须提交搜索（重新进入搜索结果视图）");
                Assert.That(vm.FilteredEntries.Count, Is.EqualTo(1));
                Assert.That(vm.FilteredEntries[0].Title, Is.EqualTo("GitHub Account"));
            }
            finally
            {
                win.ForceClose();
            }
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
