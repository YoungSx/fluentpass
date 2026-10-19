using FluentPass.ViewModels;
using NUnit.Framework;

namespace FluentPass.Tests
{
    [TestFixture, Category("Unit")]
    public class AttachmentItemViewModelUnitTests
    {
        [Test, Category("Unit")]
        public void AttachmentItemViewModel_SmallBytes_FormatsAsBytes()
        {
            var vm = new AttachmentItemViewModel("secret.key", 512);

            Assert.That(vm.Name, Is.EqualTo("secret.key"));
            Assert.That(vm.SizeDisplay, Is.EqualTo("512 B"));
        }

        [Test, Category("Unit")]
        public void AttachmentItemViewModel_ZeroBytes_FormatsAs0B()
        {
            var vm = new AttachmentItemViewModel("empty.txt", 0);

            Assert.That(vm.Name, Is.EqualTo("empty.txt"));
            Assert.That(vm.SizeDisplay, Is.EqualTo("0 B"));
        }

        [Test, Category("Unit")]
        public void AttachmentItemViewModel_Kilobytes_FormatsAsKB()
        {
            var vm = new AttachmentItemViewModel("document.pdf", 2048);

            Assert.That(vm.Name, Is.EqualTo("document.pdf"));
            Assert.That(vm.SizeDisplay, Is.EqualTo("2.0 KB"));
        }

        [Test, Category("Unit")]
        public void AttachmentItemViewModel_Megabytes_FormatsAsMB()
        {
            var vm = new AttachmentItemViewModel("archive.zip", 5 * 1024 * 1024);

            Assert.That(vm.Name, Is.EqualTo("archive.zip"));
            Assert.That(vm.SizeDisplay, Is.EqualTo("5.0 MB"));
        }
    }
}
