using System.Runtime.CompilerServices;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using KeePass.Plugins;

namespace FluentPass.Common
{
    public class Loc : INotifyPropertyChanged
    {
        public static Loc Instance { get; } = new Loc();

        private string _languagePreference = "Auto";
        private string _currentLanguage = "zh-CN";
        private CultureInfo _currentCulture = new CultureInfo("zh-CN");

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? LanguageChanged;

        public string LanguagePreference => _languagePreference;
        public string CurrentLanguage => _currentLanguage;
        public CultureInfo CurrentCulture => _currentCulture;

        public string this[string key] => Get(key);

        public void Initialize(IPluginHost? host)
        {
            var savedPref = SettingsService.GetLanguage(host);
            SetLanguagePreference(savedPref);
        }

        public void SetLanguagePreference(string? preference)
        {
            _languagePreference = string.IsNullOrWhiteSpace(preference) ? "Auto" : preference!;

            string targetCode;
            if (string.Equals(_languagePreference, "zh-CN", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_languagePreference, "zh-Hans", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_languagePreference, "zh", StringComparison.OrdinalIgnoreCase))
            {
                targetCode = "zh-CN";
            }
            else if (string.Equals(_languagePreference, "zh-TW", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(_languagePreference, "zh-Hant", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(_languagePreference, "zh-HK", StringComparison.OrdinalIgnoreCase))
            {
                targetCode = "zh-TW";
            }
            else if (string.Equals(_languagePreference, "ja-JP", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(_languagePreference, "ja", StringComparison.OrdinalIgnoreCase))
            {
                targetCode = "ja-JP";
            }
            else if (string.Equals(_languagePreference, "ko-KR", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(_languagePreference, "ko", StringComparison.OrdinalIgnoreCase))
            {
                targetCode = "ko-KR";
            }
            else if (string.Equals(_languagePreference, "ru-RU", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(_languagePreference, "ru", StringComparison.OrdinalIgnoreCase))
            {
                targetCode = "ru-RU";
            }
            else if (string.Equals(_languagePreference, "es-ES", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(_languagePreference, "es", StringComparison.OrdinalIgnoreCase))
            {
                targetCode = "es-ES";
            }
            else if (string.Equals(_languagePreference, "de-DE", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(_languagePreference, "de", StringComparison.OrdinalIgnoreCase))
            {
                targetCode = "de-DE";
            }
            else if (string.Equals(_languagePreference, "en-US", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(_languagePreference, "en", StringComparison.OrdinalIgnoreCase))
            {
                targetCode = "en-US";
            }
            else
            {
                targetCode = DetectKeePassOrSystemLanguage();
            }

            ApplyLanguage(targetCode);
        }

        private void ApplyLanguage(string languageCode)
        {
            bool changed = !string.Equals(_currentLanguage, languageCode, StringComparison.OrdinalIgnoreCase);
            _currentLanguage = languageCode;

            try
            {
                _currentCulture = new CultureInfo(languageCode);
            }
            catch
            {
                _currentCulture = CultureInfo.InvariantCulture;
            }

            if (changed)
            {
                // Update WPF application resource dictionary if available
                UpdateApplicationResources();

                // Notify all WPF UI bindings targeting Loc.Instance[...]
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguage)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentCulture)));

                LanguageChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void UpdateApplicationResources()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                try
                {
                    UpdateWpfApplicationResourcesCore();
                }
                catch { }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void UpdateWpfApplicationResourcesCore()
        {
            var app = Application.Current;
            if (app == null) return;

            var dict = TranslationStore.GetDictionary(_currentLanguage);
            foreach (var kvp in dict)
            {
                app.Resources[kvp.Key] = kvp.Value;
                app.Resources["Loc_" + kvp.Key] = kvp.Value;
            }
        }

        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            var primaryDict = TranslationStore.GetDictionary(Instance.CurrentLanguage);
            if (primaryDict.TryGetValue(key, out var val))
            {
                return val;
            }

            var fallbackDict = TranslationStore.GetFallbackDictionary(Instance.CurrentLanguage);
            if (fallbackDict.TryGetValue(key, out var fallbackVal))
            {
                return fallbackVal;
            }

            // Fallback to KeePass official KPRes if property exists
            try
            {
                var kpResProp = typeof(KeePass.Resources.KPRes).GetProperty(
                    key,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (kpResProp != null)
                {
                    var kpVal = kpResProp.GetValue(null, null) as string;
                    if (!string.IsNullOrEmpty(kpVal)) return kpVal!;
                }
            }
            catch { }

            return key;
        }

        public static string Get(string key, params object[] args)
        {
            var pattern = Get(key);
            if (args == null || args.Length == 0) return pattern;

            try
            {
                return string.Format(Instance.CurrentCulture, pattern, args);
            }
            catch
            {
                return pattern;
            }
        }

        public static string DetectKeePassOrSystemLanguage()
        {
            // 1. Check KeePass loaded translation
            try
            {
                var trans = KeePass.Program.Translation;
                if (trans?.Properties != null && !string.IsNullOrWhiteSpace(trans.Properties.Iso6391Code))
                {
                    var code = trans.Properties.Iso6391Code.Trim().ToLowerInvariant();
                    if (code.StartsWith("zh-tw") || code.StartsWith("zh-hk") || code.StartsWith("zh-mo") || code.Contains("hant")) return "zh-TW";
                    if (code.StartsWith("zh")) return "zh-CN";
                    if (code.StartsWith("ja")) return "ja-JP";
                    if (code.StartsWith("ko")) return "ko-KR";
                    if (code.StartsWith("ru")) return "ru-RU";
                    if (code.StartsWith("es")) return "es-ES";
                    if (code.StartsWith("de")) return "de-DE";
                    if (code.StartsWith("en")) return "en-US";
                }
            }
            catch { }

            // 2. Check KeePass configuration file
            try
            {
                var langFile = KeePass.Program.Config?.Application?.LanguageFile;
                if (!string.IsNullOrWhiteSpace(langFile))
                {
                    var lower = langFile!.ToLowerInvariant();
                    if (lower.Contains("trad") || lower.Contains("zh-tw") || lower.Contains("zh-hk") || lower.Contains("cht"))
                    {
                        return "zh-TW";
                    }
                    if (lower.Contains("chinese") || lower.Contains("zh") || lower.Contains("simp") || lower.Contains("chs"))
                    {
                        return "zh-CN";
                    }
                    if (lower.Contains("japan") || lower.Contains("ja"))
                    {
                        return "ja-JP";
                    }
                    if (lower.Contains("korean") || lower.Contains("ko"))
                    {
                        return "ko-KR";
                    }
                    if (lower.Contains("russ") || lower.Contains("ru"))
                    {
                        return "ru-RU";
                    }
                    if (lower.Contains("span") || lower.Contains("es"))
                    {
                        return "es-ES";
                    }
                    if (lower.Contains("german") || lower.Contains("deutsch") || lower.Contains("de-de"))
                    {
                        return "de-DE";
                    }
                }
            }
            catch { }

            // 3. Fallback to System Culture
            try
            {
                var uiCulture = CultureInfo.CurrentUICulture;
                var name = uiCulture.Name.ToLowerInvariant();
                var twoLetter = uiCulture.TwoLetterISOLanguageName.ToLowerInvariant();

                if (name.StartsWith("zh-tw") || name.StartsWith("zh-hk") || name.StartsWith("zh-mo") || name.Contains("hant"))
                {
                    return "zh-TW";
                }
                if (twoLetter == "zh")
                {
                    return "zh-CN";
                }
                if (twoLetter == "ja")
                {
                    return "ja-JP";
                }
                if (twoLetter == "ko")
                {
                    return "ko-KR";
                }
                if (twoLetter == "ru")
                {
                    return "ru-RU";
                }
                if (twoLetter == "es")
                {
                    return "es-ES";
                }
                if (twoLetter == "de")
                {
                    return "de-DE";
                }
                if (twoLetter == "en")
                {
                    return "en-US";
                }
            }
            catch { }

            // Default to Chinese for FluentPass if indeterminate
            return "zh-CN";
        }
    }
}
