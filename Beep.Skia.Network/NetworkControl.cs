using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;

namespace Beep.Skia.Network
{
    /// <summary>
    /// Base class for all network-related controls in the Beep.Skia framework.
    /// Provides common functionality for network visualization components.
    /// </summary>
    public abstract class NetworkControl : SkiaComponent
    {
        /// <summary>
        /// Gets or sets the primary color for this network control.
        /// </summary>
        public SKColor PrimaryColor { get; set; } = new SKColor(0x42, 0xA5, 0xF5); // Material Blue 400

        /// <summary>
        /// Gets or sets the secondary color for this network control.
        /// </summary>
        public SKColor SecondaryColor { get; set; } = new SKColor(0x81, 0xC7, 0x84); // Material Green 400

        /// <summary>
        /// Gets or sets the accent color for this network control.
        /// </summary>
        public SKColor AccentColor { get; set; } = new SKColor(0xFF, 0x98, 0x00); // Material Orange 400

        /// <summary>
        /// Gets or sets the border color for this network control.
        /// </summary>
        public SKColor BorderColor { get; set; } = SKColors.Black;

        /// <summary>
        /// Gets or sets the border thickness for this network control.
        /// </summary>
        public float BorderThickness { get; set; } = 1.5f;

        /// <summary>
        /// Gets or sets the corner radius for rounded elements.
        /// </summary>
        public float CornerRadius { get; set; } = 6f;

        /// <summary>
        /// Gets or sets whether this control is highlighted (for selection, hover, etc.).
        /// </summary>
        public bool IsHighlighted { get; set; } = false;

        /// <summary>
        /// Gets or sets the highlight color.
        /// </summary>
        public SKColor HighlightColor { get; set; } = new SKColor(0xFF, 0xEB, 0x3B); // Material Yellow 500

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkControl"/> class.
        /// </summary>
        protected NetworkControl()
        {
            // Set default text properties for network controls
            DisplayText = Name;
            TextPosition = TextPosition.Below;
            ShowDisplayText = true;
            TextColor = SKColors.Black;
            TextFontSize = 12f;
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