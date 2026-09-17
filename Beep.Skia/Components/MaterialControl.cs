using SkiaSharp;
using SkiaSharp.Extended.Svg;
using System;
using System.IO;
using Beep.Skia.Model;
namespace Beep.Skia.Components
{
    /// <summary>
    /// Base class for Material Design 3.0 controls with SVG support and modern styling.
    /// </summary>
    public abstract class MaterialControl : SkiaComponent
    {
        /// <summary>
        /// Occurs when the control is clicked.
        /// </summary>
        public event EventHandler<EventArgs> Clicked;

        // Material Design 3.0 Color Tokens — now reads from ThemeManager.Current
        protected static class MaterialColors
        {
            private static SkiaTheme T => ThemeManager.Current;
            /// <summary>
            /// Gets or sets the primary.
            /// </summary>
            public static SKColor Primary => T.Primary;
            /// <summary>
            /// Gets or sets the on primary.
            /// </summary>
            public static SKColor OnPrimary => T.OnPrimary;
            /// <summary>
            /// Gets or sets the primary container.
            /// </summary>
            public static SKColor PrimaryContainer => T.PrimaryContainer;
            /// <summary>
            /// Gets or sets the on primary container.
            /// </summary>
            public static SKColor OnPrimaryContainer => T.OnPrimaryContainer;
            /// <summary>
            /// Gets or sets the secondary.
            /// </summary>
            public static SKColor Secondary => T.Secondary;
            /// <summary>
            /// Gets or sets the on secondary.
            /// </summary>
            public static SKColor OnSecondary => T.OnSecondary;
            /// <summary>
            /// Gets or sets the secondary container.
            /// </summary>
            public static SKColor SecondaryContainer => T.SecondaryContainer;
            /// <summary>
            /// Gets or sets the on secondary container.
            /// </summary>
            public static SKColor OnSecondaryContainer => T.OnSecondaryContainer;
            /// <summary>
            /// Gets or sets the tertiary.
            /// </summary>
            public static SKColor Tertiary => T.Tertiary;
            /// <summary>
            /// Gets or sets the on tertiary.
            /// </summary>
            public static SKColor OnTertiary => T.OnTertiary;
            /// <summary>
            /// Gets or sets the tertiary container.
            /// </summary>
            public static SKColor TertiaryContainer => T.TertiaryContainer;
            /// <summary>
            /// Gets or sets the on tertiary container.
            /// </summary>
            public static SKColor OnTertiaryContainer => T.OnTertiaryContainer;
            /// <summary>
            /// Gets or sets the error.
            /// </summary>
            public static SKColor Error => T.Error;
            /// <summary>
            /// Gets or sets the on error.
            /// </summary>
            public static SKColor OnError => T.OnError;
            /// <summary>
            /// Gets or sets the error container.
            /// </summary>
            public static SKColor ErrorContainer => T.ErrorContainer;
            /// <summary>
            /// Gets or sets the on error container.
            /// </summary>
            public static SKColor OnErrorContainer => T.OnErrorContainer;
            /// <summary>
            /// Gets or sets the surface.
            /// </summary>
            public static SKColor Surface => T.Surface;
            /// <summary>
            /// Gets or sets the on surface.
            /// </summary>
            public static SKColor OnSurface => T.OnSurface;
            /// <summary>
            /// Gets or sets the surface variant.
            /// </summary>
            public static SKColor SurfaceVariant => T.SurfaceVariant;
            /// <summary>
            /// Gets or sets the on surface variant.
            /// </summary>
            public static SKColor OnSurfaceVariant => T.OnSurfaceVariant;
            /// <summary>
            /// Gets or sets the surface container.
            /// </summary>
            public static SKColor SurfaceContainer => T.SurfaceContainer;
            /// <summary>
            /// Gets or sets the surface container high.
            /// </summary>
            public static SKColor SurfaceContainerHigh => T.SurfaceContainerHigh;
            /// <summary>
            /// Gets or sets the outline.
            /// </summary>
            public static SKColor Outline => T.Outline;
            /// <summary>
            /// Gets or sets the outline variant.
            /// </summary>
            public static SKColor OutlineVariant => T.OutlineVariant;
        }

        // Material Design 3.0 State Layer Opacities
        protected static class StateLayerOpacity
        {
            public const float Hover = 0.08f;
            public const float Focus = 0.12f;
            public const float Press = 0.12f;
            public const float Drag = 0.16f;
        }

        // SVG Support
        private SkiaSharp.Extended.Svg.SKSvg _svgDocument;
        private string _svgSource;
        private SKPicture _cachedPicture;
        private bool _isSvgLoaded;
        private SvgScaleMode _svgScaleMode = SvgScaleMode.Fit;
        private bool _maintainAspectRatio = true;

        // Material Design Properties
    private ControlState _controlState = ControlState.Normal;
        private float _elevation = 0;
        private float _stateLayerOpacity = 0;
        private SKColor _stateLayerColor = SKColors.Black;

        // Control States
        protected enum ControlState
        {
            Normal,
            Hovered,
            Pressed,
            Focused,
            Disabled
        }

        /// <summary>
        /// Gets or sets the current control state.
        /// </summary>
        protected new ControlState State
        {
            get => _controlState;
            set
            {
                if (_controlState != value)
                {
                    _controlState = value;
                    UpdateStateLayer();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the elevation of the control (Material Design shadow).
        /// </summary>
        public float Elevation
        {
            get => _elevation;
            set
            {
                if (_elevation != value)
                {
                    _elevation = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the SVG source for the control's icon.
        /// </summary>
        public string SvgIcon
        {
            get => _svgSource;
            set
            {
                if (_svgSource != value)
                {
                    _svgSource = value;
                    LoadSvg();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets how the SVG should be scaled.
        /// </summary>
        public SvgScaleMode SvgScaleMode
        {
            get => _svgScaleMode;
            set
            {
                if (_svgScaleMode != value)
                {
                    _svgScaleMode = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to maintain aspect ratio when scaling SVG.
        /// </summary>
        public bool MaintainAspectRatio
        {
            get => _maintainAspectRatio;
            set
            {
                if (_maintainAspectRatio != value)
                {
                    _maintainAspectRatio = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Invalidates the visual representation of the control.
        /// </summary>
        public virtual void InvalidateVisual()
        {
            // In a real implementation, this would trigger a redraw
            // For now, we'll just mark the component as needing update
            // State = ComponentState.Updating; // Fixed: This was causing the type conversion error
        }

        /// <summary>
        /// Public wrapper for InvalidateVisual() to allow access from demo classes.
        /// </summary>
        public void RefreshVisual()
        {
            InvalidateVisual();
        }

        /// <summary>
        /// Ensures connection ports are laid out before drawing. Families should
        /// call this early inside DrawContent. The base implementation relies on
        /// family classes implementing a protected virtual void LayoutPorts().
        /// </summary>
        protected void EnsurePortLayout(Action layoutPorts)
        {
            if (ArePortsDirty)
            {
                try { layoutPorts?.Invoke(); }
                finally { ClearPortsDirty(); }
            }
        }

        /// <summary>
        /// Initializes a new instance of the MaterialControl class.
        /// </summary>
        protected MaterialControl()
        {
            // Set default Material Design properties
            Width = 120;
            Height = 40;
        }

        /// <summary>
        /// Loads the SVG from the specified source.
        /// </summary>
        private void LoadSvg()
        {
            if (string.IsNullOrEmpty(_svgSource))
            {
                _svgDocument = null;
                _cachedPicture = null;
                _isSvgLoaded = false;
                return;
            }

            try
            {
                _svgDocument = new SkiaSharp.Extended.Svg.SKSvg();

                // Check if source is a file path
                if (File.Exists(_svgSource))
                {
                    using (var stream = File.OpenRead(_svgSource))
                    {
                        _svgDocument.Load(stream);
                    }
                }
                else
                {
                    // Assume it's SVG content
                    using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(_svgSource)))
                    {
                        _svgDocument.Load(stream);
                    }
                }

                _cachedPicture = _svgDocument.Picture;
                _isSvgLoaded = true;
            }
            catch (Exception)
            {
                // Handle loading errors gracefully
                _svgDocument = null;
                _cachedPicture = null;
                _isSvgLoaded = false;
            }
        }

        /// <summary>
        /// Updates the state layer based on the current control state.
        /// </summary>
        private void UpdateStateLayer()
        {
            switch (_controlState)
            {
                case ControlState.Hovered:
                    _stateLayerOpacity = StateLayerOpacity.Hover;
                    break;
                case ControlState.Pressed:
                    _stateLayerOpacity = StateLayerOpacity.Press;
                    break;
                case ControlState.Focused:
                    _stateLayerOpacity = StateLayerOpacity.Focus;
                    break;
                default:
                    _stateLayerOpacity = 0;
                    break;
            }
        }

        /// <summary>
        /// Draws the SVG icon if available.
        /// </summary>
        protected void DrawSvgIcon(SKCanvas canvas, SKRect iconRect)
        {
            if (!_isSvgLoaded || _cachedPicture == null)
                return;

            // Calculate the destination rectangle for the SVG
            SKRect destRect = CalculateSvgDestinationRect(iconRect);

            // Draw the SVG picture
            var matrix = SKMatrix.CreateTranslation(destRect.Left, destRect.Top);
            canvas.DrawPicture(_cachedPicture, in matrix);
        }

        /// <summary>
        /// Draws a specific SVG icon at the given rectangle.
        /// </summary>
        protected void DrawSvgIcon(SKCanvas canvas, SKRect iconRect, string iconPath)
        {
            if (string.IsNullOrEmpty(iconPath))
                return;

            // Temporarily store current SVG state
            var originalSource = _svgSource;
            var originalLoaded = _isSvgLoaded;
            var originalPicture = _cachedPicture;

            try
            {
                // Load the specific icon
                _svgSource = iconPath;
                LoadSvg();

                if (_isSvgLoaded && _cachedPicture != null)
                {
                    // Calculate the destination rectangle for the SVG
                    SKRect destRect = CalculateSvgDestinationRect(iconRect);

                    // Draw the SVG picture
                    var matrix = SKMatrix.CreateTranslation(destRect.Left, destRect.Top);
                    canvas.DrawPicture(_cachedPicture, in matrix);
                }
            }
            finally
            {
                // Restore original SVG state
                _svgSource = originalSource;
                _isSvgLoaded = originalLoaded;
                _cachedPicture = originalPicture;
            }
        }

        /// <summary>
        /// Draws a specific SVG icon at the given position with specified size and color.
        /// </summary>
        protected void DrawSvgIcon(SKCanvas canvas, string iconPath, float x, float y, float width, float height, SKColor color)
        {
            if (string.IsNullOrEmpty(iconPath))
                return;

            // Temporarily store current SVG state
            var originalSource = _svgSource;
            var originalLoaded = _isSvgLoaded;
            var originalPicture = _cachedPicture;

            try
            {
                // Load the specific icon
                _svgSource = iconPath;
                LoadSvg();

                if (_isSvgLoaded && _cachedPicture != null)
                {
                    // Create destination rectangle
                    SKRect destRect = new SKRect(x, y, x + width, y + height);

                    // Calculate the destination rectangle for the SVG within the given bounds
                    SKRect finalRect = CalculateSvgDestinationRect(destRect);

                    // Draw the SVG picture
                    var matrix = SKMatrix.CreateTranslation(finalRect.Left, finalRect.Top);
                    canvas.DrawPicture(_cachedPicture, in matrix);
                }
            }
            finally
            {
                // Restore original SVG state
                _svgSource = originalSource;
                _isSvgLoaded = originalLoaded;
                _cachedPicture = originalPicture;
            }
        }

        /// <summary>
        /// Calculates the destination rectangle for the SVG within the given bounds.
        /// </summary>
        private SKRect CalculateSvgDestinationRect(SKRect bounds)
        {
            if (_cachedPicture == null)
                return bounds;

            var sourceRect = _cachedPicture.CullRect;
            float scaleX = bounds.Width / sourceRect.Width;
            float scaleY = bounds.Height / sourceRect.Height;
            float scale = Math.Min(scaleX, scaleY);

            if (_svgScaleMode == SvgScaleMode.Fill)
                scale = Math.Max(scaleX, scaleY);
            else if (_svgScaleMode == SvgScaleMode.Stretch)
                scale = 1.0f;

            if (!_maintainAspectRatio)
                scale = 1.0f;

            float destWidth = sourceRect.Width * scale;
            float destHeight = sourceRect.Height * scale;
            float destX = bounds.Left + (bounds.Width - destWidth) / 2;
            float destY = bounds.Top + (bounds.Height - destHeight) / 2;

            return new SKRect(destX, destY, destX + destWidth, destY + destHeight);
        }

        /// <summary>
        /// Draws a Material Design state layer.
        /// </summary>
        protected void DrawStateLayer(SKCanvas canvas, SKRect bounds, SKColor baseColor)
        {
            if (_stateLayerOpacity > 0)
            {
                using (var paint = new SKPaint
                {
                    Color = _stateLayerColor.WithAlpha((byte)(_stateLayerOpacity * 255)),
                    Style = SKPaintStyle.Fill
                })
                {
                    canvas.DrawRect(bounds, paint);
                }
            }
        }

        /// <summary>
        /// Draws a Material Design elevation shadow.
        /// </summary>
        protected void DrawElevationShadow(SKCanvas canvas, SKRect bounds)
        {
            if (_elevation <= 0)
                return;

            // Simple shadow implementation - in a real implementation,
            // you'd use more sophisticated shadow rendering
            var shadowColor = SKColors.Black.WithAlpha(0x20);
            var shadowOffset = _elevation;

            using (var paint = new SKPaint
            {
                Color = shadowColor,
                Style = SKPaintStyle.Fill,
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, _elevation / 2)
            })
            {
                var shadowRect = new SKRect(
                    bounds.Left + shadowOffset,
                    bounds.Top + shadowOffset,
                    bounds.Right + shadowOffset,
                    bounds.Bottom + shadowOffset);
                canvas.DrawRect(shadowRect, paint);
            }
        }

        /// <summary>
        /// Gets the appropriate color based on the current state.
        /// </summary>
        protected SKColor GetStateAwareColor(SKColor normalColor, SKColor hoverColor, SKColor pressedColor)
        {
            switch (_controlState)
            {
                case ControlState.Hovered:
                    return hoverColor;
                case ControlState.Pressed:
                    return pressedColor;
                default:
                    return normalColor;
            }
        }

        /// <summary>
        /// Called when the mouse enters the component bounds.
        /// </summary>
        protected virtual void OnMouseEnter()
        {
            // Default implementation does nothing
        }

        /// <summary>
        /// Called when the mouse leaves the component bounds.
        /// </summary>
        protected virtual void OnMouseLeave()
        {
            // Default implementation does nothing
        }

        /// <summary>
        /// Handles mouse move events and detects enter/leave events.
        /// </summary>
        protected override bool OnMouseMove(SKPoint point, InteractionContext context)
        {
            // For now, we'll use a simple approach to detect mouse enter/leave
            // In a more sophisticated implementation, we could track previous mouse position
            var isInside = ContainsPoint(point);

            if (isInside && _controlState == ControlState.Normal)
            {
                OnMouseEnter();
            }
            else if (!isInside && _controlState != ControlState.Normal)
            {
                OnMouseLeave();
            }

            return base.OnMouseMove(point, context);
        }

        /// <summary>
        /// Handles mouse down events.
        /// </summary>
        protected override bool OnMouseDown(SKPoint location, InteractionContext context)
        {
            State = ControlState.Pressed;
            return base.OnMouseDown(location, context);
        }

        /// <summary>
        /// Handles mouse up events and triggers the Clicked event.
        /// </summary>
        protected override bool OnMouseUp(SKPoint location, InteractionContext context)
        {
            if (State == ControlState.Pressed)
            {
                // Trigger the Clicked event
                Clicked?.Invoke(this, EventArgs.Empty);
            }
            State = ControlState.Normal;
            return base.OnMouseUp(location, context);
        }

        /// <summary>
        /// Disposes the SVG resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cachedPicture?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Specifies how the SVG should be scaled within its container.
    /// </summary>
    public enum SvgScaleMode
    {
        /// <summary>
        /// Scales the SVG to fit within the container while maintaining aspect ratio.
        /// </summary>
        Fit,

        /// <summary>
        /// Scales the SVG to fill the container while maintaining aspect ratio (may crop).
        /// </summary>
        Fill,

        /// <summary>
        /// Stretches the SVG to fill the container without maintaining aspect ratio.
        /// </summary>
        Stretch
    }
}
