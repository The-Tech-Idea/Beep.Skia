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
        Conditional,
        Escalation,
        Compensation,
        Link,
        Terminate,
        Cancel,
        Multiple
    }

    /// <summary>
    /// Specifies the type of event position (start, intermediate, end).
    /// </summary>
    public enum EventPosition
    {
        Start,
        IntermediateCatch,
        IntermediateThrow,
        End
    }

    /// <summary>
    /// Specifies the type of gateway.
    /// </summary>
    public enum GatewayType
    {
        Exclusive,   // XOR - diamond with X
        Inclusive,   // OR  - diamond with circle
        Parallel,    // AND - diamond with +
        Complex,     // diamond with *
        EventBased   // diamond with pentagon
    }
    {
        Sequence,
        Conditional,
        Parallel,
        Message,
        Exception
    }
}