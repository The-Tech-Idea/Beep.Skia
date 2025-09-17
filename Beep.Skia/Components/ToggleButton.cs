using System;
using SkiaSharp;
using Beep.Skia.Model;
namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 toggle button component
    /// </summary>
    public class ToggleButton : MaterialControl
    {
        private string _text = "";
        private bool _checked = false;
        private SKColor _checkedBackgroundColor = MaterialDesignColors.Primary;
        private SKColor _uncheckedBackgroundColor = MaterialDesignColors.Surface;
        private SKColor _checkedTextColor = MaterialDesignColors.OnPrimary;
        private SKColor _uncheckedTextColor = MaterialDesignColors.Primary;
        private SKColor _borderColor = MaterialDesignColors.Outline;
        private float _borderWidth = 1;
        private float _cornerRadius = 4;
        private TextAlignment _textAlignment = TextAlignment.Center;
        private bool _isPressed = false;

        /// <summary>
        /// Gets or sets the button text
        /// </summary>
        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value ?? "";
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the toggle button is checked
        /// </summary>
        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked != value)
                {
                    _checked = value;
                    InvalidateVisual();
                    OnCheckedChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the background color when checked
        /// </summary>
        public SKColor CheckedBackgroundColor
        {
            get => _checkedBackgroundColor;
            set
            {
                if (_checkedBackgroundColor != value)
                {
                    _checkedBackgroundColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the background color when unchecked
        /// </summary>
        public SKColor UncheckedBackgroundColor
        {
            get => _uncheckedBackgroundColor;
            set
            {
                if (_uncheckedBackgroundColor != value)
                {
                    _uncheckedBackgroundColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the text color when checked
        /// </summary>
        public SKColor CheckedTextColor
        {
            get => _checkedTextColor;
            set
            {
                if (_checkedTextColor != value)
                {
                    _checkedTextColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the text color when unchecked
        /// </summary>
        public SKColor UncheckedTextColor
        {
            get => _uncheckedTextColor;
            set
            {
                if (_uncheckedTextColor != value)
                {
                    _uncheckedTextColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the border color
        /// </summary>
        public SKColor BorderColor
        {
            get => _borderColor;
            set
            {
                if (_borderColor != value)
                {
                    _borderColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the border width
        /// </summary>
        public float BorderWidth
        {
            get => _borderWidth;
            set
            {
                if (_borderWidth != value)
                {
                    _borderWidth = Math.Max(0, value);
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the corner radius
        /// </summary>
        public float CornerRadius
        {
            get => _cornerRadius;
            set
            {
                if (_cornerRadius != value)
                {
                    _cornerRadius = Math.Max(0, value);
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the text alignment
        /// </summary>
        public TextAlignment TextAlignment
        {
            get => _textAlignment;
            set
            {
                if (_textAlignment != value)
                {
                    _textAlignment = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Occurs when the checked state changes
        /// </summary>
        public event EventHandler CheckedChanged;

        /// <summary>
        /// Initializes a new instance of the ToggleButton class
        /// </summary>
        public ToggleButton()
        {
            Width = 120;
            Height = 40;
            _text = "Toggle";
        }

        /// <summary>
        /// Draws the toggle button content
        /// </summary>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds))
                return;

            var backgroundColor = _checked ? _checkedBackgroundColor : _uncheckedBackgroundColor;
            var textColor = _checked ? _checkedTextColor : _uncheckedTextColor;

            // Apply state layer for pressed state
            if (_isPressed)
            {
                var stateLayerOpacity = StateLayerOpacity.Press;
                backgroundColor = backgroundColor.WithAlpha((byte)(backgroundColor.Alpha * (1 - stateLayerOpacity) +
                    (MaterialDesignColors.Primary.Alpha * stateLayerOpacity)));
            }

            // Draw background
            using (var backgroundPaint = new SKPaint
            {
                Color = backgroundColor,
                IsAntialias = true
            })
            {
                var rect = new SKRect(X, Y, X + Width, Y + Height);
                canvas.DrawRoundRect(rect, _cornerRadius, _cornerRadius, backgroundPaint);
            }

            // Draw border
            if (_borderWidth > 0)
            {
                using (var borderPaint = new SKPaint
                {
                    Color = _borderColor,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = _borderWidth,
                    IsAntialias = true
                })
                {
                    var rect = new SKRect(X + _borderWidth / 2, Y + _borderWidth / 2,
                        X + Width - _borderWidth / 2, Y + Height - _borderWidth / 2);
                    canvas.DrawRoundRect(rect, _cornerRadius, _cornerRadius, borderPaint);
                }
            }

            // Draw text (modern SKFont metrics)
            if (!string.IsNullOrEmpty(_text))
            {
                using var font = new SKFont(SKTypeface.Default, 14);
                using var paint = new SKPaint { Color = textColor, IsAntialias = true };
                var metrics = font.Metrics;
                var textWidth = font.MeasureText(_text);
                float textX = GetTextX(textWidth);
                float baseline = Y + (Height + metrics.CapHeight) / 2f; // cap-height vertical centering
                canvas.DrawText(_text, textX, baseline, SKTextAlign.Left, font, paint);
            }
        }

        private float GetTextX(float textWidth)
        {
            switch (_textAlignment)
            {
                case TextAlignment.Center:
                    return X + (Width - textWidth) / 2;
                case TextAlignment.Right:
                    return X + Width - textWidth - 8;
                case TextAlignment.Left:
                default:
                    return X + 8;
            }
        }

        /// <summary>
        /// Handles mouse down events
        /// </summary>
        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            base.OnMouseDown(point, context);

            if (ContainsPoint(point))
            {
                _isPressed = true;
                InvalidateVisual();
            }
            return true;
        }

        /// <summary>
        /// Handles mouse up events
        /// </summary>
        protected override bool OnMouseUp(SKPoint point, InteractionContext context)
        {
            bool wasPressed = _isPressed;
            base.OnMouseUp(point, context);

            if (wasPressed)
            {
                _isPressed = false;
                InvalidateVisual();

                if (Bounds.Contains(point))
                {
                    Checked = !_checked;
                }
            }
            return true;
        }

        /// <summary>
        /// Handles mouse move events
        /// </summary>
        protected override bool OnMouseMove(SKPoint point, InteractionContext context)
        {
            base.OnMouseMove(point, context);

            if (_isPressed && !Bounds.Contains(point))
            {
                _isPressed = false;
                InvalidateVisual();
            }
            return true;
        }

        /// <summary>
        /// Raises the CheckedChanged event
        /// </summary>
        protected virtual void OnCheckedChanged()
        {
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
