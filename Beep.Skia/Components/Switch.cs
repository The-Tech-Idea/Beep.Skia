using SkiaSharp;
using System;

namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 Switch component that provides a toggle between on and off states.
    /// </summary>
    public class Switch : MaterialControl
    {
        private bool _isChecked = false;
        private bool _isHovered = false;
        private bool _isPressed = false;
        private float _thumbPosition = 0; // 0 = off, 1 = on
        private float _animationProgress = 0; // For smooth transitions

        // Switch dimensions (Material Design 3.0 specifications)
        private const float TrackWidth = 52f;
        private const float TrackHeight = 32f;
        private const float ThumbDiameter = 24f;
        private const float ThumbMargin = 4f;

        /// <summary>
        /// Occurs when the switch state changes.
        /// </summary>
        public new event EventHandler<SwitchStateChangedEventArgs> StateChanged;

        /// <summary>
        /// Gets or sets whether the switch is in the checked (on) state.
        /// </summary>
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    StartAnimation();
                    OnStateChanged(new SwitchStateChangedEventArgs(_isChecked));
                    RefreshVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the switch is hovered.
        /// </summary>
        public bool IsHovered
        {
            get => _isHovered;
            set
            {
                if (_isHovered != value)
                {
                    _isHovered = value;
                    RefreshVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the switch is pressed.
        /// </summary>
        public bool IsPressed
        {
            get => _isPressed;
            set
            {
                if (_isPressed != value)
                {
                    _isPressed = value;
                    RefreshVisual();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the Switch class.
        /// </summary>
        public Switch()
        {
            Width = TrackWidth;
            Height = TrackHeight;
            _thumbPosition = _isChecked ? 1 : 0;
            _animationProgress = _thumbPosition;
        }

        /// <summary>
        /// Draws the switch component.
        /// </summary>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Update animation progress
            UpdateAnimation();

            // Calculate switch bounds
            float centerY = Height / 2;
            float trackLeft = 0;
            float trackTop = centerY - TrackHeight / 2;
            float trackRight = TrackWidth;
            float trackBottom = centerY + TrackHeight / 2;

            // Draw track
            DrawTrack(canvas, trackLeft, trackTop, trackRight, trackBottom);

            // Draw thumb
            DrawThumb(canvas, trackLeft, trackTop, trackRight, trackBottom);
        }

        private void DrawTrack(SKCanvas canvas, float left, float top, float right, float bottom)
        {
            using (var trackPaint = new SKPaint())
            {
                trackPaint.IsAntialias = true;
                trackPaint.Style = SKPaintStyle.Fill;

                // Calculate track color based on state
                SKColor trackColor = GetTrackColor();
                trackPaint.Color = trackColor;

                // Draw track background
                var trackRect = new SKRect(left, top, right, bottom);
                float trackCornerRadius = TrackHeight / 2;
                canvas.DrawRoundRect(trackRect, trackCornerRadius, trackCornerRadius, trackPaint);
            }
        }

        private void DrawThumb(SKCanvas canvas, float trackLeft, float trackTop, float trackRight, float trackBottom)
        {
            using (var thumbPaint = new SKPaint())
            {
                thumbPaint.IsAntialias = true;
                thumbPaint.Style = SKPaintStyle.Fill;

                // Calculate thumb position
                float thumbCenterX = trackLeft + ThumbMargin + ThumbDiameter / 2 +
                                   (_animationProgress * (TrackWidth - ThumbDiameter - ThumbMargin * 2));
                float thumbCenterY = (trackTop + trackBottom) / 2;

                // Calculate thumb color
                SKColor thumbColor = GetThumbColor();
                thumbPaint.Color = thumbColor;

                // Draw thumb shadow (subtle elevation effect)
                using (var shadowPaint = new SKPaint())
                {
                    shadowPaint.Color = MaterialColors.OnSurface.WithAlpha(30);
                    shadowPaint.Style = SKPaintStyle.Fill;
                    shadowPaint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 1);

                    canvas.DrawCircle(thumbCenterX + 0.5f, thumbCenterY + 0.5f, ThumbDiameter / 2, shadowPaint);
                }

                // Draw thumb
                canvas.DrawCircle(thumbCenterX, thumbCenterY, ThumbDiameter / 2, thumbPaint);

                // Draw state layer if needed
                float stateOpacity = GetStateLayerOpacity();
                if (stateOpacity > 0)
                {
                    using (var statePaint = new SKPaint())
                    {
                        statePaint.IsAntialias = true;
                        statePaint.Style = SKPaintStyle.Fill;
                        statePaint.Color = GetStateLayerColor().WithAlpha((byte)(stateOpacity * 255));

                        canvas.DrawCircle(thumbCenterX, thumbCenterY, ThumbDiameter / 2, statePaint);
                    }
                }
            }
        }

        private SKColor GetTrackColor()
        {
            if (_isChecked)
            {
                // Checked state - use primary color
                return MaterialColors.Primary;
            }
            else
            {
                // Unchecked state - use surface variant
                return MaterialColors.SurfaceVariant;
            }
        }

        private SKColor GetThumbColor()
        {
            if (_isChecked)
            {
                // Checked state - use on primary
                return MaterialColors.OnPrimary;
            }
            else
            {
                // Unchecked state - use outline
                return MaterialColors.Outline;
            }
        }

        private SKColor GetStateLayerColor()
        {
            if (_isChecked)
            {
                return MaterialColors.OnPrimary;
            }
            else
            {
                return MaterialColors.OnSurfaceVariant;
            }
        }

        private float GetStateLayerOpacity()
        {
            if (_isPressed)
            {
                return StateLayerOpacity.Press;
            }
            else if (_isHovered)
            {
                return StateLayerOpacity.Hover;
            }
            else
            {
                return 0;
            }
        }

        private void StartAnimation()
        {
            _animationProgress = _thumbPosition;
        }

        private void UpdateAnimation()
        {
            // Simple linear animation
            float targetPosition = _isChecked ? 1 : 0;
            float animationSpeed = 0.15f; // Adjust for desired animation speed

            if (Math.Abs(_animationProgress - targetPosition) > 0.01f)
            {
                if (_animationProgress < targetPosition)
                {
                    _animationProgress += animationSpeed;
                    if (_animationProgress > targetPosition)
                        _animationProgress = targetPosition;
                }
                else
                {
                    _animationProgress -= animationSpeed;
                    if (_animationProgress < targetPosition)
                        _animationProgress = targetPosition;
                }

                RefreshVisual();
            }
            else
            {
                _animationProgress = targetPosition;
            }

            _thumbPosition = _animationProgress;
        }

        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            base.OnMouseDown(point, context);

            if (new SKRect(X, Y, X + Width, Y + Height).Contains(point))
            {
                _isPressed = true;
                RefreshVisual();
                return true;
            }

            return false;
        }

        protected override bool OnMouseUp(SKPoint point, InteractionContext context)
        {
            base.OnMouseUp(point, context);

            if (_isPressed && new SKRect(X, Y, X + Width, Y + Height).Contains(point))
            {
                // Toggle the switch state
                IsChecked = !_isChecked;
            }

            _isPressed = false;
            RefreshVisual();
            return true;
        }

        protected override bool OnMouseMove(SKPoint point, InteractionContext context)
        {
            base.OnMouseMove(point, context);

            // Update hover state
            bool wasHovered = _isHovered;
            _isHovered = new SKRect(X, Y, X + Width, Y + Height).Contains(point);

            if (wasHovered != _isHovered)
            {
                RefreshVisual();
            }

            return false;
        }

        protected override void OnMouseLeave()
        {
            base.OnMouseLeave();
            _isHovered = false;
            RefreshVisual();
        }

        /// <summary>
        /// Raises the StateChanged event.
        /// </summary>
        protected virtual void OnStateChanged(SwitchStateChangedEventArgs e)
        {
            StateChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Toggles the switch state.
        /// </summary>
        public void Toggle()
        {
            IsChecked = !_isChecked;
        }

        /// <summary>
        /// Event arguments for switch state changes.
        /// </summary>
        public class SwitchStateChangedEventArgs : EventArgs
        {
            /// <summary>
            /// Gets whether the switch is checked.
            /// </summary>
            public bool IsChecked { get; }

            /// <summary>
            /// Initializes a new instance of the SwitchStateChangedEventArgs class.
            /// </summary>
            public SwitchStateChangedEventArgs(bool isChecked)
            {
                IsChecked = isChecked;
            }
        }
    }
}
