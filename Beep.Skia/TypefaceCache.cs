using System.Collections.Concurrent;
using SkiaSharp;

namespace Beep.Skia
{
    /// <summary>
    /// Process-wide cache of <see cref="SKTypeface"/> instances.
    ///
    /// Font matching is comparatively expensive and components resolve typefaces while drawing,
    /// so instances are created once per (family, weight, width, slant) and reused for the
    /// lifetime of the process. Cached instances are intentionally never disposed.
    /// </summary>
    public static class TypefaceCache
    {
        private static readonly ConcurrentDictionary<(string Family, int Weight, int Width, SKFontStyleSlant Slant), SKTypeface> Cache =
            new ConcurrentDictionary<(string, int, int, SKFontStyleSlant), SKTypeface>();

        /// <summary>Default font family used when no family is supplied.</summary>
        public const string DefaultFamily = "Segoe UI";

        /// <summary>
        /// Gets a cached typeface for the family and style, falling back to the platform default
        /// when the family cannot be resolved.
        /// </summary>
        public static SKTypeface Get(string family, SKFontStyle style)
        {
            var resolvedFamily = string.IsNullOrWhiteSpace(family) ? DefaultFamily : family.Trim();
            var key = (resolvedFamily, style.Weight, style.Width, style.Slant);

            return Cache.GetOrAdd(key, _ =>
                SKTypeface.FromFamilyName(resolvedFamily, style) ?? SKTypeface.Default);
        }

        /// <summary>Gets a cached typeface for the family in the normal style.</summary>
        public static SKTypeface Get(string family) => Get(family, SKFontStyle.Normal);

        /// <summary>
        /// Gets a cached typeface using explicit style parts (mirrors the four-argument
        /// <c>SKTypeface.FromFamilyName</c> overload).
        /// </summary>
        public static SKTypeface Get(string family, SKFontStyleWeight weight, SKFontStyleWidth width, SKFontStyleSlant slant)
            => Get(family, new SKFontStyle(weight, width, slant));

        /// <summary>The cached default typeface.</summary>
        public static SKTypeface Default => Get(DefaultFamily, SKFontStyle.Normal);

        /// <summary>Number of distinct typefaces currently cached (diagnostics/tests).</summary>
        public static int CachedCount => Cache.Count;
    }
}
