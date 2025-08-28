using SkiaSharp;
using System.Collections.Generic;

namespace Beep.Skia
{
    /// <summary>
    /// Base class for all drawing actions that can be undone/redone.
    /// </summary>
    public abstract class DrawingAction
    {
        /// <summary>
        /// Executes the action.
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

        public AddComponentAction(DrawingManager manager, SkiaComponent component)
        {
            _manager = manager;
            _component = component;
        }

        public override void Execute()
        {
            // Component is already added in the manager
        }

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

        public RemoveComponentAction(DrawingManager manager, SkiaComponent component, List<IConnectionLine> lines)
        {
            _manager = manager;
            _component = component;
            _lines = lines;
        }

        public override void Execute()
        {
            // Component is already removed in the manager
        }

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

        public DeleteComponentsAction(DrawingManager manager, List<SkiaComponent> components, List<IConnectionLine> lines)
        {
            _manager = manager;
            _components = components;
            _lines = lines;
        }

        public override void Execute()
        {
            // Components are already deleted in the manager
        }

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

        public MoveComponentsAction(DrawingManager manager, List<SkiaComponent> components, SKPoint offset)
        {
            _manager = manager;
            _components = components;
            _offset = offset;
        }

        public override void Execute()
        {
            // Components are already moved in the manager
        }

        public override void Undo()
        {
            foreach (var component in _components)
            {
                component.Move(new SKPoint(-_offset.X, -_offset.Y));
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

        public ConnectComponentsAction(DrawingManager manager, SkiaComponent component1, SkiaComponent component2, IConnectionLine line)
        {
            _manager = manager;
            _component1 = component1;
            _component2 = component2;
            _line = line;
        }

        public override void Execute()
        {
            // Components are already connected in the manager
        }

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

        public DisconnectComponentsAction(DrawingManager manager, SkiaComponent component1, SkiaComponent component2, IConnectionLine line)
        {
            _manager = manager;
            _component1 = component1;
            _component2 = component2;
            _line = line;
        }

        public override void Execute()
        {
            // Components are already disconnected in the manager
        }

        public override void Undo()
        {
            _manager.ConnectComponents(_component1, _component2);
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

        public override void Execute()
        {
            // Line is already moved in the manager
        }

        public override void Undo()
        {
            _manager.MoveConnectionLine(_line, _oldStartPoint, _oldEndPoint);
        }
    }
}
