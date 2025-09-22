using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Beep.Skia.Model;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Report;

namespace Beep.Skia.Components
{
    /// <summary>
    /// Automation node that provides access to BeepDataSources for use in workflow pipelines.
    /// Allows querying databases, files, APIs, and other data sources as part of automation workflows.
    /// </summary>
    public class DataSourceAutomationNode : AutomationNode
    {
        #region Private Fields
        private string _dataSourceName = "";
        private string _entityName = "";
        private string _queryString = "";
        private Dictionary<string, object> _queryParameters = new Dictionary<string, object>();
        private string _outputFormat = "json";
        private int _maxRows = 1000;
        private bool _useCache = false;
        private TimeSpan _cacheTimeout = TimeSpan.FromMinutes(5);
        private IDMEEditor _dmeEditor;
        private IDMLogger _logger;
        private IErrorsInfo _errorObject;
        #endregion

        #region Properties
        public string DataSourceName
        {
            get => _dataSourceName;
            set
            {
                if (_dataSourceName != value)
                {
                    _dataSourceName = value;
                    Configuration["DataSourceName"] = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the name of the entity (table, collection, etc.) to query.
        /// </summary>
        public string EntityName
        {
            get => _entityName;
            set
            {
                if (_entityName != value)
                {
                    _entityName = value;
                    Configuration["EntityName"] = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the query string to execute against the data source.
        /// Can be SQL, MongoDB query, REST endpoint, etc. depending on the data source type.
        /// </summary>
        public string QueryString
        {
            get => _queryString;
            set
            {
                if (_queryString != value)
                {
                    _queryString = value;
                    Configuration["QueryString"] = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the parameters for the query.
        /// </summary>
        public Dictionary<string, object> QueryParameters
        {
            get => _queryParameters;
            set
            {
                _queryParameters = value ?? new Dictionary<string, object>();
                Configuration["QueryParameters"] = _queryParameters;
            }
        }

        /// <summary>
        /// Gets or sets the output format for the data (json, xml, csv, etc.).
        /// </summary>
        public string OutputFormat
        {
            get => _outputFormat;
            set
            {
                if (_outputFormat != value)
                {
                    _outputFormat = value;
                    Configuration["OutputFormat"] = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of rows to return.
        /// </summary>
        public int MaxRows
        {
            get => _maxRows;
            set
            {
                if (_maxRows != value)
                {
                    _maxRows = Math.Max(1, value);
                    Configuration["MaxRows"] = _maxRows;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to use caching for query results.
        /// </summary>
        public bool UseCache
        {
            get => _useCache;
            set
            {
                if (_useCache != value)
                {
                    _useCache = value;
                    Configuration["UseCache"] = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the cache timeout duration.
        /// </summary>
        public TimeSpan CacheTimeout
        {
            get => _cacheTimeout;
            set
            {
                if (_cacheTimeout != value)
                {
                    _cacheTimeout = value;
                    Configuration["CacheTimeout"] = value;
                }
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the DataSourceAutomationNode class.
        /// </summary>
        public DataSourceAutomationNode()
        {
            // Initialize default values
            _queryParameters = new Dictionary<string, object>();
        }

        /// <summary>
        /// Initializes a new instance of the DataSourceAutomationNode class with data source manager dependencies.
        /// </summary>
        /// <param name="dmeEditor">The DME editor instance for data source management.</param>
        /// <param name="logger">The logger instance for logging operations.</param>
        /// <param name="errorObject">The error handler instance.</param>
        public DataSourceAutomationNode(IDMEEditor dmeEditor, IDMLogger logger, IErrorsInfo errorObject)
        {
            _dmeEditor = dmeEditor;
            _logger = logger;
            _errorObject = errorObject;
            _queryParameters = new Dictionary<string, object>();
        }

        /// <summary>
        /// Sets the data source manager dependencies for this node.
        /// </summary>
        /// <param name="dmeEditor">The DME editor instance for data source management.</param>
        /// <param name="logger">The logger instance for logging operations.</param>
        /// <param name="errorObject">The error handler instance.</param>
        public void SetDataSourceManager(IDMEEditor dmeEditor, IDMLogger logger, IErrorsInfo errorObject)
        {
            _dmeEditor = dmeEditor;
            _logger = logger;
            _errorObject = errorObject;
        }
        #endregion

        #region AutomationNode Implementation
        /// <summary>
        /// Gets the description of what this automation node does.
        /// </summary>
        public override string Description => "Retrieves data from configured BeepDataSources including databases, files, APIs, and other data sources";

        /// <summary>
        /// Gets the type of this automation node.
        /// </summary>
        public override NodeType NodeType => NodeType.DataSource;

        /// <summary>
        /// Initializes the data source automation node with the specified configuration.
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
                
                DataSourceName = Configuration.GetValueOrDefault("DataSourceName", "")?.ToString() ?? "";
                EntityName = Configuration.GetValueOrDefault("EntityName", "")?.ToString() ?? "";
                QueryString = Configuration.GetValueOrDefault("QueryString", "")?.ToString() ?? "";
                OutputFormat = Configuration.GetValueOrDefault("OutputFormat", "json")?.ToString() ?? "json";
                MaxRows = Convert.ToInt32(Configuration.GetValueOrDefault("MaxRows", 1000));
                UseCache = Convert.ToBoolean(Configuration.GetValueOrDefault("UseCache", false));
                
                if (Configuration.TryGetValue("CacheTimeout", out var timeoutObj))
                {
                    if (timeoutObj is TimeSpan ts)
                        CacheTimeout = ts;
                    else if (TimeSpan.TryParse(timeoutObj?.ToString(), out var parsed))
                        CacheTimeout = parsed;
                }

                if (Configuration.TryGetValue("QueryParameters", out var paramsObj) && paramsObj is Dictionary<string, object> paramDict)
                {
                    QueryParameters = paramDict;
                }

                Status = NodeStatus.Idle;
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                OnError(ex, "Failed to initialize DataSourceAutomationNode");
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

            // Validate required configuration
            if (string.IsNullOrWhiteSpace(DataSourceName))
                issues.Add("DataSourceName is required");

            if (string.IsNullOrWhiteSpace(EntityName) && string.IsNullOrWhiteSpace(QueryString))
                issues.Add("Either EntityName or QueryString must be specified");

            if (MaxRows <= 0)
                issues.Add("MaxRows must be greater than 0");

            var supportedFormats = new[] { "json", "xml", "csv", "raw" };
            if (!supportedFormats.Contains(OutputFormat.ToLowerInvariant()))
                issues.Add($"OutputFormat must be one of: {string.Join(", ", supportedFormats)}");

            // Validate data source exists (simulated - in real implementation would check BeepDataSources)
            if (!string.IsNullOrWhiteSpace(DataSourceName))
            {
                // TODO: In real implementation, check if data source exists in BeepDataSources
                // For now, we'll assume it's valid if not empty
            }

            var result = issues.Count == 0 
                ? ValidationResult.Success()
                : ValidationResult.Failure(issues.ToArray());

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Executes the data source query and returns the results.
        /// </summary>
        /// <param name="context">The execution context containing input data and workflow state.</param>
        /// <param name="cancellationToken">Token to cancel the execution.</param>
        /// <returns>A task containing the execution result.</returns>
        public override async Task<NodeResult> ExecuteAsync(Model.ExecutionContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                Status = NodeStatus.Executing;

                // Process input parameters from context
                var processedParameters = ProcessInputParameters(context);

                // Execute the data query (simulated - in real implementation would use BeepDataSources)
                var queryResult = await ExecuteDataQuery(processedParameters, cancellationToken);

                // Format the output
                var formattedOutput = FormatOutput(queryResult);

                // Create result data
                var resultData = new Dictionary<string, object>
                {
                    ["data"] = formattedOutput,
                    ["rowCount"] = queryResult?.Count() ?? 0,
                    ["dataSourceName"] = DataSourceName,
                    ["entityName"] = EntityName,
                    ["format"] = OutputFormat,
                    ["executedAt"] = DateTime.UtcNow
                };

                Status = NodeStatus.Completed;
                return NodeResult.CreateSuccess(resultData);
            }
            catch (OperationCanceledException)
            {
                Status = NodeStatus.Cancelled;
                return NodeResult.CreateFailure("Data query was cancelled");
            }
            catch (Exception ex)
            {
                OnError(ex, "Failed to execute data query");
                return NodeResult.CreateFailure($"Data query failed: {ex.Message}", ex);
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
                    ["parameters"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["description"] = "Query parameters to substitute in the query string",
                        ["additionalProperties"] = true
                    },
                    ["filters"] = new Dictionary<string, object>
                    {
                        ["type"] = "array",
                        ["description"] = "Additional filters to apply to the query",
                        ["items"] = new Dictionary<string, object>
                        {
                            ["type"] = "object",
                            ["properties"] = new Dictionary<string, object>
                            {
                                ["field"] = new Dictionary<string, object> { ["type"] = "string" },
                                ["operator"] = new Dictionary<string, object> { ["type"] = "string" },
                                ["value"] = new Dictionary<string, object> { ["type"] = "string" }
                            }
                        }
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
                    ["data"] = new Dictionary<string, object>
                    {
                        ["type"] = "array",
                        ["description"] = "The retrieved data records",
                        ["items"] = new Dictionary<string, object> { ["type"] = "object" }
                    },
                    ["rowCount"] = new Dictionary<string, object>
                    {
                        ["type"] = "integer",
                        ["description"] = "Number of rows returned"
                    },
                    ["dataSourceName"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "Name of the data source used"
                    },
                    ["entityName"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "Name of the entity queried"
                    },
                    ["format"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "Format of the returned data"
                    },
                    ["executedAt"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["format"] = "date-time",
                        ["description"] = "When the query was executed"
                    }
                }
            };
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Processes input parameters from the execution context.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <returns>Processed parameters for the query.</returns>
        private Dictionary<string, object> ProcessInputParameters(Model.ExecutionContext context)
        {
            var parameters = new Dictionary<string, object>(QueryParameters);

            // Merge with input data parameters
            if (context.Data.TryGetValue("parameters", out var inputParams) && inputParams is Dictionary<string, object> paramDict)
            {
                foreach (var kvp in paramDict)
                {
                    parameters[kvp.Key] = kvp.Value;
                }
            }

            // Add context variables as potential parameters
            foreach (var variable in context.Variables)
            {
                if (!parameters.ContainsKey(variable.Key))
                {
                    parameters[variable.Key] = variable.Value;
                }
            }

            return parameters;
        }

        /// <summary>
        /// Executes the data query against the configured data source.
        /// Uses the BeepDataSources system to retrieve real data.
        /// </summary>
        /// <param name="parameters">Query parameters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Query results.</returns>
        private async Task<IEnumerable<object>> ExecuteDataQuery(Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            // Check if data source manager is available
            if (_dmeEditor == null)
            {
                _logger?.LogWarning("DataSourceAutomationNode: DME Editor not configured, returning simulated data");
                return await GetSimulatedData(parameters, cancellationToken);
            }

            // Validate data source name
            if (string.IsNullOrEmpty(DataSourceName))
            {
                throw new InvalidOperationException("DataSourceName is required but not specified");
            }

            IDataSource dataSource = null;
            try
            {
                // Get the data source from the DME editor
                dataSource = _dmeEditor.GetDataSource(DataSourceName);
                if (dataSource == null)
                {
                    throw new InvalidOperationException($"Data source '{DataSourceName}' not found");
                }

                // Open connection
                var connectionResult = dataSource.Openconnection();
                if (connectionResult != System.Data.ConnectionState.Open)
                {
                    throw new InvalidOperationException($"Failed to open connection to data source '{DataSourceName}': {connectionResult}");
                }

                IEnumerable<object> results;

                // Determine query type and execute
                if (!string.IsNullOrEmpty(QueryString))
                {
                    // Execute raw SQL/query string
                    results = ExecuteQueryString(dataSource, parameters);
                }
                else if (!string.IsNullOrEmpty(EntityName))
                {
                    // Execute entity-based query
                    results = ExecuteEntityQuery(dataSource, parameters);
                }
                else
                {
                    throw new InvalidOperationException("Either QueryString or EntityName must be specified");
                }

                // Apply MaxRows limit if specified
                if (MaxRows > 0 && results is IList<object> list)
                {
                    results = list.Take(MaxRows);
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"DataSourceAutomationNode: Error executing query - {ex.Message}");
                throw;
            }
            finally
            {
                // Always close connection
                if (dataSource != null)
                {
                    try
                    {
                        dataSource.Closeconnection();
                    }
                    catch (Exception closeEx)
                    {
                        _logger?.LogWarning($"DataSourceAutomationNode: Error closing connection - {closeEx.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Executes a query string against the data source.
        /// </summary>
        /// <param name="dataSource">The data source instance.</param>
        /// <param name="parameters">Query parameters.</param>
        /// <returns>Query results.</returns>
        private IEnumerable<object> ExecuteQueryString(IDataSource dataSource, Dictionary<string, object> parameters)
        {
            // For query strings, we need to handle parameter substitution
            string processedQuery = QueryString;

            // Simple parameter substitution - replace {paramName} with values
            foreach (var param in parameters)
            {
                string placeholder = $"{{{param.Key}}}";
                if (processedQuery.Contains(placeholder))
                {
                    processedQuery = processedQuery.Replace(placeholder, param.Value?.ToString() ?? "");
                }
            }

            // Execute the query
            var result = dataSource.RunQuery(processedQuery);
            return result?.Cast<object>() ?? Enumerable.Empty<object>();
        }

        /// <summary>
        /// Executes an entity-based query against the data source.
        /// </summary>
        /// <param name="dataSource">The data source instance.</param>
        /// <param name="parameters">Query parameters.</param>
        /// <returns>Query results.</returns>
        private IEnumerable<object> ExecuteEntityQuery(IDataSource dataSource, Dictionary<string, object> parameters)
        {
            // Convert parameters to AppFilter format for entity queries
            var filters = new List<AppFilter>();

            foreach (var param in parameters)
            {
                filters.Add(new AppFilter
                {
                    FieldName = param.Key,
                    FilterValue = param.Value?.ToString() ?? "",
                    Operator = "="
                });
            }

            // Execute entity query
            var result = dataSource.GetEntity(EntityName, filters);
            return result?.Cast<object>() ?? Enumerable.Empty<object>();
        }

        /// <summary>
        /// Returns simulated data when data source manager is not available.
        /// </summary>
        /// <param name="parameters">Query parameters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Simulated query results.</returns>
        private async Task<IEnumerable<object>> GetSimulatedData(Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            // Simulate async data query delay
            await Task.Delay(100, cancellationToken);

            var sampleData = new List<object>();

            for (int i = 1; i <= Math.Min(MaxRows, 10); i++)
            {
                var record = new Dictionary<string, object>
                {
                    ["id"] = i,
                    ["dataSource"] = DataSourceName,
                    ["entity"] = EntityName,
                    ["timestamp"] = DateTime.UtcNow.AddMinutes(-i),
                    ["sampleValue"] = $"Sample data {i}",
                    ["numericValue"] = i * 10.5
                };

                // Add parameter values to the record for demonstration
                foreach (var param in parameters)
                {
                    record[$"param_{param.Key}"] = param.Value;
                }

                sampleData.Add(record);
            }

            return sampleData;
        }

        /// <summary>
        /// Formats the query results according to the specified output format.
        /// </summary>
        /// <param name="data">The raw query results.</param>
        /// <returns>Formatted output.</returns>
        private object FormatOutput(IEnumerable<object> data)
        {
            if (data == null)
                return null;

            return OutputFormat.ToLowerInvariant() switch
            {
                "json" => data.ToList(), // Already in object format suitable for JSON
                "xml" => ConvertToXml(data),
                "csv" => ConvertToCsv(data),
                "raw" => data,
                _ => data.ToList()
            };
        }

        /// <summary>
        /// Converts data to XML format (simulated).
        /// </summary>
        /// <param name="data">The data to convert.</param>
        /// <returns>XML representation.</returns>
        private string ConvertToXml(IEnumerable<object> data)
        {
            // Simplified XML conversion - in real implementation would use proper XML serialization
            var xml = "<data>\n";
            foreach (var item in data)
            {
                xml += "  <record>\n";
                if (item is Dictionary<string, object> dict)
                {
                    foreach (var kvp in dict)
                    {
                        xml += $"    <{kvp.Key}>{kvp.Value}</{kvp.Key}>\n";
                    }
                }
                xml += "  </record>\n";
            }
            xml += "</data>";
            return xml;
        }

        /// <summary>
        /// Converts data to CSV format (simulated).
        /// </summary>
        /// <param name="data">The data to convert.</param>
        /// <returns>CSV representation.</returns>
        private string ConvertToCsv(IEnumerable<object> data)
        {
            // Simplified CSV conversion - in real implementation would use proper CSV library
            var dataList = data.ToList();
            if (!dataList.Any())
                return "";

            var csv = "";
            if (dataList.First() is Dictionary<string, object> firstRecord)
            {
                // Header
                csv += string.Join(",", firstRecord.Keys) + "\n";
                
                // Data rows
                foreach (var item in dataList)
                {
                    if (item is Dictionary<string, object> dict)
                    {
                        csv += string.Join(",", dict.Values.Select(v => $"\"{v}\"")) + "\n";
                    }
                }
            }
            return csv;
        }
        #endregion

        #region Visual Customization
        /// <summary>
        /// Draws the data source node with custom styling to indicate it's a data source.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The node bounds.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawNodeContent(SKCanvas canvas, SKRect bounds, DrawingContext context)
        {
            // Draw data source icon/indicator
            DrawDataSourceIcon(canvas, bounds);

            // Draw node title
            using var font = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), 12);
            using var textPaint = new SKPaint
            {
                IsAntialias = true,
                Color = GetTextColor()
            };

            var title = "Data Source";
            var titleWidth = font.MeasureText(title);
            var titleX = bounds.MidX - titleWidth / 2;
            var titleY = bounds.Top + 16;
            canvas.DrawText(title, titleX, titleY, font, textPaint);

            // Draw data source name if configured
            if (!string.IsNullOrWhiteSpace(DataSourceName))
            {
                using var nameFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 10);
                var nameText = DataSourceName.Length > 15 ? DataSourceName.Substring(0, 12) + "..." : DataSourceName;
                var nameWidth = nameFont.MeasureText(nameText);
                var nameX = bounds.MidX - nameWidth / 2;
                var nameY = bounds.Top + 32;
                canvas.DrawText(nameText, nameX, nameY, nameFont, textPaint);
            }

            // Draw entity name if configured
            if (!string.IsNullOrWhiteSpace(EntityName))
            {
                using var entityFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Italic), 9);
                var entityText = EntityName.Length > 18 ? EntityName.Substring(0, 15) + "..." : EntityName;
                var entityWidth = entityFont.MeasureText(entityText);
                var entityX = bounds.MidX - entityWidth / 2;
                var entityY = bounds.Bottom - 8;
                canvas.DrawText(entityText, entityX, entityY, entityFont, textPaint);
            }
        }

        /// <summary>
        /// Draws a data source icon to visually identify this node type.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The node bounds.</param>
        private void DrawDataSourceIcon(SKCanvas canvas, SKRect bounds)
        {
            using var iconPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = SKColor.Parse("#4CAF50") // Green for data sources
            };

            // Draw a simple database-like icon
            var iconSize = 12f;
            var iconX = bounds.Right - iconSize - 8;
            var iconY = bounds.Top + 8;
            var iconRect = new SKRect(iconX, iconY, iconX + iconSize, iconY + iconSize);

            // Draw cylinder shape to represent database
            canvas.DrawOval(iconRect, iconPaint);
            
            // Draw a few horizontal lines to suggest data
            using var linePaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = SKColors.White,
                StrokeWidth = 1
            };

            for (int i = 1; i <= 3; i++)
            {
                var lineY = iconY + (iconSize / 4) * i;
                canvas.DrawLine(iconX + 2, lineY, iconX + iconSize - 2, lineY, linePaint);
            }
        }

        /// <summary>
        /// Gets the background color for data source nodes.
        /// </summary>
        /// <returns>Background color with data source styling.</returns>
        protected override SKColor GetBackgroundColor()
        {
            if (!IsEnabled)
                return SKColors.LightGray.WithAlpha(128);

            return Status switch
            {
                NodeStatus.Executing => SKColor.Parse("#E8F5E8"), // Light green for executing
                NodeStatus.Completed => SKColor.Parse("#C8E6C9"), // Lighter green for completed
                NodeStatus.Failed => SKColor.Parse("#FFCDD2"), // Light red for failed
                NodeStatus.Cancelled => SKColor.Parse("#FFE0B2"), // Light orange for cancelled
                _ => SKColor.Parse("#F1F8E9") // Very light green default for data sources
            };
        }
        #endregion
    }
}