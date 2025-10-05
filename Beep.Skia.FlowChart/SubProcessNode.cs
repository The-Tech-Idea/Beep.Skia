using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Sub-process node: rectangle with double border for calling/referencing another process.
    /// Can be collapsed (shows + icon) or expanded to show nested details.
    /// </summary>
    public class SubProcessNode : FlowchartControl
    {
        private string _label = "Sub-Process";
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

        private string _subProcessId = "";
        public string SubProcessId
        {
            get => _subProcessId;
            set
            {
                var v = value ?? "";
                if (!string.Equals(_subProcessId, v, System.StringComparison.Ordinal))
                {
                    _subProcessId = v;
                    if (NodeProperties.TryGetValue("SubProcessId", out var pi))
                        pi.ParameterCurrentValue = _subProcessId;
                    InvalidateVisual();
                }
            }
        }

        private bool _isExpanded = false;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    if (NodeProperties.TryGetValue("IsExpanded", out var pi))
                        pi.ParameterCurrentValue = _isExpanded;
                    InvalidateVisual();
                }
            }
        }

        public SubProcessNode()
        {
            Name = "Flowchart Sub-Process";
            Width = 180;
            Height = 80;
            EnsurePortCounts(1, 1);

            NodeProperties["Label"] = new ParameterInfo
            {
                ParameterName = "Label",
                ParameterType = typeof(string),
                DefaultParameterValue = _label,
                ParameterCurrentValue = _label,
                Description = "Sub-process name/description."
            };
            NodeProperties["SubProcessId"] = new ParameterInfo
            {
                ParameterName = "SubProcessId",
                ParameterType = typeof(string),
                DefaultParameterValue = _subProcessId,
                ParameterCurrentValue = _subProcessId,
                Description = "Reference ID to another diagram/process definition."
            };
            NodeProperties["IsExpanded"] = new ParameterInfo
            {
                ParameterName = "IsExpanded",
                ParameterType = typeof(bool),
                DefaultParameterValue = _isExpanded,
                ParameterCurrentValue = _isExpanded,
                Description = "If true, shows expanded view with nested details."
            };
        }

        protected override void LayoutPorts()
        {
            LayoutPortsVerticalSegments(topInset: 6f, bottomInset: 6f);
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            float innerInset = 4f;
            var innerRect = new SKRect(
                r.Left + innerInset,
                r.Top + innerInset,
                r.Right - innerInset,
                r.Bottom - innerInset
            );

            using var fill = new SKPaint { Color = CustomFillColor ?? new SKColor(0xE0, 0xF2, 0xF1), IsAntialias = true }; // Light teal
            using var stroke = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0x00, 0x96, 0x88), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 }; // Teal
            using var text = new SKPaint { Color = CustomTextColor ?? SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 14);

            // Draw outer rectangle
            canvas.DrawRoundRect(r, CornerRadius, CornerRadius, fill);
            canvas.DrawRoundRect(r, CornerRadius, CornerRadius, stroke);

            // Draw inner rectangle (double border)
            canvas.DrawRoundRect(innerRect, CornerRadius - 2, CornerRadius - 2, stroke);

            // Draw expand/collapse icon if collapsed
            if (!IsExpanded)
            {
                float iconSize = 16f;
                float iconX = r.Right - iconSize - 8;
                float iconY = r.Top + 8;
                
                using var iconPaint = new SKPaint { Color = CustomStrokeColor ?? new SKColor(0x00, 0x96, 0x88), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
                
                // Plus sign
                canvas.DrawLine(iconX + iconSize / 2, iconY, iconX + iconSize / 2, iconY + iconSize, iconPaint);
                canvas.DrawLine(iconX, iconY + iconSize / 2, iconX + iconSize, iconY + iconSize / 2, iconPaint);
            }

            // Draw label centered
            var tx = r.MidX - font.MeasureText(Label, text) / 2;
            var ty = r.MidY + 5;
            canvas.DrawText(Label, tx, ty, SKTextAlign.Left, font, text);

            // Draw subprocess ID in smaller text if provided
            if (!string.IsNullOrWhiteSpace(SubProcessId))
            {
                using var smallFont = new SKFont(SKTypeface.Default, 10);
                using var grayText = new SKPaint { Color = new SKColor(0x60, 0x60, 0x60), IsAntialias = true };
                float idWidth = smallFont.MeasureText(SubProcessId, grayText);
                canvas.DrawText(SubProcessId, r.MidX - idWidth / 2, r.Bottom - 10, SKTextAlign.Left, smallFont, grayText);
            }

            DrawPorts(canvas);
        }
    }
}
