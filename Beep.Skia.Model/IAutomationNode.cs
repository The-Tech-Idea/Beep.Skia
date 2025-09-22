using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Beep.Skia.Model
{
    /// <summary>
    /// Represents a node in a workflow automation system that can execute actions, validate inputs, and handle state.
    /// This interface extends the visual component system to support executable workflow logic.
    /// </summary>
    public interface IAutomationNode
    {
        /// <summary>
        /// Gets the unique identifier for this automation node.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the display name of this automation node.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the description of what this automation node does.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the type of this automation node (Action, Trigger, Condition).
        /// </summary>
        NodeType NodeType { get; }

        /// <summary>
        /// Gets the current status of this automation node.
        /// </summary>
        NodeStatus Status { get; }

        /// <summary>
        /// Gets or sets the configuration parameters for this node.
        /// </summary>
        Dictionary<string, object> Configuration { get; set; }

        /// <summary>
        /// Gets the input connection points for this automation node.
        /// </summary>
        IList<IConnectionPoint> InputConnections { get; }

        /// <summary>
        /// Gets the output connection points for this automation node.
        /// </summary>
        IList<IConnectionPoint> OutputConnections { get; }

        /// <summary>
        /// Gets a value indicating whether this node is currently enabled for execution.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Event raised when the node's execution status changes.
        /// </summary>
        event EventHandler<NodeExecutionEventArgs> StatusChanged;

        /// <summary>
        /// Event raised when the node encounters an error during execution.
        /// </summary>
        event EventHandler<NodeErrorEventArgs> ErrorOccurred;

        /// <summary>
        /// Initializes the automation node with the specified configuration.
        /// </summary>
        /// <param name="configuration">The configuration parameters for the node.</param>
        /// <param name="cancellationToken">Token to cancel the initialization.</param>
        /// <returns>A task representing the initialization operation.</returns>
        Task<bool> InitializeAsync(Dictionary<string, object> configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates the current configuration and input data for this node.
        /// </summary>
        /// <param name="inputData">The input data to validate.</param>
        /// <param name="cancellationToken">Token to cancel the validation.</param>
        /// <returns>A task containing the validation result.</returns>
        Task<ValidationResult> ValidateAsync(ExecutionContext inputData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the automation node's logic with the provided execution context.
        /// </summary>
        /// <param name="context">The execution context containing input data and workflow state.</param>
        /// <param name="cancellationToken">Token to cancel the execution.</param>
        /// <returns>A task containing the execution result.</returns>
        Task<NodeResult> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resets the node to its initial state, clearing any cached data or temporary state.
        /// </summary>
        Task ResetAsync();

        /// <summary>
        /// Gets the schema defining the input parameters this node expects.
        /// </summary>
        /// <returns>A dictionary describing the input schema.</returns>
        Dictionary<string, object> GetInputSchema();

        /// <summary>
        /// Gets the schema defining the output parameters this node produces.
        /// </summary>
        /// <returns>A dictionary describing the output schema.</returns>
        Dictionary<string, object> GetOutputSchema();
    }

    /// <summary>
    /// Represents the execution status of an automation node.
    /// </summary>
    public enum NodeStatus
    {
        /// <summary>
        /// The node is idle and ready for execution.
        /// </summary>
        Idle,

        /// <summary>
        /// The node is currently being initialized.
        /// </summary>
        Initializing,

        /// <summary>
        /// The node is currently executing.
        /// </summary>
        Executing,

        /// <summary>
        /// The node has completed execution successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// The node execution failed with an error.
        /// </summary>
        Failed,

        /// <summary>
        /// The node execution was cancelled.
        /// </summary>
        Cancelled,

        /// <summary>
        /// The node is disabled and cannot execute.
        /// </summary>
        Disabled
    }

    /// <summary>
    /// Represents the type of automation node in a workflow.
    /// </summary>
    public enum NodeType
    {
        /// <summary>
        /// A trigger node that starts workflow execution based on events or conditions.
        /// </summary>
        Trigger,

        /// <summary>
        /// An action node that performs operations or transformations on data.
        /// </summary>
        Action,

        /// <summary>
        /// A condition node that evaluates logic and controls workflow branching.
        /// </summary>
        Condition,

        /// <summary>
        /// A data source node that retrieves or provides data to the workflow.
        /// </summary>
        DataSource,

        /// <summary>
        /// A data transformation node that modifies or processes data.
        /// </summary>
        Transform,

        /// <summary>
        /// An output node that sends data to external systems or displays results.
        /// </summary>
        Output,
        Conditional
    }
}