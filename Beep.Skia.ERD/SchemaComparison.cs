using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Beep.Skia.ERD
{
    /// <summary>
    /// Kind of schema change detected between a source (current) and target (desired) schema.
    /// </summary>
    public enum SchemaChangeKind
    {
        AddedTable,
        RemovedTable,
        AddedColumn,
        RemovedColumn,
        AlteredColumn,
        AddedForeignKey,
        RemovedForeignKey,
        AddedConstraint,
        RemovedConstraint
    }

    /// <summary>
    /// A single schema difference. For altered columns, <see cref="Column"/> is the target definition
    /// and <see cref="PreviousColumn"/> is the source definition (used to build rollback scripts).
    /// </summary>
    public class SchemaChange
    {
        /// <summary>
        /// Gets or sets the kind.
        /// </summary>
        public SchemaChangeKind Kind { get; set; }
        /// <summary>
        /// Gets or sets the table name.
        /// </summary>
        public string TableName { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the column.
        /// </summary>
        public DDLImporter.ColumnInfo? Column { get; set; }
        /// <summary>
        /// Gets or sets the previous column.
        /// </summary>
        public DDLImporter.ColumnInfo? PreviousColumn { get; set; }
        /// <summary>
        /// Gets or sets the constraint.
        /// </summary>
        public DDLImporter.ConstraintInfo? Constraint { get; set; }
        /// <summary>
        /// Gets or sets the table.
        /// </summary>
        public DDLImporter.TableInfo? Table { get; set; }
        /// <summary>
        /// Gets or sets the previous table.
        /// </summary>
        public DDLImporter.TableInfo? PreviousTable { get; set; }

        /// <summary>
        /// Gets or sets the to string.
        /// </summary>
        public override string ToString() => $"{Kind}: {TableName}{(Column != null ? "." + Column.Name : "")}";
    }

    /// <summary>
    /// Result of comparing two schemas.
    /// </summary>
    public class SchemaComparisonResult
    {
        /// <summary>
        /// Gets or sets the changes.
        /// </summary>
        public List<SchemaChange> Changes { get; } = new List<SchemaChange>();

        /// <summary>
        /// Gets or sets the has changes.
        /// </summary>
        public bool HasChanges => Changes.Count > 0;

        /// <summary>
        /// Gets or sets the of kind.
        /// </summary>
        public IEnumerable<SchemaChange> OfKind(SchemaChangeKind kind) => Changes.Where(c => c.Kind == kind);

        /// <summary>
        /// Human-readable one-line-per-change summary.
        /// </summary>
        public string Summary()
        {
            if (!HasChanges) return "Schemas are identical.";

            var sb = new StringBuilder();
            foreach (var change in Changes)
            {
                switch (change.Kind)
                {
                    case SchemaChangeKind.AddedTable:
                        sb.AppendLine($"+ table {change.TableName}");
                        break;
                    case SchemaChangeKind.RemovedTable:
                        sb.AppendLine($"- table {change.TableName}");
                        break;
                    case SchemaChangeKind.AddedColumn:
                        sb.AppendLine($"+ {change.TableName}.{change.Column?.Name} ({change.Column?.DataType})");
                        break;
                    case SchemaChangeKind.RemovedColumn:
                        sb.AppendLine($"- {change.TableName}.{change.Column?.Name}");
                        break;
                    case SchemaChangeKind.AlteredColumn:
                        sb.AppendLine($"~ {change.TableName}.{change.Column?.Name}: {Describe(change.PreviousColumn)} -> {Describe(change.Column)}");
                        break;
                    case SchemaChangeKind.AddedForeignKey:
                        sb.AppendLine($"+ FK {change.TableName}.{change.Constraint?.Columns} -> {change.Constraint?.ReferencedTable}.{change.Constraint?.ReferencedColumns}");
                        break;
                    case SchemaChangeKind.RemovedForeignKey:
                        sb.AppendLine($"- FK {change.TableName}.{change.Constraint?.Columns} -> {change.Constraint?.ReferencedTable}.{change.Constraint?.ReferencedColumns}");
                        break;
                    case SchemaChangeKind.AddedConstraint:
                        sb.AppendLine($"+ constraint {change.Constraint?.Name ?? change.Constraint?.Type} on {change.TableName} ({change.Constraint?.Columns})");
                        break;
                    case SchemaChangeKind.RemovedConstraint:
                        sb.AppendLine($"- constraint {change.Constraint?.Name ?? change.Constraint?.Type} on {change.TableName} ({change.Constraint?.Columns})");
                        break;
                }
            }
            return sb.ToString().TrimEnd();
        }

        private static string Describe(DDLImporter.ColumnInfo? column)
        {
            if (column == null) return "(none)";
            var nullability = column.IsNullable ? "NULL" : "NOT NULL";
            var defaultText = string.IsNullOrWhiteSpace(column.DefaultValue) ? "" : $" DEFAULT {column.DefaultValue}";
            return $"{column.DataType} {nullability}{defaultText}".Trim();
        }
    }

    /// <summary>
    /// Compares a source schema against a target schema and reports the changes needed
    /// to migrate source to target. Tables/columns are matched case-insensitively.
    /// </summary>
    public class SchemaComparer
    {
        /// <summary>
        /// Gets or sets the compare.
        /// </summary>
        public SchemaComparisonResult Compare(
            IEnumerable<DDLImporter.TableInfo> source,
            IEnumerable<DDLImporter.TableInfo> target)
        {
            var result = new SchemaComparisonResult();
            // Duplicate table names (common in messy dumps) must not throw; the last definition wins.
            var sourceTables = new Dictionary<string, DDLImporter.TableInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var table in source ?? Enumerable.Empty<DDLImporter.TableInfo>())
            {
                if (table == null || string.IsNullOrWhiteSpace(table.TableName)) continue;
                sourceTables[table.TableName] = table;
            }

            var targetTables = new Dictionary<string, DDLImporter.TableInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var table in target ?? Enumerable.Empty<DDLImporter.TableInfo>())
            {
                if (table == null || string.IsNullOrWhiteSpace(table.TableName)) continue;
                targetTables[table.TableName] = table;
            }

            foreach (var targetTable in targetTables.Values)
            {
                if (!sourceTables.TryGetValue(targetTable.TableName, out var sourceTable))
                {
                    result.Changes.Add(new SchemaChange
                    {
                        Kind = SchemaChangeKind.AddedTable,
                        TableName = targetTable.TableName,
                        Table = targetTable
                    });
                    continue;
                }

                CompareColumns(sourceTable, targetTable, result);
                CompareConstraints(sourceTable, targetTable, result);
            }

            foreach (var sourceTable in sourceTables.Values)
            {
                if (!targetTables.ContainsKey(sourceTable.TableName))
                {
                    result.Changes.Add(new SchemaChange
                    {
                        Kind = SchemaChangeKind.RemovedTable,
                        TableName = sourceTable.TableName,
                        PreviousTable = sourceTable
                    });
                }
            }

            return result;
        }

        private static void CompareColumns(
            DDLImporter.TableInfo sourceTable,
            DDLImporter.TableInfo targetTable,
            SchemaComparisonResult result)
        {
            var sourceColumns = (sourceTable.Columns ?? new List<DDLImporter.ColumnInfo>())
                .Where(c => c != null && !string.IsNullOrWhiteSpace(c.Name))
                .ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);
            var targetColumns = (targetTable.Columns ?? new List<DDLImporter.ColumnInfo>())
                .Where(c => c != null && !string.IsNullOrWhiteSpace(c.Name))
                .ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);

            foreach (var targetColumn in targetColumns.Values)
            {
                if (!sourceColumns.TryGetValue(targetColumn.Name, out var sourceColumn))
                {
                    result.Changes.Add(new SchemaChange
                    {
                        Kind = SchemaChangeKind.AddedColumn,
                        TableName = targetTable.TableName,
                        Column = targetColumn,
                        Table = targetTable
                    });
                }
                else if (!ColumnsEqual(sourceColumn, targetColumn))
                {
                    result.Changes.Add(new SchemaChange
                    {
                        Kind = SchemaChangeKind.AlteredColumn,
                        TableName = targetTable.TableName,
                        Column = targetColumn,
                        PreviousColumn = sourceColumn,
                        Table = targetTable,
                        PreviousTable = sourceTable
                    });
                }
            }

            foreach (var sourceColumn in sourceColumns.Values)
            {
                if (!targetColumns.ContainsKey(sourceColumn.Name))
                {
                    result.Changes.Add(new SchemaChange
                    {
                        Kind = SchemaChangeKind.RemovedColumn,
                        TableName = targetTable.TableName,
                        Column = sourceColumn,
                        Table = sourceTable
                    });
                }
            }
        }

        private static void CompareConstraints(
            DDLImporter.TableInfo sourceTable,
            DDLImporter.TableInfo targetTable,
            SchemaComparisonResult result)
        {
            var sourceFks = GetConstraints(sourceTable, c => !string.IsNullOrWhiteSpace(c.ReferencedTable))
                .ToDictionary(Signature, c => c, StringComparer.OrdinalIgnoreCase);
            var targetFks = GetConstraints(targetTable, c => !string.IsNullOrWhiteSpace(c.ReferencedTable))
                .ToDictionary(Signature, c => c, StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in targetFks)
            {
                if (!sourceFks.ContainsKey(kvp.Key))
                {
                    result.Changes.Add(new SchemaChange
                    {
                        Kind = SchemaChangeKind.AddedForeignKey,
                        TableName = targetTable.TableName,
                        Constraint = kvp.Value,
                        Table = targetTable
                    });
                }
            }

            foreach (var kvp in sourceFks)
            {
                if (!targetFks.ContainsKey(kvp.Key))
                {
                    result.Changes.Add(new SchemaChange
                    {
                        Kind = SchemaChangeKind.RemovedForeignKey,
                        TableName = sourceTable.TableName,
                        Constraint = kvp.Value,
                        Table = sourceTable
                    });
                }
            }

            // UNIQUE / CHECK (and other non-FK) constraints.
            var sourceOthers = GetConstraints(sourceTable, IsTableConstraint)
                .ToDictionary(ConstraintSignature, c => c, StringComparer.OrdinalIgnoreCase);
            var targetOthers = GetConstraints(targetTable, IsTableConstraint)
                .ToDictionary(ConstraintSignature, c => c, StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in targetOthers)
            {
                if (!sourceOthers.ContainsKey(kvp.Key))
                {
                    result.Changes.Add(new SchemaChange
                    {
                        Kind = SchemaChangeKind.AddedConstraint,
                        TableName = targetTable.TableName,
                        Constraint = kvp.Value,
                        Table = targetTable
                    });
                }
            }

            foreach (var kvp in sourceOthers)
            {
                if (!targetOthers.ContainsKey(kvp.Key))
                {
                    result.Changes.Add(new SchemaChange
                    {
                        Kind = SchemaChangeKind.RemovedConstraint,
                        TableName = sourceTable.TableName,
                        Constraint = kvp.Value,
                        Table = sourceTable
                    });
                }
            }
        }

        private static IEnumerable<DDLImporter.ConstraintInfo> GetConstraints(
            DDLImporter.TableInfo table,
            Func<DDLImporter.ConstraintInfo, bool> predicate)
            => (table?.Constraints ?? new List<DDLImporter.ConstraintInfo>())
                .Where(c => c != null && !string.IsNullOrWhiteSpace(c.Type))
                .Where(predicate);

        private static bool IsTableConstraint(DDLImporter.ConstraintInfo constraint)
        {
            if (!string.IsNullOrWhiteSpace(constraint.ReferencedTable)) return false;
            var type = (constraint.Type ?? string.Empty).Trim().ToUpperInvariant();
            return type == "UNIQUE" || type == "CHECK";
        }

        private static string Signature(DDLImporter.ConstraintInfo constraint)
            => $"{Normalize(constraint.Columns)}|{Normalize(constraint.ReferencedTable)}|{Normalize(constraint.ReferencedColumns)}";

        private static string ConstraintSignature(DDLImporter.ConstraintInfo constraint)
            => $"{Normalize(constraint.Type)}|{Normalize(constraint.Columns)}";

        private static string Normalize(string? value) => (value ?? string.Empty).Replace(" ", string.Empty).ToUpperInvariant();

        private static bool ColumnsEqual(DDLImporter.ColumnInfo a, DDLImporter.ColumnInfo b)
        {
            return string.Equals(a.DataType ?? string.Empty, b.DataType ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                && a.IsNullable == b.IsNullable
                && string.Equals(a.DefaultValue ?? string.Empty, b.DefaultValue ?? string.Empty, StringComparison.Ordinal)
                && a.IsPrimaryKey == b.IsPrimaryKey
                && a.MaxLength == b.MaxLength
                && a.Precision == b.Precision
                && a.Scale == b.Scale;
        }
    }
}
