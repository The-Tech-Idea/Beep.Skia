using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beep.Skia
{
    /// <summary>
    /// Manages the lifecycle, rendering, and interaction of Skia components.
    /// This is the central coordinator for the Skia component framework.
    /// </summary>
    public class ComponentManager : IDisposable
    {
        private readonly List<SkiaComponent> _components;
        private readonly List<SkiaComponent> _componentsToAdd;
        private readonly List<SkiaComponent> _componentsToRemove;
    // The component that has captured the pointer after a successful mouse down
    private SkiaComponent _capturedComponent;
        private bool _isDisposed;
        private SKCanvas _canvas;
        private DrawingContext _drawingContext;
        private InteractionContext _interactionContext;

        /// <summary>
        /// Gets or sets the Skia canvas used for rendering.
        /// </summary>
        public SKCanvas Canvas
        {
            get => _canvas;
            set
            {
                if (_canvas != value)
                {
                    _canvas = value;
                    OnCanvasChanged();
                }
            }
        }

        /// <summary>
        /// Gets the current drawing context.
        /// </summary>
        public DrawingContext DrawingContext => _drawingContext;

        /// <summary>
        /// Gets the current interaction context.
        /// </summary>
        public InteractionContext InteractionContext => _interactionContext;

        /// <summary>
        /// Gets or sets the background color of the canvas.
        /// </summary>
        public SKColor BackgroundColor { get; set; } = SKColors.White;

        /// <summary>
        /// Gets or sets a value indicating whether the component manager should automatically clear the canvas before rendering.
        /// </summary>
        public bool AutoClearCanvas { get; set; } = true;

        /// <summary>
        /// Occurs when the canvas is changed.
        /// </summary>
        public event EventHandler CanvasChanged;

        /// <summary>
        /// Occurs before rendering begins.
        /// </summary>
        public event EventHandler Rendering;

        /// <summary>
        /// Occurs after rendering is complete.
        /// </summary>
        public event EventHandler Rendered;

        /// <summary>
        /// Occurs when a component is added to the manager.
        /// </summary>
        public event EventHandler<ComponentEventArgs> ComponentAdded;

        /// <summary>
        /// Occurs when a component is removed from the manager.
        /// </summary>
        public event EventHandler<ComponentEventArgs> ComponentRemoved;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentManager"/> class.
        /// </summary>
        public ComponentManager()
        {
            _components = new List<SkiaComponent>();
            _componentsToAdd = new List<SkiaComponent>();
            _componentsToRemove = new List<SkiaComponent>();
            _drawingContext = new DrawingContext();
            _interactionContext = new InteractionContext();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentManager"/> class with the specified canvas.
        /// </summary>
        /// <param name="canvas">The Skia canvas to use for rendering.</param>
        public ComponentManager(SKCanvas canvas) : this()
        {
            Canvas = canvas;
        }

        /// <summary>
        /// Adds a component to the manager.
        /// </summary>
        /// <param name="component">The component to add.</param>
        public void AddComponent(SkiaComponent component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            if (_isDisposed)
                throw new ObjectDisposedException(nameof(ComponentManager));

            _componentsToAdd.Add(component);
        }

        /// <summary>
        /// Removes a component from the manager.
        /// </summary>
        /// <param name="component">The component to remove.</param>
        public void RemoveComponent(SkiaComponent component)
        {
            if (component == null)
                return;

            if (_isDisposed)
                throw new ObjectDisposedException(nameof(ComponentManager));

            _componentsToRemove.Add(component);
        }

        /// <summary>
        /// Gets all components managed by this manager.
        /// </summary>
        /// <returns>A read-only collection of components.</returns>
        public IReadOnlyCollection<SkiaComponent> GetComponents()
        {
            return _components.AsReadOnly();
        }

        /// <summary>
        /// Finds a component by name.
        /// </summary>
        /// <param name="name">The name of the component to find.</param>
        /// <returns>The component with the specified name, or null if not found.</returns>
        public SkiaComponent FindComponent(string name)
        {
            return _components.FirstOrDefault(c => c.Name == name);
        }

        /// <summary>
        /// Finds components of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of components to find.</typeparam>
        /// <returns>A collection of components of the specified type.</returns>
        public IEnumerable<T> FindComponents<T>() where T : SkiaComponent
        {
            return _components.OfType<T>();
        }

        /// <summary>
        /// Updates all managed components.
        /// </summary>
        public void Update()
        {
            if (_isDisposed || Canvas == null)
                return;

            // Process pending additions and removals
            ProcessPendingChanges();

            // Update all components
            foreach (var component in _components)
            {
                component.Update(_drawingContext);
            }
        }

        /// <summary>
        /// Renders all managed components to the canvas.
        /// </summary>
        public void Render()
        {
            if (_isDisposed || Canvas == null)
                return;

            // Process pending additions and removals
            ProcessPendingChanges();

            // Raise rendering event
            Rendering?.Invoke(this, EventArgs.Empty);

            // Clear canvas if requested
            if (AutoClearCanvas)
            {
                Canvas.Clear(BackgroundColor);
            }

            // Render all visible components in priority order
            foreach (var component in _components
                .Where(c => c.IsVisible)
                .OrderBy(c => c.Priority))
            {
                component.Draw(Canvas, _drawingContext);
            }

            // Raise rendered event
            Rendered?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Handles mouse down events.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="button">The mouse button that was pressed.</param>
        /// <param name="modifiers">Modifier keys that were pressed.</param>
        /// <returns>true if the event was handled; otherwise, false.</returns>
        public bool HandleMouseDown(SKPoint point, int button = 0, int modifiers = 0)
        {
            if (_isDisposed)
                return false;

            _interactionContext.MousePosition = point;
            _interactionContext.MouseButton = button;
            _interactionContext.Modifiers = modifiers;

            // Process in reverse order for proper hit testing
            foreach (var component in _components
                .Where(c => c.IsVisible && c.IsEnabled)
                .OrderByDescending(c => c.Priority))
            {
                if (component.ContainsPoint(point) && component.HandleMouseDown(point, _interactionContext))
                {
                    // capture the pointer so move/up continue to be delivered to this component
                    _capturedComponent = component;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Handles mouse move events.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="modifiers">Modifier keys that were pressed.</param>
        /// <returns>true if the event was handled; otherwise, false.</returns>
        public bool HandleMouseMove(SKPoint point, int modifiers = 0)
        {
            if (_isDisposed)
                return false;

            _interactionContext.MousePosition = point;
            _interactionContext.Modifiers = modifiers;

            // If a component captured the pointer on MouseDown, forward moves to it regardless of containment
            if (_capturedComponent != null)
            {
                try
                {
                    if (_capturedComponent.IsVisible && _capturedComponent.IsEnabled)
                    {
                        if (_capturedComponent.HandleMouseMove(point, _interactionContext))
                            return true;
                    }
                }
                catch { }
            }

            // Process in reverse order for proper hit testing
            foreach (var component in _components
                .Where(c => c.IsVisible && c.IsEnabled)
                .OrderByDescending(c => c.Priority))
            {
                if (component.ContainsPoint(point) && component.HandleMouseMove(point, _interactionContext))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Handles mouse up events.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="button">The mouse button that was released.</param>
        /// <param name="modifiers">Modifier keys that were pressed.</param>
        /// <returns>true if the event was handled; otherwise, false.</returns>
        public bool HandleMouseUp(SKPoint point, int button = 0, int modifiers = 0)
        {
            if (_isDisposed)
                return false;

            _interactionContext.MousePosition = point;
            _interactionContext.MouseButton = button;
            _interactionContext.Modifiers = modifiers;

            // If a component captured the pointer on MouseDown, forward the up to it regardless of containment
            if (_capturedComponent != null)
            {
                try
                {
                    if (_capturedComponent.IsVisible && _capturedComponent.IsEnabled)
                    {
                        if (_capturedComponent.HandleMouseUp(point, _interactionContext))
                        {
                            // release capture after mouse up
                            _capturedComponent = null;
                            return true;
                        }
                    }
                }
                catch { }
                finally
                {
                    // ensure capture is released even on exceptions
                    _capturedComponent = null;
                }
            }

            // Process in reverse order for proper hit testing
            foreach (var component in _components
                .Where(c => c.IsVisible && c.IsEnabled)
                .OrderByDescending(c => c.Priority))
            {
                if (component.ContainsPoint(point) && component.HandleMouseUp(point, _interactionContext))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Processes pending component additions and removals.
        /// </summary>
        private void ProcessPendingChanges()
        {
            // Add new components
            foreach (var component in _componentsToAdd)
            {
                if (!_components.Contains(component))
                {
                    _components.Add(component);
                    OnComponentAdded(component);
                }
            }
            _componentsToAdd.Clear();

            // Remove components
            foreach (var component in _componentsToRemove)
            {
                if (_components.Remove(component))
                {
                    OnComponentRemoved(component);
                }
            }
            _componentsToRemove.Clear();
        }

        /// <summary>
        /// Called when the canvas is changed.
        /// </summary>
        protected virtual void OnCanvasChanged()
        {
            CanvasChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called when a component is added.
        /// </summary>
        /// <param name="component">The component that was added.</param>
        protected virtual void OnComponentAdded(SkiaComponent component)
        {
            ComponentAdded?.Invoke(this, new ComponentEventArgs(component));
        }

        /// <summary>
        /// Called when a component is removed.
        /// </summary>
        /// <param name="component">The component that was removed.</param>
        protected virtual void OnComponentRemoved(SkiaComponent component)
        {
            ComponentRemoved?.Invoke(this, new ComponentEventArgs(component));
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
                    // Dispose all components
                    foreach (var component in _components)
                    {
                        component.Dispose();
                    }
                    _components.Clear();

                    // Clear event handlers
                    CanvasChanged = null;
                    Rendering = null;
                    Rendered = null;
                    ComponentAdded = null;
                    ComponentRemoved = null;
                }

                _isDisposed = true;
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="ComponentManager"/> class.
        /// </summary>
        ~ComponentManager()
        {
            Dispose(false);
        }
    }

    /// <summary>
    /// Provides event arguments for component-related events.
    /// </summary>
    public class ComponentEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the component associated with the event.
        /// </summary>
        public SkiaComponent Component { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentEventArgs"/> class.
        /// </summary>
        /// <param name="component">The component associated with the event.</param>
        public ComponentEventArgs(SkiaComponent component)
        {
            Component = component ?? throw new ArgumentNullException(nameof(component));
        }
    }
}
