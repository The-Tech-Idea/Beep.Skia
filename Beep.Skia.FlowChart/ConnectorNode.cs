using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Off-page connector: pentagon shape for referencing flow on another page/diagram.
    /// Use matching ConnectorId to link From/To pairs.
    /// </summary>
    public class ConnectorNode : FlowchartControl
    {
        private string _connectorId = "A";
        public string ConnectorId
        {
            get => _connectorId;
            set
            {
                var v = value ?? "A";
                if (!string.Equals(_connectorId, v, System.StringComparison.Ordinal))
                {
                    _connectorId = v;
                    if (NodeProperties.TryGetValue("ConnectorId", out var pi))
                        pi.ParameterCurrentValue = _connectorId;
                    InvalidateVisual();
                }
            }
        }

        private ConnectorDirection _direction = ConnectorDirection.To;
        public ConnectorDirection Direction
        {
            get => _direction;
            set
            {
                if (_direction != value)
                {
                    _direction = value;
                    if (NodeProperties.TryGetValue("Direction", out var pi))
                        pi.ParameterCurrentValue = _direction;
                    // Adjust port counts based on direction
                    EnsurePortCounts(_direction == ConnectorDirection.To ? 1 : 0, _direction == ConnectorDirection.From ? 1 : 0);
                    InvalidateVisual();
                }
            }
        }

        public enum ConnectorDirection
        {
            To,    // Incoming connector (receives flow from this page)
            From   // Outgoing connector (sends flow to another page)
        }

        public ConnectorNode()
        {
            Name = "Flowchart Connector";
            Width = 70;
            Height = 70;
            EnsurePortCounts(1, 0); // Default: To connector

            NodeProperties["ConnectorId"] = new ParameterInfo
            {
                ParameterName = "ConnectorId",
                ParameterType = typeof(string),
                DefaultParameterValue = _connectorId,
                ParameterCurrentValue = _connectorId,
                Description = "Identifier for matching From/To pairs (e.g., 'A', '1', 'Page2')."
            };
            NodeProperties["Direction"] = new ParameterInfo
            {
                ParameterName = "Direction",
                ParameterType = typeof(ConnectorDirection),
                DefaultParameterValue = _direction,
                ParameterCurrentValue = _direction,
                Description = "To = receives flow, From = sends flow to another page",
                Choices = System.Enum.GetNames(typeof(ConnectorDirection))
            };
        }

        protected override void LayoutPorts()
        {
            var r = Bounds;
            float cx = r.MidX;
            float cy = r.MidY;

            // To connector: input port on left
            if (Direction == ConnectorDirection.To && InConnectionPoints.Count > 0)
            {
                var inPt = InConnectionPoints[0];
                inPt.Center = new SKPoint(r.Left, cy);
                inPt.Position = new SKPoint(r.Left - PortRadius, cy);
                inPt.Bounds = new SKRect(
                    inPt.Center.X - PortRadius,
                    inPt.Center.Y - PortRadius,
                    inPt.Center.X + PortRadius,
                    inPt.Center.Y + PortRadius
                );
                inPt.Rect = inPt.Bounds;
            }

            // From connector: output port on right
            if (Direction == ConnectorDirection.From && OutConnectionPoints.Count > 0)
            {
                var outPt = OutConnectionPoints[0];
                outPt.Center = new SKPoint(r.Right, cy);
                outPt.Position = new SKPoint(r.Right + PortRadius, cy);
                outPt.Bounds = new SKRect(
                    outPt.Center.X - PortRadius,
                    outPt.Center.Y - PortRadius,
                    outPt.Center.X + PortRadius,
                    outPt.Center.Y + PortRadius
                );
                outPt.Rect = outPt.Bounds;
            }
        }

        protected override void DrawFlowchartContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var b = Bounds;
            float cx = b.MidX;
            float cy = b.MidY;
            float w = b.Width;
            float h = b.Height;

            // Pentagon points (home plate shape pointing right for From, left for To)
            SKPoint[] points;
            if (Direction == ConnectorDirection.From)
            {
                // Pointing right (outgoing)
                points = new SKPoint[]
                {
                    new SKPoint(b.Left, b.Top),
                    new SKPoint(b.Right - w * 0.3f, b.Top),
                    new SKPoint(b.Right, cy),
                    new SKPoint(b.Right - w * 0.3f, b.Bottom),
                    new SKPoint(b.Left, b.Bottom)
                };
            }
            else
            {
                // Pointing left (incoming)
                points = new SKPoint[]
                {
                    new SKPoint(b.Left + w * 0.3f, b.Top),
                    new SKPoint(b.Right, b.Top),
                    new SKPoint(b.Right, b.Bottom),
                    new SKPoint(b.Left + w * 0.3f, b.Bottom),
                    new SKPoint(b.Left, cy)
                };
            }

            using var fill = new SKPaint { Color = new SKColor(0xFFF9C4), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(0xF9A825), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var text = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            using var font = new SKFont(SKTypeface.Default, 18, 1f, 0f) { Embolden = true };
            using var path = new SKPath();

            path.MoveTo(points[0]);
            for (int i = 1; i < points.Length; i++)
                path.LineTo(points[i]);
            path.Close();

            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);

            // Draw connector ID centered and bold
            var tx = cx - font.MeasureText(ConnectorId, text) / 2;
            var ty = cy + 6;
            canvas.DrawText(ConnectorId, tx, ty, SKTextAlign.Left, font, text);

            DrawPorts(canvas);
        }
    }
}
