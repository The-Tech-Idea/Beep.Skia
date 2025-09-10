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
    private float _verticalPadding = 8; // top+bottom padding
    private float _horizontalPadding = 8; // left+right padding
    private bool _autoSize = true;      // dynamic height
    private bool _autoWidth = true;     // dynamic width
    private float? _maxHeight = null;   // limit height and enable scrolling
    private float _scrollOffset = 0f;   // vertical scroll offset in pixels
    private bool _scrollThumbDragging = false;
    private float _scrollDragStartY;
    private float _scrollStartOffset;
    private SKRect _lastThumbRect;      // cached for hit-test
    private float _cachedContentTotalHeight; // total height of all items

        // drag state
        private int _dragIndex = -1;
        private bool _isDragging = false;
        private SKPoint _dragStartPoint;
        private SKPoint _dragCurrentPoint;
        private const float DragThreshold = 4f;

        public Palette()
        {
            Width = 160;
            Height = 400; // initial; will shrink/grow if _autoSize
            X = 8;
            Y = 40;
        }

        public bool AutoHeight
        {
            get => _autoSize;
            set => _autoSize = value;
        }

        public bool AutoWidth
        {
            get => _autoWidth;
            set => _autoWidth = value;
        }

        public float? MaxHeight
        {
            get => _maxHeight;
            set
            {
                _maxHeight = value;
                if (value.HasValue && Height > value.Value) Height = value.Value;
                ClampScroll();
                UpdateBounds();
            }
        }

        protected override void UpdateChildren(DrawingContext context)
        {
            // Palette manages its own hit interaction; no children
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            if (_autoSize) RecalculateHeight();
            if (_autoWidth) RecalculateWidth(canvas);
            ApplyMaxHeightConstraint();
            ClampScroll();
            using var bg = new SKPaint { Color = new SKColor(240, 240, 240), Style = SKPaintStyle.Fill };
            var outer = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRect(outer, bg);

            using var textPaint = new SKPaint { Color = SKColors.Black }; // for glyph rendering
            using var font = new SKFont { Size = 14 };
            // Compute visible range based on scroll
            _cachedContentTotalHeight = Items.Count * _itemHeight + _verticalPadding * 2;
            var firstVisibleIndex = (int)Math.Floor((_scrollOffset) / _itemHeight);
            var visibleItemCapacity = (int)Math.Ceiling((Height - _verticalPadding * 2) / _itemHeight) + 1;
            if (firstVisibleIndex < 0) firstVisibleIndex = 0;
            int lastVisibleIndex = Math.Min(Items.Count - 1, firstVisibleIndex + visibleItemCapacity);

            // Clip to palette region
            canvas.Save();
            canvas.ClipRect(outer);

            for (int i = firstVisibleIndex; i <= lastVisibleIndex; i++)
            {
                var itemTopGlobal = Y + _verticalPadding + i * _itemHeight - _scrollOffset;
                var rect = new SKRect(
                    X + _horizontalPadding,
                    itemTopGlobal + 4,
                    X + Width - _horizontalPadding,
                    itemTopGlobal + _itemHeight - 4);
                using var itemBg = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill };
                canvas.DrawRect(rect, itemBg);
                canvas.DrawText(Items[i].Name, rect.Left + 8, rect.MidY + 6, SKTextAlign.Left, font, textPaint);
            }
            canvas.Restore();

            DrawScrollbar(canvas);

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

    // (Old non-scrolling HandleMouse* methods removed; new scrolling-aware versions exist later in file.)

        private void Log(string message)
        {
            try
            {
                var lp = Path.Combine(Path.GetTempPath(), "beepskia_palette.log");
                File.AppendAllText(lp, DateTime.UtcNow.ToString("o") + " " + message + Environment.NewLine, Encoding.UTF8);
            }
            catch { }
        }

        private void RecalculateHeight()
        {
            var desired = _verticalPadding * 2 + Items.Count * _itemHeight;
            if (_maxHeight.HasValue && desired > _maxHeight.Value)
            {
                desired = _maxHeight.Value; // we will scroll
            }
            if (Math.Abs(desired - Height) > 0.1f)
            {
                Height = desired;
                UpdateBounds();
            }
        }

        // Public helper to force refresh when external code adds/removes items
        public void RefreshLayout()
        {
            if (_autoSize) RecalculateHeight();
            if (_autoWidth) RecalculateWidth();
            UpdateBounds();
        }

        // Convenience add/remove wrappers to auto-size
        public void AddItem(PaletteItem item)
        {
            Items.Add(item);
            if (_autoSize) RecalculateHeight();
            if (_autoWidth) RecalculateWidth();
            UpdateBounds();
        }

        public bool RemoveItem(PaletteItem item)
        {
            var removed = Items.Remove(item);
            if (removed && _autoSize) RecalculateHeight();
            if (removed && _autoWidth) RecalculateWidth();
            if (removed) UpdateBounds();
            return removed;
        }

    private void RecalculateWidth(SKCanvas measurementCanvas = null)
        {
            if (!_autoWidth) return;
            using var font = new SKFont { Size = 14 };
            float maxText = 0f;
            foreach (var it in Items)
            {
                var tb = new SKRect();
                font.MeasureText(it.Name ?? string.Empty, out tb);
                if (tb.Width > maxText) maxText = tb.Width;
            }
            // Base width: text + icon spacing + padding
            var desired = _horizontalPadding * 2 + 16 + maxText + 16; // some extra padding
            if (Math.Abs(desired - Width) > 0.1f)
            {
                Width = desired;
                UpdateBounds();
            }
        }

        private void ApplyMaxHeightConstraint()
        {
            if (_maxHeight.HasValue && Height > _maxHeight.Value)
            {
                Height = _maxHeight.Value;
                UpdateBounds();
            }
        }

        private void ClampScroll()
        {
            var full = Items.Count * _itemHeight + _verticalPadding * 2;
            if (!_maxHeight.HasValue || full <= Height)
            {
                _scrollOffset = 0;
                return;
            }
            var maxScroll = full - Height;
            if (_scrollOffset < 0) _scrollOffset = 0;
            if (_scrollOffset > maxScroll) _scrollOffset = maxScroll;
        }

        private void DrawScrollbar(SKCanvas canvas)
        {
            if (!_maxHeight.HasValue) return;
            var total = Items.Count * _itemHeight + _verticalPadding * 2;
            if (total <= Height + 0.5f) return; // no need

            float trackWidth = 8f;
            float trackPadding = 2f;
            var trackRect = new SKRect(
                X + Width - trackWidth - trackPadding,
                Y + trackPadding,
                X + Width - trackPadding,
                Y + Height - trackPadding);

            using var trackPaint = new SKPaint { Color = new SKColor(0, 0, 0, 30), Style = SKPaintStyle.Fill };
            canvas.DrawRoundRect(trackRect, 4, 4, trackPaint);

            float visibleRatio = Height / total;
            float thumbHeight = Math.Max(20f, trackRect.Height * visibleRatio);
            float maxScroll = total - Height;
            float scrollRatio = maxScroll <= 0 ? 0 : _scrollOffset / maxScroll;
            float thumbY = trackRect.Top + (trackRect.Height - thumbHeight) * scrollRatio;
            var thumbRect = new SKRect(trackRect.Left + 1, thumbY, trackRect.Right - 1, thumbY + thumbHeight);
            using var thumbPaint = new SKPaint { Color = new SKColor(80, 80, 80, 140), Style = SKPaintStyle.Fill };
            canvas.DrawRoundRect(thumbRect, 4, 4, thumbPaint);
            _lastThumbRect = thumbRect;
        }

        public override bool HandleMouseDown(SKPoint point, InteractionContext context)
        {
            // Scroll thumb hit-test first
            if (_lastThumbRect.Contains(point))
            {
                _scrollThumbDragging = true;
                _scrollDragStartY = point.Y;
                _scrollStartOffset = _scrollOffset;
                return true;
            }

            // Adjust for scroll when detecting item index
            var handled = base.HandleMouseDown(point, context);
            if (!handled)
            {
                if (Bounds.Contains(point.X, point.Y))
                {
                    var localYAdj = point.Y - Y + _scrollOffset - _verticalPadding;
                    var idx = (int)(localYAdj / _itemHeight);
                    if (idx >= 0 && idx < Items.Count)
                    {
                        _dragIndex = idx;
                        _isDragging = true;
                        _dragStartPoint = point;
                        _dragCurrentPoint = point;
                        return true;
                    }
                }
            }
            return handled;
        }

        public override bool HandleMouseMove(SKPoint point, InteractionContext context)
        {
            if (_scrollThumbDragging)
            {
                var total = Items.Count * _itemHeight + _verticalPadding * 2;
                if (_maxHeight.HasValue && total > Height)
                {
                    float trackHeight = Height - 4; // minus padding used in DrawScrollbar
                    float thumbTravel = trackHeight - Math.Max(20f, trackHeight * (Height / total));
                    if (thumbTravel < 1) thumbTravel = 1;
                    float dy = point.Y - _scrollDragStartY;
                    float ratio = dy / thumbTravel;
                    float maxScroll = total - Height;
                    _scrollOffset = _scrollStartOffset + ratio * maxScroll;
                    ClampScroll();
                }
                return true;
            }
            if (_isDragging && _dragIndex >= 0)
            {
                _dragCurrentPoint = point;
                return true;
            }
            return base.HandleMouseMove(point, context);
        }

        public override bool HandleMouseUp(SKPoint point, InteractionContext context)
        {
            if (_scrollThumbDragging)
            {
                _scrollThumbDragging = false;
                return true;
            }
            if (_isDragging)
            {
                try
                {
                    var idx = _dragIndex;
                    if (idx >= 0 && idx < Items.Count)
                    {
                        var movedDist = Math.Abs(point.X - _dragStartPoint.X) + Math.Abs(point.Y - _dragStartPoint.Y);
                        var item = Items[idx];
                        if (movedDist >= DragThreshold)
                        {
                            // Treat as a drop. Emit ItemDropped with current point as canvas coordinates.
                            ItemDropped?.Invoke(this, (item, point));
                        }
                        else
                        {
                            // Treat as simple activation (click without drag)
                            ItemActivated?.Invoke(this, item);
                        }
                    }
                }
                catch { }
                _isDragging = false;
                _dragIndex = -1;
                return true;
            }
            return base.HandleMouseUp(point, context);
        }

        public void ScrollBy(float deltaPixels)
        {
            _scrollOffset += deltaPixels;
            ClampScroll();
        }
    }
}
