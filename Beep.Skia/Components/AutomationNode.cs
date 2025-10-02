using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Beep.Skia.Model;

namespace Beep.Skia.Components
{
    /// <summary>
    /// Base class for automation nodes that provides both visual representation and automation execution capabilities.
    /// Combines SkiaComponent visual behavior with IAutomationNode automation interface.
    /// </summary>
    public abstract class AutomationNode : SkiaComponent, IAutomationNode
    {
        #region Private Fields
        private NodeStatus _status = NodeStatus.Idle;
        private Dictionary<string, object> _configuration = new Dictionary<string, object>();
        private bool _isEnabled = true;
        private readonly List<IConnectionPoint> _inputConnections = new List<IConnectionPoint>();
        private readonly List<IConnectionPoint> _outputConnections = new List<IConnectionPoint>();
        #endregion

        #region IAutomationNode Properties
        /// <summary>
        /// Gets the unique identifier for this automation node.
        /// </summary>
        public new string Id => base.Id.ToString();

        /// <summary>
        /// Gets the display name of this automation node.
        /// </summary>
        public new string Name
        {
            get => base.Name ?? GetType().Name;
            set => base.Name = value;
        }

        /// <summary>
        /// Gets the description of what this automation node does.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Gets the type of this automation node.
        /// </summary>
        public abstract NodeType NodeType { get; }

        /// <summary>
        /// Gets the current status of this automation node.
        /// </summary>
        public NodeStatus Status
        {
            get => _status;
            protected set
            {
                if (_status != value)
                {
                    var oldStatus = _status;
                    _status = value;
                    OnStatusChanged(oldStatus, value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the configuration parameters for this node.
        /// </summary>
        public Dictionary<string, object> Configuration
        {
            get => _configuration;
            set => _configuration = value ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets the input connection points for this automation node.
        /// </summary>
        public IList<IConnectionPoint> InputConnections => _inputConnections;

        /// <summary>
        /// Gets the output connection points for this automation node.
        /// </summary>
        public IList<IConnectionPoint> OutputConnections => _outputConnections;

        /// <summary>
        /// Gets a value indicating whether this node is currently enabled for execution.
        /// </summary>
        public new bool IsEnabled
        {
            get => _isEnabled && base.IsEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    base.IsEnabled = value;
                    OnEnabledStateChanged();
                }
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Event raised when the node's execution status changes.
        /// </summary>
        public event EventHandler<NodeExecutionEventArgs> StatusChanged;

        /// <summary>
        /// Event raised when the node encounters an error during execution.
        /// </summary>
        public event EventHandler<NodeErrorEventArgs> ErrorOccurred;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the AutomationNode class.
        /// </summary>
        protected AutomationNode()
        {
            // Set default automation node dimensions
            Width = 150;
            Height = 80;
            
            // Initialize default connection points
            InitializeConnectionPoints();
        }
        #endregion

        #region Abstract Methods
        /// <summary>
        /// Initializes the automation node with the specified configuration.
        /// </summary>
        /// <param name="configuration">The configuration parameters for the node.</param>
        /// <param name="cancellationToken">Token to cancel the initialization.</param>
        /// <returns>A task representing the initialization operation.</returns>
        public abstract Task<bool> InitializeAsync(Dictionary<string, object> configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates the current configuration and input data for this node.
        /// </summary>
        /// <param name="inputData">The input data to validate.</param>
        /// <param name="cancellationToken">Token to cancel the validation.</param>
        /// <returns>A task containing the validation result.</returns>
        public abstract Task<ValidationResult> ValidateAsync(Model.ExecutionContext inputData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the automation node's logic with the provided execution context.
        /// </summary>
        /// <param name="context">The execution context containing input data and workflow state.</param>
        /// <param name="cancellationToken">Token to cancel the execution.</param>
        /// <returns>A task containing the execution result.</returns>
        public abstract Task<NodeResult> ExecuteAsync(Model.ExecutionContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the schema defining the input parameters this node expects.
        /// </summary>
        /// <returns>A dictionary describing the input schema.</returns>
        public abstract Dictionary<string, object> GetInputSchema();

        /// <summary>
        /// Gets the schema defining the output parameters this node produces.
        /// </summary>
        /// <returns>A dictionary describing the output schema.</returns>
        public abstract Dictionary<string, object> GetOutputSchema();
        #endregion

        #region Virtual Methods
        /// <summary>
        /// Resets the node to its initial state, clearing any cached data or temporary state.
        /// </summary>
        public virtual Task ResetAsync()
        {
            Status = NodeStatus.Idle;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the data type of the output for this node.
        /// Override in derived classes to provide specific data types.
        /// </summary>
        /// <returns>The output data type string.</returns>
        protected virtual string GetOutputDataType()
        {
            // Default implementation - can be overridden by specific node types
            return "object";
        }

        /// <summary>
        /// Initializes the default connection points for the node.
        /// Override to customize connection points for specific node types.
        /// </summary>
        protected virtual void InitializeConnectionPoints()
        {
            // Most nodes have at least one input and one output
            if (NodeType != NodeType.Trigger)
            {
                var inputPoint = new ConnectionPoint
                {
                    Type = ConnectionPointType.In,
                    Position = new SKPoint(0, Height / 2),
                    Component = this,
                    Radius = 8,
                    Shape = ComponentShape.Circle,
                    DataType = "any" // Default input accepts any data type
                };
                _inputConnections.Add(inputPoint);
            }

            var outputPoint = new ConnectionPoint
            {
                Type = ConnectionPointType.Out,
                Position = new SKPoint(Width, Height / 2),
                Component = this,
                Radius = 8,
                Shape = ComponentShape.Circle,
                DataType = GetOutputDataType() // Determine output data type
            };
            _outputConnections.Add(outputPoint);
        }

        /// <summary>
        /// Called when the node's execution status changes.
        /// </summary>
        /// <param name="oldStatus">The previous status.</param>
        /// <param name="newStatus">The new status.</param>
        protected virtual void OnStatusChanged(NodeStatus oldStatus, NodeStatus newStatus)
        {
            StatusChanged?.Invoke(this, new NodeExecutionEventArgs(Id, oldStatus, newStatus));
        }

        /// <summary>
        /// Called when the node's enabled state changes.
        /// </summary>
        protected virtual void OnEnabledStateChanged()
        {
        }

        /// <summary>
        /// Called when an error occurs during node execution.
        /// </summary>
        /// <param name="error">The error that occurred.</param>
        /// <param name="context">Additional context about the error.</param>
        protected virtual void OnError(Exception error, string context = null)
        {
            Status = NodeStatus.Failed;
            ErrorOccurred?.Invoke(this, new NodeErrorEventArgs(Id, error, context));
        }
        #endregion

        #region NodeProperties helper
        /// <summary>
        /// Ensures a NodeProperties entry exists with the correct type and updates its current value.
        /// Use this from derived nodes in constructors and setters when geometry/config changes.
        /// </summary>
        protected void UpsertNodeProperty(string key, Type type, object value, string description = null)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            if (!NodeProperties.TryGetValue(key, out var p) || p == null)
            {
                NodeProperties[key] = new Beep.Skia.Model.ParameterInfo
                {
                    ParameterName = key,
                    ParameterType = type,
                    DefaultParameterValue = value,
                    ParameterCurrentValue = value,
                    Description = description
                };
            }
            else
            {
                if (p.ParameterType == null) p.ParameterType = type;
                p.ParameterCurrentValue = value;
                if (p.DefaultParameterValue == null) p.DefaultParameterValue = value;
                if (string.IsNullOrEmpty(p.Description) && !string.IsNullOrEmpty(description)) p.Description = description;
            }
        }
        #endregion

        #region NodeProperties mapping
        /// <summary>
        /// Applies values from NodeProperties back to this node's public settable properties (by name) and updates Configuration.
        /// Name matching is case-insensitive.
        /// </summary>
        public virtual void ApplyNodeProperties()
        {
            if (NodeProperties == null || NodeProperties.Count == 0) return;

            var props = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var kv in NodeProperties)
            {
                var key = kv.Key;
                var pinfo = kv.Value;
                var value = pinfo?.ParameterCurrentValue ?? pinfo?.DefaultParameterValue;
                if (key == null) continue;

                // Update Configuration bag with raw value
                try { Configuration[key] = value; } catch { }

                // Find matching property by name (case-insensitive)
                PropertyInfo targetProp = null;
                foreach (var pi in props)
                {
                    if (string.Equals(pi.Name, key, StringComparison.OrdinalIgnoreCase)) { targetProp = pi; break; }
                }
                if (targetProp == null || !targetProp.CanWrite) continue;

                try
                {
                    var targetType = targetProp.PropertyType;
                    object converted = ConvertToType(value, targetType);
                    if (converted != null || value == null) // allow null assignment
                    {
                        targetProp.SetValue(this, converted);
                    }
                }
                catch { /* ignore assignment failures */ }
            }
        }

        private static object ConvertToType(object value, Type targetType)
        {
            if (targetType == null) return value;
            if (value == null) return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

            var valType = value.GetType();
            if (targetType.IsAssignableFrom(valType)) return value;

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
                if (targetType == typeof(SKColor) || targetType == typeof(SKColor?))
                {
                    if (value is SKColor c) return c;
                    if (value is string s && TryParseColor(s, out var col)) return col;
                }
                // fallback
                return System.Convert.ChangeType(value, targetType);
            }
            catch { return null; }
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

        #region Drawing Methods
        /// <summary>
        /// Draws the automation node with Material Design styling.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            var bounds = new SKRect(X, Y, X + Width, Y + Height);
            
            // Draw node background with status-dependent styling
            DrawNodeBackground(canvas, bounds);
            
            // Draw node content (text, icon, etc.)
            DrawNodeContent(canvas, bounds, context);
            
            // Draw connection points
            DrawConnectionPoints(canvas, context);
            
            // Draw status indicator
            DrawStatusIndicator(canvas, bounds);
        }

        /// <summary>
        /// Draws the node background with Material Design styling.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The node bounds.</param>
        protected virtual void DrawNodeBackground(SKCanvas canvas, SKRect bounds)
        {
            using var backgroundPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = GetBackgroundColor()
            };

            using var borderPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = GetBorderColor(),
                StrokeWidth = 2
            };

            // Draw rounded rectangle background
            var cornerRadius = 8f;
            canvas.DrawRoundRect(bounds, cornerRadius, cornerRadius, backgroundPaint);
            canvas.DrawRoundRect(bounds, cornerRadius, cornerRadius, borderPaint);
        }

        /// <summary>
        /// Draws the node content (text, icons, etc.).
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The node bounds.</param>
        /// <param name="context">The drawing context.</param>
        protected virtual void DrawNodeContent(SKCanvas canvas, SKRect bounds, DrawingContext context)
        {
            // Draw node name using SKFont
            using var font = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 14);
            using var textPaint = new SKPaint
            {
                IsAntialias = true,
                Color = GetTextColor()
            };

            var text = Name ?? "Automation Node";
            var textBounds = font.MeasureText(text);
            
            var textX = bounds.MidX - textBounds / 2;
            var textY = bounds.MidY + font.Size / 3; // Adjust for baseline
            
            canvas.DrawText(text, textX, textY, font, textPaint);
        }

        /// <summary>
        /// Draws the connection points for the node.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected virtual void DrawConnectionPoints(SKCanvas canvas, DrawingContext context)
        {
            // Draw input connection points
            for (int i = 0; i < InputConnections.Count; i++)
            {
                var point = InputConnections[i];
                if (point is ConnectionPoint cp)
                {
                    DrawConnectionPoint(canvas, cp, i, InputConnections.Count, true, context);
                }
            }

            // Draw output connection points
            for (int i = 0; i < OutputConnections.Count; i++)
            {
                var point = OutputConnections[i];
                if (point is ConnectionPoint cp)
                {
                    DrawConnectionPoint(canvas, cp, i, OutputConnections.Count, false, context);
                }
            }
        }

        /// <summary>
        /// Draws a single connection point with enhanced visualization.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="point">The connection point to draw.</param>
        /// <param name="index">The index of this point in its group.</param>
        /// <param name="totalCount">The total number of points in this group.</param>
        /// <param name="isInput">Whether this is an input point.</param>
        /// <param name="context">The drawing context.</param>
        protected virtual void DrawConnectionPoint(SKCanvas canvas, ConnectionPoint point, int index, int totalCount, bool isInput, DrawingContext context)
        {
            var absolutePos = new SKPoint(X + point.Position.X, Y + point.Position.Y);

            // Adjust position for multiple points
            if (totalCount > 1)
            {
                float spacing = Math.Min(30, Height / (totalCount + 1));
                float startY = Y + spacing;
                absolutePos.Y = startY + (index * spacing);
                point.Position = new SKPoint(point.Position.X, absolutePos.Y - Y);
            }

            // Determine colors based on type and state
            SKColor fillColor, borderColor;
            float radius = point.Radius;

            if (isInput)
            {
                fillColor = point.IsAvailable ? SKColor.Parse("#4CAF50") : SKColor.Parse("#81C784"); // Green
                borderColor = SKColor.Parse("#2E7D32");
            }
            else
            {
                fillColor = point.IsAvailable ? SKColor.Parse("#2196F3") : SKColor.Parse("#64B5F6"); // Blue
                borderColor = SKColor.Parse("#0D47A1");
            }

            // Highlight if connected
            if (!point.IsAvailable)
            {
                fillColor = SKColor.Parse("#FF9800"); // Orange for connected
                borderColor = SKColor.Parse("#E65100");
            }

            // Hover effect
            var mousePos = context.MousePosition;
            var distance = SKPoint.Distance(absolutePos, mousePos);
            if (distance <= radius + 2)
            {
                radius += 2;
                fillColor = SKColor.Parse("#FFC107"); // Yellow hover
                borderColor = SKColor.Parse("#FF8F00");
            }

            // Draw outer glow for available points
            if (point.IsAvailable)
            {
                using var glowPaint = new SKPaint
                {
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill,
                    Color = fillColor.WithAlpha(100)
                };
                canvas.DrawCircle(absolutePos, radius + 3, glowPaint);
            }

            // Draw border
            using var borderPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = borderColor,
                StrokeWidth = 2
            };
            canvas.DrawCircle(absolutePos, radius, borderPaint);

            // Draw fill
            using var fillPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = fillColor
            };
            canvas.DrawCircle(absolutePos, radius - 1, fillPaint);

            // Draw inner highlight
            using var highlightPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = SKColors.White.WithAlpha(150)
            };
            canvas.DrawCircle(absolutePos.X - 1, absolutePos.Y - 1, radius - 3, highlightPaint);
        }

        /// <summary>
        /// Draws the status indicator for the node.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The node bounds.</param>
        protected virtual void DrawStatusIndicator(SKCanvas canvas, SKRect bounds)
        {
            var indicatorColor = GetStatusColor();
            if (indicatorColor == SKColors.Transparent)
                return;

            using var statusPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = indicatorColor
            };

            // Draw small status circle in top-right corner
            var indicatorPos = new SKPoint(bounds.Right - 10, bounds.Top + 10);
            canvas.DrawCircle(indicatorPos, 4, statusPaint);
        }

        /// <summary>
        /// Public method to draw the node (used by WorkflowCanvas).
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        public void DrawNode(SKCanvas canvas, DrawingContext context)
        {
            DrawContent(canvas, context);
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Gets the background color based on the current node state.
        /// </summary>
        /// <returns>The background color to use.</returns>
        protected virtual SKColor GetBackgroundColor()
        {
            if (!IsEnabled)
                return SKColors.LightGray.WithAlpha(128);

            return Status switch
            {
                NodeStatus.Executing => SKColor.Parse("#E3F2FD"), // Light blue
                NodeStatus.Completed => SKColor.Parse("#E8F5E8"), // Light green
                NodeStatus.Failed => SKColor.Parse("#FFEBEE"), // Light red
                NodeStatus.Cancelled => SKColor.Parse("#FFF3E0"), // Light orange
                _ => SKColors.White
            };
        }

        /// <summary>
        /// Gets the border color based on the current node state.
        /// </summary>
        /// <returns>The border color to use.</returns>
        protected virtual SKColor GetBorderColor()
        {
            if (!IsEnabled)
                return SKColors.Gray;

            return Status switch
            {
                NodeStatus.Executing => SKColor.Parse("#2196F3"), // Blue
                NodeStatus.Completed => SKColor.Parse("#4CAF50"), // Green
                NodeStatus.Failed => SKColor.Parse("#F44336"), // Red
                NodeStatus.Cancelled => SKColor.Parse("#FF9800"), // Orange
                _ => SKColors.Gray
            };
        }

        /// <summary>
        /// Gets the text color based on the current node state.
        /// </summary>
        /// <returns>The text color to use.</returns>
        protected virtual SKColor GetTextColor()
        {
            return IsEnabled ? SKColors.Black : SKColors.Gray;
        }

        /// <summary>
        /// Gets the status indicator color.
        /// </summary>
        /// <returns>The status indicator color, or Transparent if no indicator should be shown.</returns>
        protected virtual SKColor GetStatusColor()
        {
            return Status switch
            {
                NodeStatus.Executing => SKColor.Parse("#2196F3"), // Blue
                NodeStatus.Completed => SKColor.Parse("#4CAF50"), // Green
                NodeStatus.Failed => SKColor.Parse("#F44336"), // Red
                NodeStatus.Cancelled => SKColor.Parse("#FF9800"), // Orange
                _ => SKColors.Transparent
            };
        }
        #endregion

        #region Mouse Interaction
        /// <summary>
        /// Handles mouse down events for the automation node.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        /// <returns>true if the event was handled.</returns>
        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            if (!IsEnabled)
                return false;

            // Check if clicking on a connection point
            foreach (var connectionPoint in InputConnections.Concat(OutputConnections))
            {
                if (connectionPoint is ConnectionPoint cp)
                {
                    var absolutePos = new SKPoint(X + cp.Position.X, Y + cp.Position.Y);
                    var distance = SKPoint.Distance(point, absolutePos);
                    if (distance <= 8) // Connection point hit radius
                    {
                        // Handle connection point interaction
                        OnConnectionPointClicked(cp, context);
                        return true;
                    }
                }
            }

            // Handle general node selection/interaction
            return base.OnMouseDown(point, context);
        }

        /// <summary>
        /// Called when a connection point is clicked.
        /// </summary>
        /// <param name="connectionPoint">The connection point that was clicked.</param>
        /// <param name="context">The interaction context.</param>
        protected virtual void OnConnectionPointClicked(ConnectionPoint connectionPoint, InteractionContext context)
        {
            // This would be handled by the workflow canvas to create connections
            // For now, just mark as handled
        }
        #endregion

        #region Cleanup
        /// <summary>
        /// Disposes managed resources.
        /// </summary>
        protected override void DisposeManagedResources()
        {
            // Clean up any automation-specific resources
            _inputConnections.Clear();
            _outputConnections.Clear();
            _configuration.Clear();
            
            base.DisposeManagedResources();
        }
        #endregion
    }

    /// <summary>
    /// Extension methods for LINQ operations on connection points.
    /// </summary>
    public static class ConnectionPointExtensions
    {
        /// <summary>
        /// Concatenates two collections of connection points.
        /// </summary>
        public static IEnumerable<IConnectionPoint> Concat(this IList<IConnectionPoint> first, IList<IConnectionPoint> second)
        {
            foreach (var item in first)
                yield return item;
            foreach (var item in second)
                yield return item;
        }
    }
}