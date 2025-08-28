using Beep.Skia.Components;
using SkiaSharp;

namespace Beep.Skia.Demo
{
    /// <summary>
    /// Demo showcasing Material Design 3.0 components: Slider, Switch, and Tabs.
    /// </summary>
    public class MaterialDesign3ComponentsDemo
    {
        private Slider _continuousSlider;
        private Slider _discreteSlider;
        private Switch _primarySwitch;
        private Switch _secondarySwitch;
        private Tabs _primaryTabs;
        private Tabs _secondaryTabs;
        private Panel _demoPanel;
        private ComponentManager _componentManager;
        private Label _statusLabel;

        public MaterialDesign3ComponentsDemo()
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
            _statusLabel = new Label("Interact with the components below")
            {
                Width = 400,
                Height = 30,
                Style = Label.LabelStyle.BodyMedium,
                TextColor = SKColors.DarkGray,
                TextAlign = SKTextAlign.Center
            };

            InitializeSliders();
            InitializeSwitches();
            InitializeTabs();
            SetupLayout();
            SetupEventHandlers();
        }

        private void InitializeSliders()
        {
            // Create continuous slider
            _continuousSlider = new Slider
            {
                Width = 300,
                Height = 40,
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                SliderType = SliderType.Continuous
            };

            // Create discrete slider
            _discreteSlider = new Slider
            {
                Width = 300,
                Height = 40,
                Minimum = 0,
                Maximum = 10,
                Value = 5,
                StepSize = 1,
                SliderType = SliderType.Discrete
            };
        }

        private void InitializeSwitches()
        {
            // Create primary switch
            _primarySwitch = new Switch
            {
                Width = 52,
                Height = 32,
                IsChecked = true
            };

            // Create secondary switch
            _secondarySwitch = new Switch
            {
                Width = 52,
                Height = 32,
                IsChecked = false
            };
        }

        private void InitializeTabs()
        {
            // Create primary tabs
            _primaryTabs = new Tabs
            {
                Width = 400,
                Height = 48,
                Variant = Tabs.TabVariant.Primary
            };
            _primaryTabs.AddTab("Home", "<svg width='24' height='24' viewBox='0 0 24 24'><path d='M10 20v-6h4v6h5v-8h3L12 3 2 12h3v8z' fill='currentColor'/></svg>");
            _primaryTabs.AddTab("Favorites", "<svg width='24' height='24' viewBox='0 0 24 24'><path d='M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z' fill='currentColor'/></svg>");
            _primaryTabs.AddTab("Settings", "<svg width='24' height='24' viewBox='0 0 24 24'><path d='M19.14,12.94c0.04-0.3,0.06-0.61,0.06-0.94c0-0.32-0.02-0.64-0.07-0.94l2.03-1.58c0.18-0.14,0.23-0.41,0.12-0.61 l-1.92-3.32c-0.12-0.22-0.37-0.29-0.59-0.22l-2.39,0.96c-0.5-0.38-1.03-0.7-1.62-0.94L14.4,2.81c-0.04-0.24-0.24-0.41-0.48-0.41 h-3.84c-0.24,0-0.43,0.17-0.47,0.41L9.25,5.35C8.66,5.59,8.12,5.92,7.63,6.29L5.24,5.33c-0.22-0.08-0.47,0-0.59,0.22L2.74,8.87 C2.62,9.08,2.66,9.34,2.86,9.48l2.03,1.58C4.84,11.36,4.82,11.69,4.82,12s0.02,0.64,0.07,0.94l-2.03,1.58 c-0.18,0.14-0.23,0.41-0.12,0.61l1.92,3.32c0.12,0.22,0.37,0.29,0.59,0.22l2.39-0.96c0.5,0.38,1.03,0.7,1.62,0.94l0.36,2.54 c0.05,0.24,0.24,0.41,0.48,0.41h3.84c0.24,0,0.43-0.17,0.47-0.41l0.36-2.54c0.59-0.24,1.13-0.56,1.62-0.94l2.39,0.96 c0.22,0.08,0.47,0,0.59-0.22l1.92-3.32c0.12-0.22,0.07-0.47-0.12-0.61L19.14,12.94z M12,15.6c-1.98,0-3.6-1.62-3.6-3.6 s1.62-3.6,3.6-3.6s3.6,1.62,3.6,3.6S13.98,15.6,12,15.6z' fill='currentColor'/></svg>");

            // Create secondary tabs
            _secondaryTabs = new Tabs
            {
                Width = 400,
                Height = 40,
                Variant = Tabs.TabVariant.Secondary
            };
            _secondaryTabs.AddTab("Overview");
            _secondaryTabs.AddTab("Analytics");
            _secondaryTabs.AddTab("Reports");
            _secondaryTabs.AddTab("Settings");
        }

        private void SetupLayout()
        {
            // Position components in the demo panel
            float yOffset = 60;

            // Add title
            var titleLabel = new Label("Material Design 3.0 Components")
            {
                Width = 400,
                Height = 40,
                Style = Label.LabelStyle.HeadlineSmall,
                TextColor = SKColors.Black,
                TextAlign = SKTextAlign.Center
            };
            titleLabel.X = (_demoPanel.Width - titleLabel.Width) / 2;
            titleLabel.Y = yOffset;
            _demoPanel.AddChild(titleLabel);
            yOffset += 60;

            // Add status label
            _statusLabel.X = (_demoPanel.Width - _statusLabel.Width) / 2;
            _statusLabel.Y = yOffset;
            _demoPanel.AddChild(_statusLabel);
            yOffset += 50;

            // Add sliders section
            var slidersLabel = new Label("Sliders")
            {
                Width = 200,
                Height = 30,
                Style = Label.LabelStyle.TitleMedium,
                TextColor = SKColors.Black
            };
            slidersLabel.X = 50;
            slidersLabel.Y = yOffset;
            _demoPanel.AddChild(slidersLabel);
            yOffset += 40;

            // Continuous slider
            var continuousLabel = new Label("Continuous: " + _continuousSlider.Value.ToString("F0"))
            {
                Width = 150,
                Height = 20,
                Style = Label.LabelStyle.BodySmall,
                TextColor = SKColors.DarkGray
            };
            continuousLabel.X = 50;
            continuousLabel.Y = yOffset;
            _demoPanel.AddChild(continuousLabel);

            _continuousSlider.X = 200;
            _continuousSlider.Y = yOffset - 5;
            _demoPanel.AddChild(_continuousSlider);
            yOffset += 50;

            // Discrete slider
            var discreteLabel = new Label("Discrete: " + _discreteSlider.Value.ToString("F0"))
            {
                Width = 150,
                Height = 20,
                Style = Label.LabelStyle.BodySmall,
                TextColor = SKColors.DarkGray
            };
            discreteLabel.X = 50;
            discreteLabel.Y = yOffset;
            _demoPanel.AddChild(discreteLabel);

            _discreteSlider.X = 200;
            _discreteSlider.Y = yOffset - 5;
            _demoPanel.AddChild(_discreteSlider);
            yOffset += 60;

            // Add switches section
            var switchesLabel = new Label("Switches")
            {
                Width = 200,
                Height = 30,
                Style = Label.LabelStyle.TitleMedium,
                TextColor = SKColors.Black
            };
            switchesLabel.X = 50;
            switchesLabel.Y = yOffset;
            _demoPanel.AddChild(switchesLabel);
            yOffset += 40;

            // Primary switch
            var primarySwitchLabel = new Label("Primary Switch")
            {
                Width = 120,
                Height = 20,
                Style = Label.LabelStyle.BodySmall,
                TextColor = SKColors.DarkGray
            };
            primarySwitchLabel.X = 50;
            primarySwitchLabel.Y = yOffset;
            _demoPanel.AddChild(primarySwitchLabel);

            _primarySwitch.X = 180;
            _primarySwitch.Y = yOffset - 4;
            _demoPanel.AddChild(_primarySwitch);
            yOffset += 40;

            // Secondary switch
            var secondarySwitchLabel = new Label("Secondary Switch")
            {
                Width = 130,
                Height = 20,
                Style = Label.LabelStyle.BodySmall,
                TextColor = SKColors.DarkGray
            };
            secondarySwitchLabel.X = 50;
            secondarySwitchLabel.Y = yOffset;
            _demoPanel.AddChild(secondarySwitchLabel);

            _secondarySwitch.X = 180;
            _secondarySwitch.Y = yOffset - 4;
            _demoPanel.AddChild(_secondarySwitch);
            yOffset += 60;

            // Add tabs section
            var tabsLabel = new Label("Tabs")
            {
                Width = 200,
                Height = 30,
                Style = Label.LabelStyle.TitleMedium,
                TextColor = SKColors.Black
            };
            tabsLabel.X = 50;
            tabsLabel.Y = yOffset;
            _demoPanel.AddChild(tabsLabel);
            yOffset += 40;

            // Primary tabs
            var primaryTabsLabel = new Label("Primary Tabs")
            {
                Width = 120,
                Height = 20,
                Style = Label.LabelStyle.BodySmall,
                TextColor = SKColors.DarkGray
            };
            primaryTabsLabel.X = 50;
            primaryTabsLabel.Y = yOffset;
            _demoPanel.AddChild(primaryTabsLabel);

            _primaryTabs.X = 50;
            _primaryTabs.Y = yOffset + 25;
            _demoPanel.AddChild(_primaryTabs);
            yOffset += 80;

            // Secondary tabs
            var secondaryTabsLabel = new Label("Secondary Tabs")
            {
                Width = 130,
                Height = 20,
                Style = Label.LabelStyle.BodySmall,
                TextColor = SKColors.DarkGray
            };
            secondaryTabsLabel.X = 50;
            secondaryTabsLabel.Y = yOffset;
            _demoPanel.AddChild(secondaryTabsLabel);

            _secondaryTabs.X = 50;
            _secondaryTabs.Y = yOffset + 25;
            _demoPanel.AddChild(_secondaryTabs);
        }

        private void SetupEventHandlers()
        {
            // Slider event handlers
            _continuousSlider.ValueChanged += (s, e) =>
            {
                var label = _demoPanel.Children.OfType<Label>().FirstOrDefault(l => l.Text.StartsWith("Continuous:"));
                if (label != null)
                {
                    label.Text = $"Continuous: {e.PrimaryValue:F0}";
                    _statusLabel.Text = $"Continuous slider changed to {e.PrimaryValue:F0}";
                }
            };

            _discreteSlider.ValueChanged += (s, e) =>
            {
                var label = _demoPanel.Children.OfType<Label>().FirstOrDefault(l => l.Text.StartsWith("Discrete:"));
                if (label != null)
                {
                    label.Text = $"Discrete: {e.PrimaryValue:F0}";
                    _statusLabel.Text = $"Discrete slider changed to {e.PrimaryValue:F0}";
                }
            };

            // Switch event handlers
            _primarySwitch.StateChanged += (s, e) =>
            {
                _statusLabel.Text = $"Primary switch is now {(e.IsChecked ? "ON" : "OFF")}";
            };

            _secondarySwitch.StateChanged += (s, e) =>
            {
                _statusLabel.Text = $"Secondary switch is now {(e.IsChecked ? "ON" : "OFF")}";
            };

            // Tabs event handlers
            _primaryTabs.SelectedIndexChanged += (s, e) =>
            {
                var selectedTab = _primaryTabs.TabItems[e.SelectedIndex];
                _statusLabel.Text = $"Primary tabs: Selected '{selectedTab.Text}' (index {e.SelectedIndex})";
            };

            _secondaryTabs.SelectedIndexChanged += (s, e) =>
            {
                var selectedTab = _secondaryTabs.TabItems[e.SelectedIndex];
                _statusLabel.Text = $"Secondary tabs: Selected '{selectedTab.Text}' (index {e.SelectedIndex})";
            };
        }

        /// <summary>
        /// Gets the demo panel containing all components.
        /// </summary>
        public Panel DemoPanel => _demoPanel;

        /// <summary>
        /// Gets the component manager for this demo.
        /// </summary>
        public ComponentManager ComponentManager => _componentManager;
    }
}
