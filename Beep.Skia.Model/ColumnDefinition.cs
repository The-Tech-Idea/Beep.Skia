using System;

namespace Beep.Skia.Model
{
    /// <summary>
    /// Describes a single column/attribute for ERD entities and ETL schemas.
    /// </summary>
    public class ColumnDefinition
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the data type.
        /// </summary>
        public string DataType { get; set; } = "string"; // free-form; e.g., int, string, datetime
        /// <summary>
        /// Gets or sets the is primary key.
        /// </summary>
        public bool IsPrimaryKey { get; set; }
        /// <summary>
        /// Gets or sets the is foreign key.
        /// </summary>
        public bool IsForeignKey { get; set; }
        /// <summary>
        /// Gets or sets the is nullable.
        /// </summary>
        public bool IsNullable { get; set; } = true;
        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        public string DefaultValue { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }
}
