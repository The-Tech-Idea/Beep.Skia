using Beep.Skia.Components;
using Beep.Skia.Model;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Beep.Skia.Flowchart
{
    /// <summary>
    /// Base class for Flowchart nodes providing consistent sizing and port layout helpers.
    /// </summary>
    public abstract class FlowchartControl : MaterialControl
    {
        protected const float PortRadius = 4f;
        protected const float CornerRadius = 8f;

        private SKColor? _customFillColor = null;
        private SKColor? _customStrokeColor = null;
        private SKColor? _customTextColor = null;

        /// <summary>
        /// Optional custom fill color (overrides default node color). Set to null to use default.
        /// </summary>
        public SKColor? CustomFillColor
        {
            get => _customFillColor;
            set
            {
                if (_customFillColor != value)
                {
                    _customFillColor = value;
                    if (NodeProperties.TryGetValue("CustomFillColor", out var pi))
                        pi.ParameterCurrentValue = value?.ToString() ?? "";
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Optional custom stroke color (overrides default border color). Set to null to use default.
        /// </summary>
        public SKColor? CustomStrokeColor
        {
            get => _customStrokeColor;
            set
            {
                if (_customStrokeColor != value)
                {
                    _customStrokeColor = value;
                    if (NodeProperties.TryGetValue("CustomStrokeColor", out var pi))
                        pi.ParameterCurrentValue = value?.ToString() ?? "";
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Optional custom text color (overrides default text color). Set to null to use default.
        /// </summary>
        public SKColor? CustomTextColor
        {
            get => _customTextColor;
            set
            {
                if (_customTextColor != value)
                {
                    _customTextColor = value;
                    if (NodeProperties.TryGetValue("CustomTextColor", out var pi))
                        pi.ParameterCurrentValue = value?.ToString() ?? "";
                    InvalidateVisual();
                }
            }
        }

        protected FlowchartControl()
        {
            Width = Math.Max(120, Width);
            Height = Math.Max(60, Height);
            ShowDisplayText = true;
            TextPosition = TextPosition.Below;
            EnsurePortCounts(1, 1);

            // Seed common Flowchart metadata for editors
            NodeProperties["InPortCount"] = new ParameterInfo
            {
                ParameterName = "InPortCount",
                ParameterType = typeof(int),
                DefaultParameterValue = InPortCount,
                ParameterCurrentValue = InPortCount,
                Description = "Number of input ports (left side)."
            };
            NodeProperties["OutPortCount"] = new ParameterInfo
            {
                ParameterName = "OutPortCount",
                ParameterType = typeof(int),
                DefaultParameterValue = OutPortCount,
                ParameterCurrentValue = OutPortCount,
                Description = "Number of output ports (right side)."
            };
            NodeProperties["CustomFillColor"] = new ParameterInfo
            {
                ParameterName = "CustomFillColor",
                ParameterType = typeof(string),
                DefaultParameterValue = "",
                ParameterCurrentValue = "",
                Description = "Custom fill color (hex format: #RRGGBB or #AARRGGBB). Leave empty for default."
            };
            NodeProperties["CustomStrokeColor"] = new ParameterInfo
            {
                ParameterName = "CustomStrokeColor",
                ParameterType = typeof(string),
                DefaultParameterValue = "",
                ParameterCurrentValue = "",
                Description = "Custom border color (hex format). Leave empty for default."
            };
            NodeProperties["CustomTextColor"] = new ParameterInfo
            {
                ParameterName = "CustomTextColor",
                ParameterType = typeof(string),
                DefaultParameterValue = "",
                ParameterCurrentValue = "",
                Description = "Custom text color (hex format). Leave empty for default."
            };
        }

        // Public port count properties so editors can adjust counts at runtime
        public int InPortCount
        {
            get => InConnectionPoints?.Count ?? 0;
            set
            {
                int v = Math.Max(0, value);
                EnsurePortCounts(v, OutPortCount);
                // Keep NodeProperties in sync for editor round-trip
                if (NodeProperties.TryGetValue("InPortCount", out var piIn))
                {
                    piIn.ParameterCurrentValue = v;
                }
                InvalidateVisual();
            }
        }

        public int OutPortCount
        {
            get => OutConnectionPoints?.Count ?? 0;
            set
            {
                int v = Math.Max(0, value);
                EnsurePortCounts(InPortCount, v);
                if (NodeProperties.TryGetValue("OutPortCount", out var piOut))
                {
                    piOut.ParameterCurrentValue = v;
                }
                InvalidateVisual();
            }
        }

        protected void EnsurePortCounts(int inputs, int outputs)
        {
            while (InConnectionPoints.Count < inputs)
                InConnectionPoints.Add(new ConnectionPoint { Type = ConnectionPointType.In, Shape = ComponentShape.Circle, DataType = "link", IsAvailable = true, Component = this, Radius = (int)PortRadius });
            while (InConnectionPoints.Count > inputs)
                InConnectionPoints.RemoveAt(InConnectionPoints.Count - 1);

            while (OutConnectionPoints.Count < outputs)
                OutConnectionPoints.Add(new ConnectionPoint { Type = ConnectionPointType.Out, Shape = ComponentShape.Circle, DataType = "link", IsAvailable = true, Component = this, Radius = (int)PortRadius });
            while (OutConnectionPoints.Count > outputs)
                OutConnectionPoints.RemoveAt(OutConnectionPoints.Count - 1);

            // Keep editor metadata aligned when counts change programmatically
            if (NodeProperties.TryGetValue("InPortCount", out var piIn))
                piIn.ParameterCurrentValue = InConnectionPoints.Count;
            if (NodeProperties.TryGetValue("OutPortCount", out var piOut))
                piOut.ParameterCurrentValue = OutConnectionPoints.Count;

            MarkPortsDirty();
            try { OnBoundsChanged(Bounds); } catch { }
        }

        protected virtual void LayoutPorts()
        {
            LayoutPortsVerticalSegments(topInset: 6f, bottomInset: 6f);
        }

        protected void LayoutPortsVerticalSegments(float topInset, float bottomInset, float leftOffset = -2f, float rightOffset = 2f)
        {
            var b = Bounds;
            float yTop = b.Top + Math.Max(0, topInset);
            float yBottom = b.Bottom - Math.Max(0, bottomInset);
            yBottom = Math.Max(yTop, yBottom);

            int nIn = Math.Max(InConnectionPoints.Count, 1);
            for (int i = 0; i < InConnectionPoints.Count; i++)
            {
                float t = (i + 1) / (float)(nIn + 1);
                float cy = yTop + t * (yBottom - yTop);
                float cx = b.Left - Math.Abs(leftOffset);
                var cp = InConnectionPoints[i];
                cp.Center = new SKPoint(cx, cy);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(cx - PortRadius, cy - PortRadius, cx + PortRadius, cy + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = i;
                cp.Component = this;
                cp.IsAvailable = true;
            }

            int nOut = Math.Max(OutConnectionPoints.Count, 1);
            for (int i = 0; i < OutConnectionPoints.Count; i++)
            {
                float t = (i + 1) / (float)(nOut + 1);
                float cy = yTop + t * (yBottom - yTop);
                float cx = b.Right + Math.Abs(rightOffset);
                var cp = OutConnectionPoints[i];
                cp.Center = new SKPoint(cx, cy);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(cx - PortRadius, cy - PortRadius, cx + PortRadius, cy + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = i;
                cp.Component = this;
                cp.IsAvailable = true;
            }
        }

        // Horizontal convenience wrapper (left->right)
        protected void PlacePortsAlongHorizontalEdge(IList<IConnectionPoint> ports, float yEdge, float x1, float x2, float outwardSign)
        {
            var a = new SKPoint(Math.Min(x1, x2), yEdge);
            var b = new SKPoint(Math.Max(x1, x2), yEdge);
            PlacePortsOnSegment(ports, a, b, outwardSign);
        }

        protected void DrawPorts(SKCanvas canvas)
        {
            // Use Material Design tokens for consistent theming across families
            using var inPaint = new SKPaint { Color = MaterialColors.SecondaryContainer, IsAntialias = true };
            using var outPaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true };
            foreach (var p in InConnectionPoints) canvas.DrawCircle(p.Center, PortRadius, inPaint);
            foreach (var p in OutConnectionPoints) canvas.DrawCircle(p.Center, PortRadius, outPaint);
        }

        // Geometry helpers for shape-aware placement
        protected void PlacePortsAlongVerticalEdge(IList<IConnectionPoint> ports, float xEdge, float y1, float y2, float outwardSign)
        {
            if (ports == null || ports.Count == 0)
                return;
            float top = Math.Min(y1, y2);
            float bottom = Math.Max(y1, y2);
            int n = Math.Max(ports.Count, 1);
            float offset = (PortRadius + 1f) * outwardSign;
            for (int i = 0; i < ports.Count; i++)
            {
                float t = (i + 1f) / (n + 1f);
                float cy = top + t * (bottom - top);
                var center = new SKPoint(xEdge + offset, cy);
                SetPort(ports[i], center, i);
            }
        }

        protected void PlacePortsOnSegment(IList<IConnectionPoint> ports, SKPoint a, SKPoint b, float outwardSign)
        {
            if (ports == null || ports.Count == 0)
                return;
            var v = new SKPoint(b.X - a.X, b.Y - a.Y);
            float len = MathF.Sqrt(v.X * v.X + v.Y * v.Y);
            if (len < 1e-3f)
                return;
            var dir = new SKPoint(v.X / len, v.Y / len);
            // CCW normal
            var normal = new SKPoint(-dir.Y, dir.X);
            float offset = (PortRadius + 1f) * outwardSign;
            int n = Math.Max(ports.Count, 1);
            for (int i = 0; i < ports.Count; i++)
            {
                float t = (i + 1f) / (n + 1f);
                var p = new SKPoint(a.X + t * v.X, a.Y + t * v.Y);
                var center = new SKPoint(p.X + normal.X * offset, p.Y + normal.Y * offset);
                SetPort(ports[i], center, i);
            }
        }

        // Optional: place ports along an arc with outward/inward radial offset (for curved edges)
        protected void PlacePortsOnArc(IList<IConnectionPoint> ports, SKPoint center, float radius, float startAngleRad, float endAngleRad, float outwardSign)
        {
            if (ports == null || ports.Count == 0)
                return;
            int n = Math.Max(ports.Count, 1);
            for (int i = 0; i < ports.Count; i++)
            {
                float t = (i + 1f) / (n + 1f);
                float ang = startAngleRad + t * (endAngleRad - startAngleRad);
                float ca = MathF.Cos(ang);
                float sa = MathF.Sin(ang);
                var edge = new SKPoint(center.X + radius * ca, center.Y + radius * sa);
                // Outward normal for arc is radial vector (ca, sa)
                float offset = (PortRadius + 1f) * outwardSign;
                var centerPt = new SKPoint(edge.X + ca * offset, edge.Y + sa * offset);
                SetPort(ports[i], centerPt, i);
            }
        }

        protected void SetPort(IConnectionPoint cp, SKPoint center, int index)
        {
            cp.Center = center;
            cp.Position = center;
            cp.Bounds = new SKRect(center.X - PortRadius, center.Y - PortRadius, center.X + PortRadius, center.Y + PortRadius);
            cp.Rect = cp.Bounds;
            cp.Index = index;
            cp.Component = this;
            cp.IsAvailable = true;
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Ensure ports are laid out before any derived drawing logic uses them
            EnsurePortLayout(() => LayoutPorts());
            // Defer actual content to derived controls via a template method
            DrawFlowchartContent(canvas, context);
        }

        // Derived flowchart nodes should override this instead of DrawContent
        protected virtual void DrawFlowchartContent(SKCanvas canvas, DrawingContext context) { }

        protected void PlacePortsAcrossTwoSegments(IList<IConnectionPoint> ports, SKPoint a, SKPoint b, SKPoint c, float outwardSign)
        {
            if (ports == null || ports.Count == 0) return;
            int n1 = (ports.Count + 1) / 2; // first half on seg ab
            int n2 = ports.Count - n1;      // remainder on seg bc
            if (n1 > 0)
            {
                var sub = new List<IConnectionPoint>();
                for (int i = 0; i < n1; i++) sub.Add(ports[i]);
                PlacePortsOnSegment(sub, a, b, outwardSign);
            }
            if (n2 > 0)
            {
                var sub = new List<IConnectionPoint>();
                for (int i = 0; i < n2; i++) sub.Add(ports[n1 + i]);
                PlacePortsOnSegment(sub, b, c, outwardSign);
            }
        }

        protected override void OnBoundsChanged(SKRect bounds)
        {
            MarkPortsDirty();
            base.OnBoundsChanged(bounds);
        }
    }
}
