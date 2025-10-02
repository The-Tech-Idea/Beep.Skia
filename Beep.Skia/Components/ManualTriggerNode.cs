using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Beep.Skia.Model;

namespace Beep.Skia.Components
{
    /// <summary>
    /// Automation node that triggers workflow execution manually through user interaction.
    /// Provides interactive buttons, keyboard shortcuts, and programmatic triggering capabilities.
    /// </summary>
    public class ManualTriggerNode : AutomationNode
    {
        public ManualTriggerNode()
        {
            UpsertNodeProperty("TriggerText", typeof(string), _triggerText, "Button text");
            UpsertNodeProperty("KeyboardShortcut", typeof(string), _keyboardShortcut, "e.g., Ctrl+Enter");
            UpsertNodeProperty("RequireConfirmation", typeof(bool), _requireConfirmation);
            UpsertNodeProperty("ConfirmationMessage", typeof(string), _confirmationMessage);
            UpsertNodeProperty("IsTriggerable", typeof(bool), _isTriggerable);
            UpsertNodeProperty("AutoReset", typeof(bool), _autoReset);
            UpsertNodeProperty("CooldownPeriod", typeof(string), _cooldownPeriod.ToString());
        }
        #region Private Fields
        private string _triggerText = "Start Workflow";
        private string _keyboardShortcut = "";
        private bool _requireConfirmation = false;
        private string _confirmationMessage = "Are you sure you want to start this workflow?";
        private bool _isTriggerable = true;
        private int _executionCount = 0;
        private DateTime _lastExecution = DateTime.MinValue;
        private Dictionary<string, object> _triggerPayload = new Dictionary<string, object>();
        private bool _autoReset = true;
        private TimeSpan _cooldownPeriod = TimeSpan.Zero;
        private DateTime _lastTriggerTime = DateTime.MinValue;
        #endregion

        #region Events
        /// <summary>
        /// Event raised when the manual trigger is activated.
        /// </summary>
        public event EventHandler<ManualTriggerEventArgs> ManuallyTriggered;

        /// <summary>
        /// Event raised when trigger confirmation is requested.
        /// </summary>
        public event EventHandler<TriggerConfirmationEventArgs> ConfirmationRequested;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the text displayed on the trigger button.
        /// </summary>
        public string TriggerText
        {
            get => _triggerText;
            set
            {
                if (_triggerText != value)
                {
                    _triggerText = value ?? "Start Workflow";
                    Configuration["TriggerText"] = _triggerText;
                    UpsertNodeProperty("TriggerText", typeof(string), _triggerText);
                }
            }
        }

        /// <summary>
        /// Gets or sets the keyboard shortcut for triggering (e.g., "Ctrl+Enter").
        /// </summary>
        public string KeyboardShortcut
        {
            get => _keyboardShortcut;
            set
            {
                if (_keyboardShortcut != value)
                {
                    _keyboardShortcut = value ?? "";
                    Configuration["KeyboardShortcut"] = _keyboardShortcut;
                    UpsertNodeProperty("KeyboardShortcut", typeof(string), _keyboardShortcut);
                }
            }
        }

        /// <summary>
        /// Gets or sets whether confirmation is required before triggering.
        /// </summary>
        public bool RequireConfirmation
        {
            get => _requireConfirmation;
            set
            {
                if (_requireConfirmation != value)
                {
                    _requireConfirmation = value;
                    Configuration["RequireConfirmation"] = value;
                    UpsertNodeProperty("RequireConfirmation", typeof(bool), _requireConfirmation);
                }
            }
        }

        /// <summary>
        /// Gets or sets the confirmation message displayed to users.
        /// </summary>
        public string ConfirmationMessage
        {
            get => _confirmationMessage;
            set
            {
                if (_confirmationMessage != value)
                {
                    _confirmationMessage = value ?? "Are you sure you want to start this workflow?";
                    Configuration["ConfirmationMessage"] = _confirmationMessage;
                    UpsertNodeProperty("ConfirmationMessage", typeof(string), _confirmationMessage);
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the trigger is currently available for activation.
        /// </summary>
        public bool IsTriggerable
        {
            get => _isTriggerable;
            set
            {
                if (_isTriggerable != value)
                {
                    _isTriggerable = value;
                    Configuration["IsTriggerable"] = value;
                    UpsertNodeProperty("IsTriggerable", typeof(bool), _isTriggerable);
                }
            }
        }

        /// <summary>
        /// Gets the number of times this trigger has been activated.
        /// </summary>
        public int ExecutionCount => _executionCount;

        /// <summary>
        /// Gets the time of the last manual trigger activation.
        /// </summary>
        public DateTime LastExecution => _lastExecution;

        /// <summary>
        /// Gets or sets additional payload data to include with trigger execution.
        /// </summary>
        public Dictionary<string, object> TriggerPayload
        {
            get => _triggerPayload;
            set
            {
                _triggerPayload = value ?? new Dictionary<string, object>();
                Configuration["TriggerPayload"] = _triggerPayload;
            }
        }

        /// <summary>
        /// Gets or sets whether the trigger automatically resets to triggerable state after execution.
        /// </summary>
        public bool AutoReset
        {
            get => _autoReset;
            set
            {
                if (_autoReset != value)
                {
                    _autoReset = value;
                    Configuration["AutoReset"] = value;
                    UpsertNodeProperty("AutoReset", typeof(bool), _autoReset);
                }
            }
        }

        /// <summary>
        /// Gets or sets the cooldown period between trigger activations.
        /// </summary>
        public TimeSpan CooldownPeriod
        {
            get => _cooldownPeriod;
            set
            {
                if (_cooldownPeriod != value)
                {
                    _cooldownPeriod = value;
                    var s = value.ToString();
                    Configuration["CooldownPeriod"] = s;
                    UpsertNodeProperty("CooldownPeriod", typeof(string), s);
                }
            }
        }

        /// <summary>
        /// Gets whether the trigger is currently in cooldown period.
        /// </summary>
        public bool IsInCooldown => CooldownPeriod > TimeSpan.Zero && 
                                   DateTime.UtcNow - _lastTriggerTime < CooldownPeriod;

        /// <summary>
        /// Gets the time remaining in the cooldown period.
        /// </summary>
        public TimeSpan CooldownRemaining => IsInCooldown 
            ? CooldownPeriod - (DateTime.UtcNow - _lastTriggerTime) 
            : TimeSpan.Zero;
        #endregion

        #region AutomationNode Implementation
        /// <summary>
        /// Gets the description of what this automation node does.
        /// </summary>
        public override string Description => "Provides manual workflow triggering through user interaction, buttons, and keyboard shortcuts";

        /// <summary>
        /// Gets the type of this automation node.
        /// </summary>
        public override NodeType NodeType => NodeType.Trigger;

        /// <summary>
        /// Initializes the manual trigger node with the specified configuration.
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
                
                TriggerText = Configuration.GetValueOrDefault("TriggerText", "Start Workflow")?.ToString() ?? "Start Workflow";
                KeyboardShortcut = Configuration.GetValueOrDefault("KeyboardShortcut", "")?.ToString() ?? "";
                RequireConfirmation = Convert.ToBoolean(Configuration.GetValueOrDefault("RequireConfirmation", false));
                ConfirmationMessage = Configuration.GetValueOrDefault("ConfirmationMessage", "Are you sure you want to start this workflow?")?.ToString() ?? "Are you sure you want to start this workflow?";
                IsTriggerable = Convert.ToBoolean(Configuration.GetValueOrDefault("IsTriggerable", true));
                AutoReset = Convert.ToBoolean(Configuration.GetValueOrDefault("AutoReset", true));

                // Parse cooldown period
                if (Configuration.TryGetValue("CooldownPeriod", out var cooldownObj))
                {
                    if (cooldownObj is TimeSpan ts)
                        CooldownPeriod = ts;
                    else if (TimeSpan.TryParse(cooldownObj.ToString(), out var parsedCooldown))
                        CooldownPeriod = parsedCooldown;
                }

                // Load trigger payload
                if (Configuration.TryGetValue("TriggerPayload", out var payloadObj) && payloadObj is Dictionary<string, object> payloadDict)
                {
                    TriggerPayload = payloadDict;
                }

                Status = NodeStatus.Idle;
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                OnError(ex, "Failed to initialize ManualTriggerNode");
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Validates the current configuration for this manual trigger node.
        /// </summary>
        /// <param name="inputData">The input data to validate (not used for trigger nodes).</param>
        /// <param name="cancellationToken">Token to cancel the validation.</param>
        /// <returns>A task containing the validation result.</returns>
        public override async Task<ValidationResult> ValidateAsync(Model.ExecutionContext inputData, CancellationToken cancellationToken = default)
        {
            var issues = new List<string>();

            // Validate trigger text
            if (string.IsNullOrWhiteSpace(TriggerText))
                issues.Add("Trigger text cannot be empty");

            // Validate keyboard shortcut format (if provided)
            if (!string.IsNullOrWhiteSpace(KeyboardShortcut))
            {
                if (!IsValidKeyboardShortcut(KeyboardShortcut))
                    issues.Add($"Invalid keyboard shortcut format: {KeyboardShortcut}");
            }

            // Validate confirmation message (if confirmation is required)
            if (RequireConfirmation && string.IsNullOrWhiteSpace(ConfirmationMessage))
                issues.Add("Confirmation message is required when RequireConfirmation is true");

            // Validate cooldown period
            if (CooldownPeriod < TimeSpan.Zero)
                issues.Add("Cooldown period cannot be negative");

            var result = issues.Count == 0 
                ? ValidationResult.Success()
                : ValidationResult.Failure(issues.ToArray());

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Executes the manual trigger and returns trigger information.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <param name="cancellationToken">Token to cancel the execution.</param>
        /// <returns>A task containing the execution result.</returns>
        public override async Task<NodeResult> ExecuteAsync(Model.ExecutionContext context, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                Status = NodeStatus.Executing;

                // Check if trigger is available
                if (!CanTrigger())
                {
                    Status = NodeStatus.Idle;
                    return NodeResult.CreateFailure("Manual trigger is not currently available");
                }

                // Handle confirmation if required
                if (RequireConfirmation)
                {
                    var confirmed = await RequestConfirmation(cancellationToken);
                    if (!confirmed)
                    {
                        Status = NodeStatus.Idle;
                        return NodeResult.CreateSuccess(new Dictionary<string, object>
                        {
                            ["triggered"] = false,
                            ["reason"] = "User cancelled confirmation"
                        });
                    }
                }

                // Increment execution count and update timing
                Interlocked.Increment(ref _executionCount);
                _lastExecution = DateTime.UtcNow;
                _lastTriggerTime = DateTime.UtcNow;

                // Set trigger state
                if (!AutoReset)
                {
                    IsTriggerable = false;
                }

                // Merge context data with trigger payload
                var mergedData = new Dictionary<string, object>(context.Data);
                foreach (var kvp in TriggerPayload)
                {
                    mergedData[kvp.Key] = kvp.Value;
                }

                // Create trigger result data
                var resultData = new Dictionary<string, object>
                {
                    ["triggered"] = true,
                    ["triggerType"] = "Manual",
                    ["executionCount"] = _executionCount,
                    ["triggerTime"] = _lastExecution,
                    ["triggerData"] = new Dictionary<string, object>
                    {
                        ["triggerText"] = TriggerText,
                        ["keyboardShortcut"] = KeyboardShortcut,
                        ["confirmed"] = RequireConfirmation,
                        ["payload"] = TriggerPayload,
                        ["autoReset"] = AutoReset,
                        ["cooldownPeriod"] = CooldownPeriod.ToString()
                    },
                    ["workflowData"] = mergedData
                };

                // Raise trigger event
                OnManuallyTriggered(new ManualTriggerEventArgs
                {
                    TriggerTime = _lastExecution,
                    ExecutionCount = _executionCount,
                    Context = new Model.ExecutionContext("manual-workflow", Guid.NewGuid().ToString())
                    {
                        Data = mergedData,
                        Variables = context.Variables,
                        PreviousResults = context.PreviousResults
                    },
                    TriggerData = resultData,
                    TriggerSource = "Execute"
                });

                Status = NodeStatus.Completed;
                var result = NodeResult.CreateSuccess(resultData);
                result.ExecutionTime = DateTime.UtcNow - startTime;
                return result;
            }
            catch (OperationCanceledException)
            {
                Status = NodeStatus.Cancelled;
                return NodeResult.CreateFailure("Manual trigger execution was cancelled");
            }
            catch (Exception ex)
            {
                OnError(ex, "Failed to execute manual trigger");
                return NodeResult.CreateFailure($"Manual trigger failed: {ex.Message}", ex);
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
                ["description"] = "Manual trigger nodes don't require input data",
                ["properties"] = new Dictionary<string, object>
                {
                    ["triggerData"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["description"] = "Optional trigger metadata",
                        ["additionalProperties"] = true
                    },
                    ["userInput"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["description"] = "Optional user-provided input data",
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
                        ["description"] = "Whether the trigger was activated"
                    },
                    ["triggerType"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "The type of trigger that was activated"
                    },
                    ["executionCount"] = new Dictionary<string, object>
                    {
                        ["type"] = "integer",
                        ["description"] = "Total number of times this trigger has been activated"
                    },
                    ["triggerTime"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["format"] = "date-time",
                        ["description"] = "The time when the trigger was activated"
                    },
                    ["triggerData"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["description"] = "Additional trigger configuration and state data"
                    },
                    ["workflowData"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["description"] = "Combined context and payload data for workflow execution"
                    }
                }
            };
        }
        #endregion

        #region Manual Trigger Methods
        /// <summary>
        /// Manually triggers the workflow execution.
        /// </summary>
        /// <param name="source">The source of the trigger (Button, Keyboard, API, etc.).</param>
        /// <param name="additionalData">Additional data to include with the trigger.</param>
        /// <returns>True if trigger was successful.</returns>
        public async Task<bool> TriggerAsync(string source = "Manual", Dictionary<string, object> additionalData = null)
        {
            if (!CanTrigger())
                return false;

            try
            {
                var context = new Model.ExecutionContext("manual-workflow", Guid.NewGuid().ToString())
                {
                    Data = additionalData ?? new Dictionary<string, object>(),
                    Variables = new Dictionary<string, object>
                    {
                        ["triggerSource"] = source,
                        ["triggerTime"] = DateTime.UtcNow
                    },
                    PreviousResults = new List<object>()
                };

                var result = await ExecuteAsync(context, CancellationToken.None);
                return result.IsSuccess;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Triggers the workflow when triggered by button click.
        /// </summary>
        /// <returns>True if trigger was successful.</returns>
        public async Task<bool> TriggerFromButtonAsync()
        {
            return await TriggerAsync("Button");
        }

        /// <summary>
        /// Triggers the workflow when triggered by keyboard shortcut.
        /// </summary>
        /// <returns>True if trigger was successful.</returns>
        public async Task<bool> TriggerFromKeyboardAsync()
        {
            return await TriggerAsync("Keyboard");
        }

        /// <summary>
        /// Resets the trigger to be available for activation again.
        /// </summary>
        public void Reset()
        {
            IsTriggerable = true;
        }

        /// <summary>
        /// Sets the trigger payload data.
        /// </summary>
        /// <param name="payload">The payload data to set.</param>
        public void SetPayload(Dictionary<string, object> payload)
        {
            TriggerPayload = payload ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Checks if the trigger can currently be activated.
        /// </summary>
        /// <returns>True if the trigger can be activated.</returns>
        public bool CanTrigger()
        {
            return IsTriggerable && IsEnabled && !IsInCooldown;
        }

        /// <summary>
        /// Requests confirmation from the user if required.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if confirmed or confirmation not required.</returns>
        private async Task<bool> RequestConfirmation(CancellationToken cancellationToken)
        {
            if (!RequireConfirmation)
                return true;

            var confirmationArgs = new TriggerConfirmationEventArgs
            {
                Message = ConfirmationMessage,
                IsConfirmed = false
            };

            OnConfirmationRequested(confirmationArgs);

            // Wait for confirmation response (with timeout)
            var timeout = TimeSpan.FromSeconds(30);
            var start = DateTime.UtcNow;
            
            while (!confirmationArgs.IsResponseReceived && DateTime.UtcNow - start < timeout)
            {
                await Task.Delay(100, cancellationToken);
            }

            return confirmationArgs.IsConfirmed;
        }

        /// <summary>
        /// Validates keyboard shortcut format.
        /// </summary>
        /// <param name="shortcut">The keyboard shortcut to validate.</param>
        /// <returns>True if valid.</returns>
        private bool IsValidKeyboardShortcut(string shortcut)
        {
            if (string.IsNullOrWhiteSpace(shortcut))
                return true; // Empty is valid

            // Basic validation for common shortcut patterns
            var validModifiers = new[] { "Ctrl", "Alt", "Shift", "Meta", "Cmd" };
            var parts = shortcut.Split('+');
            
            if (parts.Length < 2)
                return false; // Need at least modifier + key

            // Check modifiers
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (!validModifiers.Contains(parts[i].Trim(), StringComparer.OrdinalIgnoreCase))
                    return false;
            }

            // Last part should be the key
            var key = parts[parts.Length - 1].Trim();
            return !string.IsNullOrWhiteSpace(key);
        }
        #endregion

        #region Event Handling
        /// <summary>
        /// Raises the ManuallyTriggered event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnManuallyTriggered(ManualTriggerEventArgs e)
        {
            ManuallyTriggered?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the ConfirmationRequested event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnConfirmationRequested(TriggerConfirmationEventArgs e)
        {
            ConfirmationRequested?.Invoke(this, e);
        }
        #endregion

        #region Mouse Interaction
        /// <summary>
        /// Handles mouse down events for trigger activation.
        /// </summary>
        /// <param name="point">The mouse position.</param>
        /// <param name="context">The interaction context.</param>
        protected override bool OnMouseDown(SKPoint point, InteractionContext context)
        {
            var handled = base.OnMouseDown(point, context);

            // Check if click is within trigger button area
            if (IsPointInTriggerButton(point) && CanTrigger())
            {
                // Fire and forget - don't await in mouse event handler
                _ = TriggerFromButtonAsync();
                return true;
            }

            return handled;
        }

        /// <summary>
        /// Checks if a point is within the trigger button area.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>True if point is in trigger button.</returns>
        private bool IsPointInTriggerButton(SKPoint point)
        {
            // Use the entire node bounds as the trigger button
            return Bounds.Contains(point);
        }
        #endregion

        #region Visual Customization
        /// <summary>
        /// Draws the manual trigger node with custom styling.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The node bounds.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawNodeContent(SKCanvas canvas, SKRect bounds, DrawingContext context)
        {
            // Draw trigger button
            DrawTriggerButton(canvas, bounds);

            // Draw keyboard shortcut if specified
            if (!string.IsNullOrWhiteSpace(KeyboardShortcut))
            {
                DrawKeyboardShortcut(canvas, bounds);
            }

            // Draw execution count
            DrawExecutionInfo(canvas, bounds);

            // Draw cooldown indicator if in cooldown
            if (IsInCooldown)
            {
                DrawCooldownIndicator(canvas, bounds);
            }
        }

        /// <summary>
        /// Draws the trigger button.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The node bounds.</param>
        private void DrawTriggerButton(SKCanvas canvas, SKRect bounds)
        {
            // Determine button state
            var canTrigger = CanTrigger();
            var buttonColor = canTrigger ? SKColor.Parse("#4CAF50") : SKColor.Parse("#BDBDBD");
            var textColor = canTrigger ? SKColors.White : SKColor.Parse("#757575");

            // Draw button background
            using var buttonPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = buttonColor
            };

            var buttonRect = new SKRect(bounds.Left + 8, bounds.Top + 8, bounds.Right - 8, bounds.Bottom - 20);
            canvas.DrawRoundRect(buttonRect, 4, 4, buttonPaint);

            // Draw button border
            using var borderPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = buttonColor.WithAlpha(200),
                StrokeWidth = 1
            };
            canvas.DrawRoundRect(buttonRect, 4, 4, borderPaint);

            // Draw button text
            using var font = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), 11);
            using var textPaint = new SKPaint
            {
                IsAntialias = true,
                Color = textColor
            };

            var textWidth = font.MeasureText(TriggerText);
            var textX = buttonRect.MidX - textWidth / 2;
            var textY = buttonRect.MidY + 3;
            canvas.DrawText(TriggerText, textX, textY, font, textPaint);

            // Draw play icon
            if (canTrigger)
            {
                DrawPlayIcon(canvas, new SKPoint(bounds.Left + 16, bounds.Top + 16), 8);
            }
        }

        /// <summary>
        /// Draws a play icon.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="center">The center point of the icon.</param>
        /// <param name="size">The size of the icon.</param>
        private void DrawPlayIcon(SKCanvas canvas, SKPoint center, float size)
        {
            using var iconPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = SKColors.White
            };

            using var path = new SKPath();
            path.MoveTo(center.X - size / 3, center.Y - size / 2);
            path.LineTo(center.X + size / 2, center.Y);
            path.LineTo(center.X - size / 3, center.Y + size / 2);
            path.Close();

            canvas.DrawPath(path, iconPaint);
        }

        /// <summary>
        /// Draws the keyboard shortcut display.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The node bounds.</param>
        private void DrawKeyboardShortcut(SKCanvas canvas, SKRect bounds)
        {
            using var font = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 8);
            using var textPaint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColor.Parse("#666666")
            };

            var shortcutText = KeyboardShortcut;
            var textWidth = font.MeasureText(shortcutText);
            var textX = bounds.Right - textWidth - 4;
            var textY = bounds.Top + 12;
            canvas.DrawText(shortcutText, textX, textY, font, textPaint);
        }

        /// <summary>
        /// Draws execution information.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The node bounds.</param>
        private void DrawExecutionInfo(SKCanvas canvas, SKRect bounds)
        {
            using var font = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 8);
            using var textPaint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColor.Parse("#666666")
            };

            var infoText = $"Executed: {_executionCount} times";
            if (_lastExecution != DateTime.MinValue)
            {
                infoText += $" | Last: {_lastExecution:HH:mm:ss}";
            }

            var textWidth = font.MeasureText(infoText);
            var textX = bounds.MidX - textWidth / 2;
            var textY = bounds.Bottom - 6;
            canvas.DrawText(infoText, textX, textY, font, textPaint);
        }

        /// <summary>
        /// Draws the cooldown indicator.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The node bounds.</param>
        private void DrawCooldownIndicator(SKCanvas canvas, SKRect bounds)
        {
            var remaining = CooldownRemaining;
            var progress = 1.0f - (float)(remaining.TotalMilliseconds / CooldownPeriod.TotalMilliseconds);

            // Draw cooldown progress bar
            using var bgPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = SKColor.Parse("#FFE0E0")
            };

            using var progressPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = SKColor.Parse("#FF5722")
            };

            var barRect = new SKRect(bounds.Left + 8, bounds.Bottom - 4, bounds.Right - 8, bounds.Bottom - 2);
            canvas.DrawRect(barRect, bgPaint);

            var progressRect = new SKRect(barRect.Left, barRect.Top, 
                                        barRect.Left + barRect.Width * progress, barRect.Bottom);
            canvas.DrawRect(progressRect, progressPaint);

            // Draw cooldown text
            using var font = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 7);
            using var textPaint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColor.Parse("#D32F2F")
            };

            var cooldownText = $"Cooldown: {remaining.TotalSeconds:F0}s";
            var textX = bounds.Left + 10;
            var textY = bounds.Bottom - 8;
            canvas.DrawText(cooldownText, textX, textY, font, textPaint);
        }

        /// <summary>
        /// Gets the background color for manual trigger nodes.
        /// </summary>
        /// <returns>Background color with trigger styling.</returns>
        protected override SKColor GetBackgroundColor()
        {
            if (!IsEnabled)
                return SKColors.LightGray.WithAlpha(128);

            if (IsInCooldown)
                return SKColor.Parse("#FFEBEE"); // Light red for cooldown

            if (!IsTriggerable)
                return SKColor.Parse("#F5F5F5"); // Light gray for disabled

            return Status switch
            {
                NodeStatus.Executing => SKColor.Parse("#E8F5E8"), // Light green for executing
                NodeStatus.Completed => SKColor.Parse("#E8F5E8"), // Light green for completed
                NodeStatus.Failed => SKColor.Parse("#FFEBEE"), // Light red for failed
                NodeStatus.Cancelled => SKColor.Parse("#F3E5F5"), // Light purple for cancelled
                _ => SKColor.Parse("#E3F2FD") // Light blue default for manual triggers
            };
        }
        #endregion
    }

    #region Event Arguments
    /// <summary>
    /// Event arguments for manual trigger events.
    /// </summary>
    public class ManualTriggerEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the time when the trigger was activated.
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

        /// <summary>
        /// Gets or sets the source of the trigger (Button, Keyboard, API, etc.).
        /// </summary>
        public string TriggerSource { get; set; }
    }

    /// <summary>
    /// Event arguments for trigger confirmation requests.
    /// </summary>
    public class TriggerConfirmationEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the confirmation message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets whether the trigger was confirmed.
        /// </summary>
        public bool IsConfirmed { get; set; }

        /// <summary>
        /// Gets or sets whether a response has been received.
        /// </summary>
        public bool IsResponseReceived { get; set; }

        /// <summary>
        /// Confirms the trigger activation.
        /// </summary>
        public void Confirm()
        {
            IsConfirmed = true;
            IsResponseReceived = true;
        }

        /// <summary>
        /// Cancels the trigger activation.
        /// </summary>
        public void Cancel()
        {
            IsConfirmed = false;
            IsResponseReceived = true;
        }
    }
    #endregion
}