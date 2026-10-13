using System.Collections.Generic;
using Beep.Skia.Components;
using SkiaSharp;

namespace Beep.Skia.Text
{
    /// <summary>
    /// A block of text in one bundled family and size: paragraphs split at '\n', each wrapped to a width when one is
    /// given, and drawn top-down with every line aligned in its box. Sketch text, sketch labels and table cells use it, so
    /// text sits the same way everywhere.
    /// </summary>
    public static class TextBlock
    {
        /// <summary>The distance between baselines, as a multiple of the font size.</summary>
        public const float LineSpacing = 1.4f;

        /// <summary>The font every text block draws with: outline glyphs at fractional positions, so the layout does not
        /// change with the screen's pixel grid and exports match the screen.</summary>
        public static SKFont Font(string family, float size) => new SKFont(TypefaceCache.GetRequired(family), size)
        { Hinting = SKFontHinting.None, Subpixel = true, LinearMetrics = true, Edging = SKFontEdging.Antialias };

        /// <summary>The laid-out lines of <paramref name="text"/>: each paragraph in turn, wrapped to
        /// <paramref name="maxWidth"/> when it is given (see <see cref="TextLayout.Paragraph"/>).</summary>
        public static List<GlyphLine> Lines(string family, SKFont font, string text, float? maxWidth = null)
        {
            var lines = new List<GlyphLine>();
            foreach (var paragraph in text.Split('\n')) lines.AddRange(TextLayout.Paragraph(family, font, paragraph, maxWidth));
            return lines;
        }

        /// <summary>
        /// Draws <paramref name="lines"/> from the baseline <paramref name="firstBaseline"/> down, <paramref name="lineHeight"/>
        /// apart, each placed between <paramref name="box"/>'s left and right edges by <paramref name="align"/>. With
        /// <paramref name="captureBounds"/>, returns the union of the glyph outlines drawn; otherwise an empty rectangle.
        /// </summary>
        public static SKRect Draw(SKCanvas canvas, SKFont font, IReadOnlyList<GlyphLine> lines, SKRect box, SketchTextAlign align,
            float firstBaseline, float lineHeight, SKPaint paint, bool captureBounds)
        {
            var bounds = SKRect.Empty; var baseline = firstBaseline;
            foreach (var line in lines)
            {
                var left = align switch
                {
                    SketchTextAlign.Left => box.Left,
                    SketchTextAlign.Center => box.MidX - line.Width / 2,
                    SketchTextAlign.Right => box.Right - line.Width,
                };
                TypefaceCache.DrawGlyphs(canvas, font, line.Glyphs, line.PositionsAt(new SKPoint(left, baseline)), paint, captureBounds, out var ink);
                if (captureBounds && !ink.IsEmpty) bounds = bounds.IsEmpty ? ink : SKRect.Union(bounds, ink);
                baseline += lineHeight;
            }
            return bounds;
        }
    }
}
