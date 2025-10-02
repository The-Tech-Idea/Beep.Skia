using SkiaSharp;

namespace Beep.Skia.DFD
{
    /// <summary>
    /// DFD Process supporting configurable multiple inputs and outputs.
    /// </summary>
    public class DFDProcessMultiIO : DFDControl
    {
        private int _inputs = 2;
        private int _outputs = 2;

        public int Inputs
        {
            get => _inputs;
            set { _inputs = System.Math.Max(0, value); EnsurePortCounts(_inputs, _outputs); InvalidateVisual(); }
        }

        public int Outputs
        {
            get => _outputs;
            set { _outputs = System.Math.Max(0, value); EnsurePortCounts(_inputs, _outputs); InvalidateVisual(); }
        }

        public DFDProcessMultiIO()
        {
            Name = "Process (Multi-IO)";
            DisplayText = "Process";
            TextPosition = Beep.Skia.TextPosition.Below;
            EnsurePortCounts(_inputs, _outputs);
        }

        protected override void LayoutPorts()
        {
            // Rounded rectangle base
            LayoutPortsVerticalSegments(topInset: CornerRadius, bottomInset: CornerRadius);
        }

    protected override void DrawDFDContent(SKCanvas canvas, Beep.Skia.Model.DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var rect = Bounds;
            using var fill = new SKPaint { Color = new SKColor(0xE0, 0xF2, 0xF1), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0x00, 0x96, 0x88), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, fill);
            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, stroke);

            DrawPorts(canvas);
        }
    }
}
