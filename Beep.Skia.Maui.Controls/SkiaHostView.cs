using System;
using System.Collections.Generic;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;
using Microsoft.Maui.Controls;

namespace Beep.Skia.Maui.Controls
{
    /// <summary>
    /// MAUI host element — cross-platform Skia drawing surface with DrawingManager.
    /// Runs on Android, iOS, macOS Catalyst, and Windows.
    /// </summary>
    public class SkiaHostView : ContentView
    {
        private SKCanvasView _skiaView;
        private DrawingManager _drawingManager;
        private readonly Dictionary<Guid, SkiaComponent> _componentRegistry = new();
        private SKPoint _lastPanPoint;

        /// <summary>
        /// Gets or sets the drawing manager.
        /// </summary>
        public DrawingManager DrawingManager => _drawingManager;

        // Design-time descriptors (shared format with other hosts)
        private SkiaComponentDescriptorCollection _designTimeComponents = new();
        /// <summary>
        /// Gets or sets the design time components.
        /// </summary>
        public SkiaComponentDescriptorCollection DesignTimeComponents => _designTimeComponents;

        /// <summary>
        /// Initializes a new instance of the SkiaHostView class.
        /// </summary>
        public SkiaHostView()
        {
            _drawingManager = new DrawingManager();
            _drawingManager.DrawSurface += (_, _) =>
                MainThread.BeginInvokeOnMainThread(() => _skiaView?.InvalidateSurface());

            _drawingManager.SelectionChanged += (_, _) =>
                MainThread.BeginInvokeOnMainThread(() => _skiaView?.InvalidateSurface());

            _skiaView = new SKCanvasView();
            _skiaView.PaintSurface += OnPaintSurface;
            _skiaView.EnableTouchEvents = true;
            _skiaView.Touch += OnTouch;

            Content = _skiaView;
            BackgroundColor = Colors.White;
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White);
            _drawingManager.Draw(canvas);
        }

        private void OnTouch(object sender, SKTouchEventArgs e)
        {
            var pt = e.Location;
            var canvasPt = new SKPoint(pt.X, pt.Y);

            switch (e.ActionType)
            {
                case SKTouchAction.Pressed:
                    _lastPanPoint = canvasPt;
                    _drawingManager.HandleMouseDown(canvasPt);
                    break;

                case SKTouchAction.Moved:
                    _drawingManager.HandleMouseMove(canvasPt);
                    break;

                case SKTouchAction.Released:
                    _drawingManager.HandleMouseUp(canvasPt);
                    break;

                case SKTouchAction.WheelChanged:
                    _drawingManager.HandleMouseWheel(canvasPt, e.WheelDelta);
                    break;
            }
            e.Handled = true;
        }

        /// <summary>
        /// Instantiates design-time descriptors into real components at runtime.
        /// </summary>
        public void LoadDesignTimeComponents()
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
                    }
                }
                catch { }
            }
            _skiaView?.InvalidateSurface();
        }

        /// <summary>
        /// Gets or sets the create and add component.
        /// </summary>
        public SkiaComponent CreateAndAddComponent(Type type, float x, float y, float w, float h, string name = null)
        {
            if (type == null || Activator.CreateInstance(type) is not SkiaComponent comp) return null;
            comp.X = x; comp.Y = y; comp.Width = w; comp.Height = h;
            comp.Name = name ?? type.Name;
            _drawingManager.AddComponent(comp);
            _componentRegistry[Guid.NewGuid()] = comp;
            _skiaView?.InvalidateSurface();
            return comp;
        }
    }
}
