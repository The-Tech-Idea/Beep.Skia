using SkiaSharp;
using System.Linq;
using Beep.Skia.Model;
using Beep.Skia.Components;
using System;
using System.Collections.Generic;
using System.Text.Json;
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
                var line = new ConnectionLine(connectionPoint1, connectionPoint2, () => RequestRedraw());
                // Row-level mapping if ports expose RowId
                try { if (connectionPoint1 is ConnectionPoint ocp) line.SourceRowId = ocp.RowId; } catch { }
                try { if (connectionPoint2 is ConnectionPoint icp) line.TargetRowId = icp.RowId; } catch { }
                ApplyPendingLinePreset(line);
                // Simple PK/FK validation for ERD-style connections
                TryValidatePkFk(line);
                // ETL: propagate OutputSchema from ETLSource/Transform if present
                try
                {
                    string schema = TryGetOutputSchema(component1);
                    if (string.IsNullOrWhiteSpace(schema)) schema = TryGetOutputSchema(component2);
                    if (!string.IsNullOrWhiteSpace(schema)) line.SchemaJson = schema;

                    // If either endpoint exposes ExpectedSchema (ETLTarget), validate against the actual schema on the line
                    // Prefer validating component2 as the typical sink; fallback to component1 if component2 has none
                    string expected = null;
                    try { if (component2?.NodeProperties != null && component2.NodeProperties.TryGetValue("ExpectedSchema", out var ep2) && ep2?.ParameterCurrentValue is string e2 && !string.IsNullOrWhiteSpace(e2)) expected = e2; } catch { }
                    if (string.IsNullOrWhiteSpace(expected))
                    {
                        try { if (component1?.NodeProperties != null && component1.NodeProperties.TryGetValue("ExpectedSchema", out var ep1) && ep1?.ParameterCurrentValue is string e1 && !string.IsNullOrWhiteSpace(e1)) expected = e1; } catch { }
                    }
                    if (!string.IsNullOrWhiteSpace(expected) && !string.IsNullOrWhiteSpace(line.SchemaJson))
                    {
                        line.ExpectedSchemaJson = expected;
                        if (!SchemasCompatible(expected, line.SchemaJson))
                        {
                            line.Status = LineStatus.Warning;
                            line.StatusColor = new SKColor(255, 152, 0); // Amber
                            line.ShowStatusIndicator = true;
                        }
                    }
                }
                catch { }
                // Auto-infer transform output when applicable
                try { TryInferTransformSchema(component1); TryInferTransformSchema(component2); } catch { }
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
            var line = new ConnectionLine(outputPoint, inputPoint, () => RequestRedraw());
            // Row-level mapping if ports expose RowId
            try { if (outputPoint is ConnectionPoint ocp) line.SourceRowId = ocp.RowId; } catch { }
            try { if (inputPoint is ConnectionPoint icp) line.TargetRowId = icp.RowId; } catch { }
            ApplyPendingLinePreset(line);
            line.IsDataFlowAnimated = true; // Enable data flow animation by default
            line.FlowDirection = DataFlowDirection.Forward; // Default flow direction
            line.DataFlowColor = GetDataFlowColor(outputPoint.DataType); // Set color based on data type
            // Propagate ETL schema if the source node exposes OutputSchema NodeProperty
            try
            {
                if (node1?.NodeProperties != null && node1.NodeProperties.TryGetValue("OutputSchema", out var p) && p?.ParameterCurrentValue is string js && !string.IsNullOrWhiteSpace(js))
                {
                    line.SchemaJson = js;
                }
            }
            catch { }
            // If node2 is an ETLTarget with ExpectedSchema, validate
            try
            {
                if (node2?.NodeProperties != null && node2.NodeProperties.TryGetValue("ExpectedSchema", out var ep) && ep?.ParameterCurrentValue is string exp && !string.IsNullOrWhiteSpace(exp) && !string.IsNullOrWhiteSpace(line.SchemaJson))
                {
                    line.ExpectedSchemaJson = exp;
                    if (!SchemasCompatible(exp, line.SchemaJson))
                    {
                        line.Status = LineStatus.Warning;
                        line.StatusColor = new SKColor(255, 152, 0); // Amber
                        line.ShowStatusIndicator = true;
                    }
                }
            }
            catch { }
            // Auto-infer transform output when applicable
            try { TryInferTransformSchema(node1); TryInferTransformSchema(node2); } catch { }
            _lines.Add(line);

            // Mark points as connected
            outputPoint.IsAvailable = false;
            inputPoint.IsAvailable = false;
            outputPoint.Connection = inputPoint;
            inputPoint.Connection = outputPoint;

            _historyManager.ExecuteAction(new ConnectAutomationNodesAction(this, node1, node2, line, outputPoint, inputPoint));
            DrawSurface?.Invoke(this, null);
        }

        private string TryGetOutputSchema(SkiaComponent c)
        {
            try
            {
                if (c?.NodeProperties != null && c.NodeProperties.TryGetValue("OutputSchema", out var p) && p?.ParameterCurrentValue is string js)
                    return js;
            }
            catch { }
            return null;
        }

        private bool SchemasCompatible(string expectedJson, string actualJson)
        {
            try
            {
                var exp = System.Text.Json.JsonSerializer.Deserialize<List<Beep.Skia.Model.ColumnDefinition>>(expectedJson) ?? new();
                var act = System.Text.Json.JsonSerializer.Deserialize<List<Beep.Skia.Model.ColumnDefinition>>(actualJson) ?? new();
                // simple compatibility: expected columns must exist in actual (by name) with same DataType when both provided
                foreach (var e in exp)
                {
                    var a = act.Find(x => string.Equals(x.Name, e.Name, StringComparison.OrdinalIgnoreCase));
                    if (a == null) return false;
                    if (!string.IsNullOrEmpty(e.DataType) && !string.IsNullOrEmpty(a.DataType) && !string.Equals(e.DataType, a.DataType, StringComparison.OrdinalIgnoreCase))
                        return false;
                }
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Public entry to attempt schema inference for a component, if supported.
        /// </summary>
        public void InferOutputSchemaForComponent(SkiaComponent component) => TryInferTransformSchema(component);

        /// <summary>
        /// Reflection-based inference: call component.InferOutputSchemaFromUpstreams(Func<int,string>) if present,
        /// and if NodeProperties indicate a Join, validate join keys exist and are type-compatible.
        /// </summary>
        private void TryInferTransformSchema(SkiaComponent component)
        {
            if (component == null) return;
            try
            {
                string InputSchema(int index)
                {
                    var inputs = _lines.Where(l => l.End?.Component == component).ToList();
                    if (index < 0 || index >= inputs.Count) return null;
                    return (inputs[index] as ConnectionLine)?.SchemaJson;
                }

                var mi = component.GetType().GetMethod("InferOutputSchemaFromUpstreams", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, binder: null, types: new[] { typeof(Func<int, string>) }, modifiers: null);
                if (mi != null)
                {
                    mi.Invoke(component, new object[] { new Func<int, string>(InputSchema) });
                }

                if (component.NodeProperties != null && component.NodeProperties.TryGetValue("Kind", out var kindPi))
                {
                    var kindVal = Convert.ToString(kindPi?.ParameterCurrentValue ?? kindPi?.DefaultParameterValue) ?? string.Empty;
                    if (string.Equals(kindVal, "Join", StringComparison.OrdinalIgnoreCase))
                    {
                        var leftJson = InputSchema(0);
                        var rightJson = InputSchema(1);
                        if (!string.IsNullOrWhiteSpace(leftJson) && !string.IsNullOrWhiteSpace(rightJson))
                        {
                            var left = System.Text.Json.JsonSerializer.Deserialize<List<Beep.Skia.Model.ColumnDefinition>>(leftJson) ?? new();
                            var right = System.Text.Json.JsonSerializer.Deserialize<List<Beep.Skia.Model.ColumnDefinition>>(rightJson) ?? new();
                            string lk = null, rk = null;
                            try { if (component.NodeProperties.TryGetValue("JoinKeyLeft", out var pl)) lk = Convert.ToString(pl?.ParameterCurrentValue ?? pl?.DefaultParameterValue); } catch { }
                            try { if (component.NodeProperties.TryGetValue("JoinKeyRight", out var pr)) rk = Convert.ToString(pr?.ParameterCurrentValue ?? pr?.DefaultParameterValue); } catch { }
                            lk = (lk ?? string.Empty).Trim();
                            rk = (rk ?? string.Empty).Trim();
                            var lc = string.IsNullOrWhiteSpace(lk) ? null : left.FirstOrDefault(c => string.Equals(c.Name, lk, StringComparison.OrdinalIgnoreCase));
                            var rc = string.IsNullOrWhiteSpace(rk) ? null : right.FirstOrDefault(c => string.Equals(c.Name, rk, StringComparison.OrdinalIgnoreCase));
                            bool hasBoth = lc != null && rc != null;
                            bool typeOk = hasBoth && (string.IsNullOrWhiteSpace(lc.DataType) || string.IsNullOrWhiteSpace(rc.DataType) || string.Equals(lc.DataType, rc.DataType, StringComparison.OrdinalIgnoreCase));

                            foreach (var l in _lines.Where(l => l.End?.Component == component))
                            {
                                if (!hasBoth || !typeOk)
                                {
                                    l.Status = LineStatus.Warning;
                                    l.StatusColor = new SKColor(255, 152, 0);
                                    l.ShowStatusIndicator = true;
                                }
                            }
                        }
                    }
                }

                RequestRedraw();
            }
            catch (Exception ex)
            {
                try
                {
                    // Light telemetry: write to temp log and console without crashing the UI
                    var msg = $"[InferenceError] Component={component?.GetType()?.FullName} Name={component?.Name} Error={ex.Message}";
                    Console.WriteLine(msg);
                    try { var lp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beepskia_render.log"); System.IO.File.AppendAllText(lp, msg + Environment.NewLine); } catch { }
                }
                catch { }
            }
        }

        /// <summary>
        /// Sets a warning status on the line if the endpoints' selected rows don't form a valid PK/FK pair.
        /// Now checks declared ForeignKeys definitions from ERDEntity for enterprise-grade validation.
        /// Rule: Valid when one side is FK and the other is PK, OR connection matches a declared FK.
        /// Applies mainly to ERD entities exposing Columns and ForeignKeys JSON NodeProperties.
        /// </summary>
        private void TryValidatePkFk(ConnectionLine line)
        {
            try
            {
                if (line == null) return;
                if (line.SourceRowId == null || line.TargetRowId == null) return;
                var srcComp = line.Start?.Component as SkiaComponent;
                var tgtComp = line.End?.Component as SkiaComponent;
                if (srcComp == null || tgtComp == null) return;

                var srcCol = FindColumnByRowId(srcComp, line.SourceRowId.Value);
                var tgtCol = FindColumnByRowId(tgtComp, line.TargetRowId.Value);
                if (srcCol == null || tgtCol == null) return;

                // Simple flag-based check (legacy)
                bool flagOk = (srcCol.IsForeignKey && tgtCol.IsPrimaryKey) || (srcCol.IsPrimaryKey && tgtCol.IsForeignKey);
                
                // Enhanced: check if connection matches a declared FK from source or target
                bool fkDeclared = false;
                try
                {
                    // Check if srcComp declares an FK pointing to tgtComp entity
                    var srcFks = ParseForeignKeys(srcComp);
                    var tgtEntityName = GetEntityName(tgtComp);
                    foreach (var fk in srcFks)
                    {
                        if (string.Equals(fk.ReferencedEntity, tgtEntityName, StringComparison.OrdinalIgnoreCase))
                        {
                            // Check if srcCol is in FK columns and tgtCol is in referenced columns at same index
                            var fkCols = fk.Columns ?? new System.Collections.Generic.List<string>();
                            var refCols = fk.ReferencedColumns ?? new System.Collections.Generic.List<string>();
                            for (int i = 0; i < fkCols.Count && i < refCols.Count; i++)
                            {
                                if (string.Equals(fkCols[i], srcCol.Name, StringComparison.OrdinalIgnoreCase) &&
                                    string.Equals(refCols[i], tgtCol.Name, StringComparison.OrdinalIgnoreCase))
                                {
                                    fkDeclared = true;
                                    break;
                                }
                            }
                            if (fkDeclared) break;
                        }
                    }
                    
                    // Check reverse: tgtComp declares FK pointing to srcComp
                    if (!fkDeclared)
                    {
                        var tgtFks = ParseForeignKeys(tgtComp);
                        var srcEntityName = GetEntityName(srcComp);
                        foreach (var fk in tgtFks)
                        {
                            if (string.Equals(fk.ReferencedEntity, srcEntityName, StringComparison.OrdinalIgnoreCase))
                            {
                                var fkCols = fk.Columns ?? new System.Collections.Generic.List<string>();
                                var refCols = fk.ReferencedColumns ?? new System.Collections.Generic.List<string>();
                                for (int i = 0; i < fkCols.Count && i < refCols.Count; i++)
                                {
                                    if (string.Equals(fkCols[i], tgtCol.Name, StringComparison.OrdinalIgnoreCase) &&
                                        string.Equals(refCols[i], srcCol.Name, StringComparison.OrdinalIgnoreCase))
                                    {
                                        fkDeclared = true;
                                        break;
                                    }
                                }
                                if (fkDeclared) break;
                            }
                        }
                    }
                }
                catch { }

                // Accept if either flag-based or FK declaration matches
                bool ok = flagOk || fkDeclared;
                if (!ok)
                {
                    line.ShowStatusIndicator = true;
                    line.Status = LineStatus.Warning;
                    line.StatusColor = SKColors.Orange;
                }
            }
            catch { }
        }

        private System.Collections.Generic.List<ForeignKeyDefinition> ParseForeignKeys(SkiaComponent comp)
        {
            try
            {
                if (comp?.NodeProperties != null && comp.NodeProperties.TryGetValue("ForeignKeys", out var p) && p != null)
                {
                    var json = p.ParameterCurrentValue as string ?? p.DefaultParameterValue as string;
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        return JsonSerializer.Deserialize<System.Collections.Generic.List<ForeignKeyDefinition>>(json) ?? new System.Collections.Generic.List<ForeignKeyDefinition>();
                    }
                }
            }
            catch { }
            return new System.Collections.Generic.List<ForeignKeyDefinition>();
        }

        private string GetEntityName(SkiaComponent comp)
        {
            try
            {
                if (comp?.NodeProperties != null && comp.NodeProperties.TryGetValue("EntityName", out var p) && p != null)
                {
                    return p.ParameterCurrentValue as string ?? p.DefaultParameterValue as string ?? comp.Name ?? string.Empty;
                }
            }
            catch { }
            return comp?.Name ?? string.Empty;
        }

        private ColumnDefinition FindColumnByRowId(SkiaComponent comp, Guid rowId)
        {
            try
            {
                if (comp?.NodeProperties == null) return null;
                if (!comp.NodeProperties.TryGetValue("Columns", out var p) || p == null) return null;
                var raw = p.ParameterCurrentValue ?? p.DefaultParameterValue;
                var js = raw as string ?? raw?.ToString();
                if (string.IsNullOrWhiteSpace(js)) return null;
                var list = JsonSerializer.Deserialize<List<ColumnDefinition>>(js);
                return list?.FirstOrDefault(c => c.Id == rowId);
            }
            catch { return null; }
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
            // Re-infer on both endpoints if they support inference
            try { TryInferTransformSchema(component1); TryInferTransformSchema(component2); } catch { }
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
            // Re-infer on components connected to this line
            try { TryInferTransformSchema(line.Start?.Component as SkiaComponent); TryInferTransformSchema(line.End?.Component as SkiaComponent); } catch { }
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
                // Ensure the surface redraws so the newly added line appears immediately
                DrawSurface?.Invoke(this, null);
            }
        }

        /// <summary>
        /// Removes a connection line from the drawing manager.
        /// </summary>
        /// <param name="line">The line to remove.</param>
        internal void RemoveLine(IConnectionLine line)
        {
            if (line == null) return;
            if (_lines.Remove(line))
            {
                // Redraw to reflect the removal right away
                DrawSurface?.Invoke(this, null);
            }
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
