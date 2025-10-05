using System;
using System.Collections.Generic;

namespace Beep.Skia.Model
{
    /// <summary>
    /// Describes a database index for an ERD entity/table.
    /// </summary>
    public class IndexDefinition
    {
        public string Name { get; set; } = string.Empty;
        public bool IsUnique { get; set; } = false;
        /// <summary>
        /// Ordered list of column names participating in the index.
        /// </summary>
        public List<string> Columns { get; set; } = new List<string>();
        /// <summary>
        /// Optional free-form filter/predicate for filtered indexes (dialect-specific).
        /// </summary>
        public string Where { get; set; } = string.Empty;
    }
}
