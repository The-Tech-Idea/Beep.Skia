namespace Beep.Ski.Quantitative
{
    /// <summary>
    /// A quantitative indicator node (e.g., SMA), with one input series and one output series.
    /// </summary>
    public class IndicatorNode : QuantControl
    {
        public string Indicator { get; set; } = "SMA";
        public int Period { get; set; } = 20;

        public IndicatorNode()
        {
            Name = "Indicator";
            EnsurePortCounts(1, 1);
        }
    }
}
