using System;
using System.Collections.Generic;
using System.Linq;

namespace Beep.Skia.Automation
{
    /// <summary>A transport-neutral HTTP request passed to <see cref="WorkflowRequestRouter"/>.</summary>
    public class WorkflowHttpRequest
    {
        public string Method { get; set; } = "GET";
        public string Path { get; set; } = "/";
        public Dictionary<string, string> Query { get; set; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public string Body { get; set; }

        /// <summary>Convenience constructor for tests and adapters. A query string in the path is accepted.</summary>
        public WorkflowHttpRequest(string method, string path, string query = null, string body = null)
        {
            Method = (method ?? "GET").Trim().ToUpperInvariant();

            var rawPath = path ?? "/";
            var questionMark = rawPath.IndexOf('?');
            if (questionMark >= 0)
            {
                var inlineQuery = rawPath.Substring(questionMark + 1);
                rawPath = rawPath.Substring(0, questionMark);
                query = string.IsNullOrWhiteSpace(query) ? inlineQuery : query + "&" + inlineQuery;
            }

            Path = rawPath.Length == 0 ? "/" : rawPath;
            Query = ParseQueryString(query);
            Body = body;
        }

        /// <summary>Gets a query value, or null.</summary>
        public string GetQuery(string key)
            => key != null && Query.TryGetValue(key, out var value) ? value : null;

        /// <summary>Parses a URL query string (without the leading '?') into a case-insensitive map.</summary>
        public static Dictionary<string, string> ParseQueryString(string query)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(query)) return result;

            var text = query.TrimStart('?');
            foreach (var pair in text.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var separator = pair.IndexOf('=');
                if (separator < 0)
                {
                    var key = Decode(pair);
                    if (key.Length > 0 && !result.ContainsKey(key)) result[key] = string.Empty;
                    continue;
                }

                var name = Decode(pair.Substring(0, separator));
                var value = Decode(pair.Substring(separator + 1));
                if (name.Length > 0) result[name] = value;
            }
            return result;
        }

        private static string Decode(string value)
        {
            try { return Uri.UnescapeDataString((value ?? string.Empty).Replace('+', ' ')); }
            catch { return value ?? string.Empty; }
        }
    }

    /// <summary>HTTP-shaped result produced by the router.</summary>
    public class WorkflowHttpResponse
    {
        public int StatusCode { get; set; } = 200;
        public ApiResponse Body { get; set; } = ApiResponse.Ok();

        public static WorkflowHttpResponse From(ApiResponse response)
            => new WorkflowHttpResponse
            {
                StatusCode = response.Success ? 200 : IsNotFound(response.Error) ? 404 : 400,
                Body = response
            };

        private static bool IsNotFound(string error)
        {
            if (string.IsNullOrEmpty(error)) return false;
            return error.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("not published", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("Unknown route", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    /// <summary>
    /// Maps HTTP routes onto <see cref="WorkflowExecutionApi"/>. Adapters (HttpListener, ASP.NET,
    /// Azure Functions) only need to translate their request/response types.
    ///
    /// Routes:
    ///   GET  /health                     → service status
    ///   GET  /api/workflows              → published workflows
    ///   POST /api/workflows?name=...     → publish a diagram DTO (body) as a workflow
    ///   GET  /api/jobs?state=&amp;limit=      → list jobs
    ///   POST /api/jobs?workflowId=&amp;submittedBy=  → submit a job (body = inputs JSON object)
    ///   GET  /api/jobs/{id}              → job detail
    ///   POST /api/jobs/{id}/cancel       → cancel a job
    ///   POST /api/jobs/{id}/pause        → pause a job
    ///   POST /api/jobs/{id}/resume       → resume a job
    /// </summary>
    public class WorkflowRequestRouter
    {
        private readonly WorkflowExecutionApi _api;

        public WorkflowRequestRouter(WorkflowExecutionApi api)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
        }

        public WorkflowHttpResponse Route(WorkflowHttpRequest request)
        {
            if (request == null) return WorkflowHttpResponse.From(ApiResponse.Fail("Request is required."));

            var segments = SplitPath(request.Path);
            var method = request.Method;

            if (segments.Length == 1 && segments[0] == "health")
                return WorkflowHttpResponse.From(ApiResponse.Ok(new { status = "ok" }));

            if (segments.Length >= 2 && segments[0] == "api")
            {
                if (segments[1] == "workflows")
                {
                    if (segments.Length == 2 && method == "GET")
                        return WorkflowHttpResponse.From(_api.ListWorkflows());

                    if (segments.Length == 2 && method == "POST")
                        return WorkflowHttpResponse.From(_api.PublishDiagram(request.Body, request.GetQuery("name")));
                }

                if (segments[1] == "jobs")
                {
                    if (segments.Length == 2)
                    {
                        if (method == "GET")
                            return WorkflowHttpResponse.From(_api.ListJobs(request.GetQuery("state"), ParseLimit(request.GetQuery("limit"))));

                        if (method == "POST")
                            return WorkflowHttpResponse.From(_api.Submit(
                                request.GetQuery("workflowId"),
                                request.GetQuery("submittedBy"),
                                request.Body));
                    }

                    if (segments.Length == 3 && method == "GET")
                        return WorkflowHttpResponse.From(_api.GetJob(segments[2]));

                    if (segments.Length == 4 && method == "POST")
                    {
                        var jobId = segments[2];
                        switch (segments[3])
                        {
                            case "cancel": return WorkflowHttpResponse.From(_api.Cancel(jobId));
                            case "pause": return WorkflowHttpResponse.From(_api.Pause(jobId));
                            case "resume": return WorkflowHttpResponse.From(_api.Resume(jobId));
                        }
                    }
                }
            }

            return new WorkflowHttpResponse
            {
                StatusCode = 404,
                Body = ApiResponse.Fail($"Unknown route '{method} {request.Path}'.")
            };
        }

        /// <summary>Splits a path into lowercase segments without empty entries.</summary>
        public static string[] SplitPath(string path)
            => (path ?? "/")
                .Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.ToLowerInvariant())
                .ToArray();

        private static int ParseLimit(string value)
            => int.TryParse(value, out var limit) && limit > 0 ? limit : 50;
    }
}
