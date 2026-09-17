using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Beep.Skia.ETL
{
    /// <summary>
    /// Formats rows (dictionaries) as an aligned plain-text table for data previews.
    /// </summary>
    public static class DataPreviewFormatter
    {
        /// <summary>
        /// Renders up to <paramref name="maxRows"/> rows as a fixed-width text table.
        /// Long cell values are truncated to <paramref name="maxColumnWidth"/>.
        /// </summary>
        public static string ToTable(
            IEnumerable<IDictionary<string, object>> rows,
            int maxRows = 20,
            int maxColumnWidth = 24)
        {
            if (rows == null) return "(no data)";

            var materialized = rows.Where(r => r != null).ToList();
            if (materialized.Count == 0) return "(no rows)";

            var columns = new List<string>();
            foreach (var row in materialized)
            {
                foreach (var key in row.Keys)
                {
                    if (!columns.Contains(key, StringComparer.OrdinalIgnoreCase)) columns.Add(key);
                }
            }
            if (columns.Count == 0) return "(no columns)";

            var cells = materialized
                .Take(Math.Max(0, maxRows))
                .Select(row => columns
                    .Select(column => FormatCell(GetValue(row, column), maxColumnWidth))
                    .ToArray())
                .ToList();

            var widths = new int[columns.Count];
            for (int i = 0; i < columns.Count; i++)
            {
                widths[i] = Math.Min(maxColumnWidth, columns[i].Length);
                foreach (var row in cells)
                {
                    widths[i] = Math.Max(widths[i], Math.Min(maxColumnWidth, row[i].Length));
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine(BuildRow(columns.ToArray(), widths));
            sb.AppendLine(BuildSeparator(widths));
            foreach (var row in cells)
            {
                sb.AppendLine(BuildRow(row, widths));
            }

            if (materialized.Count > maxRows)
            {
                sb.AppendLine($"... and {materialized.Count - maxRows} more row(s)");
            }
            return sb.ToString().TrimEnd();
        }

        private static string BuildRow(string[] values, int[] widths)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) sb.Append(" | ");
                sb.Append(Pad(values[i] ?? string.Empty, widths[i]));
            }
            return sb.ToString();
        }

        private static string BuildSeparator(int[] widths)
            => string.Join("-+-", widths.Select(w => new string('-', Math.Max(1, w))));

        private static string Pad(string value, int width)
        {
            if (value.Length >= width) return value;
            return value + new string(' ', width - value.Length);
        }

        private static string FormatCell(object value, int maxWidth)
        {
            if (value == null || value is DBNull) return "NULL";
            var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            text = text.Replace("\r", " ").Replace("\n", " ");
            if (text.Length > maxWidth) text = text.Substring(0, Math.Max(0, maxWidth - 1)) + "…";
            return text;
        }

        private static object GetValue(IDictionary<string, object> row, string column)
        {
            if (row.TryGetValue(column, out var value)) return value;
            foreach (var kvp in row)
            {
                if (string.Equals(kvp.Key, column, StringComparison.OrdinalIgnoreCase)) return kvp.Value;
            }
            return null;
        }
    }
}
