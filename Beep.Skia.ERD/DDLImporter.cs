using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Beep.Skia.ERD
{
    /// <summary>
    /// Parses SQL CREATE TABLE statements into entity definitions.
    /// Dialect-aware parsing for SQL Server, PostgreSQL, MySQL, Oracle, and SQLite.
    /// </summary>
    public class DDLImporter
    {
        public enum SQLDialect { ANSI, SQLServer, PostgreSQL, MySQL, Oracle, SQLite }

        private readonly SQLDialect _dialect;

        public class TableInfo
        {
            /// <summary>
            /// Gets or sets the table name.
            /// </summary>
            public string TableName { get; set; } = string.Empty;
            /// <summary>
            /// Gets or sets the schema name.
            /// </summary>
            public string? SchemaName { get; set; }
            /// <summary>
            /// Gets or sets the columns.
            /// </summary>
            public List<ColumnInfo> Columns { get; set; } = new();
            /// <summary>
            /// Gets or sets the constraints.
            /// </summary>
            public List<ConstraintInfo> Constraints { get; set; } = new();
        }

        public class ColumnInfo
        {
            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            public string Name { get; set; } = string.Empty;
            /// <summary>
            /// Gets or sets the data type.
            /// </summary>
            public string DataType { get; set; } = string.Empty;
            /// <summary>
            /// Gets or sets the is primary key.
            /// </summary>
            public bool IsPrimaryKey { get; set; }
            /// <summary>
            /// Gets or sets the is nullable.
            /// </summary>
            public bool IsNullable { get; set; } = true;
            /// <summary>
            /// Gets or sets the is auto increment.
            /// </summary>
            public bool IsAutoIncrement { get; set; }
            /// <summary>
            /// Gets or sets the default value.
            /// </summary>
            public string? DefaultValue { get; set; }
            /// <summary>
            /// Gets or sets the max length.
            /// </summary>
            public int? MaxLength { get; set; }
            /// <summary>
            /// Gets or sets the precision.
            /// </summary>
            public int? Precision { get; set; }
            /// <summary>
            /// Gets or sets the scale.
            /// </summary>
            public int? Scale { get; set; }
        }

        public class ConstraintInfo
        {
            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            public string Name { get; set; } = string.Empty;
            /// <summary>
            /// Gets or sets the type.
            /// </summary>
            public string Type { get; set; } = string.Empty;
            /// <summary>
            /// Gets or sets the columns.
            /// </summary>
            public string Columns { get; set; } = string.Empty;
            /// <summary>
            /// Gets or sets the referenced table.
            /// </summary>
            public string? ReferencedTable { get; set; }
            /// <summary>
            /// Gets or sets the referenced columns.
            /// </summary>
            public string? ReferencedColumns { get; set; }
            /// <summary>
            /// Gets or sets the on delete.
            /// </summary>
            public string? OnDelete { get; set; }
            /// <summary>
            /// Gets or sets the on update.
            /// </summary>
            public string? OnUpdate { get; set; }
        }

        /// <summary>
        /// Initializes a new instance of the DDLImporter class.
        /// </summary>
        public DDLImporter(SQLDialect dialect = SQLDialect.ANSI) { _dialect = dialect; }

        /// <summary>Parses DDL and returns table definitions.</summary>
        public List<TableInfo> Parse(string ddl)
        {
            var tables = new List<TableInfo>();
            if (string.IsNullOrWhiteSpace(ddl)) return tables;

            ddl = Regex.Replace(ddl, @"--.*$", "", RegexOptions.Multiline);
            ddl = Regex.Replace(ddl, @"/\*.*?\*/", "", RegexOptions.Singleline);
            ddl = ddl.Replace("\r\n", "\n").Replace("\r", "\n");

            // Accepts quoted identifiers ("Order Items", [Order Items], `Order Items`) as well as plain ones.
            var matches = Regex.Matches(ddl,
                @"CREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?(?:""(?<name>[^""]+)""|\[(?<name>[^\]]+)\]|`(?<name>[^`]+)`|(?<name>[\w.]+))\s*\((?<body>[\s\S]*?)\)\s*;",
                RegexOptions.IgnoreCase | RegexOptions.Multiline);

            foreach (Match match in matches)
            {
                var rawName = match.Groups["name"].Value.Trim();
                if (rawName.Length == 0) continue;

                var separator = rawName.IndexOf('.');
                var table = new TableInfo
                {
                    TableName = separator >= 0 ? rawName.Substring(separator + 1) : rawName,
                    SchemaName = separator >= 0 ? rawName.Substring(0, separator) : null
                };
                ParseTableBody(match.Groups["body"].Value, table);
                ApplyPkConstraints(table);
                tables.Add(table);
            }
            return tables;
        }

        /// <summary>Creates ERDEntity instances from a DDL string.</summary>
        public List<ERDEntity> ImportToEntities(string ddl, SQLDialect dialect)
        {
            var importer = new DDLImporter(dialect);
            var tables = importer.Parse(ddl);
            return tables.Select(t => ERDEntity.FromTableInfo(t)).ToList();
        }

        // ── private parsing helpers ──────────────────────────────────────

        private void ParseTableBody(string body, TableInfo table)
        {
            foreach (var part in SplitColumns(body))
            {
                var t = part.Trim();
                if (string.IsNullOrWhiteSpace(t)) continue;
                if (IsConstraint(t)) ParseConstraint(t, table);
                else ParseColumn(t, table);
            }
        }

        private void ApplyPkConstraints(TableInfo table)
        {
            foreach (var c in table.Constraints.Where(x => x.Type == "PRIMARY KEY"))
            {
                foreach (var pk in (c.Columns ?? "").Split(','))
                {
                    var col = table.Columns.FirstOrDefault(x =>
                        string.Equals(x.Name, pk.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (col != null) col.IsPrimaryKey = true;
                }
            }
        }

        private List<string> SplitColumns(string body)
        {
            var r = new List<string>();
            int d = 0, s = 0;
            for (int i = 0; i < body.Length; i++)
            {
                if (body[i] == '(') d++;
                else if (body[i] == ')') d--;
                else if (body[i] == ',' && d == 0) { r.Add(body.Substring(s, i - s)); s = i + 1; }
            }
            if (s < body.Length) r.Add(body.Substring(s));
            return r;
        }

        private bool IsConstraint(string p) => Regex.IsMatch(p.TrimStart(), @"^(CONSTRAINT|PRIMARY\s+KEY|FOREIGN\s+KEY|UNIQUE\s|UNIQUE\(|CHECK\s|CHECK\()", RegexOptions.IgnoreCase);

        private void ParseConstraint(string part, TableInfo table)
        {
            var constraint = new ConstraintInfo();
            var up = part.TrimStart().ToUpperInvariant();

            if (up.StartsWith("CONSTRAINT"))
            {
                var m = Regex.Match(part, @"CONSTRAINT\s+(?:`?\[?""?)?""?(\w+)""?""?\]?`?\s+(PRIMARY\s+KEY|FOREIGN\s+KEY|UNIQUE|CHECK)\s*(.*)", RegexOptions.IgnoreCase);
                if (!m.Success) return;
                constraint.Name = m.Groups[1].Value;
                constraint.Type = m.Groups[2].Value.ToUpperInvariant();
                ParseDetail(m.Groups[3].Value, constraint);
            }
            else if (up.StartsWith("PRIMARY KEY")) { constraint.Type = "PRIMARY KEY"; ParseDetail(part.Substring("PRIMARY KEY".Length), constraint); }
            else if (up.StartsWith("FOREIGN KEY")) { constraint.Type = "FOREIGN KEY"; ParseDetail(part.Substring("FOREIGN KEY".Length), constraint); }
            else if (up.StartsWith("UNIQUE")) { constraint.Type = "UNIQUE"; ParseDetail(part.Substring("UNIQUE".Length), constraint); }
            else if (up.StartsWith("CHECK")) { constraint.Type = "CHECK"; ParseDetail(part.Substring("CHECK".Length), constraint); }
            else return;

            table.Constraints.Add(constraint);
        }

        private void ParseDetail(string detail, ConstraintInfo c)
        {
            var colsM = Regex.Match(detail, @"\(\s*([^)]+)\s*\)");
            if (colsM.Success) c.Columns = colsM.Groups[1].Value;

            var fkM = Regex.Match(detail,
                @"REFERENCES\s+(?:`?\[?""?)?""?(\w+\.?\w*)""?""?\]?`?\s*\(\s*([^)]+)\s*\)(?:\s+ON\s+DELETE\s+(CASCADE|SET\s+NULL|SET\s+DEFAULT|NO\s+ACTION|RESTRICT))?(?:\s+ON\s+UPDATE\s+(CASCADE|SET\s+NULL|SET\s+DEFAULT|NO\s+ACTION|RESTRICT))?",
                RegexOptions.IgnoreCase);
            if (fkM.Success)
            {
                c.Type = "FOREIGN KEY";
                c.ReferencedTable = fkM.Groups[1].Value;
                c.ReferencedColumns = fkM.Groups[2].Value;
                c.OnDelete = fkM.Groups[3].Success ? fkM.Groups[3].Value : null;
                c.OnUpdate = fkM.Groups[4].Success ? fkM.Groups[4].Value : null;
            }
        }

        private void ParseColumn(string part, TableInfo table)
        {
            // Type is a single word plus optional multi-word qualifiers (e.g., DOUBLE PRECISION, CHARACTER VARYING).
            var m = Regex.Match(part,
                @"^(?:`?\[?""?)?""?(\w+)""?""?\]?`?\s+([A-Za-z_]\w*(?:\s+(?:PRECISION|VARYING|UNSIGNED|ZEROFILL))?)(?:\s*\(\s*(\d+(?:\s*,\s*\d+)?)\s*\))?\s*(.*)",
                RegexOptions.IgnoreCase);
            if (!m.Success) return;

            var col = new ColumnInfo
            {
                Name = m.Groups[1].Value,
                DataType = NormalizeType(m.Groups[2].Value.Trim().ToUpperInvariant())
            };

            if (m.Groups[3].Success)
            {
                var parts = m.Groups[3].Value.Replace(" ", "").Split(',');
                if (int.TryParse(parts[0], out int len)) col.MaxLength = len;
                if (parts.Length > 1 && int.TryParse(parts[1], out int scale))
                {
                    col.Precision = col.MaxLength;
                    col.Scale = scale;
                    col.MaxLength = null;
                }
            }

            var attrs = m.Groups[4].Value.ToUpperInvariant();

            // Oracle GENERATED AS IDENTITY
            if (Regex.IsMatch(m.Groups[4].Value, @"GENERATED\s+(ALWAYS\s+)?(BY\s+DEFAULT\s+)?AS\s+IDENTITY", RegexOptions.IgnoreCase))
                col.IsAutoIncrement = true;

            if (attrs.Contains("NOT NULL") || attrs.Contains("NOTNULL")) col.IsNullable = false;
            if (attrs.Contains("PRIMARY KEY") || attrs.Contains("PRIMARYKEY")) col.IsPrimaryKey = true;
            if (attrs.Contains("AUTO_INCREMENT") || attrs.Contains("AUTOINCREMENT")
                || attrs.Contains("IDENTITY") || attrs.Contains("SERIAL"))
                col.IsAutoIncrement = true;

            var defM = Regex.Match(part, @"DEFAULT\s+('[^']*'|\([^)]*\)|\S+)", RegexOptions.IgnoreCase);
            if (defM.Success) col.DefaultValue = defM.Groups[1].Value.Trim('\'').Trim('(').Trim(')');

            table.Columns.Add(col);
        }

        private string NormalizeType(string raw)
        {
            // Strip whitespace prefixes
            raw = Regex.Replace(raw.Trim(), @"\s+(UNSIGNED|SIGNED|ZEROFILL)$", "", RegexOptions.IgnoreCase);

            return _dialect switch
            {
                SQLDialect.Oracle => raw switch
                {
                    "VARCHAR2" => "VARCHAR", "NUMBER" => "DECIMAL", "CLOB" => "TEXT",
                    "BLOB" => "BYTEA", "NVARCHAR2" => "NVARCHAR", "RAW" => "BYTEA",
                    "DATE" when raw == "DATE" => "TIMESTAMP",
                    _ => raw
                },
                SQLDialect.PostgreSQL => raw switch
                {
                    "SERIAL" => "INT", "BIGSERIAL" => "BIGINT", "SMALLSERIAL" => "SMALLINT",
                    "BOOLEAN" => "BOOL", "CHARACTER VARYING" => "VARCHAR",
                    "CHARACTER" => "CHAR", "JSONB" => "JSON", "TIMESTAMPTZ" => "TIMESTAMP",
                    "FLOAT8" => "DOUBLE", "FLOAT4" => "FLOAT", "INT8" => "BIGINT",
                    "INT4" => "INT", "INT2" => "SMALLINT",
                    _ => raw
                },
                SQLDialect.MySQL => raw switch
                {
                    "TINYINT" => "SMALLINT", "MEDIUMINT" => "INT",
                    "LONGTEXT" => "TEXT", "MEDIUMTEXT" => "TEXT", "TINYTEXT" => "TEXT",
                    "LONGBLOB" => "BYTEA", "MEDIUMBLOB" => "BYTEA", "TINYBLOB" => "BYTEA",
                    "DATETIME" => "TIMESTAMP",
                    _ when raw.StartsWith("ENUM") => "VARCHAR",
                    _ when raw.StartsWith("SET") => "VARCHAR",
                    _ => raw
                },
                SQLDialect.SQLite => raw switch
                {
                    _ when raw == "INTEGER" => "INT",
                    _ => raw
                },
                _ => raw
            };
        }
    }
}
