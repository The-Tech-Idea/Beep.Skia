using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.PM
{
    /// <summary>
    /// PM Deliverable node: represents a tangible output or result from a task/phase.
    /// Hexagon shape to distinguish from regular tasks.
    /// </summary>
    public class DeliverableNode : PMControl
    {
        private string _deliverableName = "Deliverable";
        public string DeliverableName
        {
            get => _deliverableName;
            set
            {
                var v = value ?? "";
                if (_deliverableName != v)
                {
                    _deliverableName = v;
                    if (NodeProperties.TryGetValue("DeliverableName", out var p))
                        p.ParameterCurrentValue = _deliverableName;
                    InvalidateVisual();
                }
            }
        }

        private string _deliveryDate = "";
        public string DeliveryDate
        {
            get => _deliveryDate;
            set
            {
                var v = value ?? "";
                if (_deliveryDate != v)
                {
                    _deliveryDate = v;
                    if (NodeProperties.TryGetValue("DeliveryDate", out var p))
                        p.ParameterCurrentValue = _deliveryDate;
                    InvalidateVisual();
                }
            }
        }

        private bool _isApproved = false;
        public bool IsApproved
        {
            get => _isApproved;
            set
            {
                if (_isApproved != value)
                {
                    _isApproved = value;
                    if (NodeProperties.TryGetValue("IsApproved", out var p))
                        p.ParameterCurrentValue = _isApproved;
                    InvalidateVisual();
                }
            }
        }

        public DeliverableNode()
        {
            Name = "PM Deliverable";
            Width = 160;
            Height = 70;
            EnsurePortCounts(1, 1);

            NodeProperties["DeliverableName"] = new ParameterInfo
            {
                ParameterName = "DeliverableName",
                ParameterType = typeof(string),
                DefaultParameterValue = _deliverableName,
                ParameterCurrentValue = _deliverableName,
                Description = "Deliverable name/description"
            };
            NodeProperties["DeliveryDate"] = new ParameterInfo
            {
                ParameterName = "DeliveryDate",
                ParameterType = typeof(string),
                DefaultParameterValue = _deliveryDate,
                ParameterCurrentValue = _deliveryDate,
                Description = "Expected delivery date"
            };
            NodeProperties["IsApproved"] = new ParameterInfo
            {
                ParameterName = "IsApproved",
                ParameterType = typeof(bool),
                DefaultParameterValue = _isApproved,
                ParameterCurrentValue = _isApproved,
                Description = "Approval status"
            };
        }

        protected override void LayoutPorts()
        {
            var r = Bounds;

            // Input port on left side
            if (InConnectionPoints.Count > 0)
            {
                var pt = InConnectionPoints[0];
                pt.Center = new SKPoint(r.Left + r.Width * 0.15f, r.MidY);
                pt.Position = new SKPoint(pt.Center.X - PortRadius, r.MidY);
                pt.Bounds = new SKRect(pt.Center.X - PortRadius, pt.Center.Y - PortRadius, pt.Center.X + PortRadius, pt.Center.Y + PortRadius);
                pt.Rect = pt.Bounds;
                pt.Component = this;
                pt.IsAvailable = true;
            }

            // Output port on right side
            if (OutConnectionPoints.Count > 0)
            {
                var pt = OutConnectionPoints[0];
                pt.Center = new SKPoint(r.Right - r.Width * 0.15f, r.MidY);
                pt.Position = new SKPoint(pt.Center.X + PortRadius, r.MidY);
                pt.Bounds = new SKRect(pt.Center.X - PortRadius, pt.Center.Y - PortRadius, pt.Center.X + PortRadius, pt.Center.Y + PortRadius);
                pt.Rect = pt.Bounds;
                pt.Component = this;
                pt.IsAvailable = true;
            }
        }

        protected override void DrawPMContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            float indent = r.Width * 0.15f;

            // Horizontal hexagon shape
            using var path = new SKPath();
            path.MoveTo(r.Left + indent, r.Top);
            path.LineTo(r.Right - indent, r.Top);
            path.LineTo(r.Right, r.MidY);
            path.LineTo(r.Right - indent, r.Bottom);
            path.LineTo(r.Left + indent, r.Bottom);
            path.LineTo(r.Left, r.MidY);
            path.Close();

            SKColor fillColor = _isApproved 
                ? new SKColor(0xE8, 0xF5, 0xE9)  // Light green if approved
                : new SKColor(0xFF, 0xF3, 0xE0); // Light orange if pending

            using var fill = new SKPaint { Color = fillColor, IsAntialias = true };
            using var stroke = new SKPaint { Color = MaterialColors.Outline, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var text = new SKPaint { Color = MaterialColors.OnSurface, IsAntialias = true };

            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);

            // Draw deliverable icon (document/package)
            float iconX = r.Left + indent + 8;
            float iconY = r.Top + 12;
            float iconSize = 12f;

            using var iconPaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };
            canvas.DrawRect(iconX, iconY, iconSize, iconSize, iconPaint);
            canvas.DrawLine(iconX + iconSize / 3, iconY, iconX + iconSize / 3, iconY + iconSize, iconPaint);
            canvas.DrawLine(iconX, iconY + iconSize / 2, iconX + iconSize, iconY + iconSize / 2, iconPaint);

            // Draw deliverable name
            using var nameFont = new SKFont(SKTypeface.Default, 12);
            float nameWidth = nameFont.MeasureText(DeliverableName, text);
            canvas.DrawText(DeliverableName, r.MidX - nameWidth / 2, r.MidY - 2, SKTextAlign.Left, nameFont, text);

            // Draw delivery date
            if (!string.IsNullOrWhiteSpace(DeliveryDate))
            {
                using var dateFont = new SKFont(SKTypeface.Default, 9);
                using var grayText = new SKPaint { Color = new SKColor(0x70, 0x70, 0x70), IsAntialias = true };
                float dateWidth = dateFont.MeasureText(DeliveryDate, grayText);
                canvas.DrawText(DeliveryDate, r.MidX - dateWidth / 2, r.MidY + 12, SKTextAlign.Left, dateFont, grayText);
            }

            // Draw approval checkmark if approved
            if (_isApproved)
            {
                float checkX = r.Right - indent - 16;
                float checkY = r.Top + 12;
                using var checkPaint = new SKPaint { Color = new SKColor(0x43, 0xA0, 0x47), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
                canvas.DrawLine(checkX, checkY + 4, checkX + 3, checkY + 7, checkPaint);
                canvas.DrawLine(checkX + 3, checkY + 7, checkX + 8, checkY, checkPaint);
            }

            DrawPorts(canvas);
        }
    }
}
