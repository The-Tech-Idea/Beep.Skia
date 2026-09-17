using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Beep.Skia.ETL
{
    /// <summary>
    /// Statistical profile of a single column.
    /// </summary>
    public class ColumnProfile
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public int NullCount { get; set; }
        public int DistinctCount { get; set; }
        public object Min { get; set; }
        public object Max { get; set; }
        public double? Average { get; set; }
        public double? Sum { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public List<object> SampleValues { get; set; } = new List<object>();
        public bool IsNumeric { get; set; }
        public bool IsDateTime { get; set; }

        public double NullPercentage(int rowCount)
            => rowCount <= 0 ? 0d : Math.Round(NullCount * 100d / rowCount, 2);
    }

    /// <summary>
    /// Profile of a dataset: row count and per-column statistics.
    /// </summary>
    public class DataProfile
    {
        public int RowCount { get; set; }
        public List<ColumnProfile> Columns { get; set; } = new List<ColumnProfile>();

        /// <summary>
        /// Human-readable profile report.
        /// </summary>
        public string Summary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Rows: {RowCount}, Columns: {Columns.Count}");
            sb.AppendLine(new string('-', 72));
            sb.AppendLine($"{"Column",-20} {"Type",-9} {"Non-Null",8} {"Null%",7} {"Distinct",8} {"Min",-12} {"Max",-12}");
            sb.AppendLine(new string('-', 72));

            foreach (var column in Columns)
            {
                var type = column.IsNumeric ? "number" : column.IsDateTime ? "date" : "text";
                var min = FormatValue(column.Min);
                var max = FormatValue(column.Max);
                sb.AppendLine($"{Truncate(column.Name, 20),-20} {type,-9} {column.Count,8} {column.NullPercentage(RowCount),7:0.##} {column.DistinctCount,8} {Truncate(min, 12),-12} {Truncate(max, 12),-12}");
            }

            sb.AppendLine(new string('-', 72));
            foreach (var column in Columns.Where(c => c.IsNumeric && c.Average.HasValue))
            {
                sb.AppendLine($"  {column.Name}: avg={column.Average.Value.ToString("0.##", CultureInfo.InvariantCulture)}, sum={column.Sum?.ToString("0.##", CultureInfo.InvariantCulture)}");
            }
            foreach (var column in Columns.Where(c => c.MaxLength.HasValue && !c.IsNumeric))
            {
                sb.AppendLine($"  {column.Name}: length {column.MinLength}..{column.MaxLength}");
            }
            return sb.ToString().TrimEnd();
        }

        private static string FormatValue(object value)
            => value == null ? "(null)" : Convert.ToString(value, CultureInfo.InvariantCulture);

        private static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= max) return text ?? string.Empty;
            return text.Substring(0, Math.Max(0, max - 1)) + "…";
        }
    }

    /// <summary>
    /// Computes column-level statistics (counts, nulls, distinct, min/max, average, string lengths)
    /// over a set of rows represented as dictionaries.
    /// </summary>
    public class DataProfiler
    {
        /// <summary>
        /// Profiles the given rows. Column order follows first-seen order.
        /// </summary>
        public DataProfile Profile(IEnumerable<IDictionary<string, object>> rows, int sampleSize = 5)
        {
            var profile = new DataProfile();
            if (rows == null) return profile;

            var accumulators = new Dictionary<string, ColumnAccumulator>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();

            foreach (var row in rows)
            {
                if (row == null) continue;
                profile.RowCount++;

                foreach (var kvp in row)
                {
                    if (!accumulators.TryGetValue(kvp.Key, out var accumulator))
                    {
                        accumulator = new ColumnAccumulator(kvp.Key, sampleSize);
                        accumulators[kvp.Key] = accumulator;
                        order.Add(kvp.Key);
                    }
                    accumulator.Add(kvp.Value);
                }
            }

            foreach (var name in order)
            {
                profile.Columns.Add(accumulators[name].ToProfile());
            }
            return profile;
        }

        private sealed class ColumnAccumulator
        {
            private readonly string _name;
            private readonly int _sampleSize;
            private readonly HashSet<string> _distinct = new HashSet<string>(StringComparer.Ordinal);
            private readonly List<object> _samples = new List<object>();

            private int _count;
            private int _nullCount;
            private double _sum;
            private int _numericCount;
            private bool _sawNonNumeric;
            private bool _sawDate;
            private object _min;
            private object _max;
            private int? _minLength;
            private int? _maxLength;

            public ColumnAccumulator(string name, int sampleSize)
            {
                _name = name;
                _sampleSize = Math.Max(0, sampleSize);
            }

            public void Add(object value)
            {
                if (value == null || value is DBNull)
                {
                    _nullCount++;
                    return;
                }

                _count++;
                _distinct.Add(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
                if (_samples.Count < _sampleSize) _samples.Add(value);

                if (value is DateTime || value is DateTimeOffset)
                {
                    _sawDate = true;
                }
                else if (TryToDouble(value, out var number))
                {
                    _sum += number;
                    _numericCount++;
                    UpdateMinMax(value, number);
                }
                else
                {
                    _sawNonNumeric = true;
                    var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                    _minLength = _minLength.HasValue ? Math.Min(_minLength.Value, text.Length) : text.Length;
                    _maxLength = _maxLength.HasValue ? Math.Max(_maxLength.Value, text.Length) : text.Length;
                    UpdateMinMax(value, null);
                }
            }

            private void UpdateMinMax(object value, double? numeric)
            {
                if (_min == null)
                {
                    _min = value;
                    _max = value;
                    return;
                }

                if (numeric.HasValue && TryToDouble(_min, out var currentMin) && TryToDouble(_max, out var currentMax))
                {
                    if (numeric.Value < currentMin) _min = value;
                    if (numeric.Value > currentMax) _max = value;
                }
                else
                {
                    var comparison = string.Compare(
                        Convert.ToString(value, CultureInfo.InvariantCulture),
                        Convert.ToString(_min, CultureInfo.InvariantCulture),
                        StringComparison.OrdinalIgnoreCase);
                    if (comparison < 0) _min = value;
                    comparison = string.Compare(
                        Convert.ToString(value, CultureInfo.InvariantCulture),
                        Convert.ToString(_max, CultureInfo.InvariantCulture),
                        StringComparison.OrdinalIgnoreCase);
                    if (comparison > 0) _max = value;
                }
            }

            public ColumnProfile ToProfile()
            {
                bool isNumeric = _numericCount > 0 && !_sawNonNumeric && !_sawDate;
                return new ColumnProfile
                {
                    Name = _name,
                    Count = _count,
                    NullCount = _nullCount,
                    DistinctCount = _distinct.Count,
                    Min = _min,
                    Max = _max,
                    Average = isNumeric && _numericCount > 0 ? _sum / _numericCount : (double?)null,
                    Sum = isNumeric && _numericCount > 0 ? _sum : (double?)null,
                    MinLength = _sawNonNumeric ? _minLength : (int?)null,
                    MaxLength = _sawNonNumeric ? _maxLength : (int?)null,
                    SampleValues = _samples,
                    IsNumeric = isNumeric,
                    IsDateTime = _sawDate && !_sawNonNumeric
                };
            }

            private static bool TryToDouble(object value, out double result)
            {
                switch (value)
                {
                    case byte b: result = b; return true;
                    case sbyte sb: result = sb; return true;
                    case short s: result = s; return true;
                    case ushort us: result = us; return true;
                    case int i: result = i; return true;
                    case uint ui: result = ui; return true;
                    case long l: result = l; return true;
                    case ulong ul: result = ul; return true;
                    case float f: result = f; return true;
                    case double d: result = d; return true;
                    case decimal m: result = (double)m; return true;
                    default:
                        result = 0;
                        return false;
                }
            }
        }
    }
}
