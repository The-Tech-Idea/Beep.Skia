using System;
using System.Collections.Generic;

namespace Beep.Skia.Model
{
    /// <summary>
    /// Provides data for automation node execution events.
    /// </summary>
    public class NodeExecutionEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the NodeExecutionEventArgs class.
        /// </summary>
        /// <param name="nodeId">The ID of the node.</param>
        /// <param name="previousStatus">The previous status of the node.</param>
        /// <param name="currentStatus">The current status of the node.</param>
        public NodeExecutionEventArgs(string nodeId, NodeStatus previousStatus, NodeStatus currentStatus)
        {
            NodeId = nodeId;
            PreviousStatus = previousStatus;
            CurrentStatus = currentStatus;
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the ID of the automation node.
        /// </summary>
        public string NodeId { get; }

        /// <summary>
        /// Gets the previous status of the node.
        /// </summary>
        public NodeStatus PreviousStatus { get; }

        /// <summary>
        /// Gets the current status of the node.
        /// </summary>
        public NodeStatus CurrentStatus { get; }

        /// <summary>
        /// Gets the timestamp when the status change occurred.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets or sets additional execution context data.
        /// </summary>
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Provides data for automation node error events.
    /// </summary>
    public class NodeErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the NodeErrorEventArgs class.
        /// </summary>
        /// <param name="nodeId">The ID of the node where the error occurred.</param>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="errorMessage">A descriptive error message.</param>
        public NodeErrorEventArgs(string nodeId, Exception exception, string errorMessage = null)
        {
            NodeId = nodeId;
            Exception = exception;
            ErrorMessage = errorMessage ?? exception?.Message ?? "Unknown error occurred";
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the ID of the automation node where the error occurred.
        /// </summary>
        public string NodeId { get; }

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

        /// <summary>
        /// Gets or sets additional error context data.
        /// </summary>
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Provides data for workflow execution events.
    /// </summary>
    public class WorkflowExecutionEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the WorkflowExecutionEventArgs class.
        /// </summary>
        /// <param name="workflowId">The ID of the workflow.</param>
        /// <param name="previousStatus">The previous status of the workflow.</param>
        /// <param name="currentStatus">The current status of the workflow.</param>
        public WorkflowExecutionEventArgs(string workflowId, WorkflowStatus previousStatus, WorkflowStatus currentStatus)
        {
            WorkflowId = workflowId;
            PreviousStatus = previousStatus;
            CurrentStatus = currentStatus;
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the ID of the workflow.
        /// </summary>
        public string WorkflowId { get; }

        /// <summary>
        /// Gets the previous status of the workflow.
        /// </summary>
        public WorkflowStatus PreviousStatus { get; }

        /// <summary>
        /// Gets the current status of the workflow.
        /// </summary>
        public WorkflowStatus CurrentStatus { get; }

        /// <summary>
        /// Gets the timestamp when the status change occurred.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets or sets the execution result if the workflow has completed.
        /// </summary>
        public WorkflowResult Result { get; set; }

        /// <summary>
        /// Gets or sets additional execution context data.
        /// </summary>
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Provides data for trigger activation events.
    /// </summary>
    public class TriggerEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the TriggerEventArgs class.
        /// </summary>
        /// <param name="triggerId">The ID of the trigger.</param>
        /// <param name="triggerType">The type of the trigger.</param>
        /// <param name="triggerData">Data associated with the trigger activation.</param>
        public TriggerEventArgs(string triggerId, TriggerType triggerType, Dictionary<string, object> triggerData = null)
        {
            TriggerId = triggerId;
            TriggerType = triggerType;
            TriggerData = triggerData ?? new Dictionary<string, object>();
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the ID of the trigger.
        /// </summary>
        public string TriggerId { get; }

        /// <summary>
        /// Gets the type of the trigger.
        /// </summary>
        public TriggerType TriggerType { get; }

        /// <summary>
        /// Gets the data associated with the trigger activation.
        /// </summary>
        public Dictionary<string, object> TriggerData { get; }

        /// <summary>
        /// Gets the timestamp when the trigger was activated.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the trigger activation should be cancelled.
        /// </summary>
        public bool Cancel { get; set; }
    }
}