using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace Beep.Skia.Model
{
    /// <summary>
    /// Represents the definition of a workflow including its nodes, connections, and metadata.
    /// This class provides the blueprint for executing automation workflows.
    /// </summary>
    public class WorkflowDefinition
    {
        /// <summary>
        /// Initializes a new instance of the WorkflowDefinition class.
        /// </summary>
        /// <param name="id">The unique identifier for the workflow.</param>
        /// <param name="name">The display name of the workflow.</param>
        public WorkflowDefinition(string id, string name)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Version = "1.0.0";
            CreatedDate = DateTime.UtcNow;
            Nodes = new List<NodeDefinition>();
            Connections = new List<ConnectionDefinition>();
            Variables = new List<WorkflowVariable>();
            Triggers = new List<TriggerDefinition>();
            Configuration = new Dictionary<string, object>();
            Metadata = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets the unique identifier for the workflow.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets or sets the display name of the workflow.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of what this workflow does.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the version of this workflow definition.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets the date when this workflow was created.
        /// </summary>
        public DateTime CreatedDate { get; }

        /// <summary>
        /// Gets or sets the date when this workflow was last modified.
        /// </summary>
        public DateTime? LastModifiedDate { get; set; }

        /// <summary>
        /// Gets or sets the author of this workflow.
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Gets or sets the category or tags for organizing workflows.
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets a value indicating whether this workflow is enabled for execution.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets the collection of nodes in this workflow.
        /// </summary>
        public IList<NodeDefinition> Nodes { get; }

        /// <summary>
        /// Gets the collection of connections between nodes in this workflow.
        /// </summary>
        public IList<ConnectionDefinition> Connections { get; }

        /// <summary>
        /// Gets the collection of workflow variables.
        /// </summary>
        public IList<WorkflowVariable> Variables { get; }

        /// <summary>
        /// Gets the collection of triggers that can start this workflow.
        /// </summary>
        public IList<TriggerDefinition> Triggers { get; }

        /// <summary>
        /// Gets or sets the global configuration for this workflow.
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; }

        /// <summary>
        /// Gets or sets additional metadata about this workflow.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }

        /// <summary>
        /// Gets or sets the maximum execution timeout for this workflow.
        /// </summary>
        public TimeSpan? MaxExecutionTime { get; set; }

        /// <summary>
        /// Gets or sets the execution priority for this workflow.
        /// </summary>
        public ExecutionPriority Priority { get; set; } = ExecutionPriority.Normal;

        /// <summary>
        /// Adds a node to the workflow.
        /// </summary>
        /// <param name="node">The node definition to add.</param>
        public void AddNode(NodeDefinition node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (Nodes.Any(n => n.Id == node.Id))
                throw new InvalidOperationException($"A node with ID '{node.Id}' already exists in the workflow.");
            
            Nodes.Add(node);
            LastModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Removes a node from the workflow and all its connections.
        /// </summary>
        /// <param name="nodeId">The ID of the node to remove.</param>
        /// <returns>True if the node was removed, false if it was not found.</returns>
        public bool RemoveNode(string nodeId)
        {
            var node = Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node == null) return false;

            // Remove all connections involving this node
            var connectionsToRemove = Connections.Where(c => c.SourceNodeId == nodeId || c.TargetNodeId == nodeId).ToList();
            foreach (var connection in connectionsToRemove)
            {
                Connections.Remove(connection);
            }

            Nodes.Remove(node);
            LastModifiedDate = DateTime.UtcNow;
            return true;
        }

        /// <summary>
        /// Adds a connection between two nodes.
        /// </summary>
        /// <param name="connection">The connection definition to add.</param>
        public void AddConnection(ConnectionDefinition connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            
            var sourceNode = Nodes.FirstOrDefault(n => n.Id == connection.SourceNodeId);
            var targetNode = Nodes.FirstOrDefault(n => n.Id == connection.TargetNodeId);
            
            if (sourceNode == null)
                throw new InvalidOperationException($"Source node '{connection.SourceNodeId}' not found in workflow.");
            if (targetNode == null)
                throw new InvalidOperationException($"Target node '{connection.TargetNodeId}' not found in workflow.");

            Connections.Add(connection);
            LastModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Removes a connection from the workflow.
        /// </summary>
        /// <param name="connectionId">The ID of the connection to remove.</param>
        /// <returns>True if the connection was removed, false if it was not found.</returns>
        public bool RemoveConnection(string connectionId)
        {
            var connection = Connections.FirstOrDefault(c => c.Id == connectionId);
            if (connection == null) return false;

            Connections.Remove(connection);
            LastModifiedDate = DateTime.UtcNow;
            return true;
        }

        /// <summary>
        /// Gets all nodes that should execute first (nodes with no incoming connections or trigger nodes).
        /// </summary>
        /// <returns>Collection of starting node definitions.</returns>
        public IEnumerable<NodeDefinition> GetStartingNodes()
        {
            var nodesWithIncomingConnections = Connections.Select(c => c.TargetNodeId).ToHashSet();
            return Nodes.Where(n => n.NodeType == NodeType.Trigger || !nodesWithIncomingConnections.Contains(n.Id));
        }

        /// <summary>
        /// Gets all nodes that connect to the specified node as input.
        /// </summary>
        /// <param name="nodeId">The ID of the target node.</param>
        /// <returns>Collection of source node definitions.</returns>
        public IEnumerable<NodeDefinition> GetSourceNodes(string nodeId)
        {
            var sourceNodeIds = Connections.Where(c => c.TargetNodeId == nodeId).Select(c => c.SourceNodeId);
            return Nodes.Where(n => sourceNodeIds.Contains(n.Id));
        }

        /// <summary>
        /// Gets all nodes that connect from the specified node as output.
        /// </summary>
        /// <param name="nodeId">The ID of the source node.</param>
        /// <returns>Collection of target node definitions.</returns>
        public IEnumerable<NodeDefinition> GetTargetNodes(string nodeId)
        {
            var targetNodeIds = Connections.Where(c => c.SourceNodeId == nodeId).Select(c => c.TargetNodeId);
            return Nodes.Where(n => targetNodeIds.Contains(n.Id));
        }

        /// <summary>
        /// Validates the workflow definition for correctness and completeness.
        /// </summary>
        /// <returns>A validation result indicating any issues found.</returns>
        public ValidationResult Validate()
        {
            var errors = new List<string>();

            // Check for nodes
            if (!Nodes.Any())
                errors.Add("Workflow must contain at least one node.");

            // Check for starting nodes
            var startingNodes = GetStartingNodes().ToList();
            if (!startingNodes.Any())
                errors.Add("Workflow must have at least one starting node (trigger or node with no inputs).");

            // Check for duplicate node IDs
            var duplicateNodeIds = Nodes.GroupBy(n => n.Id).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var duplicateId in duplicateNodeIds)
            {
                errors.Add($"Duplicate node ID found: '{duplicateId}'.");
            }

            // Check for circular dependencies
            if (HasCircularDependencies())
                errors.Add("Workflow contains circular dependencies.");

            // Validate individual nodes
            foreach (var node in Nodes)
            {
                var nodeValidation = node.Validate();
                if (!nodeValidation.IsValid)
                {
                    errors.AddRange(nodeValidation.Errors.Select(e => $"Node '{node.Id}': {e}"));
                }
            }

            // Validate connections
            foreach (var connection in Connections)
            {
                var connectionValidation = connection.Validate();
                if (!connectionValidation.IsValid)
                {
                    errors.AddRange(connectionValidation.Errors.Select(e => $"Connection '{connection.Id}': {e}"));
                }
            }

            return errors.Any() ? ValidationResult.Failure(errors) : ValidationResult.Success();
        }

        /// <summary>
        /// Checks if the workflow has circular dependencies using depth-first search.
        /// </summary>
        /// <returns>True if circular dependencies are found, false otherwise.</returns>
        private bool HasCircularDependencies()
        {
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            foreach (var node in Nodes)
            {
                if (HasCircularDependenciesRecursive(node.Id, visited, recursionStack))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Recursive helper method for circular dependency detection.
        /// </summary>
        private bool HasCircularDependenciesRecursive(string nodeId, HashSet<string> visited, HashSet<string> recursionStack)
        {
            if (recursionStack.Contains(nodeId))
                return true;

            if (visited.Contains(nodeId))
                return false;

            visited.Add(nodeId);
            recursionStack.Add(nodeId);

            var targetNodes = GetTargetNodes(nodeId);
            foreach (var targetNode in targetNodes)
            {
                if (HasCircularDependenciesRecursive(targetNode.Id, visited, recursionStack))
                    return true;
            }

            recursionStack.Remove(nodeId);
            return false;
        }

        /// <summary>
        /// Creates a deep copy of this workflow definition.
        /// </summary>
        /// <param name="newId">Optional new ID for the copied workflow.</param>
        /// <param name="newName">Optional new name for the copied workflow.</param>
        /// <returns>A new WorkflowDefinition instance that is a copy of this one.</returns>
        public WorkflowDefinition Clone(string newId = null, string newName = null)
        {
            var clone = new WorkflowDefinition(newId ?? $"{Id}_copy", newName ?? $"{Name} (Copy)")
            {
                Description = Description,
                Version = Version,
                Author = Author,
                Tags = new List<string>(Tags),
                IsEnabled = IsEnabled,
                Configuration = new Dictionary<string, object>(Configuration),
                Metadata = new Dictionary<string, object>(Metadata),
                MaxExecutionTime = MaxExecutionTime,
                Priority = Priority
            };

            // Clone nodes
            foreach (var node in Nodes)
            {
                clone.AddNode(node.Clone());
            }

            // Clone connections
            foreach (var connection in Connections)
            {
                clone.AddConnection(connection.Clone());
            }

            // Clone variables
            foreach (var variable in Variables)
            {
                clone.Variables.Add(variable.Clone());
            }

            // Clone triggers
            foreach (var trigger in Triggers)
            {
                clone.Triggers.Add(trigger.Clone());
            }

            return clone;
        }
    }

    /// <summary>
    /// Represents the definition of a single node within a workflow.
    /// </summary>
    public class NodeDefinition
    {
        /// <summary>
        /// Initializes a new instance of the NodeDefinition class.
        /// </summary>
        /// <param name="id">The unique identifier for the node.</param>
        /// <param name="name">The display name of the node.</param>
        /// <param name="nodeType">The type of the node.</param>
        /// <param name="componentType">The component type for creating the automation node.</param>
        public NodeDefinition(string id, string name, NodeType nodeType, string componentType)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            NodeType = nodeType;
            ComponentType = componentType ?? throw new ArgumentNullException(nameof(componentType));
            Configuration = new Dictionary<string, object>();
            Metadata = new Dictionary<string, object>();
            InputSchema = new Dictionary<string, object>();
            OutputSchema = new Dictionary<string, object>();
            Position = new SKPoint(0, 0);
            Size = new SKSize(100, 50);
        }

        /// <summary>
        /// Gets the unique identifier for the node.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets or sets the display name of the node.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of what this node does.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets the type of this node.
        /// </summary>
        public NodeType NodeType { get; }

        /// <summary>
        /// Gets the component type used to create the automation node instance.
        /// </summary>
        public string ComponentType { get; }

        /// <summary>
        /// Gets or sets the configuration parameters for this node.
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; }

        /// <summary>
        /// Gets or sets additional metadata about this node.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }

        /// <summary>
        /// Gets or sets the schema defining the input parameters this node expects.
        /// </summary>
        public Dictionary<string, object> InputSchema { get; set; }

        /// <summary>
        /// Gets or sets the schema defining the output parameters this node produces.
        /// </summary>
        public Dictionary<string, object> OutputSchema { get; set; }

        /// <summary>
        /// Gets or sets the position of this node on the workflow canvas.
        /// </summary>
        public SKPoint Position { get; set; }

        /// <summary>
        /// Gets or sets the size of this node on the workflow canvas.
        /// </summary>
        public SKSize Size { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this node is enabled for execution.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the execution timeout for this specific node.
        /// </summary>
        public TimeSpan? ExecutionTimeout { get; set; }

        /// <summary>
        /// Gets or sets the retry policy for this node if execution fails.
        /// </summary>
        public RetryPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Validates this node definition for correctness.
        /// </summary>
        /// <returns>A validation result indicating any issues found.</returns>
        public ValidationResult Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Id))
                errors.Add("Node ID cannot be empty.");

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Node name cannot be empty.");

            if (string.IsNullOrWhiteSpace(ComponentType))
                errors.Add("Component type cannot be empty.");

            return errors.Any() ? ValidationResult.Failure(errors) : ValidationResult.Success();
        }

        /// <summary>
        /// Creates a copy of this node definition.
        /// </summary>
        /// <param name="newId">Optional new ID for the copied node.</param>
        /// <returns>A new NodeDefinition instance that is a copy of this one.</returns>
        public NodeDefinition Clone(string newId = null)
        {
            var clone = new NodeDefinition(newId ?? Id, Name, NodeType, ComponentType)
            {
                Description = Description,
                Configuration = new Dictionary<string, object>(Configuration),
                Metadata = new Dictionary<string, object>(Metadata),
                InputSchema = new Dictionary<string, object>(InputSchema),
                OutputSchema = new Dictionary<string, object>(OutputSchema),
                Position = Position,
                Size = Size,
                IsEnabled = IsEnabled,
                ExecutionTimeout = ExecutionTimeout,
                RetryPolicy = RetryPolicy?.Clone()
            };

            return clone;
        }
    }

    /// <summary>
    /// Represents a connection between two nodes in a workflow.
    /// </summary>
    public class ConnectionDefinition
    {
        /// <summary>
        /// Initializes a new instance of the ConnectionDefinition class.
        /// </summary>
        /// <param name="id">The unique identifier for the connection.</param>
        /// <param name="sourceNodeId">The ID of the source node.</param>
        /// <param name="targetNodeId">The ID of the target node.</param>
        public ConnectionDefinition(string id, string sourceNodeId, string targetNodeId)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            SourceNodeId = sourceNodeId ?? throw new ArgumentNullException(nameof(sourceNodeId));
            TargetNodeId = targetNodeId ?? throw new ArgumentNullException(nameof(targetNodeId));
            Metadata = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets the unique identifier for the connection.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the ID of the source node.
        /// </summary>
        public string SourceNodeId { get; }

        /// <summary>
        /// Gets the ID of the target node.
        /// </summary>
        public string TargetNodeId { get; }

        /// <summary>
        /// Gets or sets the name of the output from the source node.
        /// </summary>
        public string SourceOutput { get; set; }

        /// <summary>
        /// Gets or sets the name of the input to the target node.
        /// </summary>
        public string TargetInput { get; set; }

        /// <summary>
        /// Gets or sets the condition that must be met for this connection to be followed.
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// Gets or sets the label to display on this connection.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets additional metadata about this connection.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this connection is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Validates this connection definition for correctness.
        /// </summary>
        /// <returns>A validation result indicating any issues found.</returns>
        public ValidationResult Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Id))
                errors.Add("Connection ID cannot be empty.");

            if (string.IsNullOrWhiteSpace(SourceNodeId))
                errors.Add("Source node ID cannot be empty.");

            if (string.IsNullOrWhiteSpace(TargetNodeId))
                errors.Add("Target node ID cannot be empty.");

            if (SourceNodeId == TargetNodeId)
                errors.Add("Source and target nodes cannot be the same.");

            return errors.Any() ? ValidationResult.Failure(errors) : ValidationResult.Success();
        }

        /// <summary>
        /// Creates a copy of this connection definition.
        /// </summary>
        /// <param name="newId">Optional new ID for the copied connection.</param>
        /// <returns>A new ConnectionDefinition instance that is a copy of this one.</returns>
        public ConnectionDefinition Clone(string newId = null)
        {
            var clone = new ConnectionDefinition(newId ?? Id, SourceNodeId, TargetNodeId)
            {
                SourceOutput = SourceOutput,
                TargetInput = TargetInput,
                Condition = Condition,
                Label = Label,
                Metadata = new Dictionary<string, object>(Metadata),
                IsEnabled = IsEnabled
            };

            return clone;
        }
    }

    /// <summary>
    /// Represents a workflow variable that can be used across nodes.
    /// </summary>
    public class WorkflowVariable
    {
        /// <summary>
        /// Initializes a new instance of the WorkflowVariable class.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="dataType">The data type of the variable.</param>
        public WorkflowVariable(string name, DataType dataType)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            DataType = dataType;
        }

        /// <summary>
        /// Gets the name of the variable.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the data type of the variable.
        /// </summary>
        public DataType DataType { get; set; }

        /// <summary>
        /// Gets or sets the description of what this variable is used for.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the default value for this variable.
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this variable is required.
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this variable is read-only.
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// Creates a copy of this workflow variable.
        /// </summary>
        /// <returns>A new WorkflowVariable instance that is a copy of this one.</returns>
        public WorkflowVariable Clone()
        {
            return new WorkflowVariable(Name, DataType)
            {
                Description = Description,
                DefaultValue = DefaultValue,
                IsRequired = IsRequired,
                IsReadOnly = IsReadOnly
            };
        }
    }

    /// <summary>
    /// Represents a trigger definition that can start a workflow.
    /// </summary>
    public class TriggerDefinition
    {
        /// <summary>
        /// Initializes a new instance of the TriggerDefinition class.
        /// </summary>
        /// <param name="id">The unique identifier for the trigger.</param>
        /// <param name="name">The display name of the trigger.</param>
        /// <param name="triggerType">The type of the trigger.</param>
        public TriggerDefinition(string id, string name, TriggerType triggerType)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            TriggerType = triggerType;
            Configuration = new Dictionary<string, object>();
            Metadata = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets the unique identifier for the trigger.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets or sets the display name of the trigger.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of what this trigger monitors.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets the type of this trigger.
        /// </summary>
        public TriggerType TriggerType { get; }

        /// <summary>
        /// Gets or sets the configuration parameters for this trigger.
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; }

        /// <summary>
        /// Gets or sets additional metadata about this trigger.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this trigger is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Creates a copy of this trigger definition.
        /// </summary>
        /// <param name="newId">Optional new ID for the copied trigger.</param>
        /// <returns>A new TriggerDefinition instance that is a copy of this one.</returns>
        public TriggerDefinition Clone(string newId = null)
        {
            return new TriggerDefinition(newId ?? Id, Name, TriggerType)
            {
                Description = Description,
                Configuration = new Dictionary<string, object>(Configuration),
                Metadata = new Dictionary<string, object>(Metadata),
                IsEnabled = IsEnabled
            };
        }
    }

    /// <summary>
    /// Represents a retry policy for node execution failures.
    /// </summary>
    public class RetryPolicy
    {
        /// <summary>
        /// Initializes a new instance of the RetryPolicy class.
        /// </summary>
        /// <param name="maxRetries">The maximum number of retry attempts.</param>
        /// <param name="retryDelay">The delay between retry attempts.</param>
        public RetryPolicy(int maxRetries, TimeSpan retryDelay)
        {
            MaxRetries = maxRetries;
            RetryDelay = retryDelay;
        }

        /// <summary>
        /// Gets or sets the maximum number of retry attempts.
        /// </summary>
        public int MaxRetries { get; set; }

        /// <summary>
        /// Gets or sets the delay between retry attempts.
        /// </summary>
        public TimeSpan RetryDelay { get; set; }

        /// <summary>
        /// Gets or sets the backoff strategy for increasing delay between retries.
        /// </summary>
        public RetryBackoffStrategy BackoffStrategy { get; set; } = RetryBackoffStrategy.Linear;

        /// <summary>
        /// Gets or sets the maximum delay between retries when using backoff.
        /// </summary>
        public TimeSpan? MaxRetryDelay { get; set; }

        /// <summary>
        /// Creates a copy of this retry policy.
        /// </summary>
        /// <returns>A new RetryPolicy instance that is a copy of this one.</returns>
        public RetryPolicy Clone()
        {
            return new RetryPolicy(MaxRetries, RetryDelay)
            {
                BackoffStrategy = BackoffStrategy,
                MaxRetryDelay = MaxRetryDelay
            };
        }

        /// <summary>
        /// Creates a no-retry policy.
        /// </summary>
        /// <returns>A RetryPolicy that does not retry on failure.</returns>
        public static RetryPolicy None()
        {
            return new RetryPolicy(0, TimeSpan.Zero);
        }

        /// <summary>
        /// Creates a simple retry policy with fixed delay.
        /// </summary>
        /// <param name="maxRetries">The maximum number of retries.</param>
        /// <param name="delaySeconds">The delay in seconds between retries.</param>
        /// <returns>A RetryPolicy with the specified parameters.</returns>
        public static RetryPolicy Simple(int maxRetries, int delaySeconds)
        {
            return new RetryPolicy(maxRetries, TimeSpan.FromSeconds(delaySeconds));
        }

        /// <summary>
        /// Creates an exponential backoff retry policy.
        /// </summary>
        /// <param name="maxRetries">The maximum number of retries.</param>
        /// <param name="initialDelaySeconds">The initial delay in seconds.</param>
        /// <param name="maxDelaySeconds">The maximum delay in seconds.</param>
        /// <returns>A RetryPolicy with exponential backoff.</returns>
        public static RetryPolicy ExponentialBackoff(int maxRetries, int initialDelaySeconds, int maxDelaySeconds)
        {
            return new RetryPolicy(maxRetries, TimeSpan.FromSeconds(initialDelaySeconds))
            {
                BackoffStrategy = RetryBackoffStrategy.Exponential,
                MaxRetryDelay = TimeSpan.FromSeconds(maxDelaySeconds)
            };
        }
    }

    /// <summary>
    /// Specifies the backoff strategy for retry delays.
    /// </summary>
    public enum RetryBackoffStrategy
    {
        /// <summary>
        /// Fixed delay between retries.
        /// </summary>
        Fixed,

        /// <summary>
        /// Linear increase in delay between retries.
        /// </summary>
        Linear,

        /// <summary>
        /// Exponential increase in delay between retries.
        /// </summary>
        Exponential
    }
}