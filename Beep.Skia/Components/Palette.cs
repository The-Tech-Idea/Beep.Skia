using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Beep.Skia.Model;
namespace Beep.Skia.Components
{
    public class PaletteCategory
    {
        public string Name { get; set; } = "General";
        public bool IsCollapsed { get; set; } = false;
        public List<PaletteItem> Items { get; } = new List<PaletteItem>();
        // Runtime layout cache
        internal SKRect HeaderRect;
    }

    public class PaletteItem
    {
        public string Name { get; set; }
        public string ComponentType { get; set; }
        public string Category { get; set; } = "General";
        // Optional preset for connection line multiplicities (used for ERD quick connects)
        public ERDMultiplicity? StartMultiplicity { get; set; }
        public ERDMultiplicity? EndMultiplicity { get; set; }
        // Special item if ComponentType == null and either multiplicity set: acts as a preset tool
    }

    public class Palette : SkiaComponent
    {
        // Flat list for backward compatibility (populate Categories from this)
        public List<PaletteItem> Items { get; } = new List<PaletteItem>();
        // Grouped categories used for rendering/interaction
        public List<PaletteCategory> Categories { get; } = new List<PaletteCategory>();

    public event EventHandler<PaletteItem> ItemActivated;
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

    // hover state
    private int _hoverHeaderIndex = -1;       // index into Categories
    private int _hoverItemGlobalIndex = -1;   // index into flat Items list

        // Category rendering metrics
        private const float CategoryHeaderHeight = 26f;
        private const float CategoryHeaderPaddingX = 8f;
        private const float CategoryChevronSize = 10f;

        public Palette()
        {
            Width = 160;
            Height = 400; // initial; will shrink/grow if _autoSize
            X = 8;
            Y = 40;
            // Palette itself should not be selectable — only its items are interactable
            ShowInPalette = false; // hide from other palettes if any
            IsStatic = true; // palette should not move with canvas dragging
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
            EnsureCategories();

            // Compute total content height considering categories and collapsed states
            _cachedContentTotalHeight = CalculateTotalContentHeight();

            // Clip to palette region
            canvas.Save();
            canvas.ClipRect(outer);

            float cursorY = Y + _verticalPadding - _scrollOffset;
            int headerIdx = 0;
            foreach (var cat in Categories)
            {
                // Draw category header
                var headerRect = new SKRect(
                    X + _horizontalPadding,
                    cursorY,
                    X + Width - _horizontalPadding,
                    cursorY + CategoryHeaderHeight);
                cat.HeaderRect = headerRect;
                var isHeaderHover = headerIdx == _hoverHeaderIndex;
                var headerColor = isHeaderHover ? new SKColor(200, 200, 200) : new SKColor(220, 220, 220);
                using (var headerBg = new SKPaint { Color = headerColor, Style = SKPaintStyle.Fill })
                {
                    canvas.DrawRect(headerRect, headerBg);
                }
                if (isHeaderHover)
                {
                    using var headerBorder = new SKPaint { Color = new SKColor(120, 120, 120), Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
                    canvas.DrawRect(headerRect, headerBorder);
                }
                // Chevron
                using (var chevronPaint = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Stroke, StrokeWidth = 2 })
                {
                    float cx = headerRect.Left + CategoryHeaderPaddingX;
                    float cy = headerRect.MidY;
                    if (cat.IsCollapsed)
                    {
                        // right-pointing
                        canvas.DrawLine(cx, cy - CategoryChevronSize / 2, cx + CategoryChevronSize, cy, chevronPaint);
                        canvas.DrawLine(cx + CategoryChevronSize, cy, cx, cy + CategoryChevronSize / 2, chevronPaint);
                    }
                    else
                    {
                        // down-pointing
                        canvas.DrawLine(cx, cy - CategoryChevronSize / 2, cx + CategoryChevronSize / 2, cy + CategoryChevronSize / 2, chevronPaint);
                        canvas.DrawLine(cx + CategoryChevronSize / 2, cy + CategoryChevronSize / 2, cx + CategoryChevronSize, cy - CategoryChevronSize / 2, chevronPaint);
                    }
                }
                // Header text
                canvas.DrawText(cat.Name ?? "", headerRect.Left + CategoryHeaderPaddingX + 14, headerRect.MidY + 5, SKTextAlign.Left, font, textPaint);
                cursorY += CategoryHeaderHeight;

                if (cat.IsCollapsed)
                {
                    headerIdx++;
                    continue;
                }

                // Draw items in this category
                foreach (var it in cat.Items)
                {
                    var rect = new SKRect(
                        X + _horizontalPadding,
                        cursorY + 4,
                        X + Width - _horizontalPadding,
                        cursorY + _itemHeight - 4);
                    bool isItemHover = _hoverItemGlobalIndex >= 0 && _hoverItemGlobalIndex == Items.IndexOf(it);
                    var itemColor = isItemHover ? new SKColor(230, 245, 255) : SKColors.White;
                    using var itemBg = new SKPaint { Color = itemColor, Style = SKPaintStyle.Fill };
                    canvas.DrawRect(rect, itemBg);
                    if (isItemHover)
                    {
                        using var itemBorder = new SKPaint { Color = new SKColor(120, 160, 200), Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
                        canvas.DrawRect(rect, itemBorder);
                    }
                    canvas.DrawText(it.Name ?? string.Empty, rect.Left + 8, rect.MidY + 6, SKTextAlign.Left, font, textPaint);
                    cursorY += _itemHeight;
                }
                headerIdx++;
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
                canvas.DrawText(item?.Name ?? string.Empty, rect.Left + 8, rect.MidY + 6, SKTextAlign.Left, gFont, gText);
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
            EnsureCategories();
            var desired = _verticalPadding * 2 + CalculateTotalContentHeight();
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
            EnsureCategories();
            if (_autoSize) RecalculateHeight();
            if (_autoWidth) RecalculateWidth();
            UpdateBounds();
        }

        // Convenience add/remove wrappers to auto-size
        public void AddItem(PaletteItem item)
        {
            Items.Add(item);
            // update categories
            var cat = Categories.Find(c => string.Equals(c.Name, item.Category ?? "General", StringComparison.OrdinalIgnoreCase));
            if (cat == null)
            {
                cat = new PaletteCategory { Name = item.Category ?? "General" };
                Categories.Add(cat);
            }
            cat.Items.Add(item);
            if (_autoSize) RecalculateHeight();
            if (_autoWidth) RecalculateWidth();
            UpdateBounds();
        }

        public bool RemoveItem(PaletteItem item)
        {
            var removed = Items.Remove(item);
            if (removed)
            {
                var cat = Categories.Find(c => string.Equals(c.Name, item.Category ?? "General", StringComparison.OrdinalIgnoreCase));
                if (cat != null)
                {
                    cat.Items.Remove(item);
                    if (cat.Items.Count == 0)
                    {
                        Categories.Remove(cat);
                    }
                }
            }
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
            var full = CalculateTotalContentHeight() + _verticalPadding * 2;
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
            var total = CalculateTotalContentHeight() + _verticalPadding * 2;
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
            // Delegate to component-specific mouse down (category-aware)
            return OnMouseDown(point, context);
        }

        public override bool HandleMouseMove(SKPoint point, InteractionContext context)
        {
            if (_scrollThumbDragging)
            {
                // Use category-aware total height (headers + visible items)
                var total = CalculateTotalContentHeight() + _verticalPadding * 2;
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
            // Hover detection when not dragging the scrollbar thumb
            UpdateHoverState(point);
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
                _hoverHeaderIndex = -1;
                _hoverItemGlobalIndex = -1;
                return true;
            }
            _hoverHeaderIndex = -1;
            _hoverItemGlobalIndex = -1;
            return base.HandleMouseUp(point, context);
        }

        public void ScrollBy(float deltaPixels)
        {
            _scrollOffset += deltaPixels;
            ClampScroll();
        }

        private void UpdateHoverState(SKPoint point)
        {
            int newHeaderHover = -1;
            int newItemHover = -1;

            if (Bounds.Contains(point.X, point.Y))
            {
                // Check headers first
                for (int i = 0; i < Categories.Count; i++)
                {
                    var cat = Categories[i];
                    if (cat.HeaderRect.Contains(point))
                    {
                        newHeaderHover = i;
                        break;
                    }
                }
                if (newHeaderHover == -1)
                {
                    // Hit test items with scroll applied
                    foreach (var cat in Categories)
                    {
                        float yPtr = cat.HeaderRect.Bottom; // start after header
                        if (!cat.IsCollapsed)
                        {
                            for (int i = 0; i < cat.Items.Count; i++)
                            {
                                var rowTop = yPtr;
                                var rowBottom = yPtr + _itemHeight;
                                if (point.Y >= rowTop && point.Y <= rowBottom)
                                {
                                    newItemHover = Items.IndexOf(cat.Items[i]);
                                    break;
                                }
                                yPtr += _itemHeight;
                            }
                        }
                        if (newItemHover != -1) break;
                    }
                }
            }

            if (newHeaderHover != _hoverHeaderIndex || newItemHover != _hoverItemGlobalIndex)
            {
                _hoverHeaderIndex = newHeaderHover;
                _hoverItemGlobalIndex = newItemHover;
                // No direct invalidation here; DrawingManager triggers a redraw after mouse move.
            }
        }

        // =========================
        // Category helpers & layout
        // =========================
        private void EnsureCategories()
        {
            if (Categories.Count == 0 && Items.Count > 0)
            {
                foreach (var grp in Items.GroupBy(i => i.Category ?? "General"))
                {
                    var cat = new PaletteCategory { Name = grp.Key };
                    cat.Items.AddRange(grp);
                    Categories.Add(cat);
                }
            }
        }

        private float CalculateTotalContentHeight()
        {
            EnsureCategories();
            float total = 0f;
            foreach (var cat in Categories)
            {
                total += CategoryHeaderHeight;
                if (!cat.IsCollapsed)
                {
                    total += cat.Items.Count * _itemHeight;
                }
            }
            return total;
        }

        // =========================
        // Interaction overrides
        // =========================
        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            // Prevent the palette itself from being marked selected by parent logic
            this.IsSelected = false;

            // Toggle category collapse when clicking the header
            bool toggled = false;
            for (int i = 0; i < Categories.Count; i++)
            {
                var cat = Categories[i];
                if (cat.HeaderRect.Contains(point))
                {
                    cat.IsCollapsed = !cat.IsCollapsed;
                    toggled = true;
                    break;
                }
            }
            if (toggled)
            {
                RefreshLayout();
                return true;
            }

            // Begin drag on item click within bounds
            if (Bounds.Contains(point.X, point.Y))
            {
                // Determine which item based on scroll and category layout
                float cursorY = Y + _verticalPadding - _scrollOffset + CategoryHeaderHeight; // start after first header baseline shift within loop
                foreach (var cat in Categories)
                {
                    // header already tested
                    float yPtr = cat.HeaderRect.Bottom; // start after header
                    if (!cat.IsCollapsed)
                    {
                        for (int i = 0; i < cat.Items.Count; i++)
                        {
                            var rowTop = yPtr;
                            var rowBottom = yPtr + _itemHeight;
                            if (point.Y >= rowTop && point.Y <= rowBottom)
                            {
                                var globalIndex = Items.IndexOf(cat.Items[i]);
                                if (globalIndex >= 0)
                                {
                                    _dragIndex = globalIndex;
                                    _isDragging = true;
                                    _dragStartPoint = point;
                                    _dragCurrentPoint = point;
                                    return true;
                                }
                            }
                            yPtr += _itemHeight;
                        }
                    }
                }
            }
            return false;
        }

        protected override bool OnMouseMove(SKPoint point, InteractionContext context)
        {
            // No additional per-component move handling beyond public HandleMouseMove
            // Avoid calling base.HandleMouseMove here to prevent recursion.
            return false;
        }

        protected override bool OnMouseUp(SKPoint point, InteractionContext context)
        {
            // No additional per-component mouse up handling beyond public HandleMouseUp
            // Avoid calling base.HandleMouseUp here to prevent recursion.
            return false;
        }
    }
}
