using SkiaSharp;

namespace Beep.Ski.Quantitative
{
    /// <summary>
    /// A quantitative component representing an input time series (e.g., price data).
    /// </summary>
    public class TimeSeriesNode : QuantControl
    {
        public int Length { get; set; } = 1000;
        public string Symbol { get; set; } = "EURUSD";

        public TimeSeriesNode()
        {
            Name = "Time Series";
            EnsurePortCounts(0, 1); // produces one series
        }
    }
}
