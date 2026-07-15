using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using SkiaSharp;
using SkiaSharp.Views.Avalonia;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;

namespace Beep.Skia.Avalonia.Controls
{
    /// <summary>
    /// Avalonia host control for Beep.Skia diagrams.
    /// Uses SKControl from SkiaSharp.Views.Avalonia for cross-platform rendering.
    /// </summary>
    public class SkiaHostControl : UserControl
    {
        private SKControl _skiaControl;
        private DrawingManager _drawingManager;
        private readonly Dictionary<Guid, SkiaComponent> _componentRegistry = new();

        public DrawingManager DrawingManager => _drawingManager;

        public SkiaComponentDescriptorCollection DesignTimeComponents { get; set; } = new();

        public SkiaHostControl()
        {
            ClipToBounds = true;
            Focusable = true;
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            _drawingManager = new DrawingManager();
            _drawingManager.DrawSurface += (_, _) =>
                Dispatcher.UIThread.InvokeAsync(() => _skiaControl?.InvalidateVisual());

            _drawingManager.SelectionChanged += (_, _) =>
                Dispatcher.UIThread.InvokeAsync(() => _skiaControl?.InvalidateVisual());

            _skiaControl = new SKControl();
            _skiaControl.PaintSurface += OnPaintSurface;
            _skiaControl.PointerPressed += OnPointerPressed;
            _skiaControl.PointerMoved += OnPointerMoved;
            _skiaControl.PointerReleased += OnPointerReleased;
            _skiaControl.PointerWheelChanged += OnPointerWheel;

            Content = _skiaControl;
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White);
            _drawingManager?.Draw(canvas);
        }

        private SKPoint ToSKPoint(Point pt) => new SKPoint((float)pt.X, (float)pt.Y);

        private void OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            var pt = e.GetPosition(_skiaControl);
            _drawingManager?.HandleMouseDown(ToSKPoint(pt));
            e.Handled = true;
        }

        private void OnPointerMoved(object sender, PointerEventArgs e)
        {
            var pt = e.GetPosition(_skiaControl);
            _drawingManager?.HandleMouseMove(ToSKPoint(pt));
        }

        private void OnPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            var pt = e.GetPosition(_skiaControl);
            _drawingManager?.HandleMouseUp(ToSKPoint(pt));
            e.Handled = true;
        }

        private void OnPointerWheel(object sender, PointerWheelEventArgs e)
        {
            var pt = e.GetPosition(_skiaControl);
            _drawingManager?.HandleMouseWheel(ToSKPoint(pt), (float)(e.Delta.Y * 120));
        }

        /// <summary>
        /// Instantiates design-time descriptors into real components.
        /// </summary>
        public void LoadDesignTimeComponents()
        {
            foreach (var desc in DesignTimeComponents)
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
                    }
                }
                catch { }
            }
            _skiaControl?.InvalidateVisual();
        }

        public SkiaComponent CreateAndAddComponent(Type type, float x, float y, float w, float h, string name = null)
        {
            if (type == null || Activator.CreateInstance(type) is not SkiaComponent comp) return null;
            comp.X = x; comp.Y = y; comp.Width = w; comp.Height = h;
            comp.Name = name ?? type.Name;
            _drawingManager.AddComponent(comp);
            _componentRegistry[Guid.NewGuid()] = comp;
            _skiaControl?.InvalidateVisual();
            return comp;
        }
    }
}
