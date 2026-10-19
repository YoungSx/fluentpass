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
                string.Equals(_languagePreference, "zh", StringComparison.OrdinalIgnoreCase))
            {
                targetCode = "zh-CN";
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
                    if (code.StartsWith("zh")) return "zh-CN";
                    return "en-US";
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
                    if (lower.Contains("chinese") || lower.Contains("zh") || lower.Contains("simp"))
                    {
                        return "zh-CN";
                    }
                }
            }
            catch { }

            // 3. Fallback to System Culture
            try
            {
                var uiCulture = CultureInfo.CurrentUICulture;
                if (uiCulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ||
                    uiCulture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase))
                {
                    return "zh-CN";
                }
            }
            catch { }

            // Default to Chinese for FluentPass if indeterminate
            return "zh-CN";
        }
    }
}
