using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 Segmented Buttons component that allows users to select one or more options from a set.
    /// </summary>
    public class SegmentedButtons : MaterialControl
    {
        private SegmentedButtonSelectionMode _selectionMode = SegmentedButtonSelectionMode.Single;
        private readonly List<SegmentedButtonItem> _items = new List<SegmentedButtonItem>();
        private SKColor _backgroundColor = MaterialColors.Surface;
        private SKColor _selectedBackgroundColor = MaterialColors.SecondaryContainer;
        private SKColor _dividerColor = MaterialColors.OutlineVariant;
        private SKColor _selectedTextColor = MaterialColors.OnSecondaryContainer;
        private SKColor _unselectedTextColor = MaterialColors.OnSurface;
        private float _cornerRadius = 20; // Material Design 3.0 corner radius
        private float _dividerWidth = 1;
        private float _segmentHeight = 40;
        private bool _showDividers = true;

        /// <summary>
        /// Material Design 3.0 segmented button selection modes.
        /// </summary>
        public enum SegmentedButtonSelectionMode
        {
            Single,
            Multiple
        }

        /// <summary>
        /// Represents a single segment in the segmented buttons control.
        /// </summary>
        public class SegmentedButtonItem
        {
            private string _text;
            private string _icon;
            private bool _isSelected;
            private bool _isEnabled = true;

            /// <summary>
            /// Gets or sets the text displayed on the segment.
            /// </summary>
            public string Text
            {
                get => _text;
                set => _text = value;
            }

            /// <summary>
            /// Gets or sets the icon (SVG path or resource name) for the segment.
            /// </summary>
            public string Icon
            {
                get => _icon;
                set => _icon = value;
            }

            /// <summary>
            /// Gets or sets whether this segment is selected.
            /// </summary>
            public bool IsSelected
            {
                get => _isSelected;
                set => _isSelected = value;
            }

            /// <summary>
            /// Gets or sets whether this segment is enabled.
            /// </summary>
            public bool IsEnabled
            {
                get => _isEnabled;
                set => _isEnabled = value;
            }

            /// <summary>
            /// Initializes a new instance of the SegmentedButtonItem class.
            /// </summary>
            /// <param name="text">The text to display on the segment.</param>
            /// <param name="icon">The icon for the segment (optional).</param>
            public SegmentedButtonItem(string text, string icon = null)
            {
                _text = text;
                _icon = icon;
                _isSelected = false;
            }
        }

        /// <summary>
        /// Gets or sets the selection mode for the segmented buttons.
        /// </summary>
        public SegmentedButtonSelectionMode SelectionMode
        {
            get => _selectionMode;
            set
            {
                if (_selectionMode != value)
                {
                    _selectionMode = value;
                    // If switching to single selection and multiple items are selected, keep only the first selected item
                    if (_selectionMode == SegmentedButtonSelectionMode.Single)
                    {
                        var selectedItems = _items.Where(item => item.IsSelected).ToList();
                        if (selectedItems.Count > 1)
                        {
                            foreach (var item in selectedItems.Skip(1))
                            {
                                item.IsSelected = false;
                            }
                        }
                    }
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the background color of the segmented buttons container.
        /// </summary>
        public SKColor BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                if (_backgroundColor != value)
                {
                    _backgroundColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the background color for selected segments.
        /// </summary>
        public SKColor SelectedBackgroundColor
        {
            get => _selectedBackgroundColor;
            set
            {
                if (_selectedBackgroundColor != value)
                {
                    _selectedBackgroundColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the color of the dividers between segments.
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
        /// Gets or sets the text color for selected segments.
        /// </summary>
        public SKColor SelectedTextColor
        {
            get => _selectedTextColor;
            set
            {
                if (_selectedTextColor != value)
                {
                    _selectedTextColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the text color for unselected segments.
        /// </summary>
        public SKColor UnselectedTextColor
        {
            get => _unselectedTextColor;
            set
            {
                if (_unselectedTextColor != value)
                {
                    _unselectedTextColor = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the corner radius of the segmented buttons container.
        /// </summary>
        public float CornerRadius
        {
            get => _cornerRadius;
            set
            {
                if (_cornerRadius != value)
                {
                    _cornerRadius = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the width of the dividers between segments.
        /// </summary>
        public float DividerWidth
        {
            get => _dividerWidth;
            set
            {
                if (_dividerWidth != value)
                {
                    _dividerWidth = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the height of each segment.
        /// </summary>
        public float SegmentHeight
        {
            get => _segmentHeight;
            set
            {
                if (_segmentHeight != value)
                {
                    _segmentHeight = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to show dividers between segments.
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
        /// Gets the collection of segments in the segmented buttons control.
        /// </summary>
        public IReadOnlyList<SegmentedButtonItem> Items => _items.AsReadOnly();

        /// <summary>
        /// Gets the indices of currently selected segments.
        /// </summary>
        public IEnumerable<int> SelectedIndices => _items
            .Select((item, index) => new { Item = item, Index = index })
            .Where(x => x.Item.IsSelected)
            .Select(x => x.Index);

        /// <summary>
        /// Occurs when the selection changes.
        /// </summary>
        public event EventHandler<SelectionChangedEventArgs> SelectionChanged;

        /// <summary>
        /// Initializes a new instance of the SegmentedButtons class.
        /// </summary>
        public SegmentedButtons()
        {
            // Set default size
            Width = 300;
            Height = 40;
        }

        /// <summary>
        /// Adds a new segment to the segmented buttons control.
        /// </summary>
        /// <param name="text">The text to display on the segment.</param>
        /// <param name="icon">The icon for the segment (optional).</param>
        /// <returns>The created SegmentedButtonItem.</returns>
        public SegmentedButtonItem AddSegment(string text, string icon = null)
        {
            var item = new SegmentedButtonItem(text, icon);
            _items.Add(item);
            UpdateLayout();
            InvalidateVisual();
            return item;
        }

        /// <summary>
        /// Removes a segment from the segmented buttons control.
        /// </summary>
        /// <param name="item">The segment to remove.</param>
        /// <returns>True if the segment was removed successfully.</returns>
        public bool RemoveSegment(SegmentedButtonItem item)
        {
            if (_items.Remove(item))
            {
                UpdateLayout();
                InvalidateVisual();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes a segment at the specified index.
        /// </summary>
        /// <param name="index">The index of the segment to remove.</param>
        /// <returns>True if the segment was removed successfully.</returns>
        public bool RemoveSegmentAt(int index)
        {
            if (index >= 0 && index < _items.Count)
            {
                _items.RemoveAt(index);
                UpdateLayout();
                InvalidateVisual();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clears all segments from the segmented buttons control.
        /// </summary>
        public void ClearSegments()
        {
            _items.Clear();
            UpdateLayout();
            InvalidateVisual();
        }

        /// <summary>
        /// Selects a segment at the specified index.
        /// </summary>
        /// <param name="index">The index of the segment to select.</param>
        public void SelectSegment(int index)
        {
            if (index < 0 || index >= _items.Count)
                return;

            var item = _items[index];
            if (!item.IsEnabled)
                return;

            if (_selectionMode == SegmentedButtonSelectionMode.Single)
            {
                // Deselect all other items
                foreach (var otherItem in _items)
                {
                    if (otherItem != item)
                    {
                        otherItem.IsSelected = false;
                    }
                }
            }

            bool wasSelected = item.IsSelected;
            item.IsSelected = true;

            if (!wasSelected)
            {
                OnSelectionChanged(new SelectionChangedEventArgs(SelectedIndices.ToArray()));
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Deselects a segment at the specified index.
        /// </summary>
        /// <param name="index">The index of the segment to deselect.</param>
        public void DeselectSegment(int index)
        {
            if (index < 0 || index >= _items.Count)
                return;

            var item = _items[index];
            if (!item.IsEnabled)
                return;

            bool wasSelected = item.IsSelected;
            item.IsSelected = false;

            if (wasSelected)
            {
                OnSelectionChanged(new SelectionChangedEventArgs(SelectedIndices.ToArray()));
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Toggles the selection state of a segment at the specified index.
        /// </summary>
        /// <param name="index">The index of the segment to toggle.</param>
        public void ToggleSegment(int index)
        {
            if (index < 0 || index >= _items.Count)
                return;

            var item = _items[index];
            if (!item.IsEnabled)
                return;

            if (item.IsSelected)
            {
                DeselectSegment(index);
            }
            else
            {
                SelectSegment(index);
            }
        }

        /// <summary>
        /// Updates the layout of the segmented buttons.
        /// </summary>
        private void UpdateLayout()
        {
            if (_items.Count == 0)
                return;

            // Calculate segment width
            float totalWidth = Width;
            float segmentWidth = totalWidth / _items.Count;

            // Update height based on segment height
            Height = _segmentHeight;
        }

        /// <summary>
        /// Draws the segmented buttons control.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            var bounds = new SKRect(X, Y, X + Width, Y + Height);

            // Draw background
            using (var backgroundPaint = new SKPaint())
            {
                backgroundPaint.Color = _backgroundColor;
                backgroundPaint.IsAntialias = true;

                // Create rounded rectangle path for background
                var backgroundPath = new SKPath();
                backgroundPath.AddRoundRect(bounds, _cornerRadius, _cornerRadius);
                canvas.DrawPath(backgroundPath, backgroundPaint);
            }

            if (_items.Count == 0)
                return;

            float segmentWidth = Width / _items.Count;

            // Draw each segment
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                var segmentBounds = new SKRect(
                    X + i * segmentWidth,
                    Y,
                    X + (i + 1) * segmentWidth,
                    Y + Height
                );

                DrawSegment(canvas, item, segmentBounds, i == 0, i == _items.Count - 1);

                // Draw divider (except for the last segment)
                if (_showDividers && i < _items.Count - 1)
                {
                    DrawDivider(canvas, X + (i + 1) * segmentWidth);
                }
            }
        }

        /// <summary>
        /// Draws a single segment.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="item">The segment item to draw.</param>
        /// <param name="bounds">The bounds of the segment.</param>
        /// <param name="isFirst">Whether this is the first segment.</param>
        /// <param name="isLast">Whether this is the last segment.</param>
        private void DrawSegment(SKCanvas canvas, SegmentedButtonItem item, SKRect bounds, bool isFirst, bool isLast)
        {
            // Draw segment background if selected
            if (item.IsSelected)
            {
                using (var selectedPaint = new SKPaint())
                {
                    selectedPaint.Color = _selectedBackgroundColor;
                    selectedPaint.IsAntialias = true;

                    // Create path for selected segment with proper rounded corners
                    var selectedPath = new SKPath();
                    float leftRadius = isFirst ? _cornerRadius : 0;
                    float rightRadius = isLast ? _cornerRadius : 0;

                    if (isFirst && isLast)
                    {
                        // Single segment
                        selectedPath.AddRoundRect(bounds, _cornerRadius, _cornerRadius);
                    }
                    else if (isFirst)
                    {
                        // First segment
                        selectedPath.MoveTo(bounds.Left, bounds.Top);
                        selectedPath.LineTo(bounds.Right, bounds.Top);
                        selectedPath.LineTo(bounds.Right, bounds.Bottom);
                        selectedPath.LineTo(bounds.Left + _cornerRadius, bounds.Bottom);
                        selectedPath.ArcTo(_cornerRadius, _cornerRadius, 0, SKPathArcSize.Small, SKPathDirection.Clockwise, bounds.Left + _cornerRadius, bounds.Top);
                        selectedPath.Close();
                    }
                    else if (isLast)
                    {
                        // Last segment
                        selectedPath.MoveTo(bounds.Left, bounds.Top);
                        selectedPath.LineTo(bounds.Right - _cornerRadius, bounds.Top);
                        selectedPath.ArcTo(_cornerRadius, _cornerRadius, 0, SKPathArcSize.Small, SKPathDirection.Clockwise, bounds.Right, bounds.Top + _cornerRadius);
                        selectedPath.LineTo(bounds.Right, bounds.Bottom);
                        selectedPath.LineTo(bounds.Left, bounds.Bottom);
                        selectedPath.Close();
                    }
                    else
                    {
                        // Middle segment
                        selectedPath.AddRect(bounds);
                    }

                    canvas.DrawPath(selectedPath, selectedPaint);
                }
            }

            // Draw text
            DrawSegmentText(canvas, item, bounds);

            // Draw icon if present
            if (!string.IsNullOrEmpty(item.Icon))
            {
                DrawSegmentIcon(canvas, item, bounds);
            }
        }

        /// <summary>
        /// Draws the text for a segment.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="item">The segment item.</param>
        /// <param name="bounds">The bounds of the segment.</param>
        private void DrawSegmentText(SKCanvas canvas, SegmentedButtonItem item, SKRect bounds)
        {
            if (string.IsNullOrEmpty(item.Text))
                return;
            using var font = new SKFont(SKTypeface.Default, 14);
            using var paint = new SKPaint { Color = item.IsSelected ? _selectedTextColor : _unselectedTextColor, IsAntialias = true };

            // Center text vertically using cap height
            var metrics = font.Metrics;
            float baseline = bounds.MidY + metrics.CapHeight / 2f;
            canvas.DrawText(item.Text, bounds.MidX, baseline, SKTextAlign.Center, font, paint);
        }

        /// <summary>
        /// Draws the icon for a segment.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="item">The segment item.</param>
        /// <param name="bounds">The bounds of the segment.</param>
        private void DrawSegmentIcon(SKCanvas canvas, SegmentedButtonItem item, SKRect bounds)
        {
            // For now, we'll skip icon drawing as it requires SVG loading implementation
            // This can be added later following the pattern used in the Button component
        }

        /// <summary>
        /// Draws a divider between segments.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="x">The x-coordinate of the divider.</param>
        private void DrawDivider(SKCanvas canvas, float x)
        {
            using (var dividerPaint = new SKPaint())
            {
                dividerPaint.Color = _dividerColor;
                dividerPaint.StrokeWidth = _dividerWidth;
                dividerPaint.IsAntialias = true;

                canvas.DrawLine(x, 8, x, Height - 8, dividerPaint);
            }
        }

        /// <summary>
        /// Handles mouse down events.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>True if the event was handled.</returns>
        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            var handled = base.OnMouseDown(point, context);

            if (_items.Count == 0)
                return handled;

            // Determine which segment was clicked
            float segmentWidth = Width / _items.Count;
            int clickedIndex = (int)(point.X / segmentWidth);

            if (clickedIndex >= 0 && clickedIndex < _items.Count)
            {
                var item = _items[clickedIndex];
                if (item.IsEnabled)
                {
                    ToggleSegment(clickedIndex);
                    return true; // Event was handled
                }
            }

            return handled;
        }

        /// <summary>
        /// Raises the SelectionChanged event.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Event arguments for selection changes.
        /// </summary>
        public class SelectionChangedEventArgs : EventArgs
        {
            /// <summary>
            /// Gets the indices of the currently selected segments.
            /// </summary>
            public int[] SelectedIndices { get; }

            /// <summary>
            /// Initializes a new instance of the SelectionChangedEventArgs class.
            /// </summary>
            /// <param name="selectedIndices">The indices of the selected segments.</param>
            public SelectionChangedEventArgs(int[] selectedIndices)
            {
                SelectedIndices = selectedIndices;
            }
        }

        /// <summary>
        /// Creates a new SegmentedButtons instance with the specified segments.
        /// </summary>
        /// <param name="segments">The text for each segment.</param>
        /// <returns>A new SegmentedButtons instance.</returns>
        public static SegmentedButtons Create(params string[] segments)
        {
            var segmentedButtons = new SegmentedButtons();
            foreach (var segment in segments)
            {
                segmentedButtons.AddSegment(segment);
            }
            return segmentedButtons;
        }

        /// <summary>
        /// Creates a new SegmentedButtons instance with single selection mode.
        /// </summary>
        /// <param name="segments">The text for each segment.</param>
        /// <returns>A new SegmentedButtons instance.</returns>
        public static SegmentedButtons CreateSingleSelect(params string[] segments)
        {
            var segmentedButtons = Create(segments);
            segmentedButtons.SelectionMode = SegmentedButtonSelectionMode.Single;
            return segmentedButtons;
        }

        /// <summary>
        /// Creates a new SegmentedButtons instance with multiple selection mode.
        /// </summary>
        /// <param name="segments">The text for each segment.</param>
        /// <returns>A new SegmentedButtons instance.</returns>
        public static SegmentedButtons CreateMultiSelect(params string[] segments)
        {
            var segmentedButtons = Create(segments);
            segmentedButtons.SelectionMode = SegmentedButtonSelectionMode.Multiple;
            return segmentedButtons;
        }
    }
}
