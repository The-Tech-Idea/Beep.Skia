using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;

namespace Beep.Skia
{
    /// <summary>
    /// Result of component extraction operation containing the component instance and metadata.
    /// </summary>
    public class ExtractedComponentResult
    {
        /// <summary>
        /// Gets or sets whether the extraction was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the extracted component instance.
        /// </summary>
        public SkiaComponent Component { get; set; }

        /// <summary>
        /// Gets or sets the original component definition.
        /// </summary>
        public AssemblyClassDefinition Definition { get; set; }

        /// <summary>
        /// Gets or sets the component type.
        /// </summary>
        public Type ComponentType { get; set; }

        /// <summary>
        /// Gets or sets the assembly name where the component is defined.
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        /// Gets or sets the namespace of the component.
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Gets or sets the list of methods available in the component.
        /// </summary>
        public List<MethodsClass> Methods { get; set; }

        /// <summary>
        /// Gets or sets the list of errors that occurred during extraction.
        /// </summary>
        public List<string> Errors { get; set; }

        /// <summary>
        /// Gets a formatted string of all errors.
        /// </summary>
        public string ErrorMessage => Errors != null && Errors.Count > 0 ?
            string.Join("; ", Errors) : string.Empty;
    }

    /// <summary>
    /// Static registry for managing loaded Skia components.
    /// This class provides centralized access to all available SkiaComponent types
    /// that have been registered by external loaders.
    /// </summary>
    public static class SkiaComponentRegistry
    {
        private static readonly object _lockObject = new object();
        private static List<AssemblyClassDefinition> _loadedComponents;
        private static bool _isInitialized = false;

        /// <summary>
        /// Gets the list of all loaded Skia components.
        /// </summary>
        public static List<AssemblyClassDefinition> LoadedComponents
        {
            get
            {
                lock (_lockObject)
                {
                    return _loadedComponents ?? new List<AssemblyClassDefinition>();
                }
            }
        }

        /// <summary>
        /// Gets whether the registry has been initialized.
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// Sets the component list from an external loader.
        /// This method should be called by the external loader after loading components.
        /// </summary>
        /// <param name="components">The list of AssemblyClassDefinition objects to register.</param>
        public static void SetComponents(List<AssemblyClassDefinition> components)
        {
            lock (_lockObject)
            {
                _loadedComponents = components ?? new List<AssemblyClassDefinition>();
                _isInitialized = true;
            }
        }

        /// <summary>
        /// Adds a single component to the registry.
        /// </summary>
        /// <param name="component">The AssemblyClassDefinition to add.</param>
        public static void AddComponent(AssemblyClassDefinition component)
        {
            if (component == null)
                return;

            lock (_lockObject)
            {
                if (_loadedComponents == null)
                {
                    _loadedComponents = new List<AssemblyClassDefinition>();
                }

                // Check if component already exists (by GuidID or className)
                var existing = _loadedComponents.FirstOrDefault(c =>
                    (c.GuidID == component.GuidID) ||
                    (c.className == component.className && c.dllname == component.dllname));

                if (existing == null)
                {
                    _loadedComponents.Add(component);
                }
                else
                {
                    // Update existing component
                    var index = _loadedComponents.IndexOf(existing);
                    _loadedComponents[index] = component;
                }

                _isInitialized = true;
            }
        }

        /// <summary>
        /// Adds multiple components to the registry.
        /// </summary>
        /// <param name="components">The list of AssemblyClassDefinition objects to add.</param>
        public static void AddComponents(IEnumerable<AssemblyClassDefinition> components)
        {
            if (components == null)
                return;

            lock (_lockObject)
            {
                if (_loadedComponents == null)
                {
                    _loadedComponents = new List<AssemblyClassDefinition>();
                }

                foreach (var component in components)
                {
                    if (component != null)
                    {
                        AddComponent(component);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a component definition by its class name.
        /// </summary>
        /// <param name="className">The class name to search for.</param>
        /// <returns>The AssemblyClassDefinition if found, null otherwise.</returns>
        public static AssemblyClassDefinition GetComponentByName(string className)
        {
            if (string.IsNullOrEmpty(className))
                return null;

            lock (_lockObject)
            {
                return _loadedComponents?.FirstOrDefault(c =>
                    string.Equals(c.className, className, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Gets a component definition by its GUID.
        /// </summary>
        /// <param name="guid">The GUID to search for.</param>
        /// <returns>The AssemblyClassDefinition if found, null otherwise.</returns>
        public static AssemblyClassDefinition GetComponentByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return null;

            lock (_lockObject)
            {
                return _loadedComponents?.FirstOrDefault(c =>
                    string.Equals(c.GuidID, guid, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Gets all component definitions from a specific assembly.
        /// </summary>
        /// <param name="assemblyName">The assembly name to filter by.</param>
        /// <returns>A list of AssemblyClassDefinition objects from the specified assembly.</returns>
        public static List<AssemblyClassDefinition> GetComponentsByAssembly(string assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyName))
                return new List<AssemblyClassDefinition>();

            lock (_lockObject)
            {
                return _loadedComponents?.Where(c =>
                    string.Equals(c.dllname, assemblyName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(c.AssemblyName, assemblyName, StringComparison.OrdinalIgnoreCase))
                    .ToList() ?? new List<AssemblyClassDefinition>();
            }
        }

        /// <summary>
        /// Gets all component definitions that implement a specific interface or inherit from a specific base class.
        /// </summary>
        /// <param name="typeName">The type name to filter by.</param>
        /// <returns>A list of AssemblyClassDefinition objects that match the type.</returns>
        public static List<AssemblyClassDefinition> GetComponentsByType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return new List<AssemblyClassDefinition>();

            lock (_lockObject)
            {
                return _loadedComponents?.Where(c =>
                    c.type != null && c.type.Name.Contains(typeName))
                    .ToList() ?? new List<AssemblyClassDefinition>();
            }
        }

        /// <summary>
        /// Creates an instance of a SkiaComponent from its AssemblyClassDefinition.
        /// </summary>
        /// <param name="componentDefinition">The component definition to instantiate.</param>
        /// <returns>An instance of the SkiaComponent if successful, null otherwise.</returns>
        public static SkiaComponent CreateComponentInstance(AssemblyClassDefinition componentDefinition)
        {
            if (componentDefinition?.type == null)
                return null;

            try
            {
                var instance = Activator.CreateInstance(componentDefinition.type) as SkiaComponent;
                return instance;
            }
            catch
            {
                // Log exception if needed
                return null;
            }
        }

        /// <summary>
        /// Creates an instance of a SkiaComponent by its class name.
        /// </summary>
        /// <param name="className">The class name of the component to create.</param>
        /// <returns>An instance of the SkiaComponent if successful, null otherwise.</returns>
        public static SkiaComponent CreateComponentInstance(string className)
        {
            var definition = GetComponentByName(className);
            return CreateComponentInstance(definition);
        }

        /// <summary>
        /// Clears all loaded components and resets the registry.
        /// </summary>
        public static void Clear()
        {
            lock (_lockObject)
            {
                _loadedComponents?.Clear();
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Gets the count of loaded components.
        /// </summary>
        public static int ComponentCount
        {
            get
            {
                lock (_lockObject)
                {
                    return _loadedComponents?.Count ?? 0;
                }
            }
        }

        /// <summary>
        /// Checks if a component with the specified name is registered.
        /// </summary>
        /// <param name="className">The class name to check.</param>
        /// <returns>True if the component is registered, false otherwise.</returns>
        public static bool IsComponentRegistered(string className)
        {
            return GetComponentByName(className) != null;
        }

        /// <summary>
        /// Gets all components that have methods (useful for components with interactive functionality).
        /// </summary>
        /// <returns>A list of AssemblyClassDefinition objects that have methods defined.</returns>
        public static List<AssemblyClassDefinition> GetComponentsWithMethods()
        {
            lock (_lockObject)
            {
                var components = _loadedComponents?.Where(c =>
                    c.Methods != null && c.Methods.Count > 0
                ).ToList() ?? new List<AssemblyClassDefinition>();

                return components;
            }
        }

        /// <summary>
        /// Gets all components that have visual properties or attributes.
        /// </summary>
        /// <returns>A list of AssemblyClassDefinition objects that have visual properties.</returns>
        public static List<AssemblyClassDefinition> GetComponentsWithVisualProperties()
        {
            lock (_lockObject)
            {
                var components = _loadedComponents?.Where(c =>
                    c.classProperties != null ||
                    (c.Methods != null && c.Methods.Any(m => m.iconimage != null))
                ).ToList() ?? new List<AssemblyClassDefinition>();

                return components;
            }
        }

        /// <summary>
        /// Gets all components that implement specific interfaces or have specific properties.
        /// </summary>
        /// <param name="hasLocalDB">Filter by components that support LocalDB.</param>
        /// <param name="isDataSource">Filter by components that are data sources.</param>
        /// <param name="inMemory">Filter by components that support in-memory operations.</param>
        /// <returns>A list of AssemblyClassDefinition objects that match the criteria.</returns>
        public static List<AssemblyClassDefinition> GetComponentsByCapabilities(bool? hasLocalDB = null, bool? isDataSource = null, bool? inMemory = null)
        {
            lock (_lockObject)
            {
                var components = _loadedComponents?.Where(c =>
                    (hasLocalDB == null || c.LocalDB == hasLocalDB) &&
                    (isDataSource == null || c.IsDataSource == isDataSource) &&
                    (inMemory == null || c.InMemory == inMemory)
                ).ToList() ?? new List<AssemblyClassDefinition>();

                return components;
            }
        }

        /// <summary>
        /// Gets components ordered by their specified order property.
        /// </summary>
        /// <returns>A list of AssemblyClassDefinition objects ordered by their Order property.</returns>
        public static List<AssemblyClassDefinition> GetComponentsOrdered()
        {
            lock (_lockObject)
            {
                return _loadedComponents?.OrderBy(c => c.Order).ToList() ?? new List<AssemblyClassDefinition>();
            }
        }

        /// <summary>
        /// Gets all unique namespaces from registered components.
        /// </summary>
        /// <returns>A list of unique namespace names.</returns>
        public static List<string> GetAvailableNamespaces()
        {
            lock (_lockObject)
            {
                var namespaces = _loadedComponents?
                    .Where(c => c.type != null && !string.IsNullOrEmpty(c.type.Namespace))
                    .Select(c => c.type.Namespace)
                    .Distinct()
                    .OrderBy(ns => ns)
                    .ToList() ?? new List<string>();

                return namespaces;
            }
        }

        /// <summary>
        /// Gets all unique assembly names from registered components.
        /// </summary>
        /// <returns>A list of unique assembly names.</returns>
        public static List<string> GetAvailableAssemblies()
        {
            lock (_lockObject)
            {
                var assemblies = _loadedComponents?
                    .Where(c => !string.IsNullOrEmpty(c.dllname) || !string.IsNullOrEmpty(c.AssemblyName))
                    .Select(c => c.dllname ?? c.AssemblyName)
                    .Distinct()
                    .OrderBy(asm => asm)
                    .ToList() ?? new List<string>();

                return assemblies;
            }
        }

        /// <summary>
        /// Gets component methods that match specific criteria.
        /// </summary>
        /// <param name="componentName">The component name to filter by (optional).</param>
        /// <param name="hidden">Filter by visibility (optional).</param>
        /// <returns>A list of matching MethodsClass objects with their parent component information.</returns>
        public static List<(AssemblyClassDefinition Component, MethodsClass Method)> GetComponentMethods(
            string componentName = null,
            bool? hidden = null)
        {
            lock (_lockObject)
            {
                var methods = _loadedComponents?
                    .Where(c => componentName == null || c.className == componentName)
                    .Where(c => c.Methods != null)
                    .SelectMany(c => c.Methods.Select(m => (Component: c, Method: m)))
                    .Where(cm => hidden == null || cm.Method.Hidden == hidden)
                    .ToList() ?? new List<(AssemblyClassDefinition, MethodsClass)>();

                return methods;
            }
        }

        /// <summary>
        /// Registers components from the Beep configuration system.
        /// This method should be called after the Beep loader has scanned assemblies.
        /// </summary>
        /// <param name="configEditor">The configuration editor containing the loaded components.</param>
        /// <param name="componentType">The type of components to register (e.g., "SkiaComponent").</param>
        /// <returns>The number of components that were registered.</returns>
        public static int RegisterFromConfigEditor(IConfigEditor configEditor, string componentType = "SkiaComponent")
        {
            if (configEditor?.AppComponents == null)
                return 0;

            var skiaComponents = configEditor.AppComponents
                .Where(c => c.componentType == componentType && c.type != null)
                .ToList();

            AddComponents(skiaComponents);
            return skiaComponents.Count;
        }

        /// <summary>
        /// Scans the currently loaded AppDomain assemblies for types that inherit from SkiaComponent
        /// and registers them into the component registry.
        /// </summary>
        /// <param name="includeSystemAssemblies">If true, will not skip assemblies with common system prefixes.</param>
        /// <returns>List of AssemblyClassDefinition objects that were discovered and registered.</returns>
        public static List<AssemblyClassDefinition> DiscoverAndRegisterDomainComponents(bool includeSystemAssemblies = false)
        {
            var results = new List<AssemblyClassDefinition>();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                try
                {
                    var asmName = asm?.GetName()?.Name ?? string.Empty;
                    if (!includeSystemAssemblies)
                    {
                        if (string.IsNullOrEmpty(asmName))
                            continue;
                        // skip well-known system assemblies for performance
                        if (asmName.StartsWith("System", StringComparison.OrdinalIgnoreCase)
                            || asmName.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase)
                            || asmName.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)
                            || asmName.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                    }

                    Type[] types = null;
                    try { types = asm.GetTypes(); } catch { try { types = asm.GetExportedTypes(); } catch { types = null; } }
                    if (types == null) continue;

                    foreach (var t in types)
                    {
                        try
                        {
                            if (t == null) continue;
                            if (typeof(SkiaComponent).IsAssignableFrom(t) && !t.IsAbstract)
                            {
                                var def = new AssemblyClassDefinition();
                                // populate minimal fields used elsewhere
                                def.type = t;
                                def.className = t.Name;
                                def.dllname = asm.GetName().Name;
                                def.AssemblyName = asm.FullName;
                                def.componentType = "SkiaComponent";
                                def.GuidID = Guid.NewGuid().ToString();

                                AddComponent(def);
                                results.Add(def);
                            }
                        }
                        catch { /* ignore individual type failures */ }
                    }
                }
                catch { /* ignore assembly scan failures */ }
            }

            return results;
        }

        /// <summary>
        /// Creates a detailed component instance with full metadata initialization.
        /// </summary>
        /// <param name="componentDefinition">The component definition to instantiate.</param>
        /// <returns>An ExtractedComponentResult with detailed component information.</returns>
        public static ExtractedComponentResult ExtractComponentDetailed(AssemblyClassDefinition componentDefinition)
        {
            var result = new ExtractedComponentResult
            {
                Definition = componentDefinition,
                Success = false,
                Errors = new List<string>()
            };

            if (componentDefinition == null)
            {
                result.Errors.Add("Component definition is null");
                return result;
            }

            // Create the component instance
            var component = CreateComponentInstance(componentDefinition);

            if (component == null)
            {
                result.Errors.Add("Failed to create component instance");
                return result;
            }

            // Set basic properties
            result.Component = component;
            result.ComponentType = componentDefinition.type;
            result.AssemblyName = componentDefinition.dllname ?? componentDefinition.AssemblyName;
            result.Namespace = componentDefinition.type?.Namespace;
            result.Methods = componentDefinition.Methods ?? new List<MethodsClass>();
            result.Success = true;

            // Set additional properties from the definition
            if (componentDefinition.classProperties != null)
            {
                // You can add custom initialization here based on AddinAttribute properties
                // For example: result.Component.DisplayName = componentDefinition.classProperties.Caption;
            }

            // Initialize component with method information
            if (componentDefinition.Methods != null && componentDefinition.Methods.Count > 0)
            {
                // Store method information for later use
                result.Component.Tag = new ComponentMetadata
                {
                    Definition = componentDefinition,
                    Methods = componentDefinition.Methods,
                    Categories = new List<string>() // Empty list since Category is not relevant for Skia components
                };
            }

            return result;
        }

        /// <summary>
        /// Metadata container for component information.
        /// </summary>
        public class ComponentMetadata
        {
            /// <summary>
            /// The component definition.
            /// </summary>
            public AssemblyClassDefinition Definition { get; set; }

            /// <summary>
            /// Available methods for the component.
            /// </summary>
            public List<MethodsClass> Methods { get; set; }

            /// <summary>
            /// Available categories for the component methods.
            /// </summary>
            public List<string> Categories { get; set; }
        }
    }
}
