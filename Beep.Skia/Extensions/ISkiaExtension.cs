using System;
using System.Collections.Generic;

namespace Beep.Skia.Extensions
{
    /// <summary>
    /// Entry point for a Beep.Skia extension package. Implementations must have a public
    /// parameterless constructor; the <see cref="SkiaExtensionHost"/> discovers and loads them
    /// from assemblies or a plugin directory.
    /// </summary>
    public interface ISkiaExtension
    {
        /// <summary>Unique extension id (reverse-DNS recommended), e.g., "com.acme.weather-nodes".</summary>
        string Id { get; }

        /// <summary>Human-readable extension name.</summary>
        string Name { get; }

        /// <summary>Extension version (semantic version recommended).</summary>
        string Version { get; }

        /// <summary>Short description shown in extension listings.</summary>
        string Description { get; }

        /// <summary>
        /// Component types contributed by this extension. Types must derive from
        /// <see cref="SkiaComponent"/> and have a public parameterless constructor.
        /// </summary>
        IEnumerable<Type> GetComponentTypes();

        /// <summary>
        /// Optional initialization hook for registering components with custom categories
        /// and commands. Called once after the extension is instantiated.
        /// </summary>
        void Initialize(ISkiaExtensionContext context);
    }

    /// <summary>
    /// Registration surface passed to <see cref="ISkiaExtension.Initialize"/>.
    /// </summary>
    public interface ISkiaExtensionContext
    {
        /// <summary>Registers a component type with an optional palette category and display name.</summary>
        void RegisterComponent(Type componentType, string category = null, string displayName = null);

        /// <summary>Registers a named command that can be invoked with the current selection.</summary>
        void RegisterCommand(string name, Action<SkiaComponent> command);

        /// <summary>Writes a diagnostic message to the extension host log.</summary>
        void Log(string message);
    }

    /// <summary>
    /// A component contributed by an extension.
    /// </summary>
    public sealed class ExtensionComponentDescriptor
    {
        /// <summary>
        /// Gets or sets the extension id.
        /// </summary>
        public string ExtensionId { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the component type.
        /// </summary>
        public Type ComponentType { get; set; }
        /// <summary>
        /// Gets or sets the category.
        /// </summary>
        public string Category { get; set; } = "Extensions";
        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the assembly qualified name.
        /// </summary>
        public string AssemblyQualifiedName => ComponentType?.AssemblyQualifiedName;

        /// <summary>
        /// Gets or sets the to string.
        /// </summary>
        public override string ToString() => $"{DisplayName ?? ComponentType?.Name} ({ExtensionId})";
    }

    /// <summary>
    /// Result of loading one extension.
    /// </summary>
    public sealed class LoadedExtension
    {
        /// <summary>
        /// Gets or sets the extension.
        /// </summary>
        public ISkiaExtension Extension { get; set; }
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        public string Version { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the component types.
        /// </summary>
        public List<Type> ComponentTypes { get; } = new List<Type>();
        /// <summary>
        /// Gets or sets the errors.
        /// </summary>
        public List<string> Errors { get; } = new List<string>();

        /// <summary>
        /// Gets or sets the success.
        /// </summary>
        public bool Success => Errors.Count == 0;

        /// <summary>
        /// Gets or sets the to string.
        /// </summary>
        public override string ToString() => $"{Name} {Version} ({(Success ? "ok" : "failed")})";
    }

    /// <summary>
    /// Raised after an extension has been loaded (successfully or with errors).
    /// </summary>
    public sealed class ExtensionLoadedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the ExtensionLoadedEventArgs class.
        /// </summary>
        public ExtensionLoadedEventArgs(LoadedExtension extension)
        {
            Extension = extension;
        }

        /// <summary>
        /// Gets or sets the extension.
        /// </summary>
        public LoadedExtension Extension { get; }
    }
}
