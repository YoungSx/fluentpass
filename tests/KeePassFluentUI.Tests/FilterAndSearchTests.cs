using KeePassFluentUI.Tests.Mocks;
using KeePassFluentUI.ViewModels;
using KeePassLib;
using NUnit.Framework;

namespace KeePassFluentUI.Tests
{
    [TestFixture, Apartment(System.Threading.ApartmentState.STA)]
    public class FilterAndSearchTests
    {
        private TestPluginHost _host = null!;
        private MainViewModel _mainVm = null!;

        [SetUp]
        public void Setup()
        {
            Common.MessageService.EnableDialogs = false;
            _host = new TestPluginHost();

            var e1 = TestPluginHost.CreateTestEntry("GitHub Account", "octocat", "pass1", "https://github.com", "My code repository");
            var e2 = TestPluginHost.CreateTestEntry("Google Mail", "john.doe@gmail.com", "pass2", "https://mail.google.com", "Primary email");
            var e3 = TestPluginHost.CreateTestEntry("Microsoft Azure", "admin@company.onmicrosoft.com", "pass3", "https://portal.azure.com", "Cloud infrastructure");
            var e4 = TestPluginHost.CreateTestEntry("Server SSH Key", "root", "pass4", "", "Production linux host 192.168.1.100");

            _host.Database.RootGroup.AddEntry(e1, true);
            _host.Database.RootGroup.AddEntry(e2, true);
            _host.Database.RootGroup.AddEntry(e3, true);
            _host.Database.RootGroup.AddEntry(e4, true);

            _mainVm = new MainViewModel(_host);
            _mainVm.LoadGroups();
        }

        [Test]
        public void Filter_WithEmptySearch_ShouldReturnAllEntries()
        {
            _mainVm.SearchText = string.Empty;
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(4));
        }

        [Test]
        public void Filter_ByTitleKeyword_ShouldReturnMatchingEntries()
        {
            _mainVm.SearchText = "git";
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));
            Assert.That(_mainVm.FilteredEntries[0].Title, Is.EqualTo("GitHub Account"));
        }

        [Test]
        public void Filter_ByUserName_ShouldReturnMatchingEntries()
        {
            _mainVm.SearchText = "john.doe";
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));
            Assert.That(_mainVm.FilteredEntries[0].Title, Is.EqualTo("Google Mail"));
        }

        [Test]
        public void Filter_ByUrl_ShouldReturnMatchingEntries()
        {
            _mainVm.SearchText = "azure.com";
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));
            Assert.That(_mainVm.FilteredEntries[0].Title, Is.EqualTo("Microsoft Azure"));
        }

        [Test]
        public void Filter_ByNotes_ShouldReturnMatchingEntries()
        {
            _mainVm.SearchText = "linux host";
            Assert.That(_mainVm.FilteredEntries.Count, Is.EqualTo(1));
            Assert.That(_mainVm.FilteredEntries[0].Title, Is.EqualTo("Server SSH Key"));
        }
    }
}
