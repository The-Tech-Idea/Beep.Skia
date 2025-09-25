using SkiaSharp;
using Beep.Skia.Model;
using System;
using Beep.Skia.Components;

namespace Beep.Skia.ETL
{
    /// <summary>
    /// Base visual for ETL nodes: rounded rectangle with a header/title and optional subtitle.
    /// Provides helpers to create and maintain input/output connection points along the left/right edges.
    /// </summary>
    public abstract class ETLControl : MaterialControl
    {
        private string _title = "ETL";
        public string Title { get => _title; set { if (_title == value) return; _title = value ?? string.Empty; InvalidateVisual(); } }

        private string _subtitle = string.Empty;
        public string Subtitle { get => _subtitle; set { if (_subtitle == value) return; _subtitle = value ?? string.Empty; InvalidateVisual(); } }

        // Adopt Material Design tokens for consistent theming
        private SKColor _background = MaterialColors.Surface;
        public SKColor Background { get => _background; set { if (_background == value) return; _background = value; InvalidateVisual(); } }

        private SKColor _stroke = MaterialColors.Outline;
        public SKColor Stroke { get => _stroke; set { if (_stroke == value) return; _stroke = value; InvalidateVisual(); } }

        private SKColor _headerColor = MaterialColors.PrimaryContainer;
        public SKColor HeaderColor { get => _headerColor; set { if (_headerColor == value) return; _headerColor = value; InvalidateVisual(); } }

        private SKColor _headerTextColor = MaterialColors.OnPrimaryContainer;
        public SKColor HeaderTextColor { get => _headerTextColor; set { if (_headerTextColor == value) return; _headerTextColor = value; InvalidateVisual(); } }

        // Layout constants
        protected const float CornerRadius = 8f;
        protected const float HeaderHeight = 22f;
        protected const float Padding = 8f;
        protected const float PortRadius = 5f;

        protected ETLControl()
        {
            Width = Math.Max(120, Width);
            Height = Math.Max(64, Height);
        }

        // Allow runtime adjustment via property editor
        public int InPortCount
        {
            get => InConnectionPoints?.Count ?? 0;
            set { int v = Math.Max(0, value); EnsurePortCounts(v, OutPortCount); InvalidateVisual(); }
        }

        public int OutPortCount
        {
            get => OutConnectionPoints?.Count ?? 0;
            set { int v = Math.Max(0, value); EnsurePortCounts(InPortCount, v); InvalidateVisual(); }
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Draw the shape (virtual method for customization)
            DrawShape(canvas);

            // Header
            var headerRect = new SKRect(X, Y, X + Width, Y + HeaderHeight);
            using (var hfill = new SKPaint { Color = HeaderColor, Style = SKPaintStyle.Fill })
                canvas.DrawRect(headerRect, hfill);

            using var font = new SKFont { Size = 13 };
            using var textPaint = new SKPaint { Color = HeaderTextColor, IsAntialias = true, Style = SKPaintStyle.Fill };
            canvas.DrawText(Title ?? string.Empty, headerRect.Left + Padding, headerRect.MidY + 5, SKTextAlign.Left, font, textPaint);

            if (!string.IsNullOrWhiteSpace(Subtitle))
            {
                using var subFont = new SKFont { Size = 11 };
                using var subPaint = new SKPaint { Color = new SKColor(70, 70, 70), IsAntialias = true };
                canvas.DrawText(Subtitle, X + Padding, Y + HeaderHeight + 18, SKTextAlign.Left, subFont, subPaint);
            }

            // Draw ports
            DrawPorts(canvas);
        }

        /// <summary>
        /// Draws the shape for this ETL control. Override in derived classes for custom shapes.
        /// </summary>
        protected virtual void DrawShape(SKCanvas canvas)
        {
            // Default rounded rectangle shape
            var rect = new SKRoundRect(new SKRect(X, Y, X + Width, Y + Height), CornerRadius, CornerRadius);
            using (var fill = new SKPaint { Color = Background, Style = SKPaintStyle.Fill, IsAntialias = true })
                canvas.DrawRoundRect(rect, fill);
            using (var border = new SKPaint { Color = Stroke, Style = SKPaintStyle.Stroke, StrokeWidth = 1.25f, IsAntialias = true })
                canvas.DrawRoundRect(rect, border);
        }

        protected void EnsurePortCounts(int inCount, int outCount)
        {
            // Grow/shrink input ports
            while (InConnectionPoints.Count < inCount)
                InConnectionPoints.Add(new ConnectionPoint { Type = ConnectionPointType.In, Radius = (int)PortRadius, Component = this, Shape = ComponentShape.Circle, DataType = "object", IsAvailable = true });
            while (InConnectionPoints.Count > inCount)
                InConnectionPoints.RemoveAt(InConnectionPoints.Count - 1);

            // Grow/shrink output ports
            while (OutConnectionPoints.Count < outCount)
                OutConnectionPoints.Add(new ConnectionPoint { Type = ConnectionPointType.Out, Radius = (int)PortRadius, Component = this, Shape = ComponentShape.Circle, DataType = "object", IsAvailable = true });
            while (OutConnectionPoints.Count > outCount)
                OutConnectionPoints.RemoveAt(OutConnectionPoints.Count - 1);

            LayoutPorts();
            InvalidateVisual();
        }

        protected virtual void LayoutPorts()
        {
            // Distribute ports evenly along left (inputs) and right (outputs) sides, inside content below header
            float inAreaTop = Y + HeaderHeight + Padding;
            float inAreaBottom = Y + Height - Padding;
            float outAreaTop = inAreaTop;
            float outAreaBottom = inAreaBottom;

            PositionPortsAlongEdge(InConnectionPoints, new SKPoint(X, 0), inAreaTop, inAreaBottom, -1);
            PositionPortsAlongEdge(OutConnectionPoints, new SKPoint(X + Width, 0), outAreaTop, outAreaBottom, +1);
        }

        private void PositionPortsAlongEdge(System.Collections.Generic.List<IConnectionPoint> ports, SKPoint edge, float top, float bottom, int dir)
        {
            int n = Math.Max(ports.Count, 1);
            float span = Math.Max(bottom - top, 1f);
            for (int i = 0; i < ports.Count; i++)
            {
                var p = ports[i];
                float t = (i + 1) / (float)(n + 1); // distribute
                float cy = top + t * span;
                float cx = edge.X + dir * (PortRadius + 2);
                p.Center = new SKPoint(cx, cy);
                p.Position = p.Center; // for line logic
                float r = PortRadius;
                p.Bounds = new SKRect(cx - r, cy - r, cx + r, cy + r);
                p.Rect = p.Bounds;
                p.Index = i;
                p.Component = this;
                p.IsAvailable = true;
            }
        }

        protected void DrawPorts(SKCanvas canvas)
        {
            using var inFill = new SKPaint { Color = MaterialColors.SecondaryContainer, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var outFill = new SKPaint { Color = MaterialColors.Primary, Style = SKPaintStyle.Fill, IsAntialias = true };
            foreach (var p in InConnectionPoints)
            {
                canvas.DrawCircle(p.Center, PortRadius, inFill);
            }
            foreach (var p in OutConnectionPoints)
            {
                canvas.DrawCircle(p.Center, PortRadius, outFill);
            }
        }

        protected override void OnBoundsChanged(SKRect bounds)
        {
            LayoutPorts();
            base.OnBoundsChanged(bounds);
        }

        /// <summary>
        /// Gets the effective text positioning bounds for ETL components.
        /// Accounts for the header area and component shape for proper text placement.
        /// </summary>
        /// <returns>A rectangle defining the effective bounds for text positioning.</returns>
        protected override SKRect GetTextPositioningBounds()
        {
            // For ETL components, use the full bounds but account for header in positioning
            return new SKRect(X, Y, X + Width, Y + Height);
        }
    }
}
