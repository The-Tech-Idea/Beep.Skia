using System;
using System.Collections.Generic;
using System.Linq;

namespace Beep.Skia.Model
{
    /// <summary>
    /// Provides execution context for automation nodes including input data, workflow state, and execution environment.
    /// </summary>
    public class ExecutionContext
    {
        /// <summary>
        /// Initializes a new instance of the ExecutionContext class.
        /// </summary>
        /// <param name="workflowId">The ID of the workflow being executed.</param>
        /// <param name="executionId">The unique ID for this execution instance.</param>
        public ExecutionContext(string workflowId, string executionId)
        {
            WorkflowId = workflowId ?? throw new ArgumentNullException(nameof(workflowId));
            ExecutionId = executionId ?? throw new ArgumentNullException(nameof(executionId));
            StartTime = DateTime.UtcNow;
            Data = new Dictionary<string, object>();
            Variables = new Dictionary<string, object>();
            Metadata = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets the ID of the workflow being executed.
        /// </summary>
        public string WorkflowId { get; }

        /// <summary>
        /// Gets the unique ID for this execution instance.
        /// </summary>
        public string ExecutionId { get; }

        /// <summary>
        /// Gets the time when this execution context was created.
        /// </summary>
        public DateTime StartTime { get; }

        /// <summary>
        /// Gets or sets the input data for the current node execution.
        /// </summary>
        public Dictionary<string, object> Data { get; set; }

        /// <summary>
        /// Gets or sets the workflow variables accessible to all nodes.
        /// </summary>
        public Dictionary<string, object> Variables { get; set; }

        /// <summary>
        /// Gets or sets metadata about the execution environment.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }

        /// <summary>
        /// Gets or sets the results from previous nodes in the workflow execution.
        /// </summary>
        public List<object> PreviousResults { get; set; } = new List<object>();

        /// <summary>
        /// Gets or sets the user ID who initiated the workflow execution.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the execution priority.
        /// </summary>
        public ExecutionPriority Priority { get; set; } = ExecutionPriority.Normal;

        /// <summary>
        /// Gets or sets a value indicating whether the execution is in debug mode.
        /// </summary>
        public bool IsDebugMode { get; set; }

        /// <summary>
        /// Gets or sets the maximum execution timeout.
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Creates a copy of the current execution context with optionally modified data.
        /// </summary>
        /// <param name="newData">New data to replace the current data, or null to keep existing data.</param>
        /// <returns>A new ExecutionContext instance with the specified modifications.</returns>
        public ExecutionContext CreateChild(Dictionary<string, object> newData = null)
        {
            var child = new ExecutionContext(WorkflowId, ExecutionId)
            {
                Data = newData ?? new Dictionary<string, object>(Data),
                Variables = new Dictionary<string, object>(Variables),
                Metadata = new Dictionary<string, object>(Metadata),
                PreviousResults = new List<object>(PreviousResults),
                UserId = UserId,
                Priority = Priority,
                IsDebugMode = IsDebugMode,
                Timeout = Timeout
            };

            return child;
        }

        /// <summary>
        /// Gets a strongly typed value from the data dictionary.
        /// </summary>
        /// <typeparam name="T">The type to cast the value to.</typeparam>
        /// <param name="key">The key to look up.</param>
        /// <param name="defaultValue">The default value to return if the key is not found.</param>
        /// <returns>The value cast to the specified type, or the default value.</returns>
        public T GetData<T>(string key, T defaultValue = default)
        {
            if (Data.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return defaultValue;
        }

        /// <summary>
        /// Sets a value in the data dictionary.
        /// </summary>
        /// <param name="key">The key to set.</param>
        /// <param name="value">The value to set.</param>
        public void SetData(string key, object value)
        {
            Data[key] = value;
        }

        /// <summary>
        /// Gets a strongly typed variable value.
        /// </summary>
        /// <typeparam name="T">The type to cast the value to.</typeparam>
        /// <param name="key">The key to look up.</param>
        /// <param name="defaultValue">The default value to return if the key is not found.</param>
        /// <returns>The variable value cast to the specified type, or the default value.</returns>
        public T GetVariable<T>(string key, T defaultValue = default)
        {
            if (Variables.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return defaultValue;
        }

        /// <summary>
        /// Sets a workflow variable value.
        /// </summary>
        /// <param name="key">The key to set.</param>
        /// <param name="value">The value to set.</param>
        public void SetVariable(string key, object value)
        {
            Variables[key] = value;
        }
    }

    /// <summary>
    /// Represents the result of validating an automation node's configuration and input data.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Initializes a new instance of the ValidationResult class.
        /// </summary>
        /// <param name="isValid">Whether the validation passed.</param>
        /// <param name="errors">Collection of validation errors, if any.</param>
        public ValidationResult(bool isValid, IEnumerable<string> errors = null)
        {
            IsValid = isValid;
            Errors = errors?.ToList() ?? new List<string>();
        }

        /// <summary>
        /// Gets a value indicating whether the validation passed.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets the collection of validation errors.
        /// </summary>
        public IList<string> Errors { get; }

        /// <summary>
        /// Gets or sets additional validation warnings that don't prevent execution.
        /// </summary>
        public IList<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        /// <returns>A ValidationResult indicating successful validation.</returns>
        public static ValidationResult Success()
        {
            return new ValidationResult(true);
        }

        /// <summary>
        /// Creates a failed validation result with the specified errors.
        /// </summary>
        /// <param name="errors">The validation errors.</param>
        /// <returns>A ValidationResult indicating failed validation.</returns>
        public static ValidationResult Failure(params string[] errors)
        {
            return new ValidationResult(false, errors);
        }

        /// <summary>
        /// Creates a failed validation result with the specified errors.
        /// </summary>
        /// <param name="errors">The validation errors.</param>
        /// <returns>A ValidationResult indicating failed validation.</returns>
        public static ValidationResult Failure(IEnumerable<string> errors)
        {
            return new ValidationResult(false, errors);
        }
    }

    /// <summary>
    /// Represents the result of executing an automation node.
    /// </summary>
    public class NodeResult
    {
        /// <summary>
        /// Initializes a new instance of the NodeResult class.
        /// </summary>
        /// <param name="success">Whether the execution was successful.</param>
        /// <param name="outputData">The output data produced by the node.</param>
        /// <param name="errorMessage">Error message if execution failed.</param>
        public NodeResult(bool success, Dictionary<string, object> outputData = null, string errorMessage = null)
        {
            Success = success;
            OutputData = outputData ?? new Dictionary<string, object>();
            ErrorMessage = errorMessage;
            ExecutionTime = TimeSpan.Zero;
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets a value indicating whether the node execution was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets a value indicating whether the node execution was successful (alias for Success).
        /// </summary>
        public bool IsSuccess => Success;

        /// <summary>
        /// Gets the output data produced by the node execution.
        /// </summary>
        public Dictionary<string, object> OutputData { get; }

        /// <summary>
        /// Gets the error message if the execution failed.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Gets or sets the time taken to execute the node.
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }

        /// <summary>
        /// Gets the timestamp when the execution completed.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets or sets additional metadata about the execution.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the exception that caused the failure, if any.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Creates a successful node result.
        /// </summary>
        /// <param name="outputData">The output data produced by the node.</param>
        /// <returns>A NodeResult indicating successful execution.</returns>
        public static NodeResult CreateSuccess(Dictionary<string, object> outputData = null)
        {
            return new NodeResult(true, outputData);
        }

        /// <summary>
        /// Creates a failed node result.
        /// </summary>
        /// <param name="errorMessage">The error message describing the failure.</param>
        /// <param name="exception">The exception that caused the failure.</param>
        /// <returns>A NodeResult indicating failed execution.</returns>
        public static NodeResult CreateFailure(string errorMessage, Exception exception = null)
        {
            return new NodeResult(false, null, errorMessage) { Exception = exception };
        }

        /// <summary>
        /// Gets a strongly typed output value.
        /// </summary>
        /// <typeparam name="T">The type to cast the value to.</typeparam>
        /// <param name="key">The key to look up.</param>
        /// <param name="defaultValue">The default value to return if the key is not found.</param>
        /// <returns>The output value cast to the specified type, or the default value.</returns>
        public T GetOutput<T>(string key, T defaultValue = default)
        {
            if (OutputData.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return defaultValue;
        }

        /// <summary>
        /// Sets an output value.
        /// </summary>
        /// <param name="key">The key to set.</param>
        /// <param name="value">The value to set.</param>
        public void SetOutput(string key, object value)
        {
            OutputData[key] = value;
        }
    }

    /// <summary>
    /// Represents the result of executing a complete workflow.
    /// </summary>
    public class WorkflowResult
    {
        /// <summary>
        /// Initializes a new instance of the WorkflowResult class.
        /// </summary>
        /// <param name="workflowId">The ID of the executed workflow.</param>
        /// <param name="executionId">The unique execution ID.</param>
        /// <param name="status">The final status of the workflow execution.</param>
        public WorkflowResult(string workflowId, string executionId, WorkflowStatus status)
        {
            WorkflowId = workflowId ?? throw new ArgumentNullException(nameof(workflowId));
            ExecutionId = executionId ?? throw new ArgumentNullException(nameof(executionId));
            Status = status;
            StartTime = DateTime.UtcNow;
            NodeResults = new List<NodeResult>();
            OutputData = new Dictionary<string, object>();
            Errors = new List<string>();
        }

        /// <summary>
        /// Gets the ID of the executed workflow.
        /// </summary>
        public string WorkflowId { get; }

        /// <summary>
        /// Gets the unique execution ID.
        /// </summary>
        public string ExecutionId { get; }

        /// <summary>
        /// Gets the final status of the workflow execution.
        /// </summary>
        public WorkflowStatus Status { get; private set; }

        /// <summary>
        /// Gets the time when the workflow execution started.
        /// </summary>
        public DateTime StartTime { get; }

        /// <summary>
        /// Gets or sets the time when the workflow execution completed.
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Gets the total execution time of the workflow.
        /// </summary>
        public TimeSpan? ExecutionTime => EndTime.HasValue ? EndTime.Value - StartTime : null;

        /// <summary>
        /// Gets the results of individual node executions.
        /// </summary>
        public IList<NodeResult> NodeResults { get; }

        /// <summary>
        /// Gets the final output data from the workflow execution.
        /// </summary>
        public Dictionary<string, object> OutputData { get; }

        /// <summary>
        /// Gets the collection of errors that occurred during execution.
        /// </summary>
        public IList<string> Errors { get; }

        /// <summary>
        /// Gets or sets additional metadata about the workflow execution.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Updates the workflow status and sets the end time if the workflow is completing.
        /// </summary>
        /// <param name="newStatus">The new workflow status.</param>
        public void SetStatus(WorkflowStatus newStatus)
        {
            Status = newStatus;
            if (newStatus == WorkflowStatus.Completed || newStatus == WorkflowStatus.Failed || newStatus == WorkflowStatus.Cancelled)
            {
                EndTime = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Adds a node execution result to the workflow result.
        /// </summary>
        /// <param name="nodeResult">The node execution result to add.</param>
        public void AddNodeResult(NodeResult nodeResult)
        {
            NodeResults.Add(nodeResult);
        }

        /// <summary>
        /// Adds an error to the workflow result.
        /// </summary>
        /// <param name="error">The error message to add.</param>
        public void AddError(string error)
        {
            Errors.Add(error);
        }

        /// <summary>
        /// Gets a strongly typed output value.
        /// </summary>
        /// <typeparam name="T">The type to cast the value to.</typeparam>
        /// <param name="key">The key to look up.</param>
        /// <param name="defaultValue">The default value to return if the key is not found.</param>
        /// <returns>The output value cast to the specified type, or the default value.</returns>
        public T GetOutput<T>(string key, T defaultValue = default)
        {
            if (OutputData.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return defaultValue;
        }

        /// <summary>
        /// Sets an output value.
        /// </summary>
        /// <param name="key">The key to set.</param>
        /// <param name="value">The value to set.</param>
        public void SetOutput(string key, object value)
        {
            OutputData[key] = value;
        }
    }

    /// <summary>
    /// Represents a simple connection point for automation nodes.
    /// </summary>
    public class ConnectionPoint
    {
        /// <summary>
        /// Gets or sets the type of this connection point (Input or Output).
        /// </summary>
        public ConnectionPointType Type { get; set; }

        /// <summary>
        /// Gets or sets the name of this connection point.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the data type expected/provided by this connection point.
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// Gets or sets whether this connection point is required.
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for this connection point.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}