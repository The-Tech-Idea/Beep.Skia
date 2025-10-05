using System;
using System.Collections.Generic;
using System.Linq;

namespace Beep.Skia.Model
{
    /// <summary>
    /// Structured diff between two schemas (expected vs actual) with specific categories.
    /// </summary>
    public class SchemaDiff
    {
        public List<string> MissingColumns { get; } = new();
        public List<(string Name, string ExpectedType, string ActualType)> TypeDifferences { get; } = new();
        public List<(string Name, bool ExpectedNullable, bool ActualNullable)> NullabilityDifferences { get; } = new();
        public List<(string Name, string ExpectedDefault, string ActualDefault)> DefaultDifferences { get; } = new();

        public bool HasDifferences()
        {
            return MissingColumns.Count > 0 || TypeDifferences.Count > 0 ||
                   NullabilityDifferences.Count > 0 || DefaultDifferences.Count > 0;
        }
    }

    public static class SchemaDiffUtil
    {
        public static SchemaDiff Compute(IEnumerable<ColumnDefinition> expected, IEnumerable<ColumnDefinition> actual)
        {
            var diff = new SchemaDiff();
            if (expected == null) return diff;
            actual ??= Enumerable.Empty<ColumnDefinition>();

            var actByName = actual.Where(a => !string.IsNullOrWhiteSpace(a?.Name))
                                  .ToDictionary(a => a.Name, a => a, StringComparer.OrdinalIgnoreCase);
            foreach (var e in expected)
            {
                if (e == null || string.IsNullOrWhiteSpace(e.Name)) continue;
                if (!actByName.TryGetValue(e.Name, out var a))
                {
                    diff.MissingColumns.Add(e.Name);
                    continue;
                }
                var et = e.DataType ?? string.Empty;
                var at = a.DataType ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(et) && !string.IsNullOrWhiteSpace(at) &&
                    !string.Equals(et, at, StringComparison.OrdinalIgnoreCase))
                {
                    diff.TypeDifferences.Add((e.Name, et, at));
                }

                // Nullability and defaults
                if (e.IsNullable != a.IsNullable)
                {
                    diff.NullabilityDifferences.Add((e.Name, e.IsNullable, a.IsNullable));
                }
                var ed = e.DefaultValue ?? string.Empty;
                var ad = a.DefaultValue ?? string.Empty;
                if (!string.Equals(ed, ad, StringComparison.Ordinal))
                {
                    // Ignore diffs when both are empty
                    if (!(string.IsNullOrEmpty(ed) && string.IsNullOrEmpty(ad)))
                        diff.DefaultDifferences.Add((e.Name, ed, ad));
                }
            }

            return diff;
        }
    }
}
