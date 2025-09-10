using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Beep.Skia
{
    /// <summary>
    /// Base class for all Skia components in the framework.
    /// Provides common functionality for positioning, sizing, rendering, and interaction.
    /// </summary>
    public abstract class SkiaComponent : IDrawableComponent, IDisposable
    {
        private bool _isDisposed;
        private ComponentState _state = ComponentState.Initializing;
        private SKRect _bounds;
        private bool _isVisible = true;
        private bool _isEnabled = true;
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
        protected virtual bool OnMouseUp(SKPoint point, InteractionContext context)
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
            BoundsChanged?.Invoke(this, new SKRectEventArgs(bounds));
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
