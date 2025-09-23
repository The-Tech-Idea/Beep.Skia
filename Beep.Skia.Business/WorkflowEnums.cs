namespace Beep.Skia.Business
{
    /// <summary>
    /// Specifies the status of a task.
    /// </summary>
    public enum TaskStatus
    {
        NotStarted,
        InProgress,
        Completed,
        Cancelled,
        OnHold
    }

    /// <summary>
    /// Specifies the type of event.
    /// </summary>
    public enum EventType
    {
        Start,
        End,
        Timer,
        Message,
        Error,
        Signal,
        Conditional
    }

    /// <summary>
    /// Specifies the type of flow between components.
    /// </summary>
    public enum FlowType
    {
        Sequence,
        Conditional,
        Parallel,
        Message,
        Exception
    }
}