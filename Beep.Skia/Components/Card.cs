using SkiaSharp;
using System;
using System.Collections.Generic;
using Beep.Skia.Model;
namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 Card component that displays content and actions on a single topic.
    /// Cards are surfaces that display content and actions on a single subject.
    /// </summary>
    public class Card : MaterialControl
    {
        private CardVariant _variant = CardVariant.Elevated;
        private bool _isClickable = false;
        private bool _isSelectable = false;
        private bool _isSelected = false;
        private float _cornerRadius = 12f; // Material Design 3.0 card corner radius
        private SKColor _cardBackgroundColor;
        private SKColor _cardOutlineColor;
        private float _cardElevation = 1f;

        // Card content areas
        private readonly List<SkiaComponent> _headerComponents = new List<SkiaComponent>();
        private readonly List<SkiaComponent> _mediaComponents = new List<SkiaComponent>();
        private readonly List<SkiaComponent> _contentComponents = new List<SkiaComponent>();
        private readonly List<SkiaComponent> _actionComponents = new List<SkiaComponent>();

        /// <summary>
        /// Gets or sets the card variant (Elevated, Filled, or Outlined).
        /// </summary>
        public CardVariant Variant
        {
            get => _variant;
            set
            {
                if (_variant != value)
                {
                    _variant = value;
                    UpdateCardAppearance();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the card is clickable.
        /// </summary>
        public bool IsClickable
        {
            get => _isClickable;
            set
            {
                if (_isClickable != value)
                {
                    _isClickable = value;
                    UpdateCardAppearance();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the card is selectable.
        /// </summary>
        public bool IsSelectable
        {
            get => _isSelectable;
            set
            {
                if (_isSelectable != value)
                {
                    _isSelectable = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the card is selected.
        /// </summary>
        public new bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the corner radius of the card.
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
        /// Gets or sets the background color of the card.
        /// </summary>
        public SKColor CardBackgroundColor
        {
            get => _cardBackgroundColor;
            set
            {
                if (_cardBackgroundColor != value)
                {
                    _cardBackgroundColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the outline color of the card (for Outlined variant).
        /// </summary>
        public SKColor CardOutlineColor
        {
            get => _cardOutlineColor;
            set
            {
                if (_cardOutlineColor != value)
                {
                    _cardOutlineColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the elevation of the card.
        /// </summary>
        public float CardElevation
        {
            get => _cardElevation;
            set
            {
                if (_cardElevation != value)
                {
                    _cardElevation = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Occurs when the card is clicked.
        /// </summary>
        public event EventHandler<EventArgs> CardClicked;

        /// <summary>
        /// Initializes a new instance of the <see cref="Card"/> class.
        /// </summary>
        public Card()
        {
            Width = 344f; // Default Material Design card width
            Height = 120f;
            Name = "Card";
            UpdateCardAppearance();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Card"/> class with specified dimensions.
        /// </summary>
        /// <param name="width">The width of the card.</param>
        /// <param name="height">The height of the card.</param>
        public Card(float width, float height) : this()
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Updates the card's appearance based on its variant and state.
        /// </summary>
        private void UpdateCardAppearance()
        {
            switch (_variant)
            {
                case CardVariant.Elevated:
                    _cardBackgroundColor = MaterialColors.Surface;
                    _cardOutlineColor = SKColors.Transparent;
                    _cardElevation = _isClickable ? 8f : 1f;
                    break;
                case CardVariant.Filled:
                    _cardBackgroundColor = MaterialColors.SurfaceVariant;
                    _cardOutlineColor = SKColors.Transparent;
                    _cardElevation = 0f;
                    break;
                case CardVariant.Outlined:
                    _cardBackgroundColor = MaterialColors.Surface;
                    _cardOutlineColor = MaterialColors.OutlineVariant;
                    _cardElevation = 0f;
                    break;
            }
        }

        /// <summary>
        /// Adds a component to the card's header area.
        /// </summary>
        /// <param name="component">The component to add.</param>
        public void AddHeaderComponent(SkiaComponent component)
        {
            if (component != null && !_headerComponents.Contains(component))
            {
                _headerComponents.Add(component);
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Removes a component from the card's header area.
        /// </summary>
        /// <param name="component">The component to remove.</param>
        public void RemoveHeaderComponent(SkiaComponent component)
        {
            if (_headerComponents.Remove(component))
            {
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Adds a component to the card's media area.
        /// </summary>
        /// <param name="component">The component to add.</param>
        public void AddMediaComponent(SkiaComponent component)
        {
            if (component != null && !_mediaComponents.Contains(component))
            {
                _mediaComponents.Add(component);
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Removes a component from the card's media area.
        /// </summary>
        /// <param name="component">The component to remove.</param>
        public void RemoveMediaComponent(SkiaComponent component)
        {
            if (_mediaComponents.Remove(component))
            {
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Adds a component to the card's content area.
        /// </summary>
        /// <param name="component">The component to add.</param>
        public void AddContentComponent(SkiaComponent component)
        {
            if (component != null && !_contentComponents.Contains(component))
            {
                _contentComponents.Add(component);
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Removes a component from the card's content area.
        /// </summary>
        /// <param name="component">The component to remove.</param>
        public void RemoveContentComponent(SkiaComponent component)
        {
            if (_contentComponents.Remove(component))
            {
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Adds a component to the card's action area.
        /// </summary>
        /// <param name="component">The component to add.</param>
        public void AddActionComponent(SkiaComponent component)
        {
            if (component != null && !_actionComponents.Contains(component))
            {
                _actionComponents.Add(component);
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Removes a component from the card's action area.
        /// </summary>
        /// <param name="component">The component to remove.</param>
        public void RemoveActionComponent(SkiaComponent component)
        {
            if (_actionComponents.Remove(component))
            {
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Draws the card component.
        /// </summary>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Absolute coordinate rectangle
            var cardBounds = new SKRect(X, Y, X + Width, Y + Height);

            // Draw card shadow/elevation
            DrawCardShadow(canvas, cardBounds);

            // Draw card background
            DrawCardBackground(canvas, cardBounds);

            // Draw card outline (for Outlined variant)
            if (_variant == CardVariant.Outlined)
            {
                DrawCardOutline(canvas, cardBounds);
            }

            // Draw state layer
            DrawStateLayer(canvas, cardBounds, _cardBackgroundColor);

            // Layout and draw card content
            DrawCardContent(canvas, cardBounds);
        }

        /// <summary>
        /// Draws the card shadow based on elevation.
        /// </summary>
        private void DrawCardShadow(SKCanvas canvas, SKRect bounds)
        {
            if (_cardElevation <= 0)
                return;

            using (var paint = new SKPaint
            {
                Color = SKColors.Black.WithAlpha((byte)(0.2f * 255)),
                Style = SKPaintStyle.Fill,
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, _cardElevation / 2)
            })
            {
                var shadowRect = new SKRect(
                    bounds.Left + _cardElevation,
                    bounds.Top + _cardElevation,
                    bounds.Right + _cardElevation,
                    bounds.Bottom + _cardElevation);
                canvas.DrawRoundRect(shadowRect, _cornerRadius, _cornerRadius, paint);
            }
        }

        /// <summary>
        /// Draws the card background.
        /// </summary>
        private void DrawCardBackground(SKCanvas canvas, SKRect bounds)
        {
            using (var paint = new SKPaint
            {
                Color = _cardBackgroundColor,
                Style = SKPaintStyle.Fill
            })
            {
                canvas.DrawRoundRect(bounds, _cornerRadius, _cornerRadius, paint);
            }
        }

        /// <summary>
        /// Draws the card outline for Outlined variant.
        /// </summary>
        private void DrawCardOutline(SKCanvas canvas, SKRect bounds)
        {
            using (var paint = new SKPaint
            {
                Color = _cardOutlineColor,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1f,
                IsAntialias = true
            })
            {
                canvas.DrawRoundRect(bounds, _cornerRadius, _cornerRadius, paint);
            }
        }

        /// <summary>
        /// Draws the card content by laying out components in different areas.
        /// </summary>
        private void DrawCardContent(SKCanvas canvas, SKRect bounds)
        {
            float currentY = bounds.Top + 16f; // Start with padding
            float contentWidth = bounds.Width - 32f; // Horizontal padding
            float contentX = bounds.Left + 16f;

            // Draw header components
            if (_headerComponents.Count > 0)
            {
                currentY = DrawComponentArea(canvas, _headerComponents, contentX, currentY, contentWidth, 8f);
            }

            // Draw media components
            if (_mediaComponents.Count > 0)
            {
                currentY = DrawComponentArea(canvas, _mediaComponents, contentX, currentY, contentWidth, 16f);
            }

            // Draw content components
            if (_contentComponents.Count > 0)
            {
                currentY = DrawComponentArea(canvas, _contentComponents, contentX, currentY, contentWidth, 12f);
            }

            // Draw action components
            if (_actionComponents.Count > 0)
            {
                float actionsY = bounds.Bottom - 16f - 48f; // Actions area height
                DrawComponentArea(canvas, _actionComponents, contentX, actionsY, contentWidth, 8f);
            }
        }

        /// <summary>
        /// Draws a group of components in a specific area.
        /// </summary>
        private float DrawComponentArea(SKCanvas canvas, List<SkiaComponent> components, float x, float y, float width, float spacing)
        {
            float currentY = y;
            float maxHeight = 0;

            foreach (var component in components)
            {
                if (component.IsVisible)
                {
                    component.X = x;
                    component.Y = currentY;
                    component.Width = Math.Min(component.Width, width);

                    // Draw the component using the proper drawing method
                    var context = new DrawingContext();
                    component.Draw(canvas, context);

                    maxHeight = Math.Max(maxHeight, component.Height);
                    currentY += component.Height + spacing;
                }
            }

            return currentY;
        }

        /// <summary>
        /// Handles mouse down events.
        /// </summary>
        protected override bool OnMouseDown(SKPoint location, InteractionContext context)
        {
            if (_isClickable && ContainsPoint(location))
            {
                State = ControlState.Pressed;
                return true;
            }
            return base.OnMouseDown(location, context);
        }

        /// <summary>
        /// Handles mouse up events and triggers the CardClicked event.
        /// </summary>
        protected override bool OnMouseUp(SKPoint location, InteractionContext context)
        {
            if (State == ControlState.Pressed && _isClickable && ContainsPoint(location))
            {
                CardClicked?.Invoke(this, EventArgs.Empty);

                if (_isSelectable)
                {
                    IsSelected = !IsSelected;
                }
            }
            State = ControlState.Normal;
            return base.OnMouseUp(location, context);
        }

        /// <summary>
        /// Handles mouse enter events.
        /// </summary>
        protected override void OnMouseEnter()
        {
            if (_isClickable)
            {
                State = ControlState.Hovered;
            }
        }

        /// <summary>
        /// Handles mouse leave events.
        /// </summary>
        protected override void OnMouseLeave()
        {
            State = ControlState.Normal;
        }
    }

    /// <summary>
    /// Specifies the visual variant of a card.
    /// </summary>
    public enum CardVariant
    {
        /// <summary>
        /// Elevated cards have a shadow and are raised above the surface.
        /// </summary>
        Elevated,

        /// <summary>
        /// Filled cards have a colored background and no shadow.
        /// </summary>
        Filled,

        /// <summary>
        /// Outlined cards have a border and no shadow.
        /// </summary>
        Outlined
    }
}
