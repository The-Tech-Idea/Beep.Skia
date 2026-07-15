using System;
using System.Collections.Generic;

namespace Beep.Skia
{
    /// <summary>
    /// Central theme manager that controls the active theme across all diagram components.
    /// Components read theme tokens from ThemeManager.Current.
    /// </summary>
    public static class ThemeManager
    {
        private static SkiaTheme _current = SkiaTheme.Light;

        /// <summary>
        /// Gets or sets the current active theme. Setting this fires ThemeChanged.
        /// </summary>
        public static SkiaTheme Current
        {
            get => _current ?? SkiaTheme.Light;
            set
            {
                if (_current == value) return;
                _current = value ?? SkiaTheme.Light;
                ThemeChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Fires when the active theme changes. Components subscribe to redraw.
        /// </summary>
        public static event EventHandler ThemeChanged;

        /// <summary>
        /// Applies a named theme. Built-in names: Light, Dark, HighContrast, Nord, Dracula.
        /// </summary>
        public static void ApplyTheme(string name)
        {
            Current = name?.ToLowerInvariant() switch
            {
                "dark" => SkiaTheme.Dark,
                "highcontrast" => SkiaTheme.HighContrast,
                "nord" => SkiaTheme.Nord,
                "dracula" => SkiaTheme.Dracula,
                _ => SkiaTheme.Light
            };
        }

        /// <summary>
        /// Gets a list of built-in theme names.
        /// </summary>
        public static IReadOnlyList<string> BuiltInThemes => new[] { "Light", "Dark", "HighContrast", "Nord", "Dracula" };
    }
}
