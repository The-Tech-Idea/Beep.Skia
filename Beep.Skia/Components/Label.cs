using SkiaSharp;

namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 text label component with typography support and optional SVG icons.
    /// </summary>
    public class Label : MaterialControl
    {
        private string _text = "Label";
        private LabelStyle _labelStyle = LabelStyle.BodyLarge;
        private SKTextAlign _textAlign = SKTextAlign.Left;
        private float? _customFontSize;
        private SKColor? _customTextColor;
        private string _leadingIcon;
        private string _trailingIcon;
        private string _errorMessage;
        private string _title;

        /// <summary>
        /// Material Design 3.0 typography styles for labels.
        /// </summary>
        public enum LabelStyle
        {
            DisplayLarge,
            DisplayMedium,
            DisplaySmall,
            HeadlineLarge,
            HeadlineMedium,
            HeadlineSmall,
            TitleLarge,
            TitleMedium,
            TitleSmall,
            BodyLarge,
            BodyMedium,
            BodySmall,
            LabelLarge,
            LabelMedium,
            LabelSmall
        }

        /// <summary>
        /// Gets or sets the text displayed by the label.
        /// </summary>
        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    UpdateBounds();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the Material Design 3.0 typography style.
        /// </summary>
        public LabelStyle Style
        {
            get => _labelStyle;
            set
            {
                if (_labelStyle != value)
                {
                    _labelStyle = value;
                    UpdateBounds();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the text alignment.
        /// </summary>
        public SKTextAlign TextAlign
        {
            get => _textAlign;
            set
            {
                if (_textAlign != value)
                {
                    _textAlign = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the font size (for backward compatibility).
        /// </summary>
        public float FontSize
        {
            get => _customFontSize ?? GetFontSizeForStyle(_labelStyle);
            set
            {
                _customFontSize = value;
                UpdateBounds();
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets the font weight based on the current typography style.
        /// </summary>
        public SKFontStyleWeight FontWeight => GetFontWeightForStyle(_labelStyle);

        /// <summary>
        /// Gets or sets the text color (for backward compatibility).
        /// </summary>
        public SKColor TextColor
        {
            get => _customTextColor ?? MaterialControl.MaterialColors.OnSurface;
            set
            {
                _customTextColor = value;
                InvalidateVisual();
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
                    UpdateBounds();
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
                    UpdateBounds();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the error message to display below the label.
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
        /// Gets or sets the title text to display above the label.
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
        /// Initializes a new instance of the Label class.
        /// </summary>
        public Label()
        {
            Name = "Label";
            Width = 100;
            Height = 24;
            Elevation = 0; // Labels don't have elevation
        }

        /// <summary>
        /// Initializes a new instance of the Label class with specified text.
        /// </summary>
        public Label(string text) : this()
        {
            Text = text;
        }

        /// <summary>
        /// Initializes a new instance of the Label class with specified text and style.
        /// </summary>
        public Label(string text, LabelStyle style) : this(text)
        {
            Style = style;
        }

        /// <summary>
        /// Gets the font size for a given typography style.
        /// </summary>
        private static float GetFontSizeForStyle(LabelStyle style)
        {
            return style switch
            {
                LabelStyle.DisplayLarge => 57,
                LabelStyle.DisplayMedium => 45,
                LabelStyle.DisplaySmall => 36,
                LabelStyle.HeadlineLarge => 32,
                LabelStyle.HeadlineMedium => 28,
                LabelStyle.HeadlineSmall => 24,
                LabelStyle.TitleLarge => 22,
                LabelStyle.TitleMedium => 16,
                LabelStyle.TitleSmall => 14,
                LabelStyle.BodyLarge => 16,
                LabelStyle.BodyMedium => 14,
                LabelStyle.BodySmall => 12,
                LabelStyle.LabelLarge => 14,
                LabelStyle.LabelMedium => 12,
                LabelStyle.LabelSmall => 11,
                _ => 14
            };
        }

        /// <summary>
        /// Gets the font weight for a given typography style.
        /// </summary>
        private static SKFontStyleWeight GetFontWeightForStyle(LabelStyle style)
        {
            return style switch
            {
                LabelStyle.DisplayLarge or LabelStyle.DisplayMedium or LabelStyle.DisplaySmall or
                LabelStyle.HeadlineLarge or LabelStyle.HeadlineMedium or LabelStyle.HeadlineSmall =>
                    SKFontStyleWeight.Bold,
                LabelStyle.TitleLarge or LabelStyle.TitleMedium or LabelStyle.TitleSmall =>
                    SKFontStyleWeight.SemiBold,
                LabelStyle.BodyLarge or LabelStyle.BodyMedium or LabelStyle.BodySmall or
                LabelStyle.LabelLarge or LabelStyle.LabelMedium or LabelStyle.LabelSmall =>
                    SKFontStyleWeight.Normal,
                _ => SKFontStyleWeight.Normal
            };
        }

        /// <summary>
        /// Draws the label's content.
        /// </summary>
        /// <summary>
        /// Draws the label's content including title and error messages.
        /// </summary>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Draw title if present (absolute coordinates)
            if (!string.IsNullOrEmpty(Title))
            {
                using var titleFont = new SKFont(SKTypeface.Default, 12);
                using var titlePaint = new SKPaint { Color = MaterialControl.MaterialColors.OnSurface, IsAntialias = true };
                var baseline = Y - 4; // retain layout intent
                canvas.DrawText(Title, X, baseline, SKTextAlign.Left, titleFont, titlePaint);
            }

            // If no main text, optionally draw error message and exit
            if (string.IsNullOrEmpty(Text))
            {
                if (!string.IsNullOrEmpty(ErrorMessage))
                {
                    using var errorFont = new SKFont(SKTypeface.Default, 12);
                    using var errorPaint = new SKPaint { Color = MaterialControl.MaterialColors.Error, IsAntialias = true };
                    var baseline = Y + Height + 16;
                    canvas.DrawText(ErrorMessage, X, baseline, SKTextAlign.Left, errorFont, errorPaint);
                }
                return;
            }

            // Content area in absolute coordinates
            var padding = 8;
            var contentBounds = new SKRect(
                X + padding,
                Y + padding,
                X + Width - padding,
                Y + Height - padding);

            // Calculate positions for icons and text
            var hasLeadingIcon = !string.IsNullOrEmpty(LeadingIcon);
            var hasTrailingIcon = !string.IsNullOrEmpty(TrailingIcon);
            var hasText = !string.IsNullOrEmpty(Text);
            var iconSize = Math.Min(contentBounds.Height, 20); // Material Design icon size for labels

            // Calculate total content width
            var totalContentWidth = 0f;
            if (hasLeadingIcon) totalContentWidth += iconSize + 4;
            if (hasText)
            {
                using (var font = new SKFont { Size = FontSize })
                {
                    var textBounds = new SKRect();
                    font.MeasureText(Text, out textBounds);
                    totalContentWidth += textBounds.Width;
                }
            }
            if (hasTrailingIcon) totalContentWidth += iconSize + 4;

            // Calculate starting position based on text alignment
            var startX = contentBounds.Left;
            switch (TextAlign)
            {
                case SKTextAlign.Left:
                    startX = contentBounds.Left;
                    break;
                case SKTextAlign.Center:
                    startX = contentBounds.Left + (contentBounds.Width - totalContentWidth) / 2;
                    break;
                case SKTextAlign.Right:
                    startX = contentBounds.Right - totalContentWidth;
                    break;
            }

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
                currentX += iconSize + 4;
            }

            // Draw text
            if (hasText)
            {
                using (var font = new SKFont { Size = FontSize })
                {
                    using (var paint = new SKPaint
                    {
                        Color = TextColor,
                        IsAntialias = true
                    })
                    {
                        var textBounds = new SKRect();
                        font.MeasureText(Text, out textBounds);

                        // Center vertically
                        var textY = contentBounds.Top + (contentBounds.Height + textBounds.Height) / 2;
                        canvas.DrawText(Text, currentX, textY, SKTextAlign.Left, font, paint);
                        currentX += textBounds.Width + 4;
                    }
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

            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                using var errorFont = new SKFont(SKTypeface.Default, 12);
                using var errorPaint = new SKPaint { Color = MaterialControl.MaterialColors.Error, IsAntialias = true };
                var baseline = Y + Height + 16;
                canvas.DrawText(ErrorMessage, X, baseline, SKTextAlign.Left, errorFont, errorPaint);
            }
        }

        /// <summary>
        /// Updates the bounds of the label based on its text content and typography style.
        /// </summary>
        protected override void UpdateBounds()
        {
            if (string.IsNullOrEmpty(Text))
            {
                Width = 50;
                Height = FontSize * 1.2f;
                return;
            }

            using (var font = new SKFont { Size = FontSize })
            {
                var textBounds = new SKRect();
                font.MeasureText(Text, out textBounds);

                var leadingIconWidth = !string.IsNullOrEmpty(LeadingIcon) ? 24 : 0; // Icon width + spacing
                var trailingIconWidth = !string.IsNullOrEmpty(TrailingIcon) ? 24 : 0; // Icon width + spacing
                var totalIconWidth = leadingIconWidth + trailingIconWidth;

                Width = textBounds.Width + totalIconWidth + 16; // Add padding
                Height = textBounds.Height + 16; // Add padding
            }

            base.UpdateBounds();
        }
    }
}
