using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using Beep.Skia.Model;
namespace Beep.Skia
{
    public partial class DrawingManager
    {
        /// <summary>
        /// Brings the specified component to the front of the draw order without removing it or its lines.
        /// </summary>
        /// <param name="component">The component to bring to front.</param>
        public void BringToFront(SkiaComponent component)
        {
            if (component == null) return;
            int idx = _components.IndexOf(component);
            if (idx < 0) return;
            if (idx == _components.Count - 1) return; // already on top
            _components.RemoveAt(idx);
            _components.Add(component);
            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Adds a workflow component to the drawing manager.
        /// </summary>
        /// <param name="component">The component to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when component is null.</exception>
        public void AddComponent(SkiaComponent component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component), "Component cannot be null.");

            _components.Add(component);
            try { component.BoundsChanged += OnComponentBoundsChanged; } catch { }
            RegisterConnectionPoints(component);
            _historyManager.ExecuteAction(new AddComponentAction(this, component));
            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Removes a workflow component from the drawing manager.
        /// </summary>
        /// <param name="component">The component to remove.</param>
        /// <exception cref="ArgumentNullException">Thrown when component is null.</exception>
        public void RemoveComponent(SkiaComponent component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component), "Component cannot be null.");

            // Remove all connections to/from this component
            var linesToRemove = _lines.Where(line =>
                line.Start?.Component == component || line.End?.Component == component).ToList();

            foreach (var line in linesToRemove)
            {
                _lines.Remove(line);
            }

            _components.Remove(component);
            // Unsubscribe events
            try { component.BoundsChanged -= OnComponentBoundsChanged; } catch { }
            // Unregister its connection points
            UnregisterConnectionPoints(component);
            _selectionManager.RemoveFromSelection(component);
            _historyManager.ExecuteAction(new RemoveComponentAction(this, component, linesToRemove));
            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Deletes all selected components.
        /// </summary>
        public void DeleteSelectedComponents()
        {
            var componentsToDelete = _selectionManager.SelectedComponents.ToList();
            var linesToDelete = new List<IConnectionLine>();

            foreach (var component in componentsToDelete)
            {
                var componentLines = _lines.Where(line =>
                    line.Start?.Component == component || line.End?.Component == component).ToList();
                linesToDelete.AddRange(componentLines);
            }

            _historyManager.ExecuteAction(new DeleteComponentsAction(this, componentsToDelete, linesToDelete));

            foreach (var line in linesToDelete)
            {
                _lines.Remove(line);
            }

            foreach (var component in componentsToDelete)
            {
                _components.Remove(component);
                try { component.BoundsChanged -= OnComponentBoundsChanged; } catch { }
                UnregisterConnectionPoints(component);
                _selectionManager.RemoveFromSelection(component);
            }

            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Selects all components in the diagram.
        /// </summary>
        public void SelectAllComponents()
        {
            _selectionManager.SelectAll();
            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Selects the next (or previous) component in reading order (keyboard accessibility).
        /// </summary>
        /// <param name="forward">True for next (Tab), false for previous (Shift+Tab).</param>
        /// <returns>The newly selected component, or null when the diagram is empty.</returns>
        public SkiaComponent SelectNextComponent(bool forward = true)
        {
            var selected = _selectionManager.SelectNext(forward);
            DrawSurface?.Invoke(this, null);
            return selected;
        }

        /// <summary>
        /// Copies selected components to an internal clipboard.
        /// Also copies connection lines that connect only selected components.
        /// </summary>
        public void CopySelectedComponents()
        {
            var selectedComponents = _selectionManager.SelectedComponents.ToList();
            if (selectedComponents.Count == 0) return;

            var dto = new Beep.Skia.Serialization.DiagramDto();

            foreach (var c in selectedComponents)
            {
                var comp = new Beep.Skia.Serialization.ComponentDto
                {
                    Type = c.GetType().AssemblyQualifiedName,
                    X = c.X,
                    Y = c.Y,
                    Width = c.Width,
                    Height = c.Height,
                    Name = c.Name ?? string.Empty
                };
                // Capture all NodeProperties generically
                foreach (var kvp in c.GetProperties())
                {
                    try
                    {
                        if (kvp.Value != null)
                            comp.PropertyBag[kvp.Key] = Convert.ToString(kvp.Value);
                    }
                    catch { }
                }
                // Persist known important properties per namespace
                PersistPropertyBag(c, comp);
                foreach (var p in c.InConnectionPoints)
                {
                    if (p != null) comp.InPointIds.Add(p.Id);
                }
                foreach (var p in c.OutConnectionPoints)
                {
                    if (p != null) comp.OutPointIds.Add(p.Id);
                }
                dto.Components.Add(comp);
            }

            // Only copy lines where BOTH endpoints belong to selected components
            var selectedSet = new HashSet<SkiaComponent>(selectedComponents);
            foreach (var l in _lines)
            {
                if (l?.Start?.Component is SkiaComponent s1 && l.End?.Component is SkiaComponent s2
                    && selectedSet.Contains(s1) && selectedSet.Contains(s2))
                {
                    var line = new Beep.Skia.Serialization.LineDto
                    {
                        StartPointId = l.Start.Id,
                        EndPointId = l.End.Id,
                        ShowStartArrow = l.ShowStartArrow,
                        ShowEndArrow = l.ShowEndArrow,
                        Label1 = l.Label1,
                        Label2 = l.Label2,
                        Label3 = l.Label3,
                        DataTypeLabel = (l as ConnectionLine)?.DataTypeLabel,
                        LineColor = (uint)l.LineColor,
                        RoutingMode = (int)l.RoutingMode,
                        FlowDirection = (int)l.FlowDirection,
                        Label1Placement = (int)l.Label1Placement,
                        Label2Placement = (int)l.Label2Placement,
                        Label3Placement = (int)l.Label3Placement,
                        DataLabelPlacement = (int)l.DataLabelPlacement,
                        ArrowSize = l.ArrowSize,
                        DashPattern = l.DashPattern,
                        ShowStatusIndicator = l.ShowStatusIndicator,
                        Status = (int)l.Status,
                        StatusColor = (uint)l.StatusColor,
                        IsAnimated = l.IsAnimated,
                        IsDataFlowAnimated = (l as ConnectionLine)?.IsDataFlowAnimated ?? false,
                        DataFlowSpeed = (l as ConnectionLine)?.DataFlowSpeed ?? 0f,
                        DataFlowParticleSize = (l as ConnectionLine)?.DataFlowParticleSize ?? 0f,
                        DataFlowColor = (uint)((l as ConnectionLine)?.DataFlowColor ?? default),
                        StartMultiplicity = (int)l.StartMultiplicity,
                        EndMultiplicity = (int)l.EndMultiplicity,
                        SchemaJson = (l as ConnectionLine)?.SchemaJson,
                        ExpectedSchemaJson = (l as ConnectionLine)?.ExpectedSchemaJson
                    };
                    dto.Lines.Add(line);
                }
            }

            try
            {
                _clipboardData = System.Text.Json.JsonSerializer.Serialize(dto);
            }
            catch
            {
                return;
            }
        }

        /// <summary>
        /// Pastes components from the internal clipboard at the specified position.
        /// Components are offset so the paste position aligns with the top-left of the copied bounding box.
        /// New connection point GUIDs are generated to avoid conflicts.
        /// </summary>
        /// <param name="position">The position to paste at.</param>
        public void PasteComponents(SKPoint position)
        {
            if (string.IsNullOrWhiteSpace(_clipboardData)) return;

            Beep.Skia.Serialization.DiagramDto dto;
            try
            {
                dto = System.Text.Json.JsonSerializer.Deserialize<Beep.Skia.Serialization.DiagramDto>(_clipboardData);
            }
            catch
            {
                return;
            }
            if (dto == null || dto.Components.Count == 0) return;

            // Calculate offset from original bounding box top-left to paste position
            float minX = float.MaxValue, minY = float.MaxValue;
            foreach (var c in dto.Components)
            {
                if (c.X < minX) minX = c.X;
                if (c.Y < minY) minY = c.Y;
            }
            float offsetX = position.X - minX;
            float offsetY = position.Y - minY;

            // Build a mapping of old GUID -> new GUID for connection points
            var guidMap = new Dictionary<Guid, Guid>();
            var oldToNewPoint = new Dictionary<Guid, IConnectionPoint>();

            // Deselect current selection
            _selectionManager.ClearSelection();

            // Create components
            var createdComponents = new List<SkiaComponent>();
            foreach (var compDto in dto.Components)
            {
                try
                {
                    var type = Type.GetType(compDto.Type, throwOnError: false);
                    if (type == null) continue;
                    if (Activator.CreateInstance(type) is SkiaComponent instance)
                    {
                        instance.X = compDto.X + offsetX;
                        instance.Y = compDto.Y + offsetY;
                        instance.Width = compDto.Width;
                        instance.Height = compDto.Height;
                        instance.Name = compDto.Name ?? string.Empty;
                        // Apply property bag values
                        if (compDto.PropertyBag.Count > 0)
                        {
                            var propValues = new Dictionary<string, object>();
                            foreach (var kvp in compDto.PropertyBag)
                                propValues[kvp.Key] = kvp.Value;
                            try { instance.SetPropperties(propValues); } catch { }
                        }
                        _components.Add(instance);
                        try { instance.BoundsChanged += OnComponentBoundsChanged; } catch { }
                        // Generate new GUIDs and map old->new
                        foreach (var cp in instance.InConnectionPoints)
                        {
                            if (cp != null)
                            {
                                var oldId = cp.Id;
                                if (cp is ConnectionPoint connPt)
                                {
                                    var newId = Guid.NewGuid();
                                    connPt.SetId(newId);
                                    guidMap[oldId] = newId;
                                    oldToNewPoint[oldId] = cp;
                                }
                            }
                        }
                        foreach (var cp in instance.OutConnectionPoints)
                        {
                            if (cp != null)
                            {
                                var oldId = cp.Id;
                                if (cp is ConnectionPoint connPt)
                                {
                                    var newId = Guid.NewGuid();
                                    connPt.SetId(newId);
                                    guidMap[oldId] = newId;
                                    oldToNewPoint[oldId] = cp;
                                }
                            }
                        }
                        RegisterConnectionPoints(instance);
                        createdComponents.Add(instance);
                        _selectionManager.AddToSelection(instance);
                    }
                }
                catch { }
            }

            // Create connection lines using mapped GUIDs
            var createdLines = new List<IConnectionLine>();
            foreach (var lineDto in dto.Lines)
            {
                try
                {
                    if (!guidMap.TryGetValue(lineDto.StartPointId, out var newStartId)) continue;
                    if (!guidMap.TryGetValue(lineDto.EndPointId, out var newEndId)) continue;
                    var startPt = GetConnectionPoint(newStartId);
                    var endPt = GetConnectionPoint(newEndId);
                    if (startPt == null || endPt == null) continue;

                    var l = new ConnectionLine(startPt, endPt, () => RequestRedraw())
                    {
                        ShowStartArrow = lineDto.ShowStartArrow,
                        ShowEndArrow = lineDto.ShowEndArrow,
                        LineColor = new SKColor(lineDto.LineColor),
                        RoutingMode = (LineRoutingMode)lineDto.RoutingMode,
                        FlowDirection = (DataFlowDirection)lineDto.FlowDirection,
                        Label1 = lineDto.Label1,
                        Label2 = lineDto.Label2,
                        Label3 = lineDto.Label3,
                        DataTypeLabel = lineDto.DataTypeLabel,
                        Label1Placement = (LabelPlacement)lineDto.Label1Placement,
                        Label2Placement = (LabelPlacement)lineDto.Label2Placement,
                        Label3Placement = (LabelPlacement)lineDto.Label3Placement,
                        DataLabelPlacement = (LabelPlacement)lineDto.DataLabelPlacement,
                        ArrowSize = lineDto.ArrowSize > 0 ? lineDto.ArrowSize : 10f,
                        DashPattern = lineDto.DashPattern,
                        ShowStatusIndicator = lineDto.ShowStatusIndicator,
                        Status = (LineStatus)lineDto.Status,
                        StatusColor = new SKColor(lineDto.StatusColor),
                        IsAnimated = lineDto.IsAnimated,
                        IsDataFlowAnimated = lineDto.IsDataFlowAnimated,
                        DataFlowSpeed = lineDto.DataFlowSpeed > 0 ? lineDto.DataFlowSpeed : 1f,
                        DataFlowParticleSize = lineDto.DataFlowParticleSize > 0 ? lineDto.DataFlowParticleSize : 4f,
                        DataFlowColor = lineDto.DataFlowColor != 0 ? new SKColor(lineDto.DataFlowColor) : new SKColor(41, 98, 255),
                        StartMultiplicity = (ERDMultiplicity)lineDto.StartMultiplicity,
                        EndMultiplicity = (ERDMultiplicity)lineDto.EndMultiplicity,
                        SchemaJson = lineDto.SchemaJson,
                        ExpectedSchemaJson = lineDto.ExpectedSchemaJson
                    };
                    _lines.Add(l);
                    createdLines.Add(l);
                }
                catch { }
            }

            // Record undo action
            if (createdComponents.Count > 0)
            {
                _historyManager.ExecuteAction(new PasteComponentsAction(this, createdComponents, createdLines));
            }

            DrawSurface?.Invoke(this, null);
        }

        private void PersistPropertyBag(SkiaComponent c, Beep.Skia.Serialization.ComponentDto comp)
        {
            try
            {
                var ns = c.GetType().Namespace ?? "";
                var t = c.GetType();

                Action<string, string> persist = (propName, key) =>
                {
                    var p = t.GetProperty(propName);
                    if (p != null)
                    {
                        var val = p.GetValue(c);
                        if (val is string s && !string.IsNullOrEmpty(s)) comp.PropertyBag[key] = s;
                        else if (val != null) comp.PropertyBag[key] = Convert.ToString(val);
                    }
                };

                if (ns == "Beep.Skia.MindMap" || ns == "Beep.Skia.StateMachine")
                {
                    persist("Title", "Title");
                    if (ns == "Beep.Skia.MindMap") persist("Notes", "Notes");
                }

                if (ns == "Beep.Skia.Flowchart" || ns == "Beep.Skia.PM" || ns == "Beep.Skia.ERD"
                    || ns == "Beep.Skia.MindMap" || ns == "Beep.Skia.StateMachine")
                {
                    persist("InPortCount", "InPortCount");
                    persist("OutPortCount", "OutPortCount");
                }
                if (ns == "Beep.Skia.Flowchart")
                {
                    persist("ShowTopBottomPorts", "ShowTopBottomPorts");
                    persist("OutPortsOnTop", "OutPortsOnTop");
                }
                if (ns == "Beep.Skia.PM")
                {
                    persist("Title", "Title");
                    persist("Label", "Label");
                    persist("PercentComplete", "PercentComplete");
                }
                if (ns == "Beep.Skia.ERD")
                {
                    persist("EntityName", "EntityName");
                    persist("RowsText", "RowsText");
                    persist("RowIdsCsv", "RowIdsCsv");
                }
            }
            catch { }
        }

        /// <summary>
        /// Moves selected components by the specified offset.
        /// </summary>
        /// <param name="offset">The offset to move by.</param>
        public void MoveSelectedComponents(SKPoint offset)
        {
            if (_selectionManager.SelectionCount == 0) return;

            var snappedOffset = SnapToGrid ? SnapOffset(offset) : offset;
            _historyManager.ExecuteAction(new MoveComponentsAction(this, _selectionManager.SelectedComponents.ToList(), snappedOffset));

            foreach (var component in _selectionManager.SelectedComponents)
            {
                component.MoveBy(snappedOffset.X, snappedOffset.Y);
                // Keep connection point registry in sync
                RefreshConnectionPoints(component);
            }

            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Removes all components and connection lines from the drawing manager.
        /// This is a safe public API intended for demos and tools.
        /// </summary>
        public void ClearComponents()
        {
            // Remove all lines
            _lines.Clear();

            // Remove all components
            // Unsubscribe events before clearing
            foreach (var c in _components.ToList())
            {
                try { c.BoundsChanged -= OnComponentBoundsChanged; } catch { }
            }
            _components.Clear();

            // Clear connection point registry
            _connectionPointsById.Clear();
            _ownerByConnectionPoint.Clear();

            // Clear selection
            try { _selectionManager?.ClearSelection(); } catch { }

            // Notify renderers
            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Returns a snapshot of current components.
        /// </summary>
        /// <returns>Read-only list of components.</returns>
        public IReadOnlyList<SkiaComponent> GetComponents() => _components.AsReadOnly();

        /// <summary>
        /// Gets all connection lines in the diagram.
        /// </summary>
        public IReadOnlyList<IConnectionLine> GetLines() => _lines.AsReadOnly();

        /// <summary>
        /// Loads a diagram template — creates components and auto-connects sequential components
        /// (component[i].OutPoints[0] → component[i+1].InPoints[0]) for each line in the template.
        /// For more complex connections, use LoadFromDto() with proper GUID-based serialization.
        /// </summary>
        public void LoadTemplate(Beep.Skia.Serialization.DiagramDto dto)
        {
            if (dto == null) return;
            ClearComponents();

            var created = new List<SkiaComponent>();
            foreach (var comp in dto.Components)
            {
                try
                {
                    var type = Type.GetType(comp.Type, throwOnError: false);
                    if (type == null) continue;
                    if (Activator.CreateInstance(type) is SkiaComponent instance)
                    {
                        instance.X = comp.X;
                        instance.Y = comp.Y;
                        instance.Width = comp.Width;
                        instance.Height = comp.Height;
                        instance.Name = comp.Name ?? string.Empty;
                        if (comp.PropertyBag != null && comp.PropertyBag.Count > 0)
                        {
                            var propValues = new Dictionary<string, object>();
                            foreach (var kvp in comp.PropertyBag)
                                propValues[kvp.Key] = kvp.Value;
                            try { instance.SetPropperties(propValues); } catch { }
                        }
                        _components.Add(instance);
                        try { instance.BoundsChanged += OnComponentBoundsChanged; } catch { }
                        RegisterConnectionPoints(instance);
                        created.Add(instance);
                    }
                }
                catch { }
            }

            // Connect lines using explicit component indices when provided (templates),
            // otherwise fall back to sequential auto-connect for legacy DTOs.
            for (int i = 0; i < dto.Lines.Count; i++)
            {
                try
                {
                    var lineDto = dto.Lines[i];
                    int srcIdx = lineDto.StartComponentIndex;
                    int dstIdx = lineDto.EndComponentIndex;

                    if (srcIdx < 0 || dstIdx < 0)
                    {
                        srcIdx = i;
                        dstIdx = i + 1;
                    }

                    if (srcIdx < 0 || srcIdx >= created.Count) continue;
                    if (dstIdx < 0 || dstIdx >= created.Count) continue;

                    var src = created[srcIdx];
                    var dst = created[dstIdx];
                    if (src.OutConnectionPoints.Count > 0 && dst.InConnectionPoints.Count > 0)
                    {
                        var l = new ConnectionLine(src.OutConnectionPoints[0], dst.InConnectionPoints[0], () => RequestRedraw())
                        {
                            ShowStartArrow = lineDto.ShowStartArrow,
                            ShowEndArrow = lineDto.ShowEndArrow,
                            Label1 = lineDto.Label1,
                            Label2 = lineDto.Label2,
                            Label3 = lineDto.Label3,
                            LineColor = lineDto.LineColor != 0 ? new SKColor(lineDto.LineColor) : new SKColor(0x75, 0x75, 0x75),
                            RoutingMode = (LineRoutingMode)lineDto.RoutingMode,
                            StartMultiplicity = (ERDMultiplicity)lineDto.StartMultiplicity,
                            EndMultiplicity = (ERDMultiplicity)lineDto.EndMultiplicity
                        };
                        _lines.Add(l);
                    }
                }
                catch { }
            }

            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Gets a dictionary of available diagram templates keyed by name.
        /// </summary>
        public static IReadOnlyDictionary<string, Func<Beep.Skia.Serialization.DiagramDto>> GetTemplates()
            => DiagramTemplates.All;

        /// <summary>
        /// Snaps an offset to the grid.
        /// </summary>
        /// <param name="offset">The offset to snap.</param>
        /// <returns>The snapped offset.</returns>
        private SKPoint SnapOffset(SKPoint offset)
        {
            // For movement, we want to snap the final position, not the offset
            return offset; // Grid snapping for movement is handled differently
        }

        /// <summary>
        /// Aligns selected components to the specified edge of the primary (first) selected component.
        /// </summary>
        public void AlignLeft()
        {
            var selected = _selectionManager.SelectedComponents.ToList();
            if (selected.Count < 2) return;
            var primary = selected[0];
            var others = selected.Skip(1).ToList();
            var beforePositions = others.Select(c => new SKPoint(c.X, c.Y)).ToList();
            foreach (var c in others) c.X = primary.X;
            _historyManager.ExecuteAction(new AlignComponentsAction(this, others, beforePositions));
            foreach (var c in others) RefreshConnectionPoints(c);
            DrawSurface?.Invoke(this, null);
        }

        public void AlignRight()
        {
            var selected = _selectionManager.SelectedComponents.ToList();
            if (selected.Count < 2) return;
            var primary = selected[0];
            float rightEdge = primary.X + primary.Width;
            var others = selected.Skip(1).ToList();
            var beforePositions = others.Select(c => new SKPoint(c.X, c.Y)).ToList();
            foreach (var c in others) c.X = rightEdge - c.Width;
            _historyManager.ExecuteAction(new AlignComponentsAction(this, others, beforePositions));
            foreach (var c in others) RefreshConnectionPoints(c);
            DrawSurface?.Invoke(this, null);
        }

        public void AlignTop()
        {
            var selected = _selectionManager.SelectedComponents.ToList();
            if (selected.Count < 2) return;
            var primary = selected[0];
            var others = selected.Skip(1).ToList();
            var beforePositions = others.Select(c => new SKPoint(c.X, c.Y)).ToList();
            foreach (var c in others) c.Y = primary.Y;
            _historyManager.ExecuteAction(new AlignComponentsAction(this, others, beforePositions));
            foreach (var c in others) RefreshConnectionPoints(c);
            DrawSurface?.Invoke(this, null);
        }

        public void AlignBottom()
        {
            var selected = _selectionManager.SelectedComponents.ToList();
            if (selected.Count < 2) return;
            var primary = selected[0];
            float bottomEdge = primary.Y + primary.Height;
            var others = selected.Skip(1).ToList();
            var beforePositions = others.Select(c => new SKPoint(c.X, c.Y)).ToList();
            foreach (var c in others) c.Y = bottomEdge - c.Height;
            _historyManager.ExecuteAction(new AlignComponentsAction(this, others, beforePositions));
            foreach (var c in others) RefreshConnectionPoints(c);
            DrawSurface?.Invoke(this, null);
        }

        public void AlignCenterHorizontal()
        {
            var selected = _selectionManager.SelectedComponents.ToList();
            if (selected.Count < 2) return;
            var primary = selected[0];
            float centerX = primary.X + primary.Width / 2f;
            var others = selected.Skip(1).ToList();
            var beforePositions = others.Select(c => new SKPoint(c.X, c.Y)).ToList();
            foreach (var c in others) c.X = centerX - c.Width / 2f;
            _historyManager.ExecuteAction(new AlignComponentsAction(this, others, beforePositions));
            foreach (var c in others) RefreshConnectionPoints(c);
            DrawSurface?.Invoke(this, null);
        }

        public void AlignCenterVertical()
        {
            var selected = _selectionManager.SelectedComponents.ToList();
            if (selected.Count < 2) return;
            var primary = selected[0];
            float centerY = primary.Y + primary.Height / 2f;
            var others = selected.Skip(1).ToList();
            var beforePositions = others.Select(c => new SKPoint(c.X, c.Y)).ToList();
            foreach (var c in others) c.Y = centerY - c.Height / 2f;
            _historyManager.ExecuteAction(new AlignComponentsAction(this, others, beforePositions));
            foreach (var c in others) RefreshConnectionPoints(c);
            DrawSurface?.Invoke(this, null);
        }

        public void DistributeHorizontal()
        {
            var selected = _selectionManager.SelectedComponents.OrderBy(c => c.X).ToList();
            if (selected.Count < 3) return;
            float totalWidth = selected.Sum(c => c.Width);
            float available = selected.Last().X + selected.Last().Width - selected.First().X - totalWidth;
            float spacing = available / (selected.Count - 1);
            var beforePositions = selected.Select(c => new SKPoint(c.X, c.Y)).ToList();
            float x = selected[0].X + selected[0].Width + spacing;
            for (int i = 1; i < selected.Count - 1; i++)
            {
                selected[i].X = x;
                x += selected[i].Width + spacing;
            }
            _historyManager.ExecuteAction(new AlignComponentsAction(this, selected, beforePositions));
            foreach (var c in selected) RefreshConnectionPoints(c);
            DrawSurface?.Invoke(this, null);
        }

        public void DistributeVertical()
        {
            var selected = _selectionManager.SelectedComponents.OrderBy(c => c.Y).ToList();
            if (selected.Count < 3) return;
            float totalHeight = selected.Sum(c => c.Height);
            float available = selected.Last().Y + selected.Last().Height - selected.First().Y - totalHeight;
            float spacing = available / (selected.Count - 1);
            var beforePositions = selected.Select(c => new SKPoint(c.X, c.Y)).ToList();
            float y = selected[0].Y + selected[0].Height + spacing;
            for (int i = 1; i < selected.Count - 1; i++)
            {
                selected[i].Y = y;
                y += selected[i].Height + spacing;
            }
            _historyManager.ExecuteAction(new AlignComponentsAction(this, selected, beforePositions));
            foreach (var c in selected) RefreshConnectionPoints(c);
            DrawSurface?.Invoke(this, null);
        }

        /// <summary>
        /// Gets the component at the specified point.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns>The component at the specified point, or null if no component is found.</returns>
        internal SkiaComponent GetComponentAt(SKPoint canvasPoint)
        {
            // Compute corresponding screen point for testing static overlays
            SKPoint screenPoint;
            try { screenPoint = new SKPoint(canvasPoint.X * Zoom + PanOffset.X, canvasPoint.Y * Zoom + PanOffset.Y); }
            catch { screenPoint = canvasPoint; }

            for (int i = _components.Count - 1; i >= 0; i--)
            {
                var component = _components[i];
                if (component == null) continue;
                try
                {
                    if (component.IsStatic)
                    {
                        // Screen-space hit-test for overlays (palette/property editor) so they can handle input.
                        var rect = new SKRect(component.X, component.Y, component.X + component.Width, component.Y + component.Height);
                        if (rect.Contains(screenPoint)) return component;
                    }
                    else
                    {
                        // World-space hit-test
                        if (component.HitTest(canvasPoint)) return component;
                    }
                }
                catch { }
            }
            return null;
        }

        /// <summary>
        /// Registers all connection points of a component into the central registry.
        /// </summary>
        /// <param name="component">The component whose points to register.</param>
        private void RegisterConnectionPoints(SkiaComponent component)
        {
            if (component == null) return;
            foreach (var cp in component.InConnectionPoints.Concat(component.OutConnectionPoints).Where(p => p != null))
            {
                // Ensure component reference is set
                if (cp.Component == null) cp.Component = component;
                _connectionPointsById[cp.Id] = cp;
                _ownerByConnectionPoint[cp] = component;
            }
        }

        /// <summary>
        /// Removes all connection points of a component from the central registry.
        /// </summary>
        /// <param name="component">The component to unregister.</param>
        private void UnregisterConnectionPoints(SkiaComponent component)
        {
            if (component == null) return;
            foreach (var cp in component.InConnectionPoints.Concat(component.OutConnectionPoints).Where(p => p != null))
            {
                _connectionPointsById.Remove(cp.Id);
                _ownerByConnectionPoint.Remove(cp);
            }
        }

        /// <summary>
        /// Refreshes a component's connection points in the registry, e.g. after resize/move.
        /// Call this after layout changes.
        /// </summary>
        /// <param name="component">The component to refresh.</param>
        public void RefreshConnectionPoints(SkiaComponent component)
        {
            if (component == null) return;
            // Remove then re-add to capture new instances/geometry
            UnregisterConnectionPoints(component);
            RegisterConnectionPoints(component);
        }

        private void OnComponentBoundsChanged(object sender, SKRectEventArgs e)
        {
            if (sender is SkiaComponent comp)
            {
                RefreshConnectionPoints(comp);
            }
        }
    }
}
