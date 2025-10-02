using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;

namespace Beep.Skia.StateMachine
{
    /// <summary>
    /// Base control for State Machine nodes with Material theming and standardized port management.
    /// </summary>
    public abstract class StateMachineControl : MaterialControl
    {
        protected const float PortRadius = 4f;

        private SKColor _backgroundColor = MaterialColors.Surface;
        public SKColor BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                if (_backgroundColor != value)
                {
                    _backgroundColor = value;
                    if (NodeProperties.TryGetValue("BackgroundColor", out var pi))
                        pi.ParameterCurrentValue = value;
                    InvalidateVisual();
                }
            }
        }

        private SKColor _borderColor = MaterialColors.Outline;
        public SKColor BorderColor
        {
            get => _borderColor;
            set
            {
                if (_borderColor != value)
                {
                    _borderColor = value;
                    if (NodeProperties.TryGetValue("BorderColor", out var pi))
                        pi.ParameterCurrentValue = value;
                    InvalidateVisual();
                }
            }
        }

        private float _borderThickness = 2.0f;
        public float BorderThickness
        {
            get => _borderThickness;
            set
            {
                float v = Math.Max(0f, value);
                if (Math.Abs(_borderThickness - v) > 1e-3f)
                {
                    _borderThickness = v;
                    if (NodeProperties.TryGetValue("BorderThickness", out var pi))
                        pi.ParameterCurrentValue = v;
                    InvalidateVisual();
                }
            }
        }

        protected StateMachineControl()
        {
            TextColor = MaterialColors.OnSurface;
            Width = 140;
            Height = 56;

            // Seed common metadata for editors
            NodeProperties["InPortCount"] = new ParameterInfo
            {
                ParameterName = "InPortCount",
                ParameterType = typeof(int),
                DefaultParameterValue = InPortCount,
                ParameterCurrentValue = InPortCount,
                Description = "Number of input ports (left)."
            };
            NodeProperties["OutPortCount"] = new ParameterInfo
            {
                ParameterName = "OutPortCount",
                ParameterType = typeof(int),
                DefaultParameterValue = OutPortCount,
                ParameterCurrentValue = OutPortCount,
                Description = "Number of output ports (right)."
            };
            NodeProperties["BackgroundColor"] = new ParameterInfo
            {
                ParameterName = "BackgroundColor",
                ParameterType = typeof(SKColor),
                DefaultParameterValue = BackgroundColor,
                ParameterCurrentValue = BackgroundColor,
                Description = "Fill color of the node"
            };
            NodeProperties["BorderColor"] = new ParameterInfo
            {
                ParameterName = "BorderColor",
                ParameterType = typeof(SKColor),
                DefaultParameterValue = BorderColor,
                ParameterCurrentValue = BorderColor,
                Description = "Stroke color of the node border"
            };
            NodeProperties["BorderThickness"] = new ParameterInfo
            {
                ParameterName = "BorderThickness",
                ParameterType = typeof(float),
                DefaultParameterValue = BorderThickness,
                ParameterCurrentValue = BorderThickness,
                Description = "Stroke thickness for the border"
            };
            NodeProperties["TextColor"] = new ParameterInfo
            {
                ParameterName = "TextColor",
                ParameterType = typeof(SKColor),
                DefaultParameterValue = this.TextColor,
                ParameterCurrentValue = this.TextColor,
                Description = "Primary text color"
            };
        }

        public int InPortCount
        {
            get => InConnectionPoints.Count;
            set
            {
                int v = Math.Max(0, value);
                EnsurePortCounts(v, OutConnectionPoints.Count);
                if (NodeProperties.TryGetValue("InPortCount", out var pi)) pi.ParameterCurrentValue = v;
                InvalidateVisual();
            }
        }

        public int OutPortCount
        {
            get => OutConnectionPoints.Count;
            set
            {
                int v = Math.Max(0, value);
                EnsurePortCounts(InConnectionPoints.Count, v);
                if (NodeProperties.TryGetValue("OutPortCount", out var pi)) pi.ParameterCurrentValue = v;
                InvalidateVisual();
            }
        }

        protected void EnsurePortCounts(int inputs, int outputs)
        {
            while (InConnectionPoints.Count < inputs)
                InConnectionPoints.Add(new ConnectionPoint { Type = ConnectionPointType.In, Shape = ComponentShape.Circle, DataType = "transition", IsAvailable = true, Component = this, Radius = (int)PortRadius });
            while (InConnectionPoints.Count > inputs)
                InConnectionPoints.RemoveAt(InConnectionPoints.Count - 1);

            while (OutConnectionPoints.Count < outputs)
                OutConnectionPoints.Add(new ConnectionPoint { Type = ConnectionPointType.Out, Shape = ComponentShape.Circle, DataType = "transition", IsAvailable = true, Component = this, Radius = (int)PortRadius });
            while (OutConnectionPoints.Count > outputs)
                OutConnectionPoints.RemoveAt(OutConnectionPoints.Count - 1);

            // keep editor metadata aligned when counts change programmatically
            if (NodeProperties.TryGetValue("InPortCount", out var piIn))
                piIn.ParameterCurrentValue = InConnectionPoints.Count;
            if (NodeProperties.TryGetValue("OutPortCount", out var piOut))
                piOut.ParameterCurrentValue = OutConnectionPoints.Count;

            // Defer actual layout to draw by marking ports dirty and notifying bounds
            MarkPortsDirty();
            try { OnBoundsChanged(Bounds); } catch { }
        }

        protected virtual void LayoutPorts()
        {
            LayoutPortsRightEdge(6f, 6f);
        }

        protected void LayoutPortsRightEdge(float topInset = 6f, float bottomInset = 6f)
        {
            var b = Bounds;

            if (InConnectionPoints.Count > 0)
            {
                var cp = InConnectionPoints[0];
                float cx = b.Left - 2f;
                float cy = b.MidY;
                cp.Center = new SKPoint(cx, cy);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(cx - PortRadius, cy - PortRadius, cx + PortRadius, cy + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = 0;
                cp.Component = this;
                cp.IsAvailable = true;
            }

            int nOut = Math.Max(OutConnectionPoints.Count, 1);
            float yTop = b.Top + Math.Max(0, topInset);
            float yBottom = b.Bottom - Math.Max(0, bottomInset);
            yBottom = Math.Max(yTop, yBottom);

            for (int i = 0; i < OutConnectionPoints.Count; i++)
            {
                float t = (i + 1) / (float)(nOut + 1);
                float cy = yTop + t * (yBottom - yTop);
                float cx = b.Right + 2f;
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

        protected virtual void DrawConnectionPoints(SKCanvas canvas)
        {
            using var inPaint = new SKPaint { Color = MaterialColors.SecondaryContainer, IsAntialias = true };
            using var outPaint = new SKPaint { Color = MaterialColors.Primary, IsAntialias = true };
            foreach (var p in InConnectionPoints)
            {
                var c = p.Position; canvas.DrawCircle(c.X, c.Y, PortRadius, inPaint);
            }
            foreach (var p in OutConnectionPoints)
            {
                var c = p.Position; canvas.DrawCircle(c.X, c.Y, PortRadius, outPaint);
            }
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            EnsurePortLayout(() => LayoutPorts());
            DrawStateMachineContent(canvas, context);
        }

        protected virtual void DrawStateMachineContent(SKCanvas canvas, DrawingContext context) { }

        protected override void OnBoundsChanged(SKRect bounds)
        {
            MarkPortsDirty();
            base.OnBoundsChanged(bounds);
        }
    }
}
