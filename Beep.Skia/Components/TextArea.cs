using SkiaSharp;
using System;

namespace Beep.Skia.Components
{
    /// <summary>
    /// A multi-line text input control.
    /// </summary>
    public class TextArea : MaterialControl
    {
        private string _text = "";
        private string _placeholder = "";
        private TextAlignment _textAlignment = TextAlignment.Left;
        private bool _multiline = true;
        private bool _readOnly = false;
        private int _maxLength = 0;

        /// <summary>
        /// Gets or sets the text in the text area.
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
        /// Gets or sets the placeholder text.
        /// </summary>
        public string Placeholder
        {
            get => _placeholder;
            set
            {
                if (_placeholder != value)
                {
                    _placeholder = value ?? "";
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the text alignment.
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
        /// Gets or sets whether the text area supports multiple lines.
        /// </summary>
        public bool Multiline
        {
            get => _multiline;
            set
            {
                if (_multiline != value)
                {
                    _multiline = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the text area is read-only.
        /// </summary>
        public bool ReadOnly
        {
            get => _readOnly;
            set
            {
                if (_readOnly != value)
                {
                    _readOnly = value;
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
                    _maxLength = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the TextArea class.
        /// </summary>
        public TextArea()
        {
            Width = 200;
            Height = 100;
        }

        /// <summary>
        /// Draws the text area content.
        /// </summary>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Draw background
            using (var paint = new SKPaint())
            {
                paint.Color = MaterialColors.Surface;
                paint.Style = SKPaintStyle.Fill;
                canvas.DrawRect(X, Y, X + Width, Y + Height, paint);

                // Draw border
                paint.Color = MaterialColors.OnSurfaceVariant;
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = 1;
                canvas.DrawRect(X, Y, X + Width, Y + Height, paint);
            }

            // Draw text
            if (!string.IsNullOrEmpty(_text) || !string.IsNullOrEmpty(_placeholder))
            {
                using (var paint = new SKPaint())
                {
                    paint.Color = !string.IsNullOrEmpty(_text) ? MaterialColors.OnSurface : MaterialColors.OnSurfaceVariant;
                    paint.Style = SKPaintStyle.Fill;

                    using (var font = new SKFont())
                    {
                        font.Size = 14;
                        string displayText = !string.IsNullOrEmpty(_text) ? _text : _placeholder;
                        float textY = 20;

                        // Handle multiline text
                        if (_multiline && displayText.Contains('\n'))
                        {
                            var lines = displayText.Split('\n');
                            foreach (var line in lines)
                            {
                                if (textY + font.Size > Height) break;

                                float textX = GetTextX(line, font, paint);
                                canvas.DrawText(line, textX, textY, SKTextAlign.Left, font, paint);
                                textY += font.Size + 4;
                            }
                        }
                        else
                        {
                            float textX = GetTextX(displayText, font, paint);
                            canvas.DrawText(displayText, textX, textY, SKTextAlign.Left, font, paint);
                        }
                    }
                }
            }
        }

        private float GetTextX(string text, SKFont font, SKPaint paint)
        {
            SKRect textBounds = new SKRect();
            font.MeasureText(text, out textBounds);
            float textWidth = textBounds.Width;

            switch (_textAlignment)
            {
                case TextAlignment.Left:
                    return 8;
                case TextAlignment.Center:
                    return (Width - textWidth) / 2;
                case TextAlignment.Right:
                    return Width - textWidth - 8;
                default:
                    return 8;
            }
        }
    }
}
