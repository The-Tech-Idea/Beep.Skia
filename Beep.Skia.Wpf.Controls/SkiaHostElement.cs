using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;

namespace Beep.Skia.Wpf.Controls
{
    /// <summary>
    /// WPF host element that provides a Skia drawing surface with a DrawingManager for hosting Skia components.
    /// Mirrors the API surface of SkiaHostControl (WinForms) for cross-platform parity.
    /// </summary>
    public class SkiaHostElement : FrameworkElement
    {
        private SKElement _skElement;
        private DrawingManager _drawingManager;
        private Palette _palette;
        private ComponentPropertyEditor _propertyEditor;
        private readonly Dictionary<Guid, SkiaComponent> _componentRegistry = new();

        [Category("Behavior"), DefaultValue(false)]
        [Description("Center the component on the drop point.")]
        public bool CenterOnDrop { get; set; } = false;

        [Category("Behavior"), DefaultValue(true)]
        [Description("Allow picking up and moving existing components.")]
        public bool AllowComponentDragging { get; set; } = true;

        public DrawingManager DrawingManager => _drawingManager;

        // Design-time component descriptors (shared format with WinForms host)
        private SkiaComponentDescriptorCollection _designTimeComponents = new();
        public SkiaComponentDescriptorCollection DesignTimeComponents
        {
            get => _designTimeComponents;
            set { _designTimeComponents = value ?? new SkiaComponentDescriptorCollection(); }
        }

        public SkiaHostElement()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            ClipToBounds = true;
            Focusable = true;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _drawingManager = new DrawingManager();
            _drawingManager.DrawSurface += (_, _) => _skElement?.InvalidateVisual();

            _drawingManager.SelectionChanged += (_, _) =>
            {
                var sm = _drawingManager.SelectionManager;
                if (_propertyEditor != null)
                {
                    if (sm.SelectedLines?.Count == 1)
                        _propertyEditor.SelectedLine = sm.SelectedLines[0];
                    else if (sm.SelectedComponents?.Count == 1)
                        _propertyEditor.SelectedComponent = sm.SelectedComponents[0];
                    else
                    {
                        _propertyEditor.SelectedLine = null;
                        _propertyEditor.SelectedComponent = null;
                    }
                }
                _skElement?.InvalidateVisual();
            };

            // Palette
            _palette = new Palette { X = 8, Y = 40 };
            _palette.MaxHeight = Math.Max(120, (float)ActualHeight - 80);
            this.SizeChanged += (_, _) =>
            {
                if (_palette != null)
                {
                    _palette.MaxHeight = Math.Max(120, (float)ActualHeight - 80);
                    _palette.RefreshLayout();
                    _skElement?.InvalidateVisual();
                }
            };

            PopulatePalette();
            _drawingManager.AddComponent(_palette);

            // Property editor
            _propertyEditor = new ComponentPropertyEditor { X = Math.Max(8, (float)ActualWidth - 320), Width = 300 };
            _propertyEditor.Height = Math.Max(200, (float)ActualHeight - 80);
            _propertyEditor.PropertyValueChanged += (_, _) => _skElement?.InvalidateVisual();
            _drawingManager.AddComponent(_propertyEditor);

            // SKElement
            _skElement = new SKElement();
            _skElement.PaintSurface += OnPaintSurface;
            _skElement.MouseDown += OnSkMouseDown;
            _skElement.MouseMove += OnSkMouseMove;
            _skElement.MouseUp += OnSkMouseUp;
            _skElement.MouseWheel += OnSkMouseWheel;
            _skElement.KeyDown += OnSkKeyDown;

            AddVisualChild(_skElement);
            AddLogicalChild(_skElement);

            // Instantiate design-time components
            InstantiateDesignTimeComponents();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _componentRegistry.Clear();
            _drawingManager?.ClearComponents();
        }

        private void PopulatePalette()
        {
            try
            {
                SkiaComponentRegistry.DiscoverAndRegisterDomainComponents();
                foreach (var def in SkiaComponentRegistry.GetComponentsOrdered())
                {
                    var display = def.className ?? def.type?.Name ?? def.dllname ?? "Unknown";
                    var compType = def.type?.AssemblyQualifiedName ?? def.className ?? string.Empty;
                    if (!string.IsNullOrEmpty(compType))
                        _palette.AddItem(new PaletteItem { Name = display, ComponentType = compType });
                }
            }
            catch { }
        }

        private void InstantiateDesignTimeComponents()
        {
            foreach (var desc in _designTimeComponents)
            {
                try
                {
                    var type = Type.GetType(desc.ComponentType, throwOnError: false);
                    if (type != null && Activator.CreateInstance(type) is SkiaComponent comp)
                    {
                        comp.X = desc.X; comp.Y = desc.Y;
                        comp.Width = desc.Width; comp.Height = desc.Height;
                        comp.Name = desc.Name ?? string.Empty;
                        _drawingManager.AddComponent(comp);
                        _componentRegistry[Guid.NewGuid()] = comp;
                    }
                }
                catch { }
            }
        }

        // Drawing
        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            _drawingManager.Draw(e.Surface.Canvas);
        }

        // Mouse
        private void OnSkMouseDown(object sender, MouseButtonEventArgs e)
        {
            var pt = ToCanvasPoint(e.GetPosition(_skElement));
            int btn = e.ChangedButton == System.Windows.Input.MouseButton.Left ? 0 : e.ChangedButton == System.Windows.Input.MouseButton.Right ? 1 : 2;
            _drawingManager.HandleMouseDown(pt, SKKeyModifiers.None, btn);
            this.Focus();
        }

        private void OnSkMouseMove(object sender, MouseEventArgs e)
        {
            var pt = ToCanvasPoint(e.GetPosition(_skElement));
            _drawingManager.HandleMouseMove(pt, SKKeyModifiers.None);
        }

        private void OnSkMouseUp(object sender, MouseButtonEventArgs e)
        {
            var pt = ToCanvasPoint(e.GetPosition(_skElement));
            int btn = e.ChangedButton == System.Windows.Input.MouseButton.Left ? 0 : e.ChangedButton == System.Windows.Input.MouseButton.Right ? 1 : 2;
            _drawingManager.HandleMouseUp(pt, SKKeyModifiers.None, btn);
        }

        private void OnSkMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var pt = ToCanvasPoint(e.GetPosition(_skElement));
            _drawingManager.HandleMouseWheel(pt, e.Delta);
        }

        private void OnSkKeyDown(object sender, KeyEventArgs e)
        {
            int mods = 0;
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) mods |= 1;
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) mods |= 2;
            if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)) mods |= 4;

            int winKey = KeyInterop.VirtualKeyFromKey(e.Key);
            if (_drawingManager.HandleKeyDown(winKey, mods))
            {
                e.Handled = true;
            }
        }

        private SKPoint ToCanvasPoint(Point wpfPoint)
        {
            return new SKPoint((float)wpfPoint.X, (float)wpfPoint.Y);
        }

        // Component creation (public API)
        public SkiaComponent CreateAndAddComponent(Type type, float x, float y, float w, float h, string name = null)
        {
            if (type == null) return null;
            try
            {
                if (Activator.CreateInstance(type) is SkiaComponent comp)
                {
                    comp.X = x; comp.Y = y; comp.Width = w; comp.Height = h;
                    comp.Name = name ?? type.Name;
                    _drawingManager.AddComponent(comp);
                    _componentRegistry[Guid.NewGuid()] = comp;
                    return comp;
                }
            }
            catch { }
            return null;
        }

        // WPF visual tree
        protected override int VisualChildrenCount => 1;
        protected override System.Windows.Media.Visual GetVisualChild(int index) => _skElement;

        protected override Size MeasureOverride(Size availableSize)
        {
            _skElement?.Measure(availableSize);
            return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _skElement?.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
            return finalSize;
        }
    }
}
