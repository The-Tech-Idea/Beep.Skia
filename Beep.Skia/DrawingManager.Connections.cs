using SkiaSharp;
using System.Linq;
using Beep.Skia.Model;
using Beep.Skia.Components;
namespace Beep.Skia
{
    public partial class DrawingManager
    {
        // If set, the next created ConnectionLine will have these ERD multiplicities applied,
        // then the preset is cleared. Used by palette presets for ERD quick connect.
        internal (ERDMultiplicity? Start, ERDMultiplicity? End)? PendingLineMultiplicityPreset { get; set; }

        /// <summary>
        /// Sets a one-shot ERD multiplicity preset that will be applied to the next created connection line.
        /// Pass nulls or Unspecified to clear.
        /// </summary>
        public void SetNextLineMultiplicityPreset(ERDMultiplicity? start, ERDMultiplicity? end)
        {
            PendingLineMultiplicityPreset = (start, end);
        }
        /// <summary>
        /// Connects two workflow components and creates a visual connection line between them.
        /// </summary>
        /// <param name="component1">The first component to connect.</param>
        /// <param name="component2">The second component to connect.</param>
        /// <exception cref="ArgumentNullException">Thrown when either component is null.</exception>
        /// <exception cref="ArgumentException">Thrown when trying to connect a component to itself.</exception>
        public void ConnectComponents(SkiaComponent component1, SkiaComponent component2)
        {
            if (component1 == null)
                throw new ArgumentNullException(nameof(component1), "First component cannot be null.");
            if (component2 == null)
                throw new ArgumentNullException(nameof(component2), "Second component cannot be null.");
            if (component1 == component2)
                throw new ArgumentException("Cannot connect component to itself.");

            // For automation nodes, use connection points
            if (component1 is AutomationNode node1 && component2 is AutomationNode node2)
            {
                ConnectAutomationNodes(node1, node2);
                return;
            }

            component1.ConnectTo(component2);
            component2.ConnectTo(component1);

            IConnectionPoint connectionPoint1 = component1.OutConnectionPoints.FirstOrDefault();
            IConnectionPoint connectionPoint2 = component2.InConnectionPoints.FirstOrDefault();
            if (connectionPoint1 != null && connectionPoint2 != null)
            {
                var line = new ConnectionLine(connectionPoint1, connectionPoint2, () => { /* InvalidateSurface callback */ });
                ApplyPendingLinePreset(line);
                // Default line behavior
                line.FlowDirection = DataFlowDirection.Forward;
                _lines.Add(line);
                _historyManager.ExecuteAction(new ConnectComponentsAction(this, component1, component2, line));
            }
            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Connects two automation nodes with proper validation and connection point management.
        /// </summary>
        /// <param name="node1">The first automation node.</param>
        /// <param name="node2">The second automation node.</param>
        private void ConnectAutomationNodes(AutomationNode node1, AutomationNode node2)
        {
            // Find available connection points
            var outputPoint = node1.OutputConnections.FirstOrDefault(cp => cp.IsAvailable);
            var inputPoint = node2.InputConnections.FirstOrDefault(cp => cp.IsAvailable);

            if (outputPoint == null || inputPoint == null)
            {
                // No available connection points
                return;
            }

            // Validate connection (additional business logic can be added here)
            if (!CanConnectNodes(node1, node2, outputPoint, inputPoint))
            {
                return;
            }

            // Create the connection
            var line = new ConnectionLine(outputPoint, inputPoint, () => DrawSurface?.Invoke(this, null));
            ApplyPendingLinePreset(line);
            line.IsDataFlowAnimated = true; // Enable data flow animation by default
            line.FlowDirection = DataFlowDirection.Forward; // Default flow direction
            line.DataFlowColor = GetDataFlowColor(outputPoint.DataType); // Set color based on data type
            _lines.Add(line);

            // Mark points as connected
            outputPoint.IsAvailable = false;
            inputPoint.IsAvailable = false;
            outputPoint.Connection = inputPoint;
            inputPoint.Connection = outputPoint;

            _historyManager.ExecuteAction(new ConnectAutomationNodesAction(this, node1, node2, line, outputPoint, inputPoint));
            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Validates whether two automation nodes can be connected.
        /// </summary>
        /// <param name="sourceNode">The source node.</param>
        /// <param name="targetNode">The target node.</param>
        /// <param name="outputPoint">The output connection point.</param>
        /// <param name="inputPoint">The input connection point.</param>
        /// <returns>True if the connection is valid, false otherwise.</returns>
        private bool CanConnectNodes(AutomationNode sourceNode, AutomationNode targetNode, IConnectionPoint outputPoint, IConnectionPoint inputPoint)
        {
            // Prevent self-connections
            if (sourceNode == targetNode)
                return false;

            // Prevent connecting to the same node multiple times (for now)
            // This can be enhanced to allow multiple connections with different logic
            var existingConnection = _lines.FirstOrDefault(line =>
                (line.Start?.Component == sourceNode && line.End?.Component == targetNode) ||
                (line.Start?.Component == targetNode && line.End?.Component == sourceNode));

            if (existingConnection != null)
                return false;

            // Validate data flow (output to input only)
            if (outputPoint.Type != ConnectionPointType.Out || inputPoint.Type != ConnectionPointType.In)
                return false;

            // Advanced validation: Data type compatibility
            if (!AreDataTypesCompatible(outputPoint.DataType, inputPoint.DataType))
                return false;

            // Advanced validation: Node type compatibility
            if (!AreNodeTypesCompatible(sourceNode, targetNode))
                return false;

            // Advanced validation: Prevent circular dependencies
            if (WouldCreateCircularDependency(sourceNode, targetNode))
                return false;

            // Additional validation can be added here:
            // - Business rules specific to node types
            // - Resource constraints
            // - Execution order requirements

            return true;
        }

        /// <summary>
        /// Checks if two data types are compatible for connection.
        /// </summary>
        /// <param name="outputDataType">The output data type.</param>
        /// <param name="inputDataType">The input data type.</param>
        /// <returns>True if the data types are compatible, false otherwise.</returns>
        private bool AreDataTypesCompatible(string outputDataType, string inputDataType)
        {
            // Allow any type to connect to "any" type
            if (inputDataType == "any" || outputDataType == "any")
                return true;

            // Exact type match
            if (string.Equals(outputDataType, inputDataType, StringComparison.OrdinalIgnoreCase))
                return true;

            // Type compatibility rules
            var compatibilityRules = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                // Numbers can connect to strings (string conversion)
                ["number"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "string" },

                // Strings can connect to objects (JSON parsing)
                ["string"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "object" },

                // Arrays can connect to objects
                ["array"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "object" },

                // Objects can connect to arrays (single item arrays)
                ["object"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "array" },

                // Booleans can connect to numbers (0/1) and strings
                ["boolean"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "number", "string" }
            };

            return compatibilityRules.TryGetValue(outputDataType ?? "", out var compatibleTypes) &&
                   compatibleTypes.Contains(inputDataType ?? "");
        }

        /// <summary>
        /// Checks if two node types are compatible for connection.
        /// </summary>
        /// <param name="sourceNode">The source node.</param>
        /// <param name="targetNode">The target node.</param>
        /// <returns>True if the node types are compatible, false otherwise.</returns>
        private bool AreNodeTypesCompatible(AutomationNode sourceNode, AutomationNode targetNode)
        {
            // Basic node type compatibility rules
            var incompatiblePairs = new HashSet<(NodeType, NodeType)>
            {
                // Trigger nodes should only connect to processing nodes, not other triggers
                (NodeType.Trigger, NodeType.Trigger),

                // Data sources shouldn't connect to other data sources
                (NodeType.DataSource, NodeType.DataSource)
            };

            var pair = (sourceNode.NodeType, targetNode.NodeType);
            return !incompatiblePairs.Contains(pair);
        }

        /// <summary>
        /// Checks if connecting two nodes would create a circular dependency.
        /// </summary>
        /// <param name="sourceNode">The source node.</param>
        /// <param name="targetNode">The target node.</param>
        /// <returns>True if a circular dependency would be created, false otherwise.</returns>
        private bool WouldCreateCircularDependency(AutomationNode sourceNode, AutomationNode targetNode)
        {
            // Build a dependency graph and check for cycles
            var visited = new HashSet<AutomationNode>();
            var recursionStack = new HashSet<AutomationNode>();

            return HasCircularDependency(targetNode, sourceNode, visited, recursionStack);
        }

        /// <summary>
        /// Recursive helper method to detect circular dependencies using DFS.
        /// </summary>
        /// <param name="current">The current node being visited.</param>
        /// <param name="target">The target node we're trying to reach.</param>
        /// <param name="visited">Set of visited nodes.</param>
        /// <param name="recursionStack">Current recursion stack.</param>
        /// <returns>True if a circular dependency is found, false otherwise.</returns>
        private bool HasCircularDependency(AutomationNode current, AutomationNode target, HashSet<AutomationNode> visited, HashSet<AutomationNode> recursionStack)
        {
            // If we reach the target node, there's a cycle
            if (current == target)
                return true;

            visited.Add(current);
            recursionStack.Add(current);

            // Check all nodes that this node connects to
            var connectedNodes = _lines
                .Where(line => line.Start?.Component == current)
                .Select(line => line.End?.Component as AutomationNode)
                .Where(node => node != null);

            foreach (var connectedNode in connectedNodes)
            {
                if (!visited.Contains(connectedNode) && HasCircularDependency(connectedNode, target, visited, recursionStack))
                    return true;
                else if (recursionStack.Contains(connectedNode))
                    return true;
            }

            recursionStack.Remove(current);
            return false;
        }

        /// <summary>
        /// Gets the appropriate data flow color based on the data type.
        /// </summary>
        /// <param name="dataType">The data type.</param>
        /// <returns>The color for the data flow animation.</returns>
        private SKColor GetDataFlowColor(string dataType)
        {
            return (dataType?.ToLowerInvariant()) switch
            {
                "string" => SKColors.Green,
                "number" => SKColors.Blue,
                "boolean" => SKColors.Orange,
                "object" => SKColors.Purple,
                "array" => SKColors.Red,
                "file" => SKColors.Brown,
                "image" => SKColors.Pink,
                "binary" => SKColors.Gray,
                _ => SKColors.Cyan // Default color for unknown or "any" types
            };
        }

        /// <summary>
        /// Disconnects two workflow components and removes their visual connection line.
        /// </summary>
        /// <param name="component1">The first component to disconnect.</param>
        /// <param name="component2">The second component to disconnect.</param>
        public void DisconnectComponents(SkiaComponent component1, SkiaComponent component2)
        {
            component1.DisconnectFrom(component2);
            component2.DisconnectFrom(component1);

            var lineToRemove = _lines.FirstOrDefault(line => (line.Start.Component == component1 && line.End.Component == component2) || (line.Start.Component == component2 && line.End.Component == component1));
            if (lineToRemove != null)
            {
                _lines.Remove(lineToRemove);
                _historyManager.ExecuteAction(new DisconnectComponentsAction(this, component1, component2, lineToRemove));
            }
            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Moves a connection line from one connection point to another.
        /// </summary>
        /// <param name="line">The line to move.</param>
        /// <param name="newStartPoint">The new start connection point (optional).</param>
        /// <param name="newEndPoint">The new end connection point (optional).</param>
        public void MoveConnectionLine(IConnectionLine line, IConnectionPoint newStartPoint = null, IConnectionPoint newEndPoint = null)
        {
            if (line == null) return;

            var oldStartPoint = line.Start;
            var oldEndPoint = line.End;

            if (newStartPoint != null)
            {
                // Disconnect from old start point
                if (oldStartPoint != null)
                {
                    oldStartPoint.Connection = null;
                    oldStartPoint.IsAvailable = true;
                }

                // Connect to new start point
                line.Start = newStartPoint;
                newStartPoint.Connection = line.End;
                newStartPoint.IsAvailable = false;
            }

            if (newEndPoint != null)
            {
                // Disconnect from old end point
                if (oldEndPoint != null)
                {
                    oldEndPoint.Connection = null;
                    oldEndPoint.IsAvailable = true;
                }

                // Connect to new end point
                line.End = newEndPoint;
                newEndPoint.Connection = line.Start;
                newEndPoint.IsAvailable = false;
            }

            _historyManager.ExecuteAction(new MoveLineAction(this, line, oldStartPoint, oldEndPoint, newStartPoint, newEndPoint));
            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Adds a connection line to the drawing manager.
        /// </summary>
        /// <param name="line">The line to add.</param>
        internal void AddLine(IConnectionLine line)
        {
            if (line != null && !_lines.Contains(line))
            {
                _lines.Add(line);
            }
        }

        /// <summary>
        /// Removes a connection line from the drawing manager.
        /// </summary>
        /// <param name="line">The line to remove.</param>
        internal void RemoveLine(IConnectionLine line)
        {
            _lines.Remove(line);
        }

        /// <summary>
        /// Gets the connection point at the specified point.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns>The connection point at the specified point, or null if no connection point is found.</returns>
        internal IConnectionPoint GetConnectionPointAt(SKPoint point)
        {
            foreach (var component in _components)
            {
                var connectionPoint = component.InConnectionPoints.Concat(component.OutConnectionPoints).FirstOrDefault(cp => cp.Bounds.Contains(point));
                if (connectionPoint != null)
                {
                    return connectionPoint;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the connection line at the specified point.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns>The connection line at the specified point, or null if no line is found.</returns>
        internal IConnectionLine GetLineAt(SKPoint point)
        {
            // Search from top-most to bottom-most (last drawn is on top)
            for (int i = _lines.Count - 1; i >= 0; i--)
            {
                var line = _lines[i];
                if (line == null) continue;
                try
                {
                    if (_renderingHelper.LineContainsPoint(line, point))
                        return line;
                }
                catch { }
            }
            return null;
        }

        /// <summary>
        /// Gets the arrow type ("start" or "end") at the specified point on a line.
        /// </summary>
        /// <param name="line">The connection line.</param>
        /// <param name="point">The point to test.</param>
        /// <returns>"start", "end", or null if no arrow is at the point.</returns>
        internal string GetArrowAt(IConnectionLine line, SKPoint point)
        {
            return _renderingHelper.GetArrowAt(line, point);
        }

        /// <summary>
        /// Gets the resize handle at the specified point on a component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="point">The point to test.</param>
        /// <returns>The handle identifier, or null if no handle is at the point.</returns>
        internal string GetResizeHandleAt(SkiaComponent component, SKPoint point)
        {
            return _renderingHelper.GetResizeHandleAt(component, point);
        }

        /// <summary>
        /// Gets the owning component given a connection point identifier.
        /// </summary>
        internal SkiaComponent GetOwnerForConnectionPoint(Guid connectionPointId)
        {
            var cp = GetConnectionPoint(connectionPointId);
            return GetOwnerForConnectionPoint(cp);
        }

        /// <summary>
        /// Resolves a connection point by position using the registry first.
        /// </summary>
        internal IConnectionPoint GetConnectionPointAtWithRegistry(SKPoint point)
        {
            // For now, delegate to geometry-based hit-test.
            // Registry lookups can be added here if we index spatially in the future.
            return GetConnectionPointAt(point);
        }

        /// <summary>
        /// Starts drawing a connection line from the specified source point.
        /// </summary>
        /// <param name="sourcePoint">The source connection point.</param>
        /// <param name="point">The current mouse position.</param>
        internal void StartDrawingLine(IConnectionPoint sourcePoint, SKPoint point)
        {
            var line = new ConnectionLine(sourcePoint, point, () => { /* InvalidateSurface callback */ });
            ApplyPendingLinePreset(line);
            CurrentLine = line;
        }

        /// <summary>
        /// Gets or sets the current line being drawn (for internal use by helpers).
        /// </summary>
        internal IConnectionLine CurrentLine { get; set; }

        /// <summary>
        /// Gets a value indicating whether a line is currently being drawn.
        /// </summary>
        internal bool IsDrawingLine => CurrentLine != null;

        private void ApplyPendingLinePreset(ConnectionLine line)
        {
            if (line == null) return;
            if (PendingLineMultiplicityPreset.HasValue)
            {
                var p = PendingLineMultiplicityPreset.Value;
                if (p.Start.HasValue) line.StartMultiplicity = p.Start.Value;
                if (p.End.HasValue) line.EndMultiplicity = p.End.Value;
                // clear one-shot preset
                PendingLineMultiplicityPreset = null;
            }
        }
    }
}
