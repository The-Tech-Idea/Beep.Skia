using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Beep.Skia.Extensions
{
    /// <summary>
    /// Discovers and loads Beep.Skia extension packages from assemblies or a plugin directory,
    /// collecting their component types and commands for palette/editor integration.
    /// </summary>
    public class SkiaExtensionHost
    {
        private readonly List<LoadedExtension> _extensions = new List<LoadedExtension>();
        private readonly Dictionary<string, ExtensionComponentDescriptor> _components =
            new Dictionary<string, ExtensionComponentDescriptor>(StringComparer.Ordinal);
        private readonly Dictionary<string, Action<SkiaComponent>> _commands =
            new Dictionary<string, Action<SkiaComponent>>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _log = new List<string>();

        /// <summary>Loaded extensions (successful and failed).</summary>
        public IReadOnlyList<LoadedExtension> Extensions => _extensions.AsReadOnly();

        /// <summary>Component descriptors contributed by all loaded extensions.</summary>
        public IReadOnlyList<ExtensionComponentDescriptor> Components => _components.Values.ToList().AsReadOnly();

        /// <summary>Commands registered by extensions (name → action).</summary>
        public IReadOnlyDictionary<string, Action<SkiaComponent>> Commands => _commands;

        /// <summary>Diagnostic log collected during loading.</summary>
        public IReadOnlyList<string> Log => _log.AsReadOnly();

        /// <summary>Raised after each extension is loaded.</summary>
        public event EventHandler<ExtensionLoadedEventArgs> ExtensionLoaded;

        /// <summary>
        /// Loads every <see cref="ISkiaExtension"/> found in the given assemblies.
        /// Returns the number of extensions loaded (including ones that failed initialization).
        /// </summary>
        public int LoadFromAssemblies(IEnumerable<Assembly> assemblies)
        {
            if (assemblies == null) return 0;

            int loaded = 0;
            foreach (var assembly in assemblies)
            {
                if (assembly == null) continue;

                IEnumerable<Type> types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null);
                    AddLog($"Partial type load for {assembly.GetName().Name}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    AddLog($"Failed to inspect {assembly.GetName().Name}: {ex.Message}");
                    continue;
                }

                foreach (var type in types)
                {
                    if (type == null || type.IsAbstract || type.IsInterface) continue;
                    if (!typeof(ISkiaExtension).IsAssignableFrom(type)) continue;
                    if (type.GetConstructor(Type.EmptyTypes) == null)
                    {
                        AddLog($"Extension {type.FullName} has no public parameterless constructor; skipped.");
                        continue;
                    }

                    if (LoadExtension(type) != null) loaded++;
                }
            }

            return loaded;
        }

        /// <summary>
        /// Loads extensions from every matching assembly in a plugin directory.
        /// </summary>
        public int LoadFromDirectory(string directory, string searchPattern = "*.dll")
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                AddLog($"Extension directory not found: {directory}");
                return 0;
            }

            var assemblies = new List<Assembly>();
            foreach (var file in Directory.GetFiles(directory, searchPattern))
            {
                try
                {
                    assemblies.Add(Assembly.LoadFrom(file));
                }
                catch (Exception ex)
                {
                    AddLog($"Failed to load {Path.GetFileName(file)}: {ex.Message}");
                }
            }

            return LoadFromAssemblies(assemblies);
        }

        /// <summary>
        /// Creates a component instance from an extension component descriptor's type name.
        /// </summary>
        public SkiaComponent CreateComponent(string assemblyQualifiedName)
        {
            if (string.IsNullOrWhiteSpace(assemblyQualifiedName)) return null;

            try
            {
                var type = Type.GetType(assemblyQualifiedName, throwOnError: false);
                if (type == null || !typeof(SkiaComponent).IsAssignableFrom(type)) return null;
                return Activator.CreateInstance(type) as SkiaComponent;
            }
            catch (Exception ex)
            {
                AddLog($"Failed to create component '{assemblyQualifiedName}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Invokes a registered extension command for the given target component.
        /// </summary>
        public bool InvokeCommand(string name, SkiaComponent target)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (!_commands.TryGetValue(name, out var command)) return false;

            try
            {
                command?.Invoke(target);
                return true;
            }
            catch (Exception ex)
            {
                AddLog($"Command '{name}' failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>Clears all loaded extensions, components, commands, and the log.</summary>
        public void Clear()
        {
            _extensions.Clear();
            _components.Clear();
            _commands.Clear();
            _log.Clear();
        }

        private LoadedExtension LoadExtension(Type extensionType)
        {
            ISkiaExtension extension;
            try
            {
                extension = (ISkiaExtension)Activator.CreateInstance(extensionType);
            }
            catch (Exception ex)
            {
                AddLog($"Failed to instantiate extension {extensionType.FullName}: {ex.Message}");
                return null;
            }

            string id;
            try { id = extension.Id; }
            catch { id = extensionType.FullName; }

            if (!string.IsNullOrWhiteSpace(id) &&
                _extensions.Any(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase)))
            {
                AddLog($"Extension '{id}' is already loaded; skipping duplicate.");
                return null;
            }

            var loaded = new LoadedExtension { Extension = extension, Id = id ?? extensionType.FullName };

            try { loaded.Name = extension.Name ?? loaded.Id; }
            catch (Exception ex) { loaded.Errors.Add($"Name: {ex.Message}"); }

            try { loaded.Version = extension.Version ?? string.Empty; }
            catch (Exception ex) { loaded.Errors.Add($"Version: {ex.Message}"); }

            try { loaded.Description = extension.Description ?? string.Empty; }
            catch (Exception ex) { loaded.Errors.Add($"Description: {ex.Message}"); }

            // Collect declared component types.
            try
            {
                foreach (var type in extension.GetComponentTypes() ?? Enumerable.Empty<Type>())
                {
                    RegisterComponent(loaded, type, null, null);
                }
            }
            catch (Exception ex)
            {
                loaded.Errors.Add($"GetComponentTypes: {ex.Message}");
            }

            // Optional initialization hook.
            try
            {
                extension.Initialize(new ExtensionContext(this, loaded));
            }
            catch (Exception ex)
            {
                loaded.Errors.Add($"Initialize: {ex.Message}");
            }

            _extensions.Add(loaded);
            AddLog($"Loaded extension '{loaded.Name}' v{loaded.Version} with {loaded.ComponentTypes.Count} component(s).");

            try { ExtensionLoaded?.Invoke(this, new ExtensionLoadedEventArgs(loaded)); } catch { }

            return loaded;
        }

        private void RegisterComponent(LoadedExtension owner, Type componentType, string category, string displayName)
        {
            if (componentType == null || owner == null) return;

            if (!typeof(SkiaComponent).IsAssignableFrom(componentType) || componentType.IsAbstract)
            {
                AddLog($"Extension '{owner.Id}' registered '{componentType.FullName}', which is not a concrete SkiaComponent; skipped.");
                return;
            }

            var aqn = componentType.AssemblyQualifiedName;
            if (string.IsNullOrWhiteSpace(aqn) || _components.ContainsKey(aqn)) return;

            _components[aqn] = new ExtensionComponentDescriptor
            {
                ExtensionId = owner.Id,
                ComponentType = componentType,
                Category = string.IsNullOrWhiteSpace(category) ? "Extensions" : category,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? componentType.Name : displayName
            };

            if (!owner.ComponentTypes.Contains(componentType))
                owner.ComponentTypes.Add(componentType);
        }

        private void RegisterCommand(LoadedExtension owner, string name, Action<SkiaComponent> command)
        {
            if (owner == null || string.IsNullOrWhiteSpace(name) || command == null) return;
            _commands[name] = command;
        }

        private void AddLog(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            _log.Add($"[{DateTime.UtcNow:HH:mm:ss}] {message}");
        }

        /// <summary>
        /// Registration surface handed to extensions during initialization.
        /// </summary>
        private sealed class ExtensionContext : ISkiaExtensionContext
        {
            private readonly SkiaExtensionHost _host;
            private readonly LoadedExtension _owner;

            /// <summary>
            /// Initializes a new instance of the ExtensionContext class.
            /// </summary>
            public ExtensionContext(SkiaExtensionHost host, LoadedExtension owner)
            {
                _host = host;
                _owner = owner;
            }

            /// <summary>
            /// Gets or sets the register component.
            /// </summary>
            public void RegisterComponent(Type componentType, string category = null, string displayName = null)
                => _host.RegisterComponent(_owner, componentType, category, displayName);

            /// <summary>
            /// Gets or sets the register command.
            /// </summary>
            public void RegisterCommand(string name, Action<SkiaComponent> command)
                => _host.RegisterCommand(_owner, name, command);

            /// <summary>
            /// Gets or sets the log.
            /// </summary>
            public void Log(string message)
                => _host.AddLog($"[{_owner.Id}] {message}");
        }
    }
}
