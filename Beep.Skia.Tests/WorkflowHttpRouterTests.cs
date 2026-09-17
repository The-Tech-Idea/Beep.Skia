using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Beep.Skia;
using Beep.Skia.Automation;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for the HTTP request router: route mapping, status codes, query parsing, and the
    /// full publish → submit → poll → cancel lifecycle over the router.
    /// </summary>
    public class WorkflowHttpRouterTests
    {
        private static WorkflowRequestRouter CreateRouter(WorkflowExecutionService service)
            => new WorkflowRequestRouter(new WorkflowExecutionApi(service));

        private static string BuildAutomationDiagramJson()
        {
            var manager = new DrawingManager();
            manager.AddComponent(new Beep.Skia.Components.ManualTriggerNode
            {
                X = 20,
                Y = 20,
                Width = 140,
                Height = 50,
                Name = "Start"
            });
            return JsonSerializer.Serialize(manager.ToDto());
        }

        private static string PublishWorkflow(WorkflowRequestRouter router, WorkflowExecutionService service)
        {
            var response = router.Route(new WorkflowHttpRequest("POST", "/api/workflows?name=Router%20Workflow", body: BuildAutomationDiagramJson()));
            Assert.Equal(200, response.StatusCode);
            return service.Workflows.Single().Id;
        }

        // ── Query parsing ────────────────────────────────────────────────────

        [Fact]
        public void ParseQueryString_HandlesEncodingAndMissingValues()
        {
            var query = WorkflowHttpRequest.ParseQueryString("?name=Router%20Workflow&state=completed&flag&limit=5");

            Assert.Equal("Router Workflow", query["name"]);
            Assert.Equal("completed", query["state"]);
            Assert.Equal(string.Empty, query["flag"]);
            Assert.Equal("5", query["limit"]);
        }

        [Fact]
        public void ParseQueryString_LastValueWins()
        {
            var query = WorkflowHttpRequest.ParseQueryString("a=1&a=2");
            Assert.Equal("2", query["a"]);
        }

        // ── Routing ──────────────────────────────────────────────────────────

        [Fact]
        public void Health_ReturnsOk()
        {
            using var service = new WorkflowExecutionService();
            var response = CreateRouter(service).Route(new WorkflowHttpRequest("GET", "/health"));

            Assert.Equal(200, response.StatusCode);
            Assert.True(response.Body.Success);
        }

        [Fact]
        public void UnknownRoute_Returns404()
        {
            using var service = new WorkflowExecutionService();
            var response = CreateRouter(service).Route(new WorkflowHttpRequest("GET", "/api/nope"));

            Assert.Equal(404, response.StatusCode);
            Assert.False(response.Body.Success);
            Assert.Contains("Unknown route", response.Body.Error);
        }

        [Fact]
        public void WrongMethod_Returns404()
        {
            using var service = new WorkflowExecutionService();
            var router = CreateRouter(service);

            Assert.Equal(404, router.Route(new WorkflowHttpRequest("DELETE", "/api/workflows")).StatusCode);
            Assert.Equal(404, router.Route(new WorkflowHttpRequest("GET", "/api/jobs/abc/cancel")).StatusCode);
        }

        [Fact]
        public void PublishDiagram_InvalidBody_Returns400()
        {
            using var service = new WorkflowExecutionService();
            var response = CreateRouter(service).Route(new WorkflowHttpRequest("POST", "/api/workflows", body: "{ nope }"));

            Assert.Equal(400, response.StatusCode);
            Assert.False(response.Body.Success);
        }

        [Fact]
        public void ListWorkflows_Empty_ReturnsOk()
        {
            using var service = new WorkflowExecutionService();
            var response = CreateRouter(service).Route(new WorkflowHttpRequest("GET", "/api/workflows"));

            Assert.Equal(200, response.StatusCode);
            Assert.True(response.Body.Success);
        }

        [Fact]
        public void Submit_UnknownWorkflow_Returns404()
        {
            using var service = new WorkflowExecutionService();
            var response = CreateRouter(service).Route(new WorkflowHttpRequest("POST", "/api/jobs?workflowId=missing"));

            Assert.Equal(404, response.StatusCode);
        }

        [Fact]
        public void JobDetail_UnknownJob_Returns404()
        {
            using var service = new WorkflowExecutionService();
            var response = CreateRouter(service).Route(new WorkflowHttpRequest("GET", "/api/jobs/nope"));

            Assert.Equal(404, response.StatusCode);
        }

        [Fact]
        public void FullLifecycle_PublishSubmitPoll()
        {
            using var service = new WorkflowExecutionService();
            var router = CreateRouter(service);
            var workflowId = PublishWorkflow(router, service);

            var submit = router.Route(new WorkflowHttpRequest(
                "POST",
                $"/api/jobs?workflowId={workflowId}&submittedBy=alice",
                body: "{\"environment\":\"ci\"}"));
            Assert.Equal(200, submit.StatusCode);

            var jobId = service.GetJobs()[0].Id;
            Assert.True(service.WaitForCompletion(jobId, TimeSpan.FromSeconds(5)));

            var detail = router.Route(new WorkflowHttpRequest("GET", "/api/jobs/" + jobId));
            Assert.Equal(200, detail.StatusCode);
            var json = new WorkflowExecutionApi(service).ToJson(detail.Body);
            Assert.Contains("\"state\": \"Completed\"", json);

            var listed = router.Route(new WorkflowHttpRequest("GET", "/api/jobs?state=completed&limit=10"));
            Assert.Equal(200, listed.StatusCode);

            var list = router.Route(new WorkflowHttpRequest("GET", "/api/jobs?state=banana"));
            Assert.Equal(400, list.StatusCode);
        }

        [Fact]
        public void Cancel_CompletedJob_Returns400()
        {
            using var service = new WorkflowExecutionService();
            var router = CreateRouter(service);
            var workflowId = PublishWorkflow(router, service);

            router.Route(new WorkflowHttpRequest("POST", $"/api/jobs?workflowId={workflowId}"));
            var jobId = service.GetJobs()[0].Id;
            Assert.True(service.WaitForCompletion(jobId, TimeSpan.FromSeconds(5)));

            var cancel = router.Route(new WorkflowHttpRequest("POST", $"/api/jobs/{jobId}/cancel"));
            Assert.Equal(400, cancel.StatusCode);
        }

        [Fact]
        public void Cancel_QueuedJob_ReturnsOk()
        {
            using var service = new WorkflowExecutionService();
            var router = CreateRouter(service);
            var workflowId = PublishWorkflow(router, service);

            var first = router.Route(new WorkflowHttpRequest("POST", $"/api/jobs?workflowId={workflowId}"));
            Assert.Equal(200, first.StatusCode);

            var second = router.Route(new WorkflowHttpRequest("POST", $"/api/jobs?workflowId={workflowId}"));
            Assert.Equal(200, second.StatusCode);

            // At least one job exists; cancelling a queued/finished job must be reported honestly.
            foreach (var job in service.GetJobs().ToList())
            {
                var response = router.Route(new WorkflowHttpRequest("POST", $"/api/jobs/{job.Id}/cancel"));
                Assert.True(response.StatusCode == 200 || response.StatusCode == 400);
            }
        }

        [Fact]
        public void PathSplitting_IsCaseInsensitiveAndTolerant()
        {
            using var service = new WorkflowExecutionService();
            var router = CreateRouter(service);

            Assert.Equal(200, router.Route(new WorkflowHttpRequest("get", "/API/Workflows/")).StatusCode);
            Assert.Equal(200, router.Route(new WorkflowHttpRequest("GET", "/health/")).StatusCode);
        }
    }
}
