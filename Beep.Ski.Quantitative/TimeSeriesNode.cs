using SkiaSharp;
using Beep.Skia.Model;

namespace Beep.Ski.Quantitative
{
    /// <summary>
    /// A quantitative component representing an input time series (e.g., price data).
    /// </summary>
    public class TimeSeriesNode : QuantControl
    {
        private int _length = 1000;
        public int Length { get => _length; set { if (_length == value) return; _length = value; if (NodeProperties.TryGetValue("Length", out var pi)) pi.ParameterCurrentValue = _length; InvalidateVisual(); } }
        private string _symbol = "EURUSD";
        public string Symbol { get => _symbol; set { if (_symbol == value) return; _symbol = value ?? string.Empty; if (NodeProperties.TryGetValue("Symbol", out var pi)) pi.ParameterCurrentValue = _symbol; InvalidateVisual(); } }

        public TimeSeriesNode()
        {
            Name = "Time Series";
            EnsurePortCounts(0, 1); // produces one series

            // Seed NodeProperties
            NodeProperties["Length"] = new ParameterInfo { ParameterName = "Length", ParameterType = typeof(int), DefaultParameterValue = _length, ParameterCurrentValue = _length, Description = "Number of data points" };
            NodeProperties["Symbol"] = new ParameterInfo { ParameterName = "Symbol", ParameterType = typeof(string), DefaultParameterValue = _symbol, ParameterCurrentValue = _symbol, Description = "Instrument symbol" };
        }
    }
}
