using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FlexToolBar.Avalonia
{
    public static class ToolBarThemeManager
    {
        private static readonly Dictionary<string, Uri> _themeRegistry = new(StringComparer.OrdinalIgnoreCase);
        
        public static ObservableCollection<string> AvailableThemes { get; } = new() { "Default" };

        static ToolBarThemeManager()
        {
            // Core library out-of-the-box pre-installed definitions
            RegisterThemeInternal("Compact", new Uri("avares://FlexToolBar.Avalonia/Themes/Compact.ToolBar.Theme.axaml"));
            RegisterThemeInternal("Green", new Uri("avares://FlexToolBar.Avalonia/Themes/Green.ToolBar.Theme.axaml"));
        }

        private static void RegisterThemeInternal(string themeName, Uri assetUri)
        {
            if (string.IsNullOrEmpty(themeName) || assetUri == null) return;
            
            _themeRegistry[themeName] = assetUri;
            if (!AvailableThemes.Contains(themeName))
            {
                AvailableThemes.Add(themeName);
            }
        }

        // PUBLIC API: Allows application developers to safely register custom themes anytime before or during UI rendering
        public static void RegisterTheme(string themeName, string avaresPath)
        {
            try
            {
                if (string.IsNullOrEmpty(themeName) || string.IsNullOrEmpty(avaresPath)) return;
                RegisterThemeInternal(themeName, new Uri(avaresPath));
            }
            catch { }
        }

        public static bool TryGetThemeUri(string themeName, out Uri? targetUri)
        {
            return _themeRegistry.TryGetValue(themeName, out targetUri);
        }
    }
}
