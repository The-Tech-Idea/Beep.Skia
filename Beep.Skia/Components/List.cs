using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Beep.Skia.Model;
namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 List component with support for single-line, two-line, and three-line items.
    /// </summary>
    public class List : MaterialControl
    {
        private ObservableCollection<ListItem> _items = new ObservableCollection<ListItem>();
        private ListItem _selectedItem;
        private ListType _listType = ListType.SingleLine;
        private bool _showDividers = false;
        private bool _allowSelection = true;
        private SelectionMode _selectionMode = SelectionMode.Single;
        private float _itemHeight = 56.0f;
        private float _dividerThickness = 1.0f;
        private SKColor _dividerColor = MaterialColors.OutlineVariant;
        private float _fontSize = 14.0f;

        /// <summary>
        /// Represents an item in the list.
        /// </summary>
        public class ListItem
        {
            public string PrimaryText { get; set; }
            public string SecondaryText { get; set; }
            public string TertiaryText { get; set; }
            public string LeadingIcon { get; set; }
            public string TrailingIcon { get; set; }
            public SKColor? LeadingIconColor { get; set; }
            public SKColor? TrailingIconColor { get; set; }
            public object Tag { get; set; }
            public bool IsEnabled { get; set; } = true;
            public bool ShowDivider { get; set; } = true;

            public ListItem(string primaryText, string secondaryText = null, string tertiaryText = null,
                          string leadingIcon = null, string trailingIcon = null, object tag = null)
            {
                PrimaryText = primaryText;
                SecondaryText = secondaryText;
                TertiaryText = tertiaryText;
                LeadingIcon = leadingIcon;
                TrailingIcon = trailingIcon;
                Tag = tag;
            }
        }

        /// <summary>
        /// Material Design 3.0 list types.
        /// </summary>
        public enum ListType
        {
            SingleLine,
            TwoLine,
            ThreeLine
        }

        /// <summary>
        /// Selection modes for the list.
        /// </summary>
        public enum SelectionMode
        {
            None,
            Single,
            Multiple
        }

        /// <summary>
        /// Occurs when an item is selected.
        /// </summary>
        public event EventHandler<ListItem> ItemSelected;

        /// <summary>
        /// Occurs when an item is clicked.
        /// </summary>
        public event EventHandler<ListItem> ItemClicked;

        /// <summary>
        /// Gets or sets the collection of list items.
        /// </summary>
        public ObservableCollection<ListItem> Items
        {
            get => _items;
            set
            {
                if (_items != value)
                {
                    _items = value ?? new ObservableCollection<ListItem>();
                    _items.CollectionChanged += (s, e) => InvalidateVisual();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the currently selected item (for single selection mode).
        /// </summary>
        public ListItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    ItemSelected?.Invoke(this, _selectedItem);
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the list type (single-line, two-line, or three-line).
        /// </summary>
        public ListType Type
        {
            get => _listType;
            set
            {
                if (_listType != value)
                {
                    _listType = value;
                    UpdateItemHeight();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to show dividers between items.
        /// </summary>
        public bool ShowDividers
        {
            get => _showDividers;
            set
            {
                if (_showDividers != value)
                {
                    _showDividers = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether items can be selected.
        /// </summary>
        public bool AllowSelection
        {
            get => _allowSelection;
            set
            {
                if (_allowSelection != value)
                {
                    _allowSelection = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the selection mode.
        /// </summary>
        public SelectionMode Mode
        {
            get => _selectionMode;
            set
            {
                if (_selectionMode != value)
                {
                    _selectionMode = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the height of each list item.
        /// </summary>
        public float ItemHeight
        {
            get => _itemHeight;
            set
            {
                if (_itemHeight != value)
                {
                    _itemHeight = Math.Max(48, value);
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the thickness of dividers.
        /// </summary>
        public float DividerThickness
        {
            get => _dividerThickness;
            set
            {
                if (_dividerThickness != value)
                {
                    _dividerThickness = Math.Max(0.5f, value);
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the color of dividers.
        /// </summary>
        public SKColor DividerColor
        {
            get => _dividerColor;
            set
            {
                if (_dividerColor != value)
                {
                    _dividerColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the font size for list items.
        /// </summary>
        public float FontSize
        {
            get => _fontSize;
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = Math.Max(8, value);
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the List class.
        /// </summary>
        public List()
        {
            // Set default size
            Width = 360;
            Height = 400;

            // Handle click events
            Clicked += OnListClicked;

            // Initialize with empty collection
            _items = new ObservableCollection<ListItem>();
            _items.CollectionChanged += (s, e) => InvalidateVisual();
        }

        /// <summary>
        /// Adds an item to the list.
        /// </summary>
        public void AddItem(string primaryText, string secondaryText = null, string tertiaryText = null,
                          string leadingIcon = null, string trailingIcon = null, object tag = null)
        {
            var item = new ListItem(primaryText, secondaryText, tertiaryText, leadingIcon, trailingIcon, tag);
            _items.Add(item);
        }

        /// <summary>
        /// Adds a ListItem to the list.
        /// </summary>
        public void AddItem(ListItem item)
        {
            if (item != null)
            {
                _items.Add(item);
            }
        }

        /// <summary>
        /// Removes an item from the list.
        /// </summary>
        public void RemoveItem(ListItem item)
        {
            if (_items.Remove(item) && _selectedItem == item)
            {
                _selectedItem = null;
            }
        }

        /// <summary>
        /// Clears all items from the list.
        /// </summary>
        public void ClearItems()
        {
            _items.Clear();
            _selectedItem = null;
        }

        /// <summary>
        /// Selects an item in the list.
        /// </summary>
        public void SelectItem(ListItem item)
        {
            if (_allowSelection && item != null && item.IsEnabled)
            {
                if (_selectionMode == SelectionMode.Single)
                {
                    SelectedItem = item;
                }
                else if (_selectionMode == SelectionMode.Multiple)
                {
                    // For multiple selection, we would need a separate selected items collection
                    // For now, just handle single selection
                    SelectedItem = item;
                }
            }
        }

        /// <summary>
        /// Updates the item height based on the list type.
        /// </summary>
        private void UpdateItemHeight()
        {
            switch (_listType)
            {
                case ListType.SingleLine:
                    _itemHeight = 56.0f;
                    break;
                case ListType.TwoLine:
                    _itemHeight = 72.0f;
                    break;
                case ListType.ThreeLine:
                    _itemHeight = 88.0f;
                    break;
            }
        }

        /// <summary>
        /// Handles list click events.
        /// </summary>
        private void OnListClicked(object sender, EventArgs e)
        {
            // Handle item clicks through mouse events
        }

        /// <summary>
        /// Draws the list component.
        /// </summary>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            var bounds = new SKRect(X, Y, X + Width, Y + Height);

            float currentY = Y;
            int itemIndex = 0;

            foreach (var item in _items)
            {
                var itemBounds = new SKRect(X, currentY, X + Width, currentY + _itemHeight);

                // Draw the list item
                DrawListItem(canvas, item, itemBounds, itemIndex);

                // Draw divider if enabled and not the last item
                if (_showDividers && item.ShowDivider && itemIndex < _items.Count - 1)
                {
                    DrawDivider(canvas, currentY + _itemHeight);
                }

                currentY += _itemHeight;
                itemIndex++;
            }
        }

        /// <summary>
        /// Draws a single list item.
        /// </summary>
    private void DrawListItem(SKCanvas canvas, ListItem item, SKRect bounds, int index)
        {
            // Draw selection background if item is selected
            if (_allowSelection && item == _selectedItem)
            {
                using (var paint = new SKPaint())
                {
                    paint.Color = MaterialColors.SecondaryContainer;
                    paint.IsAntialias = true;
                    canvas.DrawRect(bounds, paint);
                }
            }

            // Draw state layer for hover/press states
            DrawStateLayer(canvas, bounds, MaterialColors.OnSurface);

            float contentX = bounds.Left + 16; // Left padding (absolute)
            float contentWidth = bounds.Width - 32; // Total width minus padding

            // Draw leading icon
            if (!string.IsNullOrEmpty(item.LeadingIcon))
            {
                DrawLeadingIcon(canvas, item, bounds);
                contentX += 40; // Space for icon
                contentWidth -= 40;
            }

            // Draw trailing icon
            float trailingIconWidth = 0;
            if (!string.IsNullOrEmpty(item.TrailingIcon))
            {
                trailingIconWidth = DrawTrailingIcon(canvas, item, bounds);
                contentWidth -= trailingIconWidth + 8; // Space for icon and padding
            }

            // Draw text content
            DrawItemText(canvas, item, bounds, contentX, contentWidth);
        }

        /// <summary>
        /// Draws the leading icon for a list item.
        /// </summary>
        private void DrawLeadingIcon(SKCanvas canvas, ListItem item, SKRect bounds)
        {
            if (string.IsNullOrEmpty(item.LeadingIcon))
                return;

            float iconSize = 24;
            float iconX = 16;
            float iconY = bounds.Top + (bounds.Height - iconSize) / 2;

            DrawSvgIcon(canvas, item.LeadingIcon, iconX, iconY, iconSize, iconSize,
                       item.LeadingIconColor ?? MaterialColors.OnSurfaceVariant);
        }

        /// <summary>
        /// Draws the trailing icon for a list item.
        /// </summary>
        private float DrawTrailingIcon(SKCanvas canvas, ListItem item, SKRect bounds)
        {
            if (string.IsNullOrEmpty(item.TrailingIcon))
                return 0;

            float iconSize = 24;
            float iconX = bounds.Right - iconSize - 16;
            float iconY = bounds.Top + (bounds.Height - iconSize) / 2;

            DrawSvgIcon(canvas, item.TrailingIcon, iconX, iconY, iconSize, iconSize,
                       item.TrailingIconColor ?? MaterialColors.OnSurfaceVariant);

            return iconSize;
        }

        /// <summary>
        /// Draws the text content for a list item.
        /// </summary>
        private void DrawItemText(SKCanvas canvas, ListItem item, SKRect bounds, float startX, float availableWidth)
        {
            float currentY = bounds.Top;
            // Primary
            if (!string.IsNullOrEmpty(item.PrimaryText))
            {
                using var primaryFont = new SKFont(SKTypeface.Default, _fontSize);
                using var primaryPaint = new SKPaint { Color = MaterialColors.OnSurface, IsAntialias = true };
                float textY = _listType == ListType.SingleLine
                    ? bounds.Top + (bounds.Height + primaryFont.Metrics.CapHeight) / 2f
                    : bounds.Top + 20 + primaryFont.Metrics.CapHeight / 2f; // keep original top offset style
                canvas.DrawText(item.PrimaryText, startX, textY, SKTextAlign.Left, primaryFont, primaryPaint);
                currentY = textY + 4; // baseline + spacing
            }

            // Secondary
            if (!string.IsNullOrEmpty(item.SecondaryText) && (_listType == ListType.TwoLine || _listType == ListType.ThreeLine))
            {
                using var secondaryFont = new SKFont(SKTypeface.Default, _fontSize * 0.875f);
                using var secondaryPaint = new SKPaint { Color = MaterialColors.OnSurfaceVariant, IsAntialias = true };
                float baseline = currentY + 12 + secondaryFont.Metrics.CapHeight / 2f; // mimic old +16 total offset (12 + cap/2 approx)
                canvas.DrawText(item.SecondaryText, startX, baseline, SKTextAlign.Left, secondaryFont, secondaryPaint);
                currentY = baseline + 4;
            }

            // Tertiary
            if (!string.IsNullOrEmpty(item.TertiaryText) && _listType == ListType.ThreeLine)
            {
                using var tertiaryFont = new SKFont(SKTypeface.Default, _fontSize * 0.75f);
                using var tertiaryPaint = new SKPaint { Color = MaterialColors.OnSurfaceVariant, IsAntialias = true };
                float baseline = currentY + 12 + tertiaryFont.Metrics.CapHeight / 2f;
                canvas.DrawText(item.TertiaryText, startX, baseline, SKTextAlign.Left, tertiaryFont, tertiaryPaint);
            }
        }

        /// <summary>
        /// Draws a divider between list items.
        /// </summary>
        private void DrawDivider(SKCanvas canvas, float y)
        {
            using (var paint = new SKPaint())
            {
                paint.Color = _dividerColor;
                paint.StrokeWidth = _dividerThickness;

                float startX = 16; // Left padding
                float endX = Width - 16; // Right padding

                canvas.DrawLine(startX, y, endX, y, paint);
            }
        }

        /// <summary>
        /// Handles mouse events for item selection.
        /// </summary>
        protected override bool OnMouseDown(SKPoint location, InteractionContext context)
        {
            base.OnMouseDown(location, context);

            if (_items.Count > 0 && location.Y >= 0 && location.Y <= Height)
            {
                int itemIndex = (int)(location.Y / _itemHeight);
                if (itemIndex >= 0 && itemIndex < _items.Count)
                {
                    var item = _items[itemIndex];
                    if (item.IsEnabled)
                    {
                        ItemClicked?.Invoke(this, item);
                        if (_allowSelection)
                        {
                            SelectItem(item);
                        }
                    }
                }
            }

            return true;
        }
    }
}
