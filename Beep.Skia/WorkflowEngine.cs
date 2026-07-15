using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Beep.Skia.Model;

namespace Beep.Skia
{
    /// <summary>
    /// Basic implementation of IWorkflowEngine that executes automation node graphs.
    /// Traverses nodes in dependency order (topological sort via connection lines),
    /// calling each IAutomationNode's ValidateAsync/ExecuteAsync in sequence.
    /// </summary>
    public class WorkflowEngine : IWorkflowEngine
    {
        private readonly ConcurrentDictionary<string, WorkflowDefinition> _workflows = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, WorkflowExecution> _executions = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, Func<IAutomationNode>> _nodeFactories = new(StringComparer.Ordinal);
        private readonly List<WorkflowExecution> _history = new();
        private bool _isRunning;
        private CancellationTokenSource _engineCts;

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
            if (!_workflows.TryGetValue(workflowId, out var wf))
                return new WorkflowResult(false, "Workflow not found");

            var execution = new WorkflowExecution(context.ExecutionId, workflowId, context)
            {
                Status = WorkflowStatus.Running,
                StartTime = DateTime.UtcNow
            };
            _executions[context.ExecutionId] = execution;

            WorkflowStarted?.Invoke(this, new WorkflowExecutionEventArgs(execution));
            var result = new WorkflowResult(true);
            bool success = true;

            try
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, _engineCts?.Token ?? CancellationToken.None);

                var sortedNodes = TopologicalSort(wf);
                var nodeResults = new Dictionary<string, object>(StringComparer.Ordinal);

                foreach (var nodeDef in sortedNodes)
                {
                    linkedCts.Token.ThrowIfCancellationRequested();

                    if (execution.Status == WorkflowStatus.Paused)
                    {
                        success = false;
                        break;
                    }

                    if (!TryCreateNode(nodeDef.NodeType, out var node))
                    {
                        execution.ErrorMessage = $"Unknown node type: {nodeDef.NodeType}";
                        execution.Status = WorkflowStatus.Failed;
                        WorkflowFailed?.Invoke(this, new WorkflowExecutionEventArgs(execution));
                        return new WorkflowResult(false, execution.ErrorMessage);
                    }

                    var nodeExec = new NodeExecution(nodeDef.NodeId, context.ExecutionId)
                    {
                        Status = NodeStatus.Running,
                        StartTime = DateTime.UtcNow
                    };
                    execution.CurrentNodeId = nodeDef.NodeId;
                    execution.NodeExecutions.Add(nodeExec);

                    // Pass previous results as input
                    var nodeContext = context.CreateChild(
                        new Dictionary<string, object>(inputData ?? new Dictionary<string, object>())
                    );
                    foreach (var kvp in nodeResults)
                        nodeContext.Data[kvp.Key] = kvp.Value;

                    // Collect inputs from predecessor nodes
                    var predecessors = wf.Connections
                        .Where(c => c.TargetNodeId == nodeDef.NodeId)
                        .ToList();
                    foreach (var pred in predecessors)
                    {
                        if (nodeResults.TryGetValue(pred.SourceNodeId, out var predResult))
                            nodeContext.PreviousResults.Add(predResult);
                    }

                    try
                    {
                        var validationResult = await node.ValidateAsync(nodeContext, linkedCts.Token);
                        if (!validationResult.IsValid)
                        {
                            nodeExec.Status = NodeStatus.Failed;
                            nodeExec.ErrorMessage = string.Join("; ", validationResult.Errors ?? Enumerable.Empty<string>());
                            execution.Status = WorkflowStatus.Failed;
                            success = false;
                            execution.ErrorMessage = $"Validation failed at {nodeDef.NodeName}: {nodeExec.ErrorMessage}";
                            break;
                        }

                        var nodeResult = await node.ExecuteAsync(nodeContext, linkedCts.Token);
                        nodeExec.Status = nodeResult.Success ? NodeStatus.Completed : NodeStatus.Failed;
                        nodeExec.Result = nodeResult;
                        nodeExec.EndTime = DateTime.UtcNow;

                        if (nodeResult.Success && nodeResult.OutputData != null)
                            nodeResults[nodeDef.NodeId] = nodeResult.OutputData;
                        else if (nodeResult.OutputData == null)
                            nodeResults[nodeDef.NodeId] = nodeContext.Data;

                        if (!nodeResult.Success)
                        {
                            nodeExec.ErrorMessage = nodeResult.ErrorMessage;
                            execution.Status = WorkflowStatus.Failed;
                            success = false;
                            execution.ErrorMessage = $"Execution failed at {nodeDef.NodeName}: {nodeResult.ErrorMessage}";
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        nodeExec.Status = NodeStatus.Failed;
                        nodeExec.ErrorMessage = ex.Message;
                        execution.Status = WorkflowStatus.Failed;
                        success = false;
                        execution.ErrorMessage = $"Exception at {nodeDef.NodeName}: {ex.Message}";
                        break;
                    }
                }

                execution.EndTime = DateTime.UtcNow;
                execution.Result = result;
                execution.Status = success ? WorkflowStatus.Completed : execution.Status;

                if (success)
                {
                    result.Success = true;
                    result.OutputData = nodeResults;
                    WorkflowCompleted?.Invoke(this, new WorkflowExecutionEventArgs(execution));
                }
                else
                {
                    result.Success = false;
                    WorkflowFailed?.Invoke(this, new WorkflowExecutionEventArgs(execution));
                }
            }
            catch (OperationCanceledException)
            {
                execution.Status = WorkflowStatus.Cancelled;
                execution.EndTime = DateTime.UtcNow;
                WorkflowCancelled?.Invoke(this, new WorkflowExecutionEventArgs(execution));
                return new WorkflowResult(false, "Execution cancelled");
            }
            catch (Exception ex)
            {
                execution.Status = WorkflowStatus.Failed;
                execution.EndTime = DateTime.UtcNow;
                execution.ErrorMessage = ex.Message;
                WorkflowFailed?.Invoke(this, new WorkflowExecutionEventArgs(execution));
                return new WorkflowResult(false, ex.Message);
            }
            finally
            {
                _executions.TryRemove(context.ExecutionId, out _);
                lock (_history) { _history.Add(execution); }
            }

            return result;
        }

        public Task<bool> PauseExecutionAsync(string executionId, CancellationToken cancellationToken = default)
        {
            if (_executions.TryGetValue(executionId, out var exec) && exec.Status == WorkflowStatus.Running)
            {
                exec.Status = WorkflowStatus.Paused;
                WorkflowPaused?.Invoke(this, new WorkflowExecutionEventArgs(exec));
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<bool> ResumeExecutionAsync(string executionId, CancellationToken cancellationToken = default)
        {
            if (_executions.TryGetValue(executionId, out var exec) && exec.Status == WorkflowStatus.Paused)
            {
                exec.Status = WorkflowStatus.Running;
                WorkflowResumed?.Invoke(this, new WorkflowExecutionEventArgs(exec));
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<bool> CancelExecutionAsync(string executionId, CancellationToken cancellationToken = default)
        {
            if (_executions.TryGetValue(executionId, out var exec))
            {
                exec.Status = WorkflowStatus.Cancelled;
                exec.EndTime = DateTime.UtcNow;
                WorkflowCancelled?.Invoke(this, new WorkflowExecutionEventArgs(exec));
                _executions.TryRemove(executionId, out _);
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
            var issues = new List<string>();
            if (workflow == null) return new ValidationResult(false, "Workflow is null");

            if (workflow.Nodes.Count == 0)
                issues.Add("Workflow has no nodes");

            foreach (var node in workflow.Nodes)
            {
                if (!TryCreateNode(node.NodeType, out var instance))
                {
                    issues.Add($"Node '{node.NodeName}' uses unknown type '{node.NodeType}'");
                    continue;
                }

                try
                {
                    var result = await instance.ValidateAsync(new ExecutionContext(workflow.Id, "validate"), cancellationToken);
                    if (!result.IsValid)
                    {
                        foreach (var err in result.Errors ?? Enumerable.Empty<string>())
                            issues.Add($"{node.NodeName}: {err}");
                    }
                }
                catch (Exception ex)
                {
                    issues.Add($"{node.NodeName}: validation threw {ex.Message}");
                }
            }

            // Check for cycles
            if (HasCycle(workflow))
                issues.Add("Workflow contains cycles");

            return new ValidationResult(issues.Count == 0, string.Join("; ", issues), issues);
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
            RegisterNodeType("Start", () => new Components.ManualTriggerNode());
            RegisterNodeType("DataInput", () => new Components.DataInputNode());
            RegisterNodeType("DataTransform", () => new Components.DataTransformNode());
            RegisterNodeType("Conditional", () => new Components.ConditionalNode());
            RegisterNodeType("DataSource", () => new Components.DataSourceAutomationNode());
            RegisterNodeType("HttpRequest", () => new Components.HttpRequestNode());
            RegisterNodeType("Timer", () => new Components.TimerTriggerNode());
        }

        private bool TryCreateNode(string typeName, out IAutomationNode node)
        {
            if (_nodeFactories.TryGetValue(typeName, out var factory))
            {
                node = factory();
                return node != null;
            }

            // Fallback: try to create by type name via reflection
            try
            {
                var type = Type.GetType(typeName, throwOnError: false);
                if (type != null && typeof(IAutomationNode).IsAssignableFrom(type))
                {
                    node = (IAutomationNode)Activator.CreateInstance(type);
                    return node != null;
                }
            }
            catch { }

            node = null;
            return false;
        }

        private List<NodeDefinition> TopologicalSort(WorkflowDefinition wf)
        {
            var inDegree = wf.Nodes.ToDictionary(n => n.NodeId, _ => 0);
            var successors = wf.Nodes.ToDictionary(n => n.NodeId, _ => new List<NodeDefinition>());

            foreach (var conn in wf.Connections)
            {
                if (inDegree.ContainsKey(conn.TargetNodeId))
                    inDegree[conn.TargetNodeId]++;
                if (successors.ContainsKey(conn.SourceNodeId))
                    successors[conn.SourceNodeId].Add(wf.Nodes.FirstOrDefault(n => n.NodeId == conn.TargetNodeId));
            }

            var queue = new Queue<NodeDefinition>();
            foreach (var n in wf.Nodes.Where(n => inDegree[n.NodeId] == 0))
                queue.Enqueue(n);

            var sorted = new List<NodeDefinition>();
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                sorted.Add(node);
                foreach (var succ in successors[node.NodeId].Where(s => s != null))
                {
                    inDegree[succ.NodeId]--;
                    if (inDegree[succ.NodeId] == 0)
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
