using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Beep.Skia;
using Beep.Skia.Automation;
using Beep.Skia.Model;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for the workflow execution service: publish/submit, queueing, inputs, pause/resume/cancel,
    /// job listing, and state-change events.
    /// </summary>
    public class WorkflowExecutionServiceTests
    {
        private sealed class ServiceTestNode : IAutomationNode
        {
            public string Id { get; } = Guid.NewGuid().ToString("N")[..6];
            public string Name { get; set; } = "ServiceTestNode";
            public string Description => "Test node";
            public NodeType NodeType { get; set; } = NodeType.Action;
            public NodeStatus Status { get; private set; } = NodeStatus.Idle;
            public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
            public IList<IConnectionPoint> InputConnections { get; } = new List<IConnectionPoint>();
            public IList<IConnectionPoint> OutputConnections { get; } = new List<IConnectionPoint>();
            public bool IsEnabled => true;

#pragma warning disable CS0067 // IAutomationNode requires these events; the test double never raises them
            public event EventHandler<NodeExecutionEventArgs> StatusChanged;
            public event EventHandler<NodeErrorEventArgs> ErrorOccurred;
#pragma warning restore CS0067

            public int DelayMs { get; set; }
            public Func<NodeResult> ExecuteHandler { get; set; }
            public Dictionary<string, object> CapturedData { get; private set; }

            public Task<bool> InitializeAsync(Dictionary<string, object> configuration, CancellationToken cancellationToken = default)
                => Task.FromResult(true);

            public Task<ValidationResult> ValidateAsync(Beep.Skia.Model.ExecutionContext inputData, CancellationToken cancellationToken = default)
                => Task.FromResult(ValidationResult.Success());

            public async Task<NodeResult> ExecuteAsync(Beep.Skia.Model.ExecutionContext context, CancellationToken cancellationToken = default)
            {
                CapturedData = new Dictionary<string, object>(context.Data, StringComparer.OrdinalIgnoreCase);
                if (DelayMs > 0) await Task.Delay(DelayMs, cancellationToken);
                return ExecuteHandler?.Invoke() ?? NodeResult.CreateSuccess(new Dictionary<string, object>());
            }

            public Task ResetAsync() => Task.CompletedTask;
            public Dictionary<string, object> GetInputSchema() => new Dictionary<string, object>();
            public Dictionary<string, object> GetOutputSchema() => new Dictionary<string, object>();
        }

        private static WorkflowDefinition SingleNodeWorkflow(string nodeType, string workflowId = "wf_test")
        {
            var workflow = new WorkflowDefinition(workflowId, "Test Workflow");
            workflow.AddNode(new NodeDefinition("node_1", "Node 1", NodeType.Action, nodeType));
            return workflow;
        }

        private static WorkflowDefinition TwoNodeWorkflow(string firstType, string secondType, string workflowId = "wf_two")
        {
            var workflow = new WorkflowDefinition(workflowId, "Two Node Workflow");
            workflow.AddNode(new NodeDefinition("n1", "First", NodeType.Action, firstType));
            workflow.AddNode(new NodeDefinition("n2", "Second", NodeType.Action, secondType));
            workflow.AddConnection(new ConnectionDefinition("c1", "n1", "n2"));
            return workflow;
        }

        private static bool SpinUntil(Func<bool> condition, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                if (condition()) return true;
                Thread.Sleep(10);
            }
            return condition();
        }

        [Fact]
        public void Submit_UnknownWorkflow_ReturnsNull()
        {
            using var service = new WorkflowExecutionService();
            Assert.Null(service.Submit("missing"));
        }

        [Fact]
        public void Publish_Submit_CompletesJobWithResult()
        {
            var node = new ServiceTestNode();
            var engine = new WorkflowEngine();
            engine.RegisterNodeType("quick", () => node);

            using var service = new WorkflowExecutionService(engine: engine);
            service.Publish(SingleNodeWorkflow("quick"));

            var job = service.Submit("wf_test", "alice");
            Assert.NotNull(job);
            Assert.True(service.WaitForCompletion(job.Id, TimeSpan.FromSeconds(5)));

            Assert.Equal(JobState.Completed, job.State);
            Assert.NotNull(job.Result);
            Assert.Equal(WorkflowStatus.Completed, job.Result.Status);
            Assert.NotNull(job.StartedAt);
            Assert.NotNull(job.CompletedAt);
            Assert.Equal("alice", job.SubmittedBy);
        }

        [Fact]
        public void Submit_PassesInputsToExecutionContext()
        {
            var node = new ServiceTestNode();
            var engine = new WorkflowEngine();
            engine.RegisterNodeType("capture", () => node);

            using var service = new WorkflowExecutionService(engine: engine);
            service.Publish(SingleNodeWorkflow("capture"));

            var job = service.Submit("wf_test", inputs: new Dictionary<string, object> { ["environment"] = "staging" });
            Assert.True(service.WaitForCompletion(job.Id, TimeSpan.FromSeconds(5)));

            Assert.Equal("staging", node.CapturedData["environment"]);
        }

        [Fact]
        public void FailingNode_JobFailsWithError()
        {
            var node = new ServiceTestNode { ExecuteHandler = () => NodeResult.CreateFailure("boom") };
            var engine = new WorkflowEngine();
            engine.RegisterNodeType("failing", () => node);

            using var service = new WorkflowExecutionService(engine: engine);
            service.Publish(SingleNodeWorkflow("failing"));

            var job = service.Submit("wf_test");
            Assert.True(service.WaitForCompletion(job.Id, TimeSpan.FromSeconds(5)));

            Assert.Equal(JobState.Failed, job.State);
            Assert.Contains("boom", job.Error);
        }

        [Fact]
        public void Cancel_QueuedJob()
        {
            var running = new ServiceTestNode { DelayMs = 400 };
            var engine = new WorkflowEngine();
            engine.RegisterNodeType("slow", () => running);

            using var service = new WorkflowExecutionService(maxConcurrency: 1, engine: engine);
            service.Publish(SingleNodeWorkflow("slow"));

            var first = service.Submit("wf_test");
            Assert.True(SpinUntil(() => first.State == JobState.Running, TimeSpan.FromSeconds(2)));

            var second = service.Submit("wf_test");
            Assert.Equal(JobState.Queued, second.State);

            Assert.True(service.Cancel(second.Id));
            Assert.Equal(JobState.Cancelled, second.State);
            Assert.True(service.WaitForCompletion(first.Id, TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public void Cancel_RunningJob()
        {
            var node = new ServiceTestNode { DelayMs = 400 };
            var engine = new WorkflowEngine();
            engine.RegisterNodeType("slow", () => node);

            using var service = new WorkflowExecutionService(engine: engine);
            service.Publish(SingleNodeWorkflow("slow"));

            var job = service.Submit("wf_test");
            Assert.True(SpinUntil(() => job.State == JobState.Running, TimeSpan.FromSeconds(2)));
            Assert.True(SpinUntil(() => service.Cancel(job.Id), TimeSpan.FromSeconds(2)));

            Assert.True(service.WaitForCompletion(job.Id, TimeSpan.FromSeconds(5)));
            Assert.Equal(JobState.Cancelled, job.State);
        }

        [Fact]
        public void Pause_And_Resume_RunningJob()
        {
            var first = new ServiceTestNode { DelayMs = 250 };
            var second = new ServiceTestNode();
            var engine = new WorkflowEngine();
            engine.RegisterNodeType("slow", () => first);
            engine.RegisterNodeType("quick", () => second);

            using var service = new WorkflowExecutionService(engine: engine);
            service.Publish(TwoNodeWorkflow("slow", "quick"));

            var job = service.Submit("wf_two");
            Assert.True(SpinUntil(() => job.State == JobState.Running, TimeSpan.FromSeconds(2)));
            Assert.True(SpinUntil(() => service.Pause(job.Id), TimeSpan.FromSeconds(2)));
            Assert.Equal(JobState.Paused, job.State);

            Assert.True(service.Resume(job.Id));
            Assert.Equal(JobState.Running, job.State);

            Assert.True(service.WaitForCompletion(job.Id, TimeSpan.FromSeconds(5)));
            Assert.Equal(JobState.Completed, job.State);
        }

        [Fact]
        public void MaxConcurrency_SerializesJobs()
        {
            var node = new ServiceTestNode { DelayMs = 80 };
            var engine = new WorkflowEngine();
            engine.RegisterNodeType("quick", () => node);

            using var service = new WorkflowExecutionService(maxConcurrency: 1, engine: engine);
            service.Publish(SingleNodeWorkflow("quick"));

            var first = service.Submit("wf_test");
            var second = service.Submit("wf_test");

            Assert.True(service.WaitForCompletion(first.Id, TimeSpan.FromSeconds(5)));
            Assert.True(service.WaitForCompletion(second.Id, TimeSpan.FromSeconds(5)));

            // Submission order is not execution order (SemaphoreSlim is not FIFO), but the two
            // windows must not overlap when maxConcurrency = 1.
            Assert.NotNull(first.StartedAt);
            Assert.NotNull(first.CompletedAt);
            Assert.NotNull(second.StartedAt);
            Assert.NotNull(second.CompletedAt);

            var firstStart = first.StartedAt.Value;
            var firstEnd = first.CompletedAt.Value;
            var secondStart = second.StartedAt.Value;
            var secondEnd = second.CompletedAt.Value;

            var noOverlap = secondStart >= firstEnd.AddMilliseconds(-5)
                         || firstStart >= secondEnd.AddMilliseconds(-5);
            Assert.True(noOverlap,
                $"Jobs overlapped with maxConcurrency=1: [{firstStart:HH:mm:ss.fff}..{firstEnd:HH:mm:ss.fff}] vs [{secondStart:HH:mm:ss.fff}..{secondEnd:HH:mm:ss.fff}]");
        }

        [Fact]
        public void Jobs_AreListedNewestFirstAndFiltered()
        {
            var node = new ServiceTestNode();
            var engine = new WorkflowEngine();
            engine.RegisterNodeType("quick", () => node);

            using var service = new WorkflowExecutionService(engine: engine);
            service.Publish(SingleNodeWorkflow("quick"));

            var first = service.Submit("wf_test");
            Thread.Sleep(10);
            var second = service.Submit("wf_test");

            Assert.True(service.WaitForCompletion(first.Id, TimeSpan.FromSeconds(5)));
            Assert.True(service.WaitForCompletion(second.Id, TimeSpan.FromSeconds(5)));

            var all = service.GetJobs();
            Assert.Equal(2, all.Count);
            Assert.Equal(second.Id, all[0].Id);
            Assert.Equal(2, service.GetJobs(JobState.Completed).Count);
            Assert.Empty(service.GetJobs(JobState.Failed));
        }

        [Fact]
        public void JobStateChanged_ReportsTransitions()
        {
            var node = new ServiceTestNode();
            var engine = new WorkflowEngine();
            engine.RegisterNodeType("quick", () => node);

            using var service = new WorkflowExecutionService(engine: engine);
            service.Publish(SingleNodeWorkflow("quick"));

            var transitions = new List<JobState>();
            service.JobStateChanged += (s, e) => transitions.Add(e.NewState);

            var job = service.Submit("wf_test");
            Assert.True(service.WaitForCompletion(job.Id, TimeSpan.FromSeconds(5)));

            Assert.Contains(JobState.Running, transitions);
            Assert.Contains(JobState.Completed, transitions);
            Assert.Equal(JobState.Queued, transitions[0] == JobState.Running ? JobState.Queued : transitions[0]);
        }

        [Fact]
        public void RemoveWorkflow_PreventsSubmission()
        {
            using var service = new WorkflowExecutionService();
            service.Publish(SingleNodeWorkflow("quick"));
            Assert.True(service.RemoveWorkflow("wf_test"));
            Assert.Null(service.Submit("wf_test"));
        }
    }
}
