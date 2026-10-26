using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
            "de-DE",
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

        private static string FindRepositoryRoot()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "FluentPass.sln")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }

            dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "FluentPass.sln")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }

            return Directory.GetCurrentDirectory();
        }

        [Test]
        public void TranslationStore_AllEightLanguages_ShouldHaveIdenticalKeys()
        {
            var dictionaries = AllLanguages.ToDictionary(lang => lang, lang => TranslationStore.GetDictionary(lang));

            var referenceLang = "zh-CN";
            var referenceDict = dictionaries[referenceLang];
            Assert.That(referenceDict.Count, Is.GreaterThan(200), "Expected at least 200 localized keys in reference language");

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
        public void TranslationStore_AllEightLanguages_NoEmptyStringsOrNullValues()
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
        public void TranslationStore_AllReferencedXamlKeys_MustExistInAllLanguageDictionaries()
        {
            var repoRoot = FindRepositoryRoot();
            var srcDir = Path.Combine(repoRoot, "src");
            Assert.That(Directory.Exists(srcDir), Is.True, $"Source directory not found at {srcDir}");

            var xamlFiles = Directory.GetFiles(srcDir, "*.xaml", SearchOption.AllDirectories);
            Assert.That(xamlFiles.Length, Is.GreaterThan(0), "No XAML files found to validate");

            var locKeyRegex = new Regex(@"\{common:Loc\s+([^},]+)", RegexOptions.Compiled);

            var referencedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var keySources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var xamlFile in xamlFiles)
            {
                var content = File.ReadAllText(xamlFile);
                var matches = locKeyRegex.Matches(content);
                foreach (Match match in matches)
                {
                    var key = match.Groups[1].Value.Trim();
                    if (key.StartsWith("Key=", StringComparison.OrdinalIgnoreCase))
                    {
                        key = key.Substring(4).Trim();
                    }
                    if (!string.IsNullOrEmpty(key))
                    {
                        referencedKeys.Add(key);
                        if (!keySources.ContainsKey(key))
                        {
                            keySources[key] = Path.GetFileName(xamlFile);
                        }
                    }
                }
            }

            Assert.That(referencedKeys.Count, Is.GreaterThan(50), "Expected to find at least 50 XAML localization keys");

            foreach (var lang in AllLanguages)
            {
                var dict = TranslationStore.GetDictionary(lang);
                var missingKeys = referencedKeys.Where(k => !dict.ContainsKey(k)).ToList();

                Assert.That(missingKeys, Is.Empty,
                    $"Language '{lang}' is missing {missingKeys.Count} keys referenced in XAML:\n" +
                    string.Join("\n", missingKeys.Select(k => $"  - {k} (in {keySources[k]})")));
            }
        }

        [Test]
        public void TranslationStore_AllReferencedCsKeys_MustExistInAllLanguageDictionaries()
        {
            var repoRoot = FindRepositoryRoot();
            var srcDir = Path.Combine(repoRoot, "src");
            Assert.That(Directory.Exists(srcDir), Is.True, $"Source directory not found at {srcDir}");

            var csFiles = Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !Path.GetFileName(f).Equals("TranslationStore.cs", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var getRegex = new Regex(@"Loc\.Get\(\s*""([^""]+)""", RegexOptions.Compiled);
            var indexerRegex = new Regex(@"Loc\.Instance\[\s*""([^""]+)""\]", RegexOptions.Compiled);

            var referencedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var keySources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var csFile in csFiles)
            {
                var content = File.ReadAllText(csFile);
                foreach (Match match in getRegex.Matches(content))
                {
                    var key = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(key))
                    {
                        referencedKeys.Add(key);
                        if (!keySources.ContainsKey(key))
                        {
                            keySources[key] = Path.GetFileName(csFile);
                        }
                    }
                }
                foreach (Match match in indexerRegex.Matches(content))
                {
                    var key = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(key))
                    {
                        referencedKeys.Add(key);
                        if (!keySources.ContainsKey(key))
                        {
                            keySources[key] = Path.GetFileName(csFile);
                        }
                    }
                }
            }

            Assert.That(referencedKeys.Count, Is.GreaterThan(50), "Expected to find at least 50 C# localization keys");

            foreach (var lang in AllLanguages)
            {
                var dict = TranslationStore.GetDictionary(lang);
                var missingKeys = referencedKeys.Where(k => !dict.ContainsKey(k)).ToList();

                Assert.That(missingKeys, Is.Empty,
                    $"Language '{lang}' is missing {missingKeys.Count} keys referenced in C#:\n" +
                    string.Join("\n", missingKeys.Select(k => $"  - {k} (in {keySources[k]})")));
            }
        }

        [Test]
        public void TranslationStore_AllKeys_MustBeReferencedByXamlOrCs()
        {
            // 翻译 key 只增不减会累积死条目（旧命名残留），且每次新增都要 ×8 语言维护。
            // 这里反向校验：字典里的每个 key 必须在 XAML 的 {common:Loc} 或 C# 的 Loc.Get/索引器中出现。
            var repoRoot = FindRepositoryRoot();
            var srcDir = Path.Combine(repoRoot, "src");
            Assert.That(Directory.Exists(srcDir), Is.True, $"Source directory not found at {srcDir}");

            var xamlRegex = new Regex(@"\{common:Loc\s+(?:Key=)?([^},\s]+)", RegexOptions.Compiled);
            var getRegex = new Regex(@"Loc\.Get\(\s*""([^""]+)""", RegexOptions.Compiled);
            var indexerRegex = new Regex(@"Loc\.Instance\[\s*""([^""]+)""\]", RegexOptions.Compiled);

            var referencedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in Directory.GetFiles(srcDir, "*.*", SearchOption.AllDirectories))
            {
                var extension = Path.GetExtension(file);
                bool isXaml = extension.Equals(".xaml", StringComparison.OrdinalIgnoreCase);
                bool isCs = extension.Equals(".cs", StringComparison.OrdinalIgnoreCase);
                if (!isXaml && !isCs) continue;
                if (Path.GetFileName(file).Equals("TranslationStore.cs", StringComparison.OrdinalIgnoreCase)) continue;

                var content = File.ReadAllText(file);
                if (isXaml)
                {
                    foreach (Match match in xamlRegex.Matches(content)) referencedKeys.Add(match.Groups[1].Value.Trim());
                    continue;
                }

                foreach (Match match in getRegex.Matches(content)) referencedKeys.Add(match.Groups[1].Value.Trim());
                foreach (Match match in indexerRegex.Matches(content)) referencedKeys.Add(match.Groups[1].Value.Trim());
            }

            foreach (var lang in AllLanguages)
            {
                var dict = TranslationStore.GetDictionary(lang);
                var deadKeys = dict.Keys.Where(k => !referencedKeys.Contains(k)).OrderBy(k => k, StringComparer.Ordinal).ToList();

                Assert.That(deadKeys, Is.Empty,
                    $"Language '{lang}' has {deadKeys.Count} keys referenced nowhere in src (rename leftover or delete):\n" +
                    string.Join("\n", deadKeys.Select(k => $"  - {k}")));
            }
        }

        [Test]
        public void Loc_DirectGetAndIndexer_ReturnsCorrectString_AcrossLanguages()
        {
            // Simplified Chinese
            Loc.Instance.SetLanguagePreference("zh-CN");
            Assert.That(Loc.Get("Detail_Button_Save"), Is.EqualTo("保存"));
            Assert.That(Loc.Instance["Detail_Button_Save"], Is.EqualTo("保存"));

            // Traditional Chinese
            Loc.Instance.SetLanguagePreference("zh-TW");
            Assert.That(Loc.Get("Detail_Button_Save"), Is.EqualTo("儲存"));
            Assert.That(Loc.Instance["Detail_Button_Save"], Is.EqualTo("儲存"));

            // English
            Loc.Instance.SetLanguagePreference("en-US");
            Assert.That(Loc.Get("Detail_Button_Save"), Is.EqualTo("Save"));
            Assert.That(Loc.Instance["Detail_Button_Save"], Is.EqualTo("Save"));

            // Japanese
            Loc.Instance.SetLanguagePreference("ja-JP");
            Assert.That(Loc.Get("Detail_Button_Save"), Is.EqualTo("保存"));
            Assert.That(Loc.Instance["Detail_Button_Cancel"], Is.EqualTo("キャンセル"));

            // Korean
            Loc.Instance.SetLanguagePreference("ko-KR");
            Assert.That(Loc.Get("Detail_Button_Save"), Is.EqualTo("저장"));
            Assert.That(Loc.Instance["Detail_Button_Cancel"], Is.EqualTo("취소"));

            // Russian
            Loc.Instance.SetLanguagePreference("ru-RU");
            Assert.That(Loc.Get("Detail_Button_Save"), Is.EqualTo("Сохранить"));
            Assert.That(Loc.Instance["Detail_Button_Cancel"], Is.EqualTo("Отмена"));

            // Spanish
            Loc.Instance.SetLanguagePreference("es-ES");
            Assert.That(Loc.Get("Detail_Button_Save"), Is.EqualTo("Guardar"));
            Assert.That(Loc.Instance["Detail_Button_Cancel"], Is.EqualTo("Cancelar"));

            // German
            Loc.Instance.SetLanguagePreference("de-DE");
            Assert.That(Loc.Get("Detail_Button_Save"), Is.EqualTo("Speichern"));
            Assert.That(Loc.Instance["Detail_Button_Cancel"], Is.EqualTo("Abbrechen"));
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

            Loc.Instance.SetLanguagePreference("de-DE");
            var deFormatted = Loc.Get("Entries_SummaryFormat", 100, 25);
            Assert.That(deFormatted, Is.EqualTo("100 Einträge (25 angezeigt)"));
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
