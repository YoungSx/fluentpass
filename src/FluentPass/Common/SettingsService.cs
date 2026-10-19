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
        public const string AutoMaskSecondsKey = "FluentPass_AutoMaskSeconds";

        public const bool DefaultAutoTakeoverUI = true;
        public const WindowCloseAction DefaultCloseAction = WindowCloseAction.RestoreOriginalWindow;
        public const string DefaultAppTheme = "System";
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
