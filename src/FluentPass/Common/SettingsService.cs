using System;
using KeePass.Plugins;

namespace FluentPass.Common
{
    public enum WindowCloseAction
    {
        RestoreOriginalWindow = 0,
        MinimizeToTray = 1,
        ExitKeePass = 2
    }

    public static class SettingsService
    {
        public const string AutoTakeoverKey = "FluentPass_AutoTakeoverUI";
        public const string CloseActionKey = "FluentPass_CloseAction";
        public const string AppThemeKey = "FluentPass_AppTheme";
        public const string LanguageKey = "FluentPass_Language";
        public const string AutoMaskSecondsKey = "FluentPass_AutoMaskSeconds";

        public const bool DefaultAutoTakeoverUI = true;
        public const WindowCloseAction DefaultCloseAction = WindowCloseAction.RestoreOriginalWindow;
        public const string DefaultAppTheme = "System";
        public const string DefaultLanguage = "Auto";
        public const int DefaultAutoMaskSeconds = 10;

        public static bool GetAutoTakeoverUI(IPluginHost? host)
        {
            var val = host?.CustomConfig?.GetString(AutoTakeoverKey);
            if (string.IsNullOrWhiteSpace(val))
            {
                return DefaultAutoTakeoverUI;
            }

            if (bool.TryParse(val, out bool result))
            {
                return result;
            }

            return DefaultAutoTakeoverUI;
        }

        public static void SetAutoTakeoverUI(IPluginHost? host, bool value)
        {
            try
            {
                host?.CustomConfig?.SetString(AutoTakeoverKey, value.ToString());
            }
            catch { }
        }

        public static WindowCloseAction GetCloseAction(IPluginHost? host)
        {
            var val = host?.CustomConfig?.GetString(CloseActionKey);
            if (string.IsNullOrWhiteSpace(val))
            {
                return DefaultCloseAction;
            }

            if (int.TryParse(val, out int intVal) && Enum.IsDefined(typeof(WindowCloseAction), intVal))
            {
                return (WindowCloseAction)intVal;
            }

            if (Enum.TryParse<WindowCloseAction>(val, true, out var enumVal) && Enum.IsDefined(typeof(WindowCloseAction), enumVal))
            {
                return enumVal;
            }

            return DefaultCloseAction;
        }

        public static void SetCloseAction(IPluginHost? host, WindowCloseAction action)
        {
            try
            {
                host?.CustomConfig?.SetString(CloseActionKey, ((int)action).ToString());
            }
            catch { }
        }

        public static string GetAppTheme(IPluginHost? host)
        {
            var val = host?.CustomConfig?.GetString(AppThemeKey);
            if (string.IsNullOrWhiteSpace(val))
            {
                return DefaultAppTheme;
            }

            if (string.Equals(val, "Dark", StringComparison.OrdinalIgnoreCase)) return "Dark";
            if (string.Equals(val, "Light", StringComparison.OrdinalIgnoreCase)) return "Light";
            if (string.Equals(val, "System", StringComparison.OrdinalIgnoreCase)) return "System";

            return DefaultAppTheme;
        }

        public static void SetAppTheme(IPluginHost? host, string theme)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(theme) ||
                    (!string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase) &&
                     !string.Equals(theme, "Light", StringComparison.OrdinalIgnoreCase) &&
                     !string.Equals(theme, "System", StringComparison.OrdinalIgnoreCase)))
                {
                    theme = DefaultAppTheme;
                }
                host?.CustomConfig?.SetString(AppThemeKey, theme);
            }
            catch { }
        }

        public static string GetLanguage(IPluginHost? host)
        {
            var val = host?.CustomConfig?.GetString(LanguageKey);
            if (string.IsNullOrWhiteSpace(val))
            {
                return DefaultLanguage;
            }

            if (string.Equals(val, "zh-CN", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(val, "zh", StringComparison.OrdinalIgnoreCase)) return "zh-CN";
            if (string.Equals(val, "zh-TW", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(val, "zh-Hant", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(val, "zh-HK", StringComparison.OrdinalIgnoreCase)) return "zh-TW";
            if (string.Equals(val, "en-US", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(val, "en", StringComparison.OrdinalIgnoreCase)) return "en-US";
            if (string.Equals(val, "ja-JP", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(val, "ja", StringComparison.OrdinalIgnoreCase)) return "ja-JP";
            if (string.Equals(val, "ko-KR", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(val, "ko", StringComparison.OrdinalIgnoreCase)) return "ko-KR";
            if (string.Equals(val, "ru-RU", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(val, "ru", StringComparison.OrdinalIgnoreCase)) return "ru-RU";
            if (string.Equals(val, "es-ES", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(val, "es", StringComparison.OrdinalIgnoreCase)) return "es-ES";
            if (string.Equals(val, "Auto", StringComparison.OrdinalIgnoreCase)) return "Auto";

            return DefaultLanguage;
        }

        public static void SetLanguage(IPluginHost? host, string language)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(language))
                {
                    language = DefaultLanguage;
                }
                else
                {
                    string upper = language.Trim().ToUpperInvariant();
                    if (upper == "ZH-CN" || upper == "ZH") language = "zh-CN";
                    else if (upper == "ZH-TW" || upper == "ZH-HK" || upper == "ZH-HANT") language = "zh-TW";
                    else if (upper == "EN-US" || upper == "EN") language = "en-US";
                    else if (upper == "JA-JP" || upper == "JA") language = "ja-JP";
                    else if (upper == "KO-KR" || upper == "KO") language = "ko-KR";
                    else if (upper == "RU-RU" || upper == "RU") language = "ru-RU";
                    else if (upper == "ES-ES" || upper == "ES") language = "es-ES";
                    else if (upper == "AUTO") language = "Auto";
                    else language = DefaultLanguage;
                }
                host?.CustomConfig?.SetString(LanguageKey, language);
            }
            catch { }
        }

        public static int GetAutoMaskSeconds(IPluginHost? host)
        {
            var val = host?.CustomConfig?.GetString(AutoMaskSecondsKey);
            if (string.IsNullOrWhiteSpace(val))
            {
                return DefaultAutoMaskSeconds;
            }

            if (int.TryParse(val, out int seconds) && seconds >= 3 && seconds <= 300)
            {
                return seconds;
            }

            return DefaultAutoMaskSeconds;
        }

        public static void SetAutoMaskSeconds(IPluginHost? host, int seconds)
        {
            try
            {
                if (seconds < 3 || seconds > 300)
                {
                    seconds = DefaultAutoMaskSeconds;
                }
                host?.CustomConfig?.SetString(AutoMaskSecondsKey, seconds.ToString());
            }
            catch { }
        }
    }
}
