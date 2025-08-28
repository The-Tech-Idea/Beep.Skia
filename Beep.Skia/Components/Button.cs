using SkiaSharp;
using System;

namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 button component with SVG icon support.
    /// </summary>
    public class Button : MaterialControl
    {
        private string _text = "Button";
        private SKColor _backgroundColor = MaterialControl.MaterialColors.Primary;
        private SKColor _textColor = MaterialControl.MaterialColors.OnPrimary;
        private float _cornerRadius = 20; // Material Design 3.0 button corner radius
        private ButtonVariant _variant = ButtonVariant.Filled;
        private string _leadingIcon;
        private string _trailingIcon;
        private string _errorMessage;
        private string _title;

        /// <summary>
        /// Material Design 3.0 button variants.
        /// </summary>
        public enum ButtonVariant
        {
            Filled,
            Outlined,
            Text,
            Elevated,
            Tonal
        }

        /// <summary>
        /// Gets or sets the text displayed on the button.
        /// </summary>
        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the background color of the button.
        /// </summary>
        public SKColor BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                if (_backgroundColor != value)
                {
                    _backgroundColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the text color of the button.
        /// </summary>
        public SKColor TextColor
        {
            get => _textColor;
            set
            {
                if (_textColor != value)
                {
                    _textColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the corner radius of the button.
        /// </summary>
        public float CornerRadius
        {
            get => _cornerRadius;
            set
            {
                if (_cornerRadius != value)
                {
                    _cornerRadius = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the button variant (Material Design 3.0).
        /// </summary>
        public ButtonVariant Variant
        {
            get => _variant;
            set
            {
                if (_variant != value)
                {
                    _variant = value;
                    UpdateVariantProperties();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the leading SVG icon path or URL.
        /// </summary>
        public string LeadingIcon
        {
            get => _leadingIcon;
            set
            {
                if (_leadingIcon != value)
                {
                    _leadingIcon = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the trailing SVG icon path or URL.
        /// </summary>
        public string TrailingIcon
        {
            get => _trailingIcon;
            set
            {
                if (_trailingIcon != value)
                {
                    _trailingIcon = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the error message to display below the button.
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the title text to display above the button.
        /// </summary>
        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the Button class.
        /// </summary>
        public Button()
        {
            Width = 120;
            Height = 40;
            Name = "Button";
            Elevation = 0; // Default elevation for filled button
        }

        /// <summary>
        /// Initializes a new instance of the Button class with specified text.
        /// </summary>
        public Button(string text) : this()
        {
            Text = text;
        }

        /// <summary>
        /// Updates properties based on the selected variant.
        /// </summary>
        private void UpdateVariantProperties()
        {
            switch (_variant)
            {
                case ButtonVariant.Filled:
                    BackgroundColor = MaterialControl.MaterialColors.Primary;
                    TextColor = MaterialControl.MaterialColors.OnPrimary;
                    Elevation = 0;
                    break;

                case ButtonVariant.Outlined:
                    BackgroundColor = SKColors.Transparent;
                    TextColor = MaterialControl.MaterialColors.Primary;
                    Elevation = 0;
                    break;

                case ButtonVariant.Text:
                    BackgroundColor = SKColors.Transparent;
                    TextColor = MaterialControl.MaterialColors.Primary;
                    Elevation = 0;
                    break;

                case ButtonVariant.Elevated:
                    BackgroundColor = MaterialControl.MaterialColors.Surface;
                    TextColor = MaterialControl.MaterialColors.Primary;
                    Elevation = 1;
                    break;

                case ButtonVariant.Tonal:
                    BackgroundColor = MaterialControl.MaterialColors.SecondaryContainer;
                    TextColor = MaterialControl.MaterialColors.OnSecondaryContainer;
                    Elevation = 0;
                    break;
            }
        }

        /// <summary>
        /// Draws the button's content.
        /// </summary>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            var buttonBounds = new SKRect(0, 0, Width, Height);

            // Draw elevation shadow
            DrawElevationShadow(canvas, buttonBounds);

            // Draw background
            using (var paint = new SKPaint
            {
                Color = GetStateAwareColor(
                    BackgroundColor,
                    BackgroundColor.WithAlpha((byte)(BackgroundColor.Alpha * 0.9f)),
                    BackgroundColor.WithAlpha((byte)(BackgroundColor.Alpha * 0.8f))),
                Style = SKPaintStyle.Fill
            })
            {
                if (_cornerRadius > 0)
                {
                    canvas.DrawRoundRect(buttonBounds, _cornerRadius, _cornerRadius, paint);
                }
                else
                {
                    canvas.DrawRect(buttonBounds, paint);
                }
            }

            // Draw outline for outlined variant
            if (_variant == ButtonVariant.Outlined)
            {
                using (var outlinePaint = new SKPaint
                {
                    Color = MaterialControl.MaterialColors.Outline,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1
                })
                {
                    if (_cornerRadius > 0)
                    {
                        canvas.DrawRoundRect(buttonBounds, _cornerRadius, _cornerRadius, outlinePaint);
                    }
                    else
                    {
                        canvas.DrawRect(buttonBounds, outlinePaint);
                    }
                }
            }

            // Draw state layer
            DrawStateLayer(canvas, buttonBounds, MaterialControl.MaterialColors.OnPrimary);

            // Calculate content area (accounting for padding)
            var padding = 16;
            var contentBounds = new SKRect(
                buttonBounds.Left + padding,
                buttonBounds.Top + padding,
                buttonBounds.Right - padding,
                buttonBounds.Bottom - padding);

            // Calculate positions for icons and text
            var hasLeadingIcon = !string.IsNullOrEmpty(LeadingIcon);
            var hasTrailingIcon = !string.IsNullOrEmpty(TrailingIcon);
            var hasText = !string.IsNullOrEmpty(Text);
            var iconSize = Math.Min(contentBounds.Height, 24); // Material Design icon size

            // Calculate total content width
            var totalContentWidth = 0f;
            if (hasLeadingIcon) totalContentWidth += iconSize + 8;
            if (hasText)
            {
                using (var textPaint = new SKPaint { TextSize = 14, IsAntialias = true })
                {
                    var textBounds = new SKRect();
                    textPaint.MeasureText(Text, ref textBounds);
                    totalContentWidth += textBounds.Width;
                }
            }
            if (hasTrailingIcon) totalContentWidth += iconSize + 8;
            if (hasLeadingIcon && hasText) totalContentWidth += 8;
            if (hasText && hasTrailingIcon) totalContentWidth += 8;

            // Center the content horizontally
            var startX = contentBounds.Left + (contentBounds.Width - totalContentWidth) / 2;
            var currentX = startX;

            // Draw leading icon
            if (hasLeadingIcon)
            {
                var iconRect = new SKRect(
                    currentX,
                    contentBounds.Top + (contentBounds.Height - iconSize) / 2,
                    currentX + iconSize,
                    contentBounds.Top + (contentBounds.Height - iconSize) / 2 + iconSize);

                DrawSvgIcon(canvas, iconRect, LeadingIcon);
                currentX += iconSize + 8;
            }

            // Draw text
            if (hasText)
            {
                using (var textPaint = new SKPaint
                {
                    Color = TextColor,
                    TextSize = 14, // Material Design 3.0 label large
                    IsAntialias = true
                })
                {
                    var textBounds = new SKRect();
                    textPaint.MeasureText(Text, ref textBounds);

                    var textY = contentBounds.Top + (contentBounds.Height + textBounds.Height) / 2;
                    canvas.DrawText(Text, currentX, textY, textPaint);
                    currentX += textBounds.Width + 8;
                }
            }

            // Draw trailing icon
            if (hasTrailingIcon)
            {
                var iconRect = new SKRect(
                    currentX,
                    contentBounds.Top + (contentBounds.Height - iconSize) / 2,
                    currentX + iconSize,
                    contentBounds.Top + (contentBounds.Height - iconSize) / 2 + iconSize);

                DrawSvgIcon(canvas, iconRect, TrailingIcon);
            }
        }

        /// <summary>
        /// Draws the complete button including title and error message.
        /// </summary>
        public override void Draw(SKCanvas canvas, DrawingContext context)
        {
            // Calculate total height needed for title and error message
            var titleHeight = !string.IsNullOrEmpty(Title) ? 20f : 0f;
            var errorHeight = !string.IsNullOrEmpty(ErrorMessage) ? 16f : 0f;
            var totalHeight = Height + titleHeight + errorHeight;

            // Save the original bounds
            var originalBounds = Bounds;

            // Draw title if present
            if (!string.IsNullOrEmpty(Title))
            {
                using (var titlePaint = new SKPaint
                {
                    Color = MaterialControl.MaterialColors.OnSurface,
                    TextSize = 12,
                    IsAntialias = true
                })
                {
                    var titleY = Y - 4; // Small gap above the button
                    canvas.DrawText(Title, X, titleY, titlePaint);
                }
            }

            // Draw the button content (this will call DrawContent)
            base.Draw(canvas, context);

            // Draw error message if present
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                using (var errorPaint = new SKPaint
                {
                    Color = MaterialControl.MaterialColors.Error,
                    TextSize = 12,
                    IsAntialias = true
                })
                {
                    var errorY = Y + Height + 20; // Below the button
                    canvas.DrawText(ErrorMessage, X, errorY, errorPaint);
                }
            }
        }

        /// <summary>
        /// Handles mouse enter event for hover state.
        /// </summary>
        protected override void OnMouseEnter()
        {
            base.OnMouseEnter();
            State = ControlState.Hovered;
        }

        /// <summary>
        /// Handles mouse leave event.
        /// </summary>
        protected override void OnMouseLeave()
        {
            base.OnMouseLeave();
            State = ControlState.Normal;
        }

        /// <summary>
        /// Handles mouse down event for pressed state.
        /// </summary>
        protected override bool OnMouseDown(SKPoint location, InteractionContext context)
        {
            var handled = base.OnMouseDown(location, context);
            State = ControlState.Pressed;
            return handled;
        }

        /// <summary>
        /// Handles mouse up event.
        /// </summary>
        protected override bool OnMouseUp(SKPoint location, InteractionContext context)
        {
            var handled = base.OnMouseUp(location, context);
            State = ControlState.Normal;
            return handled;
        }
    }
}
