using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using Beep.Skia.Components;

namespace Beep.Skia.Network
{
    /// <summary>
    /// Base class for all network-related controls in the Beep.Skia framework.
    /// Provides common functionality for network visualization components.
    /// </summary>
    public abstract class NetworkControl : MaterialControl
    {
        private SKColor _primaryColor = MaterialColors.Primary;
        public SKColor PrimaryColor { get => _primaryColor; set { if (_primaryColor == value) return; _primaryColor = value; if (NodeProperties.TryGetValue("PrimaryColor", out var pi)) pi.ParameterCurrentValue = _primaryColor; InvalidateVisual(); } }

        private SKColor _secondaryColor = MaterialColors.Secondary;
        public SKColor SecondaryColor { get => _secondaryColor; set { if (_secondaryColor == value) return; _secondaryColor = value; if (NodeProperties.TryGetValue("SecondaryColor", out var pi)) pi.ParameterCurrentValue = _secondaryColor; InvalidateVisual(); } }

        private SKColor _accentColor = MaterialColors.Tertiary;
        public SKColor AccentColor { get => _accentColor; set { if (_accentColor == value) return; _accentColor = value; if (NodeProperties.TryGetValue("AccentColor", out var pi)) pi.ParameterCurrentValue = _accentColor; InvalidateVisual(); } }

        private SKColor _borderColor = MaterialColors.Outline;
        public SKColor BorderColor { get => _borderColor; set { if (_borderColor == value) return; _borderColor = value; if (NodeProperties.TryGetValue("BorderColor", out var pi)) pi.ParameterCurrentValue = _borderColor; InvalidateVisual(); } }

        private float _borderThickness = 1.5f;
        public float BorderThickness { get => _borderThickness; set { if (System.Math.Abs(_borderThickness - value) < 0.0001f) return; _borderThickness = value; if (NodeProperties.TryGetValue("BorderThickness", out var pi)) pi.ParameterCurrentValue = _borderThickness; InvalidateVisual(); } }

        private float _cornerRadius = 6f;
        public float CornerRadius { get => _cornerRadius; set { if (System.Math.Abs(_cornerRadius - value) < 0.0001f) return; _cornerRadius = value; if (NodeProperties.TryGetValue("CornerRadius", out var pi)) pi.ParameterCurrentValue = _cornerRadius; InvalidateVisual(); } }

        private bool _isHighlighted = false;
        public bool IsHighlighted { get => _isHighlighted; set { if (_isHighlighted == value) return; _isHighlighted = value; if (NodeProperties.TryGetValue("IsHighlighted", out var pi)) pi.ParameterCurrentValue = _isHighlighted; InvalidateVisual(); } }

        private SKColor _highlightColor = MaterialColors.Tertiary;
        public SKColor HighlightColor { get => _highlightColor; set { if (_highlightColor == value) return; _highlightColor = value; if (NodeProperties.TryGetValue("HighlightColor", out var pi)) pi.ParameterCurrentValue = _highlightColor; InvalidateVisual(); } }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkControl"/> class.
        /// </summary>
        protected NetworkControl()
        {
            // Set default text properties for network controls
            DisplayText = Name;
            TextPosition = TextPosition.Below;
            ShowDisplayText = true;
            TextColor = MaterialColors.OnSurface;
            TextFontSize = 12f;

            // Seed NodeProperties
            NodeProperties["PrimaryColor"] = new ParameterInfo { ParameterName = "PrimaryColor", ParameterType = typeof(SKColor), DefaultParameterValue = _primaryColor, ParameterCurrentValue = _primaryColor, Description = "Primary color for fills" };
            NodeProperties["SecondaryColor"] = new ParameterInfo { ParameterName = "SecondaryColor", ParameterType = typeof(SKColor), DefaultParameterValue = _secondaryColor, ParameterCurrentValue = _secondaryColor, Description = "Secondary color" };
            NodeProperties["AccentColor"] = new ParameterInfo { ParameterName = "AccentColor", ParameterType = typeof(SKColor), DefaultParameterValue = _accentColor, ParameterCurrentValue = _accentColor, Description = "Accent color" };
            NodeProperties["BorderColor"] = new ParameterInfo { ParameterName = "BorderColor", ParameterType = typeof(SKColor), DefaultParameterValue = _borderColor, ParameterCurrentValue = _borderColor, Description = "Border color" };
            NodeProperties["BorderThickness"] = new ParameterInfo { ParameterName = "BorderThickness", ParameterType = typeof(float), DefaultParameterValue = _borderThickness, ParameterCurrentValue = _borderThickness, Description = "Border thickness" };
            NodeProperties["CornerRadius"] = new ParameterInfo { ParameterName = "CornerRadius", ParameterType = typeof(float), DefaultParameterValue = _cornerRadius, ParameterCurrentValue = _cornerRadius, Description = "Corner radius" };
            NodeProperties["IsHighlighted"] = new ParameterInfo { ParameterName = "IsHighlighted", ParameterType = typeof(bool), DefaultParameterValue = _isHighlighted, ParameterCurrentValue = _isHighlighted, Description = "Highlight state" };
            NodeProperties["HighlightColor"] = new ParameterInfo { ParameterName = "HighlightColor", ParameterType = typeof(SKColor), DefaultParameterValue = _highlightColor, ParameterCurrentValue = _highlightColor, Description = "Highlight color" };
            NodeProperties["TextColor"] = new ParameterInfo { ParameterName = "TextColor", ParameterType = typeof(SKColor), DefaultParameterValue = this.TextColor, ParameterCurrentValue = this.TextColor, Description = "Text color" };
        }

        /// <summary>
        /// Gets the effective fill color, considering highlight state.
        /// </summary>
        protected SKColor GetEffectiveFillColor(SKColor baseColor)
        {
            return IsHighlighted ? HighlightColor : baseColor;
        }

        /// <summary>
        /// Draws a standard border around the control.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="rect">The rectangle to draw the border around.</param>
        protected void DrawBorder(SKCanvas canvas, SKRect rect)
        {
            using var borderPaint = new SKPaint
            {
                Color = BorderColor,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = BorderThickness,
                IsAntialias = true
            };
            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, borderPaint);
        }

        /// <summary>
        /// Draws a filled background with border.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="rect">The rectangle to draw.</param>
        /// <param name="fillColor">The fill color.</param>
        protected void DrawFilledRect(SKCanvas canvas, SKRect rect, SKColor fillColor)
        {
            using var fillPaint = new SKPaint
            {
                Color = GetEffectiveFillColor(fillColor),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, fillPaint);
            DrawBorder(canvas, rect);
        }

        /// <summary>
        /// Draws centered text within a rectangle.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="text">The text to draw.</param>
        /// <param name="rect">The rectangle to center text in.</param>
        /// <param name="fontSize">The font size.</param>
        /// <param name="textColor">The text color.</param>
        protected void DrawCenteredText(SKCanvas canvas, string text, SKRect rect, float fontSize, SKColor textColor)
        {
            using var font = new SKFont { Size = fontSize };
            using var textPaint = new SKPaint { Color = textColor, IsAntialias = true };

            var textBounds = new SKRect();
            font.MeasureText(text, out textBounds);

            float textX = rect.Left + (rect.Width - textBounds.Width) / 2;
            float textY = rect.Top + (rect.Height + textBounds.Height) / 2 - textBounds.Top;

            canvas.DrawText(text, textX, textY, SKTextAlign.Left, font, textPaint);
        }
    }
}