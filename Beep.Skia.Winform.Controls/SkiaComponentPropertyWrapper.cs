using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using SkiaSharp;

namespace Beep.Skia.Winform.Controls
{
    /// <summary>
    /// Wraps a SkiaComponent to expose its NodeProperties in the Visual Studio PropertyGrid.
    /// Implements ICustomTypeDescriptor so the VS designer can display editable properties.
    /// Edits are applied back to the component (public setters) and reported through the change callback.
    /// </summary>
    public class SkiaComponentPropertyWrapper : ICustomTypeDescriptor, IDisposable
    {
        private readonly SkiaComponent _component;
        private readonly Action<SkiaComponent> _onChanged;
        private readonly PropertyDescriptorCollection _properties;

        /// <summary>
        /// Gets or sets the component.
        /// </summary>
        public SkiaComponent Component => _component;
        /// <summary>
        /// Gets or sets the component type name.
        /// </summary>
        public string ComponentTypeName { get; }
        /// <summary>
        /// Gets or sets the component name.
        /// </summary>
        public string ComponentName { get; }

        /// <summary>
        /// Initializes a new instance of the SkiaComponentPropertyWrapper class.
        /// </summary>
        public SkiaComponentPropertyWrapper(SkiaComponent component, Action<SkiaComponent> onChanged = null)
        {
            _component = component ?? throw new ArgumentNullException(nameof(component));
            _onChanged = onChanged;
            ComponentTypeName = component.GetType().Name;
            ComponentName = component.Name ?? ComponentTypeName;

            var props = new List<PropertyDescriptor>();

            // Geometry properties
            props.Add(new SkiaNodePropertyDescriptor("X", typeof(float), () => component.X,
                v => { component.X = (float)v; _onChanged?.Invoke(component); }, "Position", "X coordinate"));
            props.Add(new SkiaNodePropertyDescriptor("Y", typeof(float), () => component.Y,
                v => { component.Y = (float)v; _onChanged?.Invoke(component); }, "Position", "Y coordinate"));
            props.Add(new SkiaNodePropertyDescriptor("Width", typeof(float), () => component.Width,
                v => { component.Width = (float)v; _onChanged?.Invoke(component); }, "Layout", "Component width"));
            props.Add(new SkiaNodePropertyDescriptor("Height", typeof(float), () => component.Height,
                v => { component.Height = (float)v; _onChanged?.Invoke(component); }, "Layout", "Component height"));
            props.Add(new SkiaNodePropertyDescriptor("Name", typeof(string), () => component.Name ?? "",
                v => { component.Name = (string)v ?? ""; _onChanged?.Invoke(component); }, "Identity", "Component display name"));

            // NodeProperties (dynamic)
            foreach (var kvp in component.NodeProperties.OrderBy(k => k.Key))
            {
                if (kvp.Value == null) continue;
                var pi = kvp.Value;
                var key = pi.ParameterName;
                var desc = new SkiaNodePropertyDescriptor(
                    key,
                    pi.ParameterType ?? typeof(string),
                    () => pi.ParameterCurrentValue,
                    v =>
                    {
                        // Apply through public setters so the component updates (and NodeProperties re-sync),
                        // then fall back to the raw ParameterInfo value for metadata-only fields.
                        try { component.SetPropperties(new Dictionary<string, object> { [key] = v }); }
                        catch { pi.ParameterCurrentValue = v; }
                        _onChanged?.Invoke(component);
                    },
                    "Properties",
                    pi.Description ?? key,
                    pi.Choices);
                props.Add(desc);
            }

            _properties = new PropertyDescriptorCollection(props.ToArray());
        }

        /// <summary>
        /// Gets or sets the to string.
        /// </summary>
        public override string ToString() => $"[{ComponentTypeName}] {ComponentName}";

        // ICustomTypeDescriptor — delegates everything else to default
        AttributeCollection ICustomTypeDescriptor.GetAttributes() => TypeDescriptor.GetAttributes(GetType());
        string ICustomTypeDescriptor.GetClassName() => ComponentTypeName;
        string ICustomTypeDescriptor.GetComponentName() => ComponentName;
        TypeConverter ICustomTypeDescriptor.GetConverter() => TypeDescriptor.GetConverter(GetType());
        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent() => null;
        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty() => _properties.Count > 0 ? _properties[0] : null;
        object ICustomTypeDescriptor.GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(GetType(), editorBaseType);
        EventDescriptorCollection ICustomTypeDescriptor.GetEvents() => TypeDescriptor.GetEvents(GetType());
        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes) => TypeDescriptor.GetEvents(GetType(), attributes);
        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() => _properties;
        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes) => _properties;
        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd) => this;

        /// <summary>
        /// Gets or sets the dispose.
        /// </summary>
        public void Dispose()
        {
            _properties.Clear();
        }
    }

    /// <summary>
    /// PropertyGrid wrapper for multiple selected components: exposes geometry plus the
    /// NodeProperties common to every selected component; edits apply to all of them.
    /// </summary>
    public class SkiaMultiComponentWrapper : ICustomTypeDescriptor, IDisposable
    {
        private readonly List<SkiaComponent> _components;
        private readonly Action<IReadOnlyList<SkiaComponent>> _onChanged;
        private readonly PropertyDescriptorCollection _properties;

        /// <summary>
        /// Gets or sets the components.
        /// </summary>
        public IReadOnlyList<SkiaComponent> Components => _components;
        /// <summary>
        /// Gets or sets the count.
        /// </summary>
        public int Count => _components.Count;

        /// <summary>
        /// Initializes a new instance of the SkiaMultiComponentWrapper class.
        /// </summary>
        public SkiaMultiComponentWrapper(IReadOnlyList<SkiaComponent> components, Action<IReadOnlyList<SkiaComponent>> onChanged = null)
        {
            _components = (components ?? throw new ArgumentNullException(nameof(components)))
                .Where(c => c != null)
                .ToList();
            if (_components.Count == 0) throw new ArgumentException("At least one component is required.", nameof(components));
            _onChanged = onChanged;

            var props = new List<PropertyDescriptor>();

            props.Add(new SkiaNodePropertyDescriptor("X", typeof(float), () => _components[0].X,
                v => ApplyAll(c => c.X = (float)v), "Position", "X coordinate (applies to all selected)"));
            props.Add(new SkiaNodePropertyDescriptor("Y", typeof(float), () => _components[0].Y,
                v => ApplyAll(c => c.Y = (float)v), "Position", "Y coordinate (applies to all selected)"));
            props.Add(new SkiaNodePropertyDescriptor("Width", typeof(float), () => _components[0].Width,
                v => ApplyAll(c => c.Width = (float)v), "Layout", "Component width (applies to all selected)"));
            props.Add(new SkiaNodePropertyDescriptor("Height", typeof(float), () => _components[0].Height,
                v => ApplyAll(c => c.Height = (float)v), "Layout", "Component height (applies to all selected)"));

            foreach (var key in GetCommonPropertyKeys())
            {
                var first = _components[0].NodeProperties[key];
                props.Add(new SkiaNodePropertyDescriptor(
                    first.ParameterName,
                    first.ParameterType ?? typeof(string),
                    () => first.ParameterCurrentValue,
                    v => ApplyAll(c => ApplyProperty(c, first.ParameterName, v)),
                    "Properties (common)",
                    (first.Description ?? first.ParameterName) + " — applies to all selected",
                    first.Choices));
            }

            _properties = new PropertyDescriptorCollection(props.ToArray());
        }

        private void ApplyAll(Action<SkiaComponent> apply)
        {
            foreach (var component in _components) apply(component);
            _onChanged?.Invoke(_components);
        }

        private static void ApplyProperty(SkiaComponent component, string key, object value)
        {
            try { component.SetPropperties(new Dictionary<string, object> { [key] = value }); }
            catch
            {
                if (component.NodeProperties.TryGetValue(key, out var pi) && pi != null)
                    pi.ParameterCurrentValue = value;
            }
        }

        private List<string> GetCommonPropertyKeys()
        {
            if (_components.Count == 0) return new List<string>();

            var keys = new HashSet<string>(
                _components[0].NodeProperties.Keys,
                StringComparer.OrdinalIgnoreCase);

            for (int i = 1; i < _components.Count; i++)
            {
                keys.IntersectWith(_components[i].NodeProperties.Keys);
            }

            return keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Gets or sets the to string.
        /// </summary>
        public override string ToString() => $"[{Count} components selected]";

        AttributeCollection ICustomTypeDescriptor.GetAttributes() => TypeDescriptor.GetAttributes(GetType());
        string ICustomTypeDescriptor.GetClassName() => $"{Count} components";
        string ICustomTypeDescriptor.GetComponentName() => $"{Count} components selected";
        TypeConverter ICustomTypeDescriptor.GetConverter() => TypeDescriptor.GetConverter(GetType());
        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent() => null;
        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty() => _properties.Count > 0 ? _properties[0] : null;
        object ICustomTypeDescriptor.GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(GetType(), editorBaseType);
        EventDescriptorCollection ICustomTypeDescriptor.GetEvents() => TypeDescriptor.GetEvents(GetType());
        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes) => TypeDescriptor.GetEvents(GetType(), attributes);
        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() => _properties;
        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes) => _properties;
        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd) => this;

        /// <summary>
        /// Gets or sets the dispose.
        /// </summary>
        public void Dispose()
        {
            _properties.Clear();
        }
    }

    /// <summary>
    /// Property descriptor for a single SkiaComponent NodeProperty.
    /// Supports enum choices, simple type conversion, and SKColor hex editing.
    /// </summary>
    public class SkiaNodePropertyDescriptor : PropertyDescriptor
    {
        private readonly Func<object> _getter;
        private readonly Action<object> _setter;
        private readonly string[] _choices;

        /// <summary>
        /// Initializes a new instance of the SkiaNodePropertyDescriptor class.
        /// </summary>
        public SkiaNodePropertyDescriptor(string name, Type type, Func<object> getter, Action<object> setter,
            string category, string description, string[] choices = null)
            : base(name, null)
        {
            _getter = getter;
            _setter = setter;
            _choices = choices;
            Description = description;
            Category = category;
            PropertyType = type;
        }

        /// <summary>
        /// Gets or sets the component type.
        /// </summary>
        public override Type ComponentType => typeof(object);
        /// <summary>
        /// Gets or sets the is read only.
        /// </summary>
        public override bool IsReadOnly => false;
        /// <summary>
        /// Gets or sets the property type.
        /// </summary>
        public override Type PropertyType { get; }
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public override string Description { get; }
        /// <summary>
        /// Gets or sets the category.
        /// </summary>
        public override string Category { get; }

        /// <summary>
        /// Gets or sets the can reset value.
        /// </summary>
        public override bool CanResetValue(object component) => false;
        /// <summary>
        /// Gets or sets the reset value.
        /// </summary>
        public override void ResetValue(object component) { }
        /// <summary>
        /// Gets or sets the should serialize value.
        /// </summary>
        public override bool ShouldSerializeValue(object component) => true;

        /// <summary>
        /// Gets or sets the get value.
        /// </summary>
        public override object GetValue(object component)
        {
            try { return _getter(); }
            catch { return null; }
        }

        /// <summary>
        /// Gets or sets the set value.
        /// </summary>
        public override void SetValue(object component, object value)
        {
            try
            {
                // Type conversion for common types
                if (value != null && PropertyType != value.GetType())
                {
                    if (PropertyType == typeof(string))
                        value = value.ToString();
                    else if (PropertyType == typeof(int))
                        value = Convert.ToInt32(value);
                    else if (PropertyType == typeof(float))
                        value = Convert.ToSingle(value);
                    else if (PropertyType == typeof(bool) && value is bool b)
                        value = b;
                    else if (PropertyType == typeof(double))
                        value = Convert.ToDouble(value);
                    else if (PropertyType == typeof(SKColor))
                        value = SkiaColorConverter.Parse(value.ToString());
                }
                _setter(value);
            }
            catch { }
        }

        /// <summary>
        /// Gets or sets the converter.
        /// </summary>
        public override TypeConverter Converter
        {
            get
            {
                if (PropertyType == typeof(SKColor))
                    return new SkiaColorConverter();
                if (_choices != null && _choices.Length > 0)
                    return new SkiaEnumConverter(_choices);
                return base.Converter;
            }
        }
    }

    /// <summary>
    /// TypeConverter that provides a dropdown list of string choices.
    /// </summary>
    public class SkiaEnumConverter : StringConverter
    {
        private readonly string[] _values;

        /// <summary>
        /// Initializes a new instance of the SkiaEnumConverter class.
        /// </summary>
        public SkiaEnumConverter(string[] values)
        {
            _values = values ?? Array.Empty<string>();
        }

        /// <summary>
        /// Gets or sets the get standard values supported.
        /// </summary>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        /// <summary>
        /// Gets or sets the get standard values exclusive.
        /// </summary>
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => false;
        /// <summary>
        /// Gets or sets the get standard values.
        /// </summary>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            => new StandardValuesCollection(_values);
    }

    /// <summary>
    /// TypeConverter for SKColor: edits colors as "#AARRGGBB" hex strings in the PropertyGrid.
    /// </summary>
    public class SkiaColorConverter : TypeConverter
    {
        /// <summary>
        /// Gets or sets the can convert from.
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        /// <summary>
        /// Gets or sets the can convert to.
        /// </summary>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

        /// <summary>
        /// Gets or sets the convert from.
        /// </summary>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string text) return Parse(text);
            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// Gets or sets the convert to.
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is SKColor color)
                return Format(color);
            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>Parses "#AARRGGBB", "#RRGGBB", or a named color; invalid input returns transparent.</summary>
        public static SKColor Parse(string text)
        {
            var value = (text ?? string.Empty).Trim();
            if (value.StartsWith("#")) value = value.Substring(1);

            try
            {
                if (value.Length == 6)
                {
                    byte r = byte.Parse(value.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    byte g = byte.Parse(value.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    byte b = byte.Parse(value.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    return new SKColor(r, g, b);
                }
                if (value.Length == 8)
                {
                    byte a = byte.Parse(value.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    byte r = byte.Parse(value.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    byte g = byte.Parse(value.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    byte b = byte.Parse(value.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    return new SKColor(r, g, b, a);
                }
            }
            catch { }

            return SKColors.Transparent;
        }

        /// <summary>Formats a color as "#AARRGGBB".</summary>
        public static string Format(SKColor color)
            => $"#{color.Alpha:X2}{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
    }
}
