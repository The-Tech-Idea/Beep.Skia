using SkiaSharp;
using System;
using Beep.Skia.Components;
using Beep.Skia.Layout;

namespace Beep.Skia.Demo
{
    /// <summary>
    /// A demo application that showcases the Skia components framework.
    /// </summary>
    public class ComponentDemo
    {
        private readonly ComponentManager _componentManager;
        private readonly Panel _mainPanel;
        private readonly Label _titleLabel;
        private readonly Button _clickButton;
        private readonly Label _clickCountLabel;
        private int _clickCount = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentDemo"/> class.
        /// </summary>
        public ComponentDemo()
        {
            _componentManager = new ComponentManager();

            // Create main panel
            _mainPanel = new Panel
            {
                Width = 400,
                Height = 300,
                BackgroundColor = SKColors.White,
                BorderColor = SKColors.DarkGray,
                BorderWidth = 2,
                CornerRadius = 8
            };

            // Create title label
            _titleLabel = new Label("Skia Components Demo")
            {
                FontSize = 18,
                TextColor = SKColors.DarkBlue,
                TextAlign = SKTextAlign.Center
            };

            // Create click button
            _clickButton = new Button("Click Me!")
            {
                BackgroundColor = SKColors.LightGreen,
                Width = 100,
                Height = 35
            };
            _clickButton.Clicked += OnButtonClicked;

            // Create click count label
            _clickCountLabel = new Label("Clicks: 0")
            {
                TextColor = SKColors.DarkRed
            };

            // Add components to main panel
            _mainPanel.AddChild(_titleLabel, 20, 20);
            _mainPanel.AddChild(_clickButton, 150, 100);
            _mainPanel.AddChild(_clickCountLabel, 150, 150);

            // Add main panel to component manager
            _componentManager.AddComponent(_mainPanel);
        }

        /// <summary>
        /// Draws the demo on the specified canvas.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="width">The width of the drawing area.</param>
        /// <param name="height">The height of the drawing area.</param>
        public void Draw(SKCanvas canvas, int width, int height)
        {
            // Clear canvas
            canvas.Clear(SKColors.LightGray);

            // Create drawing context
            var context = new DrawingContext();

            // Center the main panel
            _mainPanel.X = (width - _mainPanel.Width) / 2;
            _mainPanel.Y = (height - _mainPanel.Height) / 2;

            // Draw all components
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

        /// <summary>
        /// Handles button click events.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnButtonClicked(object sender, EventArgs e)
        {
            _clickCount++;
            _clickCountLabel.Text = $"Clicks: {_clickCount}";

            // Change button color based on click count
            if (_clickCount % 2 == 0)
            {
                _clickButton.BackgroundColor = SKColors.LightGreen;
            }
            else
            {
                _clickButton.BackgroundColor = SKColors.LightBlue;
            }
        }
    }

    /// <summary>
    /// A demo that showcases the layout managers.
    /// </summary>
    public class LayoutDemo
    {
        private readonly ComponentManager _componentManager;
        private readonly Panel _flowPanel;
        private readonly Panel _gridPanel;
        private readonly Panel _stackPanel;
        private readonly ILayoutManager _flowLayout;
        private readonly ILayoutManager _gridLayout;
        private readonly ILayoutManager _stackLayout;

        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutDemo"/> class.
        /// </summary>
        public LayoutDemo()
        {
            _componentManager = new ComponentManager();

            // Initialize layout managers
            _flowLayout = new FlowLayout { HorizontalSpacing = 10, VerticalSpacing = 10 };
            _gridLayout = new GridLayout(3) { HorizontalSpacing = 8, VerticalSpacing = 8 };
            _stackLayout = new StackLayout(Orientation.Vertical) { Spacing = 12 };

            // Create flow layout panel
            _flowPanel = new Panel
            {
                Width = 380,
                Height = 120,
                BackgroundColor = SKColors.LightCyan,
                BorderColor = SKColors.DarkCyan,
                BorderWidth = 1,
                CornerRadius = 4
            };

            // Create grid layout panel
            _gridPanel = new Panel
            {
                Width = 380,
                Height = 120,
                BackgroundColor = SKColors.LightYellow,
                BorderColor = SKColors.DarkOrange,
                BorderWidth = 1,
                CornerRadius = 4
            };

            // Create stack layout panel
            _stackPanel = new Panel
            {
                Width = 380,
                Height = 120,
                BackgroundColor = SKColors.LightPink,
                BorderColor = SKColors.DarkMagenta,
                BorderWidth = 1,
                CornerRadius = 4
            };

            // Create components for flow layout
            var flowComponents = new List<SkiaComponent>();
            for (int i = 1; i <= 8; i++)
            {
                var button = new Button($"Flow {i}")
                {
                    Width = 60,
                    Height = 25,
                    BackgroundColor = SKColors.LightGreen
                };
                flowComponents.Add(button);
                _flowPanel.AddChild(button);
            }

            // Create components for grid layout
            var gridComponents = new List<SkiaComponent>();
            for (int i = 1; i <= 6; i++)
            {
                var label = new Label($"Grid {i}")
                {
                    Width = 80,
                    Height = 20,
                    TextColor = SKColors.White,
                    TextAlign = SKTextAlign.Center
                };
                gridComponents.Add(label);
                _gridPanel.AddChild(label);
            }

            // Create components for stack layout
            var stackComponents = new List<SkiaComponent>();
            for (int i = 1; i <= 4; i++)
            {
                var button = new Button($"Stack {i}")
                {
                    Width = 100,
                    Height = 25,
                    BackgroundColor = SKColors.LightCoral
                };
                stackComponents.Add(button);
                _stackPanel.AddChild(button);
            }

            // Apply layouts
            _flowLayout.Layout(flowComponents, new SKRect(10, 10, _flowPanel.Width - 10, _flowPanel.Height - 10));
            _gridLayout.Layout(gridComponents, new SKRect(10, 10, _gridPanel.Width - 10, _gridPanel.Height - 10));
            _stackLayout.Layout(stackComponents, new SKRect(10, 10, _stackPanel.Width - 10, _stackPanel.Height - 10));

            // Position panels
            _flowPanel.X = 10;
            _flowPanel.Y = 10;

            _gridPanel.X = 10;
            _gridPanel.Y = 140;

            _stackPanel.X = 10;
            _stackPanel.Y = 270;

            // Add panels to component manager
            _componentManager.AddComponent(_flowPanel);
            _componentManager.AddComponent(_gridPanel);
            _componentManager.AddComponent(_stackPanel);
        }

        /// <summary>
        /// Draws the layout demo on the specified canvas.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="width">The width of the drawing area.</param>
        /// <param name="height">The height of the drawing area.</param>
        public void Draw(SKCanvas canvas, int width, int height)
        {
            // Clear canvas
            canvas.Clear(SKColors.White);

            // Create drawing context
            var context = new DrawingContext();

            // Draw all components
            _componentManager.Render();
        }
    }
}
