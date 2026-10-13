using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Multi-Document node: stacked documents for multiple document output.
    /// Visual: Multiple overlapping document shapes with curved bottoms.
    /// </summary>
    public class MultiDocumentNode : DirectedFlowchartControl
    {
        private string _label = "Multi-Document";
        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        public string Label
        {
            get => _label;
            set
            {
                var v = value ?? string.Empty;
                if (!string.Equals(_label, v, System.StringComparison.Ordinal))
                {
                    _label = v;
                    if (NodeProperties.TryGetValue("Label", out var pi))
                        pi.ParameterCurrentValue = _label;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Multi-document label.
        /// </summary>
        public MultiDocumentNode()
        {
            Name = "Flowchart Multi-Document";
            Width = 140;
            Height = 90;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Multi-document label."
            };
        }

        protected override void LayoutSidePorts()
        {
            LayoutPortsVerticalSegments(topInset: 6f, bottomInset: 15f);
        }

        private const float WaveHeight = 12f, StackOffset = 6f;

        /// <summary>The two curves of a document's wavy bottom edge, from its right end to its left end.</summary>
        private static ((SKPoint Control1, SKPoint Control2, SKPoint End) First, (SKPoint Control1, SKPoint Control2, SKPoint End) Second) Wave(SKRect doc)
        {
            var width = doc.Width / 3;
            return ((new(doc.Right - width * 0.5f, doc.Bottom), new(doc.Right - width * 1.5f, doc.Bottom - WaveHeight * 2), new(doc.Right - width * 2, doc.Bottom - WaveHeight * 0.5f)),
                (new(doc.Left + width * 0.5f, doc.Bottom), new(doc.Left, doc.Bottom - WaveHeight), new(doc.Left, doc.Bottom - WaveHeight)));
        }

        /// <summary>Where document <paramref name="i"/> of the stack is drawn: 0 at the front, 2 at the back, each one offset
        /// from the one in front and ending higher.</summary>
        private static SKRect Document(SKRect r, int i) =>
            new(r.Left + i * StackOffset, r.Top + i * StackOffset, r.Right + i * StackOffset, r.Bottom - (2 - i) * StackOffset);

        /// <summary>The y of a document's wavy bottom edge at <paramref name="x"/>.</summary>
        private static float WaveY(SKRect doc, float x)
        {
            var (first, second) = Wave(doc);
            var start = new SKPoint(doc.Right, doc.Bottom - WaveHeight);
            return x >= first.End.X ? CubicYAt(start, first.Control1, first.Control2, first.End, x)
                : CubicYAt(first.End, second.Control1, second.Control2, second.End, x);
        }

        /// <summary>Inputs along the top of the front document, outputs along the lowest bottom edge of the stack (the waves
        /// of the three documents cross, so it is not always the one at the back).</summary>
        protected override void LayoutTopBottomPorts()
        {
            var r = Bounds;
            var back = Document(r, 2);
            PlaceAlongEdge(InConnectionPoints, r.Left + FlowInset, r.Right - FlowInset, _ => r.Top, -1f);
            PlaceAlongEdge(OutConnectionPoints, back.Left + FlowInset, back.Right - FlowInset, x =>
            {
                var lowest = float.MinValue;
                for (int i = 0; i < 3; i++)
                {
                    var doc = Document(r, i);
                    if (x >= doc.Left && x <= doc.Right) lowest = MathF.Max(lowest, WaveY(doc, x));
                }
                return lowest;
            }, +1f);
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            const float waveHeight = WaveHeight;

            using var fill = new SKPaint { Color = CustomFillColor ?? SKColors.White, IsAntialias = true };
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0x42, 0x42, 0x42), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Dark gray
            using var text = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            using var font = new SKFont(Beep.Skia.TypefaceCache.Default, 14);

            // Draw 3 stacked documents (back to front)
            for (int i = 2; i >= 0; i--)
            {
                var docRect = Document(r, i);

                using var pathBuilder = new SKPathBuilder();
                pathBuilder.MoveTo(docRect.Left, docRect.Top);
                pathBuilder.LineTo(docRect.Right, docRect.Top);
                pathBuilder.LineTo(docRect.Right, docRect.Bottom - waveHeight);

                // Wavy bottom edge
                var (first, second) = Wave(docRect);
                pathBuilder.CubicTo(first.Control1, first.Control2, first.End);
                pathBuilder.CubicTo(second.Control1, second.Control2, second.End);
                pathBuilder.Close();

                using var path = pathBuilder.Detach();
                canvas.DrawPath(path, fill);
                canvas.DrawPath(path, stroke);
            }

            // Draw label on front document
            var tx = r.Left + 10;
            var ty = r.Top + r.Height / 2;
            canvas.DrawText(Label, tx, ty, SKTextAlign.Left, font, text);

            DrawPorts(canvas);
        }
    }
}
