using System;
using System.Text.Json;
using Beep.Skia;
using Beep.Skia.Automation;
using Beep.Skia.Serialization;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for the transport-neutral execution API facade: diagram publishing, job lifecycle,
    /// inputs parsing, listing/filtering, and JSON serialization.
    /// </summary>
    public class WorkflowExecutionApiTests
    {
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

        [Fact]
        public void PublishDiagram_RegistersExecutableWorkflow()
        {
            using var service = new WorkflowExecutionService();
            var api = new WorkflowExecutionApi(service);

            var response = api.PublishDiagram(BuildAutomationDiagramJson(), "Nightly Sync");

            Assert.True(response.Success, response.Error);
            Assert.Single(service.Workflows);
            Assert.Equal("Nightly Sync", service.Workflows[0].Name);
            Assert.Single(service.Workflows[0].Nodes);
        }

        [Fact]
        public void PublishDiagram_InvalidPayload_Fails()
        {
            using var service = new WorkflowExecutionService();
            var api = new WorkflowExecutionApi(service);

            Assert.False(api.PublishDiagram(null).Success);
            Assert.False(api.PublishDiagram("{ not json }").Success);

            var flowchartOnly = new DiagramDto();
            flowchartOnly.Components.Add(new ComponentDto
            {
                Type = "Beep.Skia.Flowchart.ProcessNode, Beep.Skia.FlowChart",
                Name = "Process",
                Width = 100,
                Height = 50
            });
            var response = api.PublishDiagram(JsonSerializer.Serialize(flowchartOnly));
            Assert.False(response.Success);
            Assert.Contains("automation nodes", response.Error);
        }

        [Fact]
        public void Submit_UnknownWorkflow_Fails()
        {
            using var service = new WorkflowExecutionService();
            var api = new WorkflowExecutionApi(service);

            var response = api.Submit("nope");

            Assert.False(response.Success);
            Assert.Contains("not published", response.Error);
        }

        [Fact]
        public void Submit_WithInputsJson_RunsToCompletion()
        {
            using var service = new WorkflowExecutionService();
            var api = new WorkflowExecutionApi(service);
            Assert.True(api.PublishDiagram(BuildAutomationDiagramJson()).Success);

            var workflowId = service.Workflows[0].Id;
            var response = api.Submit(workflowId, "alice", "{\"environment\":\"staging\",\"batchSize\":5,\"dryRun\":true}");
            Assert.True(response.Success, response.Error);

            var jobId = service.GetJobs()[0].Id;
            Assert.True(service.WaitForCompletion(jobId, TimeSpan.FromSeconds(5)));

            var job = service.GetJob(jobId);
            Assert.Equal(JobState.Completed, job.State);
            Assert.Equal("staging", job.Inputs["environment"]);
            Assert.Equal(5L, job.Inputs["batchSize"]);
            Assert.Equal(true, job.Inputs["dryRun"]);

            var detail = api.GetJob(jobId);
            Assert.True(detail.Success);
            var json = api.ToJson(detail);
            Assert.Contains("\"state\": \"Completed\"", json);
        }

        [Fact]
        public void Submit_MalformedInputsJson_Fails()
        {
            using var service = new WorkflowExecutionService();
            var api = new WorkflowExecutionApi(service);
            api.PublishDiagram(BuildAutomationDiagramJson());

            var response = api.Submit(service.Workflows[0].Id, inputsJson: "[1,2,3]");

            Assert.False(response.Success);
        }

        [Fact]
        public void ListJobs_FiltersAndValidatesState()
        {
            using var service = new WorkflowExecutionService();
            var api = new WorkflowExecutionApi(service);
            api.PublishDiagram(BuildAutomationDiagramJson());
            var workflowId = service.Workflows[0].Id;

            api.Submit(workflowId);
            var jobId = service.GetJobs()[0].Id;
            Assert.True(service.WaitForCompletion(jobId, TimeSpan.FromSeconds(5)));

            Assert.True(api.ListJobs("completed").Success);
            Assert.True(api.ListJobs().Success);
            Assert.False(api.ListJobs("banana").Success);
        }

        [Fact]
        public void Cancel_FinishedJob_ReturnsError()
        {
            using var service = new WorkflowExecutionService();
            var api = new WorkflowExecutionApi(service);
            api.PublishDiagram(BuildAutomationDiagramJson());
            api.Submit(service.Workflows[0].Id);

            var jobId = service.GetJobs()[0].Id;
            Assert.True(service.WaitForCompletion(jobId, TimeSpan.FromSeconds(5)));

            var response = api.Cancel(jobId);
            Assert.False(response.Success);
        }

        [Fact]
        public void ListWorkflows_ReportsPublishedWorkflows()
        {
            using var service = new WorkflowExecutionService();
            var api = new WorkflowExecutionApi(service);
            api.PublishDiagram(BuildAutomationDiagramJson(), "One");

            var response = api.ListWorkflows();

            Assert.True(response.Success);
            var json = api.ToJson(response);
            Assert.Contains("One", json);
            Assert.Contains("workflowId", json);
        }
    }
}
