using SkiaSharp;
using Beep.Skia.Model;
using System;

namespace Beep.Ski.Quantitative
{
    /// <summary>
    /// Statistical distribution analysis node.
    /// </summary>
    public class DistributionNode : QuantControl
    {
        private string _distributionType = "Normal";
        public string DistributionType { get => _distributionType; set { if (_distributionType == value) return; _distributionType = value ?? ""; if (NodeProperties.TryGetValue("DistributionType", out var pi)) pi.ParameterCurrentValue = _distributionType; InvalidateVisual(); } }
        
        private int _bins = 50;
        public int Bins { get => _bins; set { if (_bins == value) return; _bins = value; if (NodeProperties.TryGetValue("Bins", out var pi)) pi.ParameterCurrentValue = _bins; InvalidateVisual(); } }
        
        private bool _showKDE = true;
        public bool ShowKDE { get => _showKDE; set { if (_showKDE == value) return; _showKDE = value; if (NodeProperties.TryGetValue("ShowKDE", out var pi)) pi.ParameterCurrentValue = _showKDE; InvalidateVisual(); } }

        public DistributionNode()
        {
            Name = "Distribution";
            Width = 160;
            EnsurePortCounts(1, 2); // Data input, Histogram & Statistics outputs

            NodeProperties["DistributionType"] = new ParameterInfo { ParameterName = "DistributionType", ParameterType = typeof(string), DefaultParameterValue = _distributionType, ParameterCurrentValue = _distributionType, Description = "Distribution type", Choices = new[] { "Normal", "LogNormal", "Exponential", "Uniform", "StudentT" } };
            NodeProperties["Bins"] = new ParameterInfo { ParameterName = "Bins", ParameterType = typeof(int), DefaultParameterValue = _bins, ParameterCurrentValue = _bins, Description = "Number of histogram bins" };
            NodeProperties["ShowKDE"] = new ParameterInfo { ParameterName = "ShowKDE", ParameterType = typeof(bool), DefaultParameterValue = _showKDE, ParameterCurrentValue = _showKDE, Description = "Show kernel density estimate" };
        }
    }

    /// <summary>
    /// Correlation matrix calculator node.
    /// </summary>
    public class CorrelationNode : QuantControl
    {
        private string _method = "Pearson";
        public string Method { get => _method; set { if (_method == value) return; _method = value ?? ""; if (NodeProperties.TryGetValue("Method", out var pi)) pi.ParameterCurrentValue = _method; InvalidateVisual(); } }
        
        private int _window = 252;
        public int Window { get => _window; set { if (_window == value) return; _window = value; if (NodeProperties.TryGetValue("Window", out var pi)) pi.ParameterCurrentValue = _window; InvalidateVisual(); } }
        
        private bool _rolling = false;
        public bool Rolling { get => _rolling; set { if (_rolling == value) return; _rolling = value; if (NodeProperties.TryGetValue("Rolling", out var pi)) pi.ParameterCurrentValue = _rolling; InvalidateVisual(); } }

        public CorrelationNode()
        {
            Name = "Correlation";
            Width = 160;
            EnsurePortCounts(2, 1); // Multiple series input, Correlation matrix output

            NodeProperties["Method"] = new ParameterInfo { ParameterName = "Method", ParameterType = typeof(string), DefaultParameterValue = _method, ParameterCurrentValue = _method, Description = "Correlation method", Choices = new[] { "Pearson", "Spearman", "Kendall" } };
            NodeProperties["Window"] = new ParameterInfo { ParameterName = "Window", ParameterType = typeof(int), DefaultParameterValue = _window, ParameterCurrentValue = _window, Description = "Rolling window size" };
            NodeProperties["Rolling"] = new ParameterInfo { ParameterName = "Rolling", ParameterType = typeof(bool), DefaultParameterValue = _rolling, ParameterCurrentValue = _rolling, Description = "Use rolling correlation" };
        }
    }

    /// <summary>
    /// Regression analysis node for linear/polynomial fitting.
    /// </summary>
    public class RegressionNode : QuantControl
    {
        private string _regressionType = "Linear";
        public string RegressionType { get => _regressionType; set { if (_regressionType == value) return; _regressionType = value ?? ""; if (NodeProperties.TryGetValue("RegressionType", out var pi)) pi.ParameterCurrentValue = _regressionType; InvalidateVisual(); } }
        
        private int _degree = 1;
        public int Degree { get => _degree; set { if (_degree == value) return; _degree = value; if (NodeProperties.TryGetValue("Degree", out var pi)) pi.ParameterCurrentValue = _degree; InvalidateVisual(); } }
        
        private bool _includeIntercept = true;
        public bool IncludeIntercept { get => _includeIntercept; set { if (_includeIntercept == value) return; _includeIntercept = value; if (NodeProperties.TryGetValue("IncludeIntercept", out var pi)) pi.ParameterCurrentValue = _includeIntercept; InvalidateVisual(); } }

        public RegressionNode()
        {
            Name = "Regression";
            Width = 160;
            EnsurePortCounts(2, 2); // X & Y inputs, Fitted values & Coefficients outputs

            NodeProperties["RegressionType"] = new ParameterInfo { ParameterName = "RegressionType", ParameterType = typeof(string), DefaultParameterValue = _regressionType, ParameterCurrentValue = _regressionType, Description = "Regression type", Choices = new[] { "Linear", "Polynomial", "Ridge", "Lasso", "Logistic" } };
            NodeProperties["Degree"] = new ParameterInfo { ParameterName = "Degree", ParameterType = typeof(int), DefaultParameterValue = _degree, ParameterCurrentValue = _degree, Description = "Polynomial degree" };
            NodeProperties["IncludeIntercept"] = new ParameterInfo { ParameterName = "IncludeIntercept", ParameterType = typeof(bool), DefaultParameterValue = _includeIntercept, ParameterCurrentValue = _includeIntercept, Description = "Include intercept term" };
        }
    }

    /// <summary>
    /// Time series forecasting node using ARIMA/Prophet models.
    /// </summary>
    public class ForecastNode : QuantControl
    {
        private string _model = "ARIMA";
        public string Model { get => _model; set { if (_model == value) return; _model = value ?? ""; if (NodeProperties.TryGetValue("Model", out var pi)) pi.ParameterCurrentValue = _model; InvalidateVisual(); } }
        
        private int _horizonDays = 30;
        public int HorizonDays { get => _horizonDays; set { if (_horizonDays == value) return; _horizonDays = value; if (NodeProperties.TryGetValue("HorizonDays", out var pi)) pi.ParameterCurrentValue = _horizonDays; InvalidateVisual(); } }
        
        private double _confidenceLevel = 0.95;
        public double ConfidenceLevel { get => _confidenceLevel; set { if (Math.Abs(_confidenceLevel - value) < 0.0001) return; _confidenceLevel = value; if (NodeProperties.TryGetValue("ConfidenceLevel", out var pi)) pi.ParameterCurrentValue = _confidenceLevel; InvalidateVisual(); } }
        
        private bool _includeSeasonality = true;
        public bool IncludeSeasonality { get => _includeSeasonality; set { if (_includeSeasonality == value) return; _includeSeasonality = value; if (NodeProperties.TryGetValue("IncludeSeasonality", out var pi)) pi.ParameterCurrentValue = _includeSeasonality; InvalidateVisual(); } }

        public ForecastNode()
        {
            Name = "Forecast";
            Width = 160;
            EnsurePortCounts(1, 3); // Historical data input, Forecast/Upper/Lower bounds outputs

            NodeProperties["Model"] = new ParameterInfo { ParameterName = "Model", ParameterType = typeof(string), DefaultParameterValue = _model, ParameterCurrentValue = _model, Description = "Forecasting model", Choices = new[] { "ARIMA", "Prophet", "ETS", "LSTM", "XGBoost" } };
            NodeProperties["HorizonDays"] = new ParameterInfo { ParameterName = "HorizonDays", ParameterType = typeof(int), DefaultParameterValue = _horizonDays, ParameterCurrentValue = _horizonDays, Description = "Forecast horizon (days)" };
            NodeProperties["ConfidenceLevel"] = new ParameterInfo { ParameterName = "ConfidenceLevel", ParameterType = typeof(double), DefaultParameterValue = _confidenceLevel, ParameterCurrentValue = _confidenceLevel, Description = "Confidence interval level" };
            NodeProperties["IncludeSeasonality"] = new ParameterInfo { ParameterName = "IncludeSeasonality", ParameterType = typeof(bool), DefaultParameterValue = _includeSeasonality, ParameterCurrentValue = _includeSeasonality, Description = "Include seasonal components" };
        }
    }

    /// <summary>
    /// Monte Carlo simulation node for scenario analysis.
    /// </summary>
    public class MonteCarloNode : QuantControl
    {
        private int _numSimulations = 10000;
        public int NumSimulations { get => _numSimulations; set { if (_numSimulations == value) return; _numSimulations = value; if (NodeProperties.TryGetValue("NumSimulations", out var pi)) pi.ParameterCurrentValue = _numSimulations; InvalidateVisual(); } }
        
        private int _timeSteps = 252;
        public int TimeSteps { get => _timeSteps; set { if (_timeSteps == value) return; _timeSteps = value; if (NodeProperties.TryGetValue("TimeSteps", out var pi)) pi.ParameterCurrentValue = _timeSteps; InvalidateVisual(); } }
        
        private double _initialValue = 100.0;
        public double InitialValue { get => _initialValue; set { if (Math.Abs(_initialValue - value) < 0.01) return; _initialValue = value; if (NodeProperties.TryGetValue("InitialValue", out var pi)) pi.ParameterCurrentValue = _initialValue; InvalidateVisual(); } }
        
        private int _randomSeed = 42;
        public int RandomSeed { get => _randomSeed; set { if (_randomSeed == value) return; _randomSeed = value; if (NodeProperties.TryGetValue("RandomSeed", out var pi)) pi.ParameterCurrentValue = _randomSeed; InvalidateVisual(); } }

        public MonteCarloNode()
        {
            Name = "Monte Carlo";
            Width = 160;
            EnsurePortCounts(1, 3); // Parameters input, Simulations/Percentiles/Statistics outputs

            NodeProperties["NumSimulations"] = new ParameterInfo { ParameterName = "NumSimulations", ParameterType = typeof(int), DefaultParameterValue = _numSimulations, ParameterCurrentValue = _numSimulations, Description = "Number of simulations" };
            NodeProperties["TimeSteps"] = new ParameterInfo { ParameterName = "TimeSteps", ParameterType = typeof(int), DefaultParameterValue = _timeSteps, ParameterCurrentValue = _timeSteps, Description = "Time steps per simulation" };
            NodeProperties["InitialValue"] = new ParameterInfo { ParameterName = "InitialValue", ParameterType = typeof(double), DefaultParameterValue = _initialValue, ParameterCurrentValue = _initialValue, Description = "Initial value" };
            NodeProperties["RandomSeed"] = new ParameterInfo { ParameterName = "RandomSeed", ParameterType = typeof(int), DefaultParameterValue = _randomSeed, ParameterCurrentValue = _randomSeed, Description = "Random seed for reproducibility" };
        }
    }

    /// <summary>
    /// Value at Risk (VaR) and Conditional VaR calculator.
    /// </summary>
    public class VaRNode : QuantControl
    {
        private double _confidenceLevel = 0.95;
        public double ConfidenceLevel { get => _confidenceLevel; set { if (Math.Abs(_confidenceLevel - value) < 0.0001) return; _confidenceLevel = value; if (NodeProperties.TryGetValue("ConfidenceLevel", out var pi)) pi.ParameterCurrentValue = _confidenceLevel; InvalidateVisual(); } }
        
        private string _method = "Historical";
        public string Method { get => _method; set { if (_method == value) return; _method = value ?? ""; if (NodeProperties.TryGetValue("Method", out var pi)) pi.ParameterCurrentValue = _method; InvalidateVisual(); } }
        
        private int _windowSize = 252;
        public int WindowSize { get => _windowSize; set { if (_windowSize == value) return; _windowSize = value; if (NodeProperties.TryGetValue("WindowSize", out var pi)) pi.ParameterCurrentValue = _windowSize; InvalidateVisual(); } }

        public VaRNode()
        {
            Name = "VaR";
            Width = 140;
            EnsurePortCounts(1, 2); // Returns input, VaR & CVaR outputs

            NodeProperties["ConfidenceLevel"] = new ParameterInfo { ParameterName = "ConfidenceLevel", ParameterType = typeof(double), DefaultParameterValue = _confidenceLevel, ParameterCurrentValue = _confidenceLevel, Description = "Confidence level (e.g., 0.95)" };
            NodeProperties["Method"] = new ParameterInfo { ParameterName = "Method", ParameterType = typeof(string), DefaultParameterValue = _method, ParameterCurrentValue = _method, Description = "VaR calculation method", Choices = new[] { "Historical", "Parametric", "MonteCarlo" } };
            NodeProperties["WindowSize"] = new ParameterInfo { ParameterName = "WindowSize", ParameterType = typeof(int), DefaultParameterValue = _windowSize, ParameterCurrentValue = _windowSize, Description = "Historical window size" };
        }
    }
}
