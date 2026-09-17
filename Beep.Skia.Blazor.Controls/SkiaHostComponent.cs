using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Components;
using SkiaSharp;
using SkiaSharp.Views.Blazor;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;

namespace Beep.Skia.Blazor.Controls
{
    /// <summary>
    /// Blazor WebAssembly host component for Beep.Skia diagrams.
    /// Uses SKCanvasView for WebGL/Canvas2D rendering in the browser.
    /// </summary>
    [SupportedOSPlatform("browser")]
    public class SkiaHostComponent : ComponentBase, IDisposable
    {
        protected SKCanvasView _skiaView;

        [Parameter] public string Width { get; set; } = "100%";
        [Parameter] public string Height { get; set; } = "600px";
        [Parameter] public string CssClass { get; set; } = "skia-host";

        public DrawingManager DrawingManager { get; private set; }
        public SkiaComponentDescriptorCollection DesignTimeComponents { get; set; } = new();

        private readonly Dictionary<Guid, SkiaComponent> _componentRegistry = new();

        protected override void OnInitialized()
        {
            DrawingManager = new DrawingManager();
            DrawingManager.DrawSurface += (_, _) => InvokeAsync(() => _skiaView?.Invalidate());
            DrawingManager.SelectionChanged += (_, _) => InvokeAsync(() => _skiaView?.Invalidate());
        }

        protected void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White);
            DrawingManager?.Draw(canvas);
        }

        protected void OnMouseDown(SKPaintSurfaceEventArgs e)
        {
            DrawingManager?.HandleMouseDown(GetCanvasPoint(e));
        }

        protected void OnMouseMove(SKPaintSurfaceEventArgs e)
        {
            DrawingManager?.HandleMouseMove(GetCanvasPoint(e));
        }

        protected void OnMouseUp(SKPaintSurfaceEventArgs e)
        {
            DrawingManager?.HandleMouseUp(GetCanvasPoint(e));
        }

        private SKPoint GetCanvasPoint(SKPaintSurfaceEventArgs e)
        {
            return new SKPoint(e.Info.Width / 2f, e.Info.Height / 2f);
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
                        DrawingManager.AddComponent(comp);
                    }
                }
                catch { }
            }
            InvokeAsync(() => _skiaView?.Invalidate());
        }

        public void Dispose()
        {
            DrawingManager?.ClearComponents();
        }
    }
}
