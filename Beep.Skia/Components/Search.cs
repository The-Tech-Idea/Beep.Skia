using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 Search component that provides search functionality with expandable search view.
    /// </summary>
    public class Search : MaterialControl
    {
        private string _placeholderText = "Search";
        private string _text = "";
        private SKColor _backgroundColor;
        private SKColor _surfaceTintColor;
        private SKColor _onSurfaceColor;
        private SKColor _onSurfaceVariantColor;
        private SKColor _outlineColor;
        private float _cornerRadius = 28f;
        private float _strokeWidth = 1f;
        private bool _isActive = false;
        private bool _isFocused = false;
        private bool _isHovered = false;
        private bool _isPressed = false;
        private float _searchBarHeight = 56f;
        private float _searchViewHeight = 400f;
        private List<SearchSuggestion> _suggestions = new List<SearchSuggestion>();
        private int _selectedSuggestionIndex = -1;
        private SKPaint _textPaint;
        private SKPaint _placeholderPaint;
        private SKPaint _backgroundPaint;
        private SKPaint _strokePaint;
        private SKFont _font;

        /// <summary>
        /// Gets or sets the placeholder text displayed when the search field is empty.
        /// </summary>
        public string PlaceholderText
        {
            get => _placeholderText;
            set
            {
                if (_placeholderText != value)
                {
                    _placeholderText = value ?? "";
                    RefreshVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the current search text.
        /// </summary>
        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value ?? "";
                    OnTextChanged(EventArgs.Empty);
                    RefreshVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the search is in active (expanded) state.
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    if (!_isActive)
                    {
                        _selectedSuggestionIndex = -1;
                        IsFocused = false;
                    }
                    OnActiveStateChanged(EventArgs.Empty);
                    RefreshVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the search field is focused.
        /// </summary>
        public bool IsFocused
        {
            get => _isFocused;
            set
            {
                if (_isFocused != value)
                {
                    _isFocused = value;
                    RefreshVisual();
                }
            }
        }

        /// <summary>
        /// Gets the list of search suggestions.
        /// </summary>
        public IReadOnlyList<SearchSuggestion> Suggestions => _suggestions.AsReadOnly();

        /// <summary>
        /// Gets or sets the height of the search bar when inactive.
        /// </summary>
        public float SearchBarHeight
        {
            get => _searchBarHeight;
            set
            {
                if (_searchBarHeight != value)
                {
                    _searchBarHeight = value;
                    RefreshVisual();
                }
            }
        }

        /// <summary>
        /// Gets or sets the height of the search view when active.
        /// </summary>
        public float SearchViewHeight
        {
            get => _searchViewHeight;
            set
            {
                if (_searchViewHeight != value)
                {
                    _searchViewHeight = value;
                    RefreshVisual();
                }
            }
        }

        /// <summary>
        /// Occurs when the search text changes.
        /// </summary>
        public event EventHandler TextChanged;

        /// <summary>
        /// Occurs when the active state changes.
        /// </summary>
        public event EventHandler ActiveStateChanged;

        /// <summary>
        /// Occurs when a search is performed.
        /// </summary>
        public event EventHandler<SearchEventArgs> SearchPerformed;

        /// <summary>
        /// Occurs when a suggestion is selected.
        /// </summary>
        public event EventHandler<SearchSuggestionEventArgs> SuggestionSelected;

        /// <summary>
        /// Initializes a new instance of the Search class.
        /// </summary>
        public Search()
        {
            InitializePaints();
            InitializeColors();
            Width = 360f;
            Height = _searchBarHeight;
        }

        private void InitializePaints()
        {
            _font = new SKFont
            {
                Size = 16f
            };

            _textPaint = new SKPaint
            {
                Color = _onSurfaceColor,
                IsAntialias = true
            };

            _placeholderPaint = new SKPaint
            {
                Color = _onSurfaceVariantColor,
                IsAntialias = true
            };

            _backgroundPaint = new SKPaint
            {
                Color = _backgroundColor,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            _strokePaint = new SKPaint
            {
                Color = _outlineColor,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = _strokeWidth
            };
        }

        private void InitializeColors()
        {
            _backgroundColor = MaterialDesignColors.SurfaceContainerHigh;
            _surfaceTintColor = MaterialDesignColors.Primary;
            _onSurfaceColor = MaterialDesignColors.OnSurface;
            _onSurfaceVariantColor = MaterialDesignColors.OnSurfaceVariant;
            _outlineColor = MaterialDesignColors.Outline;
        }

        /// <summary>
        /// Adds a search suggestion.
        /// </summary>
        /// <param name="suggestion">The search suggestion to add.</param>
        public void AddSuggestion(SearchSuggestion suggestion)
        {
            if (suggestion == null) throw new ArgumentNullException(nameof(suggestion));
            _suggestions.Add(suggestion);
            RefreshVisual();
        }

        /// <summary>
        /// Adds a search suggestion with the specified text.
        /// </summary>
        /// <param name="text">The suggestion text.</param>
        /// <param name="icon">Optional icon for the suggestion.</param>
        /// <param name="tag">Optional tag object.</param>
        public void AddSuggestion(string text, string icon = null, object tag = null)
        {
            var suggestion = new SearchSuggestion(text, icon, tag);
            AddSuggestion(suggestion);
        }

        /// <summary>
        /// Clears all search suggestions.
        /// </summary>
        public void ClearSuggestions()
        {
            _suggestions.Clear();
            _selectedSuggestionIndex = -1;
            RefreshVisual();
        }

        /// <summary>
        /// Performs a search with the current text.
        /// </summary>
        public void PerformSearch()
        {
            OnSearchPerformed(new SearchEventArgs(_text));
        }

        /// <summary>
        /// Clears the search text.
        /// </summary>
        public void ClearText()
        {
            Text = "";
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            if (_isActive)
            {
                DrawSearchView(canvas, context);
            }
            else
            {
                DrawSearchBar(canvas, context);
            }
        }

        private void DrawSearchBar(SKCanvas canvas, DrawingContext context)
        {
            var bounds = new SKRect(X, Y, X + Width, Y + _searchBarHeight);

            // Calculate state layer opacity
            float stateOpacity = 0f;
            if (_isPressed) stateOpacity = StateLayerOpacity.Press;
            else if (_isHovered) stateOpacity = StateLayerOpacity.Hover;
            else if (_isFocused) stateOpacity = StateLayerOpacity.Focus;

            // Draw background with state layer
            var backgroundColor = _backgroundColor;
            if (stateOpacity > 0)
            {
                var stateColor = _surfaceTintColor.WithAlpha((byte)(stateOpacity * 255));
                backgroundColor = BlendColors(backgroundColor, stateColor);
            }

            _backgroundPaint.Color = backgroundColor;

            // Draw rounded rectangle background
            var backgroundRect = new SKRect(bounds.Left + 4, bounds.Top + 4, bounds.Right - 4, bounds.Bottom - 4);
            canvas.DrawRoundRect(backgroundRect, _cornerRadius, _cornerRadius, _backgroundPaint);

            // Draw stroke if focused
            if (_isFocused)
            {
                _strokePaint.Color = MaterialDesignColors.Primary;
                canvas.DrawRoundRect(backgroundRect, _cornerRadius, _cornerRadius, _strokePaint);
            }

            // Draw search icon
            DrawSearchIcon(canvas, bounds);

            // Draw text or placeholder
            DrawText(canvas, bounds);

            // Draw trailing icon (clear button if there's text)
            if (!string.IsNullOrEmpty(_text))
            {
                DrawClearIcon(canvas, bounds);
            }
        }

        private void DrawSearchView(SKCanvas canvas, DrawingContext context)
        {
            var bounds = new SKRect(X, Y, X + Width, Y + _searchViewHeight);

            // Draw scrim
            var scrimPaint = new SKPaint
            {
                Color = SKColors.Black.WithAlpha(128),
                Style = SKPaintStyle.Fill
            };
            canvas.DrawRect(bounds, scrimPaint);

            // Draw search view background
            var viewBounds = new SKRect(bounds.Left + 16, bounds.Top + 16, bounds.Right - 16, bounds.Top + 72);
            _backgroundPaint.Color = _backgroundColor;
            canvas.DrawRoundRect(viewBounds, _cornerRadius, _cornerRadius, _backgroundPaint);

            // Draw back button
            DrawBackIcon(canvas, viewBounds);

            // Draw search text in expanded view
            DrawExpandedText(canvas, viewBounds);

            // Draw suggestions
            DrawSuggestions(canvas, bounds, viewBounds.Bottom + 8);
        }

        private void DrawSearchIcon(SKCanvas canvas, SKRect bounds)
        {
            var iconPaint = new SKPaint
            {
                Color = _onSurfaceVariantColor,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            var iconSize = 20f;
            var iconX = bounds.Left + 24f;
            var iconY = bounds.Top + (bounds.Height - iconSize) / 2f;

            // Simple search icon (magnifying glass)
            var path = new SKPath();
            path.AddCircle(iconX + iconSize * 0.3f, iconY + iconSize * 0.3f, iconSize * 0.25f, SKPathDirection.Clockwise);
            path.MoveTo(iconX + iconSize * 0.55f, iconY + iconSize * 0.55f);
            path.LineTo(iconX + iconSize * 0.8f, iconY + iconSize * 0.8f);

            canvas.DrawPath(path, iconPaint);
        }

        private void DrawClearIcon(SKCanvas canvas, SKRect bounds)
        {
            var iconPaint = new SKPaint
            {
                Color = _onSurfaceVariantColor,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2f
            };

            var iconSize = 16f;
            var iconX = bounds.Right - 24f - iconSize / 2f;
            var iconY = bounds.Top + bounds.Height / 2f;

            // Draw X icon
            canvas.DrawLine(iconX - iconSize / 2f, iconY - iconSize / 2f, iconX + iconSize / 2f, iconY + iconSize / 2f, iconPaint);
            canvas.DrawLine(iconX + iconSize / 2f, iconY - iconSize / 2f, iconX - iconSize / 2f, iconY + iconSize / 2f, iconPaint);
        }

        private void DrawBackIcon(SKCanvas canvas, SKRect bounds)
        {
            var iconPaint = new SKPaint
            {
                Color = _onSurfaceColor,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2f
            };

            var iconSize = 20f;
            var iconX = bounds.Left + 24f;
            var iconY = bounds.Top + bounds.Height / 2f;

            // Draw back arrow
            var path = new SKPath();
            path.MoveTo(iconX + iconSize * 0.7f, iconY - iconSize * 0.3f);
            path.LineTo(iconX + iconSize * 0.3f, iconY);
            path.LineTo(iconX + iconSize * 0.7f, iconY + iconSize * 0.3f);

            canvas.DrawPath(path, iconPaint);
        }

        private void DrawText(SKCanvas canvas, SKRect bounds)
        {
            var textX = bounds.Left + 56f; // After search icon
            var textY = bounds.Top + bounds.Height / 2f + 6f; // Center vertically with slight offset

            if (string.IsNullOrEmpty(_text))
            {
                // Draw placeholder
                canvas.DrawText(_placeholderText, textX, textY, _font, _placeholderPaint);
            }
            else
            {
                // Draw text
                canvas.DrawText(_text, textX, textY, _font, _textPaint);
            }
        }

        private void DrawExpandedText(SKCanvas canvas, SKRect bounds)
        {
            var textX = bounds.Left + 56f; // After back icon
            var textY = bounds.Top + bounds.Height / 2f + 6f;

            if (string.IsNullOrEmpty(_text))
            {
                canvas.DrawText(_placeholderText, textX, textY, _font, _placeholderPaint);
            }
            else
            {
                canvas.DrawText(_text, textX, textY, _font, _textPaint);
            }
        }

        private void DrawSuggestions(SKCanvas canvas, SKRect bounds, float startY)
        {
            var suggestionHeight = 48f;
            var currentY = startY;

            for (int i = 0; i < _suggestions.Count && currentY < bounds.Bottom - 16; i++)
            {
                var suggestion = _suggestions[i];
                var isSelected = i == _selectedSuggestionIndex;

                DrawSuggestion(canvas, suggestion, bounds, currentY, suggestionHeight, isSelected);
                currentY += suggestionHeight;
            }
        }

        private void DrawSuggestion(SKCanvas canvas, SearchSuggestion suggestion, SKRect bounds, float y, float height, bool isSelected)
        {
            var suggestionBounds = new SKRect(bounds.Left + 16, y, bounds.Right - 16, y + height);

            // Draw background if selected
            if (isSelected)
            {
                var selectedPaint = new SKPaint
                {
                    Color = MaterialDesignColors.SecondaryContainer,
                    Style = SKPaintStyle.Fill
                };
                canvas.DrawRoundRect(suggestionBounds, 8f, 8f, selectedPaint);
            }

            // Draw icon if present
            if (!string.IsNullOrEmpty(suggestion.Icon))
            {
                var iconPaint = new SKPaint
                {
                    Color = isSelected ? MaterialDesignColors.OnSecondaryContainer : _onSurfaceVariantColor,
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill
                };

                var iconSize = 20f;
                var iconX = suggestionBounds.Left + 16f;
                var iconY = suggestionBounds.Top + (height - iconSize) / 2f;

                // Simple placeholder for icon - in real implementation, you'd load actual icons
                canvas.DrawCircle(iconX + iconSize / 2f, iconY + iconSize / 2f, iconSize / 2f, iconPaint);
            }

            // Draw text using SKFont metrics
            using var textPaint = new SKPaint { Color = isSelected ? MaterialDesignColors.OnSecondaryContainer : _onSurfaceColor, IsAntialias = true };
            var metrics = _font.Metrics;
            var textX = suggestionBounds.Left + (string.IsNullOrEmpty(suggestion.Icon) ? 16f : 56f);
            float baseline = suggestionBounds.Top + height / 2f + metrics.CapHeight / 2f;
            canvas.DrawText(suggestion.Text, textX, baseline, SKTextAlign.Left, _font, textPaint);
        }

        private SKColor BlendColors(SKColor baseColor, SKColor overlayColor)
        {
            var alpha = overlayColor.Alpha / 255f;
            var r = (byte)(baseColor.Red * (1 - alpha) + overlayColor.Red * alpha);
            var g = (byte)(baseColor.Green * (1 - alpha) + overlayColor.Green * alpha);
            var b = (byte)(baseColor.Blue * (1 - alpha) + overlayColor.Blue * alpha);
            return new SKColor(r, g, b);
        }

        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            base.OnMouseDown(point, context);

            _isPressed = true;
            RefreshVisual();

            if (!_isActive)
            {
                // Activate search when clicking on search bar
                IsActive = true;
                IsFocused = true;
                return true;
            }
            else
            {
                // Handle interactions in search view
                HandleSearchViewInteraction(point);
                return true;
            }
        }

        protected override bool OnMouseUp(SKPoint point, InteractionContext context)
        {
            base.OnMouseUp(point, context);

            _isPressed = false;
            RefreshVisual();

            return true;
        }

        protected override bool OnMouseMove(SKPoint point, InteractionContext context)
        {
            base.OnMouseMove(point, context);

            var wasHovered = _isHovered;
            _isHovered = new SKRect(X, Y, X + Width, Y + (_isActive ? _searchViewHeight : _searchBarHeight)).Contains(point);

            if (wasHovered != _isHovered)
            {
                RefreshVisual();
            }

            return true;
        }

        private void HandleSearchViewInteraction(SKPoint point)
        {
            // Handle back button
            if (point.Y >= Y + 16 && point.Y <= Y + 88 && point.X >= X + 16 && point.X <= X + 64)
            {
                IsActive = false;
                return;
            }

            // Handle clear button
            if (!string.IsNullOrEmpty(_text))
            {
                var clearBounds = new SKRect(X + Width - 64, Y + 16, X + Width - 16, Y + 88);
                if (clearBounds.Contains(point))
                {
                    ClearText();
                    return;
                }
            }

            // Handle suggestion selection
            var suggestionHeight = 48f;
            var startY = Y + 96f;

            for (int i = 0; i < _suggestions.Count; i++)
            {
                var suggestionBounds = new SKRect(X + 16, startY + i * suggestionHeight, X + Width - 16, startY + (i + 1) * suggestionHeight);
                if (suggestionBounds.Contains(point))
                {
                    _selectedSuggestionIndex = i;
                    var suggestion = _suggestions[i];
                    Text = suggestion.Text;
                    OnSuggestionSelected(new SearchSuggestionEventArgs(suggestion));
                    PerformSearch();
                    IsActive = false;
                    break;
                }
            }
        }

        protected virtual void OnTextChanged(EventArgs e)
        {
            TextChanged?.Invoke(this, e);
        }

        protected virtual void OnActiveStateChanged(EventArgs e)
        {
            ActiveStateChanged?.Invoke(this, e);
        }

        protected virtual void OnSearchPerformed(SearchEventArgs e)
        {
            SearchPerformed?.Invoke(this, e);
        }

        protected virtual void OnSuggestionSelected(SearchSuggestionEventArgs e)
        {
            SuggestionSelected?.Invoke(this, e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _textPaint?.Dispose();
                _placeholderPaint?.Dispose();
                _backgroundPaint?.Dispose();
                _strokePaint?.Dispose();
                _font?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Represents a search suggestion item.
    /// </summary>
    public class SearchSuggestion
    {
        /// <summary>
        /// Gets the text of the suggestion.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets the icon of the suggestion.
        /// </summary>
        public string Icon { get; }

        /// <summary>
        /// Gets the tag object associated with the suggestion.
        /// </summary>
        public object Tag { get; }

        /// <summary>
        /// Initializes a new instance of the SearchSuggestion class.
        /// </summary>
        /// <param name="text">The suggestion text.</param>
        /// <param name="icon">The suggestion icon.</param>
        /// <param name="tag">The tag object.</param>
        public SearchSuggestion(string text, string icon = null, object tag = null)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Icon = icon;
            Tag = tag;
        }
    }

    /// <summary>
    /// Provides data for the SearchPerformed event.
    /// </summary>
    public class SearchEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the search query.
        /// </summary>
        public string Query { get; }

        /// <summary>
        /// Initializes a new instance of the SearchEventArgs class.
        /// </summary>
        /// <param name="query">The search query.</param>
        public SearchEventArgs(string query)
        {
            Query = query;
        }
    }

    /// <summary>
    /// Provides data for the SuggestionSelected event.
    /// </summary>
    public class SearchSuggestionEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the selected suggestion.
        /// </summary>
        public SearchSuggestion Suggestion { get; }

        /// <summary>
        /// Initializes a new instance of the SearchSuggestionEventArgs class.
        /// </summary>
        /// <param name="suggestion">The selected suggestion.</param>
        public SearchSuggestionEventArgs(SearchSuggestion suggestion)
        {
            Suggestion = suggestion;
        }
    }
}
