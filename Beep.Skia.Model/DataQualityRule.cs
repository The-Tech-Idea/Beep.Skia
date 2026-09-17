using System;
using System.Collections.Generic;

namespace Beep.Skia.Model
{
    /// <summary>
    /// Represents a data quality validation rule for ETL processing.
    /// </summary>
    public class DataQualityRule
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the column name.
        /// </summary>
        public string ColumnName { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        public RuleType Type { get; set; } = RuleType.NotNull;
        /// <summary>
        /// Gets or sets the expression.
        /// </summary>
        public string Expression { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the stop on error.
        /// </summary>
        public bool StopOnError { get; set; } = false;
    }

    public enum RuleType
    {
        NotNull,
        Unique,
        Range,
        Regex,
        Length,
        Custom
    }

    /// <summary>
    /// Represents a derived column definition for ETL transformations.
    /// </summary>
    public class DerivedColumnDefinition
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the expression.
        /// </summary>
        public string Expression { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the data type.
        /// </summary>
        public string DataType { get; set; } = "string";
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a conditional split route in ETL processing.
    /// </summary>
    public class SplitCondition
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the expression.
        /// </summary>
        public string Expression { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the order.
        /// </summary>
        public int Order { get; set; } = 0;
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a lookup configuration for ETL enrichment.
    /// </summary>
    public class LookupConfiguration
    {
        /// <summary>
        /// Gets or sets the reference source.
        /// </summary>
        public string ReferenceSource { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the match columns.
        /// </summary>
        public List<string> MatchColumns { get; set; } = new();
        /// <summary>
        /// Gets or sets the return columns.
        /// </summary>
        public List<string> ReturnColumns { get; set; } = new();
        /// <summary>
        /// Gets or sets the cache mode.
        /// </summary>
        public CacheMode CacheMode { get; set; } = CacheMode.Full;
        /// <summary>
        /// Gets or sets the fail on no match.
        /// </summary>
        public bool FailOnNoMatch { get; set; } = false;
    }

    public enum CacheMode
    {
        None,
        Partial,
        Full
    }

    /// <summary>
    /// Represents SCD (Slowly Changing Dimension) configuration.
    /// </summary>
    public class ScdConfiguration
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        public ScdType Type { get; set; } = ScdType.Type1;
        /// <summary>
        /// Gets or sets the business keys.
        /// </summary>
        public List<string> BusinessKeys { get; set; } = new();
        /// <summary>
        /// Gets or sets the change tracking columns.
        /// </summary>
        public List<string> ChangeTrackingColumns { get; set; } = new();
        /// <summary>
        /// Gets or sets the effective from column.
        /// </summary>
        public string EffectiveFromColumn { get; set; } = "EffectiveFrom";
        /// <summary>
        /// Gets or sets the effective to column.
        /// </summary>
        public string EffectiveToColumn { get; set; } = "EffectiveTo";
        /// <summary>
        /// Gets or sets the current flag column.
        /// </summary>
        public string CurrentFlagColumn { get; set; } = "IsCurrent";
        /// <summary>
        /// Gets or sets the version column.
        /// </summary>
        public string VersionColumn { get; set; } = "Version";
    }

    public enum ScdType
    {
        Type1,  // Overwrite
        Type2,  // Historical tracking with effective dates
        Type3   // Previous value columns
    }
}
