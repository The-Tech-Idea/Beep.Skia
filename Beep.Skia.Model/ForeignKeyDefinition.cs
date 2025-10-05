using System;
using System.Collections.Generic;

namespace Beep.Skia.Model
{
    /// <summary>
    /// Describes a foreign key constraint from one ERD entity to another.
    /// </summary>
    public class ForeignKeyDefinition
    {
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Local column names forming the FK (order matters for composites).
        /// </summary>
        public List<string> Columns { get; set; } = new List<string>();
        /// <summary>
        /// Referenced entity/table name. Use Schema.Target for fully-qualified if needed.
        /// </summary>
        public string ReferencedEntity { get; set; } = string.Empty;
        /// <summary>
        /// Referenced column names (order must match Columns).
        /// </summary>
        public List<string> ReferencedColumns { get; set; } = new List<string>();
        /// <summary>
        /// On delete behavior (e.g., NO ACTION, CASCADE, SET NULL). Free-form for now.
        /// </summary>
        public string OnDelete { get; set; } = string.Empty;
        /// <summary>
        /// On update behavior (e.g., NO ACTION, CASCADE, SET NULL). Free-form for now.
        /// </summary>
        public string OnUpdate { get; set; } = string.Empty;
    }
}
