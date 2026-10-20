using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FluentPass.Common;
using FluentPass.Tests.Mocks;
using FluentPass.ViewModels;
using FluentPass.Views;
using KeePassLib;
using NUnit.Framework;

namespace FluentPass.Tests
{
    [TestFixture, Apartment(ApartmentState.STA), Category("Unit")]
    public class DragDropHighlightTests
    {
        [SetUp]
        public void Setup()
        {
            FluentWindowManager.EnsureWpfApplication();
            Common.MessageService.EnableDialogs = false;
        }

        [Test]
        public void DragDropHelper_NullGuards_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => DragDropHelper.GetIsDropTarget(null!));
            Assert.Throws<ArgumentNullException>(() => DragDropHelper.SetIsDropTarget(null!, true));
        }

        [Test]
        public void DragDropHelper_SetIsDropTarget_TrueAndFalse_AppliesAndClearsResource()
        {
            var item = new TreeViewItem();
            var border = new Border { Name = "Border" };
            var grid = new Grid();
            grid.Children.Add(border);

            var template = new ControlTemplate(typeof(TreeViewItem));
            var factory = new FrameworkElementFactory(typeof(Grid));
            var borderFactory = new FrameworkElementFactory(typeof(Border), "Border");
            factory.AppendChild(borderFactory);
            template.VisualTree = factory;
            item.Template = template;
            item.ApplyTemplate();

            // Initial state: not drop target
            Assert.That(DragDropHelper.GetIsDropTarget(item), Is.False);

            // Set to true
            DragDropHelper.SetIsDropTarget(item, true);
            Assert.That(DragDropHelper.GetIsDropTarget(item), Is.True);

            var resolvedBorder = DragDropHelper.FindItemBorder(item);
            Assert.That(resolvedBorder, Is.Not.Null);

            // In WPF, setting resource reference via SetResourceReference gives a non-unset value
            var bgValue = resolvedBorder!.ReadLocalValue(Border.BackgroundProperty);
            Assert.That(bgValue, Is.Not.EqualTo(DependencyProperty.UnsetValue));

            // Set to false
            DragDropHelper.SetIsDropTarget(item, false);
            Assert.That(DragDropHelper.GetIsDropTarget(item), Is.False);

            // Local value must be cleared so template triggers/defaults resume
            var clearedValue = resolvedBorder.ReadLocalValue(Border.BackgroundProperty);
            Assert.That(clearedValue, Is.EqualTo(DependencyProperty.UnsetValue));
        }

        [Test]
        public void DragDropHelper_SelectedTreeViewItem_PreservesSelectionStateOnClear()
        {
            var item = new TreeViewItem();
            var template = new ControlTemplate(typeof(TreeViewItem));
            var factory = new FrameworkElementFactory(typeof(Grid));
            var borderFactory = new FrameworkElementFactory(typeof(Border), "Border");
            factory.AppendChild(borderFactory);
            template.VisualTree = factory;
            item.Template = template;
            item.ApplyTemplate();

            item.IsSelected = true;
            Assert.That(item.IsSelected, Is.True);

            // Highlight as drop target
            DragDropHelper.SetIsDropTarget(item, true);
            Assert.That(DragDropHelper.GetIsDropTarget(item), Is.True);

            // Clear drop target
            DragDropHelper.SetIsDropTarget(item, false);
            Assert.That(DragDropHelper.GetIsDropTarget(item), Is.False);

            // Selection state must remain preserved
            Assert.That(item.IsSelected, Is.True);
        }

        [Test, Category("Smoke")]
        public void VisualSmokeTest_TreeViewItem_DragHighlightRendering_SavesVisualArtifact()
        {
            // Create a TreeView in WPF containing TreeViewItems
            var treeView = new TreeView
            {
                Width = 280,
                Height = 350,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0)
            };

            var targetItem = new TreeViewItem
            {
                Header = "Finance & Banking",
                IsExpanded = true
            };
            var childItem = new TreeViewItem
            {
                Header = "Credit Cards"
            };
            targetItem.Items.Add(childItem);

            var siblingItem = new TreeViewItem
            {
                Header = "Personal & Social"
            };

            treeView.Items.Add(targetItem);
            treeView.Items.Add(siblingItem);

            // Trigger measure & layout
            treeView.Measure(new Size(280, 350));
            treeView.Arrange(new Rect(0, 0, 280, 350));
            treeView.UpdateLayout();

            targetItem.ApplyTemplate();
            siblingItem.ApplyTemplate();

            // Set drop target highlight on the target item
            DragDropHelper.SetIsDropTarget(targetItem, true);
            Assert.That(DragDropHelper.GetIsDropTarget(targetItem), Is.True);
            Assert.That(DragDropHelper.GetIsDropTarget(siblingItem), Is.False);

            var targetBorder = DragDropHelper.FindItemBorder(targetItem);
            Assert.That(targetBorder, Is.Not.Null);
            Assert.That(targetBorder!.ReadLocalValue(Border.BackgroundProperty), Is.Not.EqualTo(DependencyProperty.UnsetValue));

            // Sibling border must NOT have local background set
            var siblingBorder = DragDropHelper.FindItemBorder(siblingItem);
            if (siblingBorder != null)
            {
                Assert.That(siblingBorder.ReadLocalValue(Border.BackgroundProperty), Is.EqualTo(DependencyProperty.UnsetValue));
            }

            // Render to visual bitmap
            var rtb = new RenderTargetBitmap(280, 350, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(treeView);
            Assert.That(rtb.PixelWidth, Is.EqualTo(280));
            Assert.That(rtb.PixelHeight, Is.EqualTo(350));

            // Save visual artifact
            string artifactsDir = Path.Combine(Path.GetTempPath(), "FluentPassScreenshots");
            Directory.CreateDirectory(artifactsDir);
            string screenshotPath = Path.Combine(artifactsDir, "drag_drop_highlight_smoke.png");

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            using (var stream = File.Create(screenshotPath))
            {
                encoder.Save(stream);
            }

            Assert.That(File.Exists(screenshotPath), Is.True);
            TestContext.WriteLine($"Visual smoke screenshot saved to: {screenshotPath}");

            // Clear drop target and verify restoration
            DragDropHelper.SetIsDropTarget(targetItem, false);
            Assert.That(DragDropHelper.GetIsDropTarget(targetItem), Is.False);
            Assert.That(targetBorder.ReadLocalValue(Border.BackgroundProperty), Is.EqualTo(DependencyProperty.UnsetValue));
        }
    }
}
