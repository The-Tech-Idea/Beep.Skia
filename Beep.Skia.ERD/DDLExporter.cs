using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beep.Skia.Model;

namespace Beep.Skia.ERD
{
    /// <summary>
    /// Exports ERD entities and relationships to DDL (Data Definition Language) statements.
    /// Supports standard SQL with basic dialect hints.
    /// </summary>
    public class DDLExporter
    {
        public enum SQLDialect
        {
            ANSI,
            SQLServer,
            PostgreSQL,
            MySQL,
            Oracle,
            SQLite
        }

        private readonly SQLDialect _dialect;
        private readonly StringBuilder _sb;

        public DDLExporter(SQLDialect dialect = SQLDialect.ANSI)
        {
            _dialect = dialect;
            _sb = new StringBuilder();
        }

        /// <summary>
        /// Exports a single ERDEntity to a CREATE TABLE statement.
        /// </summary>
        public string ExportEntity(ERDEntity entity)
        {
            _sb.Clear();
            
            // Parse entity metadata
            var schema = GetNodePropertyString(entity, "Schema");
            var entityName = entity.EntityName;
            var comment = GetNodePropertyString(entity, "Comment");
            
            // Parse columns
            var columns = ParseColumns(entity);
            if (columns == null || columns.Count == 0)
            {
                return $"-- No columns defined for {entityName}\n";
            }

            // Parse indexes
            var indexes = ParseIndexes(entity);
            
            // Parse foreign keys
            var foreignKeys = ParseForeignKeys(entity);

            // Build CREATE TABLE
            var tableName = string.IsNullOrWhiteSpace(schema) ? QuoteIdentifier(entityName) : $"{QuoteIdentifier(schema)}.{QuoteIdentifier(entityName)}";
            
            _sb.AppendLine($"CREATE TABLE {tableName} (");
            
            // Column definitions
            var columnLines = new List<string>();
            foreach (var col in columns)
            {
                columnLines.Add(BuildColumnDefinition(col));
            }
            
            // Primary key constraint
            var pkColumns = columns.Where(c => c.IsPrimaryKey).Select(c => c.Name).ToList();
            if (pkColumns.Any())
            {
                columnLines.Add($"    CONSTRAINT {QuoteIdentifier($"PK_{entityName}")} PRIMARY KEY ({string.Join(", ", pkColumns.Select(QuoteIdentifier))})");
            }
            
            // Unique constraints from indexes
            foreach (var idx in indexes.Where(i => i.IsUnique))
            {
                var idxCols = idx.Columns ?? new List<string>();
                if (idxCols.Any())
                {
                    var wherePart = string.IsNullOrWhiteSpace(idx.Where) ? "" : $" WHERE {idx.Where}";
                    columnLines.Add($"    CONSTRAINT {QuoteIdentifier(idx.Name)} UNIQUE ({string.Join(", ", idxCols.Select(QuoteIdentifier))}){wherePart}");
                }
            }
            
            // Foreign key constraints
            foreach (var fk in foreignKeys)
            {
                var fkCols = fk.Columns ?? new List<string>();
                var refCols = fk.ReferencedColumns ?? new List<string>();
                if (fkCols.Any() && refCols.Any() && !string.IsNullOrWhiteSpace(fk.ReferencedEntity))
                {
                    var fkLine = $"    CONSTRAINT {QuoteIdentifier(fk.Name)} FOREIGN KEY ({string.Join(", ", fkCols.Select(QuoteIdentifier))}) REFERENCES {QuoteIdentifier(fk.ReferencedEntity)}({string.Join(", ", refCols.Select(QuoteIdentifier))})";
                    if (!string.IsNullOrWhiteSpace(fk.OnDelete))
                        fkLine += $" ON DELETE {fk.OnDelete}";
                    if (!string.IsNullOrWhiteSpace(fk.OnUpdate))
                        fkLine += $" ON UPDATE {fk.OnUpdate}";
                    columnLines.Add(fkLine);
                }
            }
            
            _sb.AppendLine(string.Join(",\n", columnLines));
            _sb.AppendLine(");");
            
            // Table comment (dialect-specific)
            if (!string.IsNullOrWhiteSpace(comment))
            {
                _sb.AppendLine();
                _sb.AppendLine(BuildTableComment(tableName, comment));
            }
            
            // Non-unique indexes
            foreach (var idx in indexes.Where(i => !i.IsUnique))
            {
                var idxCols = idx.Columns ?? new List<string>();
                if (idxCols.Any())
                {
                    _sb.AppendLine();
                    var wherePart = string.IsNullOrWhiteSpace(idx.Where) ? "" : $" WHERE {idx.Where}";
                    _sb.AppendLine($"CREATE INDEX {QuoteIdentifier(idx.Name)} ON {tableName} ({string.Join(", ", idxCols.Select(QuoteIdentifier))}){wherePart};");
                }
            }
            
            return _sb.ToString();
        }

        /// <summary>
        /// Exports multiple entities to a complete DDL script.
        /// </summary>
        public string ExportEntities(IEnumerable<ERDEntity> entities)
        {
            _sb.Clear();
            _sb.AppendLine("-- Generated DDL Script");
            _sb.AppendLine($"-- Dialect: {_dialect}");
            _sb.AppendLine($"-- Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            _sb.AppendLine();
            
            foreach (var entity in entities)
            {
                _sb.AppendLine("-- ==========================================");
                _sb.AppendLine($"-- Table: {entity.EntityName}");
                _sb.AppendLine("-- ==========================================");
                _sb.AppendLine();
                _sb.AppendLine(ExportEntity(entity));
                _sb.AppendLine();
            }
            
            return _sb.ToString();
        }

        private string BuildColumnDefinition(ColumnDefinition col)
        {
            var parts = new List<string> { "    " + QuoteIdentifier(col.Name), MapDataType(col.DataType) };
            
            if (!col.IsNullable)
                parts.Add("NOT NULL");
            
            if (!string.IsNullOrWhiteSpace(col.DefaultValue))
                parts.Add($"DEFAULT {col.DefaultValue}");
            
            return string.Join(" ", parts);
        }

        private string BuildTableComment(string tableName, string comment)
        {
            switch (_dialect)
            {
                case SQLDialect.PostgreSQL:
                    return $"COMMENT ON TABLE {tableName} IS '{EscapeString(comment)}';";
                case SQLDialect.MySQL:
                    return $"ALTER TABLE {tableName} COMMENT = '{EscapeString(comment)}';";
                case SQLDialect.SQLServer:
                    return $"EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'{EscapeString(comment)}', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'{tableName}';";
                case SQLDialect.Oracle:
                    return $"COMMENT ON TABLE {tableName} IS '{EscapeString(comment)}';";
                default:
                    return $"-- Table comment: {comment}";
            }
        }

        private string MapDataType(string dataType)
        {
            if (string.IsNullOrWhiteSpace(dataType))
                return "VARCHAR(255)";
            
            var lower = dataType.Trim().ToLowerInvariant();
            
            // Common mappings
            switch (lower)
            {
                case "string":
                case "text":
                    return _dialect == SQLDialect.SQLServer ? "NVARCHAR(MAX)" : "TEXT";
                case "int":
                case "integer":
                    return "INTEGER";
                case "long":
                case "bigint":
                    return "BIGINT";
                case "short":
                case "smallint":
                    return "SMALLINT";
                case "decimal":
                case "numeric":
                    return "DECIMAL(18,2)";
                case "float":
                case "double":
                    return "FLOAT";
                case "bool":
                case "boolean":
                    return _dialect == SQLDialect.SQLServer ? "BIT" : "BOOLEAN";
                case "datetime":
                case "timestamp":
                    return _dialect == SQLDialect.SQLServer ? "DATETIME2" : "TIMESTAMP";
                case "date":
                    return "DATE";
                case "time":
                    return "TIME";
                case "guid":
                case "uuid":
                    return _dialect == SQLDialect.PostgreSQL ? "UUID" : "UNIQUEIDENTIFIER";
                case "binary":
                case "blob":
                    return _dialect == SQLDialect.SQLServer ? "VARBINARY(MAX)" : "BLOB";
                default:
                    // Pass through custom types as-is
                    return dataType;
            }
        }

        private string QuoteIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return "\"unnamed\"";
            
            switch (_dialect)
            {
                case SQLDialect.SQLServer:
                    return $"[{identifier.Replace("]", "]]")}]";
                case SQLDialect.MySQL:
                    return $"`{identifier.Replace("`", "``")}`";
                case SQLDialect.PostgreSQL:
                case SQLDialect.SQLite:
                case SQLDialect.Oracle:
                case SQLDialect.ANSI:
                default:
                    return $"\"{identifier.Replace("\"", "\"\"")}\"";
            }
        }

        private string EscapeString(string value)
        {
            return value?.Replace("'", "''") ?? string.Empty;
        }

        private List<ColumnDefinition> ParseColumns(ERDEntity entity)
        {
            try
            {
                if (entity.NodeProperties != null && entity.NodeProperties.TryGetValue("Columns", out var p) && p != null)
                {
                    var json = p.ParameterCurrentValue as string ?? p.DefaultParameterValue as string;
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        return System.Text.Json.JsonSerializer.Deserialize<List<ColumnDefinition>>(json) ?? new List<ColumnDefinition>();
                    }
                }
            }
            catch { }
            return new List<ColumnDefinition>();
        }

        private List<IndexDefinition> ParseIndexes(ERDEntity entity)
        {
            try
            {
                if (entity.NodeProperties != null && entity.NodeProperties.TryGetValue("Indexes", out var p) && p != null)
                {
                    var json = p.ParameterCurrentValue as string ?? p.DefaultParameterValue as string;
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        return System.Text.Json.JsonSerializer.Deserialize<List<IndexDefinition>>(json) ?? new List<IndexDefinition>();
                    }
                }
            }
            catch { }
            return new List<IndexDefinition>();
        }

        private List<ForeignKeyDefinition> ParseForeignKeys(ERDEntity entity)
        {
            try
            {
                if (entity.NodeProperties != null && entity.NodeProperties.TryGetValue("ForeignKeys", out var p) && p != null)
                {
                    var json = p.ParameterCurrentValue as string ?? p.DefaultParameterValue as string;
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        return System.Text.Json.JsonSerializer.Deserialize<List<ForeignKeyDefinition>>(json) ?? new List<ForeignKeyDefinition>();
                    }
                }
            }
            catch { }
            return new List<ForeignKeyDefinition>();
        }

        private string GetNodePropertyString(ERDEntity entity, string key)
        {
            try
            {
                if (entity.NodeProperties != null && entity.NodeProperties.TryGetValue(key, out var p) && p != null)
                {
                    return p.ParameterCurrentValue as string ?? p.DefaultParameterValue as string ?? string.Empty;
                }
            }
            catch { }
            return string.Empty;
        }
    }
}
