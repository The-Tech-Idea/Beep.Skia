using System;
using System.Collections.Generic;
using System.Text;
using SkiaSharp;

namespace Beep.Skia.MindMap
{
    /// <summary>
    /// A formatted span of mind-map note text.
    /// </summary>
    public class RichTextSpan
    {
        public string Text { get; set; } = string.Empty;
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool IsBullet { get; set; }
        public bool IsLineBreak { get; set; }
    }

    /// <summary>
    /// Minimal rich-text support for mind-map notes: **bold**, *italic*, bullet lines
    /// ("- item" / "* item"), automatic word wrapping, and line breaks.
    /// </summary>
    public static class MindMapRichText
    {
        /// <summary>
        /// Parses rich text into formatted spans (line breaks included).
        /// </summary>
        public static List<RichTextSpan> Parse(string text)
        {
            var spans = new List<RichTextSpan>();
            if (string.IsNullOrEmpty(text)) return spans;

            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            foreach (var rawLine in lines)
            {
                var line = rawLine;
                bool bullet = false;

                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
                {
                    bullet = true;
                    line = trimmed.Substring(2);
                }

                ParseInline(line, bullet, spans);
            }

            return spans;
        }

        /// <summary>
        /// Draws rich text inside the given bounds with word wrapping.
        /// </summary>
        public static void Draw(SKCanvas canvas, string text, SKRect bounds, SKColor color, float fontSize = 11f, float lineHeight = 15f)
        {
            if (canvas == null || string.IsNullOrWhiteSpace(text)) return;

            var spans = Parse(text);
            using var regularFont = new SKFont(SKTypeface.Default, fontSize);
            using var boldFont = new SKFont(SKTypeface.Default, fontSize) { Embolden = true };
            using var italicFont = new SKFont(TypefaceCache.Get("Segoe UI", SKFontStyle.Italic), fontSize);
            using var paint = new SKPaint { Color = color, IsAntialias = true };

            float x = bounds.Left;
            float y = bounds.Top + fontSize;

            foreach (var span in spans)
            {
                if (span.IsLineBreak)
                {
                    x = bounds.Left;
                    y += lineHeight;
                    continue;
                }
                if (y > bounds.Bottom) return;

                var font = span.Bold ? boldFont : span.Italic ? italicFont : regularFont;

                if (span.IsBullet)
                {
                    canvas.DrawText("•", x, y, SKTextAlign.Left, font, paint);
                    x += 10f;
                }

                var words = span.Text.Split(' ');
                for (int i = 0; i < words.Length; i++)
                {
                    var word = i < words.Length - 1 ? words[i] + " " : words[i];
                    if (word.Length == 0) continue;

                    float width = font.MeasureText(word);
                    if (x + width > bounds.Right && x > bounds.Left)
                    {
                        x = bounds.Left;
                        y += lineHeight;
                        if (y > bounds.Bottom) return;
                    }

                    canvas.DrawText(word, x, y, SKTextAlign.Left, font, paint);
                    x += width;
                }
            }
        }

        private static void ParseInline(string line, bool bullet, List<RichTextSpan> spans)
        {
            var buffer = new StringBuilder();
            bool bold = false;
            bool italic = false;
            bool firstSpan = true;

            void Flush()
            {
                if (buffer.Length == 0) return;
                spans.Add(new RichTextSpan
                {
                    Text = buffer.ToString(),
                    Bold = bold,
                    Italic = italic,
                    IsBullet = bullet && firstSpan
                });
                buffer.Clear();
                firstSpan = false;
            }

            int i = 0;
            while (i < line.Length)
            {
                if (i + 1 < line.Length && line[i] == '*' && line[i + 1] == '*')
                {
                    Flush();
                    bold = !bold;
                    i += 2;
                    continue;
                }
                if (line[i] == '*')
                {
                    Flush();
                    italic = !italic;
                    i++;
                    continue;
                }

                buffer.Append(line[i]);
                i++;
            }
            Flush();

            if (firstSpan && bullet)
            {
                spans.Add(new RichTextSpan { Text = string.Empty, IsBullet = true });
            }

            spans.Add(new RichTextSpan { IsLineBreak = true });
        }
    }
}
