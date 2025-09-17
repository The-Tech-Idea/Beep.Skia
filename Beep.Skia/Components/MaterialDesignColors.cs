using SkiaSharp;
using Beep.Skia.Model;
namespace Beep.Skia.Components
{
    /// <summary>
    /// Material Design 3.0 Color Tokens - Public static class for accessing Material Design colors
    /// </summary>
    public static class MaterialDesignColors
    {
        // Primary colors
        public static readonly SKColor Primary = new SKColor(0x67, 0x50, 0xA4); // Purple
        public static readonly SKColor OnPrimary = SKColors.White;
        public static readonly SKColor PrimaryContainer = new SKColor(0xE9, 0xDD, 0xFF);
        public static readonly SKColor OnPrimaryContainer = new SKColor(0x21, 0x00, 0x51);

        // Secondary colors
        public static readonly SKColor Secondary = new SKColor(0x62, 0x5B, 0x71);
        public static readonly SKColor OnSecondary = SKColors.White;
        public static readonly SKColor SecondaryContainer = new SKColor(0xE8, 0xDE, 0xF8);
        public static readonly SKColor OnSecondaryContainer = new SKColor(0x1D, 0x19, 0x23);

        // Tertiary colors
        public static readonly SKColor Tertiary = new SKColor(0x7D, 0x52, 0x60);
        public static readonly SKColor OnTertiary = SKColors.White;
        public static readonly SKColor TertiaryContainer = new SKColor(0xFF, 0xD8, 0xE4);
        public static readonly SKColor OnTertiaryContainer = new SKColor(0x31, 0x10, 0x1D);

        // Error colors
        public static readonly SKColor Error = new SKColor(0xBA, 0x1A, 0x1A);
        public static readonly SKColor OnError = SKColors.White;
        public static readonly SKColor ErrorContainer = new SKColor(0xFF, 0xDA, 0xD6);
        public static readonly SKColor OnErrorContainer = new SKColor(0x41, 0x00, 0x0D);

        // Surface colors
        public static readonly SKColor Surface = new SKColor(0xFF, 0xFB, 0xFE);
        public static readonly SKColor OnSurface = new SKColor(0x1C, 0x1B, 0x1F);
        public static readonly SKColor SurfaceVariant = new SKColor(0xE7, 0xE0, 0xEC);
        public static readonly SKColor OnSurfaceVariant = new SKColor(0x49, 0x45, 0x4F);
        public static readonly SKColor SurfaceContainerHigh = new SKColor(0xF3, 0xED, 0xF4);

        // Outline
        public static readonly SKColor Outline = new SKColor(0x79, 0x75, 0x7E);
        public static readonly SKColor OutlineVariant = new SKColor(0xCA, 0xC4, 0xD0);
    }
}
