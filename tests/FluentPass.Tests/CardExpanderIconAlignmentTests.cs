using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FluentPass.Views;
using NUnit.Framework;
using Wpf.Ui.Controls;

namespace FluentPass.Tests
{
    /// <summary>
    /// CardExpander header icon/text vertical alignment regression test.
    ///
    /// Root cause in WPF-UI 4.3.0: the CardExpander template hosts the Icon in a
    /// ContentControl with VerticalAlignment=Center, Margin="0,0,14,0" and FontSize=24,
    /// while the Header is placed in a ContentPresenter that sets NO VerticalAlignment.
    /// A plain string Header is implicitly wrapped in a TextBlock with
    /// VerticalAlignment=Stretch; inside the icon-tallened row that TextBlock lays its
    /// text out from the top and leaves the spare leading at the bottom of its LineHeight,
    /// so the text ink center sits ~4px above the icon ink center -- i.e. the icon looks
    /// too low (4px measured on the reported screenshot, 2.7-3.3px when rendering the
    /// template directly). Visible at high DPI.
    ///
    /// The production markup therefore passes an explicit
    /// &lt;ui:TextBlock VerticalAlignment="Center"/&gt; element as the Header.
    /// This test measures both ink centers by offscreen rendering with the real
    /// WPF-UI template and fonts, and asserts (a) the production form stays visually
    /// aligned and (b) it is clearly better than the plain-string form in the same
    /// rendering session. (b) doubles as a canary: if a WPF-UI upgrade ever fixes the
    /// template, the plain-string baseline assertion fails and the wrapper can be dropped.
    /// Offscreen rendering needs no window manager, so this is a plain Unit test like
    /// <see cref="DragDropHighlightTests"/>.
    /// </summary>
    [TestFixture, Apartment(System.Threading.ApartmentState.STA), Category("Unit")]
    public class CardExpanderIconAlignmentTests
    {
        /// <summary>
        /// Largest offset still imperceptible at this icon size. Calibrated above the worst value
        /// measured across machines (1.33px: glyph ink depends on the font actually resolved, and
        /// CI runners resolve different font versions than a dev box), yet below the broken form's
        /// 2.7-3.3px so a regression back to a string Header still fails.
        /// </summary>
        private const double VisualTolerancePx = 2.0;

        /// <summary>The centered form must be clearly better, not merely under the floor.</summary>
        private const double MaxFractionOfUnfixedOffset = 0.6;

        [SetUp]
        public void EnsureApp()
        {
            FluentWindowManager.EnsureWpfApplication();
        }

        [TestCase(96)]
        [TestCase(120)]
        [TestCase(144)]
        [TestCase(192)]
        public void CenteredHeader_KeepsIconAndTextWithinVisualTolerance(double dpi)
        {
            double offset = MeasureInkCenterOffset(dpi, centeredHeader: true);
            TestContext.Out.WriteLine($"dpi={dpi} centered-header offset={offset:+0.00;-0.00}px");
            Assert.That(Math.Abs(offset), Is.LessThanOrEqualTo(VisualTolerancePx),
                "Icon and header text ink centers must look vertically aligned");
        }

        /// <summary>
        /// A/B under one rendering session, so font resolution, DPI and GDI settings are identical
        /// for both forms. That makes the comparison portable across machines in a way an absolute
        /// pixel floor is not -- and it is what proves the wrapper (not some environmental
        /// accident) is what fixes the alignment.
        /// </summary>
        [Test]
        public void CenteredHeader_HalvesThePlainStringMisalignment()
        {
            double plain = MeasureInkCenterOffset(144, centeredHeader: false);
            double centered = MeasureInkCenterOffset(144, centeredHeader: true);
            TestContext.Out.WriteLine($"dpi=144 plain-string offset={plain:+0.00;-0.00}px, " +
                                      $"centered offset={centered:+0.00;-0.00}px");

            Assert.That(Math.Abs(plain), Is.GreaterThanOrEqualTo(2.0),
                "WPF-UI 4.3.0 string Header should still reproduce the known offset. " +
                "If it aligns now, upstream fixed the template -- drop the explicit TextBlock wrapper");
            Assert.That(Math.Abs(centered), Is.LessThanOrEqualTo(Math.Abs(plain) * MaxFractionOfUnfixedOffset),
                "the centered Header must visibly reduce the misalignment caused by a string Header");
        }

        /// <summary>
        /// Returns icon ink center minus text ink center. Positive means the icon sits lower
        /// (which is what a string Header produces: text is pinned to the top of the icon-tall row).
        /// </summary>
        private static double MeasureInkCenterOffset(double dpi, bool centeredHeader)
        {
            const string HeaderText = "\u5386\u53f2\u7248\u672c\u5feb\u7167 (1)";
            const string OpenTag = @"<ui:CardExpander xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                xmlns:ui='http://schemas.lepo.co/wpfui/2022/xaml'
                Icon='{ui:SymbolIcon History24}'";

            // Attribute markup and element markup cannot be spliced together, so the two
            // header forms need whole documents.
            string xaml = centeredHeader
                ? $@"{OpenTag}>
                    <ui:CardExpander.Header>
                        <ui:TextBlock Text='{HeaderText}' VerticalAlignment='Center' />
                    </ui:CardExpander.Header>
                </ui:CardExpander>"
                : $@"{OpenTag} Header='{HeaderText}' />";

            var expander = (CardExpander)System.Windows.Markup.XamlReader.Parse(xaml);
            expander.ApplyTemplate();
            expander.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            expander.Arrange(new Rect(expander.DesiredSize));
            expander.UpdateLayout();

            var iconHost = (ContentControl)expander.Template.FindName("ControlIcon", expander);
            var headerHost = (ContentPresenter)expander.Template.FindName("HeaderContentPresenter", expander);
            Assert.That(iconHost, Is.Not.Null);
            Assert.That(headerHost, Is.Not.Null);

            double scale = dpi / 96.0;
            int widthPx = (int)Math.Ceiling(expander.ActualWidth * scale);
            int heightPx = (int)Math.Ceiling(expander.ActualHeight * scale);
            var bitmap = new RenderTargetBitmap(widthPx, heightPx, dpi, dpi, PixelFormats.Pbgra32);
            bitmap.Render(expander);
            bitmap.Freeze();

            int stride = widthPx * 4;
            var pixels = new byte[stride * heightPx];
            bitmap.CopyPixels(pixels, stride, 0);

            var iconRect = iconHost.TransformToVisual(expander).TransformBounds(new Rect(iconHost.RenderSize));
            var headerRect = headerHost.TransformToVisual(expander).TransformBounds(new Rect(headerHost.RenderSize));
            // Text ink only lives right of the icon host; start scanning there plus the
            // icon host's right margin so a stray icon antialias pixel can never be counted.
            double textLeft = iconRect.Right + iconHost.Margin.Right;
            var textRect = new Rect(textLeft, headerRect.Y, Math.Max(headerRect.Right - textLeft, 1), headerRect.Height);

            double iconInk = InkCenterY(pixels, stride, widthPx, heightPx, iconRect, scale);
            double textInk = InkCenterY(pixels, stride, widthPx, heightPx, textRect, scale);
            Assert.That(double.IsNaN(iconInk), Is.False, "icon must render dark ink");
            Assert.That(double.IsNaN(textInk), Is.False, "header text must render dark ink");
            return iconInk - textInk;
        }

        /// <summary>
        /// Vertical center (in DIPs) of dark ink inside <paramref name="logicalRect"/>.
        /// Dark = mostly opaque and dark luminance, which excludes the card's light border
        /// and background while keeping antialiased glyph/icon pixels.
        /// </summary>
        private static double InkCenterY(byte[] pixels, int stride, int widthPx, int heightPx,
            Rect logicalRect, double scale)
        {
            int x0 = Math.Max(0, (int)Math.Floor(logicalRect.X * scale));
            int x1 = Math.Min(widthPx, (int)Math.Ceiling(logicalRect.Right * scale));
            int first = -1, last = -1;
            for (int y = 0; y < heightPx; y++)
            {
                for (int x = x0; x < x1; x++)
                {
                    int i = y * stride + x * 4; // BGRA
                    if (pixels[i + 3] < 128) continue;
                    if ((pixels[i] + pixels[i + 1] + pixels[i + 2]) / 3 >= 160) continue;
                    if (first < 0) first = y;
                    last = y;
                    break;
                }
            }
            return first < 0 ? double.NaN : (first + last) / 2.0 / scale;
        }
    }
}
