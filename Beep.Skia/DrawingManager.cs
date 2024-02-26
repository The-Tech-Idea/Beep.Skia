 using SkiaSharp;
    using System.Collections.Generic;
    using System.ComponentModel;

namespace Beep.Skia
{
    using SkiaSharp;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class DrawingManager
    {
        private readonly List<ISkiaWorkFlowComponent> _components;
        private readonly List<IConnectionLine> _lines;
        private bool _isDragging;
        private bool _isDrawingLine;
        private ISkiaWorkFlowComponent _draggingComponent;
        private SKPoint _draggingOffset;
        private SKPoint _mousePosition;
        private IConnectionPoint _sourcePoint;
        private IConnectionLine _currentLine;
        public  SKCanvas Canvas { get; set; }
        public event EventHandler<ConnectionEventArgs> DrawSurface;
        public DrawingManager()
        {
            _components = new List<ISkiaWorkFlowComponent>();
            _lines = new List<IConnectionLine>();
           

           
        }
        public void AddComponent(ISkiaWorkFlowComponent component)
        {
            _components.Add(component);
            DrawSurface?.Invoke(this, null);
        }
        public void RemoveComponent(ISkiaWorkFlowComponent component)
        {
            _components.Remove(component);
            DrawSurface?.Invoke(this, null);
        }
        public void ConnectComponents(ISkiaWorkFlowComponent component1, ISkiaWorkFlowComponent component2)
        {
            component1.ConnectTo(component2);
            component2.ConnectTo(component1);

            IConnectionPoint connectionPoint1 = component1.OutConnectionPoints.FirstOrDefault();
            IConnectionPoint connectionPoint2 = component2.InConnectionPoints.FirstOrDefault();
            if (connectionPoint1 != null && connectionPoint2 != null)
            {
                var line = new ConnectionLine(connectionPoint1, connectionPoint2, () => { /* InvalidateSurface callback */ });
                _lines.Add(line);
            }
            DrawSurface?.Invoke(this, null);
        }
        public void DisconnectComponents(ISkiaWorkFlowComponent component1, ISkiaWorkFlowComponent component2)
        {
            component1.DisconnectFrom(component2);
            component2.DisconnectFrom(component1);

            var lineToRemove = _lines.FirstOrDefault(line => (line.Start.Component == component1 && line.End.Component == component2) || (line.Start.Component == component2 && line.End.Component == component1));
            if (lineToRemove != null)
            {
                _lines.Remove(lineToRemove);
            }
            DrawSurface?.Invoke(this, null);
        }
        public void Draw(SKCanvas canvas)
        {

            Canvas = canvas;
            foreach (var component in _components)
            {
                component.Draw(canvas);
            }

            foreach (var line in _lines)
            {
                line.Draw(canvas);
            }

            if (_isDrawingLine && _currentLine != null)
            {
                _currentLine.Draw(canvas);
            }
        }
        public void HandleMouseDown(SKPoint point)
        {
            var component = GetComponentAt(point);
            if (component != null)
            {
                _isDragging = true;
                _draggingComponent = component;
                _draggingOffset = point - _draggingComponent.Bounds.Location;
            }
            else
            {
                var sourcePoint = GetConnectionPointAt(point);
                if (sourcePoint != null)
                {
                    _isDrawingLine = true;
                    StartDrawingLine(sourcePoint, point);
                }
            }
            DrawSurface?.Invoke(this, null);
        }
        public void HandleMouseUp(SKPoint point)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _draggingComponent = null;
            }
            else if (_isDrawingLine)
            {
                _isDrawingLine = false;
                var targetPoint = GetConnectionPointAt(point);
                if (targetPoint != null && _sourcePoint.Component != targetPoint.Component && !_sourcePoint.Component.IsConnectedTo(targetPoint.Component))
                {
                    _currentLine.End = targetPoint;
                    _lines.Add(_currentLine);
                    ConnectComponents(_sourcePoint.Component, targetPoint.Component);
                }
                _currentLine = null;
            }
            DrawSurface?.Invoke(this, null);
        }
        public void HandleMouseMove(SKPoint point)
        {
            _mousePosition = point;
            if (_isDragging && _draggingComponent != null)
            {
                _draggingComponent.Move(_mousePosition - _draggingOffset - _draggingComponent.Bounds.Location);
            }
            else if (_isDrawingLine && _currentLine != null)
            {
                _currentLine.EndPoint = point;
            }
            DrawSurface?.Invoke(this, null);
        }
        private ISkiaWorkFlowComponent GetComponentAt(SKPoint point)
        {
            return _components.LastOrDefault(component => component.HitTest(point));
        }
        private IConnectionPoint GetConnectionPointAt(SKPoint point)
        {
            foreach (var component in _components)
            {
                var connectionPoint = component.InConnectionPoints.Concat(component.OutConnectionPoints).FirstOrDefault(cp=>cp.Bounds.Contains(point));
                if (connectionPoint != null)
                {
                    return connectionPoint;
                }
            }
            return null;
        }
        private void StartDrawingLine(IConnectionPoint sourcePoint, SKPoint point)
        {
            _sourcePoint = sourcePoint;
            _currentLine = new ConnectionLine(sourcePoint, point, () => { /* InvalidateSurface callback */ });
            DrawSurface?.Invoke(this, null);
        }
    }
}
