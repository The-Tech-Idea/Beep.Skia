using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Beep.Skia.Model;

namespace Beep.Skia.Components
{
    /// <summary>
    /// Automation node that transforms and manipulates data as part of workflow automation.
    /// Supports filtering, mapping, aggregation, and custom transformations on input data.
    /// </summary>
    public class DataTransformNode : AutomationNode
    {
        #region Private Fields
        private string _transformType = "Map";
        private Dictionary<string, object> _transformConfig = new Dictionary<string, object>();
        private List<FieldMapping> _fieldMappings = new List<FieldMapping>();
        private List<FilterCondition> _filters = new List<FilterCondition>();
        private string _customScript = "";
        private string _outputFormat = "json";
        private bool _preserveOriginal = false;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the type of transformation to perform (Map, Filter, Aggregate, Custom).
        /// </summary>
        public string TransformType
        {
            get => _transformType;
            set
            {
                if (_transformType != value)
                {
                    _transformType = value ?? "Map";
                    Configuration["TransformType"] = _transformType;
                }
            }
        }

        /// <summary>
        /// Gets or sets the transformation configuration parameters.
        /// </summary>
        public Dictionary<string, object> TransformConfig
        {
            get => _transformConfig;
            set
            {
                _transformConfig = value ?? new Dictionary<string, object>();
                Configuration["TransformConfig"] = _transformConfig;
            }
        }

        /// <summary>
        /// Gets or sets the field mappings for data transformation.
        /// </summary>
        public List<FieldMapping> FieldMappings
        {
            get => _fieldMappings;
            set
            {
                _fieldMappings = value ?? new List<FieldMapping>();
                Configuration["FieldMappings"] = _fieldMappings;
            }
        }

        /// <summary>
        /// Gets or sets the filter conditions for data filtering.
        /// </summary>
        public List<FilterCondition> Filters
        {
            get => _filters;
            set
            {
                _filters = value ?? new List<FilterCondition>();
                Configuration["Filters"] = _filters;
            }
        }

        /// <summary>
        /// Gets or sets the custom transformation script (for Custom transform type).
        /// </summary>
        public string CustomScript
        {
            get => _customScript;
            set
            {
                if (_customScript != value)
                {
                    _customScript = value ?? "";
                    Configuration["CustomScript"] = _customScript;
                }
            }
        }

        /// <summary>
        /// Gets or sets the output format for the transformed data.
        /// </summary>
        public string OutputFormat
        {
            get => _outputFormat;
            set
            {
                if (_outputFormat != value)
                {
                    _outputFormat = value?.ToLowerInvariant() ?? "json";
                    Configuration["OutputFormat"] = _outputFormat;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to preserve the original data alongside the transformed data.
        /// </summary>
        public bool PreserveOriginal
        {
            get => _preserveOriginal;
            set
            {
                if (_preserveOriginal != value)
                {
                    _preserveOriginal = value;
                    Configuration["PreserveOriginal"] = value;
                }
            }
        }
        #endregion

        #region AutomationNode Implementation
        /// <summary>
        /// Gets the description of what this automation node does.
        /// </summary>
        public override string Description => "Transforms and manipulates data through mapping, filtering, aggregation, and custom transformations";

        /// <summary>
        /// Gets the type of this automation node.
        /// </summary>
        public override NodeType NodeType => NodeType.Transform;

        /// <summary>
        /// Initializes the data transform node with the specified configuration.
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
                
                TransformType = Configuration.GetValueOrDefault("TransformType", "Map")?.ToString() ?? "Map";
                CustomScript = Configuration.GetValueOrDefault("CustomScript", "")?.ToString() ?? "";
                OutputFormat = Configuration.GetValueOrDefault("OutputFormat", "json")?.ToString() ?? "json";
                PreserveOriginal = Convert.ToBoolean(Configuration.GetValueOrDefault("PreserveOriginal", false));

                if (Configuration.TryGetValue("TransformConfig", out var configObj) && configObj is Dictionary<string, object> configDict)
                {
                    TransformConfig = configDict;
                }

                if (Configuration.TryGetValue("FieldMappings", out var mappingsObj) && mappingsObj is List<FieldMapping> mappingsList)
                {
                    FieldMappings = mappingsList;
                }

                if (Configuration.TryGetValue("Filters", out var filtersObj) && filtersObj is List<FilterCondition> filtersList)
                {
                    Filters = filtersList;
                }

                Status = NodeStatus.Idle;
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                OnError(ex, "Failed to initialize DataTransformNode");
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

            // Validate transform type
            var validTransformTypes = new[] { "Map", "Filter", "Aggregate", "Custom" };
            if (!validTransformTypes.Contains(TransformType, StringComparer.OrdinalIgnoreCase))
                issues.Add($"Transform type must be one of: {string.Join(", ", validTransformTypes)}");

            // Validate based on transform type
            switch (TransformType.ToLowerInvariant())
            {
                case "map":
                    if (FieldMappings == null || !FieldMappings.Any())
                        issues.Add("Field mappings are required for Map transform type");
                    else
                    {
                        foreach (var mapping in FieldMappings)
                        {
                            if (string.IsNullOrWhiteSpace(mapping.TargetField))
                                issues.Add("All field mappings must have a target field");
                        }
                    }
                    break;

                case "filter":
                    if (Filters == null || !Filters.Any())
                        issues.Add("Filter conditions are required for Filter transform type");
                    else
                    {
                        foreach (var filter in Filters)
                        {
                            if (string.IsNullOrWhiteSpace(filter.Field))
                                issues.Add("All filter conditions must specify a field");
                            if (string.IsNullOrWhiteSpace(filter.Operator))
                                issues.Add("All filter conditions must specify an operator");
                        }
                    }
                    break;

                case "aggregate":
                    if (!TransformConfig.ContainsKey("GroupByFields") || !TransformConfig.ContainsKey("AggregateFields"))
                        issues.Add("Aggregate transform requires GroupByFields and AggregateFields in configuration");
                    break;

                case "custom":
                    if (string.IsNullOrWhiteSpace(CustomScript))
                        issues.Add("Custom script is required for Custom transform type");
                    break;
            }

            // Validate output format
            var validFormats = new[] { "json", "csv", "xml" };
            if (!validFormats.Contains(OutputFormat.ToLowerInvariant()))
                issues.Add($"Output format must be one of: {string.Join(", ", validFormats)}");

            var result = issues.Count == 0 
                ? ValidationResult.Success()
                : ValidationResult.Failure(issues.ToArray());

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Executes the data transformation and returns the results.
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

                // Get input data
                var inputData = ExtractInputData(context);
                if (inputData == null)
                {
                    return NodeResult.CreateFailure("No input data found for transformation");
                }

                // Perform transformation based on type
                var transformedData = await PerformTransformation(inputData, cancellationToken);

                // Format output
                var formattedOutput = FormatOutput(transformedData);

                // Create result data
                var resultData = new Dictionary<string, object>
                {
                    ["transformedData"] = formattedOutput,
                    ["transformType"] = TransformType,
                    ["inputRecordCount"] = GetRecordCount(inputData),
                    ["outputRecordCount"] = GetRecordCount(transformedData),
                    ["executedAt"] = DateTime.UtcNow,
                    ["outputFormat"] = OutputFormat
                };

                // Include original data if requested
                if (PreserveOriginal)
                {
                    resultData["originalData"] = inputData;
                }

                Status = NodeStatus.Completed;
                var result = NodeResult.CreateSuccess(resultData);
                result.ExecutionTime = DateTime.UtcNow - startTime;
                return result;
            }
            catch (OperationCanceledException)
            {
                Status = NodeStatus.Cancelled;
                return NodeResult.CreateFailure("Data transformation was cancelled");
            }
            catch (Exception ex)
            {
                OnError(ex, "Failed to execute data transformation");
                return NodeResult.CreateFailure($"Data transformation failed: {ex.Message}", ex);
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
                        ["description"] = "The data to transform (array of objects or single object)",
                        ["oneOf"] = new[]
                        {
                            new Dictionary<string, object> { ["type"] = "array", ["items"] = new Dictionary<string, object> { ["type"] = "object" } },
                            new Dictionary<string, object> { ["type"] = "object" }
                        }
                    },
                    ["transformParameters"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["description"] = "Runtime parameters for the transformation",
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
                    ["transformedData"] = new Dictionary<string, object>
                    {
                        ["description"] = "The transformed data result",
                        ["oneOf"] = new[]
                        {
                            new Dictionary<string, object> { ["type"] = "array" },
                            new Dictionary<string, object> { ["type"] = "object" },
                            new Dictionary<string, object> { ["type"] = "string" }
                        }
                    },
                    ["transformType"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "The type of transformation that was applied"
                    },
                    ["inputRecordCount"] = new Dictionary<string, object>
                    {
                        ["type"] = "integer",
                        ["description"] = "Number of input records processed"
                    },
                    ["outputRecordCount"] = new Dictionary<string, object>
                    {
                        ["type"] = "integer",
                        ["description"] = "Number of output records produced"
                    },
                    ["originalData"] = new Dictionary<string, object>
                    {
                        ["description"] = "Original input data (only if PreserveOriginal is true)"
                    }
                }
            };
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Extracts input data from the execution context.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <returns>The extracted input data.</returns>
        private object ExtractInputData(Model.ExecutionContext context)
        {
            // Try to get data from various sources
            if (context.Data.TryGetValue("data", out var data))
                return data;

            if (context.Data.TryGetValue("transformedData", out var transformedData))
                return transformedData;

            if (context.Data.TryGetValue("body", out var body))
                return body;

            // Return all data if no specific data field found
            return context.Data.Count > 0 ? context.Data : null;
        }

        /// <summary>
        /// Performs the data transformation based on the configured transform type.
        /// </summary>
        /// <param name="inputData">The input data to transform.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The transformed data.</returns>
        private async Task<object> PerformTransformation(object inputData, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken); // Make method async-compatible

            return TransformType.ToLowerInvariant() switch
            {
                "map" => PerformMapTransformation(inputData),
                "filter" => PerformFilterTransformation(inputData),
                "aggregate" => PerformAggregateTransformation(inputData),
                "custom" => PerformCustomTransformation(inputData),
                _ => inputData
            };
        }

        /// <summary>
        /// Performs field mapping transformation.
        /// </summary>
        /// <param name="inputData">The input data.</param>
        /// <returns>The mapped data.</returns>
        private object PerformMapTransformation(object inputData)
        {
            if (FieldMappings == null || !FieldMappings.Any())
                return inputData;

            if (inputData is IEnumerable<object> enumerable && !(inputData is string))
            {
                // Transform array of objects
                var results = new List<object>();
                foreach (var item in enumerable)
                {
                    results.Add(MapSingleObject(item));
                }
                return results;
            }
            else
            {
                // Transform single object
                return MapSingleObject(inputData);
            }
        }

        /// <summary>
        /// Maps a single object according to the field mappings.
        /// </summary>
        /// <param name="obj">The object to map.</param>
        /// <returns>The mapped object.</returns>
        private object MapSingleObject(object obj)
        {
            var result = new Dictionary<string, object>();
            
            if (obj is Dictionary<string, object> dict)
            {
                foreach (var mapping in FieldMappings)
                {
                    var value = GetFieldValue(dict, mapping.SourceField);
                    
                    // Apply transformation if specified
                    if (!string.IsNullOrWhiteSpace(mapping.Transform))
                    {
                        value = ApplyFieldTransform(value, mapping.Transform);
                    }
                    
                    result[mapping.TargetField] = value;
                }
            }
            else
            {
                // Try to use reflection for other object types
                var objType = obj.GetType();
                foreach (var mapping in FieldMappings)
                {
                    var property = objType.GetProperty(mapping.SourceField);
                    var value = property?.GetValue(obj);
                    
                    if (!string.IsNullOrWhiteSpace(mapping.Transform))
                    {
                        value = ApplyFieldTransform(value, mapping.Transform);
                    }
                    
                    result[mapping.TargetField] = value;
                }
            }

            return result;
        }

        /// <summary>
        /// Performs filter transformation.
        /// </summary>
        /// <param name="inputData">The input data.</param>
        /// <returns>The filtered data.</returns>
        private object PerformFilterTransformation(object inputData)
        {
            if (Filters == null || !Filters.Any())
                return inputData;

            if (inputData is IEnumerable<object> enumerable && !(inputData is string))
            {
                var results = new List<object>();
                foreach (var item in enumerable)
                {
                    if (EvaluateFilters(item))
                    {
                        results.Add(item);
                    }
                }
                return results;
            }
            else
            {
                return EvaluateFilters(inputData) ? inputData : null;
            }
        }

        /// <summary>
        /// Performs aggregate transformation.
        /// </summary>
        /// <param name="inputData">The input data.</param>
        /// <returns>The aggregated data.</returns>
        private object PerformAggregateTransformation(object inputData)
        {
            if (!(inputData is IEnumerable<object> enumerable) || inputData is string)
                return inputData;

            var groupByFields = TransformConfig.GetValueOrDefault("GroupByFields") as List<string> ?? new List<string>();
            var aggregateFields = TransformConfig.GetValueOrDefault("AggregateFields") as Dictionary<string, string> ?? new Dictionary<string, string>();

            var groups = new Dictionary<string, List<object>>();

            // Group the data
            foreach (var item in enumerable)
            {
                var groupKey = GenerateGroupKey(item, groupByFields);
                if (!groups.ContainsKey(groupKey))
                {
                    groups[groupKey] = new List<object>();
                }
                groups[groupKey].Add(item);
            }

            // Aggregate each group
            var results = new List<object>();
            foreach (var group in groups)
            {
                var aggregatedItem = new Dictionary<string, object>();
                
                // Add group by fields
                if (group.Value.Any() && group.Value.First() is Dictionary<string, object> firstItem)
                {
                    foreach (var field in groupByFields)
                    {
                        aggregatedItem[field] = firstItem.GetValueOrDefault(field);
                    }
                }

                // Add aggregated fields
                foreach (var aggField in aggregateFields)
                {
                    var values = group.Value.Select(item => GetFieldValue(item, aggField.Key))
                                          .Where(v => v != null && IsNumeric(v))
                                          .Select(v => Convert.ToDouble(v))
                                          .ToList();

                    aggregatedItem[aggField.Key] = aggField.Value.ToLowerInvariant() switch
                    {
                        "sum" => values.Sum(),
                        "avg" or "average" => values.Any() ? values.Average() : 0,
                        "min" => values.Any() ? values.Min() : 0,
                        "max" => values.Any() ? values.Max() : 0,
                        "count" => group.Value.Count,
                        _ => group.Value.Count
                    };
                }

                results.Add(aggregatedItem);
            }

            return results;
        }

        /// <summary>
        /// Performs custom transformation using the provided script.
        /// </summary>
        /// <param name="inputData">The input data.</param>
        /// <returns>The transformed data.</returns>
        private object PerformCustomTransformation(object inputData)
        {
            // Simplified custom transformation - in a real implementation would use a proper scripting engine
            // For now, support basic operations via simple string replacement
            
            if (string.IsNullOrWhiteSpace(CustomScript))
                return inputData;

            try
            {
                // This is a very basic implementation - in production would use something like:
                // - Roslyn for C# expressions
                // - JavaScript engine for JS scripts
                // - Python.NET for Python scripts
                
                var script = CustomScript.ToLowerInvariant();
                
                if (inputData is IEnumerable<object> enumerable && !(inputData is string))
                {
                    var list = enumerable.ToList();
                    
                    if (script.Contains("reverse"))
                    {
                        list.Reverse();
                        return list;
                    }
                    
                    if (script.Contains("first"))
                    {
                        var match = Regex.Match(script, @"first\((\d+)\)");
                        if (match.Success && int.TryParse(match.Groups[1].Value, out var count))
                        {
                            return list.Take(count).ToList();
                        }
                        return list.FirstOrDefault();
                    }
                    
                    if (script.Contains("last"))
                    {
                        var match = Regex.Match(script, @"last\((\d+)\)");
                        if (match.Success && int.TryParse(match.Groups[1].Value, out var count))
                        {
                            return list.TakeLast(count).ToList();
                        }
                        return list.LastOrDefault();
                    }
                }

                return inputData;
            }
            catch
            {
                return inputData;
            }
        }

        /// <summary>
        /// Gets a field value from an object, supporting nested field access.
        /// </summary>
        /// <param name="obj">The object to get the value from.</param>
        /// <param name="fieldPath">The field path (supports dot notation).</param>
        /// <returns>The field value.</returns>
        private object GetFieldValue(object obj, string fieldPath)
        {
            if (obj == null || string.IsNullOrEmpty(fieldPath))
                return null;

            var current = obj;
            var parts = fieldPath.Split('.');

            foreach (var part in parts)
            {
                if (current is Dictionary<string, object> dict)
                {
                    if (!dict.TryGetValue(part, out current))
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
        /// Applies a field transformation to a value.
        /// </summary>
        /// <param name="value">The value to transform.</param>
        /// <param name="transform">The transformation to apply.</param>
        /// <returns>The transformed value.</returns>
        private object ApplyFieldTransform(object value, string transform)
        {
            if (value == null)
                return null;

            return transform.ToLowerInvariant() switch
            {
                "upper" => value.ToString().ToUpperInvariant(),
                "lower" => value.ToString().ToLowerInvariant(),
                "trim" => value.ToString().Trim(),
                "string" => value.ToString(),
                "int" => IsNumeric(value) ? Convert.ToInt32(value) : 0,
                "double" => IsNumeric(value) ? Convert.ToDouble(value) : 0.0,
                "bool" => Convert.ToBoolean(value),
                _ => value
            };
        }

        /// <summary>
        /// Evaluates filter conditions against an object.
        /// </summary>
        /// <param name="obj">The object to evaluate.</param>
        /// <returns>True if the object passes all filters.</returns>
        private bool EvaluateFilters(object obj)
        {
            foreach (var filter in Filters)
            {
                var fieldValue = GetFieldValue(obj, filter.Field);
                if (!EvaluateCondition(fieldValue, filter.Operator, filter.Value))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Evaluates a single filter condition.
        /// </summary>
        /// <param name="fieldValue">The field value.</param>
        /// <param name="op">The operator.</param>
        /// <param name="filterValue">The filter value.</param>
        /// <returns>True if the condition is met.</returns>
        private bool EvaluateCondition(object fieldValue, string op, object filterValue)
        {
            return op.ToLowerInvariant() switch
            {
                "equals" or "eq" or "=" => Equals(fieldValue, filterValue),
                "notequals" or "ne" or "!=" => !Equals(fieldValue, filterValue),
                "contains" => fieldValue?.ToString().Contains(filterValue?.ToString() ?? "") ?? false,
                "startswith" => fieldValue?.ToString().StartsWith(filterValue?.ToString() ?? "") ?? false,
                "endswith" => fieldValue?.ToString().EndsWith(filterValue?.ToString() ?? "") ?? false,
                "greaterthan" or "gt" or ">" => CompareNumeric(fieldValue, filterValue) > 0,
                "lessthan" or "lt" or "<" => CompareNumeric(fieldValue, filterValue) < 0,
                "isnull" => fieldValue == null,
                "isnotnull" => fieldValue != null,
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
            return 0;
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
        /// Generates a group key for aggregation.
        /// </summary>
        /// <param name="obj">The object to generate a key for.</param>
        /// <param name="groupByFields">The fields to group by.</param>
        /// <returns>The group key.</returns>
        private string GenerateGroupKey(object obj, List<string> groupByFields)
        {
            var keyParts = new List<string>();
            foreach (var field in groupByFields)
            {
                var value = GetFieldValue(obj, field);
                keyParts.Add(value?.ToString() ?? "null");
            }
            return string.Join("|", keyParts);
        }

        /// <summary>
        /// Gets the record count from data.
        /// </summary>
        /// <param name="data">The data to count.</param>
        /// <returns>The record count.</returns>
        private int GetRecordCount(object data)
        {
            if (data is IEnumerable<object> enumerable && !(data is string))
            {
                return enumerable.Count();
            }
            return data == null ? 0 : 1;
        }

        /// <summary>
        /// Formats the output data according to the specified format.
        /// </summary>
        /// <param name="data">The data to format.</param>
        /// <returns>The formatted output.</returns>
        private object FormatOutput(object data)
        {
            return OutputFormat.ToLowerInvariant() switch
            {
                "json" => data, // Already in object format
                "csv" => ConvertToCsv(data),
                "xml" => ConvertToXml(data),
                _ => data
            };
        }

        /// <summary>
        /// Converts data to CSV format.
        /// </summary>
        /// <param name="data">The data to convert.</param>
        /// <returns>CSV representation.</returns>
        private string ConvertToCsv(object data)
        {
            if (!(data is IEnumerable<object> enumerable) || data is string)
                return data?.ToString() ?? "";

            var list = enumerable.ToList();
            if (!list.Any())
                return "";

            var csv = new StringBuilder();
            
            // Get headers from first object
            if (list.First() is Dictionary<string, object> firstRecord)
            {
                csv.AppendLine(string.Join(",", firstRecord.Keys));
                
                foreach (var item in list)
                {
                    if (item is Dictionary<string, object> record)
                    {
                        var values = record.Values.Select(v => $"\"{v}\"");
                        csv.AppendLine(string.Join(",", values));
                    }
                }
            }

            return csv.ToString();
        }

        /// <summary>
        /// Converts data to XML format.
        /// </summary>
        /// <param name="data">The data to convert.</param>
        /// <returns>XML representation.</returns>
        private string ConvertToXml(object data)
        {
            // Simplified XML conversion
            var xml = new StringBuilder("<data>\n");
            
            if (data is IEnumerable<object> enumerable && !(data is string))
            {
                foreach (var item in enumerable)
                {
                    xml.AppendLine("  <record>");
                    if (item is Dictionary<string, object> dict)
                    {
                        foreach (var kvp in dict)
                        {
                            xml.AppendLine($"    <{kvp.Key}>{kvp.Value}</{kvp.Key}>");
                        }
                    }
                    xml.AppendLine("  </record>");
                }
            }
            else if (data is Dictionary<string, object> singleDict)
            {
                xml.AppendLine("  <record>");
                foreach (var kvp in singleDict)
                {
                    xml.AppendLine($"    <{kvp.Key}>{kvp.Value}</{kvp.Key}>");
                }
                xml.AppendLine("  </record>");
            }
            
            xml.AppendLine("</data>");
            return xml.ToString();
        }
        #endregion

        #region Visual Customization
        /// <summary>
        /// Draws the data transform node with custom styling.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The node bounds.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawNodeContent(SKCanvas canvas, SKRect bounds, DrawingContext context)
        {
            // Draw transform type badge
            DrawTransformBadge(canvas, bounds);

            // Draw node title
            using var font = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), 12);
            using var textPaint = new SKPaint
            {
                IsAntialias = true,
                Color = GetTextColor()
            };

            var title = "Transform";
            var titleWidth = font.MeasureText(title);
            var titleX = bounds.MidX - titleWidth / 2;
            var titleY = bounds.Top + 16;
            canvas.DrawText(title, titleX, titleY, font, textPaint);

            // Draw transform type
            using var typeFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 10);
            var typeWidth = typeFont.MeasureText(TransformType);
            var typeX = bounds.MidX - typeWidth / 2;
            var typeY = bounds.Bottom - 8;
            canvas.DrawText(TransformType, typeX, typeY, typeFont, textPaint);
        }

        /// <summary>
        /// Draws the transform type badge.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The node bounds.</param>
        private void DrawTransformBadge(SKCanvas canvas, SKRect bounds)
        {
            var badgeColor = GetTransformColor(TransformType);
            
            using var badgePaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = badgeColor
            };

            // Draw transform icon
            var iconSize = 12f;
            var iconX = bounds.Right - iconSize - 8;
            var iconY = bounds.Top + 8;
            var iconRect = new SKRect(iconX, iconY, iconX + iconSize, iconY + iconSize);

            // Draw gear-like shape for transformation
            canvas.DrawOval(iconRect, badgePaint);
            
            using var centerPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = SKColors.White
            };
            
            var centerSize = iconSize * 0.4f;
            var centerRect = new SKRect(iconX + (iconSize - centerSize) / 2, iconY + (iconSize - centerSize) / 2,
                                      iconX + (iconSize + centerSize) / 2, iconY + (iconSize + centerSize) / 2);
            canvas.DrawOval(centerRect, centerPaint);
        }

        /// <summary>
        /// Gets the color associated with a transform type.
        /// </summary>
        /// <param name="transformType">The transform type.</param>
        /// <returns>The color to use.</returns>
        private SKColor GetTransformColor(string transformType)
        {
            return transformType.ToLowerInvariant() switch
            {
                "map" => SKColor.Parse("#FF9800"),    // Orange
                "filter" => SKColor.Parse("#2196F3"), // Blue
                "aggregate" => SKColor.Parse("#4CAF50"), // Green
                "custom" => SKColor.Parse("#9C27B0"),  // Purple
                _ => SKColor.Parse("#607D8B")          // Blue Gray
            };
        }

        /// <summary>
        /// Gets the background color for transform nodes.
        /// </summary>
        /// <returns>Background color with transform styling.</returns>
        protected override SKColor GetBackgroundColor()
        {
            if (!IsEnabled)
                return SKColors.LightGray.WithAlpha(128);

            return Status switch
            {
                NodeStatus.Executing => SKColor.Parse("#FFF3E0"), // Light orange for executing
                NodeStatus.Completed => SKColor.Parse("#E8F5E8"), // Light green for completed
                NodeStatus.Failed => SKColor.Parse("#FFEBEE"), // Light red for failed
                NodeStatus.Cancelled => SKColor.Parse("#F3E5F5"), // Light purple for cancelled
                _ => SKColor.Parse("#FFF8E1") // Very light yellow default for transforms
            };
        }
        #endregion
    }

    #region Supporting Classes
    /// <summary>
    /// Represents a field mapping for data transformation.
    /// </summary>
    public class FieldMapping
    {
        /// <summary>
        /// Gets or sets the source field name or path.
        /// </summary>
        public string SourceField { get; set; }

        /// <summary>
        /// Gets or sets the target field name.
        /// </summary>
        public string TargetField { get; set; }

        /// <summary>
        /// Gets or sets the transformation to apply to the field value.
        /// </summary>
        public string Transform { get; set; }
    }

    /// <summary>
    /// Represents a filter condition for data filtering.
    /// </summary>
    public class FilterCondition
    {
        /// <summary>
        /// Gets or sets the field name to filter on.
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// Gets or sets the filter operator (equals, contains, etc.).
        /// </summary>
        public string Operator { get; set; }

        /// <summary>
        /// Gets or sets the value to filter by.
        /// </summary>
        public object Value { get; set; }
    }
    #endregion
}