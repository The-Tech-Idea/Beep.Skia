namespace Beep.Skia.Business
{
    /// <summary>
    /// Specifies the type of data object.
    /// </summary>
    public enum DataType
    {
        Document,
        Database,
        Email,
        XML,
        JSON,
        Binary,
        Text
    }

    /// <summary>
    /// Specifies the type of group.
    /// </summary>
    public enum GroupType
    {
        Process,
        Data,
        Annotation,
        Security,
        Performance
    }

    /// <summary>
    /// Specifies the type of annotation.
    /// </summary>
    public enum AnnotationType
    {
        Note,
        Warning,
        Comment,
        Instruction,
        Reference
    }
}