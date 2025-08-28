using SkiaSharp;
using System;
using System.Text;

namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 text input field component.
    /// </summary>
    public class TextBox : MaterialControl
    {
        private string _text = string.Empty;
        private string _placeholder = string.Empty;
        private string _label = string.Empty;
        private string _errorMessage = string.Empty;
        private string _leadingIcon = string.Empty;
        private string _trailingIcon = string.Empty;
        private TextBoxVariant _variant = TextBoxVariant.Filled;
        private TextInputType _inputType = TextInputType.Text;
        private bool _isPassword = false;
        private bool _isReadOnly = false;
        private bool _isMultiline = false;
        private int _maxLength = int.MaxValue;
        private int _cursorPosition = 0;
        private int _selectionStart = -1;
        private int _selectionEnd = -1;
        private float _cornerRadius = 8.0f;
        private SKColor? _textColor;
        private SKColor? _placeholderColor;
        private SKColor? _labelColor;
        private SKColor? _borderColor;
        private SKColor? _backgroundColor;
        private float _fontSize = 14.0f;
        private bool _hasFocus = false;
        private bool _showCursor = false;
        private DateTime _lastCursorBlink = DateTime.Now;

        /// <summary>
        /// Material Design 3.0 text box variants.
        /// </summary>
        public enum TextBoxVariant
        {
            Filled,
            Outlined
        }

        /// <summary>
        /// Text input types.
        /// </summary>
        public enum TextInputType
        {
            Text,
            Email,
            Password,
            Number,
            Phone,
            Url
        }

        /// <summary>
        /// Gets or sets the text content of the text box.
        /// </summary>
        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value ?? string.Empty;
                    if (_text.Length > _maxLength)
                    {
                        _text = _text.Substring(0, _maxLength);
                    }
                    _cursorPosition = Math.Min(_cursorPosition, _text.Length);
                    InvalidateVisual();
                    TextChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Gets or sets the placeholder text.
        /// </summary>
        public string Placeholder
        {
            get => _placeholder;
            set
            {
                if (_placeholder != value)
                {
                    _placeholder = value ?? string.Empty;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the label text displayed above the text box.
        /// </summary>
        public string Label
        {
            get => _label;
            set
            {
                if (_label != value)
                {
                    _label = value ?? string.Empty;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value ?? string.Empty;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the leading icon SVG.
        /// </summary>
        public string LeadingIcon
        {
            get => _leadingIcon;
            set
            {
                if (_leadingIcon != value)
                {
                    _leadingIcon = value ?? string.Empty;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the trailing icon SVG.
        /// </summary>
        public string TrailingIcon
        {
            get => _trailingIcon;
            set
            {
                if (_trailingIcon != value)
                {
                    _trailingIcon = value ?? string.Empty;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the text box variant.
        /// </summary>
        public TextBoxVariant Variant
        {
            get => _variant;
            set
            {
                if (_variant != value)
                {
                    _variant = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the input type.
        /// </summary>
        public TextInputType InputType
        {
            get => _inputType;
            set
            {
                if (_inputType != value)
                {
                    _inputType = value;
                    _isPassword = (_inputType == TextInputType.Password);
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the text box is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get => _isReadOnly;
            set
            {
                if (_isReadOnly != value)
                {
                    _isReadOnly = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the text box supports multiline input.
        /// </summary>
        public bool IsMultiline
        {
            get => _isMultiline;
            set
            {
                if (_isMultiline != value)
                {
                    _isMultiline = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum length of text.
        /// </summary>
        public int MaxLength
        {
            get => _maxLength;
            set
            {
                if (_maxLength != value)
                {
                    _maxLength = Math.Max(0, value);
                    if (_text.Length > _maxLength)
                    {
                        Text = _text.Substring(0, _maxLength);
                    }
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the corner radius.
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
        /// Gets or sets the text color.
        /// </summary>
        public SKColor TextColor
        {
            get => _textColor ?? MaterialControl.MaterialColors.OnSurface;
            set
            {
                _textColor = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the placeholder color.
        /// </summary>
        public SKColor PlaceholderColor
        {
            get => _placeholderColor ?? MaterialControl.MaterialColors.OnSurfaceVariant;
            set
            {
                _placeholderColor = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the label color.
        /// </summary>
        public SKColor LabelColor
        {
            get => _labelColor ?? MaterialControl.MaterialColors.OnSurfaceVariant;
            set
            {
                _labelColor = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the border color.
        /// </summary>
        public SKColor BorderColor
        {
            get => _borderColor ?? GetBorderColorForState();
            set
            {
                _borderColor = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the background color.
        /// </summary>
        public SKColor BackgroundColor
        {
            get => _backgroundColor ?? GetBackgroundColorForVariant();
            set
            {
                _backgroundColor = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the font size.
        /// </summary>
        public float FontSize
        {
            get => _fontSize;
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = Math.Max(8, value);
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets whether the text box has focus.
        /// </summary>
        public bool HasFocus => _hasFocus;

        /// <summary>
        /// Event raised when text changes.
        /// </summary>
        public event EventHandler TextChanged;

        /// <summary>
        /// Event raised when the text box gains focus.
        /// </summary>
        public event EventHandler GotFocus;

        /// <summary>
        /// Event raised when the text box loses focus.
        /// </summary>
        public event EventHandler LostFocus;

        /// <summary>
        /// Initializes a new instance of the TextBox class.
        /// </summary>
        public TextBox()
        {
            Width = 200;
            Height = 48;
            Name = "TextBox";
            Elevation = 0;
        }

        /// <summary>
        /// Initializes a new instance of the TextBox class with specified dimensions.
        /// </summary>
        public TextBox(float width, float height) : this()
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Gets the background color based on variant.
        /// </summary>
        private SKColor GetBackgroundColorForVariant()
        {
            if (_variant == TextBoxVariant.Filled)
            {
                return MaterialControl.MaterialColors.Surface;
            }
            return SKColors.Transparent;
        }

        /// <summary>
        /// Gets the border color based on current state.
        /// </summary>
        private SKColor GetBorderColorForState()
        {
            if (!string.IsNullOrEmpty(_errorMessage))
            {
                return MaterialControl.MaterialColors.Error;
            }

            if (_hasFocus)
            {
                return MaterialControl.MaterialColors.Primary;
            }

            if (State == ControlState.Hovered)
            {
                return MaterialControl.MaterialColors.OnSurfaceVariant;
            }

            return _variant == TextBoxVariant.Outlined
                ? MaterialControl.MaterialColors.Outline
                : SKColors.Transparent;
        }

        /// <summary>
        /// Gets the display text (masked for password fields).
        /// </summary>
        private string GetDisplayText()
        {
            if (_isPassword && !string.IsNullOrEmpty(_text))
            {
                return new string('•', _text.Length);
            }
            return _text;
        }

        /// <summary>
        /// Updates the cursor blink animation.
        /// </summary>
        private void UpdateCursorBlink()
        {
            var now = DateTime.Now;
            if ((now - _lastCursorBlink).TotalMilliseconds > 500)
            {
                _showCursor = !_showCursor;
                _lastCursorBlink = now;
                if (_hasFocus)
                {
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Draws the text box's content.
        /// </summary>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            UpdateCursorBlink();

            var textBoxRect = new SKRect(0, 0, Width, Height);

            // Draw background
            DrawBackground(canvas, textBoxRect);

            // Draw border
            DrawBorder(canvas, textBoxRect);

            // Draw label if present
            if (!string.IsNullOrEmpty(_label))
            {
                DrawLabel(canvas);
            }

            // Draw leading icon
            float iconOffset = 0;
            if (!string.IsNullOrEmpty(_leadingIcon))
            {
                iconOffset = DrawLeadingIcon(canvas);
            }

            // Draw trailing icon
            float trailingIconOffset = 0;
            if (!string.IsNullOrEmpty(_trailingIcon))
            {
                trailingIconOffset = DrawTrailingIcon(canvas);
            }

            // Draw text
            DrawText(canvas, iconOffset, trailingIconOffset);

            // Draw cursor
            if (_hasFocus && _showCursor && !_isReadOnly)
            {
                DrawCursor(canvas, iconOffset);
            }

            // Draw error message
            if (!string.IsNullOrEmpty(_errorMessage))
            {
                DrawErrorMessage(canvas);
            }
        }

        /// <summary>
        /// Draws the background.
        /// </summary>
        private void DrawBackground(SKCanvas canvas, SKRect rect)
        {
            using (var backgroundPaint = new SKPaint
            {
                Color = BackgroundColor,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            })
            {
                if (_cornerRadius > 0)
                {
                    canvas.DrawRoundRect(rect, _cornerRadius, _cornerRadius, backgroundPaint);
                }
                else
                {
                    canvas.DrawRect(rect, backgroundPaint);
                }
            }
        }

        /// <summary>
        /// Draws the border.
        /// </summary>
        private void DrawBorder(SKCanvas canvas, SKRect rect)
        {
            if (_variant == TextBoxVariant.Outlined || _hasFocus || !string.IsNullOrEmpty(_errorMessage))
            {
                using (var borderPaint = new SKPaint
                {
                    Color = BorderColor,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = _hasFocus ? 2.0f : 1.0f,
                    IsAntialias = true
                })
                {
                    if (_cornerRadius > 0)
                    {
                        canvas.DrawRoundRect(rect, _cornerRadius, _cornerRadius, borderPaint);
                    }
                    else
                    {
                        canvas.DrawRect(rect, borderPaint);
                    }
                }
            }
        }

        /// <summary>
        /// Draws the label.
        /// </summary>
        private void DrawLabel(SKCanvas canvas)
        {
            using (var labelPaint = new SKPaint
            {
                Color = _hasFocus || !string.IsNullOrEmpty(_text) ? LabelColor : PlaceholderColor,
                TextSize = 12,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Roboto", SKFontStyle.Normal)
            })
            {
                float labelY = -8;
                canvas.DrawText(_label, 0, labelY, labelPaint);
            }
        }

        /// <summary>
        /// Draws the leading icon.
        /// </summary>
        private float DrawLeadingIcon(SKCanvas canvas)
        {
            float iconSize = 20;
            float iconX = 12;
            float iconY = (Height - iconSize) / 2;

            DrawSvgIcon(canvas, _leadingIcon, iconX, iconY, iconSize, iconSize, PlaceholderColor);
            return iconSize + 24; // Icon size + padding
        }

        /// <summary>
        /// Draws the trailing icon.
        /// </summary>
        private float DrawTrailingIcon(SKCanvas canvas)
        {
            float iconSize = 20;
            float iconX = Width - iconSize - 12;
            float iconY = (Height - iconSize) / 2;

            DrawSvgIcon(canvas, _trailingIcon, iconX, iconY, iconSize, iconSize, PlaceholderColor);
            return iconSize + 24; // Icon size + padding
        }

        /// <summary>
        /// Draws the text content.
        /// </summary>
        private void DrawText(SKCanvas canvas, float leadingOffset, float trailingOffset)
        {
            string displayText = GetDisplayText();
            bool isPlaceholder = string.IsNullOrEmpty(displayText);

            using (var textPaint = new SKPaint
            {
                Color = isPlaceholder ? PlaceholderColor : TextColor,
                TextSize = _fontSize,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Roboto", SKFontStyle.Normal)
            })
            {
                string textToDraw = isPlaceholder ? _placeholder : displayText;
                float textX = leadingOffset + 16;
                float textY = Height / 2 + _fontSize / 3;

                // Handle text alignment and clipping
                float maxTextWidth = Width - leadingOffset - trailingOffset - 32;
                if (maxTextWidth > 0)
                {
                    // Simple text clipping for now
                    canvas.DrawText(textToDraw, textX, textY, textPaint);
                }
            }
        }

        /// <summary>
        /// Draws the cursor.
        /// </summary>
        private void DrawCursor(SKCanvas canvas, float leadingOffset)
        {
            if (_cursorPosition < 0 || _cursorPosition > _text.Length) return;

            using (var cursorPaint = new SKPaint
            {
                Color = TextColor,
                StrokeWidth = 1,
                Style = SKPaintStyle.Stroke
            })
            {
                string textBeforeCursor = GetDisplayText().Substring(0, _cursorPosition);
                float cursorX = leadingOffset + 16;

                if (!string.IsNullOrEmpty(textBeforeCursor))
                {
                    using (var measurePaint = new SKPaint
                    {
                        TextSize = _fontSize,
                        Typeface = SKTypeface.FromFamilyName("Roboto", SKFontStyle.Normal)
                    })
                    {
                        cursorX += measurePaint.MeasureText(textBeforeCursor);
                    }
                }

                float cursorY1 = Height / 2 - _fontSize / 2;
                float cursorY2 = Height / 2 + _fontSize / 2;

                canvas.DrawLine(cursorX, cursorY1, cursorX, cursorY2, cursorPaint);
            }
        }

        /// <summary>
        /// Draws the error message.
        /// </summary>
        private void DrawErrorMessage(SKCanvas canvas)
        {
            using (var errorPaint = new SKPaint
            {
                Color = MaterialControl.MaterialColors.Error,
                TextSize = 12,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Roboto", SKFontStyle.Normal)
            })
            {
                float errorY = Height + 20;
                canvas.DrawText(_errorMessage, 0, errorY, errorPaint);
            }
        }

        /// <summary>
        /// Handles mouse down events.
        /// </summary>
        protected override bool OnMouseDown(SKPoint location, InteractionContext context)
        {
            if (!_isReadOnly)
            {
                Focus();
                // Simple cursor positioning (would need more sophisticated text measurement)
                _cursorPosition = _text.Length;
                InvalidateVisual();
            }
            return base.OnMouseDown(location, context);
        }

        /// <summary>
        /// Handles mouse enter event.
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
        /// Sets focus to the text box.
        /// </summary>
        public void Focus()
        {
            if (!_hasFocus)
            {
                _hasFocus = true;
                _showCursor = true;
                _lastCursorBlink = DateTime.Now;
                GotFocus?.Invoke(this, EventArgs.Empty);
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Removes focus from the text box.
        /// </summary>
        public void Blur()
        {
            if (_hasFocus)
            {
                _hasFocus = false;
                _showCursor = false;
                LostFocus?.Invoke(this, EventArgs.Empty);
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Inserts text at the current cursor position.
        /// </summary>
        public void InsertText(string text)
        {
            if (_isReadOnly || string.IsNullOrEmpty(text)) return;

            string newText = _text.Insert(_cursorPosition, text);
            if (newText.Length <= _maxLength)
            {
                Text = newText;
                _cursorPosition += text.Length;
            }
        }

        /// <summary>
        /// Deletes text at the current cursor position.
        /// </summary>
        public void DeleteText(bool forward = false)
        {
            if (_isReadOnly || string.IsNullOrEmpty(_text)) return;

            if (forward)
            {
                // Delete forward
                if (_cursorPosition < _text.Length)
                {
                    Text = _text.Remove(_cursorPosition, 1);
                }
            }
            else
            {
                // Delete backward
                if (_cursorPosition > 0)
                {
                    Text = _text.Remove(_cursorPosition - 1, 1);
                    _cursorPosition--;
                }
            }
        }

        /// <summary>
        /// Moves the cursor.
        /// </summary>
        public void MoveCursor(int delta)
        {
            _cursorPosition = Math.Clamp(_cursorPosition + delta, 0, _text.Length);
            InvalidateVisual();
        }

        /// <summary>
        /// Selects all text.
        /// </summary>
        public void SelectAll()
        {
            _selectionStart = 0;
            _selectionEnd = _text.Length;
            InvalidateVisual();
        }

        /// <summary>
        /// Clears the selection.
        /// </summary>
        public void ClearSelection()
        {
            _selectionStart = -1;
            _selectionEnd = -1;
            InvalidateVisual();
        }
    }
}
