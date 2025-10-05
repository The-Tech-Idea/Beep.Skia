using SkiaSharp;
using Beep.Skia.Model;
using System;

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
        public int MaxPoints { get => _maxPoints; set { if (_maxPoints == value) return; _maxPoints = value; if (NodeProperties.TryGetValue("MaxPoints", out var pi)) pi.ParameterCurrentValue = _maxPoints; InvalidateVisual(); } }

        public ChartNode()
        {
            Name = "Chart";
            Width = 160;
            Height = 100;
            EnsurePortCounts(3, 0); // Multiple data inputs, no outputs (visualization endpoint)

            NodeProperties["ChartType"] = new ParameterInfo { ParameterName = "ChartType", ParameterType = typeof(string), DefaultParameterValue = _chartType, ParameterCurrentValue = _chartType, Description = "Chart type", Choices = new[] { "Line", "Candlestick", "Bar", "Scatter", "Heatmap", "Histogram" } };
            NodeProperties["ShowGrid"] = new ParameterInfo { ParameterName = "ShowGrid", ParameterType = typeof(bool), DefaultParameterValue = _showGrid, ParameterCurrentValue = _showGrid, Description = "Show grid lines" };
            NodeProperties["ShowLegend"] = new ParameterInfo { ParameterName = "ShowLegend", ParameterType = typeof(bool), DefaultParameterValue = _showLegend, ParameterCurrentValue = _showLegend, Description = "Show legend" };
            NodeProperties["MaxPoints"] = new ParameterInfo { ParameterName = "MaxPoints", ParameterType = typeof(int), DefaultParameterValue = _maxPoints, ParameterCurrentValue = _maxPoints, Description = "Max points to display" };
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
