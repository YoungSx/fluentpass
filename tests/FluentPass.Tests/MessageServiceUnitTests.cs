using System.Windows;
using FluentPass.Common;
using NUnit.Framework;

namespace FluentPass.Tests
{
    [TestFixture, Category("Unit")]
    public class MessageServiceUnitTests
    {
        [SetUp]
        public void Setup()
        {
            MessageService.EnableDialogs = false;
        }

        [TearDown]
        public void TearDown()
        {
            MessageService.EnableDialogs = false;
        }

        [Test, Category("Unit")]
        public void Show_WhenDialogsDisabled_ReturnsOkForDefault()
        {
            var res = MessageService.Show("Message", "Title", MessageBoxButton.OK);
            Assert.That(res, Is.EqualTo(MessageBoxResult.OK));
        }

        [Test, Category("Unit")]
        public void Show_WhenDialogsDisabled_ReturnsYesForYesNoButton()
        {
            var res = MessageService.Show("Are you sure?", "Confirm", MessageBoxButton.YesNo);
            Assert.That(res, Is.EqualTo(MessageBoxResult.Yes));
        }

        [Test, Category("Unit")]
        public void Show_WhenDialogsDisabled_ReturnsYesForYesNoCancelButton()
        {
            var res = MessageService.Show("Save changes?", "Confirm", MessageBoxButton.YesNoCancel);
            Assert.That(res, Is.EqualTo(MessageBoxResult.Yes));
        }

        [Test, Category("Unit")]
        public void EnableDialogs_PropertyCanBeToggled()
        {
            MessageService.EnableDialogs = true;
            Assert.That(MessageService.EnableDialogs, Is.True);

            MessageService.EnableDialogs = false;
            Assert.That(MessageService.EnableDialogs, Is.False);
        }
    }
}
