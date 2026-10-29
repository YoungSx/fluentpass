using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using FluentPass.Views;
using KeePassLib.Security;
using NUnit.Framework;
using UiPasswordBox = Wpf.Ui.Controls.PasswordBox;

namespace FluentPass.Tests
{
    /// <summary>
    /// 自定义字段多行支持回归测试。
    ///
    /// 数据层从未截断换行（值按 <see cref="ProtectedString"/> 原样保存），
    /// 但生产标记里的值输入框原本是单行控件：回车不会插入换行，多行值只能整段替换。
    /// 本组测试守住三件事：
    /// (a) 掩码输入用的 ui:PasswordBox 确实支持多行（它派生自 ui:TextBox）；
    /// (b) 编辑态成败两个输入分支都能让输入框随多行内容增高；
    /// (c) 查看态按 HasMultilineValue 切换为 Wrap，避免只读模板把多行值压成一行。
    /// </summary>
    [TestFixture, Apartment(System.Threading.ApartmentState.STA), Category("Unit")]
    public class CustomFieldMultilineLayoutTests
    {
        private const string MultilineSample = "a\r\nb\r\nc\r\nd\r\ne\r\nf";

        [SetUp]
        public void Setup()
        {
            FluentWindowManager.EnsureWpfApplication();
        }

        [Test]
        public void UiPasswordBox_SupportsMultilineEditing()
        {
            // 掩码框是 ui:PasswordBox；确认它真能从 XAML 接受多行设置。
            // 若上游改掉继承关系，这里的解析会直接失败，提示掩码多行输入已退化。
            var box = (UiPasswordBox)XamlReader.Parse(
                "<PasswordBox xmlns='clr-namespace:Wpf.Ui.Controls;assembly=Wpf.Ui'" +
                " AcceptsReturn='True' TextWrapping='Wrap' MinHeight='34' MaxHeight='180'" +
                " VerticalScrollBarVisibility='Auto' />");

            Assert.That(box.AcceptsReturn, Is.True);
            Assert.That(box.TextWrapping, Is.EqualTo(TextWrapping.Wrap));
            Assert.That(box.MinHeight, Is.EqualTo(34));
            Assert.That(box.MaxHeight, Is.EqualTo(180));
            Assert.That(box.VerticalScrollBarVisibility, Is.EqualTo(ScrollBarVisibility.Auto));
        }

        [Test]
        public void MaskedEditing_KeepsTypedLineBreaks()
        {
            // 掩码态输入的回车必须进入 Password，而不是被吞掉。
            // （掩码显示仍是同一行星号——这是遮蔽的语义，不是截断。）
            var box = new UiPasswordBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Password = "abc" };

            box.AppendText("\r\n");

            Assert.That(box.Password, Is.EqualTo("abc\r\n"), "掩码输入框必须保留换行字符");
            Assert.That(box.Text, Does.Not.Contain("\r"), "掩码显示本身不暴露换行结构");
        }

        [Test]
        public void PlainEditing_TextBoxGrowsWithMultilineValue()
        {
            var multiline = MeasureHosted(new Wpf.Ui.Controls.TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Text = MultilineSample
            });
            var single = MeasureHosted(new Wpf.Ui.Controls.TextBox { Text = "abc" });

            Assert.That(multiline, Is.GreaterThan(single + 20),
                "多行明文必须让输入框增高到足以显示全部行，而不是压成单行");
        }

        [Test]
        public void ProductionXaml_ValueInputs_AcceptLineBreaks()
        {
            var xaml = File.ReadAllText(FindMainWindowXaml());

            var plainBox = FindBlock(xaml, "<ui:TextBox Text=\"{Binding EditValue", "/>");
            var maskedBox = FindBlock(xaml, "<ui:PasswordBox Password=\"{Binding EditValue", "/>");

            Assert.That(plainBox, Does.Contain("AcceptsReturn=\"True\""), "编辑态明文框必须允许回车换行");
            Assert.That(plainBox, Does.Contain("TextWrapping=\"Wrap\""), "编辑态明文框必须自动换行");
            Assert.That(maskedBox, Does.Contain("AcceptsReturn=\"True\""), "编辑态掩码框同样必须允许回车换行");
            Assert.That(maskedBox, Does.Contain("TextWrapping=\"Wrap\""), "编辑态掩码框同样必须自动换行");
        }

        [Test]
        public void ProductionXaml_ViewModeValueBox_WrapsForMultilineValues()
        {
            var xaml = File.ReadAllText(FindMainWindowXaml());

            Assert.That(xaml, Does.Contain("{Binding HasMultilineValue}"),
                "查看态必须按值是否含换行切换多行样式");

            var viewBox = FindBlock(xaml, "<TextBox Grid.Column=\"1\"", "</TextBox>");
            Assert.That(viewBox, Does.Contain("FluentReadOnlyFieldStyle"));
            Assert.That(viewBox, Does.Contain("TextWrapping"), "多行触发必须把只读展示切到 Wrap");
            Assert.That(viewBox, Does.Contain("{Binding DisplayValue, Mode=OneWay}"),
                "只读展示保持单向绑定，避免回写被掩码值覆盖");
        }

        private static double MeasureHosted(Control control)
        {
            var root = new Grid { Width = 200 };
            root.Children.Add(control);
            root.Measure(new Size(200, double.PositiveInfinity));
            root.Arrange(new Rect(root.DesiredSize));
            root.UpdateLayout();
            return control.ActualHeight;
        }

        private static string FindMainWindowXaml()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "src", "FluentPass", "Views", "FluentMainWindow.xaml");
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }

            throw new FileNotFoundException("FluentMainWindow.xaml not found above test directory");
        }

        private static string FindBlock(string xaml, string startMarker, string endMarker)
        {
            var start = xaml.IndexOf(startMarker, System.StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThanOrEqualTo(0), $"未找到标记: {startMarker}");
            var end = xaml.IndexOf(endMarker, start + startMarker.Length, System.StringComparison.Ordinal);
            Assert.That(end, Is.GreaterThan(start), $"未找到结束标记: {endMarker}");
            return xaml.Substring(start, end - start);
        }
    }
}