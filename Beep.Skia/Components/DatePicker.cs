using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using static Beep.Skia.Components.Button;

namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 Date Picker component that allows users to select dates.
    /// </summary>
    public class DatePicker : MaterialControl
    {
        private DateTime _selectedDate = DateTime.Today;
        private DateTime _displayMonth = DateTime.Today;
        private bool _isOpen = false;
        private string _dateFormat = "MMM dd, yyyy";
        private SKColor _textColor = MaterialColors.OnSurface;
        private SKColor _accentColor = MaterialColors.Primary;
        private float _dialogWidth = 320;
        private float _dialogHeight = 400;
        private bool _showTodayButton = true;
        private bool _allowFutureDates = true;
        private bool _allowPastDates = true;
        private DateTime? _minDate;
        private DateTime? _maxDate;

        // UI Components
        private TextBox _dateTextBox;
        private Button _calendarButton;
        private Panel _calendarDialog;
        private Label _monthYearLabel;
        private Button _prevMonthButton;
        private Button _nextMonthButton;
        private Button _todayButton;
        private Button _cancelButton;
        private Button _okButton;

        // Calendar grid
        private readonly List<Button> _dayButtons = new List<Button>();
        private const int DaysPerWeek = 7;
        private const int MaxWeeks = 6;

        /// <summary>
        /// Occurs when the selected date changes.
        /// </summary>
        public event EventHandler<DateTime> DateSelected;

        /// <summary>
        /// Occurs when the date picker dialog opens.
        /// </summary>
        public event EventHandler DialogOpened;

        /// <summary>
        /// Occurs when the date picker dialog closes.
        /// </summary>
        public event EventHandler DialogClosed;

        /// <summary>
        /// Gets or sets the currently selected date.
        /// </summary>
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (_selectedDate != value)
                {
                    _selectedDate = value;
                    UpdateDateText();
                    DateSelected?.Invoke(this, _selectedDate);
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the date format string.
        /// </summary>
        public string DateFormat
        {
            get => _dateFormat;
            set
            {
                if (_dateFormat != value)
                {
                    _dateFormat = value;
                    UpdateDateText();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the text color.
        /// </summary>
        public SKColor TextColor
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
        /// Gets or sets the accent color for selected dates.
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
        /// Gets or sets whether to show the "Today" button.
        /// </summary>
        public bool ShowTodayButton
        {
            get => _showTodayButton;
            set
            {
                if (_showTodayButton != value)
                {
                    _showTodayButton = value;
                    UpdateDialogLayout();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the minimum selectable date.
        /// </summary>
        public DateTime? MinDate
        {
            get => _minDate;
            set
            {
                if (_minDate != value)
                {
                    _minDate = value;
                    UpdateCalendarGrid();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum selectable date.
        /// </summary>
        public DateTime? MaxDate
        {
            get => _maxDate;
            set
            {
                if (_maxDate != value)
                {
                    _maxDate = value;
                    UpdateCalendarGrid();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatePicker"/> class.
        /// </summary>
        public DatePicker()
        {
            Width = 200;
            Height = 40;
            InitializeComponents();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatePicker"/> class with specified dimensions.
        /// </summary>
        /// <param name="width">The width of the date picker.</param>
        /// <param name="height">The height of the date picker.</param>
        public DatePicker(float width, float height) : this()
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
            _dateTextBox = new TextBox
            {
                Width = Width - 40,
                Height = Height,
                X = 0,
                Y = 0,
                IsReadOnly = true,
                Placeholder = "Select date"
            };
            _dateTextBox.TextChanged += (s, e) => UpdateDateText();

            // Create calendar button
            _calendarButton = new Button
            {
                Width = 32,
                Height = Height,
                X = Width - 32,
                Y = 0,
                Text = "📅",
                Variant = ButtonVariant.Text
            };
            _calendarButton.Clicked += (s, e) => ToggleCalendar();

            // Create calendar dialog
            CreateCalendarDialog();

            UpdateDateText();
        }

        /// <summary>
        /// Creates the calendar dialog component.
        /// </summary>
        private void CreateCalendarDialog()
        {
            _calendarDialog = new Panel
            {
                Width = _dialogWidth,
                Height = _dialogHeight,
                X = 0,
                Y = Height + 8,
                BackgroundColor = MaterialColors.Surface,
                CornerRadius = 12,
                IsVisible = false
            };

            // Header with month/year and navigation
            CreateCalendarHeader();

            // Calendar grid
            CreateCalendarGrid();

            // Action buttons
            CreateActionButtons();
        }

        /// <summary>
        /// Creates the calendar header with month/year and navigation buttons.
        /// </summary>
        private void CreateCalendarHeader()
        {
            // Previous month button
            _prevMonthButton = new Button
            {
                Width = 32,
                Height = 32,
                X = 16,
                Y = 16,
                Text = "‹",
                Variant = ButtonVariant.Text
            };
            _prevMonthButton.Clicked += (s, e) => NavigateMonth(-1);

            // Month/Year label
            _monthYearLabel = new Label
            {
                X = 56,
                Y = 16,
                Width = _dialogWidth - 112,
                Height = 32,
                TextColor = MaterialColors.OnSurface,
                TextAlign = SKTextAlign.Center
            };

            // Next month button
            _nextMonthButton = new Button
            {
                Width = 32,
                Height = 32,
                X = _dialogWidth - 48,
                Y = 16,
                Text = "›",
                Variant = ButtonVariant.Text
            };
            _nextMonthButton.Clicked += (s, e) => NavigateMonth(1);

            _calendarDialog.AddChild(_prevMonthButton);
            _calendarDialog.AddChild(_monthYearLabel);
            _calendarDialog.AddChild(_nextMonthButton);
        }

        /// <summary>
        /// Creates the calendar grid with day buttons.
        /// </summary>
        private void CreateCalendarGrid()
        {
            float gridStartY = 64;
            float cellWidth = (_dialogWidth - 32) / DaysPerWeek;
            float cellHeight = 32;

            // Day headers
            string[] dayNames = { "S", "M", "T", "W", "T", "F", "S" };
            for (int i = 0; i < DaysPerWeek; i++)
            {
                var dayLabel = new Label
                {
                    X = 16 + i * cellWidth,
                    Y = gridStartY,
                    Width = cellWidth,
                    Height = cellHeight,
                    Text = dayNames[i],
                    TextColor = MaterialColors.OnSurfaceVariant,
                    TextAlign = SKTextAlign.Center
                };
                _calendarDialog.AddChild(dayLabel);
            }

            // Day buttons
            for (int week = 0; week < MaxWeeks; week++)
            {
                for (int day = 0; day < DaysPerWeek; day++)
                {
                    var dayButton = new Button
                    {
                        Width = cellWidth - 4,
                        Height = cellHeight - 4,
                        X = 18 + day * cellWidth,
                        Y = gridStartY + 40 + week * cellHeight,
                        Variant = ButtonVariant.Text,
                        CornerRadius = 16
                    };
                    dayButton.Clicked += (s, e) => SelectDay(dayButton);

                    _dayButtons.Add(dayButton);
                    _calendarDialog.AddChild(dayButton);
                }
            }
        }

        /// <summary>
        /// Creates the action buttons (Today, Cancel, OK).
        /// </summary>
        private void CreateActionButtons()
        {
            float buttonY = _dialogHeight - 56;

            if (_showTodayButton)
            {
                _todayButton = new Button
                {
                    Width = 80,
                    Height = 36,
                    X = 16,
                    Y = buttonY,
                    Text = "Today",
                    Variant = ButtonVariant.Text
                };
                _todayButton.Clicked += (s, e) => SelectToday();
                _calendarDialog.AddChild(_todayButton);
            }

            _cancelButton = new Button
            {
                Width = 80,
                Height = 36,
                X = _dialogWidth - 176,
                Y = buttonY,
                Text = "Cancel",
                Variant = ButtonVariant.Text
            };
            _cancelButton.Clicked += (s, e) => CloseCalendar();

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

            _calendarDialog.AddChild(_cancelButton);
            _calendarDialog.AddChild(_okButton);
        }

        /// <summary>
        /// Updates the date text in the input field.
        /// </summary>
        private void UpdateDateText()
        {
            if (_dateTextBox != null)
            {
                _dateTextBox.Text = _selectedDate.ToString(_dateFormat);
            }
        }

        /// <summary>
        /// Updates the colors of UI components.
        /// </summary>
        private void UpdateColors()
        {
            if (_dateTextBox != null)
            {
                _dateTextBox.TextColor = _textColor;
            }
        }

        /// <summary>
        /// Updates the dialog layout.
        /// </summary>
        private void UpdateDialogLayout()
        {
            // Update dialog position relative to the date picker
            if (_calendarDialog != null)
            {
                _calendarDialog.X = X;
                _calendarDialog.Y = Y + Height + 8;
            }
        }

        /// <summary>
        /// Updates the calendar grid with the current month.
        /// </summary>
        private void UpdateCalendarGrid()
        {
            _monthYearLabel.Text = _displayMonth.ToString("MMMM yyyy");

            DateTime firstDayOfMonth = new DateTime(_displayMonth.Year, _displayMonth.Month, 1);
            int startDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
            DateTime currentDate = firstDayOfMonth.AddDays(-startDayOfWeek);

            for (int i = 0; i < _dayButtons.Count; i++)
            {
                var button = _dayButtons[i];
                button.Text = currentDate.Day.ToString();

                // Check if date is in current month
                bool isCurrentMonth = currentDate.Month == _displayMonth.Month;
                bool isToday = currentDate.Date == DateTime.Today.Date;
                bool isSelected = currentDate.Date == _selectedDate.Date;
                bool isEnabled = IsDateEnabled(currentDate);

                button.IsEnabled = isEnabled;

                if (isSelected)
                {
                    button.BackgroundColor = _accentColor;
                    button.TextColor = MaterialColors.OnPrimary;
                    button.Variant = ButtonVariant.Filled;
                }
                else if (isToday)
                {
                    button.BackgroundColor = MaterialColors.PrimaryContainer;
                    button.TextColor = MaterialColors.OnPrimaryContainer;
                    button.Variant = ButtonVariant.Filled;
                }
                else if (isCurrentMonth)
                {
                    button.BackgroundColor = SKColors.Transparent;
                    button.TextColor = MaterialColors.OnSurface;
                    button.Variant = ButtonVariant.Text;
                }
                else
                {
                    button.BackgroundColor = SKColors.Transparent;
                    button.TextColor = MaterialColors.OnSurfaceVariant;
                    button.Variant = ButtonVariant.Text;
                }

                // Store the date in the button's tag
                button.Tag = currentDate;
                currentDate = currentDate.AddDays(1);
            }
        }

        /// <summary>
        /// Checks if a date is enabled for selection.
        /// </summary>
        private bool IsDateEnabled(DateTime date)
        {
            if (_minDate.HasValue && date < _minDate.Value.Date)
                return false;
            if (_maxDate.HasValue && date > _maxDate.Value.Date)
                return false;
            if (!_allowPastDates && date < DateTime.Today.Date)
                return false;
            if (!_allowFutureDates && date > DateTime.Today.Date)
                return false;
            return true;
        }

        /// <summary>
        /// Toggles the calendar dialog visibility.
        /// </summary>
        public void ToggleCalendar()
        {
            if (_isOpen)
            {
                CloseCalendar();
            }
            else
            {
                OpenCalendar();
            }
        }

        /// <summary>
        /// Opens the calendar dialog.
        /// </summary>
        public void OpenCalendar()
        {
            if (!_isOpen)
            {
                _isOpen = true;
                _calendarDialog.IsVisible = true;
                UpdateCalendarGrid();
                DialogOpened?.Invoke(this, EventArgs.Empty);
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Closes the calendar dialog.
        /// </summary>
        public void CloseCalendar()
        {
            if (_isOpen)
            {
                _isOpen = false;
                _calendarDialog.IsVisible = false;
                DialogClosed?.Invoke(this, EventArgs.Empty);
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Navigates to the previous or next month.
        /// </summary>
        private void NavigateMonth(int direction)
        {
            _displayMonth = _displayMonth.AddMonths(direction);
            UpdateCalendarGrid();
        }

        /// <summary>
        /// Selects today's date.
        /// </summary>
        private void SelectToday()
        {
            _selectedDate = DateTime.Today;
            _displayMonth = DateTime.Today;
            UpdateCalendarGrid();
        }

        /// <summary>
        /// Selects a day from the calendar grid.
        /// </summary>
        private void SelectDay(Button dayButton)
        {
            if (dayButton.Tag is DateTime selectedDate && IsDateEnabled(selectedDate))
            {
                _selectedDate = selectedDate;
                UpdateCalendarGrid();
            }
        }

        /// <summary>
        /// Confirms the current selection and closes the dialog.
        /// </summary>
        private void ConfirmSelection()
        {
            UpdateDateText();
            DateSelected?.Invoke(this, _selectedDate);
            CloseCalendar();
        }

        /// <summary>
        /// Draws the date picker component.
        /// </summary>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Draw input field and calendar button
            if (_dateTextBox != null)
            {
                _dateTextBox.X = X;
                _dateTextBox.Y = Y;
                _dateTextBox.Width = Width - 40;
                _dateTextBox.Draw(canvas, context);
            }

            if (_calendarButton != null)
            {
                _calendarButton.X = X + Width - 32;
                _calendarButton.Y = Y;
                _calendarButton.Draw(canvas, context);
            }

            // Draw calendar dialog if open
            if (_isOpen && _calendarDialog != null)
            {
                _calendarDialog.Draw(canvas, context);
            }
        }

        /// <summary>
        /// Handles mouse down events.
        /// </summary>
        protected override bool OnMouseDown(SKPoint location, InteractionContext context)
        {
            // Check if click is outside the dialog to close it
            if (_isOpen && _calendarDialog != null)
            {
                var dialogBounds = new SKRect(
                    _calendarDialog.X,
                    _calendarDialog.Y,
                    _calendarDialog.X + _calendarDialog.Width,
                    _calendarDialog.Y + _calendarDialog.Height);

                if (!dialogBounds.Contains(location))
                {
                    CloseCalendar();
                    return true;
                }
            }

            return base.OnMouseDown(location, context);
        }
    }
}
