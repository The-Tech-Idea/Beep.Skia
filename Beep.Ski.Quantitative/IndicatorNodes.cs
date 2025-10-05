using SkiaSharp;
using Beep.Skia.Model;

namespace Beep.Ski.Quantitative
{
    /// <summary>
    /// Moving Average Convergence Divergence (MACD) indicator with signal line.
    /// Inputs: Price series
    /// Outputs: MACD line, Signal line, Histogram
    /// </summary>
    public class MACDNode : QuantControl
    {
        private int _fastPeriod = 12;
        public int FastPeriod { get => _fastPeriod; set { if (_fastPeriod == value) return; _fastPeriod = value; if (NodeProperties.TryGetValue("FastPeriod", out var pi)) pi.ParameterCurrentValue = _fastPeriod; InvalidateVisual(); } }
        
        private int _slowPeriod = 26;
        public int SlowPeriod { get => _slowPeriod; set { if (_slowPeriod == value) return; _slowPeriod = value; if (NodeProperties.TryGetValue("SlowPeriod", out var pi)) pi.ParameterCurrentValue = _slowPeriod; InvalidateVisual(); } }
        
        private int _signalPeriod = 9;
        public int SignalPeriod { get => _signalPeriod; set { if (_signalPeriod == value) return; _signalPeriod = value; if (NodeProperties.TryGetValue("SignalPeriod", out var pi)) pi.ParameterCurrentValue = _signalPeriod; InvalidateVisual(); } }

        public MACDNode()
        {
            Name = "MACD";
            EnsurePortCounts(1, 3); // 1 input (price), 3 outputs (MACD, Signal, Histogram)

            NodeProperties["FastPeriod"] = new ParameterInfo { ParameterName = "FastPeriod", ParameterType = typeof(int), DefaultParameterValue = _fastPeriod, ParameterCurrentValue = _fastPeriod, Description = "Fast EMA period" };
            NodeProperties["SlowPeriod"] = new ParameterInfo { ParameterName = "SlowPeriod", ParameterType = typeof(int), DefaultParameterValue = _slowPeriod, ParameterCurrentValue = _slowPeriod, Description = "Slow EMA period" };
            NodeProperties["SignalPeriod"] = new ParameterInfo { ParameterName = "SignalPeriod", ParameterType = typeof(int), DefaultParameterValue = _signalPeriod, ParameterCurrentValue = _signalPeriod, Description = "Signal line period" };
        }
    }

    /// <summary>
    /// Bollinger Bands indicator with upper, middle, and lower bands.
    /// </summary>
    public class BollingerBandsNode : QuantControl
    {
        private int _period = 20;
        public int Period { get => _period; set { if (_period == value) return; _period = value; if (NodeProperties.TryGetValue("Period", out var pi)) pi.ParameterCurrentValue = _period; InvalidateVisual(); } }
        
        private double _stdDev = 2.0;
        public double StdDev { get => _stdDev; set { if (Math.Abs(_stdDev - value) < 0.0001) return; _stdDev = value; if (NodeProperties.TryGetValue("StdDev", out var pi)) pi.ParameterCurrentValue = _stdDev; InvalidateVisual(); } }

        public BollingerBandsNode()
        {
            Name = "Bollinger Bands";
            EnsurePortCounts(1, 3); // 1 input, 3 outputs (Upper, Middle, Lower)

            NodeProperties["Period"] = new ParameterInfo { ParameterName = "Period", ParameterType = typeof(int), DefaultParameterValue = _period, ParameterCurrentValue = _period, Description = "Moving average period" };
            NodeProperties["StdDev"] = new ParameterInfo { ParameterName = "StdDev", ParameterType = typeof(double), DefaultParameterValue = _stdDev, ParameterCurrentValue = _stdDev, Description = "Standard deviation multiplier" };
        }
    }

    /// <summary>
    /// Relative Strength Index (RSI) oscillator.
    /// </summary>
    public class RSINode : QuantControl
    {
        private int _period = 14;
        public int Period { get => _period; set { if (_period == value) return; _period = value; if (NodeProperties.TryGetValue("Period", out var pi)) pi.ParameterCurrentValue = _period; InvalidateVisual(); } }
        
        private double _overbought = 70.0;
        public double Overbought { get => _overbought; set { if (Math.Abs(_overbought - value) < 0.0001) return; _overbought = value; if (NodeProperties.TryGetValue("Overbought", out var pi)) pi.ParameterCurrentValue = _overbought; InvalidateVisual(); } }
        
        private double _oversold = 30.0;
        public double Oversold { get => _oversold; set { if (Math.Abs(_oversold - value) < 0.0001) return; _oversold = value; if (NodeProperties.TryGetValue("Oversold", out var pi)) pi.ParameterCurrentValue = _oversold; InvalidateVisual(); } }

        public RSINode()
        {
            Name = "RSI";
            EnsurePortCounts(1, 1);

            NodeProperties["Period"] = new ParameterInfo { ParameterName = "Period", ParameterType = typeof(int), DefaultParameterValue = _period, ParameterCurrentValue = _period, Description = "RSI period" };
            NodeProperties["Overbought"] = new ParameterInfo { ParameterName = "Overbought", ParameterType = typeof(double), DefaultParameterValue = _overbought, ParameterCurrentValue = _overbought, Description = "Overbought threshold" };
            NodeProperties["Oversold"] = new ParameterInfo { ParameterName = "Oversold", ParameterType = typeof(double), DefaultParameterValue = _oversold, ParameterCurrentValue = _oversold, Description = "Oversold threshold" };
        }
    }

    /// <summary>
    /// Stochastic Oscillator with %K and %D lines.
    /// </summary>
    public class StochasticNode : QuantControl
    {
        private int _kPeriod = 14;
        public int KPeriod { get => _kPeriod; set { if (_kPeriod == value) return; _kPeriod = value; if (NodeProperties.TryGetValue("KPeriod", out var pi)) pi.ParameterCurrentValue = _kPeriod; InvalidateVisual(); } }
        
        private int _dPeriod = 3;
        public int DPeriod { get => _dPeriod; set { if (_dPeriod == value) return; _dPeriod = value; if (NodeProperties.TryGetValue("DPeriod", out var pi)) pi.ParameterCurrentValue = _dPeriod; InvalidateVisual(); } }
        
        private int _smooth = 3;
        public int Smooth { get => _smooth; set { if (_smooth == value) return; _smooth = value; if (NodeProperties.TryGetValue("Smooth", out var pi)) pi.ParameterCurrentValue = _smooth; InvalidateVisual(); } }

        public StochasticNode()
        {
            Name = "Stochastic";
            EnsurePortCounts(1, 2); // High/Low/Close input, %K and %D outputs

            NodeProperties["KPeriod"] = new ParameterInfo { ParameterName = "KPeriod", ParameterType = typeof(int), DefaultParameterValue = _kPeriod, ParameterCurrentValue = _kPeriod, Description = "%K period" };
            NodeProperties["DPeriod"] = new ParameterInfo { ParameterName = "DPeriod", ParameterType = typeof(int), DefaultParameterValue = _dPeriod, ParameterCurrentValue = _dPeriod, Description = "%D period" };
            NodeProperties["Smooth"] = new ParameterInfo { ParameterName = "Smooth", ParameterType = typeof(int), DefaultParameterValue = _smooth, ParameterCurrentValue = _smooth, Description = "Smoothing period" };
        }
    }

    /// <summary>
    /// Average True Range (ATR) volatility indicator.
    /// </summary>
    public class ATRNode : QuantControl
    {
        private int _period = 14;
        public int Period { get => _period; set { if (_period == value) return; _period = value; if (NodeProperties.TryGetValue("Period", out var pi)) pi.ParameterCurrentValue = _period; InvalidateVisual(); } }

        public ATRNode()
        {
            Name = "ATR";
            EnsurePortCounts(1, 1); // High/Low/Close input, ATR output

            NodeProperties["Period"] = new ParameterInfo { ParameterName = "Period", ParameterType = typeof(int), DefaultParameterValue = _period, ParameterCurrentValue = _period, Description = "ATR period" };
        }
    }
}
