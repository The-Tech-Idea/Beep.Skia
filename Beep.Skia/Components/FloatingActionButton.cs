using SkiaSharp;
using System;
using Beep.Skia.Model;
namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 Floating Action Button (FAB) component.
    /// </summary>
    public class FloatingActionButton : MaterialControl
    {
        private string _icon = "➕"; // Default plus icon
        private SKColor _backgroundColor = MaterialColors.PrimaryContainer;
        private SKColor _iconColor = MaterialColors.OnPrimaryContainer;
        private float _size = 56; // Material Design 3.0 medium FAB size
        private FabSize _fabSize = FabSize.Medium;
        private FabType _fabType = FabType.Primary;
        private string _extendedText = "";
        private bool _isExtended = false;
        private float _extendedPadding = 16;
        private float _iconSize = 24;

        /// <summary>
        /// Material Design 3.0 FAB sizes.
        /// </summary>
        public enum FabSize
        {
            Small,
            Medium,
            Large
        }

        /// <summary>
        /// Material Design 3.0 FAB types.
        /// </summary>
        public enum FabType
        {
            Primary,
            Secondary,
            Tertiary,
            Surface
        }

        /// <summary>
        /// Gets or sets the icon displayed on the FAB (SVG path or Unicode character).
        /// </summary>
        public string Icon
        {
            get => _icon;
            set
            {
                if (_icon != value)
                {
                    _icon = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the background color of the FAB.
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
        /// Gets or sets the icon color of the FAB.
        /// </summary>
        public SKColor IconColor
        {
            get => _iconColor;
            set
            {
                if (_iconColor != value)
                {
                    _iconColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the size of the FAB.
        /// </summary>
        public FabSize Size
        {
            get => _fabSize;
            set
            {
                if (_fabSize != value)
                {
                    _fabSize = value;
                    UpdateSize();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the type of the FAB.
        /// </summary>
        public FabType Type
        {
            get => _fabType;
            set
            {
                if (_fabType != value)
                {
                    _fabType = value;
                    UpdateColors();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the extended text for extended FAB.
        /// </summary>
        public string ExtendedText
        {
            get => _extendedText;
            set
            {
                if (_extendedText != value)
                {
                    _extendedText = value;
                    _isExtended = !string.IsNullOrEmpty(value);
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the FloatingActionButton class.
        /// </summary>
        public FloatingActionButton()
        {
            UpdateSize();
            UpdateColors();
        }

        /// <summary>
        /// Initializes a new instance of the FloatingActionButton class with an icon.
        /// </summary>
        /// <param name="icon">The icon to display.</param>
        public FloatingActionButton(string icon)
        {
            _icon = icon;
            UpdateSize();
            UpdateColors();
        }

        private void UpdateSize()
        {
            switch (_fabSize)
            {
                case FabSize.Small:
                    _size = 40;
                    _iconSize = 20;
                    break;
                case FabSize.Medium:
                    _size = 56;
                    _iconSize = 24;
                    break;
                case FabSize.Large:
                    _size = 96;
                    _iconSize = 36;
                    break;
            }

            Width = _isExtended ? _size + 200 : _size;
            Height = _size;
        }

        private void UpdateColors()
        {
            switch (_fabType)
            {
                case FabType.Primary:
                    _backgroundColor = MaterialColors.PrimaryContainer;
                    _iconColor = MaterialColors.OnPrimaryContainer;
                    break;
                case FabType.Secondary:
                    _backgroundColor = MaterialColors.SecondaryContainer;
                    _iconColor = MaterialColors.OnSecondaryContainer;
                    break;
                case FabType.Tertiary:
                    _backgroundColor = MaterialColors.TertiaryContainer;
                    _iconColor = MaterialColors.OnTertiaryContainer;
                    break;
                case FabType.Surface:
                    _backgroundColor = MaterialColors.SurfaceContainerHigh;
                    _iconColor = MaterialColors.Primary;
                    break;
            }
        }

        /// <summary>
        /// Draws the FAB on the specified canvas.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="drawingContext">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext drawingContext)
        {
            float radius = _size / 2;
            // Absolute centers
            float centerX = X + (_isExtended ? radius + _extendedPadding : radius);
            float centerY = Y + radius;

            // Draw shadow/elevation
            using (var shadowPaint = new SKPaint
            {
                Color = new SKColor(0, 0, 0, 50),
                Style = SKPaintStyle.Fill
            })
            {
                canvas.DrawCircle(centerX + 2, centerY + 2, radius, shadowPaint);
            }

            // Draw FAB background
            using (var backgroundPaint = new SKPaint
            {
                Color = _backgroundColor,
                Style = SKPaintStyle.Fill
            })
            {
                if (_isExtended)
                {
                    // Draw extended FAB (rounded rectangle) in absolute space
                    var rect = new SKRect(X + _extendedPadding, Y, X + Width - _extendedPadding, Y + Height);
                    canvas.DrawRoundRect(rect, radius, radius, backgroundPaint);
                }
                else
                {
                    // Draw circular FAB
                    canvas.DrawCircle(centerX, centerY, radius, backgroundPaint);
                }
            }

            // Draw state layer
            if (State != ControlState.Normal)
            {
                float stateOpacity = State == ControlState.Pressed ? 0.12f : 0.08f;
                using (var statePaint = new SKPaint
                {
                    Color = _iconColor.WithAlpha((byte)(255 * stateOpacity)),
                    Style = SKPaintStyle.Fill
                })
                {
                    if (_isExtended)
                    {
                        var rect = new SKRect(X + _extendedPadding, Y, X + Width - _extendedPadding, Y + Height);
                        canvas.DrawRoundRect(rect, radius, radius, statePaint);
                    }
                    else
                    {
                        canvas.DrawCircle(centerX, centerY, radius, statePaint);
                    }
                }
            }

            // Draw icon (modern SKFont API)
            if (!string.IsNullOrEmpty(_icon))
            {
                using (var font = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal), _iconSize))
                using (var textPaint = new SKPaint { Color = _iconColor, IsAntialias = true })
                {
                    float textX = centerX;
                    float textY = centerY + _iconSize / 3;
                    canvas.DrawText(_icon, textX, textY, SKTextAlign.Center, font, textPaint);
                }
            }

            // Draw extended text
            if (_isExtended && !string.IsNullOrEmpty(_extendedText))
            {
                using (var font = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal), 16))
                using (var textPaint = new SKPaint { Color = _iconColor, IsAntialias = true })
                {
                    // Measure text using font
                    // MeasureText overload with string returns advance; approximate vertical positioning
                    float advance = font.MeasureText(_extendedText);
                    float textX = centerX + radius + 16;
                    // Approx baseline adjustment using font metrics
                    var metrics = font.Metrics;
                    float textY = centerY + (metrics.CapHeight / 2);
                    canvas.DrawText(_extendedText, textX, textY, SKTextAlign.Left, font, textPaint);
                }
            }
        }

        /// <summary>
        /// Determines whether the specified point is contained within the FAB.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns>True if the point is contained within the FAB.</returns>
        public override bool ContainsPoint(SKPoint point)
        {
            if (_isExtended)
            {
                // Rectangular bounds for extended FAB (absolute)
                return point.X >= X && point.X <= X + Width && point.Y >= Y && point.Y <= Y + Height;
            }
            else
            {
                // Circular bounds for regular FAB (absolute center)
                float centerX = X + _size / 2;
                float centerY = Y + _size / 2;
                float radius = _size / 2;
                float distance = (float)Math.Sqrt(Math.Pow(point.X - centerX, 2) + Math.Pow(point.Y - centerY, 2));
                return distance <= radius;
            }
        }

        /// <summary>
        /// Handles mouse down events.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>True if the event was handled.</returns>
        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            if (IsEnabled && ContainsPoint(point))
            {
                State = ControlState.Pressed;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Handles mouse up events.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>True if the event was handled.</returns>
        protected override bool OnMouseUp(SKPoint point, InteractionContext context)
        {
            if (IsEnabled && State == ControlState.Pressed)
            {
                State = ControlState.Normal;
                if (ContainsPoint(point))
                {
                    // Let the base class handle the Clicked event
                    return base.OnMouseUp(point, context);
                }
            }
            return base.OnMouseUp(point, context);
        }

        /// <summary>
        /// Handles mouse move events.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>True if the event was handled.</returns>
        protected override bool OnMouseMove(SKPoint point, InteractionContext context)
        {
            if (IsEnabled)
            {
                if (ContainsPoint(point))
                {
                    if (State != ControlState.Pressed)
                    {
                        State = ControlState.Normal;
                    }
                }
                else
                {
                    State = ControlState.Normal;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Public wrapper for mouse down events (used by demos).
        /// </summary>
        public new bool HandleMouseDown(SKPoint point, InteractionContext context)
        {
            if (IsEnabled && ContainsPoint(point))
            {
                State = ControlState.Pressed;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Public wrapper for mouse move events (used by demos).
        /// </summary>
        public new bool HandleMouseMove(SKPoint point, InteractionContext context)
        {
            if (IsEnabled)
            {
                if (ContainsPoint(point))
                {
                    if (State != ControlState.Pressed)
                    {
                        State = ControlState.Normal;
                    }
                }
                else
                {
                    State = ControlState.Normal;
                }
                return true;
            }
            return false;
        }
    }
}
