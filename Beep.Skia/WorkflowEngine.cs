using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Beep.Skia.Model;
using ExecutionContext = Beep.Skia.Model.ExecutionContext;

namespace Beep.Skia
{
    /// <summary>
    /// Basic implementation of IWorkflowEngine that executes automation node graphs.
    /// Traverses nodes in dependency order (topological sort via connection lines),
    /// calling each IAutomationNode's InitializeAsync/ValidateAsync/ExecuteAsync in sequence.
    /// </summary>
    public class WorkflowEngine : IWorkflowEngine
    {
        private readonly ConcurrentDictionary<string, WorkflowDefinition> _workflows = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, WorkflowExecution> _executions = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, Func<IAutomationNode>> _nodeFactories = new(StringComparer.Ordinal);
        private readonly List<WorkflowExecution> _history = new();
        private bool _isRunning;
        private CancellationTokenSource _engineCts;

        /// <summary>
        /// Initializes a new workflow engine and registers the built-in automation node types.
        /// </summary>
        public WorkflowEngine()
        {
            RegisterDefaults();
        }

        private readonly ConcurrentDictionary<string, ExecutionControl> _controls = new(StringComparer.Ordinal);

        /// <summary>
        /// Per-execution control state enabling real pause/resume/cancel.
        /// The semaphore gate is available while running and held while paused.
        /// </summary>
        private sealed class ExecutionControl : IDisposable
        {
            public readonly CancellationTokenSource Cts = new CancellationTokenSource();
            public readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);
            public volatile bool Paused;

            public void PauseGate()
            {
                Paused = true;
                if (Gate.CurrentCount > 0)
                {
                    try { Gate.Wait(0); } catch { }
                }
            }

            public void ResumeGate()
            {
                Paused = false;
                if (Gate.CurrentCount == 0)
                {
                    try { Gate.Release(); } catch (SemaphoreFullException) { } catch { }
                }
            }

            public void Dispose()
            {
                try { Cts.Dispose(); } catch { }
                try { Gate.Dispose(); } catch { }
            }
        }

        public string EngineId { get; } = Guid.NewGuid().ToString("N")[..8];
        public bool IsRunning => _isRunning;
        public IReadOnlyCollection<WorkflowDefinition> LoadedWorkflows => _workflows.Values.ToList().AsReadOnly();
        public IReadOnlyCollection<WorkflowExecution> ActiveExecutions => _executions.Values.ToList().AsReadOnly();

        public event EventHandler<WorkflowExecutionEventArgs> WorkflowStarted;
        public event EventHandler<WorkflowExecutionEventArgs> WorkflowCompleted;
        public event EventHandler<WorkflowExecutionEventArgs> WorkflowFailed;
        public event EventHandler<WorkflowExecutionEventArgs> WorkflowPaused;
        public event EventHandler<WorkflowExecutionEventArgs> WorkflowResumed;
        public event EventHandler<WorkflowExecutionEventArgs> WorkflowCancelled;

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            _isRunning = true;
            _engineCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(bool graceful = true, CancellationToken cancellationToken = default)
        {
            if (!graceful)
            {
                foreach (var control in _controls.Values)
                {
                    try { control.Cts.Cancel(); } catch { }
                    control.ResumeGate();
                }
                foreach (var exec in _executions.Values)
                {
                    exec.Status = WorkflowStatus.Cancelled;
                    exec.EndTime = DateTime.UtcNow;
                }
                _executions.Clear();
            }
            _isRunning = false;
            _engineCts?.Cancel();
            return Task.CompletedTask;
        }

        public Task<bool> LoadWorkflowAsync(WorkflowDefinition workflow, CancellationToken cancellationToken = default)
        {
            if (workflow == null) return Task.FromResult(false);
            _workflows[workflow.Id] = workflow;
            return Task.FromResult(true);
        }

        public Task<bool> UnloadWorkflowAsync(string workflowId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_workflows.TryRemove(workflowId, out _));
        }

        public Task<WorkflowResult> ExecuteWorkflowAsync(
            string workflowId,
            Dictionary<string, object> inputData = null,
            ExecutionPriority priority = ExecutionPriority.Normal,
            CancellationToken cancellationToken = default)
        {
            var context = new ExecutionContext(workflowId, Guid.NewGuid().ToString("N")[..12])
            {
                Priority = priority,
                Data = inputData ?? new Dictionary<string, object>()
            };
            return ExecuteWorkflowAsync(workflowId, context, cancellationToken);
        }

        public async Task<WorkflowResult> ExecuteWorkflowAsync(
            string workflowId,
            ExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            var result = new WorkflowResult(workflowId, context.ExecutionId, WorkflowStatus.Ready);

            if (!_workflows.TryGetValue(workflowId, out var wf))
            {
                result.SetStatus(WorkflowStatus.Failed);
                result.AddError("Workflow not found");
                return result;
            }

            var execution = new WorkflowExecution(context.ExecutionId, workflowId, context)
            {
                Status = WorkflowStatus.Running,
                StartTime = DateTime.UtcNow
            };
            _executions[context.ExecutionId] = execution;

            var control = new ExecutionControl();
            _controls[context.ExecutionId] = control;

            // Seed workflow variables into the execution context.
            foreach (var variable in wf.Variables)
            {
                if (variable == null || string.IsNullOrWhiteSpace(variable.Name)) continue;
                context.Variables[variable.Name] = variable.DefaultValue;
            }

            WorkflowStarted?.Invoke(this, CreateExecutionArgs(
                workflowId, WorkflowStatus.Ready, WorkflowStatus.Running, result));

            bool success = true;
            var nodeResults = new Dictionary<string, object>(StringComparer.Ordinal);

            try
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, _engineCts?.Token ?? CancellationToken.None, control.Cts.Token);

                var sortedNodes = TopologicalSort(wf);

                foreach (var nodeDef in sortedNodes)
                {
                    linkedCts.Token.ThrowIfCancellationRequested();

                    await WaitIfPausedAsync(control, linkedCts.Token);

                    if (!TryCreateNode(nodeDef, out var node))
                    {
                        execution.Status = WorkflowStatus.Failed;
                        execution.ErrorMessage = $"Unknown node type: {nodeDef.NodeType} ({nodeDef.ComponentType})";
                        result.AddError(execution.ErrorMessage);
                        WorkflowFailed?.Invoke(this, CreateExecutionArgs(
                            workflowId, WorkflowStatus.Running, WorkflowStatus.Failed, result));
                        return result;
                    }

                    var nodeExec = new NodeExecution(nodeDef.Id, context.ExecutionId)
                    {
                        Status = NodeStatus.Executing,
                        StartTime = DateTime.UtcNow
                    };
                    execution.CurrentNodeId = nodeDef.Id;
                    execution.NodeExecutions.Add(nodeExec);

                    try
                    {
                        await node.InitializeAsync(
                            nodeDef.Configuration ?? new Dictionary<string, object>(),
                            linkedCts.Token);
                    }
                    catch (Exception ex)
                    {
                        nodeExec.Status = NodeStatus.Failed;
                        nodeExec.ErrorMessage = ex.Message;
                        nodeExec.EndTime = DateTime.UtcNow;
                        execution.Status = WorkflowStatus.Failed;
                        execution.ErrorMessage = $"Initialization failed at {nodeDef.Name}: {ex.Message}";
                        result.AddError(execution.ErrorMessage);
                        success = false;
                        break;
                    }

                    // Pass workflow input data plus results from predecessor nodes.
                    var nodeContext = context.CreateChild(new Dictionary<string, object>(context.Data));
                    foreach (var kvp in nodeResults)
                        nodeContext.Data[kvp.Key] = kvp.Value;

                    var predecessors = wf.Connections
                        .Where(c => c.TargetNodeId == nodeDef.Id)
                        .ToList();
                    foreach (var pred in predecessors)
                    {
                        if (nodeResults.TryGetValue(pred.SourceNodeId, out var predResult))
                            nodeContext.PreviousResults.Add(predResult);
                    }

                    try
                    {
                        var nodeResult = await ExecuteWithRetryAsync(nodeDef, node, nodeContext, nodeExec, linkedCts.Token);
                        nodeExec.Result = nodeResult;
                        nodeExec.EndTime = DateTime.UtcNow;

                        if (nodeResult.Success)
                        {
                            nodeExec.Status = NodeStatus.Completed;
                            nodeResults[nodeDef.Id] = nodeResult.OutputData;
                            result.AddNodeResult(nodeResult);
                        }
                        else
                        {
                            nodeExec.Status = NodeStatus.Failed;
                            nodeExec.ErrorMessage = nodeResult.ErrorMessage;
                            execution.Status = WorkflowStatus.Failed;
                            execution.ErrorMessage = $"Execution failed at {nodeDef.Name}: {nodeResult.ErrorMessage}";
                            result.AddError(execution.ErrorMessage);
                            success = false;
                            break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        nodeExec.Status = NodeStatus.Failed;
                        nodeExec.ErrorMessage = ex.Message;
                        nodeExec.EndTime = DateTime.UtcNow;
                        execution.Status = WorkflowStatus.Failed;
                        execution.ErrorMessage = $"Exception at {nodeDef.Name}: {ex.Message}";
                        result.AddError(execution.ErrorMessage);
                        success = false;
                        break;
                    }
                }

                execution.EndTime = DateTime.UtcNow;
                execution.Result = result;

                if (success)
                {
                    foreach (var kvp in nodeResults)
                        result.SetOutput(kvp.Key, kvp.Value);

                    result.SetStatus(WorkflowStatus.Completed);
                    execution.Status = WorkflowStatus.Completed;
                    WorkflowCompleted?.Invoke(this, CreateExecutionArgs(
                        workflowId, WorkflowStatus.Running, WorkflowStatus.Completed, result));
                }
                else if (execution.Status == WorkflowStatus.Paused)
                {
                    result.SetStatus(WorkflowStatus.Paused);
                    WorkflowPaused?.Invoke(this, CreateExecutionArgs(
                        workflowId, WorkflowStatus.Running, WorkflowStatus.Paused, result));
                }
                else
                {
                    result.SetStatus(WorkflowStatus.Failed);
                    WorkflowFailed?.Invoke(this, CreateExecutionArgs(
                        workflowId, WorkflowStatus.Running, WorkflowStatus.Failed, result));
                }
            }
            catch (OperationCanceledException)
            {
                execution.Status = WorkflowStatus.Cancelled;
                execution.EndTime = DateTime.UtcNow;
                execution.Result = result;
                result.SetStatus(WorkflowStatus.Cancelled);
                result.AddError("Execution cancelled");
                WorkflowCancelled?.Invoke(this, CreateExecutionArgs(
                    workflowId, WorkflowStatus.Running, WorkflowStatus.Cancelled, result));
            }
            catch (Exception ex)
            {
                execution.Status = WorkflowStatus.Failed;
                execution.EndTime = DateTime.UtcNow;
                execution.ErrorMessage = ex.Message;
                execution.Result = result;
                result.SetStatus(WorkflowStatus.Failed);
                result.AddError(ex.Message);
                WorkflowFailed?.Invoke(this, CreateExecutionArgs(
                    workflowId, WorkflowStatus.Running, WorkflowStatus.Failed, result));
            }
            finally
            {
                _executions.TryRemove(context.ExecutionId, out _);
                if (_controls.TryRemove(context.ExecutionId, out var completedControl))
                    completedControl.Dispose();
                lock (_history) { _history.Add(execution); }
            }

            return result;
        }

        public Task<bool> PauseExecutionAsync(string executionId, CancellationToken cancellationToken = default)
        {
            if (_executions.TryGetValue(executionId, out var exec) && exec.Status == WorkflowStatus.Running)
            {
                if (_controls.TryGetValue(executionId, out var control))
                {
                    control.PauseGate();
                }

                var previous = exec.Status;
                exec.Status = WorkflowStatus.Paused;
                WorkflowPaused?.Invoke(this, CreateExecutionArgs(
                    exec.WorkflowId, previous, WorkflowStatus.Paused, exec.Result));
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<bool> ResumeExecutionAsync(string executionId, CancellationToken cancellationToken = default)
        {
            if (_executions.TryGetValue(executionId, out var exec) && exec.Status == WorkflowStatus.Paused)
            {
                if (_controls.TryGetValue(executionId, out var control))
                {
                    control.ResumeGate();
                }

                var previous = exec.Status;
                exec.Status = WorkflowStatus.Running;
                WorkflowResumed?.Invoke(this, CreateExecutionArgs(
                    exec.WorkflowId, previous, WorkflowStatus.Running, exec.Result));
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<bool> CancelExecutionAsync(string executionId, CancellationToken cancellationToken = default)
        {
            if (_executions.TryGetValue(executionId, out var exec))
            {
                if (_controls.TryGetValue(executionId, out var control))
                {
                    try { control.Cts.Cancel(); } catch { }
                    control.ResumeGate();
                }

                var previous = exec.Status;
                exec.Status = WorkflowStatus.Cancelled;
                exec.EndTime = DateTime.UtcNow;
                WorkflowCancelled?.Invoke(this, CreateExecutionArgs(
                    exec.WorkflowId, previous, WorkflowStatus.Cancelled, exec.Result));
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public WorkflowExecution GetExecutionStatus(string executionId)
        {
            _executions.TryGetValue(executionId, out var exec);
            return exec;
        }

        public Task<IEnumerable<WorkflowExecution>> GetExecutionHistoryAsync(
            string workflowId, int limit = 100, CancellationToken cancellationToken = default)
        {
            lock (_history)
            {
                return Task.FromResult(_history.Where(e => e.WorkflowId == workflowId).Take(limit).AsEnumerable());
            }
        }

        public async Task<ValidationResult> ValidateWorkflowAsync(WorkflowDefinition workflow, CancellationToken cancellationToken = default)
        {
            if (workflow == null) return ValidationResult.Failure("Workflow is null");

            var issues = new List<string>();

            if (workflow.Nodes.Count == 0)
                issues.Add("Workflow has no nodes");

            foreach (var node in workflow.Nodes)
            {
                if (!TryCreateNode(node, out var instance))
                {
                    issues.Add($"Node '{node.Name}' uses unknown type '{node.NodeType}' ({node.ComponentType})");
                    continue;
                }

                try
                {
                    await instance.InitializeAsync(
                        node.Configuration ?? new Dictionary<string, object>(),
                        cancellationToken);

                    var result = await instance.ValidateAsync(new ExecutionContext(workflow.Id, "validate"), cancellationToken);
                    if (!result.IsValid)
                    {
                        foreach (var err in result.Errors ?? Enumerable.Empty<string>())
                            issues.Add($"{node.Name}: {err}");
                    }
                }
                catch (Exception ex)
                {
                    issues.Add($"{node.Name}: validation threw {ex.Message}");
                }
            }

            // Check for cycles
            if (HasCycle(workflow))
                issues.Add("Workflow contains cycles");

            return new ValidationResult(issues.Count == 0, issues);
        }

        public void RegisterNodeType(string nodeType, Func<IAutomationNode> factory)
        {
            if (!string.IsNullOrEmpty(nodeType) && factory != null)
                _nodeFactories[nodeType] = factory;
        }

        public bool UnregisterNodeType(string nodeType)
            => _nodeFactories.TryRemove(nodeType, out _);

        public IEnumerable<string> GetRegisteredNodeTypes()
            => _nodeFactories.Keys;

        public void RegisterDefaults()
        {
            // Register by concrete type name so workflows produced by DrawingManager.ToWorkflowDefinition resolve.
            RegisterNodeType(typeof(Components.ManualTriggerNode).FullName, () => new Components.ManualTriggerNode());
            RegisterNodeType(typeof(Components.TimerTriggerNode).FullName, () => new Components.TimerTriggerNode());
            RegisterNodeType(typeof(Components.DataInputNode).FullName, () => new Components.DataInputNode());
            RegisterNodeType(typeof(Components.DataTransformNode).FullName, () => new Components.DataTransformNode());
            RegisterNodeType(typeof(Components.ConditionalNode).FullName, () => new Components.ConditionalNode());
            RegisterNodeType(typeof(Components.DataSourceAutomationNode).FullName, () => new Components.DataSourceAutomationNode());
            RegisterNodeType(typeof(Components.HttpRequestNode).FullName, () => new Components.HttpRequestNode());

            // Friendly aliases for programmatic registration.
            RegisterNodeType("Start", () => new Components.ManualTriggerNode());
            RegisterNodeType("DataInput", () => new Components.DataInputNode());
            RegisterNodeType("DataTransform", () => new Components.DataTransformNode());
            RegisterNodeType("Conditional", () => new Components.ConditionalNode());
            RegisterNodeType("DataSource", () => new Components.DataSourceAutomationNode());
            RegisterNodeType("HttpRequest", () => new Components.HttpRequestNode());
            RegisterNodeType("Timer", () => new Components.TimerTriggerNode());
        }

        private WorkflowExecutionEventArgs CreateExecutionArgs(
            string workflowId, WorkflowStatus previous, WorkflowStatus current, WorkflowResult result)
        {
            return new WorkflowExecutionEventArgs(workflowId, previous, current) { Result = result };
        }

        /// <summary>
        /// Blocks the execution loop while the execution is paused (and resumes when released).
        /// </summary>
        private static async Task WaitIfPausedAsync(ExecutionControl control, CancellationToken token)
        {
            if (!control.Paused) return;
            await control.Gate.WaitAsync(token).ConfigureAwait(false);
            try { control.Gate.Release(); } catch { }
        }

        /// <summary>
        /// Runs a node's validation + execution with retries according to its retry policy.
        /// </summary>
        private static async Task<NodeResult> ExecuteWithRetryAsync(
            NodeDefinition nodeDef,
            IAutomationNode node,
            ExecutionContext nodeContext,
            NodeExecution nodeExec,
            CancellationToken token)
        {
            var policy = nodeDef.RetryPolicy;
            int attempt = 0;

            while (true)
            {
                try
                {
                    var validation = await node.ValidateAsync(nodeContext, token).ConfigureAwait(false);
                    if (!validation.IsValid)
                    {
                        var error = string.Join("; ", validation.Errors ?? Enumerable.Empty<string>());
                        if (CanRetry(policy, attempt))
                        {
                            attempt++;
                            RecordAttempt(nodeExec, attempt);
                            await PrepareRetryAsync(node, nodeDef, policy, attempt, token).ConfigureAwait(false);
                            continue;
                        }
                        return NodeResult.CreateFailure(error);
                    }

                    var result = await node.ExecuteAsync(nodeContext, token).ConfigureAwait(false);
                    if (result.Success) return result;

                    if (CanRetry(policy, attempt))
                    {
                        attempt++;
                        RecordAttempt(nodeExec, attempt);
                        await PrepareRetryAsync(node, nodeDef, policy, attempt, token).ConfigureAwait(false);
                        continue;
                    }
                    return result;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (CanRetry(policy, attempt))
                    {
                        attempt++;
                        RecordAttempt(nodeExec, attempt);
                        await PrepareRetryAsync(node, nodeDef, policy, attempt, token).ConfigureAwait(false);
                        continue;
                    }
                    return NodeResult.CreateFailure(ex.Message, ex);
                }
            }
        }

        private static bool CanRetry(RetryPolicy policy, int attemptsSoFar)
            => policy != null && attemptsSoFar < policy.MaxRetries;

        private static void RecordAttempt(NodeExecution nodeExec, int attempt)
        {
            nodeExec.Metadata["RetryAttempt"] = attempt;
        }

        private static async Task PrepareRetryAsync(
            IAutomationNode node,
            NodeDefinition nodeDef,
            RetryPolicy policy,
            int attempt,
            CancellationToken token)
        {
            var delay = ComputeDelay(policy, attempt);
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, token).ConfigureAwait(false);

            try { await node.ResetAsync().ConfigureAwait(false); } catch { }
            try
            {
                await node.InitializeAsync(
                    nodeDef.Configuration ?? new Dictionary<string, object>(),
                    token).ConfigureAwait(false);
            }
            catch { }
        }

        private static TimeSpan ComputeDelay(RetryPolicy policy, int attempt)
        {
            if (policy == null) return TimeSpan.Zero;
            var baseDelay = policy.RetryDelay;
            if (baseDelay <= TimeSpan.Zero) return TimeSpan.Zero;

            TimeSpan delay;
            switch (policy.BackoffStrategy)
            {
                case RetryBackoffStrategy.Exponential:
                    delay = TimeSpan.FromTicks(baseDelay.Ticks * (long)Math.Pow(2, Math.Max(0, attempt - 1)));
                    break;
                case RetryBackoffStrategy.Linear:
                    delay = TimeSpan.FromTicks(baseDelay.Ticks * Math.Max(1, attempt));
                    break;
                default:
                    delay = baseDelay;
                    break;
            }

            if (policy.MaxRetryDelay.HasValue && delay > policy.MaxRetryDelay.Value)
                delay = policy.MaxRetryDelay.Value;
            return delay;
        }

        private bool TryCreateNode(NodeDefinition nodeDef, out IAutomationNode node)
        {
            node = null;
            if (nodeDef == null) return false;

            if (!string.IsNullOrEmpty(nodeDef.ComponentType))
            {
                // 1) Explicit registration by component type name.
                if (_nodeFactories.TryGetValue(nodeDef.ComponentType, out var byComponent))
                {
                    node = byComponent();
                    return node != null;
                }

                // 2) Explicit registration by short type name (e.g., "ManualTriggerNode").
                var typeName = nodeDef.ComponentType.Split(',')[0];
                var shortName = typeName.Split('.')[^1];
                if (_nodeFactories.TryGetValue(shortName, out var byShortName))
                {
                    node = byShortName();
                    return node != null;
                }
            }

            // 3) Explicit registration by node category (e.g., "Trigger", "Action").
            if (_nodeFactories.TryGetValue(nodeDef.NodeType.ToString(), out var byNodeType))
            {
                node = byNodeType();
                return node != null;
            }

            // 4) Reflection fallback by component type name.
            if (!string.IsNullOrEmpty(nodeDef.ComponentType))
            {
                try
                {
                    var type = Type.GetType(nodeDef.ComponentType, throwOnError: false);
                    if (type != null && typeof(IAutomationNode).IsAssignableFrom(type))
                    {
                        node = (IAutomationNode)Activator.CreateInstance(type);
                        return node != null;
                    }
                }
                catch { }
            }

            return false;
        }

        private List<NodeDefinition> TopologicalSort(WorkflowDefinition wf)
        {
            var inDegree = wf.Nodes.ToDictionary(n => n.Id, _ => 0);
            var successors = wf.Nodes.ToDictionary(n => n.Id, _ => new List<NodeDefinition>());

            foreach (var conn in wf.Connections)
            {
                if (inDegree.ContainsKey(conn.TargetNodeId))
                    inDegree[conn.TargetNodeId]++;
                if (successors.ContainsKey(conn.SourceNodeId))
                    successors[conn.SourceNodeId].Add(wf.Nodes.FirstOrDefault(n => n.Id == conn.TargetNodeId));
            }

            var queue = new Queue<NodeDefinition>();
            foreach (var n in wf.Nodes.Where(n => inDegree[n.Id] == 0))
                queue.Enqueue(n);

            var sorted = new List<NodeDefinition>();
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                sorted.Add(node);
                foreach (var succ in successors[node.Id].Where(s => s != null))
                {
                    inDegree[succ.Id]--;
                    if (inDegree[succ.Id] == 0)
                        queue.Enqueue(succ);
                }
            }

            return sorted;
        }

        private bool HasCycle(WorkflowDefinition wf)
        {
            var sorted = TopologicalSort(wf);
            return sorted.Count != wf.Nodes.Count;
        }
    }
}
