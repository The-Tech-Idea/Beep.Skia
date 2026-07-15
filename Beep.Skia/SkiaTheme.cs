using SkiaSharp;

namespace Beep.Skia
{
    /// <summary>
    /// Holds all Material Design 3.0 color tokens for a theme.
    /// Supports light and dark mode via IsDark flag.
    /// </summary>
    public class SkiaTheme
    {
        public string Name { get; set; } = "Default";

        // Primary
        public SKColor Primary { get; set; } = new SKColor(0x67, 0x50, 0xA4);
        public SKColor OnPrimary { get; set; } = SKColors.White;
        public SKColor PrimaryContainer { get; set; } = new SKColor(0xE9, 0xDD, 0xFF);
        public SKColor OnPrimaryContainer { get; set; } = new SKColor(0x21, 0x00, 0x51);

        // Secondary
        public SKColor Secondary { get; set; } = new SKColor(0x62, 0x5B, 0x71);
        public SKColor OnSecondary { get; set; } = SKColors.White;
        public SKColor SecondaryContainer { get; set; } = new SKColor(0xE8, 0xDE, 0xF8);
        public SKColor OnSecondaryContainer { get; set; } = new SKColor(0x1D, 0x19, 0x23);

        // Tertiary
        public SKColor Tertiary { get; set; } = new SKColor(0x7D, 0x52, 0x60);
        public SKColor OnTertiary { get; set; } = SKColors.White;
        public SKColor TertiaryContainer { get; set; } = new SKColor(0xFF, 0xD8, 0xE4);
        public SKColor OnTertiaryContainer { get; set; } = new SKColor(0x31, 0x10, 0x1D);

        // Error
        public SKColor Error { get; set; } = new SKColor(0xBA, 0x1A, 0x1A);
        public SKColor OnError { get; set; } = SKColors.White;
        public SKColor ErrorContainer { get; set; } = new SKColor(0xFF, 0xDA, 0xD6);
        public SKColor OnErrorContainer { get; set; } = new SKColor(0x41, 0x00, 0x0D);

        // Surface
        public SKColor Surface { get; set; } = new SKColor(0xFF, 0xFB, 0xFE);
        public SKColor OnSurface { get; set; } = new SKColor(0x1C, 0x1B, 0x1F);
        public SKColor SurfaceVariant { get; set; } = new SKColor(0xE7, 0xE0, 0xEC);
        public SKColor OnSurfaceVariant { get; set; } = new SKColor(0x49, 0x45, 0x4F);
        public SKColor SurfaceContainer { get; set; } = new SKColor(0xF3, 0xED, 0xF4);
        public SKColor SurfaceContainerHigh { get; set; } = new SKColor(0xF3, 0xED, 0xF4);

        // Outline
        public SKColor Outline { get; set; } = new SKColor(0x79, 0x75, 0x7E);
        public SKColor OutlineVariant { get; set; } = new SKColor(0xCA, 0xC4, 0xD0);

        // Canvas background color
        public SKColor CanvasBackground { get; set; } = SKColors.White;
        public SKColor GridColor { get; set; } = new SKColor(0xE0, 0xE0, 0xE0);

        /// <summary>
        /// Creates a light theme (default).
        /// </summary>
        public static SkiaTheme Light => new SkiaTheme { Name = "Light" };

        /// <summary>
        /// Creates a dark theme with inverted surface/on-surface colors.
        /// </summary>
        public static SkiaTheme Dark => new SkiaTheme
        {
            Name = "Dark",
            Primary = new SKColor(0xD0, 0xBC, 0xFF),
            OnPrimary = new SKColor(0x38, 0x13, 0x72),
            PrimaryContainer = new SKColor(0x4F, 0x37, 0x8B),
            OnPrimaryContainer = new SKColor(0xE9, 0xDD, 0xFF),
            Secondary = new SKColor(0xCC, 0xC2, 0xDC),
            OnSecondary = new SKColor(0x33, 0x2D, 0x41),
            SecondaryContainer = new SKColor(0x4A, 0x44, 0x58),
            OnSecondaryContainer = new SKColor(0xE8, 0xDE, 0xF8),
            Tertiary = new SKColor(0xEF, 0xB8, 0xC8),
            OnTertiary = new SKColor(0x49, 0x29, 0x33),
            TertiaryContainer = new SKColor(0x63, 0x3F, 0x49),
            OnTertiaryContainer = new SKColor(0xFF, 0xD8, 0xE4),
            Error = new SKColor(0xFF, 0xB4, 0xAB),
            OnError = new SKColor(0x69, 0x00, 0x05),
            ErrorContainer = new SKColor(0x93, 0x00, 0x0A),
            OnErrorContainer = new SKColor(0xFF, 0xDA, 0xD6),
            Surface = new SKColor(0x1C, 0x1B, 0x1F),
            OnSurface = new SKColor(0xE6, 0xE1, 0xE5),
            SurfaceVariant = new SKColor(0x49, 0x45, 0x4F),
            OnSurfaceVariant = new SKColor(0xCA, 0xC4, 0xD0),
            SurfaceContainer = new SKColor(0x21, 0x1F, 0x24),
            SurfaceContainerHigh = new SKColor(0x28, 0x25, 0x2B),
            Outline = new SKColor(0x93, 0x8F, 0x99),
            OutlineVariant = new SKColor(0x49, 0x45, 0x4F),
            CanvasBackground = new SKColor(0x1C, 0x1B, 0x1F),
            GridColor = new SKColor(0x2C, 0x2C, 0x2F)
        };

        /// <summary>
        /// High-contrast theme for accessibility.
        /// </summary>
        public static SkiaTheme HighContrast => new SkiaTheme
        {
            Name = "HighContrast",
            Primary = new SKColor(0x00, 0x00, 0xFF),
            OnPrimary = SKColors.White,
            Surface = SKColors.White,
            OnSurface = SKColors.Black,
            Outline = SKColors.Black,
            CanvasBackground = SKColors.White,
            GridColor = new SKColor(0xC0, 0xC0, 0xC0)
        };

        /// <summary>
        /// Nord theme — cool blue/gray palette.
        /// </summary>
        public static SkiaTheme Nord => new SkiaTheme
        {
            Name = "Nord",
            Primary = new SKColor(0x5E, 0x81, 0xAC),
            OnPrimary = SKColors.White,
            Surface = new SKColor(0xEC, 0xEF, 0xF4),
            OnSurface = new SKColor(0x2E, 0x34, 0x40),
            SurfaceContainer = new SKColor(0xE5, 0xE9, 0xF0),
            SurfaceContainerHigh = new SKColor(0xD8, 0xDE, 0xE9),
            Outline = new SKColor(0x81, 0xA1, 0xC1),
            CanvasBackground = new SKColor(0xEC, 0xEF, 0xF4),
            GridColor = new SKColor(0xD8, 0xDE, 0xE9)
        };

        /// <summary>
        /// Dracula theme — dark purple/green palette.
        /// </summary>
        public static SkiaTheme Dracula => new SkiaTheme
        {
            Name = "Dracula",
            Primary = new SKColor(0xBD, 0x93, 0xF9),
            OnPrimary = new SKColor(0x28, 0x2A, 0x36),
            Secondary = new SKColor(0x50, 0xFA, 0x7B),
            OnSecondary = new SKColor(0x28, 0x2A, 0x36),
            Surface = new SKColor(0x28, 0x2A, 0x36),
            OnSurface = new SKColor(0xF8, 0xF8, 0xF2),
            SurfaceVariant = new SKColor(0x38, 0x3A, 0x46),
            OnSurfaceVariant = new SKColor(0xBB, 0xBB, 0xCC),
            SurfaceContainer = new SKColor(0x32, 0x34, 0x40),
            SurfaceContainerHigh = new SKColor(0x38, 0x3A, 0x46),
            Outline = new SKColor(0x62, 0x72, 0xA4),
            OutlineVariant = new SKColor(0x44, 0x47, 0x5A),
            Error = new SKColor(0xFF, 0x55, 0x55),
            CanvasBackground = new SKColor(0x28, 0x2A, 0x36),
            GridColor = new SKColor(0x38, 0x3A, 0x46)
        };

        public SkiaTheme Clone()
        {
            return (SkiaTheme)MemberwiseClone();
        }
    }
}
