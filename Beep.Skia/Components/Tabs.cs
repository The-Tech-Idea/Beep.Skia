using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beep.Skia.Components
{
    /// <summary>
    /// Material Design 3.0 Tabs component that provides navigation between different views or content sections.
    /// Supports both primary and secondary tab variants with smooth animations and proper Material Design 3.0 styling.
    /// </summary>
    public class Tabs : MaterialControl
    {
        private List<TabItem> _tabs = new List<TabItem>();
        private int _selectedIndex = 0;
        private bool _isScrollable = false;
        private TabVariant _variant = TabVariant.Primary;
        private float _indicatorPosition = 0f;
        private float _indicatorWidth = 0f;
        private float _targetIndicatorPosition = 0f;
        private float _targetIndicatorWidth = 0f;
        private bool _isAnimating = false;
        private DateTime _animationStartTime;
        private const float AnimationDuration = 200f; // milliseconds
        private float _scrollOffset = 0f;
        private float _maxScrollOffset = 0f;
        private bool _isHovered = false;
        private bool _isPressed = false;

        /// <summary>
        /// Gets or sets the variant of the tabs (Primary or Secondary).
        /// </summary>
        public TabVariant Variant
        {
            get => _variant;
            set
            {
                if (_variant != value)
                {
                    _variant = value;
                    RefreshVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the currently selected tab index.
        /// </summary>
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex != value && value >= 0 && value < _tabs.Count)
                {
                    _selectedIndex = value;
                    StartIndicatorAnimation();
                    OnSelectedIndexChanged(new TabSelectionChangedEventArgs(_selectedIndex, _tabs[_selectedIndex]));
                    RefreshVisual();
                }
            }
        }

        /// <summary>
        /// Gets the currently selected tab item.
        /// </summary>
        public TabItem SelectedTab => _selectedIndex >= 0 && _selectedIndex < _tabs.Count ? _tabs[_selectedIndex] : null;

        /// <summary>
        /// Gets or sets whether the tabs are scrollable.
        /// </summary>
        public bool IsScrollable
        {
            get => _isScrollable;
            set
            {
                if (_isScrollable != value)
                {
                    _isScrollable = value;
                    RefreshVisual();
                }
            }
        }

        /// <summary>
        /// Gets the collection of tab items.
        /// </summary>
        public IReadOnlyList<TabItem> TabItems => _tabs.AsReadOnly();

        /// <summary>
        /// Gets or sets whether the tabs are currently hovered.
        /// </summary>
        public bool IsHovered
        {
            get => _isHovered;
            set
            {
                if (_isHovered != value)
                {
                    _isHovered = value;
                    RefreshVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the tabs are currently pressed.
        /// </summary>
        public bool IsPressed
        {
            get => _isPressed;
            set
            {
                if (_isPressed != value)
                {
                    _isPressed = value;
                    RefreshVisual();
                }
            }
        }

        /// <summary>
        /// Occurs when the selected tab index changes.
        /// </summary>
        public event EventHandler<TabSelectionChangedEventArgs> SelectedIndexChanged;

        /// <summary>
        /// Initializes a new instance of the Tabs class.
        /// </summary>
        public Tabs()
        {
            Height = _variant == TabVariant.Primary ? 48 : 40;
            _indicatorPosition = 0f;
            _indicatorWidth = 0f;
        }

        /// <summary>
        /// Adds a new tab item with the specified text.
        /// </summary>
        public TabItem AddTab(string text)
        {
            return AddTab(text, null);
        }

        /// <summary>
        /// Adds a new tab item with the specified text and icon.
        /// </summary>
        public TabItem AddTab(string text, string icon)
        {
            var tabItem = new TabItem(text, icon);
            _tabs.Add(tabItem);
            RefreshVisual();
            return tabItem;
        }

        /// <summary>
        /// Removes the specified tab item.
        /// </summary>
        public bool RemoveTab(TabItem tabItem)
        {
            int index = _tabs.IndexOf(tabItem);
            if (index >= 0)
            {
                _tabs.RemoveAt(index);
                if (_selectedIndex >= _tabs.Count && _tabs.Count > 0)
                {
                    _selectedIndex = _tabs.Count - 1;
                }
                RefreshVisual();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes the tab at the specified index.
        /// </summary>
        public bool RemoveTabAt(int index)
        {
            if (index >= 0 && index < _tabs.Count)
            {
                _tabs.RemoveAt(index);
                if (_selectedIndex >= _tabs.Count && _tabs.Count > 0)
                {
                    _selectedIndex = _tabs.Count - 1;
                }
                RefreshVisual();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clears all tab items.
        /// </summary>
        public void ClearTabs()
        {
            _tabs.Clear();
            _selectedIndex = 0;
            RefreshVisual();
        }

        /// <summary>
        /// Starts the indicator animation to the current selected tab.
        /// </summary>
        private void StartIndicatorAnimation()
        {
            if (_tabs.Count == 0) return;

            var selectedTab = _tabs[_selectedIndex];
            _targetIndicatorPosition = CalculateTabPosition(_selectedIndex);
            _targetIndicatorWidth = CalculateTabWidth(_selectedIndex);

            _isAnimating = true;
            _animationStartTime = DateTime.Now;
            RefreshVisual();
        }

        /// <summary>
        /// Updates the indicator animation progress.
        /// </summary>
        private void UpdateAnimation()
        {
            if (_isAnimating)
            {
                var elapsed = (float)(DateTime.Now - _animationStartTime).TotalMilliseconds;
                var progress = Math.Min(elapsed / AnimationDuration, 1f);

                // Easing function for smooth animation
                var easedProgress = 1f - (float)Math.Pow(1f - progress, 3f); // Ease out

                _indicatorPosition = _indicatorPosition + (_targetIndicatorPosition - _indicatorPosition) * easedProgress;
                _indicatorWidth = _indicatorWidth + (_targetIndicatorWidth - _indicatorWidth) * easedProgress;

                if (progress >= 1f)
                {
                    _isAnimating = false;
                    _indicatorPosition = _targetIndicatorPosition;
                    _indicatorWidth = _targetIndicatorWidth;
                }

                RefreshVisual();
            }
        }

        /// <summary>
        /// Calculates the position of a tab at the specified index.
        /// </summary>
        private float CalculateTabPosition(int index)
        {
            if (index < 0 || index >= _tabs.Count) return 0f;

            float position = 0f;
            for (int i = 0; i < index; i++)
            {
                position += CalculateTabWidth(i) + 8f; // 8f is the gap between tabs
            }
            return position - _scrollOffset;
        }

        /// <summary>
        /// Calculates the width of a tab at the specified index.
        /// </summary>
        private float CalculateTabWidth(int index)
        {
            if (index < 0 || index >= _tabs.Count) return 0f;

            var tabItem = _tabs[index];
            float textWidth = 0f;
            float iconWidth = 0f;

            // Measure text width
            if (!string.IsNullOrEmpty(tabItem.Text))
            {
                float size = _variant == TabVariant.Primary ? 14 : 13;
                using (var font = new SKFont(SKTypeface.FromFamilyName(null, SKFontStyle.Normal), size))
                {
                    textWidth = font.MeasureText(tabItem.Text);
                }
            }

            // Icon width (if present)
            if (!string.IsNullOrEmpty(tabItem.Icon))
            {
                iconWidth = 18f; // Icon size
                if (!string.IsNullOrEmpty(tabItem.Text))
                {
                    iconWidth += 8f; // Gap between icon and text
                }
            }

            // Add padding
            float horizontalPadding = _variant == TabVariant.Primary ? 16f : 12f;
            return textWidth + iconWidth + (horizontalPadding * 2);
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            UpdateAnimation();

            if (_tabs.Count == 0) return;

            // Calculate total content width and scrolling
            float totalWidth = 0f;
            for (int i = 0; i < _tabs.Count; i++)
            {
                totalWidth += CalculateTabWidth(i) + 8f; // 8f gap between tabs
            }
            totalWidth -= 8f; // Remove last gap

            _maxScrollOffset = Math.Max(0, totalWidth - Width);
            _scrollOffset = Math.Max(0, Math.Min(_scrollOffset, _maxScrollOffset));

            // Draw tab items (absolute X offset)
            float currentX = -_scrollOffset;
            for (int i = 0; i < _tabs.Count; i++)
            {
                var tabItem = _tabs[i];
                float tabWidth = CalculateTabWidth(i);

                // Absolute positions
                float absX = X + currentX;
                if (absX + tabWidth > X && absX < X + Width) // Only draw visible tabs
                {
                    DrawTabItem(canvas, tabItem, absX, tabWidth, i == _selectedIndex);
                }

                currentX += tabWidth + 8f; // Move to next tab position
            }

            // Draw indicator
            DrawIndicator(canvas);
        }

        private void DrawTabItem(SKCanvas canvas, TabItem tabItem, float x, float width, bool isSelected)
        {
            float tabHeight = _variant == TabVariant.Primary ? 48 : 40;
            var tabRect = new SKRect(x, Y, x + width, Y + tabHeight);

            // Draw tab content
            DrawTabContent(canvas, tabItem, tabRect, isSelected);

            // Draw state layer if needed
            if (isSelected || _isHovered || _isPressed)
            {
                float stateOpacity = 0f;
                if (_isPressed) stateOpacity = StateLayerOpacity.Press;
                else if (_isHovered) stateOpacity = StateLayerOpacity.Hover;

                if (stateOpacity > 0)
                {
                    var stateColor = isSelected ?
                        (_variant == TabVariant.Primary ? MaterialColors.Primary : MaterialColors.OnSurface) :
                        MaterialColors.OnSurface;
                    DrawStateLayer(canvas, tabRect, stateColor, stateOpacity);
                }
            }
        }

        private void DrawTabContent(SKCanvas canvas, TabItem tabItem, SKRect tabRect, bool isSelected)
        {
            float centerY = tabRect.MidY; // absolute center Y
            float currentX = tabRect.Left;

            // Add horizontal padding
            float horizontalPadding = _variant == TabVariant.Primary ? 16f : 12f;
            currentX += horizontalPadding;

            // Draw icon if present
            if (!string.IsNullOrEmpty(tabItem.Icon))
            {
                float iconSize = 18f;
                var iconRect = new SKRect(currentX, centerY - iconSize / 2, currentX + iconSize, centerY + iconSize / 2);

                // For now, draw a placeholder circle for the icon
                using (var iconPaint = new SKPaint())
                {
                    iconPaint.IsAntialias = true;
                    iconPaint.Color = isSelected ?
                        (_variant == TabVariant.Primary ? MaterialColors.OnPrimary : MaterialColors.OnSurface) :
                        MaterialColors.OnSurfaceVariant;
                    canvas.DrawCircle(iconRect.MidX, iconRect.MidY, iconSize / 2, iconPaint);
                }

                currentX += iconSize;
                if (!string.IsNullOrEmpty(tabItem.Text))
                {
                    currentX += 8f; // Gap between icon and text
                }
            }

            // Draw text
            if (!string.IsNullOrEmpty(tabItem.Text))
            {
                float size = _variant == TabVariant.Primary ? 14 : 13;
                using (var font = new SKFont(SKTypeface.FromFamilyName(null, SKFontStyle.Normal), size))
                using (var textPaint = new SKPaint { IsAntialias = true, Color = isSelected ?
                        (_variant == TabVariant.Primary ? MaterialColors.OnPrimary : MaterialColors.OnSurface) :
                        MaterialColors.OnSurfaceVariant })
                {
                    // Baseline using font metrics
                    var metrics = font.Metrics;
                    float baselineAdjust = metrics.CapHeight / 2; // approximate centering
                    float textY = centerY + baselineAdjust / 2;
                    canvas.DrawText(tabItem.Text, currentX, textY, SKTextAlign.Left, font, textPaint);
                }
            }
        }

        private void DrawIndicator(SKCanvas canvas)
        {
            if (_tabs.Count == 0) return;

            float indicatorHeight = _variant == TabVariant.Primary ? 3f : 2f;
        float indicatorY = Y + (_variant == TabVariant.Primary ? Height - indicatorHeight : Height - indicatorHeight);

            using (var indicatorPaint = new SKPaint())
            {
                indicatorPaint.IsAntialias = true;
                indicatorPaint.Color = _variant == TabVariant.Primary ?
                    MaterialColors.Primary :
                    MaterialColors.OnSurface;
                indicatorPaint.Style = SKPaintStyle.Fill;

                var indicatorRect = new SKRect(
                    X + _indicatorPosition,
                    indicatorY,
                    X + _indicatorPosition + _indicatorWidth,
                    indicatorY + indicatorHeight
                );

                canvas.DrawRect(indicatorRect, indicatorPaint);
            }
        }

        private void DrawStateLayer(SKCanvas canvas, SKRect bounds, SKColor baseColor, float opacity)
        {
            using (var paint = new SKPaint())
            {
                paint.IsAntialias = true;
                paint.Color = baseColor.WithAlpha((byte)(opacity * 255));
                canvas.DrawRect(bounds, paint);
            }
        }

        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            base.OnMouseDown(point, context);

            if (_tabs.Count == 0) return false;

            // Find which tab was clicked
            float currentX = -_scrollOffset;
            for (int i = 0; i < _tabs.Count; i++)
            {
                float tabWidth = CalculateTabWidth(i);
                var tabRect = new SKRect(X + currentX, Y, X + currentX + tabWidth, Y + Height);

                if (tabRect.Contains(point))
                {
                    IsPressed = true;
                    SelectedIndex = i;
                    return true;
                }

                currentX += tabWidth + 8f;
            }

            return false;
        }

        protected override bool OnMouseMove(SKPoint point, InteractionContext context)
        {
            base.OnMouseMove(point, context);

            // Update hover state
            bool wasHovered = IsHovered;
            IsHovered = false;

            if (_tabs.Count > 0)
            {
                float currentX = -_scrollOffset;
                for (int i = 0; i < _tabs.Count; i++)
                {
                    float tabWidth = CalculateTabWidth(i);
                    var tabRect = new SKRect(X + currentX, Y, X + currentX + tabWidth, Y + Height);

                    if (tabRect.Contains(point))
                    {
                        IsHovered = true;
                        break;
                    }

                    currentX += tabWidth + 8f;
                }
            }

            if (wasHovered != IsHovered)
            {
                RefreshVisual();
            }

            return IsHovered;
        }

        protected override void OnMouseLeave()
        {
            base.OnMouseLeave();
            IsHovered = false;
            RefreshVisual();
        }

        protected override bool OnMouseUp(SKPoint point, InteractionContext context)
        {
            base.OnMouseUp(point, context);
            IsPressed = false;
            RefreshVisual();
            return true;
        }

        /// <summary>
        /// Raises the SelectedIndexChanged event.
        /// </summary>
        protected virtual void OnSelectedIndexChanged(TabSelectionChangedEventArgs e)
        {
            SelectedIndexChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Represents a tab item in the tabs component.
        /// </summary>
        public class TabItem
        {
            /// <summary>
            /// Gets or sets the text of the tab.
            /// </summary>
            public string Text { get; set; }

            /// <summary>
            /// Gets or sets the icon of the tab.
            /// </summary>
            public string Icon { get; set; }

            /// <summary>
            /// Gets or sets custom data associated with the tab.
            /// </summary>
            public object Tag { get; set; }

            /// <summary>
            /// Initializes a new instance of the TabItem class.
            /// </summary>
            public TabItem(string text, string icon = null)
            {
                Text = text;
                Icon = icon;
            }
        }

        /// <summary>
        /// Specifies the variant of the tabs component.
        /// </summary>
        public enum TabVariant
        {
            /// <summary>
            /// Primary tabs variant for top-level navigation.
            /// </summary>
            Primary,

            /// <summary>
            /// Secondary tabs variant for organizing content within a page.
            /// </summary>
            Secondary
        }

        /// <summary>
        /// Event arguments for tab selection changes.
        /// </summary>
        public class TabSelectionChangedEventArgs : EventArgs
        {
            /// <summary>
            /// Gets the index of the selected tab.
            /// </summary>
            public int SelectedIndex { get; }

            /// <summary>
            /// Gets the selected tab item.
            /// </summary>
            public TabItem SelectedTab { get; }

            /// <summary>
            /// Initializes a new instance of the TabSelectionChangedEventArgs class.
            /// </summary>
            public TabSelectionChangedEventArgs(int selectedIndex, TabItem selectedTab)
            {
                SelectedIndex = selectedIndex;
                SelectedTab = selectedTab;
            }
        }
    }
}
