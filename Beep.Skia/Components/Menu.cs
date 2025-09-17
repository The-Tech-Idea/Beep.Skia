using SkiaSharp;
using System;
using System.Collections.Generic;
using Beep.Skia.Model;
namespace Beep.Skia.Components
{
    /// <summary>Material Design menu rendered with absolute coordinates (X,Y).</summary>
    public class Menu : MaterialControl
    {
        private readonly List<MenuItem> _items = new();
        private MenuItem _selected;
        private float _itemHeight = 48f; // MD3 spec default
        private float _menuWidth = 200f;
        private float _cornerRadius = 4f;
        private SKColor _surfaceColor = MaterialColors.SurfaceContainerHigh;
        private bool _visible;
        private SKPoint _anchorPoint;
        private MenuPosition _position = MenuPosition.BottomLeft;

        public enum MenuPosition { TopLeft, TopRight, BottomLeft, BottomRight, Center }

        public IList<MenuItem> Items => _items;
        public event EventHandler<MenuItem> ItemClicked;
        public event EventHandler Opened;
        public event EventHandler Closed;

        public MenuItem SelectedItem
        {
            get => _selected;
            set
            {
                if (_selected == value) return;
                if (_selected != null) _selected.IsSelected = false;
                _selected = value;
                if (_selected != null) _selected.IsSelected = true;
                InvalidateVisual();
            }
        }

        public float MenuWidth { get => _menuWidth; set { if (Math.Abs(_menuWidth - value) > 0.1f) { _menuWidth = value; RecalcSize(); } } }
        public MenuPosition Position { get => _position; set { if (_position != value) { _position = value; UpdatePosition(); } } }
        public SKPoint AnchorPoint { get => _anchorPoint; set { _anchorPoint = value; UpdatePosition(); } }
        public bool Visible { get => _visible; set { if (_visible == value) return; _visible = value; if (_visible) Opened?.Invoke(this, EventArgs.Empty); else Closed?.Invoke(this, EventArgs.Empty); InvalidateVisual(); } }

        public Menu() { Visible = false; RecalcSize(); }

        private void RecalcSize() { Width = _menuWidth; Height = _items.Count * _itemHeight; }
        public void AddItem(MenuItem item) { if (item == null || _items.Contains(item)) return; _items.Add(item); item.ParentMenu = this; RecalcSize(); InvalidateVisual(); }
        public void RemoveItem(MenuItem item) { if (item == null) return; if (_items.Remove(item)) { if (_selected == item) _selected = null; item.ParentMenu = null; RecalcSize(); InvalidateVisual(); } }
        public void ClearItems() { foreach (var i in _items) i.ParentMenu = null; _items.Clear(); _selected = null; RecalcSize(); InvalidateVisual(); }
        public void Show(SKPoint anchor) { AnchorPoint = anchor; Visible = true; }
        public void Hide() { Visible = false; }

    // Backwards-compatibility method for legacy MenuItem setters expecting ParentMenu?.Invalidate()
    public void Invalidate() => InvalidateVisual();

        private void UpdatePosition()
        {
            if (!Visible) return;
            float mx = _anchorPoint.X, my = _anchorPoint.Y;
            switch (_position)
            {
                case MenuPosition.TopLeft: mx = _anchorPoint.X; my = _anchorPoint.Y - Height; break;
                case MenuPosition.TopRight: mx = _anchorPoint.X - Width; my = _anchorPoint.Y - Height; break;
                case MenuPosition.BottomLeft: mx = _anchorPoint.X; my = _anchorPoint.Y; break;
                case MenuPosition.BottomRight: mx = _anchorPoint.X - Width; my = _anchorPoint.Y; break;
                case MenuPosition.Center: mx = _anchorPoint.X - Width / 2f; my = _anchorPoint.Y - Height / 2f; break;
            }
            if (mx < 0) mx = 0; if (my < 0) my = 0; X = mx; Y = my;
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            if (!Visible) return;
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            using (var bg = new SKPaint { Color = _surfaceColor, Style = SKPaintStyle.Fill, IsAntialias = true })
                canvas.DrawRoundRect(rect, _cornerRadius, _cornerRadius, bg);
            using (var sh = new SKPaint { Color = new SKColor(0, 0, 0, 30), IsAntialias = true })
                canvas.DrawRoundRect(new SKRect(rect.Left + 2, rect.Top + 2, rect.Right + 2, rect.Bottom + 2), _cornerRadius, _cornerRadius, sh);

            float yCursor = Y;
            foreach (var item in _items)
            {
                var itemRect = new SKRect(X, yCursor, X + Width, yCursor + _itemHeight);
                item.Draw(canvas, itemRect, context);
                yCursor += _itemHeight;
                if (item.ShowSeparator && yCursor < Y + Height)
                {
                    using var sep = new SKPaint { Color = MaterialColors.OutlineVariant, StrokeWidth = 1, Style = SKPaintStyle.Stroke };
                    canvas.DrawLine(X + 16, yCursor - 0.5f, X + Width - 16, yCursor - 0.5f, sep);
                }
            }
        }

        public override bool ContainsPoint(SKPoint point) => Visible && point.X >= X && point.X <= X + Width && point.Y >= Y && point.Y <= Y + Height;
        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            if (!ContainsPoint(point)) return false;
            int idx = (int)((point.Y - Y) / _itemHeight);
            if (idx >= 0 && idx < _items.Count)
            {
                var it = _items[idx];
                if (it.IsEnabled)
                {
                    SelectedItem = it;
                    ItemClicked?.Invoke(this, it);
                    it.OnClick();
                    return true;
                }
            }
            return base.OnMouseDown(point, context);
        }
        protected override bool OnMouseMove(SKPoint point, InteractionContext context)
        {
            if (!ContainsPoint(point)) return false;
            int idx = (int)((point.Y - Y) / _itemHeight);
            if (idx >= 0 && idx < _items.Count)
            {
                var it = _items[idx];
                if (it.IsEnabled)
                {
                    foreach (var o in _items) if (o != it) o.IsHovered = false;
                    it.IsHovered = true;
                    InvalidateVisual();
                    return true;
                }
            }
            return base.OnMouseMove(point, context);
        }
    }
}
