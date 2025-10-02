using SkiaSharp;
using System;
using System.Collections.Generic;
using static Beep.Skia.Components.Button;
using Beep.Skia.Model;
namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 Time Picker component that allows users to select times.
    /// </summary>
    public class TimePicker : MaterialControl
    {
        private DateTime _selectedTime = DateTime.Now;
        private bool _isOpen = false;
        private bool _use24HourFormat = false;
        private SKColor _textColor = MaterialColors.OnSurface;
        private SKColor _accentColor = MaterialColors.Primary;
        private float _dialogWidth = 280;
        private float _dialogHeight = 400;
        private TimePickerMode _mode = TimePickerMode.Clock;

        // UI Components
        private TextBox _timeTextBox;
        private Button _clockButton;
        private Panel _timeDialog;
        private Label _timeDisplayLabel;
        private Button _hourButton;
        private Button _minuteButton;
        private Button _ampmButton;
        private Button _cancelButton;
        private Button _okButton;

        // Clock face components
        private readonly List<Button> _hourButtons = new List<Button>();
        private readonly List<Button> _minuteButtons = new List<Button>();
        private bool _isHourMode = true; // true for hour selection, false for minute selection

        /// <summary>
        /// Time picker display modes.
        /// </summary>
        public enum TimePickerMode
        {
            Clock,    // Analog clock face
            Spinner   // Digital input spinners
        }

        /// <summary>
        /// Occurs when the selected time changes.
        /// </summary>
        public event EventHandler<DateTime> TimeSelected;

        /// <summary>
        /// Occurs when the time picker dialog opens.
        /// </summary>
        public event EventHandler DialogOpened;

        /// <summary>
        /// Occurs when the time picker dialog closes.
        /// </summary>
        public event EventHandler DialogClosed;

        /// <summary>
        /// Gets or sets the currently selected time.
        /// </summary>
        public DateTime SelectedTime
        {
            get => _selectedTime;
            set
            {
                if (_selectedTime != value)
                {
                    _selectedTime = value;
                    UpdateTimeText();
                    TimeSelected?.Invoke(this, _selectedTime);
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to use 24-hour format.
        /// </summary>
        public bool Use24HourFormat
        {
            get => _use24HourFormat;
            set
            {
                if (_use24HourFormat != value)
                {
                    _use24HourFormat = value;
                    UpdateTimeText();
                    UpdateClockFace();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the text color.
        /// </summary>
        public new SKColor TextColor
        {
            get => _textColor;
            set
            {
                if (_textColor != value)
                {
                    _textColor = value;
                    UpdateColors();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the accent color for selected times.
        /// </summary>
        public SKColor AccentColor
        {
            get => _accentColor;
            set
            {
                if (_accentColor != value)
                {
                    _accentColor = value;
                    UpdateColors();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the time picker mode.
        /// </summary>
        public TimePickerMode Mode
        {
            get => _mode;
            set
            {
                if (_mode != value)
                {
                    _mode = value;
                    UpdateDialogLayout();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimePicker"/> class.
        /// </summary>
        public TimePicker()
        {
            Width = 150;
            Height = 40;
            InitializeComponents();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimePicker"/> class with specified dimensions.
        /// </summary>
        /// <param name="width">The width of the time picker.</param>
        /// <param name="height">The height of the time picker.</param>
        public TimePicker(float width, float height) : this()
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Initializes the UI components.
        /// </summary>
        private void InitializeComponents()
        {
            // Create input field
            _timeTextBox = new TextBox
            {
                Width = Width - 40,
                Height = Height,
                X = 0,
                Y = 0,
                IsReadOnly = true,
                Placeholder = "Select time"
            };

            // Create clock button
            _clockButton = new Button
            {
                Width = 32,
                Height = Height,
                X = Width - 32,
                Y = 0,
                Text = "🕐",
                Variant = ButtonVariant.Text
            };
            _clockButton.Clicked += (s, e) => ToggleTimePicker();

            // Create time dialog
            CreateTimeDialog();

            UpdateTimeText();
        }

        /// <summary>
        /// Creates the time picker dialog component.
        /// </summary>
        private void CreateTimeDialog()
        {
            _timeDialog = new Panel
            {
                Width = _dialogWidth,
                Height = _dialogHeight,
                X = 0,
                Y = Height + 8,
                BackgroundColor = MaterialColors.Surface,
                CornerRadius = 12,
                IsVisible = false
            };

            // Time display
            CreateTimeDisplay();

            // Mode selector buttons
            CreateModeButtons();

            // Clock face or spinner input
            if (_mode == TimePickerMode.Clock)
            {
                CreateClockFace();
            }
            else
            {
                CreateSpinnerInput();
            }

            // Action buttons
            CreateActionButtons();
        }

        /// <summary>
        /// Creates the time display component.
        /// </summary>
        private void CreateTimeDisplay()
        {
            _timeDisplayLabel = new Label
            {
                X = 20,
                Y = 20,
                Width = _dialogWidth - 40,
                Height = 60,
                TextColor = MaterialColors.OnSurface,
                TextAlign = SKTextAlign.Center,
                FontSize = 32
            };

            _timeDialog.AddChild(_timeDisplayLabel);
        }

        /// <summary>
        /// Creates the mode selector buttons (Hour/Minute).
        /// </summary>
        private void CreateModeButtons()
        {
            _hourButton = new Button
            {
                Width = 60,
                Height = 36,
                X = (_dialogWidth - 140) / 2,
                Y = 90,
                Text = "Hour",
                Variant = ButtonVariant.Text
            };
            _hourButton.Clicked += (s, e) => SetMode(true);

            _minuteButton = new Button
            {
                Width = 60,
                Height = 36,
                X = (_dialogWidth - 140) / 2 + 80,
                Y = 90,
                Text = "Minute",
                Variant = ButtonVariant.Text
            };
            _minuteButton.Clicked += (s, e) => SetMode(false);

            _timeDialog.AddChild(_hourButton);
            _timeDialog.AddChild(_minuteButton);

            // AM/PM button for 12-hour format
            if (!_use24HourFormat)
            {
                _ampmButton = new Button
                {
                    Width = 60,
                    Height = 36,
                    X = _dialogWidth - 80,
                    Y = 90,
                    Text = "AM",
                    Variant = ButtonVariant.Text
                };
                _ampmButton.Clicked += (s, e) => ToggleAmPm();
                _timeDialog.AddChild(_ampmButton);
            }
        }

        /// <summary>
        /// Creates the clock face for time selection.
        /// </summary>
        private void CreateClockFace()
        {
            float centerX = _dialogWidth / 2;
            float centerY = 200;
            float radius = 80;

            // Create hour buttons (1-12)
            for (int hour = 1; hour <= 12; hour++)
            {
                double angle = (hour - 3) * 30 * Math.PI / 180; // Start from 12 o'clock
                float x = centerX + (float)(Math.Cos(angle) * radius) - 20;
                float y = centerY + (float)(Math.Sin(angle) * radius) - 20;

                var hourButton = new Button
                {
                    Width = 40,
                    Height = 40,
                    X = x,
                    Y = y,
                    Text = hour.ToString(),
                    Variant = ButtonVariant.Text,
                    CornerRadius = 20
                };
                hourButton.Clicked += (s, e) => SelectHour(hour);

                _hourButtons.Add(hourButton);
                _timeDialog.AddChild(hourButton);
            }

            // Create minute buttons (00, 15, 30, 45)
            int[] minutes = { 0, 15, 30, 45 };
            for (int i = 0; i < minutes.Length; i++)
            {
                double angle = (i * 90 - 90) * Math.PI / 180; // 90-degree intervals
                float x = centerX + (float)(Math.Cos(angle) * (radius - 30)) - 20;
                float y = centerY + (float)(Math.Sin(angle) * (radius - 30)) - 20;

                var minuteButton = new Button
                {
                    Width = 40,
                    Height = 40,
                    X = x,
                    Y = y,
                    Text = minutes[i].ToString("00"),
                    Variant = ButtonVariant.Text,
                    CornerRadius = 20
                };
                minuteButton.Clicked += (s, e) => SelectMinute(minutes[i]);

                _minuteButtons.Add(minuteButton);
                _timeDialog.AddChild(minuteButton);
            }
        }

        /// <summary>
        /// Creates spinner input for digital time entry.
        /// </summary>
        private void CreateSpinnerInput()
        {
            // For now, create a simple text input area
            // In a full implementation, this would have up/down spinners
            var timeInputLabel = new Label
            {
                X = 20,
                Y = 140,
                Width = _dialogWidth - 40,
                Height = 40,
                Text = "Digital input (placeholder)",
                TextColor = MaterialColors.OnSurfaceVariant,
                TextAlign = SKTextAlign.Center
            };

            _timeDialog.AddChild(timeInputLabel);
        }

        /// <summary>
        /// Creates the action buttons (Cancel, OK).
        /// </summary>
        private void CreateActionButtons()
        {
            float buttonY = _dialogHeight - 56;

            _cancelButton = new Button
            {
                Width = 80,
                Height = 36,
                X = _dialogWidth - 176,
                Y = buttonY,
                Text = "Cancel",
                Variant = ButtonVariant.Text
            };
            _cancelButton.Clicked += (s, e) => CloseTimePicker();

            _okButton = new Button
            {
                Width = 80,
                Height = 36,
                X = _dialogWidth - 88,
                Y = buttonY,
                Text = "OK",
                Variant = ButtonVariant.Filled
            };
            _okButton.Clicked += (s, e) => ConfirmSelection();

            _timeDialog.AddChild(_cancelButton);
            _timeDialog.AddChild(_okButton);
        }

        /// <summary>
        /// Updates the time text in the input field.
        /// </summary>
        private void UpdateTimeText()
        {
            if (_timeTextBox != null)
            {
                string format = _use24HourFormat ? "HH:mm" : "hh:mm tt";
                _timeTextBox.Text = _selectedTime.ToString(format);
            }

            if (_timeDisplayLabel != null)
            {
                string format = _use24HourFormat ? "HH:mm" : "hh:mm";
                _timeDisplayLabel.Text = _selectedTime.ToString(format);
            }
        }

        /// <summary>
        /// Updates the colors of UI components.
        /// </summary>
        private void UpdateColors()
        {
            if (_timeTextBox != null)
            {
                _timeTextBox.TextColor = _textColor;
            }
        }

        /// <summary>
        /// Updates the dialog layout.
        /// </summary>
        private void UpdateDialogLayout()
        {
            // Recreate dialog with new mode
            _timeDialog?.Children.Clear();
            CreateTimeDialog();
        }

        /// <summary>
        /// Updates the clock face display.
        /// </summary>
        private void UpdateClockFace()
        {
            // Update hour/minute button states
            if (_hourButton != null)
            {
                _hourButton.BackgroundColor = _isHourMode ? _accentColor : SKColors.Transparent;
                _hourButton.TextColor = _isHourMode ? MaterialColors.OnPrimary : MaterialColors.OnSurface;
            }

            if (_minuteButton != null)
            {
                _minuteButton.BackgroundColor = !_isHourMode ? _accentColor : SKColors.Transparent;
                _minuteButton.TextColor = !_isHourMode ? MaterialColors.OnPrimary : MaterialColors.OnSurface;
            }

            // Update AM/PM button
            if (_ampmButton != null)
            {
                _ampmButton.Text = _selectedTime.Hour >= 12 ? "PM" : "AM";
            }

            // Update selected hour/minute buttons
            foreach (var button in _hourButtons)
            {
                int hour = int.Parse(button.Text);
                bool isSelected = _isHourMode && ((_use24HourFormat ? _selectedTime.Hour % 12 : _selectedTime.Hour) == hour || (_selectedTime.Hour == 0 && hour == 12));
                button.BackgroundColor = isSelected ? _accentColor : SKColors.Transparent;
                button.TextColor = isSelected ? MaterialColors.OnPrimary : MaterialColors.OnSurface;
            }

            foreach (var button in _minuteButtons)
            {
                int minute = int.Parse(button.Text);
                bool isSelected = !_isHourMode && _selectedTime.Minute == minute;
                button.BackgroundColor = isSelected ? _accentColor : SKColors.Transparent;
                button.TextColor = isSelected ? MaterialColors.OnPrimary : MaterialColors.OnSurface;
            }
        }

        /// <summary>
        /// Toggles the time picker dialog visibility.
        /// </summary>
        public void ToggleTimePicker()
        {
            if (_isOpen)
            {
                CloseTimePicker();
            }
            else
            {
                OpenTimePicker();
            }
        }

        /// <summary>
        /// Opens the time picker dialog.
        /// </summary>
        public void OpenTimePicker()
        {
            if (!_isOpen)
            {
                _isOpen = true;
                _timeDialog.IsVisible = true;
                UpdateClockFace();
                DialogOpened?.Invoke(this, EventArgs.Empty);
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Closes the time picker dialog.
        /// </summary>
        public void CloseTimePicker()
        {
            if (_isOpen)
            {
                _isOpen = false;
                _timeDialog.IsVisible = false;
                DialogClosed?.Invoke(this, EventArgs.Empty);
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Sets the selection mode (hour or minute).
        /// </summary>
        private void SetMode(bool isHourMode)
        {
            _isHourMode = isHourMode;
            UpdateClockFace();
        }

        /// <summary>
        /// Toggles between AM and PM.
        /// </summary>
        private void ToggleAmPm()
        {
            int hour = _selectedTime.Hour;
            if (hour >= 12)
            {
                hour -= 12;
            }
            else
            {
                hour += 12;
            }
            _selectedTime = new DateTime(_selectedTime.Year, _selectedTime.Month, _selectedTime.Day, hour, _selectedTime.Minute, 0);
            UpdateTimeText();
            UpdateClockFace();
        }

        /// <summary>
        /// Selects an hour from the clock face.
        /// </summary>
        private void SelectHour(int hour)
        {
            if (_use24HourFormat)
            {
                // For 24-hour format, use the hour directly
                _selectedTime = new DateTime(_selectedTime.Year, _selectedTime.Month, _selectedTime.Day, hour, _selectedTime.Minute, 0);
            }
            else
            {
                // For 12-hour format, adjust based on AM/PM
                int adjustedHour = hour;
                if (_selectedTime.Hour >= 12 && hour != 12)
                {
                    adjustedHour += 12;
                }
                else if (_selectedTime.Hour < 12 && hour == 12)
                {
                    adjustedHour = 0;
                }
                _selectedTime = new DateTime(_selectedTime.Year, _selectedTime.Month, _selectedTime.Day, adjustedHour, _selectedTime.Minute, 0);
            }

            UpdateTimeText();
            UpdateClockFace();
        }

        /// <summary>
        /// Selects a minute from the clock face.
        /// </summary>
        private void SelectMinute(int minute)
        {
            _selectedTime = new DateTime(_selectedTime.Year, _selectedTime.Month, _selectedTime.Day, _selectedTime.Hour, minute, 0);
            UpdateTimeText();
            UpdateClockFace();
        }

        /// <summary>
        /// Confirms the current selection and closes the dialog.
        /// </summary>
        private void ConfirmSelection()
        {
            UpdateTimeText();
            TimeSelected?.Invoke(this, _selectedTime);
            CloseTimePicker();
        }

        /// <summary>
        /// Draws the time picker component.
        /// </summary>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Draw input field and clock button
            if (_timeTextBox != null)
            {
                _timeTextBox.X = X;
                _timeTextBox.Y = Y;
                _timeTextBox.Width = Width - 40;
                _timeTextBox.Draw(canvas, context);
            }

            if (_clockButton != null)
            {
                _clockButton.X = X + Width - 32;
                _clockButton.Y = Y;
                _clockButton.Draw(canvas, context);
            }

            // Draw time dialog if open
            if (_isOpen && _timeDialog != null)
            {
                // Reposition dialog relative to current absolute control location
                _timeDialog.X = X;
                _timeDialog.Y = Y + Height + 8;
                _timeDialog.Draw(canvas, context);
            }
        }

        /// <summary>
        /// Handles mouse down events.
        /// </summary>
        protected override bool OnMouseDown(SKPoint location, InteractionContext context)
        {
            // Check if click is outside the dialog to close it
            if (_isOpen && _timeDialog != null)
            {
                var dialogBounds = new SKRect(
                    _timeDialog.X,
                    _timeDialog.Y,
                    _timeDialog.X + _timeDialog.Width,
                    _timeDialog.Y + _timeDialog.Height);

                if (!dialogBounds.Contains(location))
                {
                    CloseTimePicker();
                    return true;
                }
            }

            return base.OnMouseDown(location, context);
        }
    }
}
