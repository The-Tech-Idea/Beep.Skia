using System;

namespace Beep.Skia.Model
{
    /// <summary>
    /// Describes a single column/attribute for ERD entities and ETL schemas.
    /// </summary>
    public class ColumnDefinition
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string DataType { get; set; } = "string"; // free-form; e.g., int, string, datetime
        public bool IsPrimaryKey { get; set; }
        public bool IsForeignKey { get; set; }
        public bool IsNullable { get; set; } = true;
        public string DefaultValue { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
