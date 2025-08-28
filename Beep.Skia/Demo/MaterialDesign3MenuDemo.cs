using SkiaSharp;
using System;
using Beep.Skia.Components;

namespace Beep.Skia.Demo
{
    /// <summary>
    /// Demo class showcasing Material Design 3.0 Menu components.
    /// </summary>
    public class MaterialDesign3MenuDemo : MaterialControl
    {
        private Menu _basicMenu;
        private ContextMenu _contextMenu;
        private MenuBar _menuBar;
        private CascadingMenu _cascadingMenu;
        private MenuButton _menuButton;
        private FloatingActionButton _fab;

        /// <summary>
        /// Initializes a new instance of the MaterialDesign3MenuDemo class.
        /// </summary>
        public MaterialDesign3MenuDemo()
        {
            InitializeComponents();
            SetupEventHandlers();
        }

        private void InitializeComponents()
        {
            // Create basic dropdown menu
            _basicMenu = new Menu();
            _basicMenu.AddItem(new MenuItem("Open", "📂", "Ctrl+O"));
            _basicMenu.AddItem(new MenuItem("Save", "💾", "Ctrl+S"));
            _basicMenu.AddItem(new MenuItem("Save As...", "💾", "Ctrl+Shift+S"));
            _basicMenu.AddItem(MenuItem.Separator());
            _basicMenu.AddItem(new MenuItem("Exit", "✖", "Alt+F4"));

            // Create context menu
            _contextMenu = new ContextMenu();
            _contextMenu.AddStandardItems(includeCopy: true, includeCut: true, includePaste: true,
                                        includeDelete: true, includeSelectAll: true);

            // Create menu bar
            _menuBar = new MenuBar();
            _menuBar.Width = 400f;

            var fileMenu = new Menu();
            fileMenu.AddItem(new MenuItem("New", "➕", "Ctrl+N"));
            fileMenu.AddItem(new MenuItem("Open", "📂", "Ctrl+O"));
            fileMenu.AddItem(new MenuItem("Save", "💾", "Ctrl+S"));
            fileMenu.AddItem(new MenuItem("Save As...", "💾", "Ctrl+Shift+S"));
            fileMenu.AddItem(MenuItem.Separator());
            fileMenu.AddItem(new MenuItem("Exit", "✖", "Alt+F4"));

            var editMenu = new Menu();
            editMenu.AddItem(new MenuItem("Undo", "↶", "Ctrl+Z"));
            editMenu.AddItem(new MenuItem("Redo", "↷", "Ctrl+Y"));
            editMenu.AddItem(MenuItem.Separator());
            editMenu.AddItem(new MenuItem("Cut", "✂", "Ctrl+X"));
            editMenu.AddItem(new MenuItem("Copy", "📋", "Ctrl+C"));
            editMenu.AddItem(new MenuItem("Paste", "📄", "Ctrl+V"));
            editMenu.AddItem(MenuItem.Separator());
            editMenu.AddItem(new MenuItem("Find", "🔍", "Ctrl+F"));
            editMenu.AddItem(new MenuItem("Replace", "🔄", "Ctrl+H"));

            var viewMenu = new Menu();
            viewMenu.AddItem(new MenuItem("Zoom In", "🔍+", "Ctrl++"));
            viewMenu.AddItem(new MenuItem("Zoom Out", "🔍-", "Ctrl+-"));
            viewMenu.AddItem(new MenuItem("Reset Zoom", "🔍", "Ctrl+0"));
            viewMenu.AddItem(MenuItem.Separator());
            viewMenu.AddItem(new MenuItem("Full Screen", "⛶", "F11"));

            _menuBar.AddItem("File", fileMenu);
            _menuBar.AddItem("Edit", editMenu);
            _menuBar.AddItem("View", viewMenu);

            // Create cascading menu
            _cascadingMenu = new CascadingMenu();

            var fileSubmenu = new CascadingMenu();
            fileSubmenu.AddItem("New File", null, "📄");
            fileSubmenu.AddItem("New Folder", null, "📁");
            fileSubmenu.AddItem("New Project", null, "🏗️");

            var openSubmenu = new CascadingMenu();
            openSubmenu.AddItem("Open File", null, "📂");
            openSubmenu.AddItem("Open Folder", null, "📁");
            openSubmenu.AddItem("Open Recent", null, "🕒");

            _cascadingMenu.AddItem("File", fileSubmenu, "📄");
            _cascadingMenu.AddItem("Open", openSubmenu, "📂");
            _cascadingMenu.AddItem("Save", null, "💾", "Ctrl+S");
            _cascadingMenu.AddItem("Exit", null, "✖", "Alt+F4");

            // Create menu button
            _menuButton = new MenuButton("Actions", _basicMenu);
            _menuButton.X = 50f;
            _menuButton.Y = 200f;

            // Create FAB
            _fab = new FloatingActionButton();
            _fab.Type = FloatingActionButton.FabType.Primary;
            _fab.ExtendedText = "Action";
            _fab.Icon = "➕";
            _fab.X = 300f;
            _fab.Y = 200f;
        }

        private void SetupEventHandlers()
        {
            // Menu bar events
            _menuBar.ItemActivated += OnMenuBarItemActivated;
            _menuBar.ItemDeactivated += OnMenuBarItemDeactivated;

            // Context menu events
            _contextMenu.Showing += OnContextMenuShowing;

            // Cascading menu events
            _cascadingMenu.SubmenuShowing += OnCascadingSubmenuShowing;
            _cascadingMenu.SubmenuHidden += OnCascadingSubmenuHidden;

            // Menu button events
            _menuButton.ButtonClicked += OnMenuButtonClicked;
            _menuButton.MenuOpened += OnMenuButtonMenuOpened;
            _menuButton.MenuClosed += OnMenuButtonMenuClosed;

            // FAB events
            _fab.Clicked += OnFabClicked;
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Draw background
            using (var paint = new SKPaint())
            {
                paint.Color = MaterialColors.Surface;
                paint.Style = SKPaintStyle.Fill;
                canvas.DrawRect(Bounds, paint);
            }

            // Draw title
            using (var paint = new SKPaint())
            {
                paint.Color = MaterialColors.OnSurface;
                paint.TextSize = 24f;
                paint.IsAntialias = true;
                paint.TextAlign = SKTextAlign.Center;

                string title = "Material Design 3.0 Menu Components Demo";
                canvas.DrawText(title, Bounds.Width / 2, 40, paint);
            }

            // Draw menu bar
            _menuBar.X = 50;
            _menuBar.Y = 80;
            _menuBar.Width = 400;
            _menuBar.Height = 32;
            _menuBar.Draw(canvas, context);

            // Draw menu button
            _menuButton.Draw(canvas, context);

            // Draw FAB
            _fab.Draw(canvas, context);

            // Draw instructions
            using (var paint = new SKPaint())
            {
                paint.Color = MaterialColors.OnSurfaceVariant;
                paint.TextSize = 14f;
                paint.IsAntialias = true;

                string[] instructions = {
                    "• Click menu bar items to see dropdown menus",
                    "• Right-click anywhere to see context menu",
                    "• Click 'Actions' button to see menu button",
                    "• Click FAB to see floating action menu",
                    "• Hover over cascading menu items to see submenus"
                };

                float y = 300f;
                foreach (string instruction in instructions)
                {
                    canvas.DrawText(instruction, 50, y, paint);
                    y += 20f;
                }
            }
        }

        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            base.OnMouseDown(point, context);

            if (context.MouseButton == 1) // Right button
            {
                // Show context menu on right-click
                _contextMenu.Show(point);
            }
            else if (context.MouseButton == 0) // Left button
            {
                // Handle menu bar clicks
                var menuBarBounds = new SKRect(50, 80, 450, 112);
                if (menuBarBounds.Contains(point))
                {
                    _menuBar.HandleMouseDown(point, context);
                }

                // Handle menu button clicks
                var menuButtonBounds = new SKRect(_menuButton.X, _menuButton.Y,
                                                _menuButton.X + _menuButton.Width,
                                                _menuButton.Y + _menuButton.Height);
                if (menuButtonBounds.Contains(point))
                {
                    _menuButton.HandleMouseDown(point, context);
                }

                // Handle FAB clicks
                var fabBounds = new SKRect(_fab.X, _fab.Y,
                                         _fab.X + _fab.Width,
                                         _fab.Y + _fab.Height);
                if (fabBounds.Contains(point))
                {
                    _fab.HandleMouseDown(point, context);
                }
            }

            return true;
        }

        protected override bool OnMouseMove(SKPoint point, InteractionContext context)
        {
            base.OnMouseMove(point, context);

            // Handle menu bar hover
            var menuBarBounds = new SKRect(50, 80, 450, 112);
            if (menuBarBounds.Contains(point))
            {
                _menuBar.HandleMouseMove(point, context);
            }

            return true;
        }

        // Event handlers
        private void OnMenuBarItemActivated(object sender, MenuBar.MenuBarItemEventArgs e)
        {
            Console.WriteLine($"Menu bar item activated: {e.Item.Text}");
        }

        private void OnMenuBarItemDeactivated(object sender, MenuBar.MenuBarItemEventArgs e)
        {
            Console.WriteLine($"Menu bar item deactivated: {e.Item.Text}");
        }

        private void OnContextMenuShowing(object sender, ContextMenu.ContextMenuEventArgs e)
        {
            Console.WriteLine($"Context menu showing at: {e.TriggerPoint}");
        }

        private void OnCascadingSubmenuShowing(object sender, CascadingMenu.CascadingMenuEventArgs e)
        {
            Console.WriteLine($"Cascading submenu showing: {e.Item.Text}");
        }

        private void OnCascadingSubmenuHidden(object sender, CascadingMenu.CascadingMenuEventArgs e)
        {
            Console.WriteLine($"Cascading submenu hidden: {e.Item.Text}");
        }

        private void OnMenuButtonClicked(object sender, EventArgs e)
        {
            Console.WriteLine("Menu button clicked");
        }

        private void OnMenuButtonMenuOpened(object sender, EventArgs e)
        {
            Console.WriteLine("Menu button menu opened");
        }

        private void OnMenuButtonMenuClosed(object sender, EventArgs e)
        {
            Console.WriteLine("Menu button menu closed");
        }

        private void OnFabClicked(object sender, EventArgs e)
        {
            Console.WriteLine("FAB clicked");
        }
    }
}
