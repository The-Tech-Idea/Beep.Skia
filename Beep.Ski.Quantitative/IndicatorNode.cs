using Beep.Skia;
using Beep.Skia.Model;

namespace Beep.Ski.Quantitative
{
    /// <summary>
    /// A quantitative indicator node (e.g., SMA), with one input series and one output series.
    /// </summary>
    public class IndicatorNode : QuantControl
    {
        private string _indicator = "SMA";
        public string Indicator { get => _indicator; set { if (_indicator == value) return; _indicator = value ?? ""; if (NodeProperties.TryGetValue("Indicator", out var pi)) pi.ParameterCurrentValue = _indicator; InvalidateVisual(); } }
        private int _period = 20;
        public int Period { get => _period; set { if (_period == value) return; _period = value; if (NodeProperties.TryGetValue("Period", out var pi)) pi.ParameterCurrentValue = _period; InvalidateVisual(); } }

        public IndicatorNode()
        {
            Name = "Indicator";
            EnsurePortCounts(1, 1);

            // Seed NodeProperties
            NodeProperties["Indicator"] = new ParameterInfo { ParameterName = "Indicator", ParameterType = typeof(string), DefaultParameterValue = _indicator, ParameterCurrentValue = _indicator, Description = "Indicator type (e.g., SMA, EMA)", Choices = new [] { "SMA", "EMA", "RSI", "MACD" } };
            NodeProperties["Period"] = new ParameterInfo { ParameterName = "Period", ParameterType = typeof(int), DefaultParameterValue = _period, ParameterCurrentValue = _period, Description = "Lookback period" };
        }
    }
}
