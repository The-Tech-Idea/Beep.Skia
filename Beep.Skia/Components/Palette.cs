using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Beep.Skia.Components
{
    public class PaletteItem
    {
        public string Name { get; set; }
        public string ComponentType { get; set; }
    }

    public class Palette : SkiaComponent
    {
        public List<PaletteItem> Items { get; } = new List<PaletteItem>();

        public event EventHandler<PaletteItem>? ItemActivated;
        // Raised when an item is dragged and dropped somewhere in the canvas
    public event EventHandler<(PaletteItem Item, SKPoint DropPoint)> ItemDropped;

        private float _itemHeight = 32;

        // drag state
        private int _dragIndex = -1;
        private bool _isDragging = false;
        private SKPoint _dragStartPoint;
        private SKPoint _dragCurrentPoint;
        private const float DragThreshold = 4f;

        public Palette()
        {
            Width = 160;
            Height = 400;
            X = 8;
            Y = 40;
        }

        protected override void UpdateChildren(DrawingContext context)
        {
            // Palette manages its own hit interaction; no children
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            using var bg = new SKPaint { Color = new SKColor(240, 240, 240), Style = SKPaintStyle.Fill };
            canvas.DrawRect(Bounds, bg);

            using var textPaint = new SKPaint { Color = SKColors.Black }; // for glyph rendering
            using var font = new SKFont { Size = 14 };

            for (int i = 0; i < Items.Count; i++)
            {
                var top = Y + i * _itemHeight;
                var rect = new SKRect(X + 4, top + 4, X + Width - 4, top + _itemHeight - 4);
                using var itemBg = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill };
                canvas.DrawRect(rect, itemBg);
                canvas.DrawText(Items[i].Name, rect.Left + 8, rect.MidY + 6, SKTextAlign.Left, font, textPaint);
            }

            // draw ghost if dragging
            if (_isDragging && _dragIndex >= 0 && _dragIndex < Items.Count)
            {
                var item = Items[_dragIndex];
                var w = 120f;
                var h = 36f;
                var rect = new SKRect(_dragCurrentPoint.X, _dragCurrentPoint.Y, _dragCurrentPoint.X + w, _dragCurrentPoint.Y + h);
                using var ghost = new SKPaint { Color = new SKColor(100, 149, 237, 150), Style = SKPaintStyle.Fill };
                canvas.DrawRect(rect, ghost);
                using var gText = new SKPaint { Color = SKColors.White };
                using var gFont = new SKFont { Size = 14 };
                canvas.DrawText(item.Name, rect.Left + 8, rect.MidY + 6, SKTextAlign.Left, gFont, gText);
            }
        }

        public override bool HandleMouseDown(SKPoint point, InteractionContext context)
        {
            Log("HandleMouseDown entry: " + point);
            if (!Bounds.Contains(point.X, point.Y)) return false;
            var localY = point.Y - Y;
            var idx = (int)(localY / _itemHeight);
            if (idx >= 0 && idx < Items.Count)
            {
                _dragIndex = idx;
                _isDragging = true;
                _dragStartPoint = point;
                _dragCurrentPoint = point;
                Log($"HandleMouseDown captured item idx={idx} name={Items[idx].Name}");
                return true; // capture the press
            }
            return false;
        }

        public override bool HandleMouseMove(SKPoint point, InteractionContext context)
        {
            if (_isDragging && _dragIndex >= 0)
            {
                // update current drag point
                _dragCurrentPoint = point;
                Log($"HandleMouseMove dragging idx={_dragIndex} pt={point}");
                // continue consuming the event
                return true;
            }
            return false;
        }

        public override bool HandleMouseUp(SKPoint point, InteractionContext context)
        {
            if (_isDragging && _dragIndex >= 0)
            {
                // If the mouse didn't move much, treat as activate
                var dx = point.X - _dragStartPoint.X;
                var dy = point.Y - _dragStartPoint.Y;
                var dist2 = dx * dx + dy * dy;
                var item = Items[_dragIndex];
                _isDragging = false;
                var idx = _dragIndex;
                _dragIndex = -1;

                Log($"HandleMouseUp idx={idx} pt={point} dist2={dist2}");

                if (dist2 <= DragThreshold * DragThreshold)
                {
                    Log($"ItemActivated idx={idx} name={item.Name}");
                    ItemActivated?.Invoke(this, item);
                    return true;
                }

                // If released outside palette bounds, raise drop event with global canvas point
                if (!Bounds.Contains(point.X, point.Y))
                {
                    Log($"ItemDropped idx={idx} name={item.Name} dropPt={point}");
                    ItemDropped?.Invoke(this, (item, point));
                    return true;
                }

                return true;
            }
            return false;
        }

        private void Log(string message)
        {
            try
            {
                var lp = Path.Combine(Path.GetTempPath(), "beepskia_palette.log");
                File.AppendAllText(lp, DateTime.UtcNow.ToString("o") + " " + message + Environment.NewLine, Encoding.UTF8);
            }
            catch { }
        }
    }
}
