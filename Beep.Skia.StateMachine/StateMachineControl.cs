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

        public SKColor BackgroundColor { get; set; } = MaterialColors.Surface;
        public SKColor BorderColor { get; set; } = MaterialColors.Outline;
        public float BorderThickness { get; set; } = 2.0f;

        protected StateMachineControl()
        {
            TextColor = MaterialColors.OnSurface;
            Width = 140;
            Height = 56;
        }

        public int InPortCount
        {
            get => InConnectionPoints.Count;
            set => EnsurePortCounts(Math.Max(0, value), OutConnectionPoints.Count);
        }

        public int OutPortCount
        {
            get => OutConnectionPoints.Count;
            set => EnsurePortCounts(InConnectionPoints.Count, Math.Max(0, value));
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

            LayoutPorts();
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

        protected override void OnBoundsChanged(SKRect bounds)
        {
            LayoutPorts();
            base.OnBoundsChanged(bounds);
        }
    }
}
