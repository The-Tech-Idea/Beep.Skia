using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beep.Skia.Model
{
    /// <summary>
    /// Specifies the shape of a workflow component.
    /// </summary>
    public enum ComponentShape
    {
        /// <summary>
        /// A circular component shape.
        /// </summary>
        Circle,

        /// <summary>
        /// A square component shape.
        /// </summary>
        Square,

        /// <summary>
        /// A triangular component shape.
        /// </summary>
        Triangle,

        /// <summary>
        /// A table-shaped component.
        /// </summary>
        Table,

        /// <summary>
        /// A diamond-shaped component.
        /// </summary>
        Diamond,

        /// <summary>
        /// A line-shaped component.
        /// </summary>
        Line,
    }

    /// <summary>
    /// Specifies the type of a connection point (input or output).
    /// </summary>
    public enum ConnectionPointType
    {
        /// <summary>
        /// An input connection point that receives connections.
        /// </summary>
        In,
        
        /// <summary>
        /// An input connection point that receives connections (alias for In).
        /// </summary>
        Input = In,

        /// <summary>
        /// An output connection point that initiates connections.
        /// </summary>
        Out,
        
        /// <summary>
        /// An output connection point that initiates connections (alias for Out).
        /// </summary>
        Output = Out
    }

    /// <summary>
    /// Specifies the border style of a group box.
    /// </summary>
    public enum GroupBoxBorderStyle
    {
        /// <summary>
        /// No border.
        /// </summary>
        None,

        /// <summary>
        /// A standard border.
        /// </summary>
        Standard,

        /// <summary>
        /// An etched border.
        /// </summary>
        Etched
    }

    /// <summary>
    /// Specifies the execution status of a workflow.
    /// </summary>
    public enum WorkflowStatus
    {
        /// <summary>
        /// The workflow is in draft state and not yet ready for execution.
        /// </summary>
        Draft,

        /// <summary>
        /// The workflow is ready to be executed.
        /// </summary>
        Ready,

        /// <summary>
        /// The workflow is currently running.
        /// </summary>
        Running,

        /// <summary>
        /// The workflow execution is paused.
        /// </summary>
        Paused,

        /// <summary>
        /// The workflow has completed successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// The workflow execution failed with errors.
        /// </summary>
        Failed,

        /// <summary>
        /// The workflow execution was cancelled.
        /// </summary>
        Cancelled,

        /// <summary>
        /// The workflow is stopped and cannot execute.
        /// </summary>
        Stopped
    }

    /// <summary>
    /// Specifies the type of trigger that can start a workflow.
    /// </summary>
    public enum TriggerType
    {
        /// <summary>
        /// Manual trigger activated by user action.
        /// </summary>
        Manual,

        /// <summary>
        /// Scheduled trigger based on time intervals or cron expressions.
        /// </summary>
        Scheduled,

        /// <summary>
        /// Webhook trigger activated by HTTP requests.
        /// </summary>
        WebHook,

        /// <summary>
        /// Data change trigger activated by database or file changes.
        /// </summary>
        DataChange,

        /// <summary>
        /// Event-based trigger activated by system or application events.
        /// </summary>
        Event,

        /// <summary>
        /// Email trigger activated by incoming email messages.
        /// </summary>
        Email,

        /// <summary>
        /// File system trigger activated by file or directory changes.
        /// </summary>
        FileSystem,

        /// <summary>
        /// API trigger activated by external API calls.
        /// </summary>
        API
    }

    /// <summary>
    /// Specifies the execution priority of a workflow or node.
    /// </summary>
    public enum ExecutionPriority
    {
        /// <summary>
        /// Low priority execution.
        /// </summary>
        Low,

        /// <summary>
        /// Normal priority execution.
        /// </summary>
        Normal,

        /// <summary>
        /// High priority execution.
        /// </summary>
        High,

        /// <summary>
        /// Critical priority execution.
        /// </summary>
        Critical
    }

    /// <summary>
    /// Specifies the data type for workflow variables and parameters.
    /// </summary>
    public enum DataType
    {
        /// <summary>
        /// String data type.
        /// </summary>
        String,

        /// <summary>
        /// Integer number data type.
        /// </summary>
        Integer,

        /// <summary>
        /// Decimal number data type.
        /// </summary>
        Decimal,

        /// <summary>
        /// Boolean data type.
        /// </summary>
        Boolean,

        /// <summary>
        /// Date and time data type.
        /// </summary>
        DateTime,

        /// <summary>
        /// Binary data type.
        /// </summary>
        Binary,

        /// <summary>
        /// JSON object data type.
        /// </summary>
        Object,

        /// <summary>
        /// Array data type.
        /// </summary>
        Array,

        /// <summary>
        /// File data type.
        /// </summary>
        File,

        /// <summary>
        /// Image data type.
        /// </summary>
        Image
    }

    /// <summary>
    /// Specifies mouse button identifiers.
    /// </summary>
    public enum MouseButton
    {
        /// <summary>
        /// No mouse button.
        /// </summary>
        None = 0,

        /// <summary>
        /// Left mouse button.
        /// </summary>
        Left = 1,

        /// <summary>
        /// Right mouse button.
        /// </summary>
        Right = 2,

        /// <summary>
        /// Middle mouse button.
        /// </summary>
        Middle = 3
    }
}
