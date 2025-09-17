using SkiaSharp;
using System;
using Beep.Skia.Model;
namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 Menu Button component that combines a button with a dropdown menu.
    /// </summary>
    public class MenuButton : MaterialControl
    {
        private string _text = "";
        private string _icon = "";
        private Menu _menu;
        private bool _isMenuOpen;
        private SKColor _buttonColor;
        private SKColor _hoverColor;
        private SKColor _pressedColor;
        private float _cornerRadius = 8f;
        private ButtonVariant _variant = ButtonVariant.Filled;
        private bool _isHovered;
        private bool _isPressed;

        /// <summary>
        /// Gets or sets the text displayed on the button.
        /// </summary>
        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the icon displayed on the button.
        /// </summary>
        public string Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the dropdown menu.
        /// </summary>
        public Menu Menu
        {
            get => _menu;
            set
            {
                if (_menu != null)
                {
                    _menu.VisibilityChanged -= OnMenuHidden;
                }

                _menu = value;

                if (_menu != null)
                {
                    _menu.VisibilityChanged += OnMenuHidden;
                }
            }
        }

        /// <summary>
        /// Gets whether the dropdown menu is currently open.
        /// </summary>
        public bool IsMenuOpen
        {
            get => _isMenuOpen;
            private set
            {
                if (_isMenuOpen != value)
                {
                    _isMenuOpen = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the button color.
        /// </summary>
        public SKColor ButtonColor
        {
            get => _buttonColor;
            set
            {
                _buttonColor = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the hover color.
        /// </summary>
        public SKColor HoverColor
        {
            get => _hoverColor;
            set
            {
                _hoverColor = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the pressed color.
        /// </summary>
        public SKColor PressedColor
        {
            get => _pressedColor;
            set
            {
                _pressedColor = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the corner radius of the button.
        /// </summary>
        public float CornerRadius
        {
            get => _cornerRadius;
            set
            {
                _cornerRadius = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets the button variant.
        /// </summary>
        public ButtonVariant Variant
        {
            get => _variant;
            set
            {
                _variant = value;
                UpdateColors();
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Occurs when the button is clicked.
        /// </summary>
        public event EventHandler ButtonClicked;

        /// <summary>
        /// Occurs when the menu is opened.
        /// </summary>
        public event EventHandler MenuOpened;

        /// <summary>
        /// Occurs when the menu is closed.
        /// </summary>
        public event EventHandler MenuClosed;

        /// <summary>
        /// Initializes a new instance of the MenuButton class.
        /// </summary>
        public MenuButton()
        {
            Width = 120f;
            Height = 36f;
            Variant = ButtonVariant.Filled;
            UpdateColors();
        }

        /// <summary>
        /// Initializes a new instance of the MenuButton class with the specified text.
        /// </summary>
        /// <param name="text">The text to display on the button.</param>
        public MenuButton(string text) : this()
        {
            Text = text;
        }

        /// <summary>
        /// Initializes a new instance of the MenuButton class with the specified text and menu.
        /// </summary>
        /// <param name="text">The text to display on the button.</param>
        /// <param name="menu">The dropdown menu.</param>
        public MenuButton(string text, Menu menu) : this(text)
        {
            Menu = menu;
        }

        private void UpdateColors()
        {
            switch (Variant)
            {
                case ButtonVariant.Filled:
                    ButtonColor = MaterialColors.Primary;
                    HoverColor = MaterialColors.Primary.WithAlpha(230);
                    PressedColor = MaterialColors.Primary.WithAlpha(200);
                    break;
                case ButtonVariant.Outlined:
                    ButtonColor = SKColors.Transparent;
                    HoverColor = MaterialColors.Primary.WithAlpha(12);
                    PressedColor = MaterialColors.Primary.WithAlpha(20);
                    break;
                case ButtonVariant.Text:
                    ButtonColor = SKColors.Transparent;
                    HoverColor = MaterialColors.OnSurface.WithAlpha(8);
                    PressedColor = MaterialColors.OnSurface.WithAlpha(12);
                    break;
                case ButtonVariant.Elevated:
                    ButtonColor = MaterialColors.Surface;
                    HoverColor = MaterialColors.Surface.WithAlpha(240);
                    PressedColor = MaterialColors.Surface.WithAlpha(220);
                    break;
                case ButtonVariant.Tonal:
                    ButtonColor = MaterialColors.SecondaryContainer;
                    HoverColor = MaterialColors.SecondaryContainer.WithAlpha(230);
                    PressedColor = MaterialColors.SecondaryContainer.WithAlpha(200);
                    break;
            }
        }

        /// <summary>
        /// Opens the dropdown menu.
        /// </summary>
        public void OpenMenu()
        {
            if (Menu == null || IsMenuOpen) return;

            // Calculate menu position (below the button)
            var buttonBounds = Bounds;
            var menuPosition = new SKPoint(buttonBounds.Left, buttonBounds.Bottom + 4);

            Menu.Show(menuPosition);
            IsMenuOpen = true;
            MenuOpened?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Closes the dropdown menu.
        /// </summary>
        public void CloseMenu()
        {
            if (Menu == null || !IsMenuOpen) return;

            Menu.Hide();
            IsMenuOpen = false;
            MenuClosed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Toggles the dropdown menu open/closed state.
        /// </summary>
        public void ToggleMenu()
        {
            if (IsMenuOpen)
            {
                CloseMenu();
            }
            else
            {
                OpenMenu();
            }
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Absolute bounds
            var buttonRect = new SKRect(X, Y, X + Width, Y + Height);

            using (var paint = new SKPaint())
            {
                paint.IsAntialias = true;

                SKColor backgroundColor = ButtonColor;
                if (_isPressed) backgroundColor = PressedColor; else if (_isHovered) backgroundColor = HoverColor;

                if (Variant == ButtonVariant.Outlined)
                {
                    paint.Color = backgroundColor;
                    paint.Style = SKPaintStyle.Fill;
                    canvas.DrawRoundRect(buttonRect, CornerRadius, CornerRadius, paint);
                    paint.Color = MaterialColors.Outline;
                    paint.Style = SKPaintStyle.Stroke;
                    paint.StrokeWidth = 1f;
                    canvas.DrawRoundRect(buttonRect, CornerRadius, CornerRadius, paint);
                }
                else
                {
                    paint.Color = backgroundColor;
                    paint.Style = SKPaintStyle.Fill;
                    canvas.DrawRoundRect(buttonRect, CornerRadius, CornerRadius, paint);
                }

                if (Variant == ButtonVariant.Elevated)
                {
                    DrawElevationShadow(canvas, buttonRect);
                }
            }

            DrawButtonContent(canvas, new SKRect(X, Y, X + Width, Y + Height));
        }

        private void DrawButtonContent(SKCanvas canvas, SKRect bounds)
        {
            // Determine text color
            SKColor textColor;
            switch (Variant)
            {
                case ButtonVariant.Filled:
                    textColor = MaterialColors.OnPrimary; break;
                case ButtonVariant.Outlined:
                case ButtonVariant.Text:
                case ButtonVariant.Elevated:
                case ButtonVariant.Tonal:
                    textColor = _isPressed ? MaterialColors.Primary : _isHovered ? MaterialColors.Primary : MaterialColors.OnSurface; break;
                default:
                    textColor = MaterialColors.OnSurface; break;
            }

            float leftPadding = 12f;

            // Fonts
            using var iconFont = new SKFont(SKTypeface.Default, 16f);
            using var textFont = new SKFont(SKTypeface.Default, 14f);
            using var arrowFont = new SKFont(SKTypeface.Default, 12f);
            var textMetrics = textFont.Metrics;
            float capHeight = textMetrics.CapHeight; // positive
            float baseline = bounds.Top + (bounds.Height + capHeight) / 2f;

            // Paint reused
            using var paint = new SKPaint { IsAntialias = true, Color = textColor };

            float cursorX = bounds.Left + leftPadding;

            // Icon
            if (!string.IsNullOrEmpty(Icon))
            {
                var iconMetrics = iconFont.Metrics;
                float iconBaseline = bounds.Top + (bounds.Height + iconMetrics.CapHeight) / 2f;
                canvas.DrawText(Icon, cursorX, iconBaseline, SKTextAlign.Left, iconFont, paint);
                float iconAdvance = iconFont.MeasureText(Icon) + 8f;
                cursorX += iconAdvance;
            }

            // Compute remaining width for centering text/arrow cluster
            float arrowWidth = arrowFont.MeasureText("▼");
            float textWidth = !string.IsNullOrEmpty(Text) ? textFont.MeasureText(Text) : 0f;
            float clusterWidth = textWidth + 8f + arrowWidth; // 8 padding between text and arrow
            float clusterStart = bounds.Left + (bounds.Width - clusterWidth) / 2f;

            // If we had an icon drawn at left, ensure cluster doesn't overlap icon; simple clamp
            if (clusterStart < cursorX) clusterStart = cursorX;

            float textX = clusterStart;
            if (!string.IsNullOrEmpty(Text))
            {
                canvas.DrawText(Text, textX, baseline, SKTextAlign.Left, textFont, paint);
                textX += textWidth + 8f;
            }

            // Arrow
            paint.Color = textColor.WithAlpha(179);
            var arrowMetrics = arrowFont.Metrics;
            float arrowBaseline = bounds.Top + (bounds.Height + arrowMetrics.CapHeight) / 2f;
            canvas.DrawText("▼", textX, arrowBaseline, SKTextAlign.Left, arrowFont, paint);
        }

        protected override void OnMouseEnter()
        {
            base.OnMouseEnter();
            _isHovered = true;
            InvalidateVisual();
        }

        protected override void OnMouseLeave()
        {
            base.OnMouseLeave();
            _isHovered = false;
            InvalidateVisual();
        }

        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            base.OnMouseDown(point, context);

            if (context.MouseButton == 0) // Left button
            {
                _isPressed = true;
                InvalidateVisual();
            }

            return true;
        }

        protected override bool OnMouseUp(SKPoint point, InteractionContext context)
        {
            base.OnMouseUp(point, context);

            if (context.MouseButton == 0) // Left button
            {
                _isPressed = false;
                InvalidateVisual();

                // Handle click
                ButtonClicked?.Invoke(this, EventArgs.Empty);
                ToggleMenu();
            }

            return true;
        }

        private void OnMenuHidden(object sender, EventArgs e)
        {
            IsMenuOpen = false;
            MenuClosed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Public wrapper for mouse down events (used by demos).
        /// </summary>
        public new bool HandleMouseDown(SKPoint point, InteractionContext context)
        {
            base.OnMouseDown(point, context);

            if (context.MouseButton == 0) // Left button
            {
                _isPressed = true;
                InvalidateVisual();
            }

            return true;
        }

        /// <summary>
        /// Button variant enumeration.
        /// </summary>
        public enum ButtonVariant
        {
            /// <summary>
            /// Filled button with solid background.
            /// </summary>
            Filled,

            /// <summary>
            /// Outlined button with transparent background and border.
            /// </summary>
            Outlined,

            /// <summary>
            /// Text button with transparent background.
            /// </summary>
            Text,

            /// <summary>
            /// Elevated button with shadow.
            /// </summary>
            Elevated,

            /// <summary>
            /// Tonal button with secondary container color.
            /// </summary>
            Tonal
        }
    }
}
