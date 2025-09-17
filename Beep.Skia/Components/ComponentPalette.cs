using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using Beep.Skia.Model;
namespace Beep.Skia.Components
{
    /// <summary>
    /// A component palette that displays available Skia components in an organized list
    /// with drag and drop functionality for design-time component creation.
    /// </summary>
    public class ComponentPalette : MaterialControl
    {
        private ObservableCollection<ComponentCategory> _categories = new ObservableCollection<ComponentCategory>();
        private ComponentCategory _selectedCategory;
        private float _itemHeight = 48.0f;
        private float _categoryHeaderHeight = 40.0f;
        private float _scrollOffset = 0.0f;
        private float _maxScrollOffset = 0.0f;
        private bool _isDragging = false;
        private ComponentItem _draggedComponent;
        private SKPoint _dragStartPoint;
        private SKPoint _lastMousePoint;

        /// <summary>
        /// Represents a category of components in the palette.
        /// </summary>
        public class ComponentCategory
        {
            public string Name { get; set; }
            public string Icon { get; set; }
            public ObservableCollection<ComponentItem> Components { get; set; } = new ObservableCollection<ComponentItem>();
            public bool IsExpanded { get; set; } = true;

            public ComponentCategory(string name, string icon = null)
            {
                Name = name;
                Icon = icon;
            }
        }

        /// <summary>
        /// Represents a component item that can be either a class or a method.
        /// </summary>
        public class ComponentItem
        {
            public AssemblyClassDefinition ClassDefinition { get; set; }
            public MethodsClass Method { get; set; }
            public bool IsClass => Method == null;
            public bool IsMethod => Method != null;
            public bool IsExpanded { get; set; } = false;
            public ObservableCollection<ComponentItem> ChildItems { get; set; } = new ObservableCollection<ComponentItem>();

            public ComponentItem(AssemblyClassDefinition classDef, MethodsClass method = null)
            {
                ClassDefinition = classDef;
                Method = method;
            }

            public string DisplayName
            {
                get
                {
                    if (IsMethod && Method != null)
                    {
                        return Method.Caption ?? Method.Name ?? "Unnamed Method";
                    }
                    else if (ClassDefinition != null)
                    {
                        return GetDisplayName(ClassDefinition);
                    }
                    return "Unknown";
                }
            }

            private string GetDisplayName(AssemblyClassDefinition component)
            {
                if (!string.IsNullOrEmpty(component.className))
                {
                    // Convert PascalCase to Title Case with spaces
                    var name = component.className;
                    return string.Concat(name.Select((c, i) =>
                        i > 0 && char.IsUpper(c) ? " " + c : c.ToString())).Trim();
                }
                return "Unknown Component";
            }
        }

        /// <summary>
        /// Occurs when a component is selected in the palette.
        /// </summary>
        public event EventHandler<ComponentItem> ComponentSelected;

        /// <summary>
        /// Occurs when a component starts being dragged from the palette.
        /// </summary>
        public event EventHandler<ComponentDragEventArgs> ComponentDragStarted;

        /// <summary>
        /// Occurs when a component is being dragged.
        /// </summary>
        public event EventHandler<ComponentDragEventArgs> ComponentDragging;

        /// <summary>
        /// Occurs when a component drag operation ends.
        /// </summary>
        public event EventHandler<ComponentDragEventArgs> ComponentDragEnded;

        /// <summary>
        /// Gets or sets the categories of components to display.
        /// </summary>
        public ObservableCollection<ComponentCategory> Categories
        {
            get => _categories;
            set
            {
                if (_categories != value)
                {
                    _categories = value ?? new ObservableCollection<ComponentCategory>();
                    _categories.CollectionChanged += (s, e) => InvalidateVisual();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the currently selected category.
        /// </summary>
        public ComponentCategory SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory != value)
                {
                    _selectedCategory = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the height of each component item.
        /// </summary>
        public float ItemHeight
        {
            get => _itemHeight;
            set
            {
                if (_itemHeight != value)
                {
                    _itemHeight = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the height of category headers.
        /// </summary>
        public float CategoryHeaderHeight
        {
            get => _categoryHeaderHeight;
            set
            {
                if (_categoryHeaderHeight != value)
                {
                    _categoryHeaderHeight = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets the currently dragged component.
        /// </summary>
        public ComponentItem DraggedComponent => _draggedComponent;

        /// <summary>
        /// Gets whether a component is currently being dragged.
        /// </summary>
        public bool IsDragging => _isDragging;

        /// <summary>
        /// Provides data for component drag events.
        /// </summary>
        public class ComponentDragEventArgs : EventArgs
        {
            /// <summary>
            /// Gets the component being dragged.
            /// </summary>
            public ComponentItem Component { get; }

            /// <summary>
            /// Gets the current mouse position.
            /// </summary>
            public SKPoint MousePosition { get; }

            /// <summary>
            /// Gets the drag state.
            /// </summary>
            public ComponentDragState State { get; }

            /// <summary>
            /// Initializes a new instance of the ComponentDragEventArgs class.
            /// </summary>
            public ComponentDragEventArgs(ComponentItem component, SKPoint mousePosition, ComponentDragState state)
            {
                Component = component;
                MousePosition = mousePosition;
                State = state;
            }
        }

        /// <summary>
        /// Initializes a new instance of the ComponentPalette class.
        /// </summary>
        public ComponentPalette()
        {
            Width = 280;
            Height = 400;

            // Initialize with default categories
            InitializeDefaultCategories();
        }

        /// <summary>
        /// Initializes the palette with default categories based on component types.
        /// </summary>
        private void InitializeDefaultCategories()
        {
            _categories.Clear();

            // Common UI Components
            var uiCategory = new ComponentCategory("UI Components", "widgets");
            _categories.Add(uiCategory);

            // Layout Components
            var layoutCategory = new ComponentCategory("Layout", "view_compact");
            _categories.Add(layoutCategory);

            // Input Components
            var inputCategory = new ComponentCategory("Input", "input");
            _categories.Add(inputCategory);

            // Navigation Components
            var navigationCategory = new ComponentCategory("Navigation", "navigation");
            _categories.Add(navigationCategory);

            // Data Components
            var dataCategory = new ComponentCategory("Data", "table");
            _categories.Add(dataCategory);

            // Load components from registry
            LoadComponentsFromRegistry();
        }

        /// <summary>
        /// Loads components from the SkiaComponentRegistry and organizes them into categories.
        /// </summary>
        public void LoadComponentsFromRegistry()
        {
            if (!SkiaComponentRegistry.IsInitialized)
                return;

            var components = SkiaComponentRegistry.LoadedComponents;

            foreach (var category in _categories)
            {
                category.Components.Clear();
            }

            foreach (var component in components)
            {
                var category = GetCategoryForComponent(component);
                if (category != null)
                {
                    var componentItem = new ComponentItem(component);

                    // Add methods as child items if they exist
                    if (component.Methods != null && component.Methods.Count > 0)
                    {
                        foreach (var method in component.Methods.Where(m => !m.Hidden))
                        {
                            var methodItem = new ComponentItem(component, method);
                            componentItem.ChildItems.Add(methodItem);
                        }
                    }

                    category.Components.Add(componentItem);
                }
            }

            InvalidateVisual();
        }

        /// <summary>
        /// Determines which category a component should belong to based on its type or properties.
        /// </summary>
        private ComponentCategory GetCategoryForComponent(AssemblyClassDefinition component)
        {
            if (component == null || component.className == null)
                return _categories.FirstOrDefault(); // Default to first category

            var className = component.className.ToLower();

            // UI Components
            if (className.Contains("button") || className.Contains("label") || className.Contains("text") ||
                className.Contains("card") || className.Contains("panel") || className.Contains("svg"))
            {
                return _categories.FirstOrDefault(c => c.Name == "UI Components");
            }

            // Layout Components
            if (className.Contains("list") || className.Contains("grid") || className.Contains("stack") ||
                className.Contains("flow") || className.Contains("dock"))
            {
                return _categories.FirstOrDefault(c => c.Name == "Layout");
            }

            // Input Components
            if (className.Contains("textbox") || className.Contains("checkbox") || className.Contains("switch") ||
                className.Contains("slider") || className.Contains("datepicker") || className.Contains("timepicker"))
            {
                return _categories.FirstOrDefault(c => c.Name == "Input");
            }

            // Navigation Components
            if (className.Contains("menu") || className.Contains("navigation") || className.Contains("tab") ||
                className.Contains("drawer") || className.Contains("bar"))
            {
                return _categories.FirstOrDefault(c => c.Name == "Navigation");
            }

            // Data Components
            if (className.Contains("table") || className.Contains("chart") || className.Contains("data"))
            {
                return _categories.FirstOrDefault(c => c.Name == "Data");
            }

            // Default to UI Components
            return _categories.FirstOrDefault(c => c.Name == "UI Components");
        }

        /// <summary>
        /// Draws the component palette.
        /// </summary>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Create bounds for clipping
            var bounds = new SKRect(X, Y, X + Width, Y + Height);

            var currentY = _scrollOffset;

            foreach (var category in _categories)
            {
                // Draw category header
                DrawCategoryHeader(canvas, context, category, ref currentY);

                if (category.IsExpanded)
                {
                    // Draw category components
                    foreach (var component in category.Components)
                    {
                        DrawComponentItem(canvas, context, component, ref currentY);
                    }
                }
            }

            // Update max scroll offset
            _maxScrollOffset = Math.Max(0, currentY - Height + 20);
        }

        /// <summary>
        /// Draws a category header.
        /// </summary>
        private void DrawCategoryHeader(SKCanvas canvas, DrawingContext context, ComponentCategory category, ref float currentY)
        {
            var headerRect = new SKRect(0, currentY, Width, currentY + _categoryHeaderHeight);

            // Background
            using (var paint = new SKPaint())
            {
                paint.Color = category == _selectedCategory ?
                    MaterialColors.SecondaryContainer :
                    MaterialColors.SurfaceContainerHigh;
                canvas.DrawRect(headerRect, paint);

                // Border
                paint.Color = MaterialColors.OutlineVariant;
                paint.StrokeWidth = 1;
                canvas.DrawLine(headerRect.Left, headerRect.Bottom, headerRect.Right, headerRect.Bottom, paint);
            }

            // Category icon (if available)
            float iconX = 12;
            if (!string.IsNullOrEmpty(category.Icon))
            {
                // For now, just draw a placeholder circle for the icon
                using (var paint = new SKPaint())
                {
                    paint.Color = MaterialColors.Primary;
                    canvas.DrawCircle(iconX + 8, currentY + _categoryHeaderHeight / 2, 6, paint);
                }
                iconX += 20;
            }

            // Category name
            using (var paint = new SKPaint())
            using (var font = new SKFont())
            {
                paint.Color = MaterialColors.OnSurface;
                font.Size = 14;
                font.Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal);
                canvas.DrawText(category.Name, iconX, currentY + _categoryHeaderHeight / 2 + 5, SKTextAlign.Left, font, paint);
            }

            // Expand/collapse indicator
            using (var paint = new SKPaint())
            {
                paint.Color = MaterialColors.OnSurfaceVariant;
                paint.StrokeWidth = 2;
                paint.StrokeCap = SKStrokeCap.Round;

                float centerX = Width - 16;
                float centerY = currentY + _categoryHeaderHeight / 2;

                if (category.IsExpanded)
                {
                    // Down arrow
                    canvas.DrawLine(centerX - 4, centerY - 2, centerX, centerY + 2, paint);
                    canvas.DrawLine(centerX, centerY + 2, centerX + 4, centerY - 2, paint);
                }
                else
                {
                    // Right arrow
                    canvas.DrawLine(centerX - 2, centerY - 4, centerX + 2, centerY, paint);
                    canvas.DrawLine(centerX + 2, centerY, centerX - 2, centerY + 4, paint);
                }
            }

            currentY += _categoryHeaderHeight;
        }

        /// <summary>
        /// Draws a component item.
        /// </summary>
        private void DrawComponentItem(SKCanvas canvas, DrawingContext context, ComponentItem componentItem, ref float currentY)
        {
            var itemRect = new SKRect(0, currentY, Width, currentY + _itemHeight);

            // Background with hover effect
            using (var paint = new SKPaint())
            {
                if (_draggedComponent == componentItem && _isDragging)
                {
                    paint.Color = MaterialColors.PrimaryContainer.WithAlpha(128);
                }
                else if (itemRect.Contains(_lastMousePoint))
                {
                    paint.Color = MaterialColors.SurfaceVariant.WithAlpha(64);
                }
                else
                {
                    paint.Color = SKColors.Transparent;
                }
                canvas.DrawRect(itemRect, paint);
            }

            // Indentation for child items (methods)
            float indent = componentItem.IsMethod ? 20 : 0;

            // Expand/collapse indicator for classes with methods
            if (componentItem.IsClass && componentItem.ChildItems.Count > 0)
            {
                using (var paint = new SKPaint())
                {
                    paint.Color = MaterialColors.OnSurfaceVariant;
                    paint.StrokeWidth = 2;
                    paint.StrokeCap = SKStrokeCap.Round;

                    float centerX = indent + 12;
                    float centerY = currentY + _itemHeight / 2;

                    if (componentItem.IsExpanded)
                    {
                        // Down arrow
                        canvas.DrawLine(centerX - 4, centerY - 2, centerX, centerY + 2, paint);
                        canvas.DrawLine(centerX, centerY + 2, centerX + 4, centerY - 2, paint);
                    }
                    else
                    {
                        // Right arrow
                        canvas.DrawLine(centerX - 2, centerY - 4, centerX + 2, centerY, paint);
                        canvas.DrawLine(centerX + 2, centerY, centerX - 2, centerY + 4, paint);
                    }
                }
                indent += 20;
            }

            // Component icon placeholder
            using (var paint = new SKPaint())
            {
                paint.Color = componentItem.IsMethod ? MaterialColors.Secondary : MaterialColors.Primary;
                canvas.DrawCircle(indent + 20, currentY + _itemHeight / 2, 6, paint);
            }

            // Component name
            using (var paint = new SKPaint())
            using (var font = new SKFont())
            {
                paint.Color = MaterialColors.OnSurface;
                font.Size = 12;
                font.Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal);

                canvas.DrawText(componentItem.DisplayName, indent + 36, currentY + _itemHeight / 2 + 4, SKTextAlign.Left, font, paint);
            }

            currentY += _itemHeight;

            // Draw child items (methods) if expanded
            if (componentItem.IsClass && componentItem.IsExpanded && componentItem.ChildItems.Count > 0)
            {
                foreach (var childItem in componentItem.ChildItems)
                {
                    DrawComponentItem(canvas, context, childItem, ref currentY);
                }
            }
        }

        /// <summary>
        /// Handles mouse down events for interaction.
        /// </summary>
        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            base.OnMouseDown(point, context);
            _lastMousePoint = point;

            var localPoint = new SKPoint(point.X - X, point.Y - Y + _scrollOffset);

            // Check category headers
            float currentY = 0;
            foreach (var category in _categories)
            {
                if (localPoint.Y >= currentY && localPoint.Y < currentY + _categoryHeaderHeight)
                {
                    category.IsExpanded = !category.IsExpanded;
                    InvalidateVisual();
                    return true;
                }
                currentY += _categoryHeaderHeight;

                if (category.IsExpanded)
                {
                    foreach (var componentItem in category.Components)
                    {
                        if (HandleComponentItemMouseDown(componentItem, localPoint, ref currentY, point))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Handles mouse down for a component item and its children recursively.
        /// </summary>
        private bool HandleComponentItemMouseDown(ComponentItem componentItem, SKPoint localPoint, ref float currentY, SKPoint mousePoint)
        {
            float indent = componentItem.IsMethod ? 20 : 0;

            // Check for expand/collapse click on classes with methods
            if (componentItem.IsClass && componentItem.ChildItems.Count > 0)
            {
                float arrowX = indent + 12;
                if (localPoint.Y >= currentY && localPoint.Y < currentY + _itemHeight &&
                    localPoint.X >= arrowX - 10 && localPoint.X <= arrowX + 10)
                {
                    componentItem.IsExpanded = !componentItem.IsExpanded;
                    InvalidateVisual();
                    return true;
                }
                indent += 20;
            }

            // Check for component item click
            if (localPoint.Y >= currentY && localPoint.Y < currentY + _itemHeight)
            {
                // Component clicked
                ComponentSelected?.Invoke(this, componentItem);

                // Start drag operation
                _isDragging = true;
                _draggedComponent = componentItem;
                _dragStartPoint = mousePoint;

                var dragArgs = new ComponentDragEventArgs(componentItem, mousePoint, ComponentDragState.Started);
                ComponentDragStarted?.Invoke(this, dragArgs);

                return true;
            }

            currentY += _itemHeight;

            // Check child items if expanded
            if (componentItem.IsClass && componentItem.IsExpanded && componentItem.ChildItems.Count > 0)
            {
                foreach (var childItem in componentItem.ChildItems)
                {
                    if (HandleComponentItemMouseDown(childItem, localPoint, ref currentY, mousePoint))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Handles mouse move events for drag operations.
        /// </summary>
        protected override bool OnMouseMove(SKPoint point, InteractionContext context)
        {
            base.OnMouseMove(point, context);
            _lastMousePoint = point;

            if (_isDragging && _draggedComponent != null)
            {
                var dragArgs = new ComponentDragEventArgs(_draggedComponent, point, ComponentDragState.Dragging);
                ComponentDragging?.Invoke(this, dragArgs);
                return true;
            }
            else
            {
                // Check if mouse is over a component for hover effect
                InvalidateVisual();
                return false;
            }
        }

        /// <summary>
        /// Handles mouse up events to end drag operations.
        /// </summary>
        protected override bool OnMouseUp(SKPoint point, InteractionContext context)
        {
            base.OnMouseUp(point, context);

            if (_isDragging && _draggedComponent != null)
            {
                var dragArgs = new ComponentDragEventArgs(_draggedComponent, point, ComponentDragState.Ended);
                ComponentDragEnded?.Invoke(this, dragArgs);

                _isDragging = false;
                _draggedComponent = null;
                InvalidateVisual();
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Specifies the state of a component drag operation.
    /// </summary>
    public enum ComponentDragState
    {
        /// <summary>
        /// The drag operation has started.
        /// </summary>
        Started,

        /// <summary>
        /// The component is being dragged.
        /// </summary>
        Dragging,

        /// <summary>
        /// The drag operation has ended.
        /// </summary>
        Ended
    }
}
