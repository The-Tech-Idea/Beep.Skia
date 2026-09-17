using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Beep.Skia;
using Beep.Skia.Model;
using Xunit;
using ExecutionContext = Beep.Skia.Model.ExecutionContext;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for workflow engine completion: retry policies, variables, real pause/resume, and cancel.
    /// </summary>
    public class WorkflowEngineTests
    {
        private sealed class TestNode : IAutomationNode
        {
            public string Id { get; } = Guid.NewGuid().ToString("N")[..6];
            public string Name { get; set; } = "TestNode";
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

            public int ExecuteCount;
            public Func<int, NodeResult> ExecuteHandler { get; set; }
            public int DelayMs { get; set; }
            public Dictionary<string, object> CapturedVariables { get; private set; }

            public Task<bool> InitializeAsync(Dictionary<string, object> configuration, CancellationToken cancellationToken = default)
            {
                if (configuration != null) Configuration = configuration;
                return Task.FromResult(true);
            }

            public Task<ValidationResult> ValidateAsync(ExecutionContext inputData, CancellationToken cancellationToken = default)
                => Task.FromResult(ValidationResult.Success());

            public async Task<NodeResult> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default)
            {
                int attempt = Interlocked.Increment(ref ExecuteCount);
                CapturedVariables = new Dictionary<string, object>(context.Variables);

                if (DelayMs > 0)
                    await Task.Delay(DelayMs, cancellationToken);

                return ExecuteHandler?.Invoke(attempt)
                       ?? NodeResult.CreateSuccess(new Dictionary<string, object> { ["attempt"] = attempt });
            }

            public Task ResetAsync() => Task.CompletedTask;
            public Dictionary<string, object> GetInputSchema() => new Dictionary<string, object>();
            public Dictionary<string, object> GetOutputSchema() => new Dictionary<string, object>();
        }

        private static WorkflowDefinition BuildWorkflow(string componentType, RetryPolicy retryPolicy = null)
        {
            var workflow = new WorkflowDefinition("wf_test", "Test Workflow");
            var nodeDef = new NodeDefinition("node_1", "Node 1", NodeType.Action, componentType)
            {
                RetryPolicy = retryPolicy
            };
            workflow.AddNode(nodeDef);
            return workflow;
        }

        [Fact]
        public async Task RetryPolicy_RetriesUntilSuccess()
        {
            var node = new TestNode
            {
                ExecuteHandler = attempt => attempt < 3
                    ? NodeResult.CreateFailure($"attempt {attempt} failed")
                    : NodeResult.CreateSuccess(new Dictionary<string, object> { ["attempt"] = attempt })
            };

            var engine = new WorkflowEngine();
            engine.RegisterNodeType("flaky", () => node);
            var workflow = BuildWorkflow("flaky", new RetryPolicy(3, TimeSpan.Zero));
            await engine.LoadWorkflowAsync(workflow);

            var result = await engine.ExecuteWorkflowAsync("wf_test");

            Assert.Equal(WorkflowStatus.Completed, result.Status);
            Assert.Equal(3, node.ExecuteCount);
            Assert.Single(result.NodeResults);
        }

        [Fact]
        public async Task RetryPolicy_Exhausted_Fails()
        {
            var node = new TestNode
            {
                ExecuteHandler = _ => NodeResult.CreateFailure("always fails")
            };

            var engine = new WorkflowEngine();
            engine.RegisterNodeType("failing", () => node);
            var workflow = BuildWorkflow("failing", new RetryPolicy(2, TimeSpan.Zero));
            await engine.LoadWorkflowAsync(workflow);

            var result = await engine.ExecuteWorkflowAsync("wf_test");

            Assert.Equal(WorkflowStatus.Failed, result.Status);
            Assert.Equal(3, node.ExecuteCount); // initial + 2 retries
        }

        [Fact]
        public async Task WithoutRetryPolicy_FailsImmediately()
        {
            var node = new TestNode { ExecuteHandler = _ => NodeResult.CreateFailure("boom") };

            var engine = new WorkflowEngine();
            engine.RegisterNodeType("failing", () => node);
            await engine.LoadWorkflowAsync(BuildWorkflow("failing"));

            var result = await engine.ExecuteWorkflowAsync("wf_test");

            Assert.Equal(WorkflowStatus.Failed, result.Status);
            Assert.Equal(1, node.ExecuteCount);
        }

        [Fact]
        public async Task WorkflowVariables_AreSeededIntoExecutionContext()
        {
            var node = new TestNode();
            var engine = new WorkflowEngine();
            engine.RegisterNodeType("capture", () => node);

            var workflow = BuildWorkflow("capture");
            workflow.Variables.Add(new WorkflowVariable("environment", DataType.String) { DefaultValue = "production" });
            await engine.LoadWorkflowAsync(workflow);

            var result = await engine.ExecuteWorkflowAsync("wf_test");

            Assert.Equal(WorkflowStatus.Completed, result.Status);
            Assert.NotNull(node.CapturedVariables);
            Assert.Equal("production", node.CapturedVariables["environment"]);
        }

        [Fact]
        public async Task Pause_BlocksProgress_AndResumeContinues()
        {
            var first = new TestNode { DelayMs = 150 };
            var second = new TestNode { DelayMs = 10 };
            int created = 0;

            var engine = new WorkflowEngine();
            engine.RegisterNodeType("step", () => created++ == 0 ? first : second);

            var workflow = new WorkflowDefinition("wf_test", "Pause Test");
            workflow.AddNode(new NodeDefinition("node_1", "First", NodeType.Action, "step"));
            workflow.AddNode(new NodeDefinition("node_2", "Second", NodeType.Action, "step"));
            workflow.Connections.Add(new ConnectionDefinition("c1", "node_1", "node_2"));
            await engine.LoadWorkflowAsync(workflow);

            var context = new ExecutionContext("wf_test", "exec_pause");
            var runTask = engine.ExecuteWorkflowAsync("wf_test", context);

            // Wait until the first node is executing.
            await WaitUntilAsync(() => first.ExecuteCount >= 1, 2000);
            Assert.True(await engine.PauseExecutionAsync("exec_pause"));

            int snapshot = second.ExecuteCount;
            await Task.Delay(250);
            Assert.Equal(snapshot, second.ExecuteCount); // no progress while paused

            Assert.True(await engine.ResumeExecutionAsync("exec_pause"));
            var result = await runTask;

            Assert.Equal(WorkflowStatus.Completed, result.Status);
            Assert.Equal(1, second.ExecuteCount);
        }

        [Fact]
        public async Task Cancel_StopsInFlightExecution()
        {
            var node = new TestNode { DelayMs = 3000 };

            var engine = new WorkflowEngine();
            engine.RegisterNodeType("slow", () => node);

            var workflow = BuildWorkflow("slow");
            await engine.LoadWorkflowAsync(workflow);

            var context = new ExecutionContext("wf_test", "exec_cancel");
            var runTask = engine.ExecuteWorkflowAsync("wf_test", context);

            await WaitUntilAsync(() => node.ExecuteCount >= 1, 2000);
            Assert.True(await engine.CancelExecutionAsync("exec_cancel"));

            var result = await runTask;
            Assert.Equal(WorkflowStatus.Cancelled, result.Status);
        }

        private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (!condition())
            {
                if (DateTime.UtcNow > deadline)
                    throw new TimeoutException("Condition was not met in time.");
                await Task.Delay(20);
            }
        }
    }
}
