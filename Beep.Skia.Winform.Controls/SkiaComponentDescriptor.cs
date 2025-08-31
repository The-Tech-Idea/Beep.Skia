using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Beep.Skia.Winform.Controls
{
    // Simple serializable descriptor for a Skia component at design time.
    // Instances of this class are emitted into InitializeComponent when a
    // Skia component is dropped onto a SkiaHostControl in the designer.
    public class SkiaComponentDescriptor
    {
        public SkiaComponentDescriptor() { }

        // Assembly-qualified type name of the Skia component (e.g. Beep.Skia.Components.Button, Beep.Skia)
        public string ComponentType { get; set; }

        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

        // Optional name for later lookup
        public string Name { get; set; }

    // Arbitrary string-based property bag for component-specific settings
    // Designer serializes dictionaries as arrays of key/value pairs, so keep it simple.
    public System.Collections.Generic.Dictionary<string, string> PropertyBag { get; set; } = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    // Collection that will be serialized as content by the WinForms designer.
    [Serializable]
    public class SkiaComponentDescriptorCollection : Collection<SkiaComponentDescriptor>
    {
        public SkiaComponentDescriptorCollection() { }
    }
}
