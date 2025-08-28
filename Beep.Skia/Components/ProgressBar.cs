using SkiaSharp;

namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 progress bar component.
    /// </summary>
    public class ProgressBar : MaterialControl
    {
        private float _progress = 0.0f; // 0.0 to 1.0
        private bool _isIndeterminate = false;
        private ProgressBarVariant _variant = ProgressBarVariant.Linear;
        private float _strokeWidth = 4.0f;
        private SKColor? _progressColor;
        private SKColor? _trackColor;
        private float _animationOffset = 0.0f;

        /// <summary>
        /// Material Design 3.0 progress bar variants.
        /// </summary>
        public enum ProgressBarVariant
        {
            Linear,
            Circular
        }

        /// <summary>
        /// Gets or sets the progress value (0.0 to 1.0).
        /// </summary>
        public float Progress
        {
            get => _progress;
            set
            {
                _progress = Math.Clamp(value, 0.0f, 1.0f);
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets whether the progress bar is in indeterminate state.
        /// </summary>
        public bool IsIndeterminate
        {
            get => _isIndeterminate;
            set
            {
                if (_isIndeterminate != value)
                {
                    _isIndeterminate = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the progress bar variant.
        /// </summary>
        public ProgressBarVariant Variant
        {
            get => _variant;
            set
            {
                if (_variant != value)
                {
                    _variant = value;
                    UpdateBounds();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the stroke width for the progress bar.
        /// </summary>
        public float StrokeWidth
        {
            get => _strokeWidth;
            set
            {
                if (_strokeWidth != value)
                {
                    _strokeWidth = value;
                    UpdateBounds();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the progress color.
        /// </summary>
        public SKColor ProgressColor
        {
            get => _progressColor ?? MaterialControl.MaterialColors.Primary;
            set
            {
                _progressColor = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the track color.
        /// </summary>
        public SKColor TrackColor
        {
            get => _trackColor ?? MaterialControl.MaterialColors.SurfaceVariant;
            set
            {
                _trackColor = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Initializes a new instance of the ProgressBar class.
        /// </summary>
        public ProgressBar()
        {
            Width = 200;
            Height = _variant == ProgressBarVariant.Linear ? StrokeWidth : 48;
            Name = "ProgressBar";
            Elevation = 0;
        }

        /// <summary>
        /// Initializes a new instance of the ProgressBar class with specified dimensions.
        /// </summary>
        public ProgressBar(float width, float height) : this()
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Sets the progress as a percentage (0-100).
        /// </summary>
        public void SetProgressPercent(float percent)
        {
            Progress = percent / 100.0f;
        }

        /// <summary>
        /// Updates the bounds based on the current variant and stroke width.
        /// </summary>
        protected override void UpdateBounds()
        {
            if (_variant == ProgressBarVariant.Linear)
            {
                // For linear progress bar, height should be at least the stroke width
                Height = Math.Max(Height, StrokeWidth);
            }
            else // Circular
            {
                // For circular progress bar, ensure it's square and large enough
                float minSize = StrokeWidth * 2 + 16; // Minimum size for circular progress
                Width = Math.Max(Width, minSize);
                Height = Math.Max(Height, minSize);
            }

            base.UpdateBounds();
        }

        /// <summary>
        /// Draws the progress bar's content.
        /// </summary>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            if (_variant == ProgressBarVariant.Linear)
            {
                DrawLinearProgress(canvas);
            }
            else
            {
                DrawCircularProgress(canvas);
            }
        }

        /// <summary>
        /// Draws a linear progress bar.
        /// </summary>
        private void DrawLinearProgress(SKCanvas canvas)
        {
            var trackRect = new SKRect(0, Height / 2 - StrokeWidth / 2, Width, Height / 2 + StrokeWidth / 2);

            // Draw track
            using (var trackPaint = new SKPaint
            {
                Color = TrackColor,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            })
            {
                canvas.DrawRoundRect(trackRect, StrokeWidth / 2, StrokeWidth / 2, trackPaint);
            }

            // Draw progress
            if (_isIndeterminate)
            {
                DrawIndeterminateLinearProgress(canvas, trackRect);
            }
            else
            {
                DrawDeterminateLinearProgress(canvas, trackRect);
            }
        }

        /// <summary>
        /// Draws determinate linear progress.
        /// </summary>
        private void DrawDeterminateLinearProgress(SKCanvas canvas, SKRect trackRect)
        {
            if (_progress > 0)
            {
                var progressRect = new SKRect(
                    trackRect.Left,
                    trackRect.Top,
                    trackRect.Left + (trackRect.Width * _progress),
                    trackRect.Bottom
                );

                using (var progressPaint = new SKPaint
                {
                    Color = ProgressColor,
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                })
                {
                    canvas.DrawRoundRect(progressRect, StrokeWidth / 2, StrokeWidth / 2, progressPaint);
                }
            }
        }

        /// <summary>
        /// Draws indeterminate linear progress with animation.
        /// </summary>
        private void DrawIndeterminateLinearProgress(SKCanvas canvas, SKRect trackRect)
        {
            // Animate the offset
            _animationOffset += 0.02f;
            if (_animationOffset > 1.0f) _animationOffset = 0.0f;

            // Draw animated segments
            float segmentWidth = trackRect.Width * 0.3f; // 30% of total width
            float startX = trackRect.Left + (_animationOffset * trackRect.Width);

            // Create progress paint outside the conditional blocks
            using (var progressPaint = new SKPaint
            {
                Color = ProgressColor,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            })
            {
                // Wrap around if needed
                if (startX + segmentWidth > trackRect.Right)
                {
                    // Draw first part
                    var firstRect = new SKRect(startX, trackRect.Top, trackRect.Right, trackRect.Bottom);
                    canvas.DrawRoundRect(firstRect, StrokeWidth / 2, StrokeWidth / 2, progressPaint);

                    // Draw second part from the beginning
                    float remainingWidth = (startX + segmentWidth) - trackRect.Right;
                    var secondRect = new SKRect(trackRect.Left, trackRect.Top,
                                              trackRect.Left + remainingWidth, trackRect.Bottom);
                    canvas.DrawRoundRect(secondRect, StrokeWidth / 2, StrokeWidth / 2, progressPaint);
                }
                else
                {
                    var progressRect = new SKRect(startX, trackRect.Top,
                                                startX + segmentWidth, trackRect.Bottom);
                    canvas.DrawRoundRect(progressRect, StrokeWidth / 2, StrokeWidth / 2, progressPaint);
                }
            }
        }

        /// <summary>
        /// Draws a circular progress bar.
        /// </summary>
        private void DrawCircularProgress(SKCanvas canvas)
        {
            float centerX = Width / 2;
            float centerY = Height / 2;
            float radius = Math.Min(Width, Height) / 2 - StrokeWidth / 2;

            // Draw track
            using (var trackPaint = new SKPaint
            {
                Color = TrackColor,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = StrokeWidth,
                IsAntialias = true
            })
            {
                canvas.DrawCircle(centerX, centerY, radius, trackPaint);
            }

            // Draw progress
            if (_isIndeterminate)
            {
                DrawIndeterminateCircularProgress(canvas, centerX, centerY, radius);
            }
            else
            {
                DrawDeterminateCircularProgress(canvas, centerX, centerY, radius);
            }
        }

        /// <summary>
        /// Draws determinate circular progress.
        /// </summary>
        private void DrawDeterminateCircularProgress(SKCanvas canvas, float centerX, float centerY, float radius)
        {
            if (_progress > 0)
            {
                using (var progressPaint = new SKPaint
                {
                    Color = ProgressColor,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = StrokeWidth,
                    IsAntialias = true
                })
                {
                    // Draw arc from 12 o'clock position
                    float sweepAngle = _progress * 360.0f;
                    using (var path = new SKPath())
                    {
                        path.AddArc(new SKRect(centerX - radius, centerY - radius, centerX + radius, centerY + radius),
                                  -90, sweepAngle);
                        canvas.DrawPath(path, progressPaint);
                    }
                }
            }
        }

        /// <summary>
        /// Draws indeterminate circular progress with animation.
        /// </summary>
        private void DrawIndeterminateCircularProgress(SKCanvas canvas, float centerX, float centerY, float radius)
        {
            // Animate the offset
            _animationOffset += 0.05f;
            if (_animationOffset > 1.0f) _animationOffset = 0.0f;

            using (var progressPaint = new SKPaint
            {
                Color = ProgressColor,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = StrokeWidth,
                IsAntialias = true
            })
            {
                // Draw animated arc segment
                float segmentAngle = 90.0f; // 90-degree arc
                float startAngle = -90 + (_animationOffset * 360.0f);

                using (var path = new SKPath())
                {
                    path.AddArc(new SKRect(centerX - radius, centerY - radius, centerX + radius, centerY + radius),
                              startAngle, segmentAngle);
                    canvas.DrawPath(path, progressPaint);
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
    }
}
