using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Beep.Skia;
using Beep.Skia.Automation;
using Beep.Skia.Model;
using ExecutionContext = Beep.Skia.Model.ExecutionContext;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Concurrency hardening for the automation runtime: the server SKU runs several jobs through
    /// one engine instance and one execution service, so parallel execution must be safe.
    /// </summary>
    public class AutomationConcurrencyTests
    {
        private sealed class QuickNode : IAutomationNode
        {
            public string Id { get; } = Guid.NewGuid().ToString("N")[..6];
            public string Name { get; set; } = "QuickNode";
            public string Description => "Quick node";
            public NodeType NodeType { get; set; } = NodeType.Action;
            public NodeStatus Status { get; private set; } = NodeStatus.Idle;
            public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
            public IList<IConnectionPoint> InputConnections { get; } = new List<IConnectionPoint>();
            public IList<IConnectionPoint> OutputConnections { get; } = new List<IConnectionPoint>();
            public bool IsEnabled => true;

#pragma warning disable CS0067 // IAutomationNode requires these events; the node never raises them
            public event EventHandler<NodeExecutionEventArgs> StatusChanged;
            public event EventHandler<NodeErrorEventArgs> ErrorOccurred;
#pragma warning restore CS0067

            public Task<bool> InitializeAsync(Dictionary<string, object> configuration, CancellationToken cancellationToken = default)
                => Task.FromResult(true);

            public Task<ValidationResult> ValidateAsync(ExecutionContext inputData, CancellationToken cancellationToken = default)
                => Task.FromResult(ValidationResult.Success());

            public Task<NodeResult> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default)
                => Task.FromResult(NodeResult.CreateSuccess(new Dictionary<string, object>()));

            public Task ResetAsync() => Task.CompletedTask;
            public Dictionary<string, object> GetInputSchema() => new Dictionary<string, object>();
            public Dictionary<string, object> GetOutputSchema() => new Dictionary<string, object>();
        }

        private static WorkflowDefinition BuildWorkflow(string id)
        {
            var workflow = new WorkflowDefinition(id, "Workflow " + id);
            workflow.AddNode(new NodeDefinition("n1", "Quick", NodeType.Action, "quick"));
            return workflow;
        }

        [Fact]
        public async Task Engine_RunsWorkflowsConcurrently_AndRecordsHistorySafely()
        {
            var engine = new WorkflowEngine();
            engine.RegisterNodeType("quick", () => new QuickNode());

            const int workflows = 8;
            for (var i = 0; i < workflows; i++)
                await engine.LoadWorkflowAsync(BuildWorkflow("wf" + i));

            var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            // Execute all workflows in parallel while reading history concurrently.
            var readers = Enumerable.Range(0, 4).Select(_ => Task.Run(async () =>
            {
                try
                {
                    for (var i = 0; i < 50; i++)
                    {
                        await engine.GetExecutionHistoryAsync("wf0");
                        await Task.Yield();
                    }
                }
                catch (Exception ex) { errors.Add(ex); }
            })).ToList();

            var runners = Enumerable.Range(0, workflows).Select(i => Task.Run(async () =>
            {
                try
                {
                    var result = await engine.ExecuteWorkflowAsync("wf" + i);
                    Assert.Equal(WorkflowStatus.Completed, result.Status);
                }
                catch (Exception ex) { errors.Add(ex); }
            })).ToList();

            await Task.WhenAll(readers.Concat(runners));

            Assert.Empty(errors);

            var all = new List<WorkflowExecution>();
            for (var i = 0; i < workflows; i++)
                all.AddRange(await engine.GetExecutionHistoryAsync("wf" + i));
            Assert.Equal(workflows, all.Count);
        }

        [Fact]
        public void ExecutionService_RunsJobsConcurrently_WithoutLossOrErrors()
        {
            var engine = new WorkflowEngine();
            engine.RegisterNodeType("quick", () => new QuickNode());

            using var service = new WorkflowExecutionService(maxConcurrency: 4, engine: engine);
            service.Publish(BuildWorkflow("wf"));

            var jobs = Enumerable.Range(0, 20).Select(_ => service.Submit("wf", "tester")).ToList();
            Assert.Equal(20, jobs.Count);
            Assert.All(jobs, j => Assert.True(service.WaitForCompletion(j.Id, TimeSpan.FromSeconds(15))));
            Assert.All(jobs, j => Assert.Equal(JobState.Completed, j.State));
        }

        [Fact]
        public void ExecutionService_ConcurrentSubmitAndQuery_IsSafe()
        {
            var engine = new WorkflowEngine();
            engine.RegisterNodeType("quick", () => new QuickNode());

            using var service = new WorkflowExecutionService(maxConcurrency: 2, engine: engine);
            service.Publish(BuildWorkflow("wf"));

            var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            Parallel.For(0, 100, i =>
            {
                try
                {
                    if (i % 3 == 0) service.Submit("wf", "u" + i);
                    else if (i % 3 == 1) service.GetJobs();
                    else service.GetJobs(JobState.Completed);
                }
                catch (Exception ex) { errors.Add(ex); }
            });

            Assert.Empty(errors);
        }
    }
}
