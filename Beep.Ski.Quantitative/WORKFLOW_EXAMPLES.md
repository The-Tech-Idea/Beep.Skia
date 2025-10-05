# Quantitative Workflow Examples

## Visual Node Connection Diagrams

### 1. Basic Moving Average Crossover Strategy

```
┌─────────────────┐
│  DataSourceNode │ (SPY, Daily, 2 years)
│   Provider:     │
│   Yahoo Finance │
└────────┬────────┘
         │ OHLCV Data
         ├────────────────────────────────┐
         │                                │
         ▼                                ▼
┌─────────────────┐              ┌─────────────────┐
│  IndicatorNode  │              │  IndicatorNode  │
│   Indicator:    │              │   Indicator:    │
│   SMA           │              │   SMA           │
│   Period: 50    │              │   Period: 200   │
└────────┬────────┘              └────────┬────────┘
         │ Fast MA                         │ Slow MA
         └────────────────┬────────────────┘
                          ▼
                 ┌─────────────────┐
                 │  StrategyNode   │
                 │   Type:         │
                 │   Crossover     │
                 │   LongOnly: Y   │
                 └────────┬────────┘
                          │ Buy/Sell Signals
                          ▼
                 ┌─────────────────┐
                 │  BacktestNode   │
                 │   Initial: 10K  │
                 │   Comm: 0.1%    │
                 └────────┬────────┘
                          │ Equity Curve
                          ├────────────┬──────────────┐
                          ▼            ▼              ▼
                 ┌─────────────┐ ┌──────────┐ ┌─────────────┐
                 │Performance  │ │ChartNode │ │ ExportNode  │
                 │   Node      │ │Line Chart│ │   CSV       │
                 └─────────────┘ └──────────┘ └─────────────┘
```

### 2. RSI Mean Reversion with Risk Management

```
┌─────────────────┐
│  DataSourceNode │ (AAPL)
└────────┬────────┘
         │ Close Price
         ▼
┌─────────────────┐
│    RSINode      │
│   Period: 14    │
│   OB: 70, OS:30 │
└────────┬────────┘
         │ RSI Values
         ▼
┌─────────────────┐
│  StrategyNode   │
│   Type:         │
│   Threshold     │
└────────┬────────┘
         │ Signals
         ▼
┌─────────────────┐
│  RiskManager    │
│   MaxRisk: 2%   │
│   StopLoss: 2%  │
│   TakeProfit:4% │
└────────┬────────┘
         │ Sized Positions
         ▼
┌─────────────────┐
│  BacktestNode   │
└────────┬────────┘
         │ Results
         ├────────────┬──────────┐
         ▼            ▼          ▼
┌─────────────┐ ┌─────────┐ ┌──────────┐
│Performance  │ │ Chart   │ │ Export   │
└─────────────┘ └─────────┘ └──────────┘
```

### 3. Multi-Asset Portfolio Optimization

```
┌──────────────┐
│ DataSource   │ SPY (S&P 500)
│ Yahoo        │
└──────┬───────┘
       │
       ▼
┌──────────────┐
│ Transform    │ Returns
└──────┬───────┘
       │
       ├─────────────────────────────────────┐
       │                                     │
┌──────────────┐                  ┌──────────────┐
│ DataSource   │ TLT (Bonds)      │ DataSource   │ GLD (Gold)
└──────┬───────┘                  └──────┬───────┘
       │                                  │
       ▼                                  ▼
┌──────────────┐                  ┌──────────────┐
│ Transform    │ Returns          │ Transform    │ Returns
└──────┬───────┘                  └──────┬───────┘
       │                                  │
       └────────────┬────────────┬────────┘
                    │            │
┌──────────────┐    │            │    ┌──────────────┐
│ DataSource   │ QQQ│            │IWM │ DataSource   │
└──────┬───────┘    │            │    └──────┬───────┘
       │            │            │           │
       ▼            │            │           ▼
┌──────────────┐    │            │    ┌──────────────┐
│ Transform    │    │            │    │ Transform    │
└──────┬───────┘    │            │    └──────┬───────┘
       │            │            │           │
       └────────────┴────────────┴───────────┘
                    │
                    ▼
           ┌─────────────────────┐
           │ PortfolioOptimizer  │
           │   Method: Sharpe    │
           │   TargetReturn: 15% │
           │   NumAssets: 5      │
           └──────────┬──────────┘
                      │ Optimal Weights
                      ├─────────────┬──────────┐
                      ▼             ▼          ▼
              ┌─────────────┐ ┌────────┐ ┌──────────┐
              │ Performance │ │ Chart  │ │ Export   │
              │    Node     │ │Pie/Bar │ │  Excel   │
              └─────────────┘ └────────┘ └──────────┘
```

### 4. MACD + Bollinger Bands Multi-Indicator Strategy

```
┌─────────────────┐
│  DataSourceNode │ (TSLA, Hourly)
└────────┬────────┘
         │ OHLC Data
         ├──────────────────────────┐
         │                          │
         ▼                          ▼
┌─────────────────┐        ┌─────────────────┐
│    MACDNode     │        │BollingerBands   │
│  Fast: 12       │        │  Period: 20     │
│  Slow: 26       │        │  StdDev: 2.0    │
│  Signal: 9      │        └────────┬────────┘
└────────┬────────┘                 │ Upper/Mid/Lower
         │ MACD/Signal/Histogram    │
         └────────────┬──────────────┘
                      │
                      ▼
             ┌─────────────────┐
             │  StrategyNode   │
             │   Type:         │
             │   Divergence    │
             └────────┬────────┘
                      │ Signals
                      ▼
             ┌─────────────────┐
             │  RiskManager    │
             │   MaxRisk: 1.5% │
             │   Volatility    │
             │   Adjusted      │
             └────────┬────────┘
                      │
                      ▼
             ┌─────────────────┐
             │  BacktestNode   │
             │   2 years       │
             └────────┬────────┘
                      │
                      ├────────────┬──────────┐
                      ▼            ▼          ▼
             ┌─────────────┐ ┌────────┐ ┌──────────┐
             │Performance  │ │ Chart  │ │ Export   │
             │   Sharpe    │ │Equity+ │ │  JSON    │
             │   MaxDD     │ │Trades  │ └──────────┘
             └─────────────┘ └────────┘
```

### 5. Statistical Distribution Analysis with Monte Carlo

```
┌─────────────────┐
│  DataSourceNode │ (Portfolio Returns)
└────────┬────────┘
         │ Returns Data
         ▼
┌─────────────────┐
│  TransformNode  │
│   Operation:    │
│   LogReturns    │
└────────┬────────┘
         │
         ├──────────────────┬────────────────┐
         │                  │                │
         ▼                  ▼                ▼
┌─────────────────┐ ┌───────────────┐ ┌──────────────┐
│DistributionNode│ │CorrelationNode│ │ VaRNode      │
│   Type: Normal  │ │ Method:Pearson│ │ Confidence:  │
│   Bins: 50      │ │ Window: 252   │ │ 95%          │
│   KDE: Yes      │ └───────┬───────┘ └──────┬───────┘
└────────┬────────┘         │                │
         │ Statistics       │ Matrix         │ VaR/CVaR
         └──────────────────┼────────────────┘
                            │
                            ▼
                   ┌─────────────────┐
                   │ MonteCarloNode  │
                   │   Simulations:  │
                   │   10,000        │
                   │   Steps: 252    │
                   └────────┬────────┘
                            │ Scenarios
                            ├─────────────┬──────────┐
                            ▼             ▼          ▼
                   ┌─────────────┐ ┌────────┐ ┌──────────┐
                   │   Chart     │ │ Chart  │ │ Export   │
                   │  Histogram  │ │Scenarios│ │ HDF5     │
                   └─────────────┘ └────────┘ └──────────┘
```

### 6. Machine Learning Price Forecast

```
┌─────────────────┐
│  DataSourceNode │ (Bitcoin, Daily, 5 years)
│   Symbol:       │
│   BTC-USD       │
└────────┬────────┘
         │ OHLCV
         ▼
┌─────────────────┐
│  TransformNode  │
│   Returns       │
│   Percentage    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   FilterNode    │
│   Type: Outlier │
│   Method: ZScore│
│   Threshold: 3  │
└────────┬────────┘
         │ Cleaned Data
         ├────────────────────┐
         │                    │
         ▼                    ▼
┌─────────────────┐  ┌─────────────────┐
│ RegressionNode  │  │  ForecastNode   │
│   Type: Ridge   │  │   Model: LSTM   │
│   Degree: 2     │  │   Horizon: 30   │
└────────┬────────┘  │   Confidence:95%│
         │ Fit       │   Seasonality:Y │
         │           └────────┬────────┘
         │                    │ Forecast + Bounds
         └──────────┬─────────┘
                    │
                    ├────────────┬──────────┐
                    ▼            ▼          ▼
           ┌─────────────┐ ┌────────┐ ┌──────────┐
           │ Performance │ │ Chart  │ │ Export   │
           │   RMSE      │ │Forecast│ │  JSON    │
           │   MAE       │ │+Actual │ └──────────┘
           └─────────────┘ └────────┘
```

### 7. Real-Time Indicator Dashboard

```
┌─────────────────┐
│  TimeSeriesNode │ (Real-time feed)
│   Symbol:       │
│   ES (S&P Fut)  │
└────────┬────────┘
         │ Tick Data
         ├──────────┬──────────┬──────────┬──────────┐
         │          │          │          │          │
         ▼          ▼          ▼          ▼          ▼
┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐
│  SMA   │ │  RSI   │ │  MACD  │ │  BB    │ │  ATR   │
│ Fast   │ │ 14     │ │ 12/26/9│ │ 20,2   │ │ 14     │
└───┬────┘ └───┬────┘ └───┬────┘ └───┬────┘ └───┬────┘
    │          │          │          │          │
    └──────────┴──────────┴──────────┴──────────┘
                          │
                          ▼
                 ┌─────────────────┐
                 │  AggregateNode  │
                 │   Combine all   │
                 │   indicators    │
                 └────────┬────────┘
                          │
                          ├────────────┬──────────┐
                          ▼            ▼          ▼
                 ┌─────────────┐ ┌────────┐ ┌──────────┐
                 │  ChartNode  │ │ Alert  │ │ Export   │
                 │  Multi-line │ │ Node   │ │ Real-time│
                 │  Dashboard  │ └────────┘ └──────────┘
                 └─────────────┘
```

## Node Color Coding (Visual Guide)

```
📊 Indicators     = Cyan 50 / Teal Border
🎯 Strategies     = Cyan 50 / Teal Border  
📈 Statistical    = Cyan 50 / Teal Border
📦 Data/IO        = Cyan 50 / Teal Border
```

All nodes use consistent Material Design styling:
- **Background**: #E0F7FA (Cyan 50)
- **Border**: #009688 (Teal 600, 1.5px)
- **Corners**: 8px rounded
- **Ports**: 5px radius circles

## Connection Line Colors (Suggested)

```
Price Data        = Blue
Indicator Output  = Green
Trading Signals   = Orange
Risk Metrics      = Red
Statistics        = Purple
```

## Port Conventions

```
Left Side (Inputs)    Right Side (Outputs)
      ○                     ○
      ○                     ○
      ○                     ○
```

- **Circle ports** = Data series connections
- **Port spacing** = Evenly distributed along edge
- **Port color** = Teal (#009688)

## Interactive Features

### Right-Click Context Menu
```
Component → Right-Click → Context Menu
  ├─ Edit Properties
  ├─ View Data
  ├─ Export Results
  ├─ Run Calculation
  ├─ Delete Node
  └─ Help
```

### Hover Tooltips
```
Hover → Show:
  ├─ Node Name
  ├─ Node Type
  ├─ Current Settings
  └─ Last Updated
```

### Double-Click
```
Double-Click → Open Property Editor
```

## Example Use Cases

### Hedge Fund Strategy
- Multiple data sources (stocks, bonds, commodities)
- Complex indicator combinations
- Multi-leg strategy logic
- Sophisticated risk management
- Performance attribution
- Real-time monitoring

### Retail Trading Bot
- Single symbol focus
- Simple indicator setup
- Clear entry/exit rules
- Fixed position sizing
- Basic performance tracking
- Alert notifications

### Academic Research
- Historical data analysis
- Statistical model fitting
- Hypothesis testing
- Visualization of results
- Publication-ready exports
- Reproducible research

### Portfolio Management
- Multi-asset allocation
- Correlation analysis
- Risk metrics (VaR, CVaR)
- Rebalancing rules
- Performance reporting
- Compliance monitoring

## Node Connection Best Practices

1. **Left to Right Flow**: Data flows left → right for clarity
2. **Top to Bottom**: Alternative layouts can flow top → bottom
3. **Minimize Crossings**: Arrange nodes to avoid line crossings
4. **Group Related**: Keep related nodes close together
5. **Color Code Lines**: Use different colors for data types
6. **Label Connections**: Add labels for complex connections
7. **Use Alignment**: Align nodes in columns by function

## Performance Tips

- **Limit Chart Points**: ChartNode MaxPoints = 1000 for responsiveness
- **Batch Processing**: Use AggregateNode to reduce connections
- **Cache Results**: Export intermediate results for reuse
- **Parallel Paths**: Independent calculations can run concurrently
- **Filter Early**: Use FilterNode early to reduce data volume

---

These workflows demonstrate the power and flexibility of the Quantitative node system! 🚀
