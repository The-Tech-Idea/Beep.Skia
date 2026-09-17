using SkiaSharp;
using System;
using Beep.Skia.Model;

namespace Beep.Skia.Components
{
    /// <summary>
    /// Diagram minimap: a scaled overview of all components and connection lines with a
    /// viewport rectangle showing the currently visible world area (from pan/zoom).
    /// Intended to be added as a static overlay component.
    /// </summary>
    public class MinimapControl : MaterialControl
    {
        /// <summary>Drawing manager providing the diagram content and viewport.</summary>
        public DrawingManager Manager { get; set; }

        /// <summary>Padding inside the minimap frame.</summary>
        public float MinimapPadding { get; set; } = 6f;

        /// <summary>Draws the viewport rectangle when true.</summary>
        public bool ShowViewport { get; set; } = true;

        public SKColor MinimapBackground { get; set; } = new SKColor(0xFA, 0xFA, 0xFA);
        public SKColor ComponentFill { get; set; } = new SKColor(0x90, 0xCA, 0xF9);
        public SKColor ComponentStroke { get; set; } = new SKColor(0x42, 0x42, 0x42);
        public SKColor ViewportStroke { get; set; } = new SKColor(0xE5, 0x39, 0x35);

        public MinimapControl()
        {
            Name = "Minimap";
            Width = 200;
            Height = 150;
            ShowInPalette = false;
            IsStatic = true;
        }

        /// <summary>World-space bounds of the diagram content (minimum 1x1).</summary>
        public SKRect CalculateWorldBounds()
        {
            if (Manager == null) return new SKRect(0, 0, 1, 1);

            var bounds = Manager.GetContentBounds(padding: 0f);
            if (bounds.Width <= 0.01f || bounds.Height <= 0.01f)
                return new SKRect(bounds.Left, bounds.Top, bounds.Left + 1f, bounds.Top + 1f);
            return bounds;
        }

        /// <summary>Scale factor mapping world units to minimap pixels.</summary>
        public float CalculateScale()
        {
            var world = CalculateWorldBounds();
            float usableWidth = Math.Max(1f, Width - MinimapPadding * 2f);
            float usableHeight = Math.Max(1f, Height - MinimapPadding * 2f);
            return Math.Min(usableWidth / world.Width, usableHeight / world.Height);
        }

        /// <summary>Maps a world-space point to canvas coordinates inside the minimap.</summary>
        public SKPoint MapWorldToMinimap(SKPoint world)
        {
            var bounds = CalculateWorldBounds();
            float scale = CalculateScale();
            float contentWidth = bounds.Width * scale;
            float contentHeight = bounds.Height * scale;
            float offsetX = X + MinimapPadding + (Width - MinimapPadding * 2f - contentWidth) / 2f;
            float offsetY = Y + MinimapPadding + (Height - MinimapPadding * 2f - contentHeight) / 2f;

            return new SKPoint(
                offsetX + (world.X - bounds.Left) * scale,
                offsetY + (world.Y - bounds.Top) * scale);
        }

        /// <summary>
        /// World-space rectangle currently visible, derived from pan/zoom and the bound canvas.
        /// Returns an empty rect when no canvas is attached.
        /// </summary>
        public SKRect CalculateViewportWorldRect()
        {
            if (Manager?.Canvas == null) return SKRect.Empty;

            float zoom = Manager.Zoom <= 0f ? 1f : Manager.Zoom;
            var clip = Manager.Canvas.DeviceClipBounds;
            if (clip.Width <= 0 || clip.Height <= 0) return SKRect.Empty;

            float left = -Manager.PanOffset.X / zoom;
            float top = -Manager.PanOffset.Y / zoom;
            return new SKRect(left, top, left + clip.Width / zoom, top + clip.Height / zoom);
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            var outer = new SKRect(X, Y, X + Width, Y + Height);

            using var background = new SKPaint { Color = MinimapBackground, Style = SKPaintStyle.Fill, IsAntialias = true };
            canvas.DrawRoundRect(outer, 6, 6, background);
            using var border = new SKPaint { Color = MaterialColors.Outline, Style = SKPaintStyle.Stroke, StrokeWidth = 1f, IsAntialias = true };
            canvas.DrawRoundRect(outer, 6, 6, border);

            var components = Manager?.GetComponents();
            if (components == null || components.Count == 0)
            {
                using var emptyFont = new SKFont(SKTypeface.Default, 10);
                using var emptyPaint = new SKPaint { Color = MaterialColors.OnSurfaceVariant, IsAntialias = true };
                canvas.DrawText("(empty)", outer.MidX, outer.MidY, SKTextAlign.Center, emptyFont, emptyPaint);
                return;
            }

            using var fill = new SKPaint { Color = ComponentFill, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = ComponentStroke, Style = SKPaintStyle.Stroke, StrokeWidth = 0.8f, IsAntialias = true };

            foreach (var component in components)
            {
                if (component == null || component.IsStatic) continue;

                var topLeft = MapWorldToMinimap(new SKPoint(component.X, component.Y));
                var bottomRight = MapWorldToMinimap(new SKPoint(component.X + component.Width, component.Y + component.Height));
                var rect = new SKRect(
                    topLeft.X,
                    topLeft.Y,
                    Math.Max(topLeft.X + 1f, bottomRight.X),
                    Math.Max(topLeft.Y + 1f, bottomRight.Y));

                canvas.DrawRect(rect, fill);
                canvas.DrawRect(rect, stroke);
            }

            var lines = Manager?.GetLines();
            if (lines != null && lines.Count > 0)
            {
                using var linePaint = new SKPaint { Color = MaterialColors.OutlineVariant, Style = SKPaintStyle.Stroke, StrokeWidth = 0.7f, IsAntialias = true };
                foreach (var line in lines)
                {
                    var start = line?.Start?.Position ?? default;
                    var end = line?.End?.Position ?? default;
                    canvas.DrawLine(MapWorldToMinimap(start), MapWorldToMinimap(end), linePaint);
                }
            }

            if (ShowViewport)
            {
                var viewport = CalculateViewportWorldRect();
                if (viewport.Width > 0 && viewport.Height > 0)
                {
                    var topLeft = MapWorldToMinimap(new SKPoint(viewport.Left, viewport.Top));
                    var bottomRight = MapWorldToMinimap(new SKPoint(viewport.Right, viewport.Bottom));
                    using var viewportPaint = new SKPaint { Color = ViewportStroke, Style = SKPaintStyle.Stroke, StrokeWidth = 1.4f, IsAntialias = true };
                    canvas.DrawRect(new SKRect(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y), viewportPaint);
                }
            }
        }
    }
}
