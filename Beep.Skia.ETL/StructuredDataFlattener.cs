using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using Beep.Skia.Model;

namespace Beep.Skia.ETL
{
    /// <summary>
    /// Options controlling flattening behavior.
    /// </summary>
    public class FlattenOptions
    {
        /// <summary>Separator used between nested field names (default ".").</summary>
        public string Separator { get; set; } = ".";

        /// <summary>Maximum nesting depth to flatten.</summary>
        public int MaxDepth { get; set; } = 32;

        /// <summary>When true, JSON nulls are emitted as null values; otherwise omitted.</summary>
        public bool IncludeNulls { get; set; } = true;
    }

    /// <summary>
    /// Flattens structured JSON/XML into tabular rows (dictionaries) for ETL processing,
    /// and infers a tabular schema from flattened rows.
    ///
    /// Nested objects become dot-separated columns (address.city); arrays are preserved
    /// as JSON text so no data is lost; XML attributes become "@name" columns.
    /// </summary>
    public class StructuredDataFlattener
    {
        // ── JSON ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Flattens a JSON document into rows.
        /// </summary>
        /// <param name="json">JSON text.</param>
        /// <param name="rootPath">Optional dot path to the array/object to flatten (e.g., "data.items").</param>
        /// <param name="options">Flattening options.</param>
        public List<Dictionary<string, object>> FlattenJson(string json, string rootPath = null, FlattenOptions options = null)
        {
            options ??= new FlattenOptions();
            var rows = new List<Dictionary<string, object>>();
            if (string.IsNullOrWhiteSpace(json)) return rows;

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(json);
            }
            catch (JsonException)
            {
                // Malformed or too-deeply-nested JSON yields no rows rather than an exception.
                return rows;
            }

            using (document)
            {
                var root = document.RootElement;

            if (!string.IsNullOrWhiteSpace(rootPath))
            {
                foreach (var segment in rootPath.Split('.'))
                {
                    if (root.ValueKind != JsonValueKind.Object) return rows;
                    if (!TryGetProperty(root, segment, out var next)) return rows;
                    root = next;
                }
            }

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in root.EnumerateArray())
                {
                    if (element.ValueKind != JsonValueKind.Object) continue;
                    var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    FlattenJsonObject(element, string.Empty, row, options, 0);
                    rows.Add(row);
                }
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                FlattenJsonObject(root, string.Empty, row, options, 0);
                rows.Add(row);
            }

                return rows;
            }
        }

        private static void FlattenJsonObject(JsonElement element, string prefix, Dictionary<string, object> row, FlattenOptions options, int depth)
        {
            if (depth > options.MaxDepth) return;

            foreach (var property in element.EnumerateObject())
            {
                var key = JoinKey(prefix, property.Name, options);
                switch (property.Value.ValueKind)
                {
                    case JsonValueKind.Object:
                        FlattenJsonObject(property.Value, key, row, options, depth + 1);
                        break;
                    case JsonValueKind.Array:
                        // Normalize to compact JSON so downstream consumers get stable text.
                        row[key] = JsonSerializer.Serialize(property.Value);
                        break;
                    case JsonValueKind.String:
                        row[key] = property.Value.GetString();
                        break;
                    case JsonValueKind.Number:
                        row[key] = property.Value.TryGetInt64(out var l) ? (object)l : property.Value.GetDouble();
                        break;
                    case JsonValueKind.True:
                        row[key] = true;
                        break;
                    case JsonValueKind.False:
                        row[key] = false;
                        break;
                    case JsonValueKind.Null:
                        if (options.IncludeNulls) row[key] = null;
                        break;
                }
            }
        }

        private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
            value = default;
            return false;
        }

        // ── XML ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Flattens an XML document into rows. When <paramref name="rowElement"/> is null,
        /// the most frequent child element of the root is used as the row element.
        /// </summary>
        public List<Dictionary<string, object>> FlattenXml(string xml, string rowElement = null, FlattenOptions options = null)
        {
            options ??= new FlattenOptions();
            var rows = new List<Dictionary<string, object>>();
            if (string.IsNullOrWhiteSpace(xml)) return rows;

            XDocument document;
            try
            {
                document = XDocument.Parse(xml);
            }
            catch (System.Xml.XmlException)
            {
                // Malformed or too-deeply-nested XML yields no rows rather than an exception.
                return rows;
            }

            var root = document.Root;
            if (root == null) return rows;

            IEnumerable<XElement> rowElements;
            if (!string.IsNullOrWhiteSpace(rowElement))
            {
                rowElements = root.Descendants()
                    .Where(e => string.Equals(e.Name.LocalName, rowElement, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                var groups = root.Elements()
                    .GroupBy(e => e.Name.LocalName, StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (groups.Count == 0) return rows;
                var chosen = groups[0].Key;
                rowElements = root.Elements()
                    .Where(e => string.Equals(e.Name.LocalName, chosen, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var element in rowElements)
            {
                var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                FlattenXmlElement(element, string.Empty, row, options, 0);
                rows.Add(row);
            }
            return rows;
        }

        private static void FlattenXmlElement(XElement element, string prefix, Dictionary<string, object> row, FlattenOptions options, int depth)
        {
            if (depth > options.MaxDepth) return;

            foreach (var attribute in element.Attributes())
            {
                row[JoinKey(prefix, "@" + attribute.Name.LocalName, options)] = attribute.Value;
            }

            var children = element.Elements().ToList();
            if (children.Count == 0)
            {
                if (!string.IsNullOrEmpty(prefix) && !row.ContainsKey(prefix))
                    row[prefix] = element.Value;
                return;
            }

            foreach (var group in children.GroupBy(c => c.Name.LocalName, StringComparer.OrdinalIgnoreCase))
            {
                var groupItems = group.ToList();
                var key = JoinKey(prefix, group.Key, options);

                if (groupItems.Count > 1)
                {
                    // Repeated elements are preserved as XML text inside a JSON array.
                    row[key] = "[" + string.Join(",", groupItems.Select(e => e.ToString(SaveOptions.DisableFormatting))) + "]";
                    continue;
                }

                var child = groupItems[0];
                if (!child.HasElements && !child.HasAttributes)
                {
                    row[key] = child.Value;
                }
                else
                {
                    FlattenXmlElement(child, key, row, options, depth + 1);
                }
            }
        }

        // ── Schema inference ─────────────────────────────────────────────────

        /// <summary>
        /// Infers a tabular schema from flattened rows. Numeric columns merge to FLOAT,
        /// mixed types fall back to STRING, and sparse/missing values mark the column nullable.
        /// </summary>
        public List<ColumnDefinition> InferSchema(IEnumerable<IDictionary<string, object>> rows, string stringType = "VARCHAR")
        {
            var schema = new List<ColumnDefinition>();
            if (rows == null) return schema;

            var types = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var presence = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var nulls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();
            int rowCount = 0;

            foreach (var row in rows)
            {
                if (row == null) continue;
                rowCount++;

                foreach (var kvp in row)
                {
                    if (!types.TryGetValue(kvp.Key, out var set))
                    {
                        set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        types[kvp.Key] = set;
                        presence[kvp.Key] = 0;
                        order.Add(kvp.Key);
                    }
                    presence[kvp.Key]++;

                    var type = ClassifyType(kvp.Value);
                    if (type == null) nulls.Add(kvp.Key);
                    else set.Add(type);
                }
            }

            foreach (var name in order)
            {
                var merged = MergeTypes(types[name]);
                schema.Add(new ColumnDefinition
                {
                    Name = name,
                    DataType = merged == "STRING" ? stringType : merged,
                    IsNullable = nulls.Contains(name) || presence[name] < rowCount
                });
            }

            return schema;
        }

        private static string ClassifyType(object value)
        {
            if (value == null || value is DBNull) return null;
            if (value is bool) return "BOOLEAN";
            if (value is byte || value is sbyte || value is short || value is ushort
                || value is int || value is uint || value is long || value is ulong) return "BIGINT";
            if (value is float || value is double || value is decimal) return "FLOAT";
            if (value is DateTime || value is DateTimeOffset) return "TIMESTAMP";
            return "STRING";
        }

        private static string MergeTypes(HashSet<string> types)
        {
            if (types.Count == 0) return "STRING";
            if (types.Contains("STRING")) return "STRING";
            if (types.Contains("FLOAT")) return "FLOAT";
            if (types.Contains("TIMESTAMP")) return "TIMESTAMP";
            if (types.Contains("BIGINT")) return "BIGINT";
            if (types.Contains("BOOLEAN")) return "BOOLEAN";
            return "STRING";
        }

        private static string JoinKey(string prefix, string name, FlattenOptions options)
            => string.IsNullOrEmpty(prefix) ? name : prefix + options.Separator + name;
    }
}
