using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace Beep.Skia
{
    /// <summary>
    /// Serializable descriptor for a Skia component at design time. Hosts (WinForms, WPF, Avalonia,
    /// MAUI, Blazor) emit instances into designer code when a component is dropped onto the canvas.
    /// </summary>
    public class SkiaComponentDescriptor
    {
        /// <summary>
        /// Initializes a new instance of the SkiaComponentDescriptor class.
        /// </summary>
        public SkiaComponentDescriptor() { }

        /// <summary>Assembly-qualified type name of the component (e.g. "Beep.Skia.Components.Button, Beep.Skia").</summary>
        public string ComponentType { get; set; }

        /// <summary>
        /// Gets or sets the x.
        /// </summary>
        public float X { get; set; }
        /// <summary>
        /// Gets or sets the y.
        /// </summary>
        public float Y { get; set; }
        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        public float Width { get; set; }
        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        public float Height { get; set; }

        /// <summary>Optional name for later lookup.</summary>
        public string Name { get; set; }

        /// <summary>Arbitrary string-based property bag for component-specific settings.</summary>
        public Dictionary<string, string> PropertyBag { get; set; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Collection of design-time descriptors, serialized as content by host designers.</summary>
    [Serializable]
    public class SkiaComponentDescriptorCollection : Collection<SkiaComponentDescriptor>
    {
        /// <summary>
        /// Initializes a new instance of the SkiaComponentDescriptorCollection class.
        /// </summary>
        public SkiaComponentDescriptorCollection() { }
    }
}
