using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 Slider component that allows users to select a value from a range.
    /// Supports continuous, discrete, and range slider types.
    /// </summary>
    public class Slider : MaterialControl
    {
        private double _value = 0;
        private double _minimum = 0;
        private double _maximum = 100;
        private double _stepSize = 1;
        private double _secondaryValue = 0; // For range sliders
        private SliderType _sliderType = SliderType.Continuous;
        private bool _isDiscrete = false;
        private bool _isRange = false;
        private bool _showTicks = false;
        private bool _isHovered = false;
        private bool _isFocused = false;
        private bool _isDragging = false;
        private bool _isDraggingSecondary = false;
        private SKColor _trackColor;
        private SKColor _activeTrackColor;
        private SKColor _thumbColor;
        private SKColor _tickColor;
        private float _trackHeight = 4f;
        private float _thumbRadius = 10f;
        private float _tickRadius = 2f;
        private string _label;
        private string _valueLabelFormat = "{0}";
        private bool _showValueLabel = false;

        /// <summary>
        /// Gets or sets the current value of the slider.
        /// </summary>
        public double Value
        {
            get => _value;
            set
            {
                double clampedValue = Math.Max(Minimum, Math.Min(Maximum, value));
                if (_isDiscrete && StepSize > 0)
                {
                    clampedValue = Math.Round(clampedValue / StepSize) * StepSize;
                }

                if (_value != clampedValue)
                {
                    _value = clampedValue;
                    RefreshVisual();
                    ValueChanged?.Invoke(this, new SliderValueChangedEventArgs(_value, _secondaryValue));
                }
            }
        }

        /// <summary>
        /// Gets or sets the minimum value of the slider.
        /// </summary>
        public double Minimum
        {
            get => _minimum;
            set
            {
                if (_minimum != value)
                {
                    _minimum = value;
                    if (_value < _minimum) Value = _minimum;
                    if (_secondaryValue < _minimum) SecondaryValue = _minimum;
                    RefreshVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum value of the slider.
        /// </summary>
        public double Maximum
        {
            get => _maximum;
            set
            {
                if (_maximum != value)
                {
                    _maximum = value;
                    if (_value > _maximum) Value = _maximum;
                    if (_secondaryValue > _maximum) SecondaryValue = _maximum;
                    RefreshVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the step size for discrete sliders.
        /// </summary>
        public double StepSize
        {
            get => _stepSize;
            set
            {
                if (_stepSize != value && value > 0)
                {
                    _stepSize = value;
                    if (_isDiscrete)
                    {
                        Value = _value; // Re-clamp to step
                        SecondaryValue = _secondaryValue;
                    }
                    RefreshVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the secondary value for range sliders.
        /// </summary>
        public double SecondaryValue
        {
            get => _secondaryValue;
            set
            {
                if (_isRange)
                {
                    double clampedValue = Math.Max(Minimum, Math.Min(Maximum, value));
                    if (_isDiscrete && StepSize > 0)
                    {
                        clampedValue = Math.Round(clampedValue / StepSize) * StepSize;
                    }

                    if (_secondaryValue != clampedValue)
                    {
                        _secondaryValue = clampedValue;
                        RefreshVisual();
                        ValueChanged?.Invoke(this, new SliderValueChangedEventArgs(_value, _secondaryValue));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the type of the slider.
        /// </summary>
        public SliderType SliderType
        {
            get => _sliderType;
            set
            {
                if (_sliderType != value)
                {
                    _sliderType = value;
                    _isDiscrete = (_sliderType == SliderType.Discrete);
                    _isRange = (_sliderType == SliderType.Range);
                    RefreshVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to show tick marks for discrete sliders.
        /// </summary>
        public bool ShowTicks
        {
            get => _showTicks;
            set
            {
                if (_showTicks != value)
                {
                    _showTicks = value;
                    RefreshVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the slider is hovered.
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
        /// Gets or sets whether the slider is focused.
        /// </summary>
        public bool IsFocused
        {
            get => _isFocused;
            set
            {
                if (_isFocused != value)
                {
                    _isFocused = value;
                    RefreshVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the track color.
        /// </summary>
        public SKColor TrackColor
        {
            get => _trackColor;
            set
            {
                _trackColor = value;
                RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets the active track color.
        /// </summary>
        public SKColor ActiveTrackColor
        {
            get => _activeTrackColor;
            set
            {
                _activeTrackColor = value;
                RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets the thumb color.
        /// </summary>
        public SKColor ThumbColor
        {
            get => _thumbColor;
            set
            {
                _thumbColor = value;
                RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets the tick color.
        /// </summary>
        public SKColor TickColor
        {
            get => _tickColor;
            set
            {
                _tickColor = value;
                RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets the height of the track.
        /// </summary>
        public float TrackHeight
        {
            get => _trackHeight;
            set
            {
                _trackHeight = value;
                RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets the radius of the thumb.
        /// </summary>
        public float ThumbRadius
        {
            get => _thumbRadius;
            set
            {
                _thumbRadius = value;
                RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets the label text for the slider.
        /// </summary>
        public string Label
        {
            get => _label;
            set
            {
                _label = value;
                RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets the format string for the value label.
        /// </summary>
        public string ValueLabelFormat
        {
            get => _valueLabelFormat;
            set
            {
                _valueLabelFormat = value;
                RefreshVisual();
            }
        }

        /// <summary>
        /// Gets or sets whether to show the value label.
        /// </summary>
        public bool ShowValueLabel
        {
            get => _showValueLabel;
            set
            {
                _showValueLabel = value;
                RefreshVisual();
            }
        }

        /// <summary>
        /// Occurs when the slider value changes.
        /// </summary>
        public event EventHandler<SliderValueChangedEventArgs> ValueChanged;

        /// <summary>
        /// Initializes a new instance of the Slider class.
        /// </summary>
        public Slider()
        {
            InitializeColors();
            Width = 200;
            Height = 40;
        }

        /// <summary>
        /// Initializes a new instance of the Slider class with specified range.
        /// </summary>
        /// <param name="minimum">The minimum value.</param>
        /// <param name="maximum">The maximum value.</param>
        /// <param name="value">The initial value.</param>
        public Slider(double minimum, double maximum, double value = 0)
            : this()
        {
            _minimum = minimum;
            _maximum = maximum;
            _value = Math.Max(minimum, Math.Min(maximum, value));
        }

        private void InitializeColors()
        {
            _trackColor = MaterialDesignColors.OutlineVariant;
            _activeTrackColor = MaterialDesignColors.Primary;
            _thumbColor = MaterialDesignColors.Primary;
            _tickColor = MaterialDesignColors.OnSurfaceVariant;
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Calculate track bounds
            float trackY = Height / 2;
            float trackLeft = ThumbRadius;
            float trackRight = Width - ThumbRadius;
            float trackTop = trackY - TrackHeight / 2;
            float trackBottom = trackY + TrackHeight / 2;

            // Draw inactive track
            using (var trackPaint = new SKPaint())
            {
                trackPaint.Color = TrackColor;
                trackPaint.Style = SKPaintStyle.Fill;

                var inactiveTrackRect = new SKRect(trackLeft, trackTop, trackRight, trackBottom);
                canvas.DrawRect(inactiveTrackRect, trackPaint);
            }

            // Draw active track(s)
            using (var activeTrackPaint = new SKPaint())
            {
                activeTrackPaint.Color = ActiveTrackColor;
                activeTrackPaint.Style = SKPaintStyle.Fill;

                if (_isRange)
                {
                    // Draw range track (between two thumbs)
                    double minValue = Math.Min(_value, _secondaryValue);
                    double maxValue = Math.Max(_value, _secondaryValue);
                    float activeLeft = (float)ValueToPosition(minValue);
                    float activeRight = (float)ValueToPosition(maxValue);

                    var activeTrackRect = new SKRect(activeLeft, trackTop, activeRight, trackBottom);
                    canvas.DrawRect(activeTrackRect, activeTrackPaint);
                }
                else
                {
                    // Draw active track from start to current value
                    float activeRight = (float)ValueToPosition(_value);
                    var activeTrackRect = new SKRect(trackLeft, trackTop, activeRight, trackBottom);
                    canvas.DrawRect(activeTrackRect, activeTrackPaint);
                }
            }

            // Draw ticks for discrete sliders
            if (_isDiscrete && _showTicks && _stepSize > 0)
            {
                DrawTicks(canvas, trackLeft, trackRight, trackY);
            }

            // Draw thumbs
            DrawThumb(canvas, _value, _isDragging);

            if (_isRange)
            {
                DrawThumb(canvas, _secondaryValue, _isDraggingSecondary);
            }

            // Draw label if present
            if (!string.IsNullOrEmpty(_label))
            {
                DrawLabel(canvas);
            }

            // Draw value label if enabled
            if (_showValueLabel)
            {
                DrawValueLabel(canvas);
            }
        }

        private void DrawTicks(SKCanvas canvas, float trackLeft, float trackRight, float trackY)
        {
            using (var tickPaint = new SKPaint())
            {
                tickPaint.Color = TickColor;
                tickPaint.Style = SKPaintStyle.Fill;

                double range = _maximum - _minimum;
                int stepCount = (int)(range / _stepSize);

                for (int i = 0; i <= stepCount; i++)
                {
                    double tickValue = _minimum + (i * _stepSize);
                    float tickX = ValueToPosition(tickValue);

                    var tickRect = new SKRect(
                        tickX - _tickRadius,
                        trackY - _tickRadius,
                        tickX + _tickRadius,
                        trackY + _tickRadius
                    );

                    canvas.DrawRect(tickRect, tickPaint);
                }
            }
        }

        private void DrawThumb(SKCanvas canvas, double value, bool isDragged)
        {
            float thumbX = ValueToPosition(value);
            float thumbY = Height / 2;

            // Draw thumb shadow (subtle elevation effect)
            using (var shadowPaint = new SKPaint())
            {
                shadowPaint.Color = MaterialDesignColors.OnSurface.WithAlpha(20);
                shadowPaint.Style = SKPaintStyle.Fill;
                shadowPaint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 2);

                canvas.DrawCircle(thumbX + 1, thumbY + 1, ThumbRadius, shadowPaint);
            }

            // Draw thumb
            using (var thumbPaint = new SKPaint())
            {
                thumbPaint.Color = ThumbColor;
                thumbPaint.Style = SKPaintStyle.Fill;
                thumbPaint.IsAntialias = true;

                // Apply state layer opacity
                float stateOpacity = 0;
                if (isDragged)
                {
                    stateOpacity = StateLayerOpacity.Press;
                }
                else if (IsHovered)
                {
                    stateOpacity = StateLayerOpacity.Hover;
                }
                else if (IsFocused)
                {
                    stateOpacity = StateLayerOpacity.Focus;
                }

                if (stateOpacity > 0)
                {
                    thumbPaint.Color = thumbPaint.Color.WithAlpha((byte)(stateOpacity * 255));
                }

                canvas.DrawCircle(thumbX, thumbY, ThumbRadius, thumbPaint);
            }

            // Draw thumb border
            using (var borderPaint = new SKPaint())
            {
                borderPaint.Color = MaterialDesignColors.Outline;
                borderPaint.Style = SKPaintStyle.Stroke;
                borderPaint.StrokeWidth = 1;
                borderPaint.IsAntialias = true;

                canvas.DrawCircle(thumbX, thumbY, ThumbRadius, borderPaint);
            }
        }

        private void DrawLabel(SKCanvas canvas)
        {
            using (var labelPaint = new SKPaint())
            {
                labelPaint.Color = MaterialDesignColors.OnSurface;
                labelPaint.TextSize = 14f;
                labelPaint.IsAntialias = true;

                float labelX = 0;
                float labelY = Height / 2 - TrackHeight / 2 - 8;

                canvas.DrawText(_label, labelX, labelY, labelPaint);
            }
        }

        private void DrawValueLabel(SKCanvas canvas)
        {
            string valueText = string.Format(_valueLabelFormat, _value);
            if (_isRange)
            {
                valueText += $" - {string.Format(_valueLabelFormat, _secondaryValue)}";
            }

            using (var valuePaint = new SKPaint())
            {
                valuePaint.Color = MaterialDesignColors.OnSurfaceVariant;
                valuePaint.TextSize = 12f;
                valuePaint.IsAntialias = true;

                float valueX = Width - valuePaint.MeasureText(valueText);
                float valueY = Height / 2 - TrackHeight / 2 - 8;

                canvas.DrawText(valueText, valueX, valueY, valuePaint);
            }
        }

        private float ValueToPosition(double value)
        {
            double range = _maximum - _minimum;
            if (range == 0) return ThumbRadius;

            double normalizedValue = (value - _minimum) / range;
            float trackWidth = Width - 2 * ThumbRadius;
            return ThumbRadius + (float)(normalizedValue * trackWidth);
        }

        private double PositionToValue(float position)
        {
            float trackWidth = Width - 2 * ThumbRadius;
            if (trackWidth == 0) return _minimum;

            float normalizedPosition = (position - ThumbRadius) / trackWidth;
            double value = _minimum + (normalizedPosition * (_maximum - _minimum));

            if (_isDiscrete && _stepSize > 0)
            {
                value = Math.Round(value / _stepSize) * _stepSize;
            }

            return Math.Max(_minimum, Math.Min(_maximum, value));
        }

        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            base.OnMouseDown(point, context);

            // Check if clicking on a thumb
            float primaryThumbX = ValueToPosition(_value);
            float primaryThumbY = Height / 2;

            if (IsPointOnThumb(point, primaryThumbX, primaryThumbY))
            {
                _isDragging = true;
                return true;
            }

            if (_isRange)
            {
                float secondaryThumbX = ValueToPosition(_secondaryValue);
                float secondaryThumbY = Height / 2;

                if (IsPointOnThumb(point, secondaryThumbX, secondaryThumbY))
                {
                    _isDraggingSecondary = true;
                    return true;
                }
            }

            // If not clicking on thumb, set value to click position
            if (point.X >= ThumbRadius && point.X <= Width - ThumbRadius)
            {
                double newValue = PositionToValue(point.X);

                if (_isRange)
                {
                    // For range sliders, determine which thumb is closer
                    double distanceToPrimary = Math.Abs(newValue - _value);
                    double distanceToSecondary = Math.Abs(newValue - _secondaryValue);

                    if (distanceToPrimary <= distanceToSecondary)
                    {
                        Value = newValue;
                        _isDragging = true;
                    }
                    else
                    {
                        SecondaryValue = newValue;
                        _isDraggingSecondary = true;
                    }
                }
                else
                {
                    Value = newValue;
                    _isDragging = true;
                }

                return true;
            }

            return false;
        }

        protected override bool OnMouseMove(SKPoint point, InteractionContext context)
        {
            base.OnMouseMove(point, context);

            // Update hover state
            bool wasHovered = _isHovered;
            _isHovered = new SKRect(0, 0, Width, Height).Contains(point);

            if (wasHovered != _isHovered)
            {
                RefreshVisual();
            }

            if (_isDragging)
            {
                if (point.X >= ThumbRadius && point.X <= Width - ThumbRadius)
                {
                    Value = PositionToValue(point.X);
                }
                return true;
            }

            if (_isDraggingSecondary)
            {
                if (point.X >= ThumbRadius && point.X <= Width - ThumbRadius)
                {
                    SecondaryValue = PositionToValue(point.X);
                }
                return true;
            }

            return false;
        }

        protected override bool OnMouseUp(SKPoint point, InteractionContext context)
        {
            base.OnMouseUp(point, context);

            _isDragging = false;
            _isDraggingSecondary = false;
            return true;
        }

        protected override void OnMouseLeave()
        {
            base.OnMouseLeave();
            IsHovered = false;
        }

        private bool IsPointOnThumb(SKPoint point, float thumbX, float thumbY)
        {
            float distance = (float)Math.Sqrt(
                Math.Pow(point.X - thumbX, 2) +
                Math.Pow(point.Y - thumbY, 2)
            );
            return distance <= ThumbRadius + 5; // Add some tolerance
        }

        /// <summary>
        /// Slider value changed event arguments.
        /// </summary>
        public class SliderValueChangedEventArgs : EventArgs
        {
            /// <summary>
            /// Gets the primary value.
            /// </summary>
            public double PrimaryValue { get; }

            /// <summary>
            /// Gets the secondary value (for range sliders).
            /// </summary>
            public double SecondaryValue { get; }

            /// <summary>
            /// Initializes a new instance of the SliderValueChangedEventArgs class.
            /// </summary>
            /// <param name="primaryValue">The primary value.</param>
            /// <param name="secondaryValue">The secondary value.</param>
            public SliderValueChangedEventArgs(double primaryValue, double secondaryValue)
            {
                PrimaryValue = primaryValue;
                SecondaryValue = secondaryValue;
            }
        }
    }

    /// <summary>
    /// Specifies the type of slider.
    /// </summary>
    public enum SliderType
    {
        /// <summary>
        /// Continuous slider with smooth value changes.
        /// </summary>
        Continuous,

        /// <summary>
        /// Discrete slider with stepped values.
        /// </summary>
        Discrete,

        /// <summary>
        /// Range slider with two thumbs for selecting a range.
        /// </summary>
        Range
    }
}
