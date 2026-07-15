using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Beep.Skia.Winform.Controls
{
    /// <summary>
    /// Wraps a SkiaComponent to expose its NodeProperties in the Visual Studio PropertyGrid.
    /// Implements ICustomTypeDescriptor so the VS designer can display editable properties.
    /// </summary>
    public class SkiaComponentPropertyWrapper : ICustomTypeDescriptor, IDisposable
    {
        private readonly SkiaComponent _component;
        private readonly PropertyDescriptorCollection _properties;

        public SkiaComponent Component => _component;
        public string ComponentTypeName { get; }
        public string ComponentName { get; }

        public SkiaComponentPropertyWrapper(SkiaComponent component)
        {
            _component = component ?? throw new ArgumentNullException(nameof(component));
            ComponentTypeName = component.GetType().Name;
            ComponentName = component.Name ?? ComponentTypeName;

            var props = new List<PropertyDescriptor>();

            // Geometry properties
            props.Add(new SkiaNodePropertyDescriptor("X", typeof(float), () => component.X,
                v => component.X = (float)v, "Position", "X coordinate"));
            props.Add(new SkiaNodePropertyDescriptor("Y", typeof(float), () => component.Y,
                v => component.Y = (float)v, "Position", "Y coordinate"));
            props.Add(new SkiaNodePropertyDescriptor("Width", typeof(float), () => component.Width,
                v => component.Width = (float)v, "Layout", "Component width"));
            props.Add(new SkiaNodePropertyDescriptor("Height", typeof(float), () => component.Height,
                v => component.Height = (float)v, "Layout", "Component height"));
            props.Add(new SkiaNodePropertyDescriptor("Name", typeof(string), () => component.Name ?? "",
                v => component.Name = (string)v ?? "", "Identity", "Component display name"));

            // NodeProperties (dynamic)
            foreach (var kvp in component.NodeProperties.OrderBy(k => k.Key))
            {
                if (kvp.Value == null) continue;
                var pi = kvp.Value;
                var desc = new SkiaNodePropertyDescriptor(
                    pi.ParameterName,
                    pi.ParameterType ?? typeof(string),
                    () => pi.ParameterCurrentValue,
                    v => pi.ParameterCurrentValue = v,
                    "Properties",
                    pi.Description ?? pi.ParameterName,
                    pi.Choices);
                props.Add(desc);
            }

            _properties = new PropertyDescriptorCollection(props.ToArray());
        }

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

        public void Dispose()
        {
            _properties.Clear();
        }
    }

    /// <summary>
    /// Property descriptor for a single SkiaComponent NodeProperty.
    /// Supports enum choices and simple type conversion.
    /// </summary>
    public class SkiaNodePropertyDescriptor : PropertyDescriptor
    {
        private readonly Func<object> _getter;
        private readonly Action<object> _setter;
        private readonly string[] _choices;

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

        public override Type ComponentType => typeof(SkiaComponentPropertyWrapper);
        public override bool IsReadOnly => false;
        public override Type PropertyType { get; }
        public override string Description { get; }
        public override string Category { get; }

        public override bool CanResetValue(object component) => false;
        public override void ResetValue(object component) { }
        public override bool ShouldSerializeValue(object component) => true;

        public override object GetValue(object component)
        {
            try { return _getter(); }
            catch { return null; }
        }

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
                }
                _setter(value);
            }
            catch { }
        }

        public override TypeConverter Converter
        {
            get
            {
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

        public SkiaEnumConverter(string[] values)
        {
            _values = values ?? Array.Empty<string>();
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => false;
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            => new StandardValuesCollection(_values);
    }
}
