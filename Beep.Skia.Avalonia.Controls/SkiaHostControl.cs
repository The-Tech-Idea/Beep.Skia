using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;

namespace Beep.Skia.Avalonia.Controls
{
    /// <summary>Event data for an Avalonia Skia surface paint.</summary>
    public class SkiaSurfacePaintEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the SkiaSurfacePaintEventArgs class.
        /// </summary>
        public SkiaSurfacePaintEventArgs(SKCanvas canvas, int width, int height)
        {
            Canvas = canvas;
            Width = width;
            Height = height;
        }

        /// <summary>Canvas to draw on (already cleared to white).</summary>
        public SKCanvas Canvas { get; }

        /// <summary>Pixel width of the surface.</summary>
        public int Width { get; }

        /// <summary>Pixel height of the surface.</summary>
        public int Height { get; }
    }

    /// <summary>
    /// Avalonia control that renders Beep.Skia diagrams.
    ///
    /// SkiaSharp ships no Avalonia view package, so this control renders into an offscreen
    /// <see cref="SKSurface"/> backed by the memory of an Avalonia <see cref="WriteableBitmap"/>
    /// (BGRA8888, premultiplied) and blits it. It therefore depends only on Avalonia + SkiaSharp
    /// and works with the same SkiaSharp version as the rest of the solution.
    /// </summary>
    public class SkiaSurface : Control
    {
        private WriteableBitmap _bitmap;
        private PixelSize _bitmapPixelSize;

        /// <summary>Raised when the surface needs to be redrawn.</summary>
        public event EventHandler<SkiaSurfacePaintEventArgs> PaintSurface;

        /// <summary>
        /// Gets or sets the render.
        /// </summary>
        public override void Render(global::Avalonia.Media.DrawingContext context)
        {
            var size = Bounds.Size;
            if (size.Width < 1 || size.Height < 1) return;

            var scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
            if (scaling <= 0) scaling = 1.0;

            var pixelSize = new PixelSize(
                Math.Max(1, (int)Math.Ceiling(size.Width * scaling)),
                Math.Max(1, (int)Math.Ceiling(size.Height * scaling)));

            if (_bitmap == null || _bitmapPixelSize != pixelSize)
            {
                _bitmap?.Dispose();
                _bitmap = new WriteableBitmap(
                    pixelSize,
                    new Vector(96 * scaling, 96 * scaling),
                    PixelFormat.Bgra8888,
                    AlphaFormat.Premul);
                _bitmapPixelSize = pixelSize;
            }

            using (var framebuffer = _bitmap.Lock())
            {
                var info = new SKImageInfo(
                    framebuffer.Size.Width,
                    framebuffer.Size.Height,
                    SKColorType.Bgra8888,
                    SKAlphaType.Premul);

                using var surface = SKSurface.Create(info, framebuffer.Address, framebuffer.RowBytes);
                if (surface != null)
                {
                    var canvas = surface.Canvas;
                    canvas.Clear(SKColors.White);
                    try { PaintSurface?.Invoke(this, new SkiaSurfacePaintEventArgs(canvas, info.Width, info.Height)); }
                    catch { }
                    canvas.Flush();
                }
            }

            context.DrawImage(_bitmap, new Rect(0, 0, size.Width, size.Height));
        }
    }

    /// <summary>
    /// Avalonia host control for Beep.Skia diagrams. Uses <see cref="SkiaSurface"/> for
    /// cross-platform rendering and forwards pointer input to the DrawingManager.
    /// </summary>
    public class SkiaHostControl : UserControl
    {
        private SkiaSurface _skiaControl;
        private DrawingManager _drawingManager;
        private readonly Dictionary<Guid, SkiaComponent> _componentRegistry = new Dictionary<Guid, SkiaComponent>();

        /// <summary>
        /// Gets or sets the drawing manager.
        /// </summary>
        public DrawingManager DrawingManager => _drawingManager;

        /// <summary>
        /// Gets or sets the design time components.
        /// </summary>
        public SkiaComponentDescriptorCollection DesignTimeComponents { get; set; } = new SkiaComponentDescriptorCollection();

        /// <summary>
        /// Initializes a new instance of the SkiaHostControl class.
        /// </summary>
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

            _skiaControl = new SkiaSurface();
            _skiaControl.PaintSurface += OnPaintSurface;
            _skiaControl.PointerPressed += OnPointerPressed;
            _skiaControl.PointerMoved += OnPointerMoved;
            _skiaControl.PointerReleased += OnPointerReleased;
            _skiaControl.PointerWheelChanged += OnPointerWheel;

            Content = _skiaControl;
        }

        private void OnPaintSurface(object sender, SkiaSurfacePaintEventArgs e)
        {
            e.Canvas.Clear(SKColors.White);
            _drawingManager?.Draw(e.Canvas);
        }

        private static SKPoint ToSKPoint(Point pt) => new SKPoint((float)pt.X, (float)pt.Y);

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
            _skiaControl?.InvalidateVisual();
            return comp;
        }
    }
}
