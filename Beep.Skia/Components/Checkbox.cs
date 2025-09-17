using SkiaSharp;
using System;
using Beep.Skia.Model;
namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 Checkbox component that allows users to select one or more items from a list, or turn an item on or off.
    /// </summary>
    public class Checkbox : MaterialControl
    {
        private CheckState _checkState = CheckState.Unchecked;
        private bool _isEnabled = true;

        /// <summary>
        /// Gets or sets the current check state of the checkbox.
        /// </summary>
        public CheckState CheckState
        {
            get => _checkState;
            set
            {
                if (_checkState != value)
                {
                    _checkState = value;
                    InvalidateVisual();
                    CheckStateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the checkbox is enabled.
        /// </summary>
        public new bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    State = _isEnabled ? ControlState.Normal : ControlState.Disabled;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the checkbox is checked (convenience property).
        /// </summary>
        public bool IsChecked
        {
            get => _checkState == CheckState.Checked;
            set => CheckState = value ? CheckState.Checked : CheckState.Unchecked;
        }

        /// <summary>
        /// Gets or sets whether the checkbox is indeterminate.
        /// </summary>
        public bool IsIndeterminate
        {
            get => _checkState == CheckState.Indeterminate;
            set => CheckState = value ? CheckState.Indeterminate : CheckState.Unchecked;
        }

        /// <summary>
        /// Occurs when the check state of the checkbox changes.
        /// </summary>
        public event EventHandler<EventArgs> CheckStateChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="Checkbox"/> class.
        /// </summary>
        public Checkbox()
        {
            Width = 40; // 40dp touch target as per Material Design specs
            Height = 40; // 40dp touch target as per Material Design specs
            Name = "Checkbox";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Checkbox"/> class with the specified initial state.
        /// </summary>
        /// <param name="initialState">The initial check state of the checkbox.</param>
        public Checkbox(CheckState initialState) : this()
        {
            _checkState = initialState;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Checkbox"/> class with the specified initial checked state.
        /// </summary>
        /// <param name="isChecked">Whether the checkbox should be initially checked.</param>
        public Checkbox(bool isChecked) : this()
        {
            _checkState = isChecked ? CheckState.Checked : CheckState.Unchecked;
        }

        /// <summary>
        /// Draws the checkbox component.
        /// </summary>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Get the component bounds
            var bounds = new SKRect(X, Y, X + Width, Y + Height);

            // Draw state layer first (behind everything)
            DrawStateLayer(canvas, bounds, GetStateLayerColor());

            // Calculate the checkbox container bounds (18dp x 18dp, centered)
            float containerSize = 18; // 18dp as per Material Design specs
            float containerX = X + (Width - containerSize) / 2;
            float containerY = Y + (Height - containerSize) / 2;
            var containerBounds = new SKRect(containerX, containerY, containerX + containerSize, containerY + containerSize);

            // Draw the checkbox container (outline or filled based on state)
            DrawCheckboxContainer(canvas, containerBounds);

            // Draw the checkmark or dash icon
            DrawCheckboxIcon(canvas, containerBounds);
        }

        /// <summary>
        /// Gets the appropriate state layer color based on the current state.
        /// </summary>
        private SKColor GetStateLayerColor()
        {
            if (!_isEnabled)
                return MaterialColors.OnSurface.WithAlpha(0x0C); // 12% opacity for disabled

            switch (_checkState)
            {
                case CheckState.Checked:
                    return MaterialColors.Primary;
                case CheckState.Indeterminate:
                    return MaterialColors.Primary;
                default:
                    return MaterialColors.OnSurface;
            }
        }

        /// <summary>
        /// Draws the checkbox container (outline or filled background).
        /// </summary>
        private void DrawCheckboxContainer(SKCanvas canvas, SKRect bounds)
        {
            SKColor containerColor;
            SKColor borderColor;
            float borderWidth = 2; // 2dp border as per Material Design specs

            if (!_isEnabled)
            {
                // Disabled state
                containerColor = SKColors.Transparent;
                borderColor = MaterialColors.OnSurface.WithAlpha(0x26); // 38% opacity
            }
            else if (_checkState == CheckState.Checked || _checkState == CheckState.Indeterminate)
            {
                // Checked or indeterminate state - filled container
                containerColor = GetStateAwareColor(
                    MaterialColors.Primary,
                    MaterialColors.Primary,
                    MaterialColors.Primary);
                borderColor = containerColor;
            }
            else
            {
                // Unchecked state - outlined container
                containerColor = SKColors.Transparent;
                borderColor = GetStateAwareColor(
                    MaterialColors.OnSurfaceVariant,
                    MaterialColors.OnSurface,
                    MaterialColors.OnSurface);
            }

            // Draw container background if filled
            if (containerColor != SKColors.Transparent)
            {
                using (var fillPaint = new SKPaint
                {
                    Color = containerColor,
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                })
                {
                    // Draw rounded rectangle with corner radius of 2dp
                    canvas.DrawRoundRect(bounds, 2, 2, fillPaint);
                }
            }

            // Draw container border
            using (var borderPaint = new SKPaint
            {
                Color = borderColor,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = borderWidth,
                IsAntialias = true
            })
            {
                // Draw rounded rectangle with corner radius of 2dp
                canvas.DrawRoundRect(bounds, 2, 2, borderPaint);
            }
        }

        /// <summary>
        /// Draws the checkmark or dash icon inside the checkbox.
        /// </summary>
        private void DrawCheckboxIcon(SKCanvas canvas, SKRect containerBounds)
        {
            if (_checkState == CheckState.Unchecked)
                return; // No icon for unchecked state

            SKColor iconColor;
            if (!_isEnabled)
            {
                iconColor = MaterialColors.OnSurface.WithAlpha(0x26); // 38% opacity for disabled
            }
            else
            {
                iconColor = GetStateAwareColor(
                    MaterialColors.OnPrimary,
                    MaterialColors.OnPrimary,
                    MaterialColors.OnPrimary);
            }

            using (var iconPaint = new SKPaint
            {
                Color = iconColor,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2, // 2dp stroke width as per Material Design specs
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round,
                IsAntialias = true
            })
            {
                if (_checkState == CheckState.Checked)
                {
                    // Draw checkmark
                    DrawCheckmark(canvas, containerBounds, iconPaint);
                }
                else if (_checkState == CheckState.Indeterminate)
                {
                    // Draw dash
                    DrawDash(canvas, containerBounds, iconPaint);
                }
            }
        }

        /// <summary>
        /// Draws the checkmark icon.
        /// </summary>
        private void DrawCheckmark(SKCanvas canvas, SKRect bounds, SKPaint paint)
        {
            // Calculate checkmark path points (scaled to fit within the container)
            float padding = 4; // Padding from container edges
            float left = bounds.Left + padding;
            float top = bounds.Top + padding;
            float right = bounds.Right - padding;
            float bottom = bounds.Bottom - padding;

            // Checkmark path: start at bottom-left, go to middle-right, then to top-right
            using (var path = new SKPath())
            {
                path.MoveTo(left + 2, top + 6); // Start point (bottom-left of checkmark)
                path.LineTo(left + 4, top + 8); // Middle point
                path.LineTo(left + 8, top + 4); // End point (top-right of checkmark)

                canvas.DrawPath(path, paint);
            }
        }

        /// <summary>
        /// Draws the dash icon for indeterminate state.
        /// </summary>
        private void DrawDash(SKCanvas canvas, SKRect bounds, SKPaint paint)
        {
            // Calculate dash position (horizontal line in the middle)
            float padding = 4; // Padding from container edges
            float left = bounds.Left + padding;
            float right = bounds.Right - padding;
            float centerY = bounds.Top + bounds.Height / 2;

            // Draw horizontal dash
            canvas.DrawLine(left, centerY, right, centerY, paint);
        }

        /// <summary>
        /// Handles mouse enter events to update the control state.
        /// </summary>
        protected override void OnMouseEnter()
        {
            if (_isEnabled)
            {
                State = ControlState.Hovered;
            }
        }

        /// <summary>
        /// Handles mouse leave events to update the control state.
        /// </summary>
        protected override void OnMouseLeave()
        {
            if (_isEnabled)
            {
                State = ControlState.Normal;
            }
        }

        /// <summary>
        /// Handles mouse down events.
        /// </summary>
        protected override bool OnMouseDown(SKPoint location, InteractionContext context)
        {
            if (_isEnabled)
            {
                State = ControlState.Pressed;
            }
            return base.OnMouseDown(location, context);
        }

        /// <summary>
        /// Handles mouse up events and toggles the checkbox state.
        /// </summary>
        protected override bool OnMouseUp(SKPoint location, InteractionContext context)
        {
            if (_isEnabled && State == ControlState.Pressed)
            {
                // Toggle the checkbox state
                ToggleState();
                CheckStateChanged?.Invoke(this, EventArgs.Empty);
            }

            if (_isEnabled)
            {
                State = ControlState.Normal;
            }

            return base.OnMouseUp(location, context);
        }

        /// <summary>
        /// Toggles the checkbox between checked and unchecked states.
        /// </summary>
        public void ToggleState()
        {
            if (!_isEnabled)
                return;

            // Cycle through states: Unchecked -> Checked -> Indeterminate (if supported) -> Unchecked
            switch (_checkState)
            {
                case CheckState.Unchecked:
                    CheckState = CheckState.Checked;
                    break;
                case CheckState.Checked:
                    CheckState = CheckState.Unchecked; // Skip indeterminate for now, can be enabled later
                    break;
                case CheckState.Indeterminate:
                    CheckState = CheckState.Unchecked;
                    break;
            }
        }

        /// <summary>
        /// Sets the checkbox to the next state in the cycle.
        /// </summary>
        public void NextState()
        {
            ToggleState();
        }

        /// <summary>
        /// Sets the checkbox to the previous state in the cycle.
        /// </summary>
        public void PreviousState()
        {
            if (!_isEnabled)
                return;

            // Cycle backwards: Unchecked <- Checked <- Indeterminate (if supported) <- Unchecked
            switch (_checkState)
            {
                case CheckState.Unchecked:
                    CheckState = CheckState.Indeterminate; // Skip indeterminate for now, can be enabled later
                    break;
                case CheckState.Checked:
                    CheckState = CheckState.Unchecked;
                    break;
                case CheckState.Indeterminate:
                    CheckState = CheckState.Checked;
                    break;
            }
        }
    }

    /// <summary>
    /// Specifies the check state of a checkbox.
    /// </summary>
    public enum CheckState
    {
        /// <summary>
        /// The checkbox is unchecked.
        /// </summary>
        Unchecked,

        /// <summary>
        /// The checkbox is checked.
        /// </summary>
        Checked,

        /// <summary>
        /// The checkbox is in an indeterminate state (partially checked).
        /// </summary>
        Indeterminate
    }
}
