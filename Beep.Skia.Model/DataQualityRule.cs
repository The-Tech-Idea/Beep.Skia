using System;
using System.Collections.Generic;

namespace Beep.Skia.Model
{
    /// <summary>
    /// Represents a data quality validation rule for ETL processing.
    /// </summary>
    public class DataQualityRule
    {
        public string Name { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
        public RuleType Type { get; set; } = RuleType.NotNull;
        public string Expression { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
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
        public string Name { get; set; } = string.Empty;
        public string Expression { get; set; } = string.Empty;
        public string DataType { get; set; } = "string";
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a conditional split route in ETL processing.
    /// </summary>
    public class SplitCondition
    {
        public string Name { get; set; } = string.Empty;
        public string Expression { get; set; } = string.Empty;
        public int Order { get; set; } = 0;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a lookup configuration for ETL enrichment.
    /// </summary>
    public class LookupConfiguration
    {
        public string ReferenceSource { get; set; } = string.Empty;
        public List<string> MatchColumns { get; set; } = new();
        public List<string> ReturnColumns { get; set; } = new();
        public CacheMode CacheMode { get; set; } = CacheMode.Full;
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
        public ScdType Type { get; set; } = ScdType.Type1;
        public List<string> BusinessKeys { get; set; } = new();
        public List<string> ChangeTrackingColumns { get; set; } = new();
        public string EffectiveFromColumn { get; set; } = "EffectiveFrom";
        public string EffectiveToColumn { get; set; } = "EffectiveTo";
        public string CurrentFlagColumn { get; set; } = "IsCurrent";
        public string VersionColumn { get; set; } = "Version";
    }

    public enum ScdType
    {
        Type1,  // Overwrite
        Type2,  // Historical tracking with effective dates
        Type3   // Previous value columns
    }
}
