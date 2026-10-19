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
        private static readonly string[] AllLanguages = new[]
        {
            "zh-CN",
            "zh-TW",
            "en-US",
            "ja-JP",
            "ko-KR",
            "ru-RU",
            "es-ES"
        };

        [SetUp]
        public void Setup()
        {
            // Reset to auto/default before each test
            Loc.Instance.SetLanguagePreference(SettingsService.DefaultLanguage);
        }

        [Test]
        public void TranslationStore_AllSevenLanguages_ShouldHaveIdenticalKeys()
        {
            var dictionaries = AllLanguages.ToDictionary(lang => lang, lang => TranslationStore.GetDictionary(lang));

            var referenceLang = "zh-CN";
            var referenceDict = dictionaries[referenceLang];
            Assert.That(referenceDict.Count, Is.GreaterThan(190), "Expected at least 190 localized keys in reference language");

            foreach (var lang in AllLanguages)
            {
                var dict = dictionaries[lang];
                Assert.That(dict, Is.Not.Null, $"Dictionary for {lang} should not be null");
                Assert.That(dict.Count, Is.EqualTo(referenceDict.Count), $"Key count mismatch in {lang}: expected {referenceDict.Count} but was {dict.Count}");

                var missingKeys = referenceDict.Keys.Where(k => !dict.ContainsKey(k)).ToList();
                Assert.That(missingKeys, Is.Empty, $"Keys in {referenceLang} missing in {lang}: {string.Join(", ", missingKeys)}");

                var extraKeys = dict.Keys.Where(k => !referenceDict.ContainsKey(k)).ToList();
                Assert.That(extraKeys, Is.Empty, $"Extra keys in {lang} not in {referenceLang}: {string.Join(", ", extraKeys)}");
            }
        }

        [Test]
        public void TranslationStore_AllSevenLanguages_NoEmptyStringsOrNullValues()
        {
            foreach (var lang in AllLanguages)
            {
                var dict = TranslationStore.GetDictionary(lang);
                foreach (var kvp in dict)
                {
                    Assert.That(kvp.Value, Is.Not.Null.And.Not.Empty, $"{lang} key '{kvp.Key}' has empty or null value");
                }
            }
        }

        [Test]
        public void Loc_DirectGetAndIndexer_ReturnsCorrectString_AcrossLanguages()
        {
            // Simplified Chinese
            Loc.Instance.SetLanguagePreference("zh-CN");
            Assert.That(Loc.Get("Common_Save"), Is.EqualTo("保存"));
            Assert.That(Loc.Instance["Common_Save"], Is.EqualTo("保存"));

            // Traditional Chinese
            Loc.Instance.SetLanguagePreference("zh-TW");
            Assert.That(Loc.Get("Common_Save"), Is.EqualTo("儲存"));
            Assert.That(Loc.Instance["Common_Save"], Is.EqualTo("儲存"));

            // English
            Loc.Instance.SetLanguagePreference("en-US");
            Assert.That(Loc.Get("Common_Save"), Is.EqualTo("Save"));
            Assert.That(Loc.Instance["Common_Save"], Is.EqualTo("Save"));

            // Japanese
            Loc.Instance.SetLanguagePreference("ja-JP");
            Assert.That(Loc.Get("Common_Save"), Is.EqualTo("保存"));
            Assert.That(Loc.Instance["Common_Cancel"], Is.EqualTo("キャンセル"));

            // Korean
            Loc.Instance.SetLanguagePreference("ko-KR");
            Assert.That(Loc.Get("Common_Save"), Is.EqualTo("저장"));
            Assert.That(Loc.Instance["Common_Cancel"], Is.EqualTo("취소"));

            // Russian
            Loc.Instance.SetLanguagePreference("ru-RU");
            Assert.That(Loc.Get("Common_Save"), Is.EqualTo("Сохранить"));
            Assert.That(Loc.Instance["Common_Cancel"], Is.EqualTo("Отмена"));

            // Spanish
            Loc.Instance.SetLanguagePreference("es-ES");
            Assert.That(Loc.Get("Common_Save"), Is.EqualTo("Guardar"));
            Assert.That(Loc.Instance["Common_Cancel"], Is.EqualTo("Cancelar"));
        }

        [Test]
        public void Loc_FormattedString_FormatsWithArgs_AcrossLanguages()
        {
            Loc.Instance.SetLanguagePreference("zh-CN");
            var zhFormatted = Loc.Get("Entries_SummaryFormat", 100, 25);
            Assert.That(zhFormatted, Is.EqualTo("共 100 个条目 (显示 25 项)"));

            Loc.Instance.SetLanguagePreference("zh-TW");
            var twFormatted = Loc.Get("Entries_SummaryFormat", 100, 25);
            Assert.That(twFormatted, Is.EqualTo("共 100 個項目 (顯示 25 項)"));

            Loc.Instance.SetLanguagePreference("en-US");
            var enFormatted = Loc.Get("Entries_SummaryFormat", 100, 25);
            Assert.That(enFormatted, Is.EqualTo("100 entries (25 shown)"));

            Loc.Instance.SetLanguagePreference("ja-JP");
            var jaFormatted = Loc.Get("Entries_SummaryFormat", 100, 25);
            Assert.That(jaFormatted, Is.EqualTo("全 100 件のエントリ (25 件表示中)"));

            Loc.Instance.SetLanguagePreference("ko-KR");
            var koFormatted = Loc.Get("Entries_SummaryFormat", 100, 25);
            Assert.That(koFormatted, Is.EqualTo("총 100개 항목 (25개 표시됨)"));

            Loc.Instance.SetLanguagePreference("ru-RU");
            var ruFormatted = Loc.Get("Entries_SummaryFormat", 100, 25);
            Assert.That(ruFormatted, Is.EqualTo("Всего записей: 100 (показано 25)"));

            Loc.Instance.SetLanguagePreference("es-ES");
            var esFormatted = Loc.Get("Entries_SummaryFormat", 100, 25);
            Assert.That(esFormatted, Is.EqualTo("100 entradas en total (25 mostradas)"));
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
                Loc.Instance.SetLanguagePreference("ja-JP");
                Assert.That(eventFired, Is.True, "LanguageChanged event should fire");
                Assert.That(propertyFired, Is.True, "PropertyChanged('Item[]') should fire for WPF bindings");
                Assert.That(Loc.Instance.CurrentLanguage, Is.EqualTo("ja-JP"));
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

            foreach (var lang in AllLanguages)
            {
                SettingsService.SetLanguage(host, lang);
                Assert.That(SettingsService.GetLanguage(host), Is.EqualTo(lang));
            }

            // Set back to Auto
            SettingsService.SetLanguage(host, "Auto");
            Assert.That(SettingsService.GetLanguage(host), Is.EqualTo("Auto"));
        }
    }
}
