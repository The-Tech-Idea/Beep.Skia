using System.Collections.Concurrent;
using System;
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
        private static readonly ConcurrentDictionary<(string Family, int Weight, int Width, SKFontStyleSlant Slant), (SKTypeface Typeface, bool Bundled)> Cache = new();

        /// <summary>Default font family used when no family is supplied.</summary>
        public static string DefaultFamily { get; set; } = "Segoe UI";

        /// <summary>
        /// Gets a cached typeface for the family and style, falling back to the platform default
        /// when the family cannot be resolved.
        /// </summary>
        public static SKTypeface Get(string family, SKFontStyle style)
        {
            var resolvedFamily = string.IsNullOrWhiteSpace(family) ? DefaultFamily : family.Trim();
            var key = (resolvedFamily, style.Weight, style.Width, style.Slant);

            return Cache.GetOrAdd(key, _ =>
                (SKTypeface.FromFamilyName(resolvedFamily, style) ?? SKTypeface.Default, false)).Typeface;
        }

        /// <summary>Gets a cached typeface for the family in the normal style.</summary>
        public static SKTypeface Get(string family) => Get(family, SKFontStyle.Normal);
        /// <summary>Registers application-bundled font bytes in the existing cache. Call before rendering that family.
        /// <paramref name="layout"/> chooses nominal glyph advances (the original contract) or HarfBuzz shaping with the
        /// Unicode bidirectional algorithm, which complex scripts such as Arabic require.</summary>
        public static void Register(string family, byte[] bytes, Text.GlyphLayout layout = Text.GlyphLayout.Nominal)
        {
            if (string.IsNullOrWhiteSpace(family) || bytes == null || bytes.Length == 0) throw new ArgumentException("Font family and bytes are required.");
            using var data = SKData.CreateCopy(bytes);
            var typeface = SKTypeface.FromData(data) ?? throw new ArgumentException("Invalid font data.", nameof(bytes));
            var style = typeface.FontStyle;
            var key = (family.Trim(), style.Weight, style.Width, style.Slant);
            Layouts.GetOrAdd(family.Trim(), _ => Text.TextLayout.CreateFamily(bytes, layout));
            while (!Cache.TryAdd(key, (typeface, true)))
            {
                if (Cache.TryGetValue(key, out var existing))
                {
                    if (existing.Bundled) { typeface.Dispose(); return; }
                    // Previously returned platform instances stay alive for their existing callers.
                    if (Cache.TryUpdate(key, (typeface, true), existing)) return;
                }
            }
        }
        private static readonly ConcurrentDictionary<string, Text.TextLayout.Family> Layouts = new();
        /// <summary>The registered layout of a bundled family; registration is required, as for <see cref="GetRequired"/>.</summary>
        internal static Text.TextLayout.Family GetRequiredLayout(string family)
            => Layouts.TryGetValue(family, out var layout) ? layout : throw new InvalidOperationException($"Bundled font '{family}' is not registered.");
        /// <summary>Resolves a required registered font without platform substitution.</summary>
        public static SKTypeface GetRequired(string family)
        {
            var style = SKFontStyle.Normal;
            if (!Cache.TryGetValue((family, style.Weight, style.Width, style.Slant), out var result) || !result.Bundled)
                throw new InvalidOperationException($"Bundled font '{family}' is not registered.");
            return result.Typeface;
        }

        /// <summary>Draws a single validated glyph run as outlines on every canvas, including SVG.</summary>
        public static void DrawGlyphRun(SKCanvas canvas, SKFont font, string text, SKPoint baseline, SKPaint paint)
            => DrawGlyphRunCore(canvas, font, text, baseline, paint, false, out _);

        /// <summary>Draws the original glyph commands and reports their conservative outline bounds.
        /// The caller must add a device-space antialias margin after transforming these bounds.</summary>
        public static void DrawGlyphRun(SKCanvas canvas, SKFont font, string text, SKPoint baseline, SKPaint paint, out SKRect bounds)
            => DrawGlyphRunCore(canvas, font, text, baseline, paint, true, out bounds);

        private static void DrawGlyphRunCore(SKCanvas canvas, SKFont font, string text, SKPoint baseline, SKPaint paint, bool captureBounds, out SKRect bounds)
        {
            var glyphs = font.GetGlyphs(text);
            if (Array.IndexOf(glyphs, (ushort)0) >= 0)
                throw new ArgumentException("The selected font does not support one or more characters.", nameof(text));
            DrawGlyphs(canvas, font, glyphs, font.GetGlyphPositions(glyphs, baseline), paint, captureBounds, out bounds);
        }

        /// <summary>Draws positioned glyphs as outlines (portable to SVG without fonts) and optionally reports their
        /// conservative outline bounds; the caller adds a device-space antialias margin.</summary>
        public static void DrawGlyphs(SKCanvas canvas, SKFont font, ushort[] glyphs, SKPoint[] positions, SKPaint paint, bool captureBounds, out SKRect bounds)
        {
            if (glyphs.Length != positions.Length) throw new ArgumentException("Each glyph needs one position.", nameof(positions));
            bounds = SKRect.Empty;
            for (int i = 0; i < glyphs.Length; i++)
            {
                using var glyph = font.GetGlyphPath(glyphs[i]);
                if (glyph is null) continue; // Spaces have no outline.
                if (captureBounds)
                {
                    var ink = glyph.Bounds;
                    ink.Offset(positions[i]);
                    if (!ink.IsEmpty) bounds = bounds.IsEmpty ? ink : SKRect.Union(bounds, ink);
                }
                canvas.Save();
                try { canvas.Translate(positions[i]); canvas.DrawPath(glyph, paint); }
                finally { canvas.Restore(); }
            }
        }

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
