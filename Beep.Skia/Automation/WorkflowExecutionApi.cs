using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Beep.Skia.Serialization;

namespace Beep.Skia.Automation
{
    /// <summary>Transport-neutral API response envelope.</summary>
    public class ApiResponse
    {
        /// <summary>
        /// Gets or sets the success.
        /// </summary>
        public bool Success { get; set; }
        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        public string Error { get; set; }
        /// <summary>
        /// Gets or sets the payload.
        /// </summary>
        public object Payload { get; set; }

        /// <summary>
        /// Gets or sets the ok.
        /// </summary>
        public static ApiResponse Ok(object payload = null) => new ApiResponse { Success = true, Payload = payload };
        /// <summary>
        /// Gets or sets the fail.
        /// </summary>
        public static ApiResponse Fail(string error) => new ApiResponse { Success = false, Error = error };
    }

    /// <summary>
    /// JSON-friendly facade over <see cref="WorkflowExecutionService"/>. Every method returns an
    /// <see cref="ApiResponse"/>, so an HTTP handler (ASP.NET, HttpListener, Azure Functions, ...)
    /// can map routes directly to these calls and serialize the response with <see cref="ToJson"/>.
    /// </summary>
    public class WorkflowExecutionApi
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private readonly WorkflowExecutionService _service;

        /// <summary>
        /// Initializes a new instance of the WorkflowExecutionApi class.
        /// </summary>
        public WorkflowExecutionApi(WorkflowExecutionService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Gets or sets the service.
        /// </summary>
        public WorkflowExecutionService Service => _service;

        /// <summary>
        /// Publishes a diagram (serialized <see cref="DiagramDto"/>) as an executable workflow.
        /// Only automation-node components are included.
        /// </summary>
        public ApiResponse PublishDiagram(string diagramJson, string workflowName = null)
        {
            if (string.IsNullOrWhiteSpace(diagramJson))
                return ApiResponse.Fail("diagramJson is required.");

            try
            {
                var dto = JsonSerializer.Deserialize<DiagramDto>(diagramJson, JsonOptions);
                if (dto == null) return ApiResponse.Fail("Invalid diagram payload.");

                var manager = new DrawingManager();
                manager.LoadFromDto(dto);
                var workflow = manager.ToWorkflowDefinition(
                    string.IsNullOrWhiteSpace(workflowName) ? "Workflow" : workflowName);

                if (workflow.Nodes.Count == 0)
                    return ApiResponse.Fail("Diagram contains no executable automation nodes.");

                _service.Publish(workflow);
                return ApiResponse.Ok(new
                {
                    workflowId = workflow.Id,
                    name = workflow.Name,
                    nodes = workflow.Nodes.Count,
                    connections = workflow.Connections.Count
                });
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Gets or sets the list workflows.
        /// </summary>
        public ApiResponse ListWorkflows()
            => ApiResponse.Ok(_service.Workflows
                .Select(w => new { workflowId = w.Id, name = w.Name, nodes = w.Nodes.Count })
                .ToList());

        /// <summary>Submits a job for a published workflow; inputsJson is an optional JSON object.</summary>
        public ApiResponse Submit(string workflowId, string submittedBy = null, string inputsJson = null)
        {
            var inputs = ParseInputs(inputsJson, out var error);
            if (error != null) return ApiResponse.Fail(error);

            var job = _service.Submit(workflowId, submittedBy, inputs);
            if (job == null) return ApiResponse.Fail($"Workflow '{workflowId}' is not published.");

            return ApiResponse.Ok(DescribeJob(job));
        }

        /// <summary>
        /// Gets or sets the get job.
        /// </summary>
        public ApiResponse GetJob(string jobId)
        {
            var job = _service.GetJob(jobId);
            return job == null ? ApiResponse.Fail("Job not found.") : ApiResponse.Ok(DescribeJob(job, includeResult: true));
        }

        /// <summary>
        /// Gets or sets the list jobs.
        /// </summary>
        public ApiResponse ListJobs(string state = null, int limit = 50)
        {
            JobState? filter = null;
            if (!string.IsNullOrWhiteSpace(state))
            {
                if (!Enum.TryParse<JobState>(state, ignoreCase: true, out var parsed))
                    return ApiResponse.Fail($"Unknown job state '{state}'.");
                filter = parsed;
            }

            return ApiResponse.Ok(_service.GetJobs(filter, limit).Select(j => DescribeJob(j)).ToList());
        }

        /// <summary>
        /// Gets or sets the cancel.
        /// </summary>
        public ApiResponse Cancel(string jobId)
            => _service.Cancel(jobId) ? ApiResponse.Ok(new { jobId, state = nameof(JobState.Cancelled) }) : ApiResponse.Fail("Job cannot be cancelled.");

        /// <summary>
        /// Gets or sets the pause.
        /// </summary>
        public ApiResponse Pause(string jobId)
            => _service.Pause(jobId) ? ApiResponse.Ok(new { jobId, state = nameof(JobState.Paused) }) : ApiResponse.Fail("Job cannot be paused.");

        /// <summary>
        /// Gets or sets the resume.
        /// </summary>
        public ApiResponse Resume(string jobId)
            => _service.Resume(jobId) ? ApiResponse.Ok(new { jobId, state = nameof(JobState.Running) }) : ApiResponse.Fail("Job cannot be resumed.");

        /// <summary>Serializes a response envelope for transport.</summary>
        public string ToJson(ApiResponse response)
            => JsonSerializer.Serialize(response, JsonOptions);

        private static object DescribeJob(ExecutionJob job, bool includeResult = false)
            => new
            {
                jobId = job.Id,
                workflowId = job.WorkflowId,
                workflowName = job.WorkflowName,
                submittedBy = job.SubmittedBy,
                state = job.State.ToString(),
                submittedAt = job.SubmittedAt,
                startedAt = job.StartedAt,
                completedAt = job.CompletedAt,
                durationMs = job.Duration?.TotalMilliseconds,
                error = job.Error,
                result = includeResult && job.Result != null
                    ? new
                    {
                        status = job.Result.Status.ToString(),
                        errors = job.Result.Errors.ToList(),
                        output = job.Result.OutputData
                    }
                    : null
            };

        private static Dictionary<string, object> ParseInputs(string inputsJson, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(inputsJson)) return null;

            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(inputsJson, JsonOptions);
                if (parsed == null) return null;

                var inputs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var pair in parsed) inputs[pair.Key] = ConvertElement(pair.Value);
                return inputs;
            }
            catch (JsonException ex)
            {
                error = "inputsJson is not a valid JSON object: " + ex.Message;
                return null;
            }
        }

        private static object ConvertElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String: return element.GetString();
                case JsonValueKind.Number: return element.TryGetInt64(out var l) ? (object)l : element.GetDouble();
                case JsonValueKind.True: return true;
                case JsonValueKind.False: return false;
                case JsonValueKind.Null: return null;
                default: return element.GetRawText();
            }
        }
    }
}
