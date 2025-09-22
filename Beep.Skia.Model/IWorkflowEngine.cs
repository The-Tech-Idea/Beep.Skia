using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Beep.Skia.Model
{
    /// <summary>
    /// Represents a workflow engine that can execute automation workflows with nodes and connections.
    /// The engine manages workflow lifecycle, execution scheduling, and state management.
    /// </summary>
    public interface IWorkflowEngine
    {
        /// <summary>
        /// Gets the unique identifier for this workflow engine instance.
        /// </summary>
        string EngineId { get; }

        /// <summary>
        /// Gets a value indicating whether the workflow engine is currently running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Gets the collection of currently loaded workflows.
        /// </summary>
        IReadOnlyCollection<WorkflowDefinition> LoadedWorkflows { get; }

        /// <summary>
        /// Gets the collection of currently active executions.
        /// </summary>
        IReadOnlyCollection<WorkflowExecution> ActiveExecutions { get; }

        /// <summary>
        /// Event raised when a workflow execution starts.
        /// </summary>
        event EventHandler<WorkflowExecutionEventArgs> WorkflowStarted;

        /// <summary>
        /// Event raised when a workflow execution completes.
        /// </summary>
        event EventHandler<WorkflowExecutionEventArgs> WorkflowCompleted;

        /// <summary>
        /// Event raised when a workflow execution fails.
        /// </summary>
        event EventHandler<WorkflowExecutionEventArgs> WorkflowFailed;

        /// <summary>
        /// Event raised when a workflow execution is paused.
        /// </summary>
        event EventHandler<WorkflowExecutionEventArgs> WorkflowPaused;

        /// <summary>
        /// Event raised when a workflow execution is resumed.
        /// </summary>
        event EventHandler<WorkflowExecutionEventArgs> WorkflowResumed;

        /// <summary>
        /// Event raised when a workflow execution is cancelled.
        /// </summary>
        event EventHandler<WorkflowExecutionEventArgs> WorkflowCancelled;

        /// <summary>
        /// Starts the workflow engine and begins processing workflows.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the startup operation.</param>
        /// <returns>A task representing the startup operation.</returns>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the workflow engine and cancels all active executions.
        /// </summary>
        /// <param name="graceful">Whether to allow current executions to complete gracefully.</param>
        /// <param name="cancellationToken">Token to cancel the shutdown operation.</param>
        /// <returns>A task representing the shutdown operation.</returns>
        Task StopAsync(bool graceful = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads a workflow definition into the engine.
        /// </summary>
        /// <param name="workflow">The workflow definition to load.</param>
        /// <param name="cancellationToken">Token to cancel the load operation.</param>
        /// <returns>A task containing the result of the load operation.</returns>
        Task<bool> LoadWorkflowAsync(WorkflowDefinition workflow, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unloads a workflow definition from the engine.
        /// </summary>
        /// <param name="workflowId">The ID of the workflow to unload.</param>
        /// <param name="cancellationToken">Token to cancel the unload operation.</param>
        /// <returns>A task containing the result of the unload operation.</returns>
        Task<bool> UnloadWorkflowAsync(string workflowId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a workflow with the specified input data.
        /// </summary>
        /// <param name="workflowId">The ID of the workflow to execute.</param>
        /// <param name="inputData">The input data for the workflow execution.</param>
        /// <param name="priority">The execution priority.</param>
        /// <param name="cancellationToken">Token to cancel the execution.</param>
        /// <returns>A task containing the workflow execution result.</returns>
        Task<WorkflowResult> ExecuteWorkflowAsync(
            string workflowId, 
            Dictionary<string, object> inputData = null, 
            ExecutionPriority priority = ExecutionPriority.Normal,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a workflow with a predefined execution context.
        /// </summary>
        /// <param name="workflowId">The ID of the workflow to execute.</param>
        /// <param name="context">The execution context for the workflow.</param>
        /// <param name="cancellationToken">Token to cancel the execution.</param>
        /// <returns>A task containing the workflow execution result.</returns>
        Task<WorkflowResult> ExecuteWorkflowAsync(
            string workflowId, 
            ExecutionContext context, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Pauses an active workflow execution.
        /// </summary>
        /// <param name="executionId">The ID of the execution to pause.</param>
        /// <param name="cancellationToken">Token to cancel the pause operation.</param>
        /// <returns>A task containing the result of the pause operation.</returns>
        Task<bool> PauseExecutionAsync(string executionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resumes a paused workflow execution.
        /// </summary>
        /// <param name="executionId">The ID of the execution to resume.</param>
        /// <param name="cancellationToken">Token to cancel the resume operation.</param>
        /// <returns>A task containing the result of the resume operation.</returns>
        Task<bool> ResumeExecutionAsync(string executionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels an active workflow execution.
        /// </summary>
        /// <param name="executionId">The ID of the execution to cancel.</param>
        /// <param name="cancellationToken">Token to cancel the cancellation operation.</param>
        /// <returns>A task containing the result of the cancellation operation.</returns>
        Task<bool> CancelExecutionAsync(string executionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current status of a workflow execution.
        /// </summary>
        /// <param name="executionId">The ID of the execution to check.</param>
        /// <returns>The current execution status, or null if the execution is not found.</returns>
        WorkflowExecution GetExecutionStatus(string executionId);

        /// <summary>
        /// Gets the execution history for a specific workflow.
        /// </summary>
        /// <param name="workflowId">The ID of the workflow.</param>
        /// <param name="limit">The maximum number of executions to return.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task containing the execution history.</returns>
        Task<IEnumerable<WorkflowExecution>> GetExecutionHistoryAsync(
            string workflowId, 
            int limit = 100, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a workflow definition for correctness and completeness.
        /// </summary>
        /// <param name="workflow">The workflow definition to validate.</param>
        /// <param name="cancellationToken">Token to cancel the validation.</param>
        /// <returns>A task containing the validation result.</returns>
        Task<ValidationResult> ValidateWorkflowAsync(WorkflowDefinition workflow, CancellationToken cancellationToken = default);

        /// <summary>
        /// Registers an automation node type with the engine.
        /// </summary>
        /// <param name="nodeType">The type name for the node.</param>
        /// <param name="factory">A factory function to create instances of the node.</param>
        void RegisterNodeType(string nodeType, Func<IAutomationNode> factory);

        /// <summary>
        /// Unregisters an automation node type from the engine.
        /// </summary>
        /// <param name="nodeType">The type name to unregister.</param>
        /// <returns>True if the node type was successfully unregistered, false otherwise.</returns>
        bool UnregisterNodeType(string nodeType);

        /// <summary>
        /// Gets the collection of registered node types.
        /// </summary>
        /// <returns>The collection of registered node type names.</returns>
        IEnumerable<string> GetRegisteredNodeTypes();
    }

    /// <summary>
    /// Represents a trigger that can initiate workflow execution based on events or conditions.
    /// Triggers monitor external systems and activate workflows when specific conditions are met.
    /// </summary>
    public interface ITrigger
    {
        /// <summary>
        /// Gets the unique identifier for this trigger.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the display name of this trigger.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the description of what this trigger monitors.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the type of this trigger.
        /// </summary>
        TriggerType TriggerType { get; }

        /// <summary>
        /// Gets a value indicating whether this trigger is currently active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Gets a value indicating whether this trigger is currently enabled.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets or sets the configuration parameters for this trigger.
        /// </summary>
        Dictionary<string, object> Configuration { get; set; }

        /// <summary>
        /// Gets the ID of the workflow that this trigger should execute.
        /// </summary>
        string WorkflowId { get; }

        /// <summary>
        /// Gets the last time this trigger was activated.
        /// </summary>
        DateTime? LastActivated { get; }

        /// <summary>
        /// Gets the number of times this trigger has been activated.
        /// </summary>
        long ActivationCount { get; }

        /// <summary>
        /// Event raised when the trigger condition is met and a workflow should be executed.
        /// </summary>
        event EventHandler<TriggerEventArgs> Triggered;

        /// <summary>
        /// Event raised when the trigger encounters an error.
        /// </summary>
        event EventHandler<TriggerErrorEventArgs> ErrorOccurred;

        /// <summary>
        /// Initializes the trigger with the specified configuration.
        /// </summary>
        /// <param name="configuration">The configuration parameters for the trigger.</param>
        /// <param name="cancellationToken">Token to cancel the initialization.</param>
        /// <returns>A task representing the initialization operation.</returns>
        Task<bool> InitializeAsync(Dictionary<string, object> configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts monitoring for trigger conditions.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the start operation.</param>
        /// <returns>A task representing the start operation.</returns>
        Task<bool> StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops monitoring for trigger conditions.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the stop operation.</param>
        /// <returns>A task representing the stop operation.</returns>
        Task<bool> StopAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates the current trigger configuration.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the validation.</param>
        /// <returns>A task containing the validation result.</returns>
        Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Tests the trigger to verify it can detect the configured conditions.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the test operation.</param>
        /// <returns>A task containing the test result.</returns>
        Task<bool> TestAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the schema defining the configuration parameters this trigger supports.
        /// </summary>
        /// <returns>A dictionary describing the configuration schema.</returns>
        Dictionary<string, object> GetConfigurationSchema();

        /// <summary>
        /// Manually activates the trigger for testing purposes.
        /// </summary>
        /// <param name="testData">Optional test data to include with the activation.</param>
        /// <param name="cancellationToken">Token to cancel the activation.</param>
        /// <returns>A task representing the manual activation.</returns>
        Task ActivateAsync(Dictionary<string, object> testData = null, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Provides data for trigger error events.
    /// </summary>
    public class TriggerErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the TriggerErrorEventArgs class.
        /// </summary>
        /// <param name="triggerId">The ID of the trigger where the error occurred.</param>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="errorMessage">A descriptive error message.</param>
        public TriggerErrorEventArgs(string triggerId, Exception exception, string errorMessage = null)
        {
            TriggerId = triggerId;
            Exception = exception;
            ErrorMessage = errorMessage ?? exception?.Message ?? "Unknown trigger error occurred";
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the ID of the trigger where the error occurred.
        /// </summary>
        public string TriggerId { get; }

        /// <summary>
        /// Gets the exception that caused the error.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets the descriptive error message.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Gets the timestamp when the error occurred.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the error was handled.
        /// </summary>
        public bool Handled { get; set; }
    }

    /// <summary>
    /// Represents an active workflow execution with its current state and progress.
    /// </summary>
    public class WorkflowExecution
    {
        /// <summary>
        /// Initializes a new instance of the WorkflowExecution class.
        /// </summary>
        /// <param name="executionId">The unique execution ID.</param>
        /// <param name="workflowId">The ID of the workflow being executed.</param>
        /// <param name="context">The execution context.</param>
        public WorkflowExecution(string executionId, string workflowId, ExecutionContext context)
        {
            ExecutionId = executionId ?? throw new ArgumentNullException(nameof(executionId));
            WorkflowId = workflowId ?? throw new ArgumentNullException(nameof(workflowId));
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Status = WorkflowStatus.Ready;
            CreatedTime = DateTime.UtcNow;
            NodeExecutions = new List<NodeExecution>();
        }

        /// <summary>
        /// Gets the unique execution ID.
        /// </summary>
        public string ExecutionId { get; }

        /// <summary>
        /// Gets the ID of the workflow being executed.
        /// </summary>
        public string WorkflowId { get; }

        /// <summary>
        /// Gets the execution context.
        /// </summary>
        public ExecutionContext Context { get; }

        /// <summary>
        /// Gets or sets the current status of the execution.
        /// </summary>
        public WorkflowStatus Status { get; set; }

        /// <summary>
        /// Gets the time when the execution was created.
        /// </summary>
        public DateTime CreatedTime { get; }

        /// <summary>
        /// Gets or sets the time when the execution started.
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Gets or sets the time when the execution completed.
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Gets the total execution time.
        /// </summary>
        public TimeSpan? ExecutionTime => StartTime.HasValue && EndTime.HasValue ? EndTime.Value - StartTime.Value : null;

        /// <summary>
        /// Gets or sets the current node being executed.
        /// </summary>
        public string CurrentNodeId { get; set; }

        /// <summary>
        /// Gets the collection of node executions within this workflow execution.
        /// </summary>
        public IList<NodeExecution> NodeExecutions { get; }

        /// <summary>
        /// Gets or sets the final result of the workflow execution.
        /// </summary>
        public WorkflowResult Result { get; set; }

        /// <summary>
        /// Gets or sets error information if the execution failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets additional metadata about the execution.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents the execution of a single node within a workflow.
    /// </summary>
    public class NodeExecution
    {
        /// <summary>
        /// Initializes a new instance of the NodeExecution class.
        /// </summary>
        /// <param name="nodeId">The ID of the node being executed.</param>
        /// <param name="executionId">The workflow execution ID.</param>
        public NodeExecution(string nodeId, string executionId)
        {
            NodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
            ExecutionId = executionId ?? throw new ArgumentNullException(nameof(executionId));
            Status = NodeStatus.Idle;
            CreatedTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the ID of the node being executed.
        /// </summary>
        public string NodeId { get; }

        /// <summary>
        /// Gets the workflow execution ID.
        /// </summary>
        public string ExecutionId { get; }

        /// <summary>
        /// Gets or sets the current status of the node execution.
        /// </summary>
        public NodeStatus Status { get; set; }

        /// <summary>
        /// Gets the time when the node execution was created.
        /// </summary>
        public DateTime CreatedTime { get; }

        /// <summary>
        /// Gets or sets the time when the node execution started.
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Gets or sets the time when the node execution completed.
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Gets the execution time for this node.
        /// </summary>
        public TimeSpan? ExecutionTime => StartTime.HasValue && EndTime.HasValue ? EndTime.Value - StartTime.Value : null;

        /// <summary>
        /// Gets or sets the result of the node execution.
        /// </summary>
        public NodeResult Result { get; set; }

        /// <summary>
        /// Gets or sets error information if the node execution failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets additional metadata about the node execution.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}