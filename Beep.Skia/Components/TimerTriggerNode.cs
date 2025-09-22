using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Beep.Skia.Model;

namespace Beep.Skia.Components
{
    /// <summary>
    /// Automation node that triggers workflow execution on a scheduled timer basis.
    /// Supports various timing patterns including intervals, cron expressions, and one-time execution.
    /// </summary>
    public class TimerTriggerNode : AutomationNode
    {
        #region Private Fields
        private string _triggerType = "Interval";
        private TimeSpan _interval = TimeSpan.FromMinutes(5);
        private string _cronExpression = "";
        private DateTime? _startTime;
        private DateTime? _endTime;
        private bool _isEnabled = true;
        private int _maxExecutions = -1; // -1 = unlimited
        private int _executionCount = 0;
        private System.Threading.Timer _timer;
        private readonly object _timerLock = new object();
        private DateTime _lastExecution = DateTime.MinValue;
        private DateTime _nextExecution = DateTime.MinValue;
        #endregion

        #region Events
        /// <summary>
        /// Event raised when the timer triggers workflow execution.
        /// </summary>
        public event EventHandler<TimerTriggeredEventArgs> TimerTriggered;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the type of timer trigger (Interval, Cron, OneTime).
        /// </summary>
        public string TriggerType
        {
            get => _triggerType;
            set
            {
                if (_triggerType != value)
                {
                    _triggerType = value ?? "Interval";
                    Configuration["TriggerType"] = _triggerType;
                    RestartTimer();
                }
            }
        }

        /// <summary>
        /// Gets or sets the interval for interval-based triggers.
        /// </summary>
        public TimeSpan Interval
        {
            get => _interval;
            set
            {
                if (_interval != value)
                {
                    _interval = value;
                    Configuration["Interval"] = value.ToString();
                    RestartTimer();
                }
            }
        }

        /// <summary>
        /// Gets or sets the cron expression for cron-based triggers.
        /// </summary>
        public string CronExpression
        {
            get => _cronExpression;
            set
            {
                if (_cronExpression != value)
                {
                    _cronExpression = value ?? "";
                    Configuration["CronExpression"] = _cronExpression;
                    RestartTimer();
                }
            }
        }

        /// <summary>
        /// Gets or sets the start time for the trigger schedule.
        /// </summary>
        public DateTime? StartTime
        {
            get => _startTime;
            set
            {
                if (_startTime != value)
                {
                    _startTime = value;
                    Configuration["StartTime"] = value?.ToString("O");
                    RestartTimer();
                }
            }
        }

        /// <summary>
        /// Gets or sets the end time for the trigger schedule.
        /// </summary>
        public DateTime? EndTime
        {
            get => _endTime;
            set
            {
                if (_endTime != value)
                {
                    _endTime = value;
                    Configuration["EndTime"] = value?.ToString("O");
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the timer trigger is enabled.
        /// </summary>
        public bool IsTimerEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    Configuration["IsEnabled"] = value;
                    if (value)
                        RestartTimer();
                    else
                        StopTimer();
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of executions (-1 for unlimited).
        /// </summary>
        public int MaxExecutions
        {
            get => _maxExecutions;
            set
            {
                if (_maxExecutions != value)
                {
                    _maxExecutions = value;
                    Configuration["MaxExecutions"] = value;
                }
            }
        }

        /// <summary>
        /// Gets the current execution count.
        /// </summary>
        public int ExecutionCount => _executionCount;

        /// <summary>
        /// Gets the time of the last execution.
        /// </summary>
        public DateTime LastExecution => _lastExecution;

        /// <summary>
        /// Gets the time of the next scheduled execution.
        /// </summary>
        public DateTime NextExecution => _nextExecution;

        /// <summary>
        /// Gets whether the timer is currently running.
        /// </summary>
        public bool IsRunning => _timer != null && _isEnabled;
        #endregion

        #region AutomationNode Implementation
        /// <summary>
        /// Gets the description of what this automation node does.
        /// </summary>
        public override string Description => "Triggers workflow execution on a scheduled timer basis using intervals, cron expressions, or one-time execution";

        /// <summary>
        /// Gets the type of this automation node.
        /// </summary>
        public override NodeType NodeType => NodeType.Trigger;

        /// <summary>
        /// Initializes the timer trigger node with the specified configuration.
        /// </summary>
        /// <param name="configuration">The configuration parameters for the node.</param>
        /// <param name="cancellationToken">Token to cancel the initialization.</param>
        /// <returns>A task representing the initialization operation.</returns>
        public override Task<bool> InitializeAsync(Dictionary<string, object> configuration, CancellationToken cancellationToken = default)
        {
            try
            {
                Status = NodeStatus.Initializing;

                // Load configuration
                Configuration = configuration ?? new Dictionary<string, object>();
                
                TriggerType = Configuration.GetValueOrDefault("TriggerType", "Interval")?.ToString() ?? "Interval";
                CronExpression = Configuration.GetValueOrDefault("CronExpression", "")?.ToString() ?? "";
                IsTimerEnabled = Convert.ToBoolean(Configuration.GetValueOrDefault("IsEnabled", true));
                MaxExecutions = Convert.ToInt32(Configuration.GetValueOrDefault("MaxExecutions", -1));

                // Parse interval
                if (Configuration.TryGetValue("Interval", out var intervalObj))
                {
                    if (intervalObj is TimeSpan ts)
                        Interval = ts;
                    else if (TimeSpan.TryParse(intervalObj.ToString(), out var parsedInterval))
                        Interval = parsedInterval;
                }

                // Parse start time
                if (Configuration.TryGetValue("StartTime", out var startObj) && DateTime.TryParse(startObj.ToString(), out var startTime))
                {
                    StartTime = startTime;
                }

                // Parse end time
                if (Configuration.TryGetValue("EndTime", out var endObj) && DateTime.TryParse(endObj.ToString(), out var endTime))
                {
                    EndTime = endTime;
                }

                Status = NodeStatus.Idle;
                
                // Start the timer if enabled
                if (IsTimerEnabled)
                {
                    RestartTimer();
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                OnError(ex, "Failed to initialize TimerTriggerNode");
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Validates the current configuration for this timer trigger node.
        /// </summary>
        /// <param name="inputData">The input data to validate (not used for trigger nodes).</param>
        /// <param name="cancellationToken">Token to cancel the validation.</param>
        /// <returns>A task containing the validation result.</returns>
        public override async Task<ValidationResult> ValidateAsync(Model.ExecutionContext inputData, CancellationToken cancellationToken = default)
        {
            var issues = new List<string>();

            // Validate trigger type
            var validTriggerTypes = new[] { "Interval", "Cron", "OneTime" };
            if (!validTriggerTypes.Contains(TriggerType, StringComparer.OrdinalIgnoreCase))
                issues.Add($"Trigger type must be one of: {string.Join(", ", validTriggerTypes)}");

            // Validate based on trigger type
            switch (TriggerType.ToLowerInvariant())
            {
                case "interval":
                    if (Interval <= TimeSpan.Zero)
                        issues.Add("Interval must be greater than zero");
                    if (Interval < TimeSpan.FromSeconds(1))
                        issues.Add("Interval should be at least 1 second for performance reasons");
                    break;

                case "cron":
                    if (string.IsNullOrWhiteSpace(CronExpression))
                        issues.Add("Cron expression is required for Cron trigger type");
                    else if (!IsValidCronExpression(CronExpression))
                        issues.Add("Invalid cron expression format");
                    break;

                case "onetime":
                    if (!StartTime.HasValue)
                        issues.Add("Start time is required for OneTime trigger type");
                    else if (StartTime.Value <= DateTime.UtcNow)
                        issues.Add("Start time must be in the future for OneTime trigger");
                    break;
            }

            // Validate time range
            if (StartTime.HasValue && EndTime.HasValue && StartTime.Value >= EndTime.Value)
                issues.Add("Start time must be before end time");

            // Validate max executions
            if (MaxExecutions < -1 || MaxExecutions == 0)
                issues.Add("Max executions must be -1 (unlimited) or a positive number");

            var result = issues.Count == 0 
                ? ValidationResult.Success()
                : ValidationResult.Failure(issues.ToArray());

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Executes the timer trigger and returns trigger information.
        /// Note: This method is called by the timer, not directly by workflow execution.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <param name="cancellationToken">Token to cancel the execution.</param>
        /// <returns>A task containing the execution result.</returns>
        public override Task<NodeResult> ExecuteAsync(Model.ExecutionContext context, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                Status = NodeStatus.Executing;

                // Check if we should execute
                if (!ShouldExecute())
                {
                    Status = NodeStatus.Idle;
                    return Task.FromResult(NodeResult.CreateSuccess(new Dictionary<string, object>
                    {
                        ["executed"] = false,
                        ["reason"] = "Execution conditions not met",
                        ["nextExecution"] = _nextExecution
                    }));
                }

                // Increment execution count
                Interlocked.Increment(ref _executionCount);
                _lastExecution = DateTime.UtcNow;

                // Calculate next execution time
                CalculateNextExecution();

                // Create trigger result data
                var resultData = new Dictionary<string, object>
                {
                    ["triggered"] = true,
                    ["triggerType"] = TriggerType,
                    ["executionCount"] = _executionCount,
                    ["triggerTime"] = _lastExecution,
                    ["nextExecution"] = _nextExecution,
                    ["timerData"] = new Dictionary<string, object>
                    {
                        ["interval"] = Interval.ToString(),
                        ["cronExpression"] = CronExpression,
                        ["maxExecutions"] = MaxExecutions,
                        ["remainingExecutions"] = MaxExecutions > 0 ? MaxExecutions - _executionCount : -1
                    }
                };

                // Raise trigger event
                OnTimerTriggered(new TimerTriggeredEventArgs
                {
                    TriggerTime = _lastExecution,
                    ExecutionCount = _executionCount,
                    Context = context,
                    TriggerData = resultData
                });

                // Check if we've reached max executions
                if (MaxExecutions > 0 && _executionCount >= MaxExecutions)
                {
                    StopTimer();
                    resultData["completed"] = true;
                    resultData["reason"] = "Maximum executions reached";
                }

                Status = NodeStatus.Completed;
                var result = NodeResult.CreateSuccess(resultData);
                result.ExecutionTime = DateTime.UtcNow - startTime;
                return Task.FromResult(result);
            }
            catch (OperationCanceledException)
            {
                Status = NodeStatus.Cancelled;
                return Task.FromResult(NodeResult.CreateFailure("Timer trigger execution was cancelled"));
            }
            catch (Exception ex)
            {
                OnError(ex, "Failed to execute timer trigger");
                return Task.FromResult(NodeResult.CreateFailure($"Timer trigger failed: {ex.Message}", ex));
            }
        }

        /// <summary>
        /// Gets the schema defining the input parameters this node expects.
        /// Trigger nodes typically don't require input data.
        /// </summary>
        /// <returns>A dictionary describing the input schema.</returns>
        public override Dictionary<string, object> GetInputSchema()
        {
            return new Dictionary<string, object>
            {
                ["type"] = "object",
                ["description"] = "Timer trigger nodes don't require input data",
                ["properties"] = new Dictionary<string, object>
                {
                    ["triggerData"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["description"] = "Optional trigger metadata",
                        ["additionalProperties"] = true
                    }
                }
            };
        }

        /// <summary>
        /// Gets the schema defining the output parameters this node produces.
        /// </summary>
        /// <returns>A dictionary describing the output schema.</returns>
        public override Dictionary<string, object> GetOutputSchema()
        {
            return new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["triggered"] = new Dictionary<string, object>
                    {
                        ["type"] = "boolean",
                        ["description"] = "Whether the trigger fired"
                    },
                    ["triggerType"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "The type of trigger that fired"
                    },
                    ["executionCount"] = new Dictionary<string, object>
                    {
                        ["type"] = "integer",
                        ["description"] = "Total number of times this trigger has fired"
                    },
                    ["triggerTime"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["format"] = "date-time",
                        ["description"] = "The time when the trigger fired"
                    },
                    ["nextExecution"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["format"] = "date-time",
                        ["description"] = "The next scheduled execution time"
                    },
                    ["timerData"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["description"] = "Additional timer configuration and state data"
                    }
                }
            };
        }
        #endregion

        #region Timer Management
        /// <summary>
        /// Starts the timer based on the current configuration.
        /// </summary>
        public void StartTimer()
        {
            if (!IsTimerEnabled || IsRunning)
                return;

            RestartTimer();
        }

        /// <summary>
        /// Stops the timer.
        /// </summary>
        public void StopTimer()
        {
            lock (_timerLock)
            {
                _timer?.Dispose();
                _timer = null;
                Status = NodeStatus.Idle;
            }
        }

        /// <summary>
        /// Restarts the timer with current configuration.
        /// </summary>
        private void RestartTimer()
        {
            if (!IsTimerEnabled)
                return;

            lock (_timerLock)
            {
                // Stop existing timer
                _timer?.Dispose();
                _timer = null;

                try
                {
                    var delay = CalculateInitialDelay();
                    if (delay.HasValue)
                    {
                        var period = TriggerType.ToLowerInvariant() == "onetime" 
                            ? Timeout.InfiniteTimeSpan 
                            : CalculatePeriod();

                        _timer = new System.Threading.Timer(OnTimerCallback, null, delay.Value, period);
                        CalculateNextExecution();
                        Status = NodeStatus.Idle; // Ready to trigger
                    }
                }
                catch (Exception ex)
                {
                    OnError(ex, "Failed to start timer");
                }
            }
        }

        /// <summary>
        /// Calculates the initial delay before the first timer execution.
        /// </summary>
        /// <returns>The initial delay, or null if timer should not start.</returns>
        private TimeSpan? CalculateInitialDelay()
        {
            var now = DateTime.UtcNow;

            // Check if we're within the allowed time window
            if (StartTime.HasValue && now < StartTime.Value)
            {
                return StartTime.Value - now;
            }

            if (EndTime.HasValue && now >= EndTime.Value)
            {
                return null; // Past end time
            }

            // Check max executions
            if (MaxExecutions > 0 && _executionCount >= MaxExecutions)
            {
                return null; // Already reached max executions
            }

            return TriggerType.ToLowerInvariant() switch
            {
                "interval" => Interval,
                "cron" => CalculateNextCronExecution() - now,
                "onetime" => StartTime.HasValue ? StartTime.Value - now : TimeSpan.Zero,
                _ => TimeSpan.Zero
            };
        }

        /// <summary>
        /// Calculates the period between timer executions.
        /// </summary>
        /// <returns>The execution period.</returns>
        private TimeSpan CalculatePeriod()
        {
            return TriggerType.ToLowerInvariant() switch
            {
                "interval" => Interval,
                "cron" => TimeSpan.FromMinutes(1), // Check every minute for cron
                "onetime" => Timeout.InfiniteTimeSpan,
                _ => Interval
            };
        }

        /// <summary>
        /// Calculates the next execution time based on the trigger type.
        /// </summary>
        private void CalculateNextExecution()
        {
            var now = DateTime.UtcNow;

            _nextExecution = TriggerType.ToLowerInvariant() switch
            {
                "interval" => now.Add(Interval),
                "cron" => CalculateNextCronExecution(),
                "onetime" => StartTime ?? now,
                _ => now.Add(Interval)
            };
        }

        /// <summary>
        /// Calculates the next execution time for cron expressions.
        /// </summary>
        /// <returns>The next cron execution time.</returns>
        private DateTime CalculateNextCronExecution()
        {
            // Simplified cron calculation - in production would use a proper cron library like Quartz.NET
            var now = DateTime.UtcNow;
            
            // Basic cron pattern matching for common cases
            if (string.IsNullOrWhiteSpace(CronExpression))
                return now.AddMinutes(5);

            try
            {
                // Simple patterns:
                // "0 */5 * * *" = every 5 minutes
                // "0 0 * * *" = daily at midnight
                // "0 0 0 * *" = monthly
                
                var parts = CronExpression.Split(' ');
                if (parts.Length >= 5)
                {
                    // Very basic minute parsing
                    if (parts[1].StartsWith("*/"))
                    {
                        if (int.TryParse(parts[1].Substring(2), out var intervalMinutes))
                        {
                            var nextMinute = (now.Minute / intervalMinutes + 1) * intervalMinutes;
                            if (nextMinute >= 60)
                            {
                                return now.AddHours(1).AddMinutes(nextMinute - 60 - now.Minute);
                            }
                            return now.AddMinutes(nextMinute - now.Minute);
                        }
                    }
                }

                return now.AddMinutes(5); // Default fallback
            }
            catch
            {
                return now.AddMinutes(5); // Fallback on parse error
            }
        }

        /// <summary>
        /// Timer callback method.
        /// </summary>
        /// <param name="state">Timer state.</param>
        private async void OnTimerCallback(object state)
        {
            try
            {
                if (!ShouldExecute())
                    return;

                // Execute the trigger
                var context = new Model.ExecutionContext("timer-workflow", Guid.NewGuid().ToString())
                {
                    Data = new Dictionary<string, object>
                    {
                        ["triggerType"] = "Timer",
                        ["triggerTime"] = DateTime.UtcNow,
                        ["executionCount"] = _executionCount + 1
                    },
                    Variables = new Dictionary<string, object>(),
                    PreviousResults = new List<object>()
                };

                await ExecuteAsync(context, CancellationToken.None);

                // For one-time triggers, stop after execution
                if (TriggerType.ToLowerInvariant() == "onetime")
                {
                    StopTimer();
                }
            }
            catch (Exception ex)
            {
                OnError(ex, "Timer callback execution failed");
            }
        }

        /// <summary>
        /// Determines if the timer should execute based on current conditions.
        /// </summary>
        /// <returns>True if should execute.</returns>
        private bool ShouldExecute()
        {
            var now = DateTime.UtcNow;

            // Check if enabled
            if (!IsTimerEnabled)
                return false;

            // Check time window
            if (StartTime.HasValue && now < StartTime.Value)
                return false;

            if (EndTime.HasValue && now >= EndTime.Value)
            {
                StopTimer();
                return false;
            }

            // Check max executions
            if (MaxExecutions > 0 && _executionCount >= MaxExecutions)
            {
                StopTimer();
                return false;
            }

            // For cron triggers, check if it's the right time
            if (TriggerType.ToLowerInvariant() == "cron")
            {
                return IsCronTimeMatch(now);
            }

            return true;
        }

        /// <summary>
        /// Checks if the current time matches the cron expression.
        /// </summary>
        /// <param name="time">The time to check.</param>
        /// <returns>True if the time matches.</returns>
        private bool IsCronTimeMatch(DateTime time)
        {
            // Simplified cron matching - would use proper cron library in production
            return time.Second == 0; // Only trigger on minute boundaries
        }

        /// <summary>
        /// Validates a cron expression format.
        /// </summary>
        /// <param name="cronExpression">The cron expression to validate.</param>
        /// <returns>True if valid.</returns>
        private bool IsValidCronExpression(string cronExpression)
        {
            if (string.IsNullOrWhiteSpace(cronExpression))
                return false;

            // Basic validation - proper implementation would use cron parsing library
            var parts = cronExpression.Split(' ');
            return parts.Length >= 5 && parts.Length <= 6;
        }
        #endregion

        #region Event Handling
        /// <summary>
        /// Raises the TimerTriggered event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnTimerTriggered(TimerTriggeredEventArgs e)
        {
            TimerTriggered?.Invoke(this, e);
        }
        #endregion

        #region Visual Customization
        /// <summary>
        /// Draws the timer trigger node with custom styling.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The node bounds.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawNodeContent(SKCanvas canvas, SKRect bounds, DrawingContext context)
        {
            // Draw timer icon
            DrawTimerIcon(canvas, bounds);

            // Draw node title
            using var font = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), 12);
            using var textPaint = new SKPaint
            {
                IsAntialias = true,
                Color = GetTextColor()
            };

            var title = "Timer";
            var titleWidth = font.MeasureText(title);
            var titleX = bounds.MidX - titleWidth / 2;
            var titleY = bounds.Top + 20;
            canvas.DrawText(title, titleX, titleY, font, textPaint);

            // Draw trigger type
            using var typeFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 10);
            var typeWidth = typeFont.MeasureText(TriggerType);
            var typeX = bounds.MidX - typeWidth / 2;
            var typeY = bounds.Top + 35;
            canvas.DrawText(TriggerType, typeX, typeY, typeFont, textPaint);

            // Draw status info
            DrawStatusInfo(canvas, bounds);

            // Draw enabled/disabled indicator
            DrawEnabledIndicator(canvas, bounds);
        }

        /// <summary>
        /// Draws the timer icon.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The node bounds.</param>
        private void DrawTimerIcon(SKCanvas canvas, SKRect bounds)
        {
            var iconSize = 16f;
            var iconX = bounds.MidX - iconSize / 2;
            var iconY = bounds.Top + 6;

            using var iconPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = IsRunning ? SKColor.Parse("#4CAF50") : SKColor.Parse("#757575"),
                StrokeWidth = 2
            };

            // Draw clock circle
            var iconCenter = new SKPoint(iconX + iconSize / 2, iconY + iconSize / 2);
            canvas.DrawCircle(iconCenter, iconSize / 2 - 1, iconPaint);

            // Draw clock hands
            using var handPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = iconPaint.Color,
                StrokeWidth = 1.5f
            };

            // Hour hand
            var hourAngle = (DateTime.Now.Hour % 12) * 30 * Math.PI / 180;
            var hourX = iconCenter.X + (iconSize / 4) * (float)Math.Sin(hourAngle);
            var hourY = iconCenter.Y - (iconSize / 4) * (float)Math.Cos(hourAngle);
            canvas.DrawLine(iconCenter, new SKPoint(hourX, hourY), handPaint);

            // Minute hand
            var minuteAngle = DateTime.Now.Minute * 6 * Math.PI / 180;
            var minuteX = iconCenter.X + (iconSize / 3) * (float)Math.Sin(minuteAngle);
            var minuteY = iconCenter.Y - (iconSize / 3) * (float)Math.Cos(minuteAngle);
            canvas.DrawLine(iconCenter, new SKPoint(minuteX, minuteY), handPaint);
        }

        /// <summary>
        /// Draws status information about the timer.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The node bounds.</param>
        private void DrawStatusInfo(SKCanvas canvas, SKRect bounds)
        {
            using var statusFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 8);
            using var statusPaint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColor.Parse("#666666")
            };

            var y = bounds.Bottom - 24;

            // Execution count
            var countText = $"Runs: {_executionCount}";
            if (MaxExecutions > 0)
                countText += $"/{MaxExecutions}";
            
            var countWidth = statusFont.MeasureText(countText);
            var countX = bounds.MidX - countWidth / 2;
            canvas.DrawText(countText, countX, y, statusFont, statusPaint);

            // Next execution (if applicable)
            if (IsRunning && _nextExecution > DateTime.UtcNow)
            {
                var nextText = $"Next: {_nextExecution:HH:mm}";
                var nextWidth = statusFont.MeasureText(nextText);
                var nextX = bounds.MidX - nextWidth / 2;
                canvas.DrawText(nextText, nextX, bounds.Bottom - 12, statusFont, statusPaint);
            }
        }

        /// <summary>
        /// Draws the enabled/disabled indicator.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The node bounds.</param>
        private void DrawEnabledIndicator(SKCanvas canvas, SKRect bounds)
        {
            var indicatorSize = 8f;
            var indicatorX = bounds.Right - indicatorSize - 4;
            var indicatorY = bounds.Top + 4;

            using var indicatorPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = IsTimerEnabled 
                    ? (IsRunning ? SKColor.Parse("#4CAF50") : SKColor.Parse("#FF9800"))
                    : SKColor.Parse("#F44336")
            };

            canvas.DrawCircle(indicatorX + indicatorSize / 2, indicatorY + indicatorSize / 2, indicatorSize / 2, indicatorPaint);
        }

        /// <summary>
        /// Gets the background color for timer trigger nodes.
        /// </summary>
        /// <returns>Background color with timer styling.</returns>
        protected override SKColor GetBackgroundColor()
        {
            if (!IsEnabled)
                return SKColors.LightGray.WithAlpha(128);

            if (!IsTimerEnabled)
                return SKColor.Parse("#FFEBEE"); // Light red for disabled

            return Status switch
            {
                NodeStatus.Executing => SKColor.Parse("#E8F5E8"), // Light green for executing
                NodeStatus.Completed => SKColor.Parse("#E8F5E8"), // Light green for completed
                NodeStatus.Failed => SKColor.Parse("#FFEBEE"), // Light red for failed
                NodeStatus.Cancelled => SKColor.Parse("#F3E5F5"), // Light purple for cancelled
                _ => IsRunning ? SKColor.Parse("#F1F8E9") : SKColor.Parse("#FFF3E0") // Light green if running, light orange if idle
            };
        }
        #endregion

        #region Cleanup
        /// <summary>
        /// Disposes of the timer trigger node and stops the timer.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopTimer();
            }
            base.Dispose(disposing);
        }
        #endregion
    }

    #region Event Arguments
    /// <summary>
    /// Event arguments for timer triggered events.
    /// </summary>
    public class TimerTriggeredEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the time when the timer triggered.
        /// </summary>
        public DateTime TriggerTime { get; set; }

        /// <summary>
        /// Gets or sets the execution count.
        /// </summary>
        public int ExecutionCount { get; set; }

        /// <summary>
        /// Gets or sets the execution context.
        /// </summary>
        public Model.ExecutionContext Context { get; set; }

        /// <summary>
        /// Gets or sets additional trigger data.
        /// </summary>
        public Dictionary<string, object> TriggerData { get; set; }
    }
    #endregion
}