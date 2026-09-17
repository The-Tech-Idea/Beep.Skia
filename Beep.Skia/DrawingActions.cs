using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using Beep.Skia.Model;
namespace Beep.Skia
{
    /// <summary>
    /// Base class for all drawing actions that can be undone/redone.
    /// Actions are created after their change has been applied by the caller;
    /// <see cref="Execute"/> re-applies the change when redone, <see cref="Undo"/> reverses it.
    /// </summary>
    public abstract class DrawingAction
    {
        /// <summary>
        /// Re-applies the action (used for redo).
        /// </summary>
        public abstract void Execute();

        /// <summary>
        /// Undoes the action.
        /// </summary>
        public abstract void Undo();
    }

    /// <summary>
    /// Action for adding a component.
    /// </summary>
    public class AddComponentAction : DrawingAction
    {
        private readonly DrawingManager _manager;
        private readonly SkiaComponent _component;

        /// <summary>
        /// Initializes a new instance of the AddComponentAction class.
        /// </summary>
        public AddComponentAction(DrawingManager manager, SkiaComponent component)
        {
            _manager = manager;
            _component = component;
        }

        /// <summary>
        /// Gets or sets the execute.
        /// </summary>
        public override void Execute()
        {
            _manager.AddComponent(_component);
        }

        /// <summary>
        /// Gets or sets the undo.
        /// </summary>
        public override void Undo()
        {
            _manager.RemoveComponent(_component);
        }
    }

    /// <summary>
    /// Action for removing a component.
    /// </summary>
    public class RemoveComponentAction : DrawingAction
    {
        private readonly DrawingManager _manager;
        private readonly SkiaComponent _component;
        private readonly List<IConnectionLine> _lines;

        /// <summary>
        /// Initializes a new instance of the RemoveComponentAction class.
        /// </summary>
        public RemoveComponentAction(DrawingManager manager, SkiaComponent component, List<IConnectionLine> lines)
        {
            _manager = manager;
            _component = component;
            _lines = lines;
        }

        /// <summary>
        /// Gets or sets the execute.
        /// </summary>
        public override void Execute()
        {
            _manager.RemoveComponent(_component);
        }

        /// <summary>
        /// Gets or sets the undo.
        /// </summary>
        public override void Undo()
        {
            _manager.AddComponent(_component);
            foreach (var line in _lines)
            {
                _manager.AddLine(line);
            }
        }
    }

    /// <summary>
    /// Action for deleting multiple components.
    /// </summary>
    public class DeleteComponentsAction : DrawingAction
    {
        private readonly DrawingManager _manager;
        private readonly List<SkiaComponent> _components;
        private readonly List<IConnectionLine> _lines;

        /// <summary>
        /// Initializes a new instance of the DeleteComponentsAction class.
        /// </summary>
        public DeleteComponentsAction(DrawingManager manager, List<SkiaComponent> components, List<IConnectionLine> lines)
        {
            _manager = manager;
            _components = components;
            _lines = lines;
        }

        /// <summary>
        /// Gets or sets the execute.
        /// </summary>
        public override void Execute()
        {
            foreach (var component in _components.ToList())
            {
                _manager.RemoveComponent(component);
            }
        }

        /// <summary>
        /// Gets or sets the undo.
        /// </summary>
        public override void Undo()
        {
            foreach (var component in _components)
            {
                _manager.AddComponent(component);
            }
            foreach (var line in _lines)
            {
                _manager.AddLine(line);
            }
        }
    }

    /// <summary>
    /// Action for moving components.
    /// </summary>
    public class MoveComponentsAction : DrawingAction
    {
        private readonly DrawingManager _manager;
        private readonly List<SkiaComponent> _components;
        private readonly SKPoint _offset;

        /// <summary>
        /// Initializes a new instance of the MoveComponentsAction class.
        /// </summary>
        public MoveComponentsAction(DrawingManager manager, List<SkiaComponent> components, SKPoint offset)
        {
            _manager = manager;
            _components = components;
            _offset = offset;
        }

        /// <summary>
        /// Gets or sets the execute.
        /// </summary>
        public override void Execute()
        {
            foreach (var component in _components)
            {
                component.MoveBy(_offset.X, _offset.Y);
                _manager.RefreshConnectionPoints(component);
            }
        }

        /// <summary>
        /// Gets or sets the undo.
        /// </summary>
        public override void Undo()
        {
            foreach (var component in _components)
            {
                component.MoveBy(-_offset.X, -_offset.Y);
                _manager.RefreshConnectionPoints(component);
            }
        }
    }

    /// <summary>
    /// Action for connecting components.
    /// </summary>
    public class ConnectComponentsAction : DrawingAction
    {
        private readonly DrawingManager _manager;
        private readonly SkiaComponent _component1;
        private readonly SkiaComponent _component2;
        private readonly IConnectionLine _line;

        /// <summary>
        /// Initializes a new instance of the ConnectComponentsAction class.
        /// </summary>
        public ConnectComponentsAction(DrawingManager manager, SkiaComponent component1, SkiaComponent component2, IConnectionLine line)
        {
            _manager = manager;
            _component1 = component1;
            _component2 = component2;
            _line = line;
        }

        /// <summary>
        /// Gets or sets the execute.
        /// </summary>
        public override void Execute()
        {
            _manager.ConnectComponents(_component1, _component2);
        }

        /// <summary>
        /// Gets or sets the undo.
        /// </summary>
        public override void Undo()
        {
            _manager.DisconnectComponents(_component1, _component2);
        }
    }

    /// <summary>
    /// Action for disconnecting components.
    /// </summary>
    public class DisconnectComponentsAction : DrawingAction
    {
        private readonly DrawingManager _manager;
        private readonly SkiaComponent _component1;
        private readonly SkiaComponent _component2;
        private readonly IConnectionLine _line;

        /// <summary>
        /// Initializes a new instance of the DisconnectComponentsAction class.
        /// </summary>
        public DisconnectComponentsAction(DrawingManager manager, SkiaComponent component1, SkiaComponent component2, IConnectionLine line)
        {
            _manager = manager;
            _component1 = component1;
            _component2 = component2;
            _line = line;
        }

        /// <summary>
        /// Gets or sets the execute.
        /// </summary>
        public override void Execute()
        {
            _manager.DisconnectComponents(_component1, _component2);
        }

        /// <summary>
        /// Gets or sets the undo.
        /// </summary>
        public override void Undo()
        {
            _manager.ConnectComponents(_component1, _component2);
        }
    }

    /// <summary>
    /// Action for pasting components from the clipboard.
    /// </summary>
    public class PasteComponentsAction : DrawingAction
    {
        private readonly DrawingManager _manager;
        private readonly List<SkiaComponent> _components;
        private readonly List<IConnectionLine> _lines;

        /// <summary>
        /// Initializes a new instance of the PasteComponentsAction class.
        /// </summary>
        public PasteComponentsAction(DrawingManager manager, List<SkiaComponent> components, List<IConnectionLine> lines = null)
        {
            _manager = manager;
            _components = components ?? new List<SkiaComponent>();
            _lines = lines ?? new List<IConnectionLine>();
        }

        /// <summary>
        /// Gets or sets the execute.
        /// </summary>
        public override void Execute()
        {
            foreach (var component in _components)
            {
                _manager.AddComponent(component);
            }
            foreach (var line in _lines)
            {
                _manager.AddLine(line);
            }
        }

        /// <summary>
        /// Gets or sets the undo.
        /// </summary>
        public override void Undo()
        {
            foreach (var component in _components.ToList())
            {
                _manager.RemoveComponent(component);
            }
        }
    }

    /// <summary>
    /// Action for alignment/distribution operations.
    /// </summary>
    public class AlignComponentsAction : DrawingAction
    {
        private readonly DrawingManager _manager;
        private readonly List<SkiaComponent> _components;
        private readonly List<SKPoint> _beforePositions;
        private readonly List<SKPoint> _afterPositions;

        /// <summary>
        /// Initializes a new instance of the AlignComponentsAction class.
        /// </summary>
        public AlignComponentsAction(DrawingManager manager, List<SkiaComponent> components, List<SKPoint> beforePositions)
        {
            _manager = manager;
            _components = components;
            _beforePositions = beforePositions;
            // Captured after the alignment was applied by the caller.
            _afterPositions = components?.Select(c => new SKPoint(c.X, c.Y)).ToList() ?? new List<SKPoint>();
        }

        /// <summary>
        /// Gets or sets the execute.
        /// </summary>
        public override void Execute()
        {
            for (int i = 0; i < Math.Min(_components.Count, _afterPositions.Count); i++)
            {
                _components[i].X = _afterPositions[i].X;
                _components[i].Y = _afterPositions[i].Y;
                _manager.RefreshConnectionPoints(_components[i]);
            }
        }

        /// <summary>
        /// Gets or sets the undo.
        /// </summary>
        public override void Undo()
        {
            for (int i = 0; i < Math.Min(_components.Count, _beforePositions.Count); i++)
            {
                _components[i].X = _beforePositions[i].X;
                _components[i].Y = _beforePositions[i].Y;
                _manager.RefreshConnectionPoints(_components[i]);
            }
        }
    }

    /// <summary>
    /// Action for moving a connection line.
    /// </summary>
    public class MoveLineAction : DrawingAction
    {
        private readonly DrawingManager _manager;
        private readonly IConnectionLine _line;
        private readonly IConnectionPoint _oldStartPoint;
        private readonly IConnectionPoint _oldEndPoint;
        private readonly IConnectionPoint _newStartPoint;
        private readonly IConnectionPoint _newEndPoint;

        /// <summary>
        /// Initializes a new instance of the MoveLineAction class.
        /// </summary>
        public MoveLineAction(DrawingManager manager, IConnectionLine line,
            IConnectionPoint oldStartPoint, IConnectionPoint oldEndPoint,
            IConnectionPoint newStartPoint, IConnectionPoint newEndPoint)
        {
            _manager = manager;
            _line = line;
            _oldStartPoint = oldStartPoint;
            _oldEndPoint = oldEndPoint;
            _newStartPoint = newStartPoint;
            _newEndPoint = newEndPoint;
        }

        /// <summary>
        /// Gets or sets the execute.
        /// </summary>
        public override void Execute()
        {
            _manager.MoveConnectionLine(_line, _newStartPoint, _newEndPoint);
        }

        /// <summary>
        /// Gets or sets the undo.
        /// </summary>
        public override void Undo()
        {
            _manager.MoveConnectionLine(_line, _oldStartPoint, _oldEndPoint);
        }
    }

    /// <summary>
    /// Action for connecting automation nodes.
    /// </summary>
    public class ConnectAutomationNodesAction : DrawingAction
    {
        private readonly DrawingManager _manager;
        private readonly Components.AutomationNode _node1;
        private readonly Components.AutomationNode _node2;
        private readonly IConnectionLine _line;
        private readonly IConnectionPoint _outputPoint;
        private readonly IConnectionPoint _inputPoint;

        /// <summary>
        /// Initializes a new instance of the ConnectAutomationNodesAction class.
        /// </summary>
        public ConnectAutomationNodesAction(DrawingManager manager, Components.AutomationNode node1,
            Components.AutomationNode node2, IConnectionLine line, IConnectionPoint outputPoint, IConnectionPoint inputPoint)
        {
            _manager = manager;
            _node1 = node1;
            _node2 = node2;
            _line = line;
            _outputPoint = outputPoint;
            _inputPoint = inputPoint;
        }

        /// <summary>
        /// Gets or sets the execute.
        /// </summary>
        public override void Execute()
        {
            _outputPoint.IsAvailable = false;
            _inputPoint.IsAvailable = false;
            _outputPoint.Connection = _inputPoint;
            _inputPoint.Connection = _outputPoint;
            _manager.AddLine(_line);
        }

        /// <summary>
        /// Gets or sets the undo.
        /// </summary>
        public override void Undo()
        {
            _outputPoint.IsAvailable = true;
            _inputPoint.IsAvailable = true;
            _outputPoint.Connection = null;
            _inputPoint.Connection = null;
            _manager.RemoveLine(_line);
        }
    }
}
