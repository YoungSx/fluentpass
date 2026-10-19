using System;
using System.Linq;
using FluentPass.Common;
using FluentPass.Tests.Mocks;
using NUnit.Framework;

namespace FluentPass.Tests
{
    [TestFixture, Category("Unit")]
    public class LocalizationTests
    {
        [SetUp]
        public void Setup()
        {
            // Reset to auto/default before each test
            Loc.Instance.SetLanguagePreference(SettingsService.DefaultLanguage);
        }

        [Test]
        public void TranslationStore_BothLanguages_ShouldHaveIdenticalKeys()
        {
            var zhDict = TranslationStore.GetDictionary("zh-CN");
            var enDict = TranslationStore.GetDictionary("en-US");

            Assert.That(zhDict, Is.Not.Null);
            Assert.That(enDict, Is.Not.Null);
            Assert.That(zhDict.Count, Is.GreaterThan(80), "Expected at least 80 localized keys");
            Assert.That(enDict.Count, Is.GreaterThan(80), "Expected at least 80 localized keys");

            var missingInEn = zhDict.Keys.Where(k => !enDict.ContainsKey(k)).ToList();
            var missingInZh = enDict.Keys.Where(k => !zhDict.ContainsKey(k)).ToList();

            Assert.That(missingInEn, Is.Empty, $"Keys in zh-CN but missing in en-US: {string.Join(", ", missingInEn)}");
            Assert.That(missingInZh, Is.Empty, $"Keys in en-US but missing in zh-CN: {string.Join(", ", missingInZh)}");
        }

        [Test]
        public void TranslationStore_NoEmptyStringsOrNullValues()
        {
            var zhDict = TranslationStore.GetDictionary("zh-CN");
            var enDict = TranslationStore.GetDictionary("en-US");

            foreach (var kvp in zhDict)
            {
                Assert.That(kvp.Value, Is.Not.Null.And.Not.Empty, $"zh-CN key '{kvp.Key}' has empty or null value");
            }

            foreach (var kvp in enDict)
            {
                Assert.That(kvp.Value, Is.Not.Null.And.Not.Empty, $"en-US key '{kvp.Key}' has empty or null value");
            }
        }

        [Test]
        public void Loc_DirectGetAndIndexer_ReturnsCorrectString()
        {
            Loc.Instance.SetLanguagePreference("zh-CN");
            Assert.That(Loc.Get("Common_Save"), Is.EqualTo("保存"));
            Assert.That(Loc.Instance["Common_Save"], Is.EqualTo("保存"));

            Loc.Instance.SetLanguagePreference("en-US");
            Assert.That(Loc.Get("Common_Save"), Is.EqualTo("Save"));
            Assert.That(Loc.Instance["Common_Save"], Is.EqualTo("Save"));
        }

        [Test]
        public void Loc_FormattedString_FormatsWithArgs()
        {
            Loc.Instance.SetLanguagePreference("zh-CN");
            var zhFormatted = Loc.Get("Entries_SummaryFormat", 100, 25);
            Assert.That(zhFormatted, Is.EqualTo("共 100 个条目 (显示 25 项)"));

            Loc.Instance.SetLanguagePreference("en-US");
            var enFormatted = Loc.Get("Entries_SummaryFormat", 100, 25);
            Assert.That(enFormatted, Is.EqualTo("100 entries (25 shown)"));
        }

        [Test]
        public void Loc_LanguageChangedEvent_And_PropertyChangedRaised()
        {
            bool eventFired = false;
            bool propertyFired = false;

            EventHandler handler = (s, e) => eventFired = true;
            System.ComponentModel.PropertyChangedEventHandler propHandler = (s, e) =>
            {
                if (e.PropertyName == "Item[]")
                {
                    propertyFired = true;
                }
            };

            Loc.Instance.LanguageChanged += handler;
            Loc.Instance.PropertyChanged += propHandler;

            try
            {
                Loc.Instance.SetLanguagePreference("en-US");
                Assert.That(eventFired, Is.True, "LanguageChanged event should fire");
                Assert.That(propertyFired, Is.True, "PropertyChanged('Item[]') should fire for WPF bindings");
                Assert.That(Loc.Instance.CurrentLanguage, Is.EqualTo("en-US"));
            }
            finally
            {
                Loc.Instance.LanguageChanged -= handler;
                Loc.Instance.PropertyChanged -= propHandler;
            }
        }

        [Test]
        public void Loc_FallbackKey_ReturnsKeyNameWhenNotFound()
        {
            string nonExistentKey = "NonExistent_Key_99999";
            var result = Loc.Get(nonExistentKey);
            Assert.That(result, Is.EqualTo(nonExistentKey));
        }

        [Test]
        public void SettingsService_LanguagePreference_PersistsAndReadsCorrectly()
        {
            var host = new TestPluginHost();

            // Default is Auto
            var defaultLang = SettingsService.GetLanguage(host);
            Assert.That(defaultLang, Is.EqualTo(SettingsService.DefaultLanguage));

            // Set to English
            SettingsService.SetLanguage(host, "en-US");
            Assert.That(SettingsService.GetLanguage(host), Is.EqualTo("en-US"));

            // Set to Simplified Chinese
            SettingsService.SetLanguage(host, "zh-CN");
            Assert.That(SettingsService.GetLanguage(host), Is.EqualTo("zh-CN"));

            // Set back to Auto
            SettingsService.SetLanguage(host, "Auto");
            Assert.That(SettingsService.GetLanguage(host), Is.EqualTo("Auto"));
        }
    }
}
