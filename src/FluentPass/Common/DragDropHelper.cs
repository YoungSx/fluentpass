using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FluentPass.Common
{
    /// <summary>
    /// Helper and attached properties for drag-and-drop visual state in WPF-UI controls.
    /// </summary>
    public static class DragDropHelper
    {
        public const string TreeViewItemPointerOverBrushKey = "TreeViewItemBackgroundPointerOver";

        /// <summary>
        /// Attached dependency property to mark a TreeViewItem as an active drop target.
        /// When true, applies WPF-UI's pointer-over dynamic resource brush to the item's header border.
        /// When false, clears the local background value so the control template triggers resume normal evaluation.
        /// </summary>
        public static readonly DependencyProperty IsDropTargetProperty =
            DependencyProperty.RegisterAttached(
                "IsDropTarget",
                typeof(bool),
                typeof(DragDropHelper),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, OnIsDropTargetChanged));

        public static bool GetIsDropTarget(DependencyObject element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return (bool)element.GetValue(IsDropTargetProperty);
        }

        public static void SetIsDropTarget(DependencyObject element, bool value)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            element.SetValue(IsDropTargetProperty, value);
        }

        private static void OnIsDropTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is System.Windows.Controls.TreeViewItem item)
            {
                ApplyDropTargetHighlight(item, (bool)e.NewValue);
            }
        }

        public static void ApplyDropTargetHighlight(System.Windows.Controls.TreeViewItem item, bool isDropTarget)
        {
            if (item == null) return;

            var border = FindItemBorder(item);
            if (border != null)
            {
                if (isDropTarget)
                {
                    border.SetResourceReference(Border.BackgroundProperty, TreeViewItemPointerOverBrushKey);
                }
                else
                {
                    border.ClearValue(Border.BackgroundProperty);
                }
            }
            else if (!item.IsLoaded)
            {
                RoutedEventHandler? onLoaded = null;
                onLoaded = (s, args) =>
                {
                    item.Loaded -= onLoaded;
                    if (GetIsDropTarget(item))
                    {
                        var b = FindItemBorder(item);
                        b?.SetResourceReference(Border.BackgroundProperty, TreeViewItemPointerOverBrushKey);
                    }
                };
                item.Loaded += onLoaded;
            }
        }

        public static Border? FindItemBorder(System.Windows.Controls.TreeViewItem item)
        {
            if (item == null) return null;

            if (item.Template?.FindName("Border", item) is Border templateBorder)
            {
                return templateBorder;
            }

            return FindVisualChild<Border>(item);
        }

        public static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}
