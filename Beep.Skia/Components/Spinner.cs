using SkiaSharp;
using System;
using Beep.Skia.Model;
namespace Beep.Skia.Components
{
    /// <summary>
    /// A spinner component for indicating loading or processing.
    /// </summary>
    public class Spinner : MaterialControl
    {
        private SpinnerStyle _style = SpinnerStyle.Standard;
        private float _rotation = 0;
        private float _speed = 5.0f; // degrees per frame
        private SKColor _color = MaterialControl.MaterialColors.Primary;
        private float _thickness = 3.0f;
        private int _segments = 8;

        /// <summary>
        /// Gets or sets the spinner style.
        /// </summary>
        public SpinnerStyle Style
        {
            get => _style;
            set
            {
                if (_style != value)
                {
                    _style = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the spinner color.
        /// </summary>
        public SKColor Color
        {
            get => _color;
            set
            {
                if (_color != value)
                {
                    _color = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the thickness of the spinner lines.
        /// </summary>
        public float Thickness
        {
            get => _thickness;
            set
            {
                if (_thickness != value)
                {
                    _thickness = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the number of segments in the spinner.
        /// </summary>
        public int Segments
        {
            get => _segments;
            set
            {
                if (_segments != value)
                {
                    _segments = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the rotation speed in degrees per frame.
        /// </summary>
        public float Speed
        {
            get => _speed;
            set => _speed = value;
        }

        /// <summary>
        /// Initializes a new instance of the Spinner class.
        /// </summary>
        public Spinner()
        {
            Width = 40;
            Height = 40;
        }

        /// <summary>
        /// Draws the spinner content.
        /// </summary>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            float centerX = X + Width / 2;
            float centerY = Y + Height / 2;
            float radius = Math.Min(Width, Height) / 2 - _thickness;

            using (var paint = new SKPaint())
            {
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = _thickness;
                paint.StrokeCap = SKStrokeCap.Round;

                float angleStep = 360.0f / _segments;

                for (int i = 0; i < _segments; i++)
                {
                    float angle = _rotation + (i * angleStep);
                    float alpha = 1.0f - (i / (float)_segments);

                    paint.Color = _color.WithAlpha((byte)(alpha * 255));

                    float startAngle = angle * (float)Math.PI / 180.0f;
                    float endAngle = (angle + angleStep * 0.7f) * (float)Math.PI / 180.0f;

                    float x1 = centerX + radius * (float)Math.Cos(startAngle);
                    float y1 = centerY + radius * (float)Math.Sin(startAngle);
                    float x2 = centerX + radius * (float)Math.Cos(endAngle);
                    float y2 = centerY + radius * (float)Math.Sin(endAngle);

                    canvas.DrawLine(x1, y1, x2, y2, paint);
                }
            }

            // Update rotation for animation
            _rotation += _speed;
            if (_rotation >= 360) _rotation -= 360;

            // Trigger redraw for animation
            InvalidateVisual();
        }

        /// <summary>
        /// Starts the spinner animation.
        /// </summary>
        public void Start()
        {
            IsVisible = true;
            InvalidateVisual();
        }

        /// <summary>
        /// Stops the spinner animation.
        /// </summary>
        public void Stop()
        {
            IsVisible = false;
            InvalidateVisual();
        }
    }
}
