using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Ski.Quantitative
{
    /// <summary>
    /// Trading strategy node that generates buy/sell signals.
    /// </summary>
    public class StrategyNode : QuantControl
    {
        private string _strategyType = "Crossover";
        /// <summary>
        /// Gets or sets the strategy type.
        /// </summary>
        public string StrategyType { get => _strategyType; set { if (_strategyType == value) return; _strategyType = value ?? ""; if (NodeProperties.TryGetValue("StrategyType", out var pi)) pi.ParameterCurrentValue = _strategyType; InvalidateVisual(); } }
        
        private double _threshold = 0.0;
        /// <summary>
        /// Gets or sets the threshold.
        /// </summary>
        public double Threshold { get => _threshold; set { if (Math.Abs(_threshold - value) < 0.0001) return; _threshold = value; if (NodeProperties.TryGetValue("Threshold", out var pi)) pi.ParameterCurrentValue = _threshold; InvalidateVisual(); } }
        
        private bool _longOnly = false;
        /// <summary>
        /// Gets or sets the long only.
        /// </summary>
        public bool LongOnly { get => _longOnly; set { if (_longOnly == value) return; _longOnly = value; if (NodeProperties.TryGetValue("LongOnly", out var pi)) pi.ParameterCurrentValue = _longOnly; InvalidateVisual(); } }

        /// <summary>
        /// Strategy type
        /// </summary>
        public StrategyNode()
        {
            Name = "Strategy";
            EnsurePortCounts(2, 1); // 2 indicator inputs, 1 signal output

            NodeProperties["StrategyType"] = new ParameterInfo { ParameterName = "StrategyType", ParameterType = typeof(string), DefaultParameterValue = _strategyType, ParameterCurrentValue = _strategyType, Description = "Strategy type", Choices = new[] { "Crossover", "Threshold", "Divergence", "Breakout" } };
            NodeProperties["Threshold"] = new ParameterInfo { ParameterName = "Threshold", ParameterType = typeof(double), DefaultParameterValue = _threshold, ParameterCurrentValue = _threshold, Description = "Signal threshold" };
            NodeProperties["LongOnly"] = new ParameterInfo { ParameterName = "LongOnly", ParameterType = typeof(bool), DefaultParameterValue = _longOnly, ParameterCurrentValue = _longOnly, Description = "Long positions only" };
        }
    }

    /// <summary>
    /// Backtesting engine node to test strategies on historical data.
    /// </summary>
    public class BacktestNode : QuantControl
    {
        private DateTime _startDate = DateTime.Now.AddYears(-1);
        /// <summary>
        /// Gets or sets the start date.
        /// </summary>
        public DateTime StartDate { get => _startDate; set { if (_startDate == value) return; _startDate = value; if (NodeProperties.TryGetValue("StartDate", out var pi)) pi.ParameterCurrentValue = _startDate; InvalidateVisual(); } }
        
        private DateTime _endDate = DateTime.Now;
        /// <summary>
        /// Gets or sets the end date.
        /// </summary>
        public DateTime EndDate { get => _endDate; set { if (_endDate == value) return; _endDate = value; if (NodeProperties.TryGetValue("EndDate", out var pi)) pi.ParameterCurrentValue = _endDate; InvalidateVisual(); } }
        
        private double _initialCapital = 10000.0;
        /// <summary>
        /// Gets or sets the initial capital.
        /// </summary>
        public double InitialCapital { get => _initialCapital; set { if (Math.Abs(_initialCapital - value) < 0.01) return; _initialCapital = value; if (NodeProperties.TryGetValue("InitialCapital", out var pi)) pi.ParameterCurrentValue = _initialCapital; InvalidateVisual(); } }
        
        private double _commission = 0.001;
        /// <summary>
        /// Gets or sets the commission.
        /// </summary>
        public double Commission { get => _commission; set { if (Math.Abs(_commission - value) < 0.0001) return; _commission = value; if (NodeProperties.TryGetValue("Commission", out var pi)) pi.ParameterCurrentValue = _commission; InvalidateVisual(); } }

        /// <summary>
        /// Backtest start date
        /// </summary>
        public BacktestNode()
        {
            Name = "Backtest";
            Width = 160;
            EnsurePortCounts(2, 3); // Price & Signals inputs, Equity/Trades/Stats outputs

            NodeProperties["StartDate"] = new ParameterInfo { ParameterName = "StartDate", ParameterType = typeof(DateTime), DefaultParameterValue = _startDate, ParameterCurrentValue = _startDate, Description = "Backtest start date" };
            NodeProperties["EndDate"] = new ParameterInfo { ParameterName = "EndDate", ParameterType = typeof(DateTime), DefaultParameterValue = _endDate, ParameterCurrentValue = _endDate, Description = "Backtest end date" };
            NodeProperties["InitialCapital"] = new ParameterInfo { ParameterName = "InitialCapital", ParameterType = typeof(double), DefaultParameterValue = _initialCapital, ParameterCurrentValue = _initialCapital, Description = "Starting capital" };
            NodeProperties["Commission"] = new ParameterInfo { ParameterName = "Commission", ParameterType = typeof(double), DefaultParameterValue = _commission, ParameterCurrentValue = _commission, Description = "Commission rate" };
        }
    }

    /// <summary>
    /// Portfolio optimization node using Modern Portfolio Theory.
    /// </summary>
    public class PortfolioOptimizerNode : QuantControl
    {
        private string _method = "Sharpe";
        /// <summary>
        /// Gets or sets the method.
        /// </summary>
        public string Method { get => _method; set { if (_method == value) return; _method = value ?? ""; if (NodeProperties.TryGetValue("Method", out var pi)) pi.ParameterCurrentValue = _method; InvalidateVisual(); } }
        
        private double _targetReturn = 0.15;
        /// <summary>
        /// Gets or sets the target return.
        /// </summary>
        public double TargetReturn { get => _targetReturn; set { if (Math.Abs(_targetReturn - value) < 0.0001) return; _targetReturn = value; if (NodeProperties.TryGetValue("TargetReturn", out var pi)) pi.ParameterCurrentValue = _targetReturn; InvalidateVisual(); } }
        
        private int _numAssets = 5;
        /// <summary>
        /// Gets or sets the num assets.
        /// </summary>
        public int NumAssets { get => _numAssets; set { if (_numAssets == value) return; _numAssets = value; if (NodeProperties.TryGetValue("NumAssets", out var pi)) pi.ParameterCurrentValue = _numAssets; InvalidateVisual(); } }

        /// <summary>
        /// Optimization method
        /// </summary>
        public PortfolioOptimizerNode()
        {
            Name = "Portfolio Optimizer";
            Width = 180;
            EnsurePortCounts(1, 2); // Returns input, Weights/Stats outputs

            NodeProperties["Method"] = new ParameterInfo { ParameterName = "Method", ParameterType = typeof(string), DefaultParameterValue = _method, ParameterCurrentValue = _method, Description = "Optimization method", Choices = new[] { "Sharpe", "MinVariance", "MaxReturn", "RiskParity" } };
            NodeProperties["TargetReturn"] = new ParameterInfo { ParameterName = "TargetReturn", ParameterType = typeof(double), DefaultParameterValue = _targetReturn, ParameterCurrentValue = _targetReturn, Description = "Target annual return" };
            NodeProperties["NumAssets"] = new ParameterInfo { ParameterName = "NumAssets", ParameterType = typeof(int), DefaultParameterValue = _numAssets, ParameterCurrentValue = _numAssets, Description = "Number of assets" };
        }
    }

    /// <summary>
    /// Risk management node for position sizing and stop-loss.
    /// </summary>
    public class RiskManagerNode : QuantControl
    {
        private double _maxRiskPerTrade = 0.02;
        /// <summary>
        /// Gets or sets the max risk per trade.
        /// </summary>
        public double MaxRiskPerTrade { get => _maxRiskPerTrade; set { if (Math.Abs(_maxRiskPerTrade - value) < 0.0001) return; _maxRiskPerTrade = value; if (NodeProperties.TryGetValue("MaxRiskPerTrade", out var pi)) pi.ParameterCurrentValue = _maxRiskPerTrade; InvalidateVisual(); } }
        
        private double _stopLossPercent = 0.05;
        /// <summary>
        /// Gets or sets the stop loss percent.
        /// </summary>
        public double StopLossPercent { get => _stopLossPercent; set { if (Math.Abs(_stopLossPercent - value) < 0.0001) return; _stopLossPercent = value; if (NodeProperties.TryGetValue("StopLossPercent", out var pi)) pi.ParameterCurrentValue = _stopLossPercent; InvalidateVisual(); } }
        
        private double _takeProfitPercent = 0.10;
        /// <summary>
        /// Gets or sets the take profit percent.
        /// </summary>
        public double TakeProfitPercent { get => _takeProfitPercent; set { if (Math.Abs(_takeProfitPercent - value) < 0.0001) return; _takeProfitPercent = value; if (NodeProperties.TryGetValue("TakeProfitPercent", out var pi)) pi.ParameterCurrentValue = _takeProfitPercent; InvalidateVisual(); } }
        
        private string _positionSizing = "FixedRisk";
        /// <summary>
        /// Gets or sets the position sizing.
        /// </summary>
        public string PositionSizing { get => _positionSizing; set { if (_positionSizing == value) return; _positionSizing = value ?? ""; if (NodeProperties.TryGetValue("PositionSizing", out var pi)) pi.ParameterCurrentValue = _positionSizing; InvalidateVisual(); } }

        /// <summary>
        /// Max risk per trade (fraction)
        /// </summary>
        public RiskManagerNode()
        {
            Name = "Risk Manager";
            Width = 160;
            EnsurePortCounts(2, 2); // Price & Signals inputs, Sized positions & Risk metrics outputs

            NodeProperties["MaxRiskPerTrade"] = new ParameterInfo { ParameterName = "MaxRiskPerTrade", ParameterType = typeof(double), DefaultParameterValue = _maxRiskPerTrade, ParameterCurrentValue = _maxRiskPerTrade, Description = "Max risk per trade (fraction)" };
            NodeProperties["StopLossPercent"] = new ParameterInfo { ParameterName = "StopLossPercent", ParameterType = typeof(double), DefaultParameterValue = _stopLossPercent, ParameterCurrentValue = _stopLossPercent, Description = "Stop loss percentage" };
            NodeProperties["TakeProfitPercent"] = new ParameterInfo { ParameterName = "TakeProfitPercent", ParameterType = typeof(double), DefaultParameterValue = _takeProfitPercent, ParameterCurrentValue = _takeProfitPercent, Description = "Take profit percentage" };
            NodeProperties["PositionSizing"] = new ParameterInfo { ParameterName = "PositionSizing", ParameterType = typeof(string), DefaultParameterValue = _positionSizing, ParameterCurrentValue = _positionSizing, Description = "Position sizing method", Choices = new[] { "FixedRisk", "KellyFraction", "FixedFractional", "VolatilityAdjusted" } };
        }
    }

    /// <summary>
    /// Performance metrics calculator node.
    /// </summary>
    public class PerformanceNode : QuantControl
    {
        private bool _includeDrawdown = true;
        /// <summary>
        /// Gets or sets the include drawdown.
        /// </summary>
        public bool IncludeDrawdown { get => _includeDrawdown; set { if (_includeDrawdown == value) return; _includeDrawdown = value; if (NodeProperties.TryGetValue("IncludeDrawdown", out var pi)) pi.ParameterCurrentValue = _includeDrawdown; InvalidateVisual(); } }
        
        private bool _includeSharpe = true;
        /// <summary>
        /// Gets or sets the include sharpe.
        /// </summary>
        public bool IncludeSharpe { get => _includeSharpe; set { if (_includeSharpe == value) return; _includeSharpe = value; if (NodeProperties.TryGetValue("IncludeSharpe", out var pi)) pi.ParameterCurrentValue = _includeSharpe; InvalidateVisual(); } }
        
        private double _riskFreeRate = 0.02;
        /// <summary>
        /// Gets or sets the risk free rate.
        /// </summary>
        public double RiskFreeRate { get => _riskFreeRate; set { if (Math.Abs(_riskFreeRate - value) < 0.0001) return; _riskFreeRate = value; if (NodeProperties.TryGetValue("RiskFreeRate", out var pi)) pi.ParameterCurrentValue = _riskFreeRate; InvalidateVisual(); } }

        /// <summary>
        /// Calculate max drawdown
        /// </summary>
        public PerformanceNode()
        {
            Name = "Performance";
            Width = 160;
            EnsurePortCounts(1, 1); // Equity curve input, Metrics output

            NodeProperties["IncludeDrawdown"] = new ParameterInfo { ParameterName = "IncludeDrawdown", ParameterType = typeof(bool), DefaultParameterValue = _includeDrawdown, ParameterCurrentValue = _includeDrawdown, Description = "Calculate max drawdown" };
            NodeProperties["IncludeSharpe"] = new ParameterInfo { ParameterName = "IncludeSharpe", ParameterType = typeof(bool), DefaultParameterValue = _includeSharpe, ParameterCurrentValue = _includeSharpe, Description = "Calculate Sharpe ratio" };
            NodeProperties["RiskFreeRate"] = new ParameterInfo { ParameterName = "RiskFreeRate", ParameterType = typeof(double), DefaultParameterValue = _riskFreeRate, ParameterCurrentValue = _riskFreeRate, Description = "Risk-free rate (annual)" };
        }
    }
}
