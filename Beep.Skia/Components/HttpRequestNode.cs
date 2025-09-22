using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Beep.Skia.Model;

namespace Beep.Skia.Components
{
    /// <summary>
    /// Automation node that makes HTTP requests as part of workflow automation.
    /// Supports GET, POST, PUT, DELETE methods with configurable headers, authentication, and data transformation.
    /// </summary>
    public class HttpRequestNode : AutomationNode
    {
        #region Private Fields
        private string _url = "";
        private string _method = "GET";
        private Dictionary<string, string> _headers = new Dictionary<string, string>();
        private string _requestBody = "";
        private string _authType = "None";
        private string _authToken = "";
        private int _timeoutSeconds = 30;
        private bool _followRedirects = true;
        private string _responseFormat = "json";
        private static readonly HttpClient _httpClient = new HttpClient();
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the URL for the HTTP request.
        /// </summary>
        public string Url
        {
            get => _url;
            set
            {
                if (_url != value)
                {
                    _url = value;
                    Configuration["Url"] = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the HTTP method (GET, POST, PUT, DELETE, etc.).
        /// </summary>
        public string Method
        {
            get => _method;
            set
            {
                if (_method != value)
                {
                    _method = value?.ToUpperInvariant() ?? "GET";
                    Configuration["Method"] = _method;
                }
            }
        }

        /// <summary>
        /// Gets or sets the HTTP headers to include in the request.
        /// </summary>
        public Dictionary<string, string> Headers
        {
            get => _headers;
            set
            {
                _headers = value ?? new Dictionary<string, string>();
                Configuration["Headers"] = _headers;
            }
        }

        /// <summary>
        /// Gets or sets the request body content.
        /// </summary>
        public string RequestBody
        {
            get => _requestBody;
            set
            {
                if (_requestBody != value)
                {
                    _requestBody = value ?? "";
                    Configuration["RequestBody"] = _requestBody;
                }
            }
        }

        /// <summary>
        /// Gets or sets the authentication type (None, Bearer, Basic, ApiKey).
        /// </summary>
        public string AuthType
        {
            get => _authType;
            set
            {
                if (_authType != value)
                {
                    _authType = value ?? "None";
                    Configuration["AuthType"] = _authType;
                }
            }
        }

        /// <summary>
        /// Gets or sets the authentication token or credentials.
        /// </summary>
        public string AuthToken
        {
            get => _authToken;
            set
            {
                if (_authToken != value)
                {
                    _authToken = value ?? "";
                    Configuration["AuthToken"] = _authToken;
                }
            }
        }

        /// <summary>
        /// Gets or sets the request timeout in seconds.
        /// </summary>
        public int TimeoutSeconds
        {
            get => _timeoutSeconds;
            set
            {
                if (_timeoutSeconds != value)
                {
                    _timeoutSeconds = Math.Max(1, Math.Min(300, value)); // 1 second to 5 minutes
                    Configuration["TimeoutSeconds"] = _timeoutSeconds;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to follow HTTP redirects automatically.
        /// </summary>
        public bool FollowRedirects
        {
            get => _followRedirects;
            set
            {
                if (_followRedirects != value)
                {
                    _followRedirects = value;
                    Configuration["FollowRedirects"] = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the expected response format for parsing (json, xml, text, binary).
        /// </summary>
        public string ResponseFormat
        {
            get => _responseFormat;
            set
            {
                if (_responseFormat != value)
                {
                    _responseFormat = value?.ToLowerInvariant() ?? "json";
                    Configuration["ResponseFormat"] = _responseFormat;
                }
            }
        }
        #endregion

        #region AutomationNode Implementation
        /// <summary>
        /// Gets the description of what this automation node does.
        /// </summary>
        public override string Description => "Makes HTTP requests to external APIs and services with configurable authentication and data handling";

        /// <summary>
        /// Gets the type of this automation node.
        /// </summary>
        public override NodeType NodeType => NodeType.Action;

        /// <summary>
        /// Initializes the HTTP request node with the specified configuration.
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
                
                Url = Configuration.GetValueOrDefault("Url", "")?.ToString() ?? "";
                Method = Configuration.GetValueOrDefault("Method", "GET")?.ToString() ?? "GET";
                RequestBody = Configuration.GetValueOrDefault("RequestBody", "")?.ToString() ?? "";
                AuthType = Configuration.GetValueOrDefault("AuthType", "None")?.ToString() ?? "None";
                AuthToken = Configuration.GetValueOrDefault("AuthToken", "")?.ToString() ?? "";
                TimeoutSeconds = Convert.ToInt32(Configuration.GetValueOrDefault("TimeoutSeconds", 30));
                FollowRedirects = Convert.ToBoolean(Configuration.GetValueOrDefault("FollowRedirects", true));
                ResponseFormat = Configuration.GetValueOrDefault("ResponseFormat", "json")?.ToString() ?? "json";

                if (Configuration.TryGetValue("Headers", out var headersObj) && headersObj is Dictionary<string, string> headerDict)
                {
                    Headers = headerDict;
                }

                Status = NodeStatus.Idle;
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                OnError(ex, "Failed to initialize HttpRequestNode");
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

            // Validate URL
            if (string.IsNullOrWhiteSpace(Url))
                issues.Add("URL is required");
            else if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri) || (!uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) && !uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)))
                issues.Add("URL must be a valid HTTP or HTTPS URL");

            // Validate HTTP method
            var validMethods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" };
            if (!validMethods.Contains(Method.ToUpperInvariant()))
                issues.Add($"HTTP method must be one of: {string.Join(", ", validMethods)}");

            // Validate timeout
            if (TimeoutSeconds <= 0 || TimeoutSeconds > 300)
                issues.Add("Timeout must be between 1 and 300 seconds");

            // Validate authentication
            var validAuthTypes = new[] { "None", "Bearer", "Basic", "ApiKey" };
            if (!validAuthTypes.Contains(AuthType, StringComparer.OrdinalIgnoreCase))
                issues.Add($"Authentication type must be one of: {string.Join(", ", validAuthTypes)}");

            if (!AuthType.Equals("None", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(AuthToken))
                issues.Add("Authentication token is required when authentication type is not None");

            // Validate response format
            var validFormats = new[] { "json", "xml", "text", "binary" };
            if (!validFormats.Contains(ResponseFormat.ToLowerInvariant()))
                issues.Add($"Response format must be one of: {string.Join(", ", validFormats)}");

            var result = issues.Count == 0 
                ? ValidationResult.Success()
                : ValidationResult.Failure(issues.ToArray());

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Executes the HTTP request and returns the response.
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

                // Process input parameters and build final request
                var processedUrl = ProcessUrlParameters(Url, context);
                var processedBody = ProcessRequestBody(RequestBody, context);
                var processedHeaders = ProcessHeaders(Headers, context);

                // Create HTTP request message
                using var request = new HttpRequestMessage(new HttpMethod(Method), processedUrl);

                // Add headers
                foreach (var header in processedHeaders)
                {
                    if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                    {
                        // Content-Type will be set with the content
                        continue;
                    }
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                // Add authentication
                AddAuthentication(request, AuthType, AuthToken);

                // Add request body for methods that support it
                if (!string.IsNullOrEmpty(processedBody) && (Method.Equals("POST", StringComparison.OrdinalIgnoreCase) || 
                    Method.Equals("PUT", StringComparison.OrdinalIgnoreCase) || Method.Equals("PATCH", StringComparison.OrdinalIgnoreCase)))
                {
                    var contentType = processedHeaders.GetValueOrDefault("Content-Type", "application/json");
                    request.Content = new StringContent(processedBody, Encoding.UTF8, contentType);
                }

                // Execute request with timeout
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

                using var response = await _httpClient.SendAsync(request, timeoutCts.Token);

                // Read response
                var responseContent = await response.Content.ReadAsStringAsync();
                var executionTime = DateTime.UtcNow - startTime;

                // Process response based on format
                var parsedResponse = ParseResponse(responseContent, response.Content.Headers.ContentType?.MediaType);

                // Create result data
                var resultData = new Dictionary<string, object>
                {
                    ["statusCode"] = (int)response.StatusCode,
                    ["statusText"] = response.ReasonPhrase,
                    ["headers"] = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                    ["body"] = parsedResponse,
                    ["rawBody"] = responseContent,
                    ["requestUrl"] = processedUrl,
                    ["requestMethod"] = Method,
                    ["executionTime"] = executionTime.TotalMilliseconds,
                    ["isSuccess"] = response.IsSuccessStatusCode,
                    ["contentType"] = response.Content.Headers.ContentType?.MediaType ?? "unknown"
                };

                Status = NodeStatus.Completed;
                var result = NodeResult.CreateSuccess(resultData);
                result.ExecutionTime = executionTime;
                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Status = NodeStatus.Cancelled;
                return NodeResult.CreateFailure("HTTP request was cancelled");
            }
            catch (OperationCanceledException)
            {
                Status = NodeStatus.Failed;
                return NodeResult.CreateFailure("HTTP request timed out");
            }
            catch (HttpRequestException ex)
            {
                OnError(ex, "HTTP request failed");
                return NodeResult.CreateFailure($"HTTP request failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                OnError(ex, "Failed to execute HTTP request");
                return NodeResult.CreateFailure($"Request execution failed: {ex.Message}", ex);
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
                    ["urlParameters"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["description"] = "Parameters to substitute in the URL using {{paramName}} syntax",
                        ["additionalProperties"] = true
                    },
                    ["bodyData"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["description"] = "Data to include in the request body or merge with configured body",
                        ["additionalProperties"] = true
                    },
                    ["headers"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["description"] = "Additional headers to include in the request",
                        ["additionalProperties"] = new Dictionary<string, object> { ["type"] = "string" }
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
                    ["statusCode"] = new Dictionary<string, object>
                    {
                        ["type"] = "integer",
                        ["description"] = "HTTP response status code"
                    },
                    ["statusText"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "HTTP response status text"
                    },
                    ["headers"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["description"] = "Response headers",
                        ["additionalProperties"] = new Dictionary<string, object> { ["type"] = "string" }
                    },
                    ["body"] = new Dictionary<string, object>
                    {
                        ["description"] = "Parsed response body (object for JSON, string for text, etc.)"
                    },
                    ["rawBody"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "Raw response body as string"
                    },
                    ["isSuccess"] = new Dictionary<string, object>
                    {
                        ["type"] = "boolean",
                        ["description"] = "Whether the request was successful (2xx status code)"
                    },
                    ["executionTime"] = new Dictionary<string, object>
                    {
                        ["type"] = "number",
                        ["description"] = "Request execution time in milliseconds"
                    }
                }
            };
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Processes URL parameters by substituting placeholders with values from the execution context.
        /// </summary>
        /// <param name="url">The URL template with {{paramName}} placeholders.</param>
        /// <param name="context">The execution context containing parameter values.</param>
        /// <returns>The processed URL with substituted parameters.</returns>
        private string ProcessUrlParameters(string url, Model.ExecutionContext context)
        {
            var processedUrl = url;

            // Get URL parameters from input data
            if (context.Data.TryGetValue("urlParameters", out var urlParams) && urlParams is Dictionary<string, object> paramDict)
            {
                foreach (var param in paramDict)
                {
                    var placeholder = $"{{{{{param.Key}}}}}";
                    processedUrl = processedUrl.Replace(placeholder, param.Value?.ToString() ?? "");
                }
            }

            // Also check workflow variables
            foreach (var variable in context.Variables)
            {
                var placeholder = $"{{{{{variable.Key}}}}}";
                if (processedUrl.Contains(placeholder))
                {
                    processedUrl = processedUrl.Replace(placeholder, variable.Value?.ToString() ?? "");
                }
            }

            return processedUrl;
        }

        /// <summary>
        /// Processes the request body by merging with input data or substituting parameters.
        /// </summary>
        /// <param name="body">The configured request body.</param>
        /// <param name="context">The execution context containing body data.</param>
        /// <returns>The processed request body.</returns>
        private string ProcessRequestBody(string body, Model.ExecutionContext context)
        {
            if (string.IsNullOrEmpty(body))
            {
                // If no configured body, use input bodyData directly
                if (context.Data.TryGetValue("bodyData", out var bodyData))
                {
                    return JsonSerializer.Serialize(bodyData);
                }
                return "";
            }

            // If body is configured, try to merge with input data or substitute parameters
            if (context.Data.TryGetValue("bodyData", out var inputBodyData) && inputBodyData is Dictionary<string, object> bodyDict)
            {
                // Try to parse existing body as JSON and merge
                try
                {
                    var existingBody = JsonSerializer.Deserialize<Dictionary<string, object>>(body);
                    foreach (var kvp in bodyDict)
                    {
                        existingBody[kvp.Key] = kvp.Value;
                    }
                    return JsonSerializer.Serialize(existingBody);
                }
                catch
                {
                    // If body isn't JSON, just substitute parameters
                    var processedBody = body;
                    foreach (var param in bodyDict)
                    {
                        var placeholder = $"{{{{{param.Key}}}}}";
                        processedBody = processedBody.Replace(placeholder, param.Value?.ToString() ?? "");
                    }
                    return processedBody;
                }
            }

            return body;
        }

        /// <summary>
        /// Processes headers by merging configured headers with input headers.
        /// </summary>
        /// <param name="headers">The configured headers.</param>
        /// <param name="context">The execution context containing additional headers.</param>
        /// <returns>The processed headers dictionary.</returns>
        private Dictionary<string, string> ProcessHeaders(Dictionary<string, string> headers, Model.ExecutionContext context)
        {
            var processedHeaders = new Dictionary<string, string>(headers);

            // Add headers from input data
            if (context.Data.TryGetValue("headers", out var inputHeaders) && inputHeaders is Dictionary<string, string> headerDict)
            {
                foreach (var header in headerDict)
                {
                    processedHeaders[header.Key] = header.Value;
                }
            }

            return processedHeaders;
        }

        /// <summary>
        /// Adds authentication to the HTTP request based on the configured authentication type.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <param name="authType">The authentication type.</param>
        /// <param name="authToken">The authentication token or credentials.</param>
        private void AddAuthentication(HttpRequestMessage request, string authType, string authToken)
        {
            if (string.IsNullOrWhiteSpace(authToken) || authType.Equals("None", StringComparison.OrdinalIgnoreCase))
                return;

            switch (authType.ToLowerInvariant())
            {
                case "bearer":
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
                    break;

                case "basic":
                    var basicBytes = Encoding.UTF8.GetBytes(authToken);
                    var basicValue = Convert.ToBase64String(basicBytes);
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basicValue);
                    break;

                case "apikey":
                    request.Headers.TryAddWithoutValidation("X-API-Key", authToken);
                    break;
            }
        }

        /// <summary>
        /// Parses the response content based on the expected response format and content type.
        /// </summary>
        /// <param name="content">The raw response content.</param>
        /// <param name="contentType">The response content type.</param>
        /// <returns>The parsed response object.</returns>
        private object ParseResponse(string content, string contentType)
        {
            if (string.IsNullOrEmpty(content))
                return null;

            // Determine parsing strategy based on ResponseFormat and actual content type
            var actualFormat = DetermineResponseFormat(contentType);
            var targetFormat = ResponseFormat.ToLowerInvariant();

            try
            {
                return targetFormat switch
                {
                    "json" when actualFormat == "json" => JsonSerializer.Deserialize<object>(content),
                    "json" => content, // Return as string if not actually JSON
                    "xml" => content, // XML parsing would require additional dependencies
                    "text" => content,
                    "binary" => Convert.ToBase64String(Encoding.UTF8.GetBytes(content)),
                    _ => content
                };
            }
            catch
            {
                // If parsing fails, return as string
                return content;
            }
        }

        /// <summary>
        /// Determines the response format based on the content type.
        /// </summary>
        /// <param name="contentType">The HTTP content type header.</param>
        /// <returns>The determined format (json, xml, text, binary).</returns>
        private string DetermineResponseFormat(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return "text";

            var lowerContentType = contentType.ToLowerInvariant();

            if (lowerContentType.Contains("json"))
                return "json";
            if (lowerContentType.Contains("xml"))
                return "xml";
            if (lowerContentType.Contains("text") || lowerContentType.Contains("html"))
                return "text";
            
            return "binary";
        }
        #endregion

        #region Visual Customization
        /// <summary>
        /// Draws the HTTP request node with custom styling.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The node bounds.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawNodeContent(SKCanvas canvas, SKRect bounds, DrawingContext context)
        {
            // Draw HTTP method badge
            DrawMethodBadge(canvas, bounds);

            // Draw node title
            using var font = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), 12);
            using var textPaint = new SKPaint
            {
                IsAntialias = true,
                Color = GetTextColor()
            };

            var title = "HTTP Request";
            var titleWidth = font.MeasureText(title);
            var titleX = bounds.MidX - titleWidth / 2;
            var titleY = bounds.Top + 16;
            canvas.DrawText(title, titleX, titleY, font, textPaint);

            // Draw URL if configured
            if (!string.IsNullOrWhiteSpace(Url))
            {
                using var urlFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 9);
                var urlText = Url.Length > 20 ? Url.Substring(0, 17) + "..." : Url;
                var urlWidth = urlFont.MeasureText(urlText);
                var urlX = bounds.MidX - urlWidth / 2;
                var urlY = bounds.Bottom - 8;
                canvas.DrawText(urlText, urlX, urlY, urlFont, textPaint);
            }
        }

        /// <summary>
        /// Draws the HTTP method badge to visually identify the request type.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="bounds">The node bounds.</param>
        private void DrawMethodBadge(SKCanvas canvas, SKRect bounds)
        {
            var badgeColor = GetMethodColor(Method);
            
            using var badgePaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = badgeColor
            };

            // Draw method badge in top-right corner
            var badgeSize = new SKSize(24, 12);
            var badgeRect = new SKRect(bounds.Right - badgeSize.Width - 4, bounds.Top + 4, 
                                     bounds.Right - 4, bounds.Top + 4 + badgeSize.Height);

            canvas.DrawRoundRect(badgeRect, 2, 2, badgePaint);

            // Draw method text
            using var methodFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), 8);
            using var methodTextPaint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.White
            };

            var methodText = Method.Length > 4 ? Method.Substring(0, 4) : Method;
            var textWidth = methodFont.MeasureText(methodText);
            var textX = badgeRect.MidX - textWidth / 2;
            var textY = badgeRect.MidY + methodFont.Size / 3;
            canvas.DrawText(methodText, textX, textY, methodFont, methodTextPaint);
        }

        /// <summary>
        /// Gets the color associated with an HTTP method.
        /// </summary>
        /// <param name="method">The HTTP method.</param>
        /// <returns>The color to use for the method badge.</returns>
        private SKColor GetMethodColor(string method)
        {
            return method.ToUpperInvariant() switch
            {
                "GET" => SKColor.Parse("#4CAF50"),    // Green
                "POST" => SKColor.Parse("#2196F3"),   // Blue
                "PUT" => SKColor.Parse("#FF9800"),    // Orange
                "DELETE" => SKColor.Parse("#F44336"), // Red
                "PATCH" => SKColor.Parse("#9C27B0"),  // Purple
                "HEAD" => SKColor.Parse("#607D8B"),   // Blue Gray
                "OPTIONS" => SKColor.Parse("#795548"), // Brown
                _ => SKColor.Parse("#9E9E9E")         // Gray
            };
        }

        /// <summary>
        /// Gets the background color for HTTP request nodes.
        /// </summary>
        /// <returns>Background color with HTTP request styling.</returns>
        protected override SKColor GetBackgroundColor()
        {
            if (!IsEnabled)
                return SKColors.LightGray.WithAlpha(128);

            return Status switch
            {
                NodeStatus.Executing => SKColor.Parse("#E3F2FD"), // Light blue for executing
                NodeStatus.Completed => SKColor.Parse("#E8F5E8"), // Light green for completed
                NodeStatus.Failed => SKColor.Parse("#FFEBEE"), // Light red for failed
                NodeStatus.Cancelled => SKColor.Parse("#FFF3E0"), // Light orange for cancelled
                _ => SKColor.Parse("#F3E5F5") // Very light purple default for HTTP requests
            };
        }
        #endregion
    }
}