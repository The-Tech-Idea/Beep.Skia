using System;
using SkiaSharp;

namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 color picker component
    /// </summary>
    public class ColorPicker : MaterialControl
    {
        private SKColor _selectedColor = SKColors.Black;
        private float _hue = 0;
        private float _saturation = 1;
        private float _value = 1;
        private float _alpha = 1;
        private bool _showAlpha = true;
        private float _colorWheelRadius = 80;
        private float _brightnessBarWidth = 20;
        private SKPoint _lastMousePoint;

        /// <summary>
        /// Gets or sets the selected color
        /// </summary>
        public SKColor SelectedColor
        {
            get => _selectedColor;
            set
            {
                if (_selectedColor != value)
                {
                    _selectedColor = value;
                    UpdateHSVFromColor();
                    InvalidateVisual();
                    OnColorChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the hue component (0-360)
        /// </summary>
        public float Hue
        {
            get => _hue;
            set
            {
                if (_hue != value)
                {
                    _hue = Math.Clamp(value, 0, 360);
                    UpdateColorFromHSV();
                    InvalidateVisual();
                    OnColorChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the saturation component (0-1)
        /// </summary>
        public float Saturation
        {
            get => _saturation;
            set
            {
                if (_saturation != value)
                {
                    _saturation = Math.Clamp(value, 0, 1);
                    UpdateColorFromHSV();
                    InvalidateVisual();
                    OnColorChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the value/brightness component (0-1)
        /// </summary>
        public float Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = Math.Clamp(value, 0, 1);
                    UpdateColorFromHSV();
                    InvalidateVisual();
                    OnColorChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the alpha component (0-1)
        /// </summary>
        public float Alpha
        {
            get => _alpha;
            set
            {
                if (_alpha != value)
                {
                    _alpha = Math.Clamp(value, 0, 1);
                    UpdateColorFromHSV();
                    InvalidateVisual();
                    OnColorChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to show the alpha slider
        /// </summary>
        public bool ShowAlpha
        {
            get => _showAlpha;
            set
            {
                if (_showAlpha != value)
                {
                    _showAlpha = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the color wheel radius
        /// </summary>
        public float ColorWheelRadius
        {
            get => _colorWheelRadius;
            set
            {
                if (_colorWheelRadius != value)
                {
                    _colorWheelRadius = Math.Max(20, value);
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the brightness bar width
        /// </summary>
        public float BrightnessBarWidth
        {
            get => _brightnessBarWidth;
            set
            {
                if (_brightnessBarWidth != value)
                {
                    _brightnessBarWidth = Math.Max(10, value);
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Occurs when the selected color changes
        /// </summary>
        public event EventHandler ColorChanged;

        /// <summary>
        /// Initializes a new instance of the ColorPicker class
        /// </summary>
        public ColorPicker()
        {
            Width = 200;
            Height = 200;
            UpdateHSVFromColor();
        }

        /// <summary>
        /// Draws the color picker content
        /// </summary>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds))
                return;

            var centerX = X + Width / 2;
            var centerY = Y + Height / 2;

            // Draw color wheel
            DrawColorWheel(canvas, centerX, centerY);

            // Draw brightness/value bar
            DrawBrightnessBar(canvas, centerX, centerY);

            // Draw alpha bar if enabled
            if (_showAlpha)
            {
                DrawAlphaBar(canvas, centerX, centerY);
            }

            // Draw current color preview
            DrawColorPreview(canvas, centerX, centerY);
        }

        private void DrawColorWheel(SKCanvas canvas, float centerX, float centerY)
        {
            var radius = _colorWheelRadius;
            var paint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            // Draw hue/saturation wheel
            for (int y = 0; y < radius * 2; y++)
            {
                for (int x = 0; x < radius * 2; x++)
                {
                    var dx = x - radius;
                    var dy = y - radius;
                    var distance = Math.Sqrt(dx * dx + dy * dy);

                    if (distance <= radius)
                    {
                        var angle = Math.Atan2(dy, dx) * 180 / Math.PI;
                        if (angle < 0) angle += 360;

                        var saturation = distance / radius;

                        paint.Color = HSVToColor((float)angle, (float)saturation, 1.0f);

                        canvas.DrawPoint(centerX - radius + x, centerY - radius + y, paint);
                    }
                }
            }

            // Draw selection indicator on color wheel
            var selectionAngle = _hue * Math.PI / 180;
            var selectionDistance = _saturation * radius;
            var selectionX = centerX + (float)(Math.Cos(selectionAngle) * selectionDistance);
            var selectionY = centerY + (float)(Math.Sin(selectionAngle) * selectionDistance);

            paint.Color = SKColors.White;
            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeWidth = 2;
            canvas.DrawCircle(selectionX, selectionY, 5, paint);

            paint.Dispose();
        }

        private void DrawBrightnessBar(SKCanvas canvas, float centerX, float centerY)
        {
            var barHeight = _colorWheelRadius * 2;
            var barLeft = centerX + _colorWheelRadius + 10;
            var barTop = centerY - barHeight / 2;

            // Draw brightness gradient
            var shader = SKShader.CreateLinearGradient(
                new SKPoint(barLeft, barTop),
                new SKPoint(barLeft, barTop + barHeight),
                new[] { HSVToColor(_hue, _saturation, 0), HSVToColor(_hue, _saturation, 1) },
                null,
                SKShaderTileMode.Clamp);

            using (var paint = new SKPaint { Shader = shader })
            {
                canvas.DrawRect(barLeft, barTop, _brightnessBarWidth, barHeight, paint);
            }

            // Draw brightness bar border
            using (var borderPaint = new SKPaint
            {
                Color = MaterialDesignColors.Outline,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                IsAntialias = true
            })
            {
                canvas.DrawRect(barLeft, barTop, _brightnessBarWidth, barHeight, borderPaint);
            }

            // Draw brightness selection indicator
            var brightnessY = barTop + (1 - _value) * barHeight;
            using (var indicatorPaint = new SKPaint
            {
                Color = SKColors.White,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2,
                IsAntialias = true
            })
            {
                canvas.DrawLine(barLeft - 5, brightnessY, barLeft + _brightnessBarWidth + 5, brightnessY, indicatorPaint);
            }
        }

        private void DrawAlphaBar(SKCanvas canvas, float centerX, float centerY)
        {
            var barHeight = _colorWheelRadius * 2;
            var barLeft = centerX + _colorWheelRadius + _brightnessBarWidth + 20;
            var barTop = centerY - barHeight / 2;

            // Draw alpha gradient
            var baseColor = HSVToColor(_hue, _saturation, _value);
            var transparentColor = new SKColor(baseColor.Red, baseColor.Green, baseColor.Blue, 0);

            var shader = SKShader.CreateLinearGradient(
                new SKPoint(barLeft, barTop),
                new SKPoint(barLeft, barTop + barHeight),
                new[] { transparentColor, baseColor },
                null,
                SKShaderTileMode.Clamp);

            using (var paint = new SKPaint { Shader = shader })
            {
                canvas.DrawRect(barLeft, barTop, _brightnessBarWidth, barHeight, paint);
            }

            // Draw alpha bar border
            using (var borderPaint = new SKPaint
            {
                Color = MaterialDesignColors.Outline,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                IsAntialias = true
            })
            {
                canvas.DrawRect(barLeft, barTop, _brightnessBarWidth, barHeight, borderPaint);
            }

            // Draw alpha selection indicator
            var alphaY = barTop + (1 - _alpha) * barHeight;
            using (var indicatorPaint = new SKPaint
            {
                Color = SKColors.White,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2,
                IsAntialias = true
            })
            {
                canvas.DrawLine(barLeft - 5, alphaY, barLeft + _brightnessBarWidth + 5, alphaY, indicatorPaint);
            }
        }

        private void DrawColorPreview(SKCanvas canvas, float centerX, float centerY)
        {
            var previewSize = 30;
            var previewX = centerX - previewSize / 2;
            var previewY = centerY + _colorWheelRadius + 20;

            // Draw current color
            using (var previewPaint = new SKPaint { Color = _selectedColor })
            {
                canvas.DrawRect(previewX, previewY, previewSize, previewSize, previewPaint);
            }

            // Draw border
            using (var borderPaint = new SKPaint
            {
                Color = MaterialDesignColors.Outline,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                IsAntialias = true
            })
            {
                canvas.DrawRect(previewX, previewY, previewSize, previewSize, borderPaint);
            }
        }

        /// <summary>
        /// Handles mouse down events
        /// </summary>
        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            base.OnMouseDown(point, context);
            _lastMousePoint = point;
            HandleMouseInteraction(point);
            return true;
        }

        /// <summary>
        /// Handles mouse move events
        /// </summary>
        protected override bool OnMouseMove(SKPoint point, InteractionContext context)
        {
            base.OnMouseMove(point, context);

            if (context.MouseButton == (int)MouseButton.Left)
            {
                _lastMousePoint = point;
                HandleMouseInteraction(point);
            }
            return true;
        }

        private void HandleMouseInteraction(SKPoint point)
        {
            var centerX = X + Width / 2;
            var centerY = Y + Height / 2;

            // Check if clicking on color wheel
            var dx = point.X - centerX;
            var dy = point.Y - centerY;
            var distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance <= _colorWheelRadius)
            {
                // Update hue and saturation
                var angle = Math.Atan2(dy, dx) * 180 / Math.PI;
                if (angle < 0) angle += 360;

                Hue = (float)angle;
                Saturation = (float)(distance / _colorWheelRadius);
            }
            else
            {
                // Check if clicking on brightness bar
                var barLeft = centerX + _colorWheelRadius + 10;
                var barTop = centerY - _colorWheelRadius;
                var barBottom = centerY + _colorWheelRadius;

                if (point.X >= barLeft && point.X <= barLeft + _brightnessBarWidth &&
                    point.Y >= barTop && point.Y <= barBottom)
                {
                    var relativeY = (point.Y - barTop) / (_colorWheelRadius * 2);
                    Value = 1 - (float)relativeY;
                }
                else if (_showAlpha)
                {
                    // Check if clicking on alpha bar
                    var alphaBarLeft = centerX + _colorWheelRadius + _brightnessBarWidth + 20;

                    if (point.X >= alphaBarLeft && point.X <= alphaBarLeft + _brightnessBarWidth &&
                        point.Y >= barTop && point.Y <= barBottom)
                    {
                        var relativeY = (point.Y - barTop) / (_colorWheelRadius * 2);
                        Alpha = 1 - (float)relativeY;
                    }
                }
            }
        }

        private void UpdateHSVFromColor()
        {
            // Convert RGB to HSV
            var r = _selectedColor.Red / 255.0f;
            var g = _selectedColor.Green / 255.0f;
            var b = _selectedColor.Blue / 255.0f;

            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));
            var delta = max - min;

            // Value/Brightness
            _value = max;

            // Saturation
            _saturation = max == 0 ? 0 : delta / max;

            // Hue
            if (delta == 0)
            {
                _hue = 0;
            }
            else if (max == r)
            {
                _hue = 60 * (((g - b) / delta) % 6);
            }
            else if (max == g)
            {
                _hue = 60 * (((b - r) / delta) + 2);
            }
            else
            {
                _hue = 60 * (((r - g) / delta) + 4);
            }

            if (_hue < 0) _hue += 360;

            // Alpha
            _alpha = _selectedColor.Alpha / 255.0f;
        }

        private void UpdateColorFromHSV()
        {
            var c = _value * _saturation;
            var x = c * (1 - Math.Abs((_hue / 60) % 2 - 1));
            var m = _value - c;

            float r, g, b;

            if (_hue < 60)
            {
                r = c; g = x; b = 0;
            }
            else if (_hue < 120)
            {
                r = x; g = c; b = 0;
            }
            else if (_hue < 180)
            {
                r = 0; g = c; b = x;
            }
            else if (_hue < 240)
            {
                r = 0; g = x; b = c;
            }
            else if (_hue < 300)
            {
                r = x; g = 0; b = c;
            }
            else
            {
                r = c; g = 0; b = x;
            }

            _selectedColor = new SKColor(
                (byte)((r + m) * 255),
                (byte)((g + m) * 255),
                (byte)((b + m) * 255),
                (byte)(_alpha * 255));
        }

        private SKColor HSVToColor(float hue, float saturation, float value)
        {
            var c = value * saturation;
            var x = c * (1 - Math.Abs((hue / 60) % 2 - 1));
            var m = value - c;

            float r, g, b;

            if (hue < 60)
            {
                r = c; g = x; b = 0;
            }
            else if (hue < 120)
            {
                r = x; g = c; b = 0;
            }
            else if (hue < 180)
            {
                r = 0; g = c; b = x;
            }
            else if (hue < 240)
            {
                r = 0; g = x; b = c;
            }
            else if (hue < 300)
            {
                r = x; g = 0; b = c;
            }
            else
            {
                r = c; g = 0; b = x;
            }

            return new SKColor(
                (byte)((r + m) * 255),
                (byte)((g + m) * 255),
                (byte)((b + m) * 255));
        }

        /// <summary>
        /// Raises the ColorChanged event
        /// </summary>
        protected virtual void OnColorChanged()
        {
            ColorChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
