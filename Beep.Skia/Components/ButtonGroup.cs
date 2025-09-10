using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 Button Group component that arranges buttons in horizontal, vertical, or grid layouts.
    /// </summary>
    public class ButtonGroup : MaterialControl
    {
        private ButtonGroupLayout _layout = ButtonGroupLayout.Horizontal;
        private ButtonGroupVariant _variant = ButtonGroupVariant.Filled;
        private float _spacing = 8; // Material Design 3.0 spacing between buttons
        private float _buttonWidth = 120; // Default button width
        private float _buttonHeight = 40; // Default button height
        private bool _equalWidth = false; // Whether all buttons should have equal width
        private readonly List<Button> _buttons = new List<Button>();
        private SKColor _groupBackgroundColor = MaterialColors.Surface;
        private float _cornerRadius = 12;

        /// <summary>
        /// Material Design 3.0 button group layout options.
        /// </summary>
        public enum ButtonGroupLayout
        {
            Horizontal,
            Vertical,
            Grid
        }

        /// <summary>
        /// Material Design 3.0 button group variants.
        /// </summary>
        public enum ButtonGroupVariant
        {
            Filled,
            Outlined,
            Elevated,
            Text
        }

        /// <summary>
        /// Gets or sets the layout direction of the button group.
        /// </summary>
        public ButtonGroupLayout Layout
        {
            get => _layout;
            set
            {
                if (_layout != value)
                {
                    _layout = value;
                    UpdateLayout();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the button group variant (Material Design 3.0).
        /// </summary>
        public ButtonGroupVariant Variant
        {
            get => _variant;
            set
            {
                if (_variant != value)
                {
                    _variant = value;
                    UpdateButtonVariants();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the spacing between buttons in pixels.
        /// </summary>
        public float Spacing
        {
            get => _spacing;
            set
            {
                if (_spacing != value)
                {
                    _spacing = value;
                    UpdateLayout();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the default width for buttons in the group.
        /// </summary>
        public float ButtonWidth
        {
            get => _buttonWidth;
            set
            {
                if (_buttonWidth != value)
                {
                    _buttonWidth = value;
                    UpdateLayout();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the default height for buttons in the group.
        /// </summary>
        public float ButtonHeight
        {
            get => _buttonHeight;
            set
            {
                if (_buttonHeight != value)
                {
                    _buttonHeight = value;
                    UpdateLayout();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether all buttons should have equal width.
        /// </summary>
        public bool EqualWidth
        {
            get => _equalWidth;
            set
            {
                if (_equalWidth != value)
                {
                    _equalWidth = value;
                    UpdateLayout();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the background color of the button group.
        /// </summary>
        public SKColor GroupBackgroundColor
        {
            get => _groupBackgroundColor;
            set
            {
                if (_groupBackgroundColor != value)
                {
                    _groupBackgroundColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the corner radius of the button group.
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
        /// Gets the collection of buttons in the group.
        /// </summary>
        public IReadOnlyList<Button> Buttons => _buttons.AsReadOnly();

        /// <summary>
        /// Initializes a new instance of the ButtonGroup class.
        /// </summary>
        public ButtonGroup()
        {
            Width = 400;
            Height = 60;
            UpdateVariantProperties();
        }

        /// <summary>
        /// Adds a button to the group.
        /// </summary>
        /// <param name="button">The button to add.</param>
        public void AddButton(Button button)
        {
            if (button == null) throw new ArgumentNullException(nameof(button));

            _buttons.Add(button);
            UpdateLayout();
            InvalidateVisual();
        }

        /// <summary>
        /// Removes a button from the group.
        /// </summary>
        /// <param name="button">The button to remove.</param>
        /// <returns>True if the button was removed; otherwise, false.</returns>
        public bool RemoveButton(Button button)
        {
            if (button == null) return false;

            bool removed = _buttons.Remove(button);
            if (removed)
            {
                UpdateLayout();
                InvalidateVisual();
            }
            return removed;
        }

        /// <summary>
        /// Creates a simple horizontal button group with the specified button texts.
        /// </summary>
        /// <param name="buttonTexts">The texts for the buttons.</param>
        /// <returns>A configured ButtonGroup instance.</returns>
        public static ButtonGroup CreateHorizontalGroup(params string[] buttonTexts)
        {
            var group = new ButtonGroup
            {
                Layout = ButtonGroupLayout.Horizontal,
                Variant = ButtonGroupVariant.Filled
            };

            foreach (var text in buttonTexts)
            {
                var button = new Button
                {
                    Text = text,
                    Width = 120,
                    Height = 40
                };
                group.AddButton(button);
            }

            return group;
        }

        /// <summary>
        /// Creates a vertical button group with the specified button texts.
        /// </summary>
        /// <param name="buttonTexts">The texts for the buttons.</param>
        /// <returns>A configured ButtonGroup instance.</returns>
        public static ButtonGroup CreateVerticalGroup(params string[] buttonTexts)
        {
            var group = new ButtonGroup
            {
                Layout = ButtonGroupLayout.Vertical,
                Variant = ButtonGroupVariant.Filled
            };

            foreach (var text in buttonTexts)
            {
                var button = new Button
                {
                    Text = text,
                    Width = 120,
                    Height = 40
                };
                group.AddButton(button);
            }

            return group;
        }

        /// <summary>
        /// Creates a button grid with the specified button texts and grid dimensions.
        /// </summary>
        /// <param name="buttonTexts">The texts for the buttons.</param>
        /// <param name="columns">The number of columns in the grid.</param>
        /// <returns>A configured ButtonGroup instance.</returns>
        public static ButtonGroup CreateButtonGrid(string[] buttonTexts, int columns)
        {
            var group = new ButtonGroup
            {
                Layout = ButtonGroupLayout.Grid,
                Variant = ButtonGroupVariant.Filled
            };

            foreach (var text in buttonTexts)
            {
                var button = new Button
                {
                    Text = text,
                    Width = 100,
                    Height = 40
                };
                group.AddButton(button);
            }

            return group;
        }

        /// <summary>
        /// Updates the layout of buttons based on the current layout settings.
        /// </summary>
        private void UpdateLayout()
        {
            if (_buttons.Count == 0) return;

            switch (_layout)
            {
                case ButtonGroupLayout.Horizontal:
                    UpdateHorizontalLayout();
                    break;
                case ButtonGroupLayout.Vertical:
                    UpdateVerticalLayout();
                    break;
                case ButtonGroupLayout.Grid:
                    UpdateGridLayout();
                    break;
            }
        }

        /// <summary>
        /// Updates the horizontal layout of buttons.
        /// </summary>
        private void UpdateHorizontalLayout()
        {
            float totalWidth = 0;
            float maxHeight = 0;

            // Calculate total width and max height
            foreach (var button in _buttons)
            {
                button.Width = _equalWidth ? _buttonWidth : Math.Max(button.Width, _buttonWidth);
                button.Height = _buttonHeight;
                totalWidth += button.Width + _spacing;
                maxHeight = Math.Max(maxHeight, button.Height);
            }

            // Remove extra spacing from the end
            totalWidth -= _spacing;

            // Position buttons horizontally
            float currentX = 0;
            foreach (var button in _buttons)
            {
                button.X = currentX;
                button.Y = 0;
                currentX += button.Width + _spacing;
            }

            // Update group dimensions
            Width = totalWidth;
            Height = maxHeight;
        }

        /// <summary>
        /// Updates the vertical layout of buttons.
        /// </summary>
        private void UpdateVerticalLayout()
        {
            float maxWidth = 0;
            float totalHeight = 0;

            // Calculate max width and total height
            foreach (var button in _buttons)
            {
                button.Width = _equalWidth ? _buttonWidth : Math.Max(button.Width, _buttonWidth);
                button.Height = _buttonHeight;
                maxWidth = Math.Max(maxWidth, button.Width);
                totalHeight += button.Height + _spacing;
            }

            // Remove extra spacing from the end
            totalHeight -= _spacing;

            // Position buttons vertically
            float currentY = 0;
            foreach (var button in _buttons)
            {
                button.X = 0;
                button.Y = currentY;
                currentY += button.Height + _spacing;
            }

            // Update group dimensions
            Width = maxWidth;
            Height = totalHeight;
        }

        /// <summary>
        /// Updates the grid layout of buttons.
        /// </summary>
        private void UpdateGridLayout()
        {
            if (_buttons.Count == 0) return;

            // Calculate grid dimensions (simple square-ish grid)
            int columns = (int)Math.Ceiling(Math.Sqrt(_buttons.Count));
            int rows = (int)Math.Ceiling((double)_buttons.Count / columns);

            float cellWidth = _buttonWidth + _spacing;
            float cellHeight = _buttonHeight + _spacing;

            // Position buttons in grid
            for (int i = 0; i < _buttons.Count; i++)
            {
                var button = _buttons[i];
                button.Width = _buttonWidth;
                button.Height = _buttonHeight;

                int row = i / columns;
                int col = i % columns;

                button.X = col * cellWidth;
                button.Y = row * cellHeight;
            }

            // Update group dimensions
            Width = columns * cellWidth - _spacing;
            Height = rows * cellHeight - _spacing;
        }

        /// <summary>
        /// Updates button variants based on the group variant.
        /// </summary>
        private void UpdateButtonVariants()
        {
            foreach (var button in _buttons)
            {
                switch (_variant)
                {
                    case ButtonGroupVariant.Filled:
                        button.Variant = Button.ButtonVariant.Filled;
                        break;
                    case ButtonGroupVariant.Outlined:
                        button.Variant = Button.ButtonVariant.Outlined;
                        break;
                    case ButtonGroupVariant.Elevated:
                        button.Variant = Button.ButtonVariant.Elevated;
                        break;
                    case ButtonGroupVariant.Text:
                        button.Variant = Button.ButtonVariant.Text;
                        break;
                }
            }
        }

        /// <summary>
        /// Updates variant-specific properties.
        /// </summary>
        private void UpdateVariantProperties()
        {
            switch (_variant)
            {
                case ButtonGroupVariant.Filled:
                    _groupBackgroundColor = MaterialColors.Surface;
                    break;
                case ButtonGroupVariant.Outlined:
                    _groupBackgroundColor = MaterialColors.Surface;
                    break;
                case ButtonGroupVariant.Elevated:
                    _groupBackgroundColor = MaterialColors.Surface;
                    break;
                case ButtonGroupVariant.Text:
                    _groupBackgroundColor = SKColor.Empty; // Transparent
                    break;
            }
        }

        /// <summary>
        /// Draws the button group content.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Absolute coordinate model: background uses this group's X,Y
            if (_groupBackgroundColor != SKColor.Empty)
            {
                using var backgroundPaint = new SKPaint { Color = _groupBackgroundColor, IsAntialias = true };
                var backgroundRect = new SKRect(X, Y, X + Width, Y + Height);
                canvas.DrawRoundRect(backgroundRect, _cornerRadius, _cornerRadius, backgroundPaint);
            }

            // Draw each button at its absolute position (button.X/Y already absolute)
            foreach (var button in _buttons)
            {
                button.Draw(canvas, context);
            }
        }

        /// <summary>
        /// Handles mouse down events for the button group.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>true if the event was handled; otherwise, false.</returns>
        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            // Forward mouse events to individual buttons
            foreach (var button in _buttons)
            {
                if (point.X >= button.X && point.X <= button.X + button.Width &&
                    point.Y >= button.Y && point.Y <= button.Y + button.Height)
                {
                    var localPoint = new SKPoint(point.X - button.X, point.Y - button.Y);
                    return button.HandleMouseDown(localPoint, context);
                }
            }

            return base.OnMouseDown(point, context);
        }

        /// <summary>
        /// Handles mouse move events for the button group.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>true if the event was handled; otherwise, false.</returns>
        protected override bool OnMouseMove(SKPoint point, InteractionContext context)
        {
            // Forward mouse events to individual buttons
            foreach (var button in _buttons)
            {
                if (point.X >= button.X && point.X <= button.X + button.Width &&
                    point.Y >= button.Y && point.Y <= button.Y + button.Height)
                {
                    var localPoint = new SKPoint(point.X - button.X, point.Y - button.Y);
                    return button.HandleMouseMove(localPoint, context);
                }
            }

            return base.OnMouseMove(point, context);
        }

        /// <summary>
        /// Handles mouse up events for the button group.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>true if the event was handled; otherwise, false.</returns>
        protected override bool OnMouseUp(SKPoint point, InteractionContext context)
        {
            // Forward mouse events to individual buttons
            foreach (var button in _buttons)
            {
                if (point.X >= button.X && point.X <= button.X + button.Width &&
                    point.Y >= button.Y && point.Y <= button.Y + button.Height)
                {
                    var localPoint = new SKPoint(point.X - button.X, point.Y - button.Y);
                    return button.HandleMouseUp(localPoint, context);
                }
            }

            return base.OnMouseUp(point, context);
        }
    }
}
