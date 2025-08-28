using Beep.Skia.Components;
using SkiaSharp;

namespace Beep.Skia.Demo
{
    /// <summary>
    /// Demo showcasing Material Design 3.0 components: ButtonGroup, SegmentedButtons, SplitButton, and FloatingActionButton.
    /// </summary>
    public class MaterialDesign3Demo
    {
        private ButtonGroup _horizontalButtonGroup;
        private ButtonGroup _verticalButtonGroup;
        private ButtonGroup _gridButtonGroup;
        private SegmentedButtons _singleSelectSegmented;
        private SegmentedButtons _multiSelectSegmented;
        private SplitButton _primaryActionSplitButton;
        private SplitButton _iconSplitButton;
        private FloatingActionButton _primaryFab;
        private FloatingActionButton _secondaryFab;
        private FloatingActionButton _extendedFab;
        private Panel _demoPanel;
        private ComponentManager _componentManager;
        private Label _statusLabel;

        public MaterialDesign3Demo()
        {
            _componentManager = new ComponentManager();

            // Create main demo panel
            _demoPanel = new Panel
            {
                Width = 800,
                Height = 600,
                BackgroundColor = SKColors.White,
                BorderColor = SKColors.LightGray,
                BorderWidth = 1,
                CornerRadius = 12,
                Title = "Material Design 3.0 Components Demo"
            };

            // Create status label for showing interactions
            _statusLabel = new Label("Click components to see interactions")
            {
                Width = 400,
                Height = 30,
                Style = Label.LabelStyle.BodyMedium,
                TextColor = SKColors.DarkGray,
                TextAlign = SKTextAlign.Center
            };

            InitializeButtonGroups();
            InitializeSegmentedButtons();
            InitializeSplitButtons();
            InitializeFloatingActionButtons();

            // Add components to the demo panel
            AddComponentsToPanel();

            // Add demo panel to component manager
            _componentManager.AddComponent(_demoPanel);
            _componentManager.AddComponent(_statusLabel);
        }

        private void InitializeButtonGroups()
        {
            // Horizontal Button Group
            _horizontalButtonGroup = new ButtonGroup
            {
                Layout = ButtonGroup.ButtonGroupLayout.Horizontal,
                Width = 300,
                Height = 48
            };

            // Add buttons to horizontal group
            var btn1 = new Button("Button 1") { Width = 80, Height = 40 };
            var btn2 = new Button("Button 2") { Width = 80, Height = 40 };
            var btn3 = new Button("Button 3") { Width = 80, Height = 40 };

            btn1.Clicked += (s, e) => UpdateStatus("Horizontal Button 1 clicked");
            btn2.Clicked += (s, e) => UpdateStatus("Horizontal Button 2 clicked");
            btn3.Clicked += (s, e) => UpdateStatus("Horizontal Button 3 clicked");

            _horizontalButtonGroup.AddButton(btn1);
            _horizontalButtonGroup.AddButton(btn2);
            _horizontalButtonGroup.AddButton(btn3);

            // Vertical Button Group
            _verticalButtonGroup = new ButtonGroup
            {
                Layout = ButtonGroup.ButtonGroupLayout.Vertical,
                Width = 120,
                Height = 150
            };

            var vbtn1 = new Button("Vertical 1") { Width = 100, Height = 35 };
            var vbtn2 = new Button("Vertical 2") { Width = 100, Height = 35 };
            var vbtn3 = new Button("Vertical 3") { Width = 100, Height = 35 };

            vbtn1.Clicked += (s, e) => UpdateStatus("Vertical Button 1 clicked");
            vbtn2.Clicked += (s, e) => UpdateStatus("Vertical Button 2 clicked");
            vbtn3.Clicked += (s, e) => UpdateStatus("Vertical Button 3 clicked");

            _verticalButtonGroup.AddButton(vbtn1);
            _verticalButtonGroup.AddButton(vbtn2);
            _verticalButtonGroup.AddButton(vbtn3);

            // Grid Button Group
            _gridButtonGroup = new ButtonGroup
            {
                Layout = ButtonGroup.ButtonGroupLayout.Grid,
                Width = 240,
                Height = 120
                // Note: Grid layout automatically arranges buttons in a grid
            };

            for (int i = 1; i <= 6; i++)
            {
                var gridBtn = new Button($"Grid {i}") { Width = 70, Height = 35 };
                int buttonIndex = i; // Capture for lambda
                gridBtn.Clicked += (s, e) => UpdateStatus($"Grid Button {buttonIndex} clicked");
                _gridButtonGroup.AddButton(gridBtn);
            }
        }

        private void InitializeSegmentedButtons()
        {
            // Single-select Segmented Buttons
            _singleSelectSegmented = new SegmentedButtons
            {
                Width = 300,
                Height = 48,
                SelectionMode = SegmentedButtons.SegmentedButtonSelectionMode.Single
            };

            _singleSelectSegmented.AddSegment("Day");
            _singleSelectSegmented.AddSegment("Week");
            _singleSelectSegmented.AddSegment("Month");
            _singleSelectSegmented.AddSegment("Year");

            _singleSelectSegmented.SelectionChanged += (s, e) =>
            {
                var selectedIndices = _singleSelectSegmented.SelectedIndices.ToArray();
                string selectedText = selectedIndices.Length > 0
                    ? _singleSelectSegmented.Items[selectedIndices[0]].Text
                    : "None";
                UpdateStatus($"Single-select: {selectedText}");
            };

            // Multi-select Segmented Buttons
            _multiSelectSegmented = new SegmentedButtons
            {
                Width = 400,
                Height = 48,
                SelectionMode = SegmentedButtons.SegmentedButtonSelectionMode.Multiple
            };

            _multiSelectSegmented.AddSegment("Bold");
            _multiSelectSegmented.AddSegment("Italic");
            _multiSelectSegmented.AddSegment("Underline");
            _multiSelectSegmented.AddSegment("Strikethrough");

            _multiSelectSegmented.SelectionChanged += (s, e) =>
            {
                var selectedIndices = _multiSelectSegmented.SelectedIndices.ToArray();
                var selectedTexts = selectedIndices.Select(i => _multiSelectSegmented.Items[i].Text);
                string selectedText = string.Join(", ", selectedTexts);
                UpdateStatus($"Multi-select: [{selectedText}]");
            };
        }

        private void InitializeSplitButtons()
        {
            // Primary Action Split Button
            _primaryActionSplitButton = new SplitButton
            {
                Text = "Save",
                Width = 150,
                Height = 48
            };

            // Add menu items
            _primaryActionSplitButton.AddMenuItem("Save", "Save the current document");
            _primaryActionSplitButton.AddMenuItem("Save As...", "Save with a different name");
            _primaryActionSplitButton.AddMenuItem("Save All", "Save all open documents");
            _primaryActionSplitButton.AddMenuItem("Export", "Export in different formats");

            _primaryActionSplitButton.PrimaryButtonClicked += (s, e) =>
                UpdateStatus("Primary action: Save clicked");

            _primaryActionSplitButton.ItemSelected += (s, e) =>
                UpdateStatus($"Menu item selected: {e.SelectedItem.Text}");

            // Icon Split Button
            _iconSplitButton = new SplitButton
            {
                Text = "",
                Width = 120,
                Height = 48,
                LeadingIcon = "<svg width='20' height='20' viewBox='0 0 24 24'><path d='M3 17V19H9V17H3M3 5V7H13V5H3M13 21V19H21V17H13V15H11V21H13M7 9V11H3V13H7V15H9V9H7M21 13V11H11V13H21M15 9H17V7H21V5H17V3H15V9Z' fill='#666666'/></svg>"
            };

            _iconSplitButton.AddMenuItem("Copy", "Copy to clipboard");
            _iconSplitButton.AddMenuItem("Paste", "Paste from clipboard");
            _iconSplitButton.AddMenuItem("Cut", "Cut selection");
            _iconSplitButton.AddMenuItem("Select All", "Select all content");

            _iconSplitButton.PrimaryButtonClicked += (s, e) =>
                UpdateStatus("Icon primary action clicked");

            _iconSplitButton.ItemSelected += (s, e) =>
                UpdateStatus($"Icon menu: {e.SelectedItem.Text}");
        }

        private void InitializeFloatingActionButtons()
        {
            // Primary FAB
            _primaryFab = new FloatingActionButton("➕")
            {
                Size = FloatingActionButton.FabSize.Medium,
                Type = FloatingActionButton.FabType.Primary
            };

            _primaryFab.Clicked += (s, e) =>
                UpdateStatus("Primary FAB clicked - Add new item");

            // Secondary FAB
            _secondaryFab = new FloatingActionButton("⭐")
            {
                Size = FloatingActionButton.FabSize.Small,
                Type = FloatingActionButton.FabType.Secondary
            };

            _secondaryFab.Clicked += (s, e) =>
                UpdateStatus("Secondary FAB clicked - Favorite");

            // Extended FAB
            _extendedFab = new FloatingActionButton("📝")
            {
                ExtendedText = "Compose",
                Type = FloatingActionButton.FabType.Primary
            };

            _extendedFab.Clicked += (s, e) =>
                UpdateStatus("Extended FAB clicked - Compose new message");
        }

        private void AddComponentsToPanel()
        {
            int currentY = 80;

            // Add section labels and components
            var horizontalLabel = new Label("Horizontal Button Group")
            {
                Width = 200,
                Height = 24,
                Style = Label.LabelStyle.TitleSmall,
                TextColor = SKColors.DarkBlue
            };
            _demoPanel.AddChild(horizontalLabel, 50, currentY);
            _demoPanel.AddChild(_horizontalButtonGroup, 50, currentY + 30);
            currentY += 120;

            var verticalLabel = new Label("Vertical Button Group")
            {
                Width = 200,
                Height = 24,
                Style = Label.LabelStyle.TitleSmall,
                TextColor = SKColors.DarkBlue
            };
            _demoPanel.AddChild(verticalLabel, 400, currentY - 50);
            _demoPanel.AddChild(_verticalButtonGroup, 400, currentY - 20);
            currentY += 80;

            var gridLabel = new Label("Grid Button Group (3 columns)")
            {
                Width = 250,
                Height = 24,
                Style = Label.LabelStyle.TitleSmall,
                TextColor = SKColors.DarkBlue
            };
            _demoPanel.AddChild(gridLabel, 50, currentY);
            _demoPanel.AddChild(_gridButtonGroup, 50, currentY + 30);
            currentY += 180;

            var segmentedLabel = new Label("Segmented Buttons")
            {
                Width = 200,
                Height = 24,
                Style = Label.LabelStyle.TitleSmall,
                TextColor = SKColors.DarkBlue
            };
            _demoPanel.AddChild(segmentedLabel, 50, currentY);

            var singleSelectLabel = new Label("Single Select:")
            {
                Width = 100,
                Height = 20,
                Style = Label.LabelStyle.BodySmall,
                TextColor = SKColors.Gray
            };
            _demoPanel.AddChild(singleSelectLabel, 50, currentY + 30);
            _demoPanel.AddChild(_singleSelectSegmented, 140, currentY + 25);

            var multiSelectLabel = new Label("Multi Select:")
            {
                Width = 100,
                Height = 20,
                Style = Label.LabelStyle.BodySmall,
                TextColor = SKColors.Gray
            };
            _demoPanel.AddChild(multiSelectLabel, 50, currentY + 85);
            _demoPanel.AddChild(_multiSelectSegmented, 140, currentY + 80);
            currentY += 160;

            var splitButtonLabel = new Label("Split Buttons")
            {
                Width = 150,
                Height = 24,
                Style = Label.LabelStyle.TitleSmall,
                TextColor = SKColors.DarkBlue
            };
            _demoPanel.AddChild(splitButtonLabel, 50, currentY);
            _demoPanel.AddChild(_primaryActionSplitButton, 50, currentY + 30);
            _demoPanel.AddChild(_iconSplitButton, 220, currentY + 30);
            currentY += 120;

            var fabLabel = new Label("Floating Action Buttons")
            {
                Width = 200,
                Height = 24,
                Style = Label.LabelStyle.TitleSmall,
                TextColor = SKColors.DarkBlue
            };
            _demoPanel.AddChild(fabLabel, 50, currentY);

            var primaryFabLabel = new Label("Primary:")
            {
                Width = 60,
                Height = 20,
                Style = Label.LabelStyle.BodySmall,
                TextColor = SKColors.Gray
            };
            _demoPanel.AddChild(primaryFabLabel, 50, currentY + 30);
            _demoPanel.AddChild(_primaryFab, 120, currentY + 25);

            var secondaryFabLabel = new Label("Secondary:")
            {
                Width = 70,
                Height = 20,
                Style = Label.LabelStyle.BodySmall,
                TextColor = SKColors.Gray
            };
            _demoPanel.AddChild(secondaryFabLabel, 200, currentY + 30);
            _demoPanel.AddChild(_secondaryFab, 280, currentY + 25);

            var extendedFabLabel = new Label("Extended:")
            {
                Width = 65,
                Height = 20,
                Style = Label.LabelStyle.BodySmall,
                TextColor = SKColors.Gray
            };
            _demoPanel.AddChild(extendedFabLabel, 350, currentY + 30);
            _demoPanel.AddChild(_extendedFab, 425, currentY + 25);
        }

        private void UpdateStatus(string message)
        {
            _statusLabel.Text = message;
        }

        /// <summary>
        /// Draws the Material Design 3.0 components demo on the specified canvas.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="width">The width of the drawing area.</param>
        /// <param name="height">The height of the drawing area.</param>
        public void Draw(SKCanvas canvas, int width, int height)
        {
            // Clear canvas with a light background
            canvas.Clear(new SKColor(250, 250, 250));

            // Center the demo panel
            _demoPanel.X = (width - _demoPanel.Width) / 2;
            _demoPanel.Y = 20;

            // Position status label at the bottom
            _statusLabel.X = (width - _statusLabel.Width) / 2;
            _statusLabel.Y = height - 60;

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

        /// <summary>
        /// Handles mouse move events.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        public void HandleMouseMove(SKPoint point)
        {
            _componentManager.HandleMouseMove(point);
        }

        /// <summary>
        /// Handles mouse up events.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        public void HandleMouseUp(SKPoint point)
        {
            _componentManager.HandleMouseUp(point);
        }
    }
}
