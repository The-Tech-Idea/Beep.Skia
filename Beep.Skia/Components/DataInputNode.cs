using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Beep.Skia.Model;

namespace Beep.Skia.Components
{
    /// <summary>
    /// Automation node that provides manual data input capabilities for workflows.
    /// Supports JSON editing, form-based input, and programmatic data entry.
    /// Similar to n8n's "Set" node or Make.com's data input functionality.
    /// </summary>
    public class DataInputNode : AutomationNode
    {
        #region Private Fields
        private string _inputMode = "JSON";
        private string _jsonData = "{}";
        private Dictionary<string, object> _formData = new Dictionary<string, object>();
        private List<FormField> _formFields = new List<FormField>();
        private bool _allowMultipleEntries = false;
        private List<Dictionary<string, object>> _multipleEntries = new List<Dictionary<string, object>>();
        private string _dataSchema = "";
        private bool _validateInput = true;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the input mode (JSON, Form, Code).
        /// </summary>
        public string InputMode
        {
            get => _inputMode;
            set
            {
                if (_inputMode != value)
                {
                    _inputMode = value ?? "JSON";
                    Configuration["InputMode"] = _inputMode;
                }
            }
        }

        /// <summary>
        /// Gets or sets the JSON data for JSON input mode.
        /// </summary>
        public string JsonData
        {
            get => _jsonData;
            set
            {
                if (_jsonData != value)
                {
                    _jsonData = value ?? "{}";
                    Configuration["JsonData"] = _jsonData;
                }
            }
        }

        /// <summary>
        /// Gets or sets the form data for Form input mode.
        /// </summary>
        public Dictionary<string, object> FormData
        {
            get => _formData;
            set
            {
                _formData = value ?? new Dictionary<string, object>();
                Configuration["FormData"] = _formData;
            }
        }

        /// <summary>
        /// Gets or sets the form fields configuration for Form input mode.
        /// </summary>
        public List<FormField> FormFields
        {
            get => _formFields;
            set
            {
                _formFields = value ?? new List<FormField>();
                Configuration["FormFields"] = _formFields;
            }
        }

        /// <summary>
        /// Gets or sets whether multiple data entries are allowed.
        /// </summary>
        public bool AllowMultipleEntries
        {
            get => _allowMultipleEntries;
            set
            {
                if (_allowMultipleEntries != value)
                {
                    _allowMultipleEntries = value;
                    Configuration["AllowMultipleEntries"] = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the multiple data entries.
        /// </summary>
        public List<Dictionary<string, object>> MultipleEntries
        {
            get => _multipleEntries;
            set
            {
                _multipleEntries = value ?? new List<Dictionary<string, object>>();
                Configuration["MultipleEntries"] = _multipleEntries;
            }
        }

        /// <summary>
        /// Gets or sets the JSON schema for input validation.
        /// </summary>
        public string DataSchema
        {
            get => _dataSchema;
            set
            {
                if (_dataSchema != value)
                {
                    _dataSchema = value ?? "";
                    Configuration["DataSchema"] = _dataSchema;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to validate input data.
        /// </summary>
        public bool ValidateInput
        {
            get => _validateInput;
            set
            {
                if (_validateInput != value)
                {
                    _validateInput = value;
                    Configuration["ValidateInput"] = value;
                }
            }
        }
        #endregion

        #region AutomationNode Implementation
        /// <summary>
        /// Gets the description of what this automation node does.
        /// </summary>
        public override string Description => "Provides manual data input capabilities with JSON editing, form-based input, and validation";

        /// <summary>
        /// Gets the type of this automation node.
        /// </summary>
        public override NodeType NodeType => NodeType.DataSource;

        /// <summary>
        /// Gets the data type of the output for this data input node.
        /// </summary>
        /// <returns>The output data type string.</returns>
        protected override string GetOutputDataType()
        {
            // Determine data type based on the current input mode and data
            try
            {
                switch (InputMode)
                {
                    case "JSON":
                        // Parse JSON to determine type
                        var jsonData = JsonData.Trim();
                        if (string.IsNullOrEmpty(jsonData) || jsonData == "{}")
                            return "object";
                        
                        // Try to parse and determine type
                        using (var doc = JsonDocument.Parse(jsonData))
                        {
                            return GetDataType(doc.RootElement);
                        }

                    case "Form":
                        // Form data is typically an object
                        return "object";

                    case "Code":
                        // Code input could be any type
                        return "any";

                    default:
                        return "object";
                }
            }
            catch
            {
                // If parsing fails, default to object
                return "object";
            }
        }

        /// <summary>
        /// Initializes the data input node with the specified configuration.
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

                InputMode = Configuration.GetValueOrDefault("InputMode", "JSON")?.ToString() ?? "JSON";
                JsonData = Configuration.GetValueOrDefault("JsonData", "{}")?.ToString() ?? "{}";
                AllowMultipleEntries = Convert.ToBoolean(Configuration.GetValueOrDefault("AllowMultipleEntries", false));
                DataSchema = Configuration.GetValueOrDefault("DataSchema", "")?.ToString() ?? "";
                ValidateInput = Convert.ToBoolean(Configuration.GetValueOrDefault("ValidateInput", true));

                if (Configuration.TryGetValue("FormData", out var formDataObj) && formDataObj is Dictionary<string, object> formDataDict)
                {
                    FormData = formDataDict;
                }

                if (Configuration.TryGetValue("FormFields", out var formFieldsObj) && formFieldsObj is List<FormField> formFieldsList)
                {
                    FormFields = formFieldsList;
                }

                if (Configuration.TryGetValue("MultipleEntries", out var entriesObj) && entriesObj is List<Dictionary<string, object>> entriesList)
                {
                    MultipleEntries = entriesList;
                }

                Status = NodeStatus.Idle;
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                OnError(ex, "Failed to initialize DataInputNode");
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

            // Validate input mode
            var validModes = new[] { "JSON", "Form", "Code" };
            if (!validModes.Contains(InputMode, StringComparer.OrdinalIgnoreCase))
                issues.Add($"Input mode must be one of: {string.Join(", ", validModes)}");

            // Validate based on input mode
            switch (InputMode.ToLowerInvariant())
            {
                case "json":
                    if (string.IsNullOrWhiteSpace(JsonData))
                        issues.Add("JSON data cannot be empty");
                    else
                    {
                        try
                        {
                            JsonDocument.Parse(JsonData);
                        }
                        catch (JsonException ex)
                        {
                            issues.Add($"Invalid JSON format: {ex.Message}");
                        }
                    }
                    break;

                case "form":
                    if (FormFields == null || !FormFields.Any())
                        issues.Add("Form fields are required for Form input mode");
                    else
                    {
                        foreach (var field in FormFields)
                        {
                            if (string.IsNullOrWhiteSpace(field.Name))
                                issues.Add("All form fields must have a name");
                            if (string.IsNullOrWhiteSpace(field.Type))
                                issues.Add($"Field '{field.Name}': Type is required");
                        }
                    }
                    break;
            }

            // Validate JSON schema if provided
            if (!string.IsNullOrWhiteSpace(DataSchema))
            {
                try
                {
                    JsonDocument.Parse(DataSchema);
                }
                catch (JsonException ex)
                {
                    issues.Add($"Invalid JSON schema: {ex.Message}");
                }
            }

            var result = issues.Count == 0
                ? ValidationResult.Success()
                : ValidationResult.Failure(issues.ToArray());

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Executes the data input and returns the configured data.
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

                // Get the input data based on mode
                var outputData = await GetInputData(cancellationToken);

                // Validate data if required
                if (ValidateInput && !string.IsNullOrWhiteSpace(DataSchema))
                {
                    var validationResult = ValidateAgainstSchema(outputData);
                    if (!validationResult.IsValid)
                    {
                        return NodeResult.CreateFailure($"Data validation failed: {string.Join(", ", validationResult.Errors)}");
                    }
                }

                // Create result data
                var resultData = new Dictionary<string, object>
                {
                    ["data"] = outputData,
                    ["inputMode"] = InputMode,
                    ["validated"] = ValidateInput,
                    ["executedAt"] = DateTime.UtcNow,
                    ["dataType"] = GetDataType(outputData)
                };

                if (AllowMultipleEntries && MultipleEntries.Any())
                {
                    resultData["multipleEntries"] = MultipleEntries;
                    resultData["entryCount"] = MultipleEntries.Count;
                }

                Status = NodeStatus.Completed;
                var result = NodeResult.CreateSuccess(resultData);
                result.ExecutionTime = DateTime.UtcNow - startTime;
                return result;
            }
            catch (OperationCanceledException)
            {
                Status = NodeStatus.Cancelled;
                return NodeResult.CreateFailure("Data input was cancelled");
            }
            catch (Exception ex)
            {
                OnError(ex, "Failed to execute data input");
                return NodeResult.CreateFailure($"Data input failed: {ex.Message}", ex);
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
                    ["inputParameters"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["description"] = "Runtime parameters for input processing",
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
                    ["data"] = new Dictionary<string, object>
                    {
                        ["description"] = "The input data provided by the user",
                        ["oneOf"] = new[]
                        {
                            new Dictionary<string, object> { ["type"] = "object" },
                            new Dictionary<string, object> { ["type"] = "array" },
                            new Dictionary<string, object> { ["type"] = "string" },
                            new Dictionary<string, object> { ["type"] = "number" },
                            new Dictionary<string, object> { ["type"] = "boolean" }
                        }
                    },
                    ["inputMode"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "The input mode used (JSON, Form, Code)"
                    },
                    ["validated"] = new Dictionary<string, object>
                    {
                        ["type"] = "boolean",
                        ["description"] = "Whether the data was validated"
                    },
                    ["multipleEntries"] = new Dictionary<string, object>
                    {
                        ["type"] = "array",
                        ["description"] = "Multiple data entries if AllowMultipleEntries is enabled"
                    }
                }
            };
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Gets the input data based on the current input mode.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The input data.</returns>
        private async Task<object> GetInputData(CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken); // Make method async-compatible

            return InputMode.ToLowerInvariant() switch
            {
                "json" => ParseJsonData(),
                "form" => GetFormData(),
                "code" => ParseJsonData(), // For now, treat code as JSON
                _ => new Dictionary<string, object>()
            };
        }

        /// <summary>
        /// Parses the JSON data string into an object.
        /// </summary>
        /// <returns>The parsed JSON data.</returns>
        private object ParseJsonData()
        {
            if (string.IsNullOrWhiteSpace(JsonData))
                return new Dictionary<string, object>();

            try
            {
                return JsonSerializer.Deserialize<object>(JsonData);
            }
            catch
            {
                return new Dictionary<string, object> { ["error"] = "Invalid JSON format" };
            }
        }

        /// <summary>
        /// Gets the form data as a dictionary.
        /// </summary>
        /// <returns>The form data.</returns>
        private object GetFormData()
        {
            return FormData ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Validates data against the JSON schema.
        /// </summary>
        /// <param name="data">The data to validate.</param>
        /// <returns>The validation result.</returns>
        private ValidationResult ValidateAgainstSchema(object data)
        {
            // Basic JSON schema validation - could be enhanced with a proper JSON schema validator
            if (string.IsNullOrWhiteSpace(DataSchema))
                return ValidationResult.Success();

            try
            {
                var schema = JsonDocument.Parse(DataSchema);
                // For now, just check if data can be serialized as JSON
                JsonSerializer.Serialize(data);
                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                return ValidationResult.Failure(new[] { ex.Message });
            }
        }

        /// <summary>
        /// Gets the data type of the output data.
        /// </summary>
        /// <param name="data">The data to analyze.</param>
        /// <returns>The data type string.</returns>
        private string GetDataType(object data)
        {
            return data switch
            {
                string => "string",
                int or long or float or double or decimal => "number",
                bool => "boolean",
                IEnumerable<object> => "array",
                Dictionary<string, object> => "object",
                _ => "unknown"
            };
        }

        /// <summary>
        /// Gets the data type of a JSON element.
        /// </summary>
        /// <param name="element">The JSON element to analyze.</param>
        /// <returns>The data type string.</returns>
        private string GetDataType(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => "string",
                JsonValueKind.Number => "number",
                JsonValueKind.True or JsonValueKind.False => "boolean",
                JsonValueKind.Array => "array",
                JsonValueKind.Object => "object",
                JsonValueKind.Null => "null",
                _ => "unknown"
            };
        }

        /// <summary>
        /// Adds a new entry to the multiple entries collection.
        /// </summary>
        /// <param name="entry">The entry to add.</param>
        public void AddEntry(Dictionary<string, object> entry)
        {
            if (entry != null)
            {
                MultipleEntries.Add(entry);
                Configuration["MultipleEntries"] = MultipleEntries;
            }
        }

        /// <summary>
        /// Removes an entry from the multiple entries collection.
        /// </summary>
        /// <param name="index">The index of the entry to remove.</param>
        public void RemoveEntry(int index)
        {
            if (index >= 0 && index < MultipleEntries.Count)
            {
                MultipleEntries.RemoveAt(index);
                Configuration["MultipleEntries"] = MultipleEntries;
            }
        }

        /// <summary>
        /// Clears all entries from the multiple entries collection.
        /// </summary>
        public void ClearEntries()
        {
            MultipleEntries.Clear();
            Configuration["MultipleEntries"] = MultipleEntries;
        }
        #endregion
    }

    /// <summary>
    /// Represents a form field configuration for the DataInputNode.
    /// </summary>
    public class FormField
    {
        /// <summary>
        /// Gets or sets the field name.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Gets or sets the field type (text, number, boolean, date, select).
        /// </summary>
        public string Type { get; set; } = "text";

        /// <summary>
        /// Gets or sets the field label.
        /// </summary>
        public string Label { get; set; } = "";

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets whether the field is required.
        /// </summary>
        public bool Required { get; set; } = false;

        /// <summary>
        /// Gets or sets the placeholder text.
        /// </summary>
        public string Placeholder { get; set; } = "";

        /// <summary>
        /// Gets or sets the options for select fields.
        /// </summary>
        public List<string> Options { get; set; } = new List<string>();
    }
}