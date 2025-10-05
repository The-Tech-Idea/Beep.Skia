# Quantitative Analysis Nodes - Complete Reference

## Overview
The Beep.Ski.Quantitative project provides **25 specialized nodes** for quantitative finance, statistical analysis, and algorithmic trading workflows. All nodes inherit from `QuantControl` and follow the lazy port layout pattern for optimal performance.

## Node Categories

### 📊 Indicator Nodes (6 nodes)
Technical indicators for market analysis

#### 1. **MACDNode** - Moving Average Convergence Divergence
- **Inputs**: 1 (Price series)
- **Outputs**: 3 (MACD line, Signal line, Histogram)
- **Properties**:
  - `FastPeriod` (int, default: 12) - Fast EMA period
  - `SlowPeriod` (int, default: 26) - Slow EMA period
  - `SignalPeriod` (int, default: 9) - Signal line period
- **Use Case**: Trend following and momentum analysis

#### 2. **BollingerBandsNode** - Volatility Bands
- **Inputs**: 1 (Price series)
- **Outputs**: 3 (Upper band, Middle band, Lower band)
- **Properties**:
  - `Period` (int, default: 20) - Moving average period
  - `StdDev` (double, default: 2.0) - Standard deviation multiplier
- **Use Case**: Volatility analysis, overbought/oversold conditions

#### 3. **RSINode** - Relative Strength Index
- **Inputs**: 1 (Price series)
- **Outputs**: 1 (RSI values)
- **Properties**:
  - `Period` (int, default: 14) - RSI period
  - `Overbought` (double, default: 70.0) - Overbought threshold
  - `Oversold` (double, default: 30.0) - Oversold threshold
- **Use Case**: Momentum oscillator, divergence detection

#### 4. **StochasticNode** - Stochastic Oscillator
- **Inputs**: 1 (High/Low/Close series)
- **Outputs**: 2 (%K line, %D line)
- **Properties**:
  - `KPeriod` (int, default: 14) - %K period
  - `DPeriod` (int, default: 3) - %D period
  - `Smooth` (int, default: 3) - Smoothing period
- **Use Case**: Momentum analysis, overbought/oversold detection

#### 5. **ATRNode** - Average True Range
- **Inputs**: 1 (High/Low/Close series)
- **Outputs**: 1 (ATR values)
- **Properties**:
  - `Period` (int, default: 14) - ATR period
- **Use Case**: Volatility measurement, stop-loss placement

#### 6. **IndicatorNode** - Generic Indicator (existing)
- **Inputs**: 1
- **Outputs**: 1
- **Properties**:
  - `Indicator` (string, default: "SMA") - Choices: SMA, EMA, RSI, MACD
  - `Period` (int, default: 20) - Lookback period

### 🎯 Strategy Nodes (5 nodes)
Trading strategy development and execution

#### 7. **StrategyNode** - Signal Generator
- **Inputs**: 2 (Indicator inputs)
- **Outputs**: 1 (Buy/Sell signals)
- **Properties**:
  - `StrategyType` (string, default: "Crossover") - Choices: Crossover, Threshold, Divergence, Breakout
  - `Threshold` (double, default: 0.0) - Signal threshold
  - `LongOnly` (bool, default: false) - Allow only long positions
- **Use Case**: Generate trading signals from indicators

#### 8. **BacktestNode** - Strategy Backtesting
- **Inputs**: 2 (Price series, Signals)
- **Outputs**: 3 (Equity curve, Trade list, Statistics)
- **Properties**:
  - `StartDate` (DateTime, default: -1 year) - Backtest start date
  - `EndDate` (DateTime, default: now) - Backtest end date
  - `InitialCapital` (double, default: 10000.0) - Starting capital
  - `Commission` (double, default: 0.001) - Commission rate
- **Use Case**: Historical strategy performance testing

#### 9. **PortfolioOptimizerNode** - Portfolio Optimization
- **Inputs**: 1 (Asset returns)
- **Outputs**: 2 (Optimal weights, Statistics)
- **Properties**:
  - `Method` (string, default: "Sharpe") - Choices: Sharpe, MinVariance, MaxReturn, RiskParity
  - `TargetReturn` (double, default: 0.15) - Target annual return
  - `NumAssets` (int, default: 5) - Number of assets
- **Use Case**: Modern Portfolio Theory, asset allocation

#### 10. **RiskManagerNode** - Position Sizing & Risk Control
- **Inputs**: 2 (Price, Signals)
- **Outputs**: 2 (Sized positions, Risk metrics)
- **Properties**:
  - `MaxRiskPerTrade` (double, default: 0.02) - Max risk per trade (fraction)
  - `StopLossPercent` (double, default: 0.05) - Stop loss percentage
  - `TakeProfitPercent` (double, default: 0.10) - Take profit percentage
  - `PositionSizing` (string, default: "FixedRisk") - Choices: FixedRisk, KellyFraction, FixedFractional, VolatilityAdjusted
- **Use Case**: Risk management, position sizing

#### 11. **PerformanceNode** - Performance Metrics
- **Inputs**: 1 (Equity curve)
- **Outputs**: 1 (Performance metrics)
- **Properties**:
  - `IncludeDrawdown` (bool, default: true) - Calculate max drawdown
  - `IncludeSharpe` (bool, default: true) - Calculate Sharpe ratio
  - `RiskFreeRate` (double, default: 0.02) - Risk-free rate (annual)
- **Use Case**: Strategy performance evaluation

### 📈 Statistical Nodes (6 nodes)
Statistical analysis and modeling

#### 12. **DistributionNode** - Distribution Analysis
- **Inputs**: 1 (Data series)
- **Outputs**: 2 (Histogram, Statistics)
- **Properties**:
  - `DistributionType` (string, default: "Normal") - Choices: Normal, LogNormal, Exponential, Uniform, StudentT
  - `Bins` (int, default: 50) - Number of histogram bins
  - `ShowKDE` (bool, default: true) - Show kernel density estimate
- **Use Case**: Return distribution analysis

#### 13. **CorrelationNode** - Correlation Analysis
- **Inputs**: 2 (Multiple series)
- **Outputs**: 1 (Correlation matrix)
- **Properties**:
  - `Method` (string, default: "Pearson") - Choices: Pearson, Spearman, Kendall
  - `Window` (int, default: 252) - Rolling window size
  - `Rolling` (bool, default: false) - Use rolling correlation
- **Use Case**: Asset correlation, diversification analysis

#### 14. **RegressionNode** - Regression Analysis
- **Inputs**: 2 (X and Y series)
- **Outputs**: 2 (Fitted values, Coefficients)
- **Properties**:
  - `RegressionType` (string, default: "Linear") - Choices: Linear, Polynomial, Ridge, Lasso, Logistic
  - `Degree` (int, default: 1) - Polynomial degree
  - `IncludeIntercept` (bool, default: true) - Include intercept term
- **Use Case**: Factor analysis, beta calculation

#### 15. **ForecastNode** - Time Series Forecasting
- **Inputs**: 1 (Historical data)
- **Outputs**: 3 (Forecast, Upper bound, Lower bound)
- **Properties**:
  - `Model` (string, default: "ARIMA") - Choices: ARIMA, Prophet, ETS, LSTM, XGBoost
  - `HorizonDays` (int, default: 30) - Forecast horizon (days)
  - `ConfidenceLevel` (double, default: 0.95) - Confidence interval level
  - `IncludeSeasonality` (bool, default: true) - Include seasonal components
- **Use Case**: Price prediction, trend forecasting

#### 16. **MonteCarloNode** - Monte Carlo Simulation
- **Inputs**: 1 (Parameters)
- **Outputs**: 3 (Simulations, Percentiles, Statistics)
- **Properties**:
  - `NumSimulations` (int, default: 10000) - Number of simulations
  - `TimeSteps` (int, default: 252) - Time steps per simulation
  - `InitialValue` (double, default: 100.0) - Initial value
  - `RandomSeed` (int, default: 42) - Random seed for reproducibility
- **Use Case**: Scenario analysis, option pricing

#### 17. **VaRNode** - Value at Risk
- **Inputs**: 1 (Returns series)
- **Outputs**: 2 (VaR, Conditional VaR)
- **Properties**:
  - `ConfidenceLevel` (double, default: 0.95) - Confidence level (e.g., 0.95)
  - `Method` (string, default: "Historical") - Choices: Historical, Parametric, MonteCarlo
  - `WindowSize` (int, default: 252) - Historical window size
- **Use Case**: Risk measurement, regulatory compliance

### 📦 Data Nodes (7 nodes)
Data management and visualization

#### 18. **DataSourceNode** - Market Data Loader
- **Inputs**: 0
- **Outputs**: 4 (Open, High, Low, Close, Volume)
- **Properties**:
  - `Provider` (string, default: "Yahoo") - Choices: Yahoo, AlphaVantage, IEX, Quandl, CSV
  - `Symbol` (string, default: "SPY") - Ticker symbol
  - `Interval` (string, default: "1D") - Choices: 1m, 5m, 15m, 1h, 1D, 1W, 1M
  - `StartDate` (DateTime, default: -2 years) - Start date for data
- **Use Case**: Load historical market data

#### 19. **TransformNode** - Data Transformation
- **Inputs**: 1
- **Outputs**: 1
- **Properties**:
  - `Operation` (string, default: "Returns") - Choices: Returns, LogReturns, Difference, Normalize, Standardize, Resample
  - `Percentage` (bool, default: true) - Return as percentage
  - `Lag` (int, default: 1) - Lag for differences/returns
- **Use Case**: Convert prices to returns, normalize data

#### 20. **FilterNode** - Data Filtering
- **Inputs**: 1
- **Outputs**: 1
- **Properties**:
  - `FilterType` (string, default: "Outlier") - Choices: Outlier, Noise, Spike, Missing
  - `Threshold` (double, default: 3.0) - Threshold for filtering
  - `Method` (string, default: "ZScore") - Choices: ZScore, IQR, MAD, Isolation
- **Use Case**: Remove outliers, clean data

#### 21. **AggregateNode** - Data Aggregation
- **Inputs**: 2 (Multiple series)
- **Outputs**: 1 (Aggregated result)
- **Properties**:
  - `Operation` (string, default: "Mean") - Choices: Mean, Median, Sum, Min, Max, StdDev, Variance
  - `SkipNaN` (bool, default: true) - Skip NaN values
- **Use Case**: Combine multiple series, portfolio aggregation

#### 22. **ChartNode** - Data Visualization
- **Inputs**: 3 (Multiple data series)
- **Outputs**: 0 (Visualization endpoint)
- **Properties**:
  - `ChartType` (string, default: "Line") - Choices: Line, Candlestick, Bar, Scatter, Heatmap, Histogram
  - `ShowGrid` (bool, default: true) - Show grid lines
  - `ShowLegend` (bool, default: true) - Show legend
  - `MaxPoints` (int, default: 1000) - Max points to display
- **Use Case**: Visualize analysis results

#### 23. **ExportNode** - Data Export
- **Inputs**: 1
- **Outputs**: 0 (Endpoint)
- **Properties**:
  - `Format` (string, default: "CSV") - Choices: CSV, JSON, Excel, Parquet, HDF5
  - `FilePath` (string, default: "output.csv") - Output file path
  - `IncludeHeader` (bool, default: true) - Include header row
- **Use Case**: Export results to file

#### 24. **TimeSeriesNode** - Time Series Input (existing)
- **Inputs**: 0
- **Outputs**: 1
- **Properties**:
  - `Length` (int, default: 1000) - Number of data points
  - `Symbol` (string, default: "EURUSD") - Instrument symbol

#### 25. **IndicatorNode** - Generic Indicator (existing, listed above)

## Example Workflows

### Workflow 1: Basic Trend Following Strategy
```
DataSourceNode (SPY) 
  → IndicatorNode (SMA, Period=50) 
  → IndicatorNode (SMA, Period=200) 
  → StrategyNode (Crossover) 
  → BacktestNode 
  → PerformanceNode 
  → ChartNode
```

### Workflow 2: RSI Mean Reversion
```
DataSourceNode (AAPL) 
  → RSINode (Period=14, Overbought=70, Oversold=30) 
  → StrategyNode (Threshold) 
  → RiskManagerNode (StopLoss=2%, TakeProfit=4%) 
  → BacktestNode 
  → PerformanceNode
```

### Workflow 3: Portfolio Optimization
```
DataSourceNode (SPY) → TransformNode (Returns) ─┐
DataSourceNode (TLT) → TransformNode (Returns) ─┤
DataSourceNode (GLD) → TransformNode (Returns) ─┼→ PortfolioOptimizerNode (Sharpe) 
DataSourceNode (QQQ) → TransformNode (Returns) ─┤     → ChartNode
DataSourceNode (IWM) → TransformNode (Returns) ─┘     → ExportNode
```

### Workflow 4: MACD + Bollinger Bands Strategy
```
DataSourceNode (TSLA) ┬→ MACDNode ────────┬→ StrategyNode 
                      └→ BollingerBandsNode─┘     → BacktestNode 
                                                    → PerformanceNode 
                                                    → ChartNode
```

### Workflow 5: Monte Carlo Risk Analysis
```
DataSourceNode (Portfolio) 
  → TransformNode (Returns) 
  → DistributionNode (Normal) 
  → MonteCarloNode (NumSimulations=10000) 
  → VaRNode (95% confidence) 
  → ChartNode
```

### Workflow 6: Machine Learning Forecast
```
DataSourceNode (BTC-USD) 
  → TransformNode (Returns) 
  → FilterNode (Outliers) 
  → ForecastNode (LSTM, Horizon=30) 
  → ChartNode 
  → ExportNode
```

### Workflow 7: Correlation Analysis
```
DataSourceNode (Tech stocks) ┬→ TransformNode (Returns) ─┐
DataSourceNode (Energy)      ┼→ TransformNode (Returns) ─┤
DataSourceNode (Finance)     ┼→ TransformNode (Returns) ─┼→ CorrelationNode (Pearson) 
DataSourceNode (Healthcare)  ┼→ TransformNode (Returns) ─┤     → ChartNode (Heatmap)
DataSourceNode (Utilities)   ┴→ TransformNode (Returns) ─┘
```

## Property Editor Integration

All nodes expose properties via `NodeProperties` dictionary for seamless editor integration:

```csharp
// Access node properties generically
var props = macdNode.GetProperties();
foreach (var kvp in props)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}

// Modify properties generically
macdNode.SetProperties(new Dictionary<string, object>
{
    ["FastPeriod"] = 10,
    ["SlowPeriod"] = 20,
    ["SignalPeriod"] = 5
});
```

## Styling and Appearance

All nodes inherit Material Design styling from `QuantControl`:
- **Fill**: Cyan 50 (#E0F7FA)
- **Stroke**: Teal 600 (#009688)
- **StrokeWidth**: 1.5px
- **Corner Radius**: 8px
- **Port Radius**: 5px
- **Padding**: 8px

## Performance Considerations

1. **Lazy Port Layout**: Ports are only recalculated when:
   - Bounds change
   - Port counts change
   - Component is first drawn
   
2. **Minimal Allocations**: Port layout helpers avoid unnecessary object creation

3. **Efficient Updates**: Property changes call `MarkPortsDirty()` instead of immediate layout

4. **Optimal Drawing**: `DrawQuantContent` template method called after port layout is ensured

## Integration with Event System

All nodes support the complete event system:
- **ComponentClicked** - Left-click
- **ComponentRightClicked** - Context menus
- **ComponentDoubleClicked** - Quick edit
- **ComponentHoverChanged** - Tooltips

Example:
```csharp
drawingManager.ComponentRightClicked += (s, e) =>
{
    if (e.Component is MACDNode macd)
    {
        ShowMACDSettings(macd, e.ScreenPosition);
    }
};
```

## Migration Notes

When creating new quantitative nodes:

1. **Inherit from `QuantControl`** (not `SkiaComponent` directly)
2. **Call `EnsurePortCounts(inCount, outCount)`** in constructor
3. **Seed `NodeProperties`** for each configurable property
4. **Override `DrawQuantContent`** (not `DrawContent`) for custom visuals
5. **Use property setters** with NodeProperties synchronization pattern
6. **Don't override `LayoutPorts`** unless you need custom port positioning

## Summary

The Quantitative family now includes **25 comprehensive nodes** covering:
- ✅ **Technical Indicators** (6 nodes): MACD, Bollinger Bands, RSI, Stochastic, ATR, Generic
- ✅ **Trading Strategies** (5 nodes): Strategy, Backtest, Portfolio Optimizer, Risk Manager, Performance
- ✅ **Statistical Analysis** (6 nodes): Distribution, Correlation, Regression, Forecast, Monte Carlo, VaR
- ✅ **Data Management** (7 nodes): Data Source, Transform, Filter, Aggregate, Chart, Export, Time Series

All nodes follow best practices for:
- Lazy port layout ⚡
- Property editor integration 🎛️
- Event system support 🎯
- Material Design styling 🎨
- Performance optimization 🚀

Build Status: ✅ **All 25 nodes compile successfully!**
