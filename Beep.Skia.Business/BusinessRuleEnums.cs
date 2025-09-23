namespace Beep.Skia.Business
{
    /// <summary>
    /// Specifies the type of condition.
    /// </summary>
    public enum ConditionType
    {
        Simple,
        Complex,
        Temporal,
        Comparative
    }

    /// <summary>
    /// Specifies the type of action.
    /// </summary>
    public enum ActionType
    {
        Execute,
        Assign,
        Send,
        Create,
        Update,
        Delete
    }

    /// <summary>
    /// Specifies the direction of rule flow.
    /// </summary>
    public enum FlowDirection
    {
        True,
        False,
        Default
    }
}