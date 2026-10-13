using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using HarfBuzzSharp;
using SkiaSharp;

namespace Beep.Skia.Text
{
    /// <summary>How a bundled font family places glyphs.</summary>
    public enum GlyphLayout
    {
        /// <summary>One glyph per character at its nominal advance, left to right: the original bundled-font contract.</summary>
        Nominal,
        /// <summary>HarfBuzz shaping of each directional run, ordered by the Unicode bidirectional algorithm.</summary>
        Shaped,
    }

    /// <summary>One laid-out line in visual order: glyph IDs, pen advances and offsets from the pen.</summary>
    public sealed class GlyphLine
    {
        private readonly float[] _advances;
        private readonly SKPoint[] _offsets;
        internal GlyphLine(ushort[] glyphs, float[] advances, SKPoint[] offsets, float width)
        { Glyphs = glyphs; _advances = advances; _offsets = offsets; Width = width; }
        public ushort[] Glyphs { get; }
        /// <summary>Total advance of the line.</summary>
        public float Width { get; }
        /// <summary>Glyph positions for a line whose left edge is at <paramref name="origin"/> on the baseline. The pen
        /// accumulates from the origin exactly as <see cref="SKFont.GetGlyphPositions(System.ReadOnlySpan{ushort}, SKPoint)"/>
        /// does, so nominal lines match Skia's own positions bit for bit.</summary>
        public SKPoint[] PositionsAt(SKPoint origin)
        {
            var result = new SKPoint[Glyphs.Length]; var pen = origin.X;
            for (int i = 0; i < result.Length; i++) { result[i] = new SKPoint(pen + _offsets[i].X, origin.Y + _offsets[i].Y); pen += _advances[i]; }
            return result;
        }
    }

    /// <summary>Lays out plain-text paragraphs in a registered bundled font (see <see cref="TypefaceCache.Register"/>).</summary>
    public static class TextLayout
    {
        internal sealed class Family
        {
            // The blob and face are held for the lifetime of the process together with the font that references them.
            private readonly Blob _blob;
            private readonly Face _face;
            internal Family(GlyphLayout layout, Blob blob, Face face, HarfBuzzSharp.Font shaper)
            { Layout = layout; _blob = blob; _face = face; Shaper = shaper; UnitsPerEm = face?.UnitsPerEm ?? 0; }
            internal GlyphLayout Layout { get; }
            internal HarfBuzzSharp.Font Shaper { get; }
            internal int UnitsPerEm { get; }
        }

        internal static Family CreateFamily(byte[] bytes, GlyphLayout layout) => layout switch
        {
            GlyphLayout.Nominal => new Family(layout, null, null, null),
            GlyphLayout.Shaped => CreateShapedFamily(bytes),
        };
        private static Family CreateShapedFamily(byte[] bytes)
        {
            // HarfBuzz reads the font in place for the life of the face. Blob.FromStream pins a managed copy only while it
            // is constructed, leaving the face on memory the garbage collector may free and reuse; the font is copied to
            // native memory that the blob owns and releases instead.
            var memory = Marshal.AllocHGlobal(bytes.Length);
            Blob blob = null;
            try
            {
                Marshal.Copy(bytes, 0, memory, bytes.Length);
                blob = new Blob(memory, bytes.Length, MemoryMode.ReadOnly, () => Marshal.FreeHGlobal(memory));
            }
            finally
            {
                // Until the blob owns the allocation, nothing else will release it.
                if (blob == null) Marshal.FreeHGlobal(memory);
            }
            blob.MakeImmutable();
            var face = new Face(blob, 0);
            if (face.UnitsPerEm <= 0) throw new ArgumentException("Invalid font data.", nameof(bytes));
            face.MakeImmutable();
            var shaper = new HarfBuzzSharp.Font(face);
            shaper.SetFunctionsOpenType();
            shaper.SetScale(face.UnitsPerEm, face.UnitsPerEm);
            return new Family(GlyphLayout.Shaped, blob, face, shaper);
        }

        /// <summary>
        /// Lays out one paragraph (text without line breaks) in <paramref name="family"/> at the font's size. With
        /// <paramref name="maxWidth"/>, lines break greedily after spaces, and a word wider than the line breaks between
        /// grapheme clusters; trailing spaces of wrapped lines are not drawn or measured. Without it, the paragraph is one
        /// line. Unsupported characters throw <see cref="ArgumentException"/>; nothing is substituted.
        /// </summary>
        public static IReadOnlyList<GlyphLine> Paragraph(string family, SKFont font, string text, float? maxWidth = null)
        {
            if (font == null) throw new ArgumentNullException(nameof(font));
            if (text == null || text.IndexOf('\n') >= 0 || text.IndexOf('\r') >= 0) throw new ArgumentException("Lay out one paragraph at a time.", nameof(text));
            if (maxWidth is { } limit && (!float.IsFinite(limit) || limit <= 0)) throw new ArgumentOutOfRangeException(nameof(maxWidth));
            var registered = TypefaceCache.GetRequiredLayout(family);
            return registered.Layout switch
            {
                GlyphLayout.Nominal => Lines(text, maxWidth, (s, e) => font.MeasureText(text.Substring(s, e - s)), (s, e) => Nominal(font, text.Substring(s, e - s))),
                GlyphLayout.Shaped => new Shaping(registered, font, text).Lines(maxWidth),
            };
        }

        /// <summary>
        /// Draws one line of text (a label) in <paramref name="family"/>, centred horizontally on
        /// <paramref name="centerBaseline"/>, as outlines. A nominal family draws exactly as
        /// <see cref="TypefaceCache.DrawGlyphRun(SKCanvas, SKFont, string, SKPoint, SKPaint)"/> from
        /// <c>x - MeasureText / 2</c>; a shaped family draws its shaped runs in visual order.
        /// </summary>
        public static void DrawCenteredLine(SKCanvas canvas, string family, SKFont font, string text, SKPoint centerBaseline, SKPaint paint)
        {
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            foreach (var line in Paragraph(family, font, text))
                TypefaceCache.DrawGlyphs(canvas, font, line.Glyphs, line.PositionsAt(new SKPoint(centerBaseline.X - line.Width / 2, centerBaseline.Y)), paint, false, out _);
        }

        /// <summary>
        /// The advance of one line of text in <paramref name="family"/> and its ink bounds relative to a pen at the origin
        /// on the baseline. A nominal family reports exactly <see cref="SKFont.MeasureText(string, out SKRect, SKPaint)"/>;
        /// a shaped family reports the shaped advance and the union of its positioned glyph outlines.
        /// </summary>
        public static float MeasureLine(string family, SKFont font, string text, out SKRect ink)
        {
            if (font == null) throw new ArgumentNullException(nameof(font));
            if (text == null) throw new ArgumentNullException(nameof(text));
            var registered = TypefaceCache.GetRequiredLayout(family);
            return registered.Layout switch
            {
                GlyphLayout.Nominal => font.MeasureText(text, out ink),
                GlyphLayout.Shaped => MeasureShaped(family, font, text, out ink),
            };
        }
        private static float MeasureShaped(string family, SKFont font, string text, out SKRect ink)
        {
            ink = SKRect.Empty; float width = 0;
            foreach (var line in Paragraph(family, font, text))
            {
                var positions = line.PositionsAt(SKPoint.Empty);
                for (int i = 0; i < line.Glyphs.Length; i++)
                {
                    using var glyph = font.GetGlyphPath(line.Glyphs[i]);
                    if (glyph is null) continue; // Spaces have no outline.
                    var bounds = glyph.Bounds; bounds.Offset(positions[i]);
                    if (!bounds.IsEmpty) ink = ink.IsEmpty ? bounds : SKRect.Union(ink, bounds);
                }
                width = Math.Max(width, line.Width);
            }
            return width;
        }

        /// <summary>True when <paramref name="family"/> can lay out every paragraph of <paramref name="text"/> (lines
        /// separated by '\n'): each character has a glyph, and shaped families get no explicit directional controls.</summary>
        public static bool Supports(string family, string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            var registered = TypefaceCache.GetRequiredLayout(family);
            var content = text.Replace("\n", "");
            return registered.Layout switch
            {
                GlyphLayout.Nominal => NominalSupports(family, content),
                GlyphLayout.Shaped => ShapedSupports(registered, content),
            };
        }
        private static bool NominalSupports(string family, string content)
        {
            using var font = new SKFont(TypefaceCache.GetRequired(family));
            return font.ContainsGlyphs(content);
        }
        private static bool ShapedSupports(Family family, string content)
        {
            for (int i = 0; i < content.Length; i++)
            {
                int codePoint;
                if (char.IsHighSurrogate(content[i]) && i + 1 < content.Length && char.IsLowSurrogate(content[i + 1])) codePoint = char.ConvertToUtf32(content[i], content[++i]);
                else if (char.IsSurrogate(content[i]) || content[i] == '\r') return false;
                else codePoint = content[i];
                if (!UnicodeBidi.Supports(codePoint)) return false;
                if (UnicodeBidi.GetClass(codePoint) != BidiClass.BN && !family.Shaper.TryGetNominalGlyph((uint)codePoint, out _)) return false;
            }
            return true;
        }

        private static GlyphLine Nominal(SKFont font, string text)
        {
            var glyphs = font.GetGlyphs(text);
            if (Array.IndexOf(glyphs, (ushort)0) >= 0) throw new ArgumentException("The selected font does not support one or more characters.", nameof(text));
            return new GlyphLine(glyphs, font.GetGlyphWidths(glyphs), new SKPoint[glyphs.Length], font.MeasureText(text));
        }

        private static List<GlyphLine> Lines(string text, float? maxWidth, Func<int, int, float> measure, Func<int, int, GlyphLine> layout)
        {
            var result = new List<GlyphLine>();
            foreach (var (start, end) in Breaks(text, maxWidth, measure)) result.Add(layout(start, end));
            return result;
        }

        /// <summary>Greedy line breaking over UTF-16 ranges; each range excludes the spaces it was broken at.</summary>
        private static List<(int Start, int End)> Breaks(string text, float? maxWidth, Func<int, int, float> measure)
        {
            var lines = new List<(int, int)>();
            if (maxWidth is not { } limit || text.Length == 0) { lines.Add((0, text.Length)); return lines; }
            int[] clusters = null;
            int start = 0;
            while (start < text.Length)
            {
                int lineEnd = -1, next = text.Length, i = start;
                while (i < text.Length)
                {
                    int wordEnd = i; while (wordEnd < text.Length && text[wordEnd] != ' ') wordEnd++;
                    int spaceEnd = wordEnd; while (spaceEnd < text.Length && text[spaceEnd] == ' ') spaceEnd++;
                    if (measure(start, wordEnd) <= limit) { lineEnd = wordEnd; next = spaceEnd; i = spaceEnd; continue; }
                    if (lineEnd < 0)
                    {
                        // A word wider than the line breaks between grapheme clusters, keeping at least one.
                        clusters ??= StringInfo.ParseCombiningCharacters(text);
                        int fit = -1;
                        foreach (var boundary in clusters)
                        {
                            if (boundary <= start) continue;
                            if (boundary > wordEnd || measure(start, boundary) > limit) break;
                            fit = boundary;
                        }
                        if (fit < 0) { fit = wordEnd; foreach (var boundary in clusters) if (boundary > start) { fit = Math.Min(boundary, wordEnd); break; } }
                        lineEnd = fit; next = fit;
                    }
                    break;
                }
                if (lineEnd < 0) { lineEnd = text.Length; next = text.Length; }
                lines.Add((start, lineEnd)); start = next;
            }
            return lines;
        }

        /// <summary>Bidi resolution and HarfBuzz shaping for one paragraph.</summary>
        private sealed class Shaping
        {
            private static readonly Language Arabic = new("ar"), English = new("en");
            private readonly Family _family;
            private readonly SKFont _font;
            private readonly string _text;
            private readonly int[] _codePoints, _unitToCodePoint;
            private readonly byte[] _levels;
            private readonly byte _paragraphLevel;

            internal Shaping(Family family, SKFont font, string text)
            {
                _family = family; _font = font; _text = text;
                var codePoints = new List<int>(text.Length); _unitToCodePoint = new int[text.Length];
                for (int i = 0; i < text.Length; i++)
                {
                    _unitToCodePoint[i] = codePoints.Count;
                    int codePoint;
                    if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                    { codePoint = char.ConvertToUtf32(text[i], text[i + 1]); _unitToCodePoint[++i] = codePoints.Count; }
                    else if (char.IsSurrogate(text[i])) throw new ArgumentException("Text contains an unpaired surrogate.", nameof(text));
                    else codePoint = text[i];
                    if (!UnicodeBidi.Supports(codePoint))
                        throw new ArgumentException("Directional formatting characters, tabs and paragraph separators are not supported.", nameof(text));
                    // Default-ignorable joiners and marks (Bidi_Class BN) are handled by HarfBuzz even without a mapped glyph.
                    if (UnicodeBidi.GetClass(codePoint) != BidiClass.BN && !family.Shaper.TryGetNominalGlyph((uint)codePoint, out _))
                        throw new ArgumentException("The selected font does not support one or more characters.", nameof(text));
                    codePoints.Add(codePoint);
                }
                _codePoints = codePoints.ToArray();
                _levels = UnicodeBidi.ResolveLevels(_codePoints, null, out _paragraphLevel);
            }

            internal List<GlyphLine> Lines(float? maxWidth) => TextLayout.Lines(_text, maxWidth, (s, e) => Layout(s, e).Width, Layout);

            /// <summary>Lays out the UTF-16 range [start, end) as one line: L1 levels, directional runs shaped by
            /// HarfBuzz in their own direction, then L2 run order.</summary>
            private GlyphLine Layout(int start, int end)
            {
                if (start == end) return new GlyphLine(Array.Empty<ushort>(), Array.Empty<float>(), Array.Empty<SKPoint>(), 0);
                int first = _unitToCodePoint[start], last = _unitToCodePoint[end - 1];
                var levels = UnicodeBidi.LineLevels(_codePoints, _levels, first, last - first + 1, _paragraphLevel);
                var runs = new List<(int Start, int End, byte Level)>();
                for (int unit = start; unit < end;)
                {
                    var level = levels[_unitToCodePoint[unit] - first];
                    int runEnd = unit + 1;
                    while (runEnd < end && levels[_unitToCodePoint[runEnd] - first] == level) runEnd++;
                    runs.Add((unit, runEnd, level)); unit = runEnd;
                }
                var runLevels = new byte[runs.Count];
                for (int i = 0; i < runs.Count; i++) runLevels[i] = runs[i].Level;
                var glyphs = new List<ushort>(); var advances = new List<float>(); var offsets = new List<SKPoint>();
                float scale = _font.Size / _family.UnitsPerEm, pen = 0;
                foreach (var index in UnicodeBidi.VisualOrder(runLevels))
                {
                    var (runStart, runEnd, level) = runs[index];
                    using var buffer = new HarfBuzzSharp.Buffer();
                    buffer.AddUtf16(_text, runStart, runEnd - runStart); // The rest of the paragraph is shaping context.
                    buffer.Direction = level % 2 == 1 ? Direction.RightToLeft : Direction.LeftToRight;
                    buffer.GuessSegmentProperties();
                    // A fixed language keeps shaping identical on every platform instead of following the process locale.
                    buffer.Language = buffer.Script == Script.Arabic ? Arabic : English;
                    _family.Shaper.Shape(buffer);
                    var infos = buffer.GlyphInfos; var placements = buffer.GlyphPositions;
                    for (int i = 0; i < infos.Length; i++)
                    {
                        if (infos[i].Codepoint == 0 || infos[i].Codepoint > ushort.MaxValue)
                            throw new ArgumentException("The selected font does not support one or more characters.");
                        glyphs.Add((ushort)infos[i].Codepoint);
                        offsets.Add(new SKPoint(placements[i].XOffset * scale, -placements[i].YOffset * scale));
                        advances.Add(placements[i].XAdvance * scale); pen += placements[i].XAdvance * scale;
                    }
                }
                return new GlyphLine(glyphs.ToArray(), advances.ToArray(), offsets.ToArray(), pen);
            }
        }
    }
}
