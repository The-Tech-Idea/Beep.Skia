using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Beep.Skia.Model;

namespace Beep.Skia.Components
{
    /// <summary>
    /// Automation node that provides conditional branching logic for workflow automation.
    /// Evaluates conditions and directs flow to different paths based on results.
    /// </summary>
    public class ConditionalNode : AutomationNode
    {
        public ConditionalNode()
        {
            UpsertNodeProperty("LogicOperator", typeof(string), _logicOperator, "AND | OR");
            UpsertNodeProperty("DefaultPath", typeof(string), _defaultPath, "true | false | custom");
            UpsertNodeProperty("EvaluateAllConditions", typeof(bool), _evaluateAllConditions);
            UpsertNodeProperty("AllowCustomPaths", typeof(bool), _allowCustomPaths);
        }
        #region Private Fields
        private List<ConditionRule> _conditions = new List<ConditionRule>();
        private string _logicOperator = "AND";
        private string _defaultPath = "false";
        private bool _evaluateAllConditions = true;
        private Dictionary<string, List<IAutomationNode>> _conditionalPaths = new Dictionary<string, List<IAutomationNode>>();
        private bool _allowCustomPaths = false;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the list of conditions to evaluate.
        /// </summary>
        public List<ConditionRule> Conditions
        {
            get => _conditions;
            set
            {
                _conditions = value ?? new List<ConditionRule>();
                Configuration["Conditions"] = _conditions;
            }
        }

        /// <summary>
        /// Gets or sets the logic operator to combine conditions (AND, OR).
        /// </summary>
        public string LogicOperator
        {
            get => _logicOperator;
            set
            {
                if (_logicOperator != value)
                {
                    _logicOperator = value?.ToUpperInvariant() ?? "AND";
                    Configuration["LogicOperator"] = _logicOperator;
                    UpsertNodeProperty("LogicOperator", typeof(string), _logicOperator);
                }
            }
        }

        /// <summary>
        /// Gets or sets the default path when conditions don't match (true, false, custom path name).
        /// </summary>
        public string DefaultPath
        {
            get => _defaultPath;
            set
            {
                if (_defaultPath != value)
                {
                    _defaultPath = value ?? "false";
                    Configuration["DefaultPath"] = _defaultPath;
                    UpsertNodeProperty("DefaultPath", typeof(string), _defaultPath);
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to evaluate all conditions or stop at first match.
        /// </summary>
        public bool EvaluateAllConditions
        {
            get => _evaluateAllConditions;
            set
            {
                if (_evaluateAllConditions != value)
                {
                    _evaluateAllConditions = value;
                    Configuration["EvaluateAllConditions"] = value;
                    UpsertNodeProperty("EvaluateAllConditions", typeof(bool), _evaluateAllConditions);
                }
            }
        }

        /// <summary>
        /// Gets or sets the conditional execution paths.
        /// </summary>
        public Dictionary<string, List<IAutomationNode>> ConditionalPaths
        {
            get => _conditionalPaths;
            set
            {
                _conditionalPaths = value ?? new Dictionary<string, List<IAutomationNode>>();
                Configuration["ConditionalPaths"] = _conditionalPaths;
            }
        }

        /// <summary>
        /// Gets or sets whether custom named paths are allowed beyond true/false.
        /// </summary>
        public bool AllowCustomPaths
        {
            get => _allowCustomPaths;
            set
            {
                if (_allowCustomPaths != value)
                {
                    _allowCustomPaths = value;
                    Configuration["AllowCustomPaths"] = value;
                    UpsertNodeProperty("AllowCustomPaths", typeof(bool), _allowCustomPaths);
                }
            }
        }
        #endregion

        #region AutomationNode Implementation
        /// <summary>
        /// Gets the description of what this automation node does.
        /// </summary>
        public override string Description => "Evaluates conditions and controls workflow branching based on logical rules";

        /// <summary>
        /// Gets the type of this automation node.
        /// </summary>
        public override NodeType NodeType => NodeType.Conditional;

        /// <summary>
        /// Initializes the conditional node with the specified configuration.
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
                
                LogicOperator = Configuration.GetValueOrDefault("LogicOperator", "AND")?.ToString() ?? "AND";
                DefaultPath = Configuration.GetValueOrDefault("DefaultPath", "false")?.ToString() ?? "false";
                EvaluateAllConditions = Convert.ToBoolean(Configuration.GetValueOrDefault("EvaluateAllConditions", true));
                AllowCustomPaths = Convert.ToBoolean(Configuration.GetValueOrDefault("AllowCustomPaths", false));

                if (Configuration.TryGetValue("Conditions", out var conditionsObj) && conditionsObj is List<ConditionRule> conditionsList)
                {
                    Conditions = conditionsList;
                }

                if (Configuration.TryGetValue("ConditionalPaths", out var pathsObj) && pathsObj is Dictionary<string, List<IAutomationNode>> pathsDict)
                {
                    ConditionalPaths = pathsDict;
                }

                Status = NodeStatus.Idle;
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                OnError(ex, "Failed to initialize ConditionalNode");
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Validates the current configuration and input data for this node.
        /// </summary>
        /// <param name="inputData">The input data to validate.</param>
        /// <param name="cancellationToken">Token to cancel the validation.</param>
        /// <returns>A task containing the validation result.</returns>
        public override async Task<ValidationResult> ValidateAsync(Model.ExecutionContext inputData, CancellationToken cancellationToken = default)
        {
            var issues = new List<string>();

            // Validate logic operator
            var validOperators = new[] { "AND", "OR" };
            if (!validOperators.Contains(LogicOperator.ToUpperInvariant()))
                issues.Add($"Logic operator must be one of: {string.Join(", ", validOperators)}");

            // Validate conditions
            if (Conditions == null || !Conditions.Any())
                issues.Add("At least one condition must be specified");
            else
            {
                for (int i = 0; i < Conditions.Count; i++)
                {
                    var condition = Conditions[i];
                    if (string.IsNullOrWhiteSpace(condition.Field))
                        issues.Add($"Condition {i + 1}: Field is required");
                    if (string.IsNullOrWhiteSpace(condition.Operator))
                        issues.Add($"Condition {i + 1}: Operator is required");
                    
                    var validOperators2 = new[] { "equals", "notequals", "contains", "startswith", "endswith", 
                                                 "greaterthan", "lessthan", "greaterequal", "lessequal", 
                                                 "isnull", "isnotnull", "regex", "in", "notin" };
                    if (!validOperators2.Contains(condition.Operator.ToLowerInvariant()))
                        issues.Add($"Condition {i + 1}: Invalid operator '{condition.Operator}'");

                    if (condition.Operator.ToLowerInvariant() == "regex" && condition.Value != null)
                    {
                        try
                        {
                            new Regex(condition.Value.ToString());
                        }
                        catch
                        {
                            issues.Add($"Condition {i + 1}: Invalid regex pattern");
                        }
                    }
                }
            }

            // Validate default path
            if (string.IsNullOrWhiteSpace(DefaultPath))
                issues.Add("Default path cannot be empty");

            var result = issues.Count == 0 
                ? ValidationResult.Success()
                : ValidationResult.Failure(issues.ToArray());

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Executes the conditional logic and returns the result with the chosen path.
        /// </summary>
        /// <param name="context">The execution context containing input data and workflow state.</param>
        /// <param name="cancellationToken">Token to cancel the execution.</param>
        /// <returns>A task containing the execution result.</returns>
        public override async Task<NodeResult> ExecuteAsync(Model.ExecutionContext context, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                Status = NodeStatus.Executing;

                // Evaluate all conditions
                var conditionResults = new List<ConditionEvaluationResult>();
                var overallResult = false;

                if (Conditions?.Any() == true)
                {
                    foreach (var condition in Conditions)
                    {
                        var conditionResult = await EvaluateCondition(condition, context, cancellationToken);
                        conditionResults.Add(conditionResult);

                        // Short-circuit evaluation if not evaluating all conditions
                        if (!EvaluateAllConditions)
                        {
                            if (LogicOperator == "OR" && conditionResult.Result)
                            {
                                overallResult = true;
                                break;
                            }
                            if (LogicOperator == "AND" && !conditionResult.Result)
                            {
                                overallResult = false;
                                break;
                            }
                        }
                    }

                    // Calculate overall result based on logic operator
                    if (EvaluateAllConditions || conditionResults.Count == Conditions.Count)
                    {
                        overallResult = LogicOperator == "AND" 
                            ? conditionResults.All(r => r.Result)
                            : conditionResults.Any(r => r.Result);
                    }
                }

                // Determine execution path
                var chosenPath = DetermineExecutionPath(overallResult, conditionResults);

                // Create result data
                var resultData = new Dictionary<string, object>
                {
                    ["conditionResult"] = overallResult,
                    ["chosenPath"] = chosenPath,
                    ["conditionDetails"] = conditionResults.Select(cr => new 
                    {
                        condition = cr.Condition,
                        result = cr.Result,
                        actualValue = cr.ActualValue,
                        expectedValue = cr.ExpectedValue,
                        evaluationTime = cr.EvaluationTime
                    }).ToList(),
                    ["logicOperator"] = LogicOperator,
                    ["evaluatedAt"] = DateTime.UtcNow,
                    ["executionPath"] = chosenPath
                };

                // Add path-specific data if available
                if (ConditionalPaths.TryGetValue(chosenPath, out var pathNodes))
                {
                    resultData["pathNodeCount"] = pathNodes?.Count ?? 0;
                    resultData["pathNodes"] = pathNodes?.Select(n => n.Name).ToList() ?? new List<string>();
                }

                Status = NodeStatus.Completed;
                var result = NodeResult.CreateSuccess(resultData);
                result.ExecutionTime = DateTime.UtcNow - startTime;
                return result;
            }
            catch (OperationCanceledException)
            {
                Status = NodeStatus.Cancelled;
                return NodeResult.CreateFailure("Conditional evaluation was cancelled");
            }
            catch (Exception ex)
            {
                OnError(ex, "Failed to execute conditional logic");
                return NodeResult.CreateFailure($"Conditional evaluation failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the schema defining the input parameters this node expects.
        /// </summary>
        /// <returns>A dictionary describing the input schema.</returns>
        public override Dictionary<string, object> GetInputSchema()
        {
            return new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["data"] = new Dictionary<string, object>
                    {
                        ["description"] = "The data to evaluate conditions against",
                        ["type"] = "object",
                        ["additionalProperties"] = true
                    },
                    ["variables"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["description"] = "Workflow variables available for condition evaluation",
                        ["additionalProperties"] = true
                    },
                    ["previousResults"] = new Dictionary<string, object>
                    {
                        ["type"] = "array",
                        ["description"] = "Results from previous nodes in the workflow",
                        ["items"] = new Dictionary<string, object> { ["type"] = "object" }
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
                    ["conditionResult"] = new Dictionary<string, object>
                    {
                        ["type"] = "boolean",
                        ["description"] = "The overall result of condition evaluation"
                    },
                    ["chosenPath"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "The execution path that was chosen (true/false or custom path name)"
                    },
                    ["conditionDetails"] = new Dictionary<string, object>
                    {
                        ["type"] = "array",
                        ["description"] = "Detailed evaluation results for each condition",
                        ["items"] = new Dictionary<string, object>
                        {
                            ["type"] = "object",
                            ["properties"] = new Dictionary<string, object>
                            {
                                ["condition"] = new Dictionary<string, object> { ["type"] = "string" },
                                ["result"] = new Dictionary<string, object> { ["type"] = "boolean" },
                                ["actualValue"] = new Dictionary<string, object> { ["description"] = "The actual field value" },
                                ["expectedValue"] = new Dictionary<string, object> { ["description"] = "The expected value for comparison" }
                            }
                        }
                    },
                    ["executionPath"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "The path the workflow should take next"
                    }
                }
            };
        }
        #endregion

        #region Condition Evaluation
        /// <summary>
        /// Evaluates a single condition against the execution context.
        /// </summary>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="context">The execution context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The condition evaluation result.</returns>
        private async Task<ConditionEvaluationResult> EvaluateCondition(ConditionRule condition, Model.ExecutionContext context, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken); // Make method async-compatible

            var startTime = DateTime.UtcNow;
            
            try
            {
                // Get the field value from context
                var actualValue = GetFieldValue(context, condition.Field);
                var expectedValue = ResolveValue(condition.Value, context);

                // Evaluate the condition
                var result = EvaluateConditionLogic(actualValue, condition.Operator, expectedValue);

                return new ConditionEvaluationResult
                {
                    Condition = $"{condition.Field} {condition.Operator} {condition.Value}",
                    Result = result,
                    ActualValue = actualValue,
                    ExpectedValue = expectedValue,
                    EvaluationTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new ConditionEvaluationResult
                {
                    Condition = $"{condition.Field} {condition.Operator} {condition.Value}",
                    Result = false,
                    ActualValue = null,
                    ExpectedValue = condition.Value,
                    Error = ex.Message,
                    EvaluationTime = DateTime.UtcNow - startTime
                };
            }
        }

        /// <summary>
        /// Gets a field value from the execution context, supporting nested field access.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <param name="fieldPath">The field path (supports dot notation).</param>
        /// <returns>The field value.</returns>
        private object GetFieldValue(Model.ExecutionContext context, string fieldPath)
        {
            if (string.IsNullOrEmpty(fieldPath))
                return null;

            // Check for special context references
            if (fieldPath.StartsWith("@"))
            {
                return fieldPath.ToLowerInvariant() switch
                {
                    "@timestamp" => DateTime.UtcNow,
                    "@date" => DateTime.UtcNow.Date,
                    "@time" => DateTime.UtcNow.TimeOfDay,
                    "@random" => new Random().NextDouble(),
                    _ => null
                };
            }

            // Try to get from different parts of context
            var sources = new[]
            {
                context.Data,
                context.Variables,
                context.PreviousResults?.LastOrDefault() as Dictionary<string, object>
            };

            foreach (var source in sources.Where(s => s != null))
            {
                var value = GetNestedValue(source, fieldPath);
                if (value != null)
                    return value;
            }

            return null;
        }

        /// <summary>
        /// Gets a nested value from an object using dot notation.
        /// </summary>
        /// <param name="obj">The object to get the value from.</param>
        /// <param name="path">The field path.</param>
        /// <returns>The nested value.</returns>
        private object GetNestedValue(object obj, string path)
        {
            if (obj == null || string.IsNullOrEmpty(path))
                return null;

            var current = obj;
            var parts = path.Split('.');

            foreach (var part in parts)
            {
                if (current is Dictionary<string, object> dict)
                {
                    if (!dict.TryGetValue(part, out current))
                        return null;
                }
                else if (current is JsonElement jsonElement)
                {
                    if (jsonElement.TryGetProperty(part, out var property))
                        current = property;
                    else
                        return null;
                }
                else
                {
                    var property = current.GetType().GetProperty(part);
                    if (property == null)
                        return null;
                    current = property.GetValue(current);
                }
                
                if (current == null)
                    return null;
            }

            return current;
        }

        /// <summary>
        /// Resolves a value, handling variable references and expressions.
        /// </summary>
        /// <param name="value">The value to resolve.</param>
        /// <param name="context">The execution context.</param>
        /// <returns>The resolved value.</returns>
        private object ResolveValue(object value, Model.ExecutionContext context)
        {
            if (value == null)
                return null;

            var stringValue = value.ToString();
            
            // Handle variable references like {{variableName}}
            if (stringValue.StartsWith("{{") && stringValue.EndsWith("}}"))
            {
                var variableName = stringValue.Substring(2, stringValue.Length - 4);
                return GetFieldValue(context, variableName);
            }

            // Handle array values for 'in' and 'notin' operators
            if (stringValue.StartsWith("[") && stringValue.EndsWith("]"))
            {
                try
                {
                    return JsonSerializer.Deserialize<object[]>(stringValue);
                }
                catch
                {
                    return value;
                }
            }

            return value;
        }

        /// <summary>
        /// Evaluates the condition logic between actual and expected values.
        /// </summary>
        /// <param name="actualValue">The actual value.</param>
        /// <param name="op">The operator.</param>
        /// <param name="expectedValue">The expected value.</param>
        /// <returns>True if the condition is met.</returns>
        private bool EvaluateConditionLogic(object actualValue, string op, object expectedValue)
        {
            return op.ToLowerInvariant() switch
            {
                "equals" or "eq" or "=" => Equals(actualValue, expectedValue),
                "notequals" or "ne" or "!=" => !Equals(actualValue, expectedValue),
                "contains" => actualValue?.ToString().Contains(expectedValue?.ToString() ?? "") ?? false,
                "startswith" => actualValue?.ToString().StartsWith(expectedValue?.ToString() ?? "") ?? false,
                "endswith" => actualValue?.ToString().EndsWith(expectedValue?.ToString() ?? "") ?? false,
                "greaterthan" or "gt" or ">" => CompareNumeric(actualValue, expectedValue) > 0,
                "lessthan" or "lt" or "<" => CompareNumeric(actualValue, expectedValue) < 0,
                "greaterequal" or "gte" or ">=" => CompareNumeric(actualValue, expectedValue) >= 0,
                "lessequal" or "lte" or "<=" => CompareNumeric(actualValue, expectedValue) <= 0,
                "isnull" => actualValue == null,
                "isnotnull" => actualValue != null,
                "regex" => EvaluateRegex(actualValue, expectedValue),
                "in" => EvaluateIn(actualValue, expectedValue),
                "notin" => !EvaluateIn(actualValue, expectedValue),
                _ => false
            };
        }

        /// <summary>
        /// Compares two values numerically.
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>Comparison result (-1, 0, 1).</returns>
        private int CompareNumeric(object left, object right)
        {
            if (IsNumeric(left) && IsNumeric(right))
            {
                var leftNum = Convert.ToDouble(left);
                var rightNum = Convert.ToDouble(right);
                return leftNum.CompareTo(rightNum);
            }
            
            // Fall back to string comparison
            return string.Compare(left?.ToString(), right?.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Evaluates a regex condition.
        /// </summary>
        /// <param name="actualValue">The actual value.</param>
        /// <param name="pattern">The regex pattern.</param>
        /// <returns>True if the pattern matches.</returns>
        private bool EvaluateRegex(object actualValue, object pattern)
        {
            if (actualValue == null || pattern == null)
                return false;

            try
            {
                var regex = new Regex(pattern.ToString());
                return regex.IsMatch(actualValue.ToString());
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Evaluates an 'in' condition (value in array).
        /// </summary>
        /// <param name="actualValue">The actual value.</param>
        /// <param name="arrayValue">The array value.</param>
        /// <returns>True if the value is in the array.</returns>
        private bool EvaluateIn(object actualValue, object arrayValue)
        {
            if (actualValue == null || arrayValue == null)
                return false;

            if (arrayValue is object[] array)
            {
                return array.Any(item => Equals(actualValue, item));
            }

            if (arrayValue is IEnumerable<object> enumerable)
            {
                return enumerable.Any(item => Equals(actualValue, item));
            }

            // If not an array, treat as single value equality
            return Equals(actualValue, arrayValue);
        }

        /// <summary>
        /// Checks if a value is numeric.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>True if the value is numeric.</returns>
        private bool IsNumeric(object value)
        {
            return value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal ||
                   (value is string str && double.TryParse(str, out _));
        }

        /// <summary>
        /// Determines the execution path based on condition results.
        /// </summary>
        /// <param name="overallResult">The overall condition result.</param>
        /// <param name="conditionResults">Individual condition results.</param>
        /// <returns>The chosen execution path.</returns>
        private string DetermineExecutionPath(bool overallResult, List<ConditionEvaluationResult> conditionResults)
        {
            // Check for custom path logic
            if (AllowCustomPaths)
            {
                // Look for conditions with custom path specifications
                foreach (var result in conditionResults.Where(r => r.Result))
                {
                    if (ConditionalPaths.ContainsKey(result.Condition))
                    {
                        return result.Condition;
                    }
                }
            }

            // Default true/false logic
            if (overallResult)
            {
                return ConditionalPaths.ContainsKey("true") ? "true" : "true";
            }
            else
            {
                return ConditionalPaths.ContainsKey("false") ? "false" : DefaultPath;
            }
        }
        #endregion

        #region Visual Customization
        /// <summary>
        /// Draws the conditional node with custom styling.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The node bounds.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawNodeContent(SKCanvas canvas, SKRect bounds, DrawingContext context)
        {
            // Draw conditional diamond shape
            DrawConditionalShape(canvas, bounds);

            // Draw node title
            using var font = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), 12);
            using var textPaint = new SKPaint
            {
                IsAntialias = true,
                Color = GetTextColor()
            };

            var title = "Condition";
            var titleWidth = font.MeasureText(title);
            var titleX = bounds.MidX - titleWidth / 2;
            var titleY = bounds.MidY + 2;
            canvas.DrawText(title, titleX, titleY, font, textPaint);

            // Draw logic operator badge
            DrawLogicOperatorBadge(canvas, bounds);

            // Draw condition count
            using var countFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 9);
            var conditionCount = Conditions?.Count ?? 0;
            var countText = $"{conditionCount} rule{(conditionCount != 1 ? "s" : "")}";
            var countWidth = countFont.MeasureText(countText);
            var countX = bounds.MidX - countWidth / 2;
            var countY = bounds.Bottom - 8;
            canvas.DrawText(countText, countX, countY, countFont, textPaint);
        }

        /// <summary>
        /// Draws the diamond shape characteristic of conditional nodes.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The node bounds.</param>
        private void DrawConditionalShape(SKCanvas canvas, SKRect bounds)
        {
            var centerX = bounds.MidX;
            var centerY = bounds.MidY;
            var halfWidth = bounds.Width / 2 - 4;
            var halfHeight = bounds.Height / 2 - 4;

            using var path = new SKPath();
            path.MoveTo(centerX, bounds.Top + 4);           // Top
            path.LineTo(bounds.Right - 4, centerY);         // Right
            path.LineTo(centerX, bounds.Bottom - 4);        // Bottom
            path.LineTo(bounds.Left + 4, centerY);          // Left
            path.Close();

            using var outlinePaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = GetBorderColor(),
                StrokeWidth = 2
            };

            canvas.DrawPath(path, outlinePaint);
        }

        /// <summary>
        /// Draws the logic operator badge.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The node bounds.</param>
        private void DrawLogicOperatorBadge(SKCanvas canvas, SKRect bounds)
        {
            var badgeColor = LogicOperator == "AND" ? SKColor.Parse("#2196F3") : SKColor.Parse("#FF9800");
            
            using var badgePaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = badgeColor
            };

            // Draw operator badge
            var badgeSize = 16f;
            var badgeX = bounds.Right - badgeSize - 4;
            var badgeY = bounds.Top + 4;
            var badgeRect = new SKRect(badgeX, badgeY, badgeX + badgeSize, badgeY + badgeSize);

            canvas.DrawRoundRect(badgeRect, 2, 2, badgePaint);
            
            // Draw operator text
            using var opFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), 8);
            using var opTextPaint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.White
            };
            
            var opText = LogicOperator.Substring(0, Math.Min(2, LogicOperator.Length));
            var opWidth = opFont.MeasureText(opText);
            var opX = badgeX + (badgeSize - opWidth) / 2;
            var opY = badgeY + badgeSize / 2 + 2;
            canvas.DrawText(opText, opX, opY, opFont, opTextPaint);
        }

        /// <summary>
        /// Gets the background color for conditional nodes.
        /// </summary>
        /// <returns>Background color with conditional styling.</returns>
        protected override SKColor GetBackgroundColor()
        {
            if (!IsEnabled)
                return SKColors.LightGray.WithAlpha(128);

            return Status switch
            {
                NodeStatus.Executing => SKColor.Parse("#E3F2FD"), // Light blue for executing
                NodeStatus.Completed => SKColor.Parse("#E8F5E8"), // Light green for completed
                NodeStatus.Failed => SKColor.Parse("#FFEBEE"), // Light red for failed
                NodeStatus.Cancelled => SKColor.Parse("#F3E5F5"), // Light purple for cancelled
                _ => SKColor.Parse("#F0F4FF") // Very light blue default for conditionals
            };
        }

        /// <summary>
        /// Gets the border color for the conditional diamond.
        /// </summary>
        /// <returns>Border color based on node state.</returns>
        protected override SKColor GetBorderColor()
        {
            return Status switch
            {
                NodeStatus.Executing => SKColor.Parse("#1976D2"),
                NodeStatus.Completed => SKColor.Parse("#388E3C"),
                NodeStatus.Failed => SKColor.Parse("#D32F2F"),
                NodeStatus.Cancelled => SKColor.Parse("#7B1FA2"),
                _ => SKColor.Parse("#1976D2")
            };
        }
        #endregion
    }

    #region Supporting Classes
    /// <summary>
    /// Represents a condition rule for evaluation.
    /// </summary>
    public class ConditionRule
    {
        /// <summary>
        /// Gets or sets the field name or path to evaluate.
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// Gets or sets the comparison operator.
        /// </summary>
        public string Operator { get; set; }

        /// <summary>
        /// Gets or sets the value to compare against.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the custom path to take if this condition is true (optional).
        /// </summary>
        public string CustomPath { get; set; }
    }

    /// <summary>
    /// Represents the result of evaluating a single condition.
    /// </summary>
    public class ConditionEvaluationResult
    {
        /// <summary>
        /// Gets or sets the condition description.
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// Gets or sets the evaluation result.
        /// </summary>
        public bool Result { get; set; }

        /// <summary>
        /// Gets or sets the actual value that was evaluated.
        /// </summary>
        public object ActualValue { get; set; }

        /// <summary>
        /// Gets or sets the expected value for comparison.
        /// </summary>
        public object ExpectedValue { get; set; }

        /// <summary>
        /// Gets or sets any error that occurred during evaluation.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the time taken to evaluate this condition.
        /// </summary>
        public TimeSpan EvaluationTime { get; set; }
    }
    #endregion
}