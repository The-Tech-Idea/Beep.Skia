using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Beep.Skia.Model;

namespace Beep.Skia
{
    /// <summary>
    /// Base class for all Skia components in the framework.
    /// Provides common functionality for positioning, sizing, rendering, and interaction.
    /// </summary>
    public abstract class SkiaComponent : IDrawableComponent, IDisposable
    {
        // Tracks whether connection port geometry needs recomputation.
        // Families should call MarkPortsDirty() when bounds-affecting properties change
        // and call their own LayoutPorts() when rendering via an EnsurePortLayout pattern.
        protected bool _portsDirty = true;

        private bool _isDisposed;
        private ComponentState _state = ComponentState.Initializing;
        private SKRect _bounds;
        private bool _isVisible = true;
        private bool _isEnabled = true;
        private bool _isStatic = false;
        private float _opacity = 1.0f;

        private float _x;
        /// <summary>
        /// Gets or sets the X coordinate of this component's position.
        /// </summary>
        public float X
        {
            get => _x;
            set
            {
                if (_x != value)
                {
                    try
                    {
                        var logPath = Path.Combine(Path.GetTempPath(), "beepskia_render.log");
                        File.AppendAllText(logPath, $"[SkiaComponent.PositionChange] {DateTime.UtcNow:o} Type={GetType().FullName} Name={Name} X:{_x}->{value} Y:{Y}\n");
                    }
                    catch { }

                    _x = value;
                    UpdateBounds();
                }
            }
        }

        private float _y;
        /// <summary>
        /// Gets or sets the Y coordinate of this component's position.
        /// </summary>
        public float Y
        {
            get => _y;
            set
            {
                if (_y != value)
                {
                    try
                    {
                        var logPath = Path.Combine(Path.GetTempPath(), "beepskia_render.log");
                        File.AppendAllText(logPath, $"[SkiaComponent.PositionChange] {DateTime.UtcNow:o} Type={GetType().FullName} Name={Name} X:{X} Y:{_y}->{value}\n");
                    }
                    catch { }

                    _y = value;
                    UpdateBounds();
                }
            }
        }

        /// <summary>
        /// Gets or sets the width of this component.
        /// </summary>
        public float Width { get; set; }

        /// <summary>
        /// Gets or sets the height of this component.
        /// </summary>
        public float Height { get; set; }

        /// <summary>
        /// Gets or sets the name or identifier of this component.
        /// </summary>
        public string Name { get; set; }

    /// <summary>
    /// Globally unique identifier for this component instance. Assigned at construction.
    /// </summary>
    [System.ComponentModel.Browsable(false)]
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Indicates whether this component should be shown in an editor palette/toolbox.
    /// Editors can use this flag to hide infrastructure or internal components.
    /// Defaults to true.
    /// </summary>
    public virtual bool ShowInPalette { get; set; } = true;

        /// <summary>
        /// Gets or sets custom data associated with this component.
        /// </summary>
        public object Tag { get; set; }

    /// <summary>
    /// Per-node configurable properties surfaced in the property editor.
    /// Keys are logical parameter names; values describe type, defaults, and current value.
    /// </summary>
    public Dictionary<string, ParameterInfo> NodeProperties { get; } = new Dictionary<string, ParameterInfo>(StringComparer.OrdinalIgnoreCase);

        #region Generic property accessors
        /// <summary>
        /// Ensures a ParameterInfo exists under the given key and updates its metadata.
        /// When <paramref name="choices"/> is provided, editors can render dropdowns for constrained strings.
        /// </summary>
        /// <param name="key">Logical parameter key (case-insensitive).</param>
        /// <param name="parameterType">CLR type for the parameter; if null, inferred from values.</param>
        /// <param name="defaultValue">Default value used when no current value is set.</param>
        /// <param name="currentValue">Current value to apply; falls back to defaultValue when null.</param>
        /// <param name="description">Optional description for UI/editor tooltips.</param>
        /// <param name="choices">Optional constrained choices for string-like parameters.</param>
        /// <returns>The upserted ParameterInfo instance.</returns>
        protected ParameterInfo UpsertNodeProperty(
            string key,
            Type parameterType = null,
            object defaultValue = null,
            object currentValue = null,
            string description = null,
            System.Collections.Generic.IEnumerable<string> choices = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

            if (!NodeProperties.TryGetValue(key, out var p) || p == null)
            {
                p = new ParameterInfo
                {
                    ParameterName = key,
                    ParameterType = parameterType,
                    DefaultParameterValue = defaultValue,
                    ParameterCurrentValue = currentValue ?? defaultValue,
                    Description = description ?? string.Empty,
                    Choices = choices != null ? choices.ToArray() : null
                };
                NodeProperties[key] = p;
            }
            else
            {
                if (parameterType != null) p.ParameterType = parameterType;
                if (defaultValue != null) p.DefaultParameterValue = defaultValue;
                if (currentValue != null) p.ParameterCurrentValue = currentValue;
                if (description != null) p.Description = description;
                if (choices != null) p.Choices = choices.ToArray();
            }
            return p;
        }

        /// <summary>
        /// Tries to read a typed value from NodeProperties with safe conversion.
        /// </summary>
        protected bool TryGetNodeProperty<T>(string key, out T value)
        {
            value = default!;
            if (!NodeProperties.TryGetValue(key, out var p) || p == null)
                return false;
            var raw = p.ParameterCurrentValue ?? p.DefaultParameterValue;
            var converted = ConvertToType(raw, typeof(T));
            if (converted is T t)
            {
                value = t;
                return true;
            }
            return false;
        }
        /// <summary>
        /// Returns a flat dictionary of this component's properties for editor/serialization use.
        /// Includes common visual properties and flattens NodeProperties (key -> current value).
        /// </summary>
        /// <param name="includeCommon">Include common component properties (X, Y, Width, Height, Name, etc.).</param>
        /// <param name="includeNodeProperties">Include NodeProperties entries as simple key/value pairs.</param>
        public virtual Dictionary<string, object> GetProperties(bool includeCommon = true, bool includeNodeProperties = true)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (includeCommon)
            {
                dict["Id"] = Id.ToString();
                dict["Name"] = Name;
                dict["X"] = X;
                dict["Y"] = Y;
                dict["Width"] = Width;
                dict["Height"] = Height;
                dict["IsVisible"] = IsVisible;
                dict["IsEnabled"] = IsEnabled;
                dict["IsStatic"] = IsStatic;
                dict["Opacity"] = Opacity;
                // Text block
                dict["DisplayText"] = DisplayText;
                dict["TextPosition"] = TextPosition.ToString();
                dict["TextFontSize"] = TextFontSize;
                dict["TextColor"] = $"#{TextColor.Alpha:X2}{TextColor.Red:X2}{TextColor.Green:X2}{TextColor.Blue:X2}";
                dict["ShowDisplayText"] = ShowDisplayText;
            }

            if (includeNodeProperties && NodeProperties != null)
            {
                foreach (var kv in NodeProperties)
                {
                    var p = kv.Value;
                    var val = p?.ParameterCurrentValue ?? p?.DefaultParameterValue;
                    dict[kv.Key] = val;
                }
            }

            return dict;
        }

        /// <summary>
        /// Applies a set of properties to this component. Keys match property names (case-insensitive) and/or NodeProperties keys.
        /// Unknown keys are ignored. Safe type conversion is attempted for common types (numeric, bool, enum, SKColor, TimeSpan).
        /// </summary>
        /// <param name="properties">Dictionary of properties to apply.</param>
        /// <param name="updateNodeProperties">When true, matching NodeProperties entries are updated with converted values.</param>
        /// <param name="applyToPublicSetters">When true, attempts to set matching public properties on this component via reflection.</param>
        public virtual void SetPropperties(IDictionary<string, object> properties, bool updateNodeProperties = true, bool applyToPublicSetters = true)
        {
            if (properties == null || properties.Count == 0) return;

            var type = this.GetType();
            var props = applyToPublicSetters
                ? type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                : Array.Empty<System.Reflection.PropertyInfo>();

            bool boundsChanged = false;

            foreach (var kv in properties)
            {
                var key = kv.Key;
                var value = kv.Value;

                // First try to update NodeProperties
                if (updateNodeProperties && NodeProperties.TryGetValue(key, out var pinfo) && pinfo != null)
                {
                    var targetType = pinfo.ParameterType ?? pinfo.ParameterCurrentValue?.GetType() ?? pinfo.DefaultParameterValue?.GetType();
                    var converted = ConvertToType(value, targetType);
                    pinfo.ParameterCurrentValue = converted ?? value;
                }

                // Then try to set a public property on the component
                if (applyToPublicSetters)
                {
                    var pi = props.FirstOrDefault(pr => string.Equals(pr.Name, key, StringComparison.OrdinalIgnoreCase) && pr.CanWrite);
                    if (pi != null)
                    {
                        try
                        {
                            var converted = ConvertToType(value, pi.PropertyType);
                            pi.SetValue(this, converted);

                            // Track if bounds-affecting properties changed
                            if (string.Equals(key, nameof(X), StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(key, nameof(Y), StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(key, nameof(Width), StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(key, nameof(Height), StringComparison.OrdinalIgnoreCase))
                            {
                                boundsChanged = true;
                            }

                            // Optionally seed NodeProperties for non-common public properties when missing
                            if (updateNodeProperties && (NodeProperties == null || !NodeProperties.ContainsKey(key)))
                            {
                                // Exclude framework/common properties from NodeProperties auto-seeding
                                var exclude = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
                                {
                                    "Id", nameof(Name), nameof(X), nameof(Y), nameof(Width), nameof(Height),
                                    nameof(IsVisible), nameof(IsEnabled), nameof(IsStatic), nameof(Opacity),
                                    nameof(DisplayText), nameof(TextPosition), nameof(TextFontSize), nameof(TextColor), nameof(ShowDisplayText)
                                };
                                if (!exclude.Contains(key))
                                {
                                    try
                                    {
                                        var ptype = pi.PropertyType;
                                        var autoPi = new ParameterInfo
                                        {
                                            ParameterName = key,
                                            ParameterType = ptype,
                                            DefaultParameterValue = converted,
                                            ParameterCurrentValue = converted,
                                            Description = string.Empty
                                        };
                                        NodeProperties[key] = autoPi;
                                    }
                                    catch { }
                                }
                            }
                        }
                        catch { /* ignore conversion/assignment errors */ }
                    }
                }
            }

            if (boundsChanged)
            {
                UpdateBounds();
            }
        }

        /// <summary>
        /// Convenience alias with corrected spelling.
        /// </summary>
        public void SetProperties(IDictionary<string, object> properties, bool updateNodeProperties = true, bool applyToPublicSetters = true)
            => SetPropperties(properties, updateNodeProperties, applyToPublicSetters);

        private static object ConvertToType(object value, Type targetType)
        {
            if (targetType == null) return value;
            if (value == null)
            {
                if (targetType.IsValueType)
                    return Activator.CreateInstance(targetType);
                return null;
            }

            var vType = value.GetType();
            if (targetType.IsAssignableFrom(vType)) return value;

            try
            {
                if (targetType.IsEnum)
                {
                    if (value is string es) return Enum.Parse(targetType, es, ignoreCase: true);
                    return Enum.ToObject(targetType, System.Convert.ChangeType(value, Enum.GetUnderlyingType(targetType)));
                }
                if (targetType == typeof(string)) return System.Convert.ToString(value);
                if (targetType == typeof(int) || targetType == typeof(int?)) return System.Convert.ToInt32(value);
                if (targetType == typeof(float) || targetType == typeof(float?)) return System.Convert.ToSingle(value);
                if (targetType == typeof(double) || targetType == typeof(double?)) return System.Convert.ToDouble(value);
                if (targetType == typeof(bool) || targetType == typeof(bool?))
                {
                    if (value is string bs) { if (bool.TryParse(bs, out var bv)) return bv; }
                    return System.Convert.ToBoolean(value);
                }
                if (targetType == typeof(TimeSpan) || targetType == typeof(TimeSpan?))
                {
                    if (value is TimeSpan ts) return ts;
                    if (TimeSpan.TryParse(value.ToString(), out var parsedTs)) return parsedTs;
                }
                if (targetType == typeof(SKColor) || targetType == typeof(SKColor?))
                {
                    if (value is SKColor c) return c;
                    if (value is string s && TryParseColor(s, out var col)) return col;
                }
                return System.Convert.ChangeType(value, targetType);
            }
            catch
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }
        }

        private static bool TryParseColor(string text, out SKColor color)
        {
            color = SKColors.Black;
            if (string.IsNullOrWhiteSpace(text)) return false;
            text = text.Trim();
            try
            {
                if (text.StartsWith("#"))
                {
                    var hex = text.Substring(1);
                    if (hex.Length == 6)
                    {
                        byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                        byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                        byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                        color = new SKColor(r, g, b);
                        return true;
                    }
                    if (hex.Length == 8)
                    {
                        byte a = Convert.ToByte(hex.Substring(0, 2), 16);
                        byte r = Convert.ToByte(hex.Substring(2, 2), 16);
                        byte g = Convert.ToByte(hex.Substring(4, 2), 16);
                        byte b = Convert.ToByte(hex.Substring(6, 2), 16);
                        color = new SKColor(r, g, b, a);
                        return true;
                    }
                }
                else if (text.Contains(','))
                {
                    var parts = text.Split(',');
                    if (parts.Length >= 3)
                    {
                        byte r = byte.Parse(parts[0].Trim());
                        byte g = byte.Parse(parts[1].Trim());
                        byte b = byte.Parse(parts[2].Trim());
                        byte a = 255;
                        if (parts.Length >= 4) a = byte.Parse(parts[3].Trim());
                        color = new SKColor(r, g, b, a);
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }
        #endregion

        /// <summary>
        /// Gets or sets a value indicating whether this component is visible.
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnVisibilityChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this component is enabled for interaction.
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnEnabledChanged();
                }
            }
        }

        /// <summary>
        /// When true, this component is static (non-movable) by user interactions.
        /// Framework drag operations should respect this and not move it.
        /// Default is false.
        /// </summary>
        public bool IsStatic
        {
            get => _isStatic;
            set => _isStatic = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this component is selected.
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// Gets or sets the opacity of this component (0.0 to 1.0).
        /// </summary>
        public float Opacity
        {
            get => _opacity;
            set
            {
                _opacity = Math.Max(0.0f, Math.Min(1.0f, value));
                OnOpacityChanged();
            }
        }

        /// <summary>
        /// Gets the current state of this component.
        /// </summary>
        public ComponentState State
        {
            get => _state;
            protected set
            {
                if (_state != value)
                {
                    var oldState = _state;
                    _state = value;
                    OnStateChanged(oldState, _state);
                }
            }
        }

        /// <summary>
        /// Gets or sets the rendering priority of this component.
        /// </summary>
        public ComponentPriority Priority { get; set; } = ComponentPriority.Normal;

        /// <summary>
        /// Gets the bounding rectangle of this component.
        /// </summary>
        public SKRect Bounds
        {
            get => _bounds;
            protected set
            {
                if (_bounds != value)
                {
                    _bounds = value;
                    OnBoundsChanged(_bounds);
                }
            }
        }

        /// <summary>
        /// Gets the parent component if this component is part of a hierarchy.
        /// </summary>
        public SkiaComponent Parent { get; set; }

        /// <summary>
        /// Gets the child components of this component.
        /// </summary>
        public List<SkiaComponent> Children { get; } = new List<SkiaComponent>();

    /// <summary>
    /// Named child components for container-like controls. Keys are logical names (usually the child's Name).
    /// This is a convenience lookup alongside the ordered Children list.
    /// </summary>
    public Dictionary<string, SkiaComponent> ChildNodes { get; } = new Dictionary<string, SkiaComponent>(StringComparer.OrdinalIgnoreCase);

        #region Text Display Properties
        /// <summary>
        /// Gets or sets the text to display on this component.
        /// </summary>
        public string DisplayText { get; set; }

        /// <summary>
        /// Gets or sets the position where text should be displayed relative to the component.
        /// </summary>
        public TextPosition TextPosition { get; set; } = TextPosition.Inside;

        /// <summary>
        /// Gets or sets the font size for the display text.
        /// </summary>
        public float TextFontSize { get; set; } = 12f;

        /// <summary>
        /// Gets or sets the color of the display text.
        /// </summary>
        public SKColor TextColor { get; set; } = SKColors.Black;

        /// <summary>
        /// Gets or sets whether to show the display text.
        /// </summary>
        public bool ShowDisplayText { get; set; } = true;
        #endregion

        /// <summary>
        /// Occurs when the component's bounds change.
        /// </summary>
        public event EventHandler<SKRectEventArgs> BoundsChanged;

        /// <summary>
        /// Occurs when the component's visibility changes.
        /// </summary>
        public event EventHandler VisibilityChanged;

        /// <summary>
        /// Occurs when the component's enabled state changes.
        /// </summary>
        public event EventHandler EnabledChanged;

        /// <summary>
        /// Occurs when the component's opacity changes.
        /// </summary>
        public event EventHandler OpacityChanged;

        /// <summary>
        /// Occurs when the component's state changes.
        /// </summary>
        public event EventHandler<ComponentStateChangedEventArgs> StateChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="SkiaComponent"/> class.
        /// </summary>
        protected SkiaComponent()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes the component. Called during construction.
        /// </summary>
        protected virtual void InitializeComponent()
        {
            State = ComponentState.Active;
        }

        /// <summary>
        /// Updates the component's layout and internal state.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public virtual void Update(DrawingContext context)
        {
            // Aggressive logging at method entry so we capture calls even when Update returns early.
            try
            {
                var logPath = Path.Combine(Path.GetTempPath(), "beepskia_update.log");
                File.AppendAllText(logPath, $"[SkiaComponent.Update.Entry] {DateTime.UtcNow:o} Type={GetType().FullName} State={State} X={X} Y={Y} W={Width} H={Height}\n");

                // also mirror to main render log for convenience
                var mainLog = Path.Combine(Path.GetTempPath(), "beepskia_render.log");
                File.AppendAllText(mainLog, $"[SkiaComponent.Update.Entry] {DateTime.UtcNow:o} Type={GetType().FullName} State={State}\\n");
            }
            catch { /* swallow logging errors */ }

            if (State == ComponentState.Disposing || State == ComponentState.Inactive)
            {
                try
                {
                    var logPath = Path.Combine(Path.GetTempPath(), "beepskia_update.log");
                    File.AppendAllText(logPath, $"[SkiaComponent.Update.EarlyReturn] {DateTime.UtcNow:o} Type={GetType().FullName} State={State}\\n");
                }
                catch { }

                return;
            }

            State = ComponentState.Updating;

            // Lightweight diagnostic logging to help debug why Bounds may remain empty at runtime.
            try
            {
                var logPath = Path.Combine(Path.GetTempPath(), "beepskia_render.log");
                File.AppendAllText(logPath, $"[SkiaComponent.Update] {GetType().FullName} X={X},Y={Y},W={Width},H={Height}\n");
            }
            catch { /* swallow logging errors */ }

            UpdateBounds();
            UpdateChildren(context);

            State = ComponentState.Active;
        }

        /// <summary>
        /// Updates the bounding rectangle of this component.
        /// </summary>
        protected virtual void UpdateBounds()
        {
            Bounds = new SKRect(X, Y, X + Width, Y + Height);

            try
            {
                var logPath = Path.Combine(Path.GetTempPath(), "beepskia_render.log");
                File.AppendAllText(logPath, $"[SkiaComponent.UpdateBounds] {GetType().FullName} Bounds={Bounds}\n");
            }
            catch { /* swallow logging errors */ }
        }

        /// <summary>
        /// Updates all child components.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        protected virtual void UpdateChildren(DrawingContext context)
        {
            foreach (var child in Children)
            {
                child.Update(context);
            }
        }

        /// <summary>
        /// Draws this component on the specified Skia canvas.
        /// </summary>
        /// <param name="canvas">The SkiaSharp canvas to draw on.</param>
        /// <param name="context">The drawing context containing additional information.</param>
        public virtual void Draw(SKCanvas canvas, DrawingContext context)
        {
            if (!IsVisible || State == ComponentState.Disposing || State == ComponentState.Inactive)
                return;

            State = ComponentState.Rendering;

            // Save canvas state (no translation; components draw with absolute X,Y like Checkbox pattern)
            var canvasState = canvas.Save();
            try
            {
                // Apply opacity using absolute bounds when needed
                if (Opacity < 1.0f)
                {
                    var absBounds = new SKRect(X, Y, X + Width, Y + Height);
                    using var paint = new SKPaint { Color = SKColors.White.WithAlpha((byte)(Opacity * 255)) };
                    canvas.SaveLayer(absBounds, paint);
                }

                DrawContent(canvas, context);
                DrawChildren(canvas, context);
                DrawText(canvas, context);
            }
            finally
            {
                canvas.RestoreToCount(canvasState);
            }

            State = ComponentState.Active;
        }

        /// <summary>
        /// Applies transformations to the canvas before drawing.
        /// </summary>
        /// <param name="canvas">The canvas to transform.</param>
        /// <param name="context">The drawing context.</param>
        protected virtual void ApplyTransformations(SKCanvas canvas, DrawingContext context)
        {
            // This method is now empty.
            // Global transformations (pan/zoom) are handled once in RenderingHelper.
            // Local transformations (position) are handled in this component's Draw method.
        }

        /// <summary>
        /// Draws the actual content of this component.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected abstract void DrawContent(SKCanvas canvas, DrawingContext context);

        /// <summary>
        /// Draws all child components.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected virtual void DrawChildren(SKCanvas canvas, DrawingContext context)
        {
            foreach (var child in Children.OrderBy(c => c.Priority))
            {
                child.Draw(canvas, context);
            }
        }

        /// <summary>
        /// Gets the effective text positioning bounds for this component.
        /// Components with non-rectangular shapes can override this to provide
        /// shape-aware text positioning.
        /// </summary>
        /// <returns>A rectangle defining the effective bounds for text positioning.</returns>
        protected virtual SKRect GetTextPositioningBounds()
        {
            // Default: use the component's rectangular bounds
            return new SKRect(X, Y, X + Width, Y + Height);
        }

        /// <summary>
        /// Draws the display text for this component based on the TextPosition setting.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected virtual void DrawText(SKCanvas canvas, DrawingContext context)
        {
            if (!ShowDisplayText || string.IsNullOrEmpty(DisplayText))
                return;

            using var font = new SKFont { Size = TextFontSize };
            using var paint = new SKPaint
            {
                Color = TextColor,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            // Get proper text metrics for accurate positioning
            var textBounds = new SKRect();
            font.MeasureText(DisplayText, out textBounds);
            var textWidth = textBounds.Width;

            // Use font metrics for proper baseline alignment
            var metrics = font.Metrics;
            var textHeight = metrics.CapHeight; // Use cap height for consistent alignment
            var descent = metrics.Descent;

            // Get shape-aware positioning bounds
            var positioningBounds = GetTextPositioningBounds();

            float textX, textY;
            float gap = 5f; // Gap between text and component

            switch (TextPosition)
            {
                case TextPosition.Inside:
                    // Center text within positioning bounds, but ensure it fits
                    textX = positioningBounds.Left + Math.Max(2, (positioningBounds.Width - textWidth) / 2);
                    textY = positioningBounds.Top + (positioningBounds.Height + textHeight) / 2 - descent;

                    // If text is too wide, left-align with small margin
                    if (textWidth > positioningBounds.Width - 4)
                    {
                        textX = positioningBounds.Left + 2;
                    }
                    break;

                case TextPosition.Above:
                    textX = positioningBounds.Left + (positioningBounds.Width - textWidth) / 2;
                    textY = positioningBounds.Top - gap - descent;
                    break;

                case TextPosition.Below:
                    textX = positioningBounds.Left + (positioningBounds.Width - textWidth) / 2;
                    textY = positioningBounds.Bottom + gap + textHeight - descent;
                    break;

                case TextPosition.Left:
                    textX = positioningBounds.Left - textWidth - gap;
                    textY = positioningBounds.Top + (positioningBounds.Height + textHeight) / 2 - descent;
                    break;

                case TextPosition.Right:
                    textX = positioningBounds.Right + gap;
                    textY = positioningBounds.Top + (positioningBounds.Height + textHeight) / 2 - descent;
                    break;

                default:
                    textX = positioningBounds.Left + (positioningBounds.Width - textWidth) / 2;
                    textY = positioningBounds.Top + (positioningBounds.Height + textHeight) / 2 - descent;
                    break;
            }

            canvas.DrawText(DisplayText, textX, textY, SKTextAlign.Left, font, paint);
        }

        /// <summary>
        /// Determines whether the specified point is contained within this component.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns>true if the point is contained within this component; otherwise, false.</returns>
        public virtual bool ContainsPoint(SKPoint point)
        {
            return Bounds.Contains(point.X, point.Y);
        }

        /// <summary>
        /// Handles mouse down events for this component.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>true if the event was handled; otherwise, false.</returns>
        public virtual bool HandleMouseDown(SKPoint point, InteractionContext context)
        {
            if (!IsEnabled || !IsVisible)
                return false;

            // Check children first (reverse order for proper hit testing)
            foreach (var child in Children.OrderByDescending(c => c.Priority))
            {
                if (child.ContainsPoint(point) && child.HandleMouseDown(point, context))
                {
                    return true;
                }
            }

            return OnMouseDown(point, context);
        }

        /// <summary>
        /// Handles mouse move events for this component.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>true if the event was handled; otherwise, false.</returns>
        public virtual bool HandleMouseMove(SKPoint point, InteractionContext context)
        {
            if (!IsEnabled || !IsVisible)
                return false;

            // Check children first (reverse order for proper hit testing)
            foreach (var child in Children.OrderByDescending(c => c.Priority))
            {
                if (child.ContainsPoint(point) && child.HandleMouseMove(point, context))
                {
                    return true;
                }
            }

            return OnMouseMove(point, context);
        }

        /// <summary>
        /// Handles mouse up events for this component.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>true if the event was handled; otherwise, false.</returns>
        public virtual bool HandleMouseUp(SKPoint point, InteractionContext context)
        {
            if (!IsEnabled || !IsVisible)
                return false;

            // Check children first (reverse order for proper hit testing)
            foreach (var child in Children.OrderByDescending(c => c.Priority))
            {
                if (child.ContainsPoint(point) && child.HandleMouseUp(point, context))
                {
                    return true;
                }
            }

            return OnMouseUp(point, context);
        }

        /// <summary>
        /// Called when the mouse button is pressed down.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>true if the event was handled; otherwise, false.</returns>
        protected virtual bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            return false;
        }

        /// <summary>
        /// Called when the mouse is moved.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>true if the event was handled; otherwise, false.</returns>
        protected virtual bool OnMouseMove(SKPoint point, InteractionContext context)
        {
            return false;
        }

        /// <summary>
        /// Called when the mouse button is released.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>true if the event was handled; otherwise, false.</returns>
        protected virtual bool OnMouseUp(SKPoint point, Beep.Skia.Model.InteractionContext context)
        {
            return false;
        }

        /// <summary>
        /// Adds a child component to this component.
        /// </summary>
        /// <param name="child">The child component to add.</param>
        public virtual void AddChild(SkiaComponent child)
        {
            if (child == null)
                throw new ArgumentNullException(nameof(child));

            if (child.Parent != null)
            {
                child.Parent.RemoveChild(child);
            }

            Children.Add(child);
            child.Parent = this;
            if (!string.IsNullOrWhiteSpace(child.Name))
            {
                ChildNodes[child.Name] = child;
            }
            OnChildAdded(child);
        }

        /// <summary>
        /// Removes a child component from this component.
        /// </summary>
        /// <param name="child">The child component to remove.</param>
        public virtual void RemoveChild(SkiaComponent child)
        {
            if (child == null)
                return;

            if (Children.Remove(child))
            {
                child.Parent = null;
                // Remove from named dictionary if mapped to same instance
                if (!string.IsNullOrWhiteSpace(child.Name) &&
                    ChildNodes.TryGetValue(child.Name, out var existing) && ReferenceEquals(existing, child))
                {
                    ChildNodes.Remove(child.Name);
                }
                OnChildRemoved(child);
            }
        }

        /// <summary>
        /// Called when a child component is added.
        /// </summary>
        /// <param name="child">The child component that was added.</param>
        protected virtual void OnChildAdded(SkiaComponent child)
        {
        }

        /// <summary>
        /// Called when a child component is removed.
        /// </summary>
        /// <param name="child">The child component that was removed.</param>
        protected virtual void OnChildRemoved(SkiaComponent child)
        {
        }

        /// <summary>
        /// Called when the component's bounds change.
        /// </summary>
        /// <param name="bounds">The new bounds.</param>
        protected virtual void OnBoundsChanged(SKRect bounds)
        {
            // Mark port layout as dirty so families can lazily re-position ports in DrawContent
            MarkPortsDirty();
            BoundsChanged?.Invoke(this, new SKRectEventArgs(bounds));
        }

        /// <summary>
        /// Marks the component's port layout as dirty. Call this when bounds-affecting
        /// changes occur (size, style that affects geometry, port counts, etc.).
        /// </summary>
        protected void MarkPortsDirty()
        {
            _portsDirty = true;
        }

        /// <summary>
        /// Returns true if the component's ports need re-layout.
        /// </summary>
        protected bool ArePortsDirty => _portsDirty;

        /// <summary>
        /// Clears the ports-dirty flag after a successful lazy layout.
        /// Use this in families that implement their own lazy ensure pattern
        /// without the MaterialControl wrapper.
        /// </summary>
        protected void ClearPortsDirty()
        {
            _portsDirty = false;
        }

        /// <summary>
        /// Called when the component's visibility changes.
        /// </summary>
        protected virtual void OnVisibilityChanged()
        {
            VisibilityChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called when the component's enabled state changes.
        /// </summary>
        protected virtual void OnEnabledChanged()
        {
            EnabledChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called when the component's opacity changes.
        /// </summary>
        protected virtual void OnOpacityChanged()
        {
            OpacityChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called when the component's state changes.
        /// </summary>
        /// <param name="oldState">The previous state.</param>
        /// <param name="newState">The new state.</param>
        protected virtual void OnStateChanged(ComponentState oldState, ComponentState newState)
        {
            StateChanged?.Invoke(this, new ComponentStateChangedEventArgs(oldState, newState));
        }

        /// <summary>
        /// Draws this component on the specified Skia canvas with a default drawing context.
        /// </summary>
        /// <param name="canvas">The SkiaSharp canvas to draw on.</param>
        public void Draw(SKCanvas canvas)
        {
            var context = new DrawingContext();
            Draw(canvas, context);
        }

        /// <summary>
        /// Moves this component to the specified position.
        /// </summary>
        /// <param name="position">The new position.</param>
        public virtual void Move(SKPoint position)
        {
            Move(position.X, position.Y);
        }

        /// <summary>
        /// Moves this component to the specified position.
        /// </summary>
        /// <param name="x">The new X coordinate.</param>
        /// <param name="y">The new Y coordinate.</param>
        public virtual void Move(float x, float y)
        {
            X = x;
            Y = y;
            UpdateBounds();
        }

        /// <summary>
        /// Moves this component by the specified offset.
        /// </summary>
        /// <param name="deltaX">The X offset.</param>
        /// <param name="deltaY">The Y offset.</param>
        public virtual void MoveBy(float deltaX, float deltaY)
        {
            X += deltaX;
            Y += deltaY;
            UpdateBounds();
        }

        /// <summary>
        /// Determines whether the specified point hits this component (alias for ContainsPoint).
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns>true if the point hits this component; otherwise, false.</returns>
        public virtual bool HitTest(SKPoint point)
        {
            return ContainsPoint(point);
        }

        /// <summary>
        /// Gets the input connection points for this component.
        /// </summary>
        public virtual List<IConnectionPoint> InConnectionPoints { get; } = new List<IConnectionPoint>();

        /// <summary>
        /// Gets the output connection points for this component.
        /// </summary>
        public virtual List<IConnectionPoint> OutConnectionPoints { get; } = new List<IConnectionPoint>();

    // --- Connection tracking (component-level) ---
    private readonly HashSet<IDrawableComponent> _outgoingConnections = new HashSet<IDrawableComponent>();
    private readonly HashSet<IDrawableComponent> _incomingConnections = new HashSet<IDrawableComponent>();

    /// <summary>
    /// Components this component has outgoing connections to.
    /// </summary>
    public IReadOnlyCollection<IDrawableComponent> OutgoingConnections => _outgoingConnections;

    /// <summary>
    /// Components that connect into this component.
    /// </summary>
    public IReadOnlyCollection<IDrawableComponent> IncomingConnections => _incomingConnections;

    /// <summary>
    /// Raised when a connection (outgoing or incoming) is added for this component.
    /// </summary>
    public event EventHandler<ConnectionChangedEventArgs> ConnectionAdded;

    /// <summary>
    /// Raised when a connection (outgoing or incoming) is removed for this component.
    /// </summary>
    public event EventHandler<ConnectionChangedEventArgs> ConnectionRemoved;

        /// <summary>
        /// Connects this component to another component.
        /// </summary>
        /// <param name="other">The component to connect to.</param>
        public virtual void ConnectTo(IDrawableComponent other)
        {
            if (other == null || other == (IDrawableComponent)this)
                return;

            // Prevent duplicate
            if (_outgoingConnections.Add(other))
            {
                if (other is SkiaComponent otherComp)
                {
                    otherComp._incomingConnections.Add(this);
                    otherComp.ConnectionAdded?.Invoke(otherComp, new ConnectionChangedEventArgs(this, otherComp, ConnectionDirection.Incoming));
                    otherComp.OnConnected(this); // notify target of new incoming connection
                }

                ConnectionAdded?.Invoke(this, new ConnectionChangedEventArgs(this, other, ConnectionDirection.Outgoing));
                OnConnected(other);
            }
        }

        /// <summary>
        /// Disconnects this component from another component.
        /// </summary>
        /// <param name="other">The component to disconnect from.</param>
        public virtual void DisconnectFrom(IDrawableComponent other)
        {
            if (other == null)
                return;

            if (_outgoingConnections.Remove(other))
            {
                if (other is SkiaComponent otherComp)
                {
                    otherComp._incomingConnections.Remove(this);
                    otherComp.ConnectionRemoved?.Invoke(otherComp, new ConnectionChangedEventArgs(this, otherComp, ConnectionDirection.Incoming));
                    otherComp.OnDisconnected(this);
                }

                ConnectionRemoved?.Invoke(this, new ConnectionChangedEventArgs(this, other, ConnectionDirection.Outgoing));
                OnDisconnected(other);
            }
        }

        /// <summary>
        /// Determines whether this component is connected to another component.
        /// </summary>
        /// <param name="other">The component to check connection with.</param>
        /// <returns>true if connected; otherwise, false.</returns>
        public virtual bool IsConnectedTo(IDrawableComponent other)
        {
            if (other == null) return false;
            return _outgoingConnections.Contains(other) || _incomingConnections.Contains(other);
        }

        /// <summary>
        /// Called when this component is connected to another component.
        /// </summary>
        /// <param name="other">The component that was connected.</param>
        protected virtual void OnConnected(IDrawableComponent other)
        {
        }

        /// <summary>
        /// Called when this component is disconnected from another component.
        /// </summary>
        /// <param name="other">The component that was disconnected.</param>
        protected virtual void OnDisconnected(IDrawableComponent other)
        {
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    State = ComponentState.Disposing;

                    // Break all outgoing connections
                    foreach (var target in _outgoingConnections.ToList())
                    {
                        DisconnectFrom(target);
                    }
                    _outgoingConnections.Clear();

                    // Inform sources (incoming) that this component is going away
                    foreach (var source in _incomingConnections.ToList())
                    {
                        if (source is SkiaComponent sc)
                        {
                            sc.DisconnectFrom(this);
                        }
                    }
                    _incomingConnections.Clear();

                    // Dispose children
                    foreach (var child in Children.ToList())
                    {
                        child.Dispose();
                    }
                    Children.Clear();
                    ChildNodes.Clear();

                    // Dispose managed resources
                    DisposeManagedResources();
                }

                // Dispose unmanaged resources
                DisposeUnmanagedResources();

                _isDisposed = true;
            }
        }

        /// <summary>
        /// Disposes managed resources.
        /// </summary>
        protected virtual void DisposeManagedResources()
        {
        }

        /// <summary>
        /// Disposes unmanaged resources.
        /// </summary>
        protected virtual void DisposeUnmanagedResources()
        {
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="SkiaComponent"/> class.
        /// </summary>
        ~SkiaComponent()
        {
            Dispose(false);
        }
    }

    /// <summary>
    /// Provides event arguments for component state changes.
    /// </summary>
    public class ComponentStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the previous state of the component.
        /// </summary>
        public ComponentState OldState { get; }

        /// <summary>
        /// Gets the new state of the component.
        /// </summary>
        public ComponentState NewState { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentStateChangedEventArgs"/> class.
        /// </summary>
        /// <param name="oldState">The previous state.</param>
        /// <param name="newState">The new state.</param>
        public ComponentStateChangedEventArgs(ComponentState oldState, ComponentState newState)
        {
            OldState = oldState;
            NewState = newState;
        }
    }

    /// <summary>
    /// Direction of a connection relative to the component raising the event.
    /// </summary>
    public enum ConnectionDirection
    {
        /// <summary>
        /// The component initiated the connection to another component.
        /// </summary>
        Outgoing,
        /// <summary>
        /// Another component initiated the connection into this component.
        /// </summary>
        Incoming
    }

    /// <summary>
    /// Event arguments for connection added/removed events.
    /// </summary>
    public class ConnectionChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The source component (connector / initiator).
        /// </summary>
        public IDrawableComponent Source { get; }

        /// <summary>
        /// The target component (connected / receiver).
        /// </summary>
        public IDrawableComponent Target { get; }

        /// <summary>
        /// Direction relative to the component firing the event.
        /// </summary>
        public ConnectionDirection Direction { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionChangedEventArgs"/> class.
        /// </summary>
        /// <param name="source">Source component.</param>
        /// <param name="target">Target component.</param>
        /// <param name="direction">Direction relative to event sender.</param>
        public ConnectionChangedEventArgs(IDrawableComponent source, IDrawableComponent target, ConnectionDirection direction)
        {
            Source = source;
            Target = target;
            Direction = direction;
        }
    }
}
