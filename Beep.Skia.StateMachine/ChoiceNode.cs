using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;

namespace Beep.Skia.StateMachine
{
    /// <summary>
    /// Choice/Junction pseudostate: evaluates guard conditions and selects one outgoing transition.
    /// Rendered as a small diamond shape.
    /// </summary>
    public class ChoiceNode : StateMachineControl
    {
        private string _title = "";
        public string Title
        {
            get => _title;
            set
            {
                var v = value ?? string.Empty;
                if (_title == v) return;
                _title = v;
                if (NodeProperties.TryGetValue("Title", out var pi)) pi.ParameterCurrentValue = _title;
                InvalidateVisual();
            }
        }

        public ChoiceNode()
        {
            Width = 32; Height = 32;
            BackgroundColor = MaterialColors.Surface;
            BorderColor = MaterialColors.Outline;
            TextColor = MaterialColors.OnSurface;
            if (NodeProperties.TryGetValue("TextColor", out var piTxt)) piTxt.ParameterCurrentValue = TextColor;
            EnsurePortCounts(1, 2);

            NodeProperties["Title"] = new ParameterInfo { ParameterName = "Title", ParameterType = typeof(string), DefaultParameterValue = _title, ParameterCurrentValue = _title, Description = "Optional condition label" };
        }

        protected override void LayoutPorts()
        {
            var b = Bounds;

            // Input on the left
            if (InConnectionPoints.Count > 0)
            {
                var cp = InConnectionPoints[0];
                cp.Center = new SKPoint(b.Left, b.MidY);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(b.Left - PortRadius, b.MidY - PortRadius, b.Left + PortRadius, b.MidY + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = 0;
                cp.Component = this;
                cp.IsAvailable = true;
            }

            // Outputs: right for 1, bottom for 2, top for 3
            if (OutConnectionPoints.Count > 0)
            {
                var cp0 = OutConnectionPoints[0];
                cp0.Center = new SKPoint(b.Right, b.MidY);
                cp0.Position = cp0.Center;
                cp0.Bounds = new SKRect(b.Right - PortRadius, b.MidY - PortRadius, b.Right + PortRadius, b.MidY + PortRadius);
                cp0.Rect = cp0.Bounds;
                cp0.Index = 0;
                cp0.Component = this;
                cp0.IsAvailable = true;
            }
            if (OutConnectionPoints.Count > 1)
            {
                var cp1 = OutConnectionPoints[1];
                cp1.Center = new SKPoint(b.MidX, b.Bottom);
                cp1.Position = cp1.Center;
                cp1.Bounds = new SKRect(b.MidX - PortRadius, b.Bottom - PortRadius, b.MidX + PortRadius, b.Bottom + PortRadius);
                cp1.Rect = cp1.Bounds;
                cp1.Index = 1;
                cp1.Component = this;
                cp1.IsAvailable = true;
            }
            if (OutConnectionPoints.Count > 2)
            {
                var cp2 = OutConnectionPoints[2];
                cp2.Center = new SKPoint(b.MidX, b.Top);
                cp2.Position = cp2.Center;
                cp2.Bounds = new SKRect(b.MidX - PortRadius, b.Top - PortRadius, b.MidX + PortRadius, b.Top + PortRadius);
                cp2.Rect = cp2.Bounds;
                cp2.Index = 2;
                cp2.Component = this;
                cp2.IsAvailable = true;
            }
        }

        protected override void DrawStateMachineContent(SKCanvas canvas, DrawingContext context)
        {
            var b = Bounds;

            using var pathBuilder = new SKPathBuilder();
            pathBuilder.MoveTo(b.MidX, b.Top);
            pathBuilder.LineTo(b.Right, b.MidY);
            pathBuilder.LineTo(b.MidX, b.Bottom);
            pathBuilder.LineTo(b.Left, b.MidY);
            pathBuilder.Close();

            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Stroke, StrokeWidth = 2f, IsAntialias = true };

            using var path = pathBuilder.Detach();
            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);

            if (!string.IsNullOrWhiteSpace(_title))
            {
                using var font = new SKFont(SKTypeface.Default, 9);
                using var textPaint = new SKPaint { Color = TextColor, IsAntialias = true };
                canvas.DrawText(_title, b.MidX, b.MidY + 3, SKTextAlign.Center, font, textPaint);
            }

            DrawConnectionPoints(canvas);
        }

        public override Dictionary<string, object> GetProperties(bool includeCommon = true, bool includeNodeProperties = true)
        {
            var props = base.GetProperties(includeCommon, includeNodeProperties);
            props["Title"] = _title;
            return props;
        }

        public override void SetPropperties(IDictionary<string, object> properties, bool updateNodeProperties = true, bool applyToPublicSetters = true)
        {
            base.SetPropperties(properties, updateNodeProperties, applyToPublicSetters);
            if (properties.TryGetValue("Title", out var t) && t is string ts) Title = ts;
        }
    }
}