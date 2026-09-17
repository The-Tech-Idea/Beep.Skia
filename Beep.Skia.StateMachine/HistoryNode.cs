using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;

namespace Beep.Skia.StateMachine
{
    /// <summary>
    /// History pseudostate: remembers the last active sub-state within a composite state.
    /// Shallow (H) remembers only the immediate sub-state.
    /// Deep (H*) remembers the full nesting path.
    /// Rendered as a circle with 'H' or 'H*' inside.
    /// </summary>
    public class HistoryNode : StateMachineControl
    {
        private string _title = "H";
        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string Title
        {
            get => _title;
            set
            {
                var v = value ?? "H";
                if (_title == v) return;
                _title = v;
                if (NodeProperties.TryGetValue("Title", out var pi)) pi.ParameterCurrentValue = _title;
                InvalidateVisual();
            }
        }

        private bool _isDeep = false;
        /// <summary>
        /// Gets or sets the is deep.
        /// </summary>
        public bool IsDeep
        {
            get => _isDeep;
            set
            {
                if (_isDeep == value) return;
                _isDeep = value;
                Title = IsDeep ? "H*" : "H";
                if (NodeProperties.TryGetValue("IsDeep", out var pi)) pi.ParameterCurrentValue = _isDeep;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Deep history (H*) or shallow history (H)
        /// </summary>
        public HistoryNode()
        {
            Width = 36; Height = 36;
            BackgroundColor = new SKColor(0xFF, 0xF3, 0xE0);
            BorderColor = new SKColor(0xFF, 0x98, 0x00);
            TextColor = MaterialColors.OnSurface;
            if (NodeProperties.TryGetValue("TextColor", out var piTxt)) piTxt.ParameterCurrentValue = TextColor;
            EnsurePortCounts(1, 1);

            NodeProperties["IsDeep"] = new ParameterInfo { ParameterName = "IsDeep", ParameterType = typeof(bool), DefaultParameterValue = _isDeep, ParameterCurrentValue = _isDeep, Description = "Deep history (H*) or shallow history (H)" };
        }

        protected override void LayoutPorts()
        {
            var b = Bounds;

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

            if (OutConnectionPoints.Count > 0)
            {
                var cp = OutConnectionPoints[0];
                cp.Center = new SKPoint(b.Right, b.MidY);
                cp.Position = cp.Center;
                cp.Bounds = new SKRect(b.Right - PortRadius, b.MidY - PortRadius, b.Right + PortRadius, b.MidY + PortRadius);
                cp.Rect = cp.Bounds;
                cp.Index = 0;
                cp.Component = this;
                cp.IsAvailable = true;
            }
        }

        protected override void DrawStateMachineContent(SKCanvas canvas, DrawingContext context)
        {
            float r = MathF.Min(Width, Height) / 2f;
            float cx = X + Width / 2f;
            float cy = Y + Height / 2f;

            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Stroke, StrokeWidth = 2f, IsAntialias = true };

            canvas.DrawCircle(cx, cy, r, fill);
            canvas.DrawCircle(cx, cy, r, stroke);

            // Draw H or H* character
            float fontSize = _isDeep ? 13f : 14f;
            using var font = new SKFont(SKTypeface.Default, fontSize) { Embolden = true };
            using var textPaint = new SKPaint { Color = BorderColor, IsAntialias = true };
            canvas.DrawText(_isDeep ? "H*" : "H", cx, cy + fontSize / 3f, SKTextAlign.Center, font, textPaint);

            DrawConnectionPoints(canvas);
        }

        public override Dictionary<string, object> GetProperties(bool includeCommon = true, bool includeNodeProperties = true)
        {
            var props = base.GetProperties(includeCommon, includeNodeProperties);
            props["IsDeep"] = _isDeep;
            return props;
        }

        /// <summary>
        /// Gets or sets the set propperties.
        /// </summary>
        public override void SetPropperties(IDictionary<string, object> properties, bool updateNodeProperties = true, bool applyToPublicSetters = true)
        {
            base.SetPropperties(properties, updateNodeProperties, applyToPublicSetters);
            if (properties.TryGetValue("IsDeep", out var d) && d is bool bd) IsDeep = bd;
        }
    }
}
