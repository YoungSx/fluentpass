using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FluentPass.Tests.Mocks;
using FluentPass.ViewModels;
using FluentPass.Views;
using NUnit.Framework;
using Wpf.Ui.Controls;

namespace FluentPass.Tests
{
    /// <summary>
    /// Guards the production detail pane markup: every CardExpander header there must pass an
    /// explicitly centered element, not a plain string. A string Header is implicitly wrapped in a
    /// VerticalAlignment=Stretch TextBlock, which lays its text out from the top of the icon-tallened
    /// row and leaves the text ~4px below the icon center (see CardExpanderIconAlignmentTests).
    ///
    /// Asserted on layout only (visual centers), so this stays fast and font-independent.
    /// </summary>
    [TestFixture, Apartment(System.Threading.ApartmentState.STA), Category("Smoke")]
    public class DetailCardExpanderAlignmentTests
    {
        [Test]
        public void EveryVisibleDetailHeaderCentersItsContentAgainstTheIcon()
        {
            FluentWindowManager.EnsureWpfApplication();
            Common.MessageService.EnableDialogs = false;

            var host = new TestPluginHost();
            host.Database.RootGroup.AddEntry(
                TestPluginHost.CreateTestEntry("GitHub Account", "octocat", "pass1", "https://github.com"), true);
            var vm = new MainViewModel(host);
            vm.LoadGroups();
            var win = new FluentMainWindow(host, vm) { Width = 1240, Height = 900 };
            win.Show();
            try
            {
                vm.SelectedEntry = vm.FilteredEntries[0];
                win.UpdateLayout();

                List<CardExpander> expanders;
                for (int attempt = 0; ; attempt++)
                {
                    win.UpdateLayout();
                    expanders = Descendants(win).OfType<CardExpander>().Where(e => e.IsVisible).ToList();
                    if (expanders.Count > 0 || attempt >= 20) break;
                    System.Threading.Thread.Sleep(50);
                }

                Assert.That(expanders, Is.Not.Empty, "detail pane should show CardExpander sections");

                foreach (var expander in expanders)
                {
                    expander.ApplyTemplate();
                    expander.UpdateLayout();

                    var iconHost = expander.Template.FindName("ControlIcon", expander) as ContentControl;
                    var headerHost = expander.Template.FindName("HeaderContentPresenter", expander) as ContentPresenter;
                    Assert.That(iconHost, Is.Not.Null);
                    Assert.That(headerHost, Is.Not.Null);
                    if (iconHost!.Content is null) continue;

                    var headerElement = headerHost!.Content as FrameworkElement;
                    Assert.That(headerElement, Is.Not.Null,
                        "CardExpander Header must be an element, not a raw string");
                    Assert.That(headerElement!.VerticalAlignment, Is.EqualTo(VerticalAlignment.Center),
                        "Header element must be vertically centered to line up with the icon");

                    double iconCenter = CenterY(iconHost, expander);
                    double headerCenter = CenterY(headerElement, expander);
                    Assert.That(Math.Abs(iconCenter - headerCenter), Is.LessThanOrEqualTo(1.0),
                        $"header '{headerElement}' must be centered against its icon");
                }
            }
            finally
            {
                win.ForceClose();
            }
        }

        private static double CenterY(FrameworkElement element, Visual root)
        {
            var bounds = element.TransformToVisual(root).TransformBounds(new Rect(element.RenderSize));
            return bounds.Top + bounds.Height / 2;
        }

        private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                yield return child;
                foreach (var sub in Descendants(child)) yield return sub;
            }
        }
    }
}
