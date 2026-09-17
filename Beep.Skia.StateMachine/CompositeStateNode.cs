using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;

namespace Beep.Skia.StateMachine
{
    /// <summary>
    /// Composite state: a state that contains nested sub-states (hierarchical state machine).
    /// Rendered as a larger rounded rectangle with a dashed border and title area at top.
    /// Child components are positioned within the inner content area.
    /// </summary>
    public class CompositeStateNode : StateMachineControl
    {
        private string _title = "Composite";
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

        private float _titleBarHeight = 24f;
        public float TitleBarHeight
        {
            get => _titleBarHeight;
            set
            {
                var v = Math.Max(18f, value);
                if (_titleBarHeight == v) return;
                _titleBarHeight = v;
                if (NodeProperties.TryGetValue("TitleBarHeight", out var pi)) pi.ParameterCurrentValue = _titleBarHeight;
                InvalidateVisual();
            }
        }

        private int _regionCount = 1;
        /// <summary>
        /// Number of orthogonal regions inside the composite state (1-4).
        /// Regions are separated by dashed dividers in the content area.
        /// </summary>
        public int RegionCount
        {
            get => _regionCount;
            set
            {
                var v = Math.Max(1, Math.Min(4, value));
                if (_regionCount == v) return;
                _regionCount = v;
                if (NodeProperties.TryGetValue("RegionCount", out var pi)) pi.ParameterCurrentValue = _regionCount;
                InvalidateVisual();
            }
        }

        public CompositeStateNode()
        {
            Width = 240; Height = 180;
            BackgroundColor = new SKColor(0xE8, 0xF5, 0xE9);
            BorderColor = new SKColor(0x4C, 0xAF, 0x50);
            TextColor = MaterialColors.OnSurface;
            if (NodeProperties.TryGetValue("TextColor", out var piTxt)) piTxt.ParameterCurrentValue = TextColor;
            EnsurePortCounts(1, 2);

            NodeProperties["Title"] = new ParameterInfo { ParameterName = "Title", ParameterType = typeof(string), DefaultParameterValue = _title, ParameterCurrentValue = _title, Description = "Composite state name" };
            NodeProperties["TitleBarHeight"] = new ParameterInfo { ParameterName = "TitleBarHeight", ParameterType = typeof(float), DefaultParameterValue = _titleBarHeight, ParameterCurrentValue = _titleBarHeight, Description = "Height of title bar" };
            NodeProperties["RegionCount"] = new ParameterInfo { ParameterName = "RegionCount", ParameterType = typeof(int), DefaultParameterValue = _regionCount, ParameterCurrentValue = _regionCount, Description = "Number of orthogonal regions (1-4)" };
        }

        /// <summary>
        /// Gets the inner content area for child components (below the title bar).
        /// </summary>
        public SKRect GetContentArea()
        {
            return new SKRect(X + 8, Y + _titleBarHeight + 4, X + Width - 8, Y + Height - 8);
        }

        protected override void LayoutPorts()
        {
            LayoutPortsRightEdge(6f, 6f);
        }

        protected override void DrawStateMachineContent(SKCanvas canvas, DrawingContext context)
        {
            var outerRect = new SKRect(X, Y, X + Width, Y + Height);
            var titleRect = new SKRect(X, Y, X + Width, Y + _titleBarHeight);

            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint
            {
                Color = BorderColor,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2f,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateDash(new float[] { 6f, 3f }, 0)
            };
            using var titleFill = new SKPaint { Color = BorderColor.WithAlpha(40), Style = SKPaintStyle.Fill, IsAntialias = true };
            using var borderSolid = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f, IsAntialias = true };

            // Outer dashed border
            canvas.DrawRoundRect(outerRect, 12, 12, fill);
            canvas.DrawRoundRect(outerRect, 12, 12, stroke);

            // Title bar
            canvas.DrawRoundRect(titleRect, 12, 12, titleFill);
            canvas.DrawLine(titleRect.Left, titleRect.Bottom, titleRect.Right, titleRect.Bottom, borderSolid);

            // Title text
            using var font = new SKFont(SKTypeface.Default, 12);
            using var textPaint = new SKPaint { Color = TextColor, IsAntialias = true };
            canvas.DrawText(_title, X + 12, Y + _titleBarHeight / 2f + 4, SKTextAlign.Left, font, textPaint);

            // Separator line for content area
            using var sepPaint = new SKPaint { Color = new SKColor(0xE0, 0xE0, 0xE0), Style = SKPaintStyle.Stroke, StrokeWidth = 0.5f, IsAntialias = true };
            canvas.DrawLine(X + 4, Y + _titleBarHeight + 2, X + Width - 4, Y + _titleBarHeight + 2, sepPaint);

            // Orthogonal region dividers
            if (_regionCount > 1)
            {
                var content = GetContentArea();
                float regionWidth = content.Width / _regionCount;
                using var dividerPaint = new SKPaint
                {
                    Color = BorderColor.WithAlpha(140),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1f,
                    IsAntialias = true,
                    PathEffect = SKPathEffect.CreateDash(new float[] { 4f, 3f }, 0)
                };
                for (int i = 1; i < _regionCount; i++)
                {
                    float dx = content.Left + i * regionWidth;
                    canvas.DrawLine(dx, content.Top, dx, content.Bottom, dividerPaint);
                }
            }

            DrawConnectionPoints(canvas);
        }

        public override Dictionary<string, object> GetProperties(bool includeCommon = true, bool includeNodeProperties = true)
        {
            var props = base.GetProperties(includeCommon, includeNodeProperties);
            props["Title"] = _title;
            props["TitleBarHeight"] = _titleBarHeight;
            props["RegionCount"] = _regionCount;
            return props;
        }

        public override void SetPropperties(IDictionary<string, object> properties, bool updateNodeProperties = true, bool applyToPublicSetters = true)
        {
            base.SetPropperties(properties, updateNodeProperties, applyToPublicSetters);
            if (properties.TryGetValue("Title", out var t) && t is string ts) Title = ts;
            if (properties.TryGetValue("TitleBarHeight", out var h) && h != null) TitleBarHeight = Convert.ToSingle(h);
            if (properties.TryGetValue("RegionCount", out var r) && r != null) RegionCount = Convert.ToInt32(r);
        }
    }
}
