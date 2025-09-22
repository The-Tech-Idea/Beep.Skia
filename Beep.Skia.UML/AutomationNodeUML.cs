using SkiaSharp;
using Beep.Skia;
using System.Collections.Generic;
using Beep.Skia.Model;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace Beep.Skia.UML
{
    /// <summary>
    /// Represents an automation node in UML diagrams using stereotypes.
    /// Displays automation node properties and connection points for workflow visualization.
    /// Implements IAutomationNode to integrate with workflow execution system.
    /// </summary>
    public class AutomationNodeUML : UMLControl, IAutomationNode
    {
        #region IAutomationNode Implementation

        /// <summary>
        /// Gets the unique identifier for this automation node.
        /// </summary>
        public new string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets the description of what this automation node does.
        /// </summary>
        public string Description => NodeDescription;

        /// <summary>
        /// Gets the type of this automation node.
        /// </summary>
        public NodeType NodeType => AutomationNodeType;

        /// <summary>
        /// Gets the current status of this automation node.
        /// </summary>
        public NodeStatus Status { get; private set; } = NodeStatus.Idle;

        /// <summary>
        /// Gets or sets the configuration parameters for this node.
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the input connection points for this automation node.
        /// </summary>
        IList<IConnectionPoint> IAutomationNode.InputConnections => InConnectionPoints;

        /// <summary>
        /// Gets the output connection points for this automation node.
        /// </summary>
        IList<IConnectionPoint> IAutomationNode.OutputConnections => OutConnectionPoints;

        /// <summary>
        /// Gets a value indicating whether this node is currently enabled for execution.
        /// </summary>
        public new bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Event raised when the node's execution status changes.
        /// </summary>
        public event EventHandler<NodeExecutionEventArgs> StatusChanged;

        /// <summary>
        /// Event raised when the node encounters an error during execution.
        /// </summary>
        public event EventHandler<NodeErrorEventArgs> ErrorOccurred;

        /// <summary>
        /// Initializes the automation node with the specified configuration.
        /// </summary>
        /// <param name="configuration">The configuration parameters for the node.</param>
        /// <param name="cancellationToken">Token to cancel the initialization.</param>
        /// <returns>A task representing the initialization operation.</returns>
        public virtual Task<bool> InitializeAsync(Dictionary<string, object> configuration, CancellationToken cancellationToken = default)
        {
            try
            {
                var previousStatus = Status;
                Status = NodeStatus.Initializing;
                StatusChanged?.Invoke(this, new NodeExecutionEventArgs(Id, previousStatus, Status));

                Configuration = configuration ?? new Dictionary<string, object>();
                
                // Update visual properties from configuration
                if (Configuration.ContainsKey("inputTypes") && Configuration["inputTypes"] is List<string> inputTypes)
                    InputDataTypes = inputTypes;
                
                if (Configuration.ContainsKey("outputTypes") && Configuration["outputTypes"] is List<string> outputTypes)
                    OutputDataTypes = outputTypes;

                previousStatus = Status;
                Status = NodeStatus.Idle;
                StatusChanged?.Invoke(this, new NodeExecutionEventArgs(Id, previousStatus, Status));
                
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                var previousStatus = Status;
                Status = NodeStatus.Failed;
                ErrorOccurred?.Invoke(this, new NodeErrorEventArgs(Id, ex, ex.Message));
                StatusChanged?.Invoke(this, new NodeExecutionEventArgs(Id, previousStatus, Status));
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Validates the current configuration and input data for this node.
        /// </summary>
        /// <param name="context">The execution context to validate.</param>
        /// <param name="cancellationToken">Token to cancel the validation.</param>
        /// <returns>A task containing the validation result.</returns>
        public virtual Task<ValidationResult> ValidateAsync(Beep.Skia.Model.ExecutionContext context, CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();
            
            // Validate that required input types are present
            foreach (var inputType in InputDataTypes)
            {
                if (!context.Data.ContainsKey(inputType))
                {
                    errors.Add($"Required input '{inputType}' is missing");
                }
            }
            
            var result = errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors.ToArray());
            return Task.FromResult(result);
        }

        /// <summary>
        /// Executes the automation node's logic with the provided execution context.
        /// </summary>
        /// <param name="context">The execution context containing input data and workflow state.</param>
        /// <param name="cancellationToken">Token to cancel the execution.</param>
        /// <returns>A task containing the execution result.</returns>
        public virtual async Task<NodeResult> ExecuteAsync(Beep.Skia.Model.ExecutionContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var previousStatus = Status;
                Status = NodeStatus.Executing;
                IsExecuting = true;
                StatusChanged?.Invoke(this, new NodeExecutionEventArgs(Id, previousStatus, Status));

                // Placeholder execution - override in derived classes for actual logic
                await Task.Delay(100, cancellationToken);

                var outputData = new Dictionary<string, object>();
                var result = new NodeResult(true, outputData, $"Node {Name} executed successfully");

                previousStatus = Status;
                Status = NodeStatus.Completed;
                IsExecuting = false;
                StatusChanged?.Invoke(this, new NodeExecutionEventArgs(Id, previousStatus, Status));

                return result;
            }
            catch (Exception ex)
            {
                var previousStatus = Status;
                Status = NodeStatus.Failed;
                IsExecuting = false;
                ErrorOccurred?.Invoke(this, new NodeErrorEventArgs(Id, ex, ex.Message));
                StatusChanged?.Invoke(this, new NodeExecutionEventArgs(Id, previousStatus, Status));
                
                return new NodeResult(false, new Dictionary<string, object>(), ex.Message);
            }
        }

        /// <summary>
        /// Resets the node to its initial state, clearing any cached data or temporary state.
        /// </summary>
        public virtual Task ResetAsync()
        {
            var previousStatus = Status;
            Status = NodeStatus.Idle;
            IsExecuting = false;
            StatusChanged?.Invoke(this, new NodeExecutionEventArgs(Id, previousStatus, Status));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the schema defining the input parameters this node expects.
        /// </summary>
        /// <returns>A dictionary describing the input schema.</returns>
        public virtual Dictionary<string, object> GetInputSchema()
        {
            return InputDataTypes.ToDictionary(
                type => type,
                type => new { Type = type, Required = true }
            ).ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
        }

        /// <summary>
        /// Gets the schema defining the output parameters this node produces.
        /// </summary>
        /// <returns>A dictionary describing the output schema.</returns>
        public virtual Dictionary<string, object> GetOutputSchema()
        {
            return OutputDataTypes.ToDictionary(
                type => type,
                type => new { Type = type, Required = false }
            ).ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
        }

        #endregion

        #endregion

        #region Visual Properties
        /// <summary>
        /// Gets or sets the automation node type.
        /// </summary>
        public NodeType AutomationNodeType { get; set; } = NodeType.Action;

        /// <summary>
        /// Gets or sets the node description.
        /// </summary>
        public string NodeDescription { get; set; } = "";

        /// <summary>
        /// Gets or sets the input data types for this node.
        /// </summary>
        public List<string> InputDataTypes { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the output data types for this node.
        /// </summary>
        public List<string> OutputDataTypes { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets whether this node is currently executing.
        /// </summary>
        public bool IsExecuting { get; set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomationNodeUML"/> class.
        /// </summary>
        public AutomationNodeUML()
        {
            Width = 140;
            Height = 80;
            Name = "AutomationNode";

            // Set stereotype based on automation node type
            UpdateStereotype();
        }

        /// <summary>
        /// Updates the UML stereotype based on the automation node type.
        /// </summary>
        private void UpdateStereotype()
        {
            Stereotype = AutomationNodeType switch
            {
                NodeType.Trigger => "<<trigger>>",
                NodeType.Action => "<<action>>",
                NodeType.Condition => "<<condition>>",
                NodeType.DataSource => "<<dataSource>>",
                NodeType.Transform => "<<transform>>",
                NodeType.Output => "<<output>>",
                _ => "<<automation>>"
            };
        }

        /// <summary>
        /// Draws the automation node with status indicators and connection points.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Update stereotype if node type changed
            UpdateStereotype();

            // Draw background with status-based coloring
            DrawNodeBackground(canvas, context);

            // Draw stereotype
            using var font = new SKFont(SKTypeface.Default, 10);
            DrawStereotype(canvas, context, font);

            // Draw node name and description
            DrawNodeInfo(canvas, context, font);

            // Draw connection points
            DrawConnectionPoints(canvas, context);

            // Draw status indicator
            DrawStatusIndicator(canvas, context);

            // Draw selection indicator
            DrawSelection(canvas, context);
        }

        /// <summary>
        /// Draws the node background with status-based coloring.
        /// </summary>
        private void DrawNodeBackground(SKCanvas canvas, DrawingContext context)
        {
            // Choose color based on node type and status
            SKColor backgroundColor = GetNodeBackgroundColor();

            using var fillPaint = new SKPaint
            {
                Color = backgroundColor,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            var backgroundRect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(backgroundRect, 8, 8, fillPaint);

            using var borderPaint = new SKPaint
            {
                Color = GetNodeBorderColor(),
                StrokeWidth = IsSelected ? 3.0f : 1.5f,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            canvas.DrawRoundRect(backgroundRect, 8, 8, borderPaint);
        }

        /// <summary>
        /// Gets the background color based on node type and status.
        /// </summary>
        private SKColor GetNodeBackgroundColor()
        {
            if (IsExecuting)
                return new SKColor(255, 255, 200); // Light yellow for executing

            return AutomationNodeType switch
            {
                NodeType.Trigger => new SKColor(200, 255, 200),     // Light green
                NodeType.Action => new SKColor(200, 200, 255),      // Light blue
                NodeType.Condition => new SKColor(255, 200, 200),   // Light red
                NodeType.DataSource => new SKColor(255, 255, 200),  // Light yellow
                NodeType.Transform => new SKColor(200, 255, 255),   // Light cyan
                NodeType.Output => new SKColor(255, 200, 255),      // Light magenta
                _ => SKColors.White
            };
        }

        /// <summary>
        /// Gets the border color based on node type.
        /// </summary>
        private SKColor GetNodeBorderColor()
        {
            return AutomationNodeType switch
            {
                NodeType.Trigger => SKColors.DarkGreen,
                NodeType.Action => SKColors.DarkBlue,
                NodeType.Condition => SKColors.DarkRed,
                NodeType.DataSource => SKColors.DarkOrange,
                NodeType.Transform => SKColors.DarkCyan,
                NodeType.Output => SKColors.DarkMagenta,
                _ => SKColors.Black
            };
        }

        /// <summary>
        /// Draws the node name and description.
        /// </summary>
        private void DrawNodeInfo(SKCanvas canvas, DrawingContext context, SKFont font)
        {
            using var textPaint = new SKPaint
            {
                Color = TextColor,
                IsAntialias = true
            };

            var centerX = X + Width / 2;
            var currentY = Y + 25; // Start below stereotype

            // Draw node name
            var nodeName = Name ?? "AutomationNode";
            canvas.DrawText(nodeName, centerX, currentY, SKTextAlign.Center, font, textPaint);

            // Draw description if present
            if (!string.IsNullOrEmpty(NodeDescription))
            {
                currentY += 15;
                using var smallFont = new SKFont(SKTypeface.Default, 8);
                canvas.DrawText(NodeDescription, centerX, currentY, SKTextAlign.Center, smallFont, textPaint);
            }
        }

        /// <summary>
        /// Draws connection points with data type indicators.
        /// </summary>
        private void DrawConnectionPoints(SKCanvas canvas, DrawingContext context)
        {
            var points = InConnectionPoints;
            foreach (var point in points)
            {
                DrawConnectionPoint(canvas, point, true);
            }

            points = OutConnectionPoints;
            foreach (var point in points)
            {
                DrawConnectionPoint(canvas, point, false);
            }
        }

        /// <summary>
        /// Draws a single connection point with data type color coding.
        /// </summary>
        private void DrawConnectionPoint(SKCanvas canvas, IConnectionPoint point, bool isInput)
        {
            const float pointRadius = 6;

            using var pointPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            // Color code based on data type (simplified)
            pointPaint.Color = isInput ? SKColors.LightGreen : SKColors.LightBlue;

            canvas.DrawCircle(point.Position, pointRadius, pointPaint);

            using var borderPaint = new SKPaint
            {
                Color = SKColors.Black,
                StrokeWidth = 1,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            canvas.DrawCircle(point.Position, pointRadius, borderPaint);
        }

        /// <summary>
        /// Draws a status indicator for the node.
        /// </summary>
        private void DrawStatusIndicator(SKCanvas canvas, DrawingContext context)
        {
            if (!IsExecuting) return;

            using var statusPaint = new SKPaint
            {
                Color = SKColors.Orange,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            // Draw small indicator in top-right corner
            var indicatorRect = new SKRect(X + Width - 15, Y + 5, X + Width - 5, Y + 15);
            canvas.DrawOval(indicatorRect, statusPaint);
        }

        /// <summary>
        /// Gets the input connection points for automation workflows.
        /// </summary>
        public override List<IConnectionPoint> InConnectionPoints
        {
            get
            {
                var points = new List<IConnectionPoint>();

                // For trigger nodes, no input points
                if (AutomationNodeType == NodeType.Trigger)
                    return points;

                // Add input points based on InputDataTypes
                var inputCount = Math.Max(1, InputDataTypes.Count);
                for (int i = 0; i < inputCount; i++)
                {
                    var yPos = Y + Height * (0.2f + (i * 0.6f) / Math.Max(1, inputCount - 1));
                    var point = new ConnectionPoint
                    {
                        Position = new SKPoint(X, yPos),
                        Component = this,
                        Type = ConnectionPointType.In,
                        DataType = i < InputDataTypes.Count ? InputDataTypes[i] : "any"
                    };
                    points.Add(point);
                }

                return points;
            }
        }

        /// <summary>
        /// Gets the output connection points for automation workflows.
        /// </summary>
        public override List<IConnectionPoint> OutConnectionPoints
        {
            get
            {
                var points = new List<IConnectionPoint>();

                // Add output points based on OutputDataTypes
                var outputCount = Math.Max(1, OutputDataTypes.Count);
                for (int i = 0; i < outputCount; i++)
                {
                    var yPos = Y + Height * (0.2f + (i * 0.6f) / Math.Max(1, outputCount - 1));
                    var point = new ConnectionPoint
                    {
                        Position = new SKPoint(X + Width, yPos),
                        Component = this,
                        Type = ConnectionPointType.Out,
                        DataType = i < OutputDataTypes.Count ? OutputDataTypes[i] : "any"
                    };
                    points.Add(point);
                }

                return points;
            }
        }
    }
}