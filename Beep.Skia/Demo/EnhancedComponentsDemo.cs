using Beep.Skia.Components;
using SkiaSharp;

namespace Beep.Skia.Demo
{
    /// <summary>
    /// Demo showing the enhanced Button and Label components with leading/trailing icons, error messages, and titles.
    /// </summary>
    public class EnhancedComponentsDemo
    {
        private Button _enhancedButton;
        private Label _enhancedLabel;
        private Panel _demoPanel;
        private Panel _titledPanel;
        private ProgressBar _linearProgress;
        private ProgressBar _circularProgress;
        private TextBox _filledTextBox;
        private TextBox _outlinedTextBox;
        private ComponentManager _componentManager;

        public EnhancedComponentsDemo()
        {
            _componentManager = new ComponentManager();

            // Create a demo panel for our enhanced components
            _demoPanel = new Panel
            {
                Width = 400,
                Height = 300,
                BackgroundColor = SKColors.White,
                BorderColor = SKColors.LightGray,
                BorderWidth = 1,
                CornerRadius = 8
            };

            // Create an enhanced button with leading and trailing icons, title, and error message
            _enhancedButton = new Button("Enhanced Button")
            {
                Width = 200,
                Height = 50,
                LeadingIcon = "<svg width='24' height='24' viewBox='0 0 24 24'><path d='M12 2L13.09 8.26L22 9.27L17.77 13.14L19.18 21.02L12 17.77L4.82 21.02L6.23 13.14L2 9.27L10.91 8.26L12 2Z' fill='#FFD700'/></svg>",
                TrailingIcon = "<svg width='24' height='24' viewBox='0 0 24 24'><path d='M8 5v14l11-7z' fill='#FFFFFF'/></svg>",
                Title = "Action Button",
                ErrorMessage = "This field is required",
                BackgroundColor = SKColors.LightBlue,
                TextColor = SKColors.White
            };
            _enhancedButton.Clicked += OnEnhancedButtonClicked;

            // Create an enhanced label with leading and trailing icons, title, and error message
            _enhancedLabel = new Label("Enhanced Label")
            {
                Width = 200,
                Height = 30,
                LeadingIcon = "<svg width='20' height='20' viewBox='0 0 24 24'><path d='M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z' fill='#4CAF50'/></svg>",
                TrailingIcon = "<svg width='20' height='20' viewBox='0 0 24 24'><path d='M12 8c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zm0 2c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2zm0 6c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2z' fill='#FF9800'/></svg>",
                Title = "Status Label",
                ErrorMessage = "Warning: Check your input",
                Style = Label.LabelStyle.BodyLarge,
                TextAlign = SKTextAlign.Left
            };

            // Create a titled panel to demonstrate the title feature
            _titledPanel = new Panel(300, 200)
            {
                Title = "Demo Panel",
                Variant = Panel.PanelVariant.Outlined,
                CornerRadius = 8,
                BackgroundColor = SKColors.LightGray.WithAlpha(128)
            };

            // Create progress bars to demonstrate the progress control
            _linearProgress = new ProgressBar(250, 8)
            {
                Progress = 0.7f, // 70% progress
                Variant = ProgressBar.ProgressBarVariant.Linear,
                StrokeWidth = 6.0f,
                ProgressColor = SKColors.Green,
                TrackColor = SKColors.LightGray
            };

            _circularProgress = new ProgressBar(60, 60)
            {
                Progress = 0.4f, // 40% progress
                Variant = ProgressBar.ProgressBarVariant.Circular,
                StrokeWidth = 4.0f,
                ProgressColor = SKColors.Blue,
                TrackColor = SKColors.LightGray
            };

            // Create text boxes to demonstrate the text input functionality
            _filledTextBox = new TextBox(250, 48)
            {
                Label = "Filled Text Box",
                Placeholder = "Enter your name...",
                Text = "John Doe",
                LeadingIcon = "<svg width='20' height='20' viewBox='0 0 24 24'><path d='M12 2C13.1 2 14 2.9 14 4C14 5.1 13.1 6 12 6C10.9 6 10 5.1 10 4C10 2.9 10.9 2 12 2ZM21 9V7L15 1H5C3.89 1 3 1.89 3 3V21C3 22.11 3.89 23 5 23H19C20.11 23 21 22.11 21 21V9M19 9H14V4H19V9Z' fill='#666666'/></svg>",
                Variant = TextBox.TextBoxVariant.Filled
            };

            _outlinedTextBox = new TextBox(250, 48)
            {
                Label = "Outlined Text Box",
                Placeholder = "Enter email address...",
                ErrorMessage = "Please enter a valid email",
                TrailingIcon = "<svg width='20' height='20' viewBox='0 0 24 24'><path d='M20 4H4C2.89 4 2 4.89 2 6V18C2 19.11 2.89 20 4 20H20C21.11 20 22 19.11 22 18V6C22 4.89 21.11 4 20 4M20 18H4V8L12 13L20 8V18M20 6L12 11L4 6V6H20V6Z' fill='#666666'/></svg>",
                Variant = TextBox.TextBoxVariant.Outlined
            };

            // Add components to the demo panel
            _demoPanel.AddChild(_enhancedButton, 50, 50);
            _demoPanel.AddChild(_enhancedLabel, 50, 120);

            // Add demo panel to component manager
            _componentManager.AddComponent(_demoPanel);
            _componentManager.AddComponent(_titledPanel);
            _componentManager.AddComponent(_linearProgress);
            _componentManager.AddComponent(_circularProgress);
            _componentManager.AddComponent(_filledTextBox);
            _componentManager.AddComponent(_outlinedTextBox);
        }

        private void OnEnhancedButtonClicked(object sender, EventArgs e)
        {
            // Toggle error message visibility
            if (string.IsNullOrEmpty(_enhancedButton.ErrorMessage))
            {
                _enhancedButton.ErrorMessage = "Button clicked! Error message shown.";
                _enhancedLabel.ErrorMessage = "Label error updated.";

                // Switch progress bars to indeterminate mode
                _linearProgress.IsIndeterminate = true;
                _circularProgress.IsIndeterminate = true;
            }
            else
            {
                _enhancedButton.ErrorMessage = null;
                _enhancedLabel.ErrorMessage = null;

                // Switch progress bars back to determinate mode
                _linearProgress.IsIndeterminate = false;
                _circularProgress.IsIndeterminate = false;
                _linearProgress.Progress = 0.7f;
                _circularProgress.Progress = 0.4f;
            }
        }

        /// <summary>
        /// Draws the enhanced components demo on the specified canvas.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="width">The width of the drawing area.</param>
        /// <param name="height">The height of the drawing area.</param>
        public void Draw(SKCanvas canvas, int width, int height)
        {
            // Clear canvas
            canvas.Clear(SKColors.LightGray);

            // Position the demo panel on the left
            _demoPanel.X = 50;
            _demoPanel.Y = 50;

            // Position the titled panel on the right
            _titledPanel.X = width - _titledPanel.Width - 50;
            _titledPanel.Y = 50;

            // Position the linear progress bar at the bottom
            _linearProgress.X = 50;
            _linearProgress.Y = height - 100;

            // Position the circular progress bar next to the linear one
            _circularProgress.X = _linearProgress.X + _linearProgress.Width + 50;
            _circularProgress.Y = height - _circularProgress.Height - 70;

            // Position the filled text box above the progress bars
            _filledTextBox.X = 50;
            _filledTextBox.Y = height - 200;

            // Position the outlined text box next to the filled one
            _outlinedTextBox.X = _filledTextBox.X + _filledTextBox.Width + 50;
            _outlinedTextBox.Y = height - 200;

            // Render all components
            _componentManager.Render();
        }

        /// <summary>
        /// Handles mouse down events.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        public void HandleMouseDown(SKPoint point)
        {
            _componentManager.HandleMouseDown(point);
        }
    }
}
