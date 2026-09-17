using SkiaSharp;
using Beep.Skia.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Beep.Ski.Quantitative
{
    /// <summary>
    /// Data source node for loading market data from various sources.
    /// </summary>
    public class DataSourceNode : QuantControl
    {
        private string _provider = "Yahoo";
        public string Provider { get => _provider; set { if (_provider == value) return; _provider = value ?? ""; if (NodeProperties.TryGetValue("Provider", out var pi)) pi.ParameterCurrentValue = _provider; InvalidateVisual(); } }
        
        private string _symbol = "SPY";
        public string Symbol { get => _symbol; set { if (_symbol == value) return; _symbol = value ?? ""; if (NodeProperties.TryGetValue("Symbol", out var pi)) pi.ParameterCurrentValue = _symbol; InvalidateVisual(); } }
        
        private string _interval = "1D";
        public string Interval { get => _interval; set { if (_interval == value) return; _interval = value ?? ""; if (NodeProperties.TryGetValue("Interval", out var pi)) pi.ParameterCurrentValue = _interval; InvalidateVisual(); } }
        
        private DateTime _startDate = DateTime.Now.AddYears(-2);
        public DateTime StartDate { get => _startDate; set { if (_startDate == value) return; _startDate = value; if (NodeProperties.TryGetValue("StartDate", out var pi)) pi.ParameterCurrentValue = _startDate; InvalidateVisual(); } }

        public DataSourceNode()
        {
            Name = "Data Source";
            Width = 160;
            EnsurePortCounts(0, 4); // OHLCV outputs (Open, High, Low, Close, Volume)

            NodeProperties["Provider"] = new ParameterInfo { ParameterName = "Provider", ParameterType = typeof(string), DefaultParameterValue = _provider, ParameterCurrentValue = _provider, Description = "Data provider", Choices = new[] { "Yahoo", "AlphaVantage", "IEX", "Quandl", "CSV" } };
            NodeProperties["Symbol"] = new ParameterInfo { ParameterName = "Symbol", ParameterType = typeof(string), DefaultParameterValue = _symbol, ParameterCurrentValue = _symbol, Description = "Ticker symbol" };
            NodeProperties["Interval"] = new ParameterInfo { ParameterName = "Interval", ParameterType = typeof(string), DefaultParameterValue = _interval, ParameterCurrentValue = _interval, Description = "Time interval", Choices = new[] { "1m", "5m", "15m", "1h", "1D", "1W", "1M" } };
            NodeProperties["StartDate"] = new ParameterInfo { ParameterName = "StartDate", ParameterType = typeof(DateTime), DefaultParameterValue = _startDate, ParameterCurrentValue = _startDate, Description = "Start date for data" };
        }
    }

    /// <summary>
    /// Data transformation node for resampling, normalization, etc.
    /// </summary>
    public class TransformNode : QuantControl
    {
        private string _operation = "Returns";
        public string Operation { get => _operation; set { if (_operation == value) return; _operation = value ?? ""; if (NodeProperties.TryGetValue("Operation", out var pi)) pi.ParameterCurrentValue = _operation; InvalidateVisual(); } }
        
        private bool _percentage = true;
        public bool Percentage { get => _percentage; set { if (_percentage == value) return; _percentage = value; if (NodeProperties.TryGetValue("Percentage", out var pi)) pi.ParameterCurrentValue = _percentage; InvalidateVisual(); } }
        
        private int _lag = 1;
        public int Lag { get => _lag; set { if (_lag == value) return; _lag = value; if (NodeProperties.TryGetValue("Lag", out var pi)) pi.ParameterCurrentValue = _lag; InvalidateVisual(); } }

        public TransformNode()
        {
            Name = "Transform";
            Width = 150;
            EnsurePortCounts(1, 1);

            NodeProperties["Operation"] = new ParameterInfo { ParameterName = "Operation", ParameterType = typeof(string), DefaultParameterValue = _operation, ParameterCurrentValue = _operation, Description = "Transformation type", Choices = new[] { "Returns", "LogReturns", "Difference", "Normalize", "Standardize", "Resample" } };
            NodeProperties["Percentage"] = new ParameterInfo { ParameterName = "Percentage", ParameterType = typeof(bool), DefaultParameterValue = _percentage, ParameterCurrentValue = _percentage, Description = "Return as percentage" };
            NodeProperties["Lag"] = new ParameterInfo { ParameterName = "Lag", ParameterType = typeof(int), DefaultParameterValue = _lag, ParameterCurrentValue = _lag, Description = "Lag for differences/returns" };
        }
    }

    /// <summary>
    /// Filter node for removing noise or outliers.
    /// </summary>
    public class FilterNode : QuantControl
    {
        private string _filterType = "Outlier";
        public string FilterType { get => _filterType; set { if (_filterType == value) return; _filterType = value ?? ""; if (NodeProperties.TryGetValue("FilterType", out var pi)) pi.ParameterCurrentValue = _filterType; InvalidateVisual(); } }
        
        private double _threshold = 3.0;
        public double Threshold { get => _threshold; set { if (Math.Abs(_threshold - value) < 0.0001) return; _threshold = value; if (NodeProperties.TryGetValue("Threshold", out var pi)) pi.ParameterCurrentValue = _threshold; InvalidateVisual(); } }
        
        private string _method = "ZScore";
        public string Method { get => _method; set { if (_method == value) return; _method = value ?? ""; if (NodeProperties.TryGetValue("Method", out var pi)) pi.ParameterCurrentValue = _method; InvalidateVisual(); } }

        public FilterNode()
        {
            Name = "Filter";
            Width = 140;
            EnsurePortCounts(1, 1);

            NodeProperties["FilterType"] = new ParameterInfo { ParameterName = "FilterType", ParameterType = typeof(string), DefaultParameterValue = _filterType, ParameterCurrentValue = _filterType, Description = "Filter type", Choices = new[] { "Outlier", "Noise", "Spike", "Missing" } };
            NodeProperties["Threshold"] = new ParameterInfo { ParameterName = "Threshold", ParameterType = typeof(double), DefaultParameterValue = _threshold, ParameterCurrentValue = _threshold, Description = "Threshold for filtering" };
            NodeProperties["Method"] = new ParameterInfo { ParameterName = "Method", ParameterType = typeof(string), DefaultParameterValue = _method, ParameterCurrentValue = _method, Description = "Detection method", Choices = new[] { "ZScore", "IQR", "MAD", "Isolation" } };
        }
    }

    /// <summary>
    /// Aggregation node for combining multiple series.
    /// </summary>
    public class AggregateNode : QuantControl
    {
        private string _operation = "Mean";
        public string Operation { get => _operation; set { if (_operation == value) return; _operation = value ?? ""; if (NodeProperties.TryGetValue("Operation", out var pi)) pi.ParameterCurrentValue = _operation; InvalidateVisual(); } }
        
        private bool _skipNaN = true;
        public bool SkipNaN { get => _skipNaN; set { if (_skipNaN == value) return; _skipNaN = value; if (NodeProperties.TryGetValue("SkipNaN", out var pi)) pi.ParameterCurrentValue = _skipNaN; InvalidateVisual(); } }

        public AggregateNode()
        {
            Name = "Aggregate";
            Width = 150;
            EnsurePortCounts(2, 1); // Multiple inputs, one aggregated output

            NodeProperties["Operation"] = new ParameterInfo { ParameterName = "Operation", ParameterType = typeof(string), DefaultParameterValue = _operation, ParameterCurrentValue = _operation, Description = "Aggregation operation", Choices = new[] { "Mean", "Median", "Sum", "Min", "Max", "StdDev", "Variance" } };
            NodeProperties["SkipNaN"] = new ParameterInfo { ParameterName = "SkipNaN", ParameterType = typeof(bool), DefaultParameterValue = _skipNaN, ParameterCurrentValue = _skipNaN, Description = "Skip NaN values" };
        }
    }

    /// <summary>
    /// Chart/visualization node for displaying data.
    /// </summary>
    public class ChartNode : QuantControl
    {
        private string _chartType = "Line";
        public string ChartType { get => _chartType; set { if (_chartType == value) return; _chartType = value ?? ""; if (NodeProperties.TryGetValue("ChartType", out var pi)) pi.ParameterCurrentValue = _chartType; InvalidateVisual(); } }

        private bool _showGrid = true;
        public bool ShowGrid { get => _showGrid; set { if (_showGrid == value) return; _showGrid = value; if (NodeProperties.TryGetValue("ShowGrid", out var pi)) pi.ParameterCurrentValue = _showGrid; InvalidateVisual(); } }

        private bool _showLegend = true;
        public bool ShowLegend { get => _showLegend; set { if (_showLegend == value) return; _showLegend = value; if (NodeProperties.TryGetValue("ShowLegend", out var pi)) pi.ParameterCurrentValue = _showLegend; InvalidateVisual(); } }

        private int _maxPoints = 1000;
        public int MaxPoints { get => _maxPoints; set { if (_maxPoints == value) return; _maxPoints = Math.Max(1, value); if (NodeProperties.TryGetValue("MaxPoints", out var pi)) pi.ParameterCurrentValue = _maxPoints; InvalidateVisual(); } }

        private bool _showAxes = true;
        public bool ShowAxes { get => _showAxes; set { if (_showAxes == value) return; _showAxes = value; if (NodeProperties.TryGetValue("ShowAxes", out var pi)) pi.ParameterCurrentValue = _showAxes; InvalidateVisual(); } }

        private string _title = string.Empty;
        public string Title { get => _title; set { var v = value ?? ""; if (_title == v) return; _title = v; if (NodeProperties.TryGetValue("Title", out var pi)) pi.ParameterCurrentValue = _title; InvalidateVisual(); } }

        /// <summary>Data series rendered by this chart.</summary>
        public List<ChartSeries> Series { get; } = new List<ChartSeries>();

        /// <summary>
        /// JSON projection of <see cref="Series"/> used for persistence.
        /// </summary>
        public string SeriesJson
        {
            get => JsonSerializer.Serialize(Series);
            set
            {
                Series.Clear();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    try
                    {
                        var list = JsonSerializer.Deserialize<List<ChartSeries>>(value);
                        if (list != null) Series.AddRange(list);
                    }
                    catch { }
                }
                SyncSeriesMetadata();
                InvalidateVisual();
            }
        }

        public ChartNode()
        {
            Name = "Chart";
            Width = 260;
            Height = 170;
            EnsurePortCounts(3, 0); // Multiple data inputs, no outputs (visualization endpoint)

            NodeProperties["ChartType"] = new ParameterInfo { ParameterName = "ChartType", ParameterType = typeof(string), DefaultParameterValue = _chartType, ParameterCurrentValue = _chartType, Description = "Chart type", Choices = new[] { "Line", "Area", "Bar", "Scatter", "Histogram", "Candlestick", "Heatmap" } };
            NodeProperties["ShowGrid"] = new ParameterInfo { ParameterName = "ShowGrid", ParameterType = typeof(bool), DefaultParameterValue = _showGrid, ParameterCurrentValue = _showGrid, Description = "Show grid lines" };
            NodeProperties["ShowLegend"] = new ParameterInfo { ParameterName = "ShowLegend", ParameterType = typeof(bool), DefaultParameterValue = _showLegend, ParameterCurrentValue = _showLegend, Description = "Show legend" };
            NodeProperties["MaxPoints"] = new ParameterInfo { ParameterName = "MaxPoints", ParameterType = typeof(int), DefaultParameterValue = _maxPoints, ParameterCurrentValue = _maxPoints, Description = "Max points to display" };
            NodeProperties["ShowAxes"] = new ParameterInfo { ParameterName = "ShowAxes", ParameterType = typeof(bool), DefaultParameterValue = _showAxes, ParameterCurrentValue = _showAxes, Description = "Show value axis labels" };
            NodeProperties["Title"] = new ParameterInfo { ParameterName = "Title", ParameterType = typeof(string), DefaultParameterValue = _title, ParameterCurrentValue = _title, Description = "Chart title" };
            NodeProperties["SeriesJson"] = new ParameterInfo { ParameterName = "SeriesJson", ParameterType = typeof(string), DefaultParameterValue = "[]", ParameterCurrentValue = "[]", Description = "Persisted chart series (JSON)" };
        }

        /// <summary>Replaces all series data.</summary>
        public void SetData(params ChartSeries[] series)
        {
            Series.Clear();
            if (series != null) Series.AddRange(series.Where(s => s != null));
            SyncSeriesMetadata();
            InvalidateVisual();
        }

        /// <summary>Adds a series and returns it.</summary>
        public ChartSeries AddSeries(string name, params double[] values)
        {
            var series = new ChartSeries { Name = name ?? "Series" };
            if (values != null) series.Values.AddRange(values);
            ApplyDefaultColor(series, Series.Count);
            Series.Add(series);
            SyncSeriesMetadata();
            InvalidateVisual();
            return series;
        }

        /// <summary>Clears all series data.</summary>
        public void ClearData()
        {
            Series.Clear();
            SyncSeriesMetadata();
            InvalidateVisual();
        }

        private void SyncSeriesMetadata()
        {
            if (NodeProperties.TryGetValue("SeriesJson", out var pi))
                pi.ParameterCurrentValue = SeriesJson;
        }

        private void ApplyDefaultColor(ChartSeries series, int index)
        {
            if (series.ColorArgb != 0 && series.ColorArgb != 0xFF1E88E5) return;
            var palette = new[]
            {
                new SKColor(0x1E, 0x88, 0xE5), new SKColor(0xE5, 0x39, 0x35),
                new SKColor(0x43, 0xA0, 0x47), new SKColor(0xFB, 0x8C, 0x00),
                new SKColor(0x8E, 0x24, 0xAA), new SKColor(0x00, 0xAC, 0xC1)
            };
            series.ColorArgb = (uint)palette[index % palette.Length];
        }

        protected override void DrawQuantContent(SKCanvas canvas, DrawingContext context)
        {
            var bounds = new SKRect(X, Y, X + Width, Y + Height);
            using var bg = new SKPaint { Color = Fill, Style = SKPaintStyle.Fill, IsAntialias = true };
            canvas.DrawRoundRect(bounds, CornerRadius, CornerRadius, bg);
            using var border = new SKPaint { Color = Stroke, Style = SKPaintStyle.Stroke, StrokeWidth = StrokeWidth, IsAntialias = true };
            canvas.DrawRoundRect(bounds, CornerRadius, CornerRadius, border);

            using var titleFont = new SKFont(SKTypeface.Default, 11) { Embolden = true };
            using var labelFont = new SKFont(SKTypeface.Default, 8);
            using var textPaint = new SKPaint { Color = TextColor, IsAntialias = true };

            const float pad = 8f;
            float titleHeight = string.IsNullOrWhiteSpace(_title) ? 0f : 16f;
            float legendHeight = (_showLegend && Series.Count > 0) ? 14f : 0f;
            var plot = new SKRect(
                bounds.Left + pad + (_showAxes ? 28f : 4f),
                bounds.Top + pad + titleHeight,
                bounds.Right - pad,
                bounds.Bottom - pad - legendHeight - (_showAxes ? 10f : 0f));

            if (!string.IsNullOrWhiteSpace(_title))
                canvas.DrawText(_title, bounds.MidX, bounds.Top + 13f, SKTextAlign.Center, titleFont, textPaint);

            var visible = Series.Where(s => s.Values != null && s.Values.Count > 0).ToList();
            if (plot.Width < 12f || plot.Height < 12f || visible.Count == 0)
            {
                if (plot.Width >= 12f && plot.Height >= 12f && visible.Count == 0)
                    canvas.DrawText("(no data)", plot.MidX, plot.MidY, SKTextAlign.Center, labelFont, textPaint);
                DrawPorts(canvas);
                return;
            }

            List<double> Effective(ChartSeries series)
                => series.Values.Count <= _maxPoints
                    ? series.Values
                    : series.Values.Skip(series.Values.Count - _maxPoints).ToList();

            var values = visible.SelectMany(Effective).ToList();
            double min = values.Min();
            double max = values.Max();
            if (Math.Abs(max - min) < 1e-9) { min -= 1; max += 1; }

            float ValueY(double v) => plot.Bottom - (float)((v - min) / (max - min)) * plot.Height;
            float IndexX(int i, int count) => count <= 1 ? plot.MidX : plot.Left + (float)i / (count - 1) * plot.Width;
            float BaselineY() => ValueY(Math.Max(min, Math.Min(max, 0)));

            if (_showGrid)
            {
                using var grid = new SKPaint { Color = Stroke.WithAlpha(50), StrokeWidth = 0.7f, Style = SKPaintStyle.Stroke, IsAntialias = true };
                for (int i = 0; i <= 4; i++)
                {
                    float y = plot.Top + plot.Height * i / 4f;
                    canvas.DrawLine(plot.Left, y, plot.Right, y, grid);
                }
            }

            if (_showAxes)
            {
                for (int i = 0; i <= 4; i++)
                {
                    float y = plot.Top + plot.Height * i / 4f;
                    double value = max - (max - min) * i / 4.0;
                    canvas.DrawText(value.ToString("0.##"), plot.Left - 3f, y + 3f, SKTextAlign.Right, labelFont, textPaint);
                }
            }

            if (min < 0 && max > 0)
            {
                using var zero = new SKPaint { Color = Stroke, StrokeWidth = 0.8f, IsAntialias = true };
                canvas.DrawLine(plot.Left, BaselineY(), plot.Right, BaselineY(), zero);
            }

            switch ((_chartType ?? "Line").Trim().ToLowerInvariant())
            {
                case "bar":
                    DrawBars(canvas, visible, Effective, plot, ValueY, BaselineY);
                    break;
                case "histogram":
                    DrawHistogram(canvas, visible[0], Effective(visible[0]), plot, min, max, labelFont, textPaint);
                    break;
                case "scatter":
                    DrawScatter(canvas, visible, Effective, IndexX, ValueY);
                    break;
                case "area":
                    DrawLines(canvas, visible, Effective, IndexX, ValueY, plot, fill: true);
                    break;
                default:
                    DrawLines(canvas, visible, Effective, IndexX, ValueY, plot, fill: false);
                    break;
            }

            if (_showLegend)
            {
                float lx = plot.Left;
                float ly = bounds.Bottom - 5f;
                foreach (var series in visible)
                {
                    using var swatch = new SKPaint { Color = series.Color, Style = SKPaintStyle.Fill, IsAntialias = true };
                    canvas.DrawRect(new SKRect(lx, ly - 7f, lx + 8f, ly + 1f), swatch);
                    lx += 11f;
                    canvas.DrawText(series.Name ?? string.Empty, lx, ly, SKTextAlign.Left, labelFont, textPaint);
                    lx += labelFont.MeasureText(series.Name ?? string.Empty) + 10f;
                }
            }

            DrawPorts(canvas);
        }

        private static void DrawLines(
            SKCanvas canvas,
            List<ChartSeries> visible,
            Func<ChartSeries, List<double>> effective,
            Func<int, int, float> indexX,
            Func<double, float> valueY,
            SKRect plot,
            bool fill)
        {
            foreach (var series in visible)
            {
                var data = effective(series);
                if (data.Count == 0) continue;

                using var pathBuilder = new SKPathBuilder();
                for (int i = 0; i < data.Count; i++)
                {
                    float x = indexX(i, data.Count);
                    float y = valueY(data[i]);
                    if (i == 0) pathBuilder.MoveTo(x, y);
                    else pathBuilder.LineTo(x, y);
                }

                using var path = pathBuilder.Detach();
                if (fill)
                {
                    using var areaBuilder = new SKPathBuilder();
                    areaBuilder.AddPath(path);
                    areaBuilder.LineTo(indexX(data.Count - 1, data.Count), plot.Bottom);
                    areaBuilder.LineTo(indexX(0, data.Count), plot.Bottom);
                    areaBuilder.Close();
                    using var areaPath = areaBuilder.Detach();
                    using var areaPaint = new SKPaint { Color = series.Color.WithAlpha(60), Style = SKPaintStyle.Fill, IsAntialias = true };
                    canvas.DrawPath(areaPath, areaPaint);
                }

                using var linePaint = new SKPaint { Color = series.Color, Style = SKPaintStyle.Stroke, StrokeWidth = 1.6f, IsAntialias = true };
                canvas.DrawPath(path, linePaint);
            }
        }

        private static void DrawScatter(
            SKCanvas canvas,
            List<ChartSeries> visible,
            Func<ChartSeries, List<double>> effective,
            Func<int, int, float> indexX,
            Func<double, float> valueY)
        {
            foreach (var series in visible)
            {
                var data = effective(series);
                using var dot = new SKPaint { Color = series.Color, Style = SKPaintStyle.Fill, IsAntialias = true };
                for (int i = 0; i < data.Count; i++)
                {
                    canvas.DrawCircle(indexX(i, data.Count), valueY(data[i]), 2.5f, dot);
                }
            }
        }

        private static void DrawBars(
            SKCanvas canvas,
            List<ChartSeries> visible,
            Func<ChartSeries, List<double>> effective,
            SKRect plot,
            Func<double, float> valueY,
            Func<float> baselineY)
        {
            int seriesCount = Math.Max(1, visible.Count);
            int maxCount = visible.Max(s => effective(s).Count);
            float groupWidth = plot.Width / Math.Max(1, maxCount);
            float barWidth = Math.Max(1f, groupWidth * 0.8f / seriesCount);
            float baseline = baselineY();

            for (int s = 0; s < visible.Count; s++)
            {
                var data = effective(visible[s]);
                using var paint = new SKPaint { Color = visible[s].Color, Style = SKPaintStyle.Fill, IsAntialias = true };

                for (int i = 0; i < data.Count; i++)
                {
                    float groupLeft = plot.Left + i * groupWidth;
                    float x = groupLeft + groupWidth * 0.1f + s * barWidth;
                    float y = valueY(data[i]);
                    float top = Math.Min(y, baseline);
                    float bottom = Math.Max(y, baseline);
                    if (bottom - top < 1f) bottom = top + 1f;
                    canvas.DrawRect(new SKRect(x, top, x + barWidth, bottom), paint);
                }
            }
        }

        private static void DrawHistogram(
            SKCanvas canvas,
            ChartSeries series,
            List<double> data,
            SKRect plot,
            double min,
            double max,
            SKFont labelFont,
            SKPaint textPaint)
        {
            if (data.Count == 0) return;

            const int binCount = 10;
            var bins = new int[binCount];
            double range = Math.Max(1e-9, max - min);
            foreach (var value in data)
            {
                int bin = (int)((value - min) / range * binCount);
                if (bin >= binCount) bin = binCount - 1;
                if (bin < 0) bin = 0;
                bins[bin]++;
            }

            int maxBin = Math.Max(1, bins.Max());
            float binWidth = plot.Width / binCount;
            using var paint = new SKPaint { Color = series.Color, Style = SKPaintStyle.Fill, IsAntialias = true };

            for (int i = 0; i < binCount; i++)
            {
                float height = plot.Height * bins[i] / (float)maxBin;
                var bar = new SKRect(
                    plot.Left + i * binWidth + 1f,
                    plot.Bottom - height,
                    plot.Left + (i + 1) * binWidth - 1f,
                    plot.Bottom);
                canvas.DrawRect(bar, paint);
            }
        }
    }

    /// <summary>
    /// Export node for saving results to file or database.
    /// </summary>
    public class ExportNode : QuantControl
    {
        private string _format = "CSV";
        public string Format { get => _format; set { if (_format == value) return; _format = value ?? ""; if (NodeProperties.TryGetValue("Format", out var pi)) pi.ParameterCurrentValue = _format; InvalidateVisual(); } }
        
        private string _filePath = "output.csv";
        public string FilePath { get => _filePath; set { if (_filePath == value) return; _filePath = value ?? ""; if (NodeProperties.TryGetValue("FilePath", out var pi)) pi.ParameterCurrentValue = _filePath; InvalidateVisual(); } }
        
        private bool _includeHeader = true;
        public bool IncludeHeader { get => _includeHeader; set { if (_includeHeader == value) return; _includeHeader = value; if (NodeProperties.TryGetValue("IncludeHeader", out var pi)) pi.ParameterCurrentValue = _includeHeader; InvalidateVisual(); } }

        public ExportNode()
        {
            Name = "Export";
            Width = 140;
            EnsurePortCounts(1, 0); // Data input, no outputs (endpoint)

            NodeProperties["Format"] = new ParameterInfo { ParameterName = "Format", ParameterType = typeof(string), DefaultParameterValue = _format, ParameterCurrentValue = _format, Description = "Export format", Choices = new[] { "CSV", "JSON", "Excel", "Parquet", "HDF5" } };
            NodeProperties["FilePath"] = new ParameterInfo { ParameterName = "FilePath", ParameterType = typeof(string), DefaultParameterValue = _filePath, ParameterCurrentValue = _filePath, Description = "Output file path" };
            NodeProperties["IncludeHeader"] = new ParameterInfo { ParameterName = "IncludeHeader", ParameterType = typeof(bool), DefaultParameterValue = _includeHeader, ParameterCurrentValue = _includeHeader, Description = "Include header row" };
        }
    }
}