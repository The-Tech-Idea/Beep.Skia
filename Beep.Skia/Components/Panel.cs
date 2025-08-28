using SkiaSharp;

namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 surface container component that can hold other components.
    /// </summary>
    public class Panel : MaterialControl
    {
        private PanelVariant _variant = PanelVariant.Filled;
        private float _cornerRadius = 12; // Material Design 3.0 default corner radius
        private SKColor? _customBackgroundColor;
        private SKColor? _customBorderColor;
        private float? _customBorderWidth;
        private string _title;

        /// <summary>
        /// Material Design 3.0 panel variants.
        /// </summary>
        public enum PanelVariant
        {
            Filled,
            Outlined,
            Elevated
        }

        /// <summary>
        /// Gets or sets the panel variant (Material Design 3.0).
        /// </summary>
        public PanelVariant Variant
        {
            get => _variant;
            set
            {
                if (_variant != value)
                {
                    _variant = value;
                    UpdateVariantProperties();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the corner radius of the panel.
        /// </summary>
        public float CornerRadius
        {
            get => _cornerRadius;
            set
            {
                if (_cornerRadius != value)
                {
                    _cornerRadius = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the background color of the panel (for backward compatibility).
        /// </summary>
        public SKColor BackgroundColor
        {
            get => _customBackgroundColor ?? GetBackgroundColorForVariant(_variant);
            set
            {
                _customBackgroundColor = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the border color of the panel (for backward compatibility).
        /// </summary>
        public SKColor BorderColor
        {
            get => _customBorderColor ?? GetBorderColorForVariant(_variant);
            set
            {
                _customBorderColor = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the border width of the panel (for backward compatibility).
        /// </summary>
        public float BorderWidth
        {
            get => _customBorderWidth ?? GetBorderWidthForVariant(_variant);
            set
            {
                _customBorderWidth = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the title text displayed above the panel.
        /// </summary>
        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the Panel class.
        /// </summary>
        public Panel()
        {
            Width = 200;
            Height = 150;
            Name = "Panel";
            Elevation = 0; // Default elevation for filled panel
        }

        /// <summary>
        /// Initializes a new instance of the Panel class with specified dimensions.
        /// </summary>
        public Panel(float width, float height) : this()
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Updates properties based on the selected variant.
        /// </summary>
        private void UpdateVariantProperties()
        {
            switch (_variant)
            {
                case PanelVariant.Filled:
                    Elevation = 0;
                    break;

                case PanelVariant.Outlined:
                    Elevation = 0;
                    break;

                case PanelVariant.Elevated:
                    Elevation = 1;
                    break;
            }
        }

        /// <summary>
        /// Gets the background color for a given variant.
        /// </summary>
        private static SKColor GetBackgroundColorForVariant(PanelVariant variant)
        {
            return variant switch
            {
                PanelVariant.Filled => MaterialControl.MaterialColors.Surface,
                PanelVariant.Outlined => SKColors.Transparent,
                PanelVariant.Elevated => MaterialControl.MaterialColors.Surface,
                _ => MaterialControl.MaterialColors.Surface
            };
        }

        /// <summary>
        /// Gets the border color for a given variant.
        /// </summary>
        private static SKColor GetBorderColorForVariant(PanelVariant variant)
        {
            return variant switch
            {
                PanelVariant.Filled => SKColors.Transparent,
                PanelVariant.Outlined => MaterialControl.MaterialColors.Outline,
                PanelVariant.Elevated => SKColors.Transparent,
                _ => SKColors.Transparent
            };
        }

        /// <summary>
        /// Gets the border width for a given variant.
        /// </summary>
        private static float GetBorderWidthForVariant(PanelVariant variant)
        {
            return variant switch
            {
                PanelVariant.Filled => 0,
                PanelVariant.Outlined => 1,
                PanelVariant.Elevated => 0,
                _ => 0
            };
        }

        /// <summary>
        /// Draws the panel's content.
        /// </summary>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            var panelRect = new SKRect(0, 0, Width, Height);

            // Draw elevation shadow
            DrawElevationShadow(canvas, panelRect);

            // Draw background
            if (BackgroundColor.Alpha > 0)
            {
                using (var backgroundPaint = new SKPaint
                {
                    Color = GetStateAwareColor(
                        BackgroundColor,
                        BackgroundColor.WithAlpha((byte)(BackgroundColor.Alpha * 0.95f)),
                        BackgroundColor.WithAlpha((byte)(BackgroundColor.Alpha * 0.9f))),
                    Style = SKPaintStyle.Fill
                })
                {
                    if (CornerRadius > 0)
                    {
                        canvas.DrawRoundRect(panelRect, CornerRadius, CornerRadius, backgroundPaint);
                    }
                    else
                    {
                        canvas.DrawRect(panelRect, backgroundPaint);
                    }
                }
            }

            // Draw border
            if (BorderWidth > 0 && BorderColor.Alpha > 0)
            {
                using (var borderPaint = new SKPaint
                {
                    Color = BorderColor,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = BorderWidth
                })
                {
                    if (CornerRadius > 0)
                    {
                        canvas.DrawRoundRect(panelRect, CornerRadius, CornerRadius, borderPaint);
                    }
                    else
                    {
                        canvas.DrawRect(panelRect, borderPaint);
                    }
                }
            }

            // Draw state layer
            DrawStateLayer(canvas, panelRect, MaterialControl.MaterialColors.OnSurface);
        }

        /// <summary>
        /// Draws the panel and its title if set.
        /// </summary>
        public override void Draw(SKCanvas canvas, DrawingContext context)
        {
            // Draw title if set
            if (!string.IsNullOrEmpty(Title))
            {
                DrawTitle(canvas, context);
            }

            // Draw the panel content
            base.Draw(canvas, context);
        }

        /// <summary>
        /// Draws the title text above the panel.
        /// </summary>
        private void DrawTitle(SKCanvas canvas, DrawingContext context)
        {
            using (var titlePaint = new SKPaint
            {
                Color = MaterialControl.MaterialColors.OnSurface,
                TextSize = 12, // Small title text
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Roboto", SKFontStyle.Normal)
            })
            {
                // Position title at top left, slightly above the panel
                float titleY = -8; // Position above the panel
                canvas.DrawText(Title, 0, titleY, titlePaint);
            }
        }

        /// <summary>
        /// Adds a child component to this panel.
        /// </summary>
        public void AddChild(SkiaComponent child, float x, float y)
        {
            child.X = x;
            child.Y = y;
            AddChild(child);
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

        /// <summary>
        /// Handles mouse down event for pressed state.
        /// </summary>
        protected override bool OnMouseDown(SKPoint location, InteractionContext context)
        {
            var handled = base.OnMouseDown(location, context);
            State = ControlState.Pressed;
            return handled;
        }

        /// <summary>
        /// Handles mouse up event.
        /// </summary>
        protected override bool OnMouseUp(SKPoint location, InteractionContext context)
        {
            var handled = base.OnMouseUp(location, context);
            State = ControlState.Normal;
            return handled;
        }
    }
}
