using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Multi-Document node: stacked documents for multiple document output.
    /// Visual: Multiple overlapping document shapes with curved bottoms.
    /// </summary>
    public class MultiDocumentNode : FlowchartControl
    {
        private string _label = "Multi-Document";
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

        protected override void LayoutPorts()
        {
            LayoutPortsVerticalSegments(topInset: 6f, bottomInset: 15f);
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            float waveHeight = 12f;
            float stackOffset = 6f;

            using var fill = new SKPaint { Color = CustomFillColor ?? SKColors.White, IsAntialias = true };
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0x42, 0x42, 0x42), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Dark gray
            using var text = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);

            // Draw 3 stacked documents (back to front)
            for (int i = 2; i >= 0; i--)
            {
                float offsetX = i * stackOffset;
                float offsetY = i * stackOffset;
                
                var docRect = new SKRect(
                    r.Left + offsetX,
                    r.Top + offsetY,
                    r.Right + offsetX,
                    r.Bottom - (2 - i) * stackOffset
                );

                using var path = new SKPath();
                path.MoveTo(docRect.Left, docRect.Top);
                path.LineTo(docRect.Right, docRect.Top);
                path.LineTo(docRect.Right, docRect.Bottom - waveHeight);

                // Wavy bottom edge
                float waveWidth = docRect.Width / 3;
                path.CubicTo(
                    docRect.Right - waveWidth * 0.5f, docRect.Bottom,
                    docRect.Right - waveWidth * 1.5f, docRect.Bottom - waveHeight * 2,
                    docRect.Right - waveWidth * 2, docRect.Bottom - waveHeight * 0.5f
                );
                path.CubicTo(
                    docRect.Left + waveWidth * 0.5f, docRect.Bottom,
                    docRect.Left, docRect.Bottom - waveHeight,
                    docRect.Left, docRect.Bottom - waveHeight
                );
                path.Close();

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
