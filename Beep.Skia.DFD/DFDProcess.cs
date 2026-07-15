using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Beep.Skia.DFD
{
    /// <summary>
    /// DFD Process: rounded rectangle node with one input and one output.
    /// Supports drill-down decomposition via ChildDiagramData.
    /// </summary>
    public class DFDProcess : DFDControl
    {
        private string _label = "Process";
        private string _processId = "";

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
        /// Gets or sets a unique process identifier (e.g., "1.0", "2.1") for DFD decomposition numbering.
        /// </summary>
        public string ProcessId
        {
            get => _processId;
            set
            {
                var v = value ?? string.Empty;
                if (!string.Equals(_processId, v, StringComparison.Ordinal))
                {
                    _processId = v;
                    if (NodeProperties.TryGetValue("ProcessId", out var pi))
                        pi.ParameterCurrentValue = _processId;
                }
            }
        }

        /// <summary>
        /// Child diagram DTO for this process's decomposition (Level N+1).
        /// Set this when a child diagram exists.
        /// </summary>
        public Beep.Skia.Serialization.DiagramDto ChildDiagramData { get; set; }

        /// <summary>
        /// Whether a child decomposition diagram exists for this process.
        /// </summary>
        public bool HasChildDiagram => ChildDiagramData != null;

        public DFDProcess()
        {
            Name = "Process";
            DisplayText = "Process";
            TextPosition = Beep.Skia.TextPosition.Below;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new Beep.Skia.Model.ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Text label shown with the process."
            };
            NodeProperties["ProcessId"] = new Beep.Skia.Model.ParameterInfo
            {
                ParameterName = "ProcessId",
                ParameterType = typeof(string),
                DefaultParameterValue = _processId,
                ParameterCurrentValue = _processId,
                Description = "Unique process ID for DFD decomposition numbering."
            };
        }

        protected override void LayoutPorts()
        {
            LayoutPortsVerticalSegments(topInset: CornerRadius, bottomInset: CornerRadius);
        }

        protected override void DrawDFDContent(SKCanvas canvas, Beep.Skia.Model.DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var rect = Bounds;
            using var fill = new SKPaint { Color = HasChildDiagram ? new SKColor(0xE8, 0xF5, 0xE9) : MaterialColors.Surface, IsAntialias = true };
            using var stroke = new SKPaint { Color = MaterialColors.Outline, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };

            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, fill);
            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, stroke);

            // If decomposed, add a small indicator
            if (HasChildDiagram)
            {
                using var indicator = new SKPaint { Color = new SKColor(0x4C, 0xAF, 0x50), IsAntialias = true, Style = SKPaintStyle.Fill };
                canvas.DrawCircle(rect.Right - 8, rect.Bottom - 8, 5, indicator);
                using var plusPaint = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2f };
                canvas.DrawLine(rect.Right - 11, rect.Bottom - 8, rect.Right - 5, rect.Bottom - 8, plusPaint);
                canvas.DrawLine(rect.Right - 8, rect.Bottom - 11, rect.Right - 8, rect.Bottom - 5, plusPaint);
            }

            DrawPorts(canvas);
        }
    }
}
