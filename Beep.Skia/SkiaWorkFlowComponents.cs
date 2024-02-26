using SkiaSharp;

namespace Beep.Skia
{
    public class SkiaWorkFlowComponents: ISkiaWorkFlowComponent
{
        
        public ISkiaWorkFlowComponent StartComponent { get; set; }
        public ISkiaWorkFlowComponent EndComponent { get; set; }
        private bool _isDragging = false;
        private SKPoint _dragOffset = SKPoint.Empty;
        public float X { get; set; }
        public float Y { get; set; }
        public float Radius { get; set; } = 30;
        public string Name { get; set; }
        public IConnectionPoint[] InConnectionPoints { get; private set; } = new ConnectionPoint[2];
        public IConnectionPoint[] OutConnectionPoints { get; private set; } = new ConnectionPoint[2];
        public bool IsRunning { get; set; } = false;
        private float _rotationAngle = 0;
        private ComponentShape _shape;
        public event EventHandler<EventArgs> Clicked;
        public event EventHandler<ConnectionEventArgs> Connected;
        public event EventHandler<ConnectionEventArgs> Disconnected;
        // Event raised when the component's bounds change
        public event EventHandler<SKRectEventArgs> BoundsChanged;
        public List<ISkiaWorkFlowComponent> ConnectedComponents { get; set; }
        public SkiaWorkFlowComponents(float x, float y, string name, ComponentShape shape)
            {
                X = x;
                Y = y;
                Name = name;
                _shape = shape;
          
            SetConnectionPoints();
            }
        private void SetConnectionPoints()
        {
            float offset = Radius / 2;
            InConnectionPoints[0] = new ConnectionPoint { Component = this, Type = ConnectionPointType.In, Index = 0, Position = new SKPoint(X - offset, Y) };
            InConnectionPoints[1] = new ConnectionPoint { Component = this, Type = ConnectionPointType.In, Index = 1, Position = new SKPoint(X, Y - offset) };
            OutConnectionPoints[0] = new ConnectionPoint { Component = this, Type = ConnectionPointType.Out, Index = 0, Position = new SKPoint(X + offset, Y) };
            OutConnectionPoints[1] = new ConnectionPoint { Component = this, Type = ConnectionPointType.Out, Index = 1, Position = new SKPoint(X, Y + offset) };
        }
        public void Draw(SKCanvas canvas)
        {
            // Create a Paint object for drawing the component
            var paint = new SKPaint
            {
                IsAntialias = true
            };

            // Draw the component with the appropriate shape
            switch (_shape)
            {
                case ComponentShape.Circle:
                    paint.Style = SKPaintStyle.Fill;
                    paint.Color = SKColors.Blue;
                    canvas.DrawCircle(X, Y, Radius, paint);
                    paint.Style = SKPaintStyle.Stroke;
                    paint.Color = SKColors.White;
                    canvas.DrawCircle(X, Y, Radius, paint);
                    break;
                case ComponentShape.Square:
                    paint.Style = SKPaintStyle.Fill;
                    paint.Color = SKColors.Blue;
                    var rect = SKRect.Create(X - Radius, Y - Radius,  Radius,  Radius);
                    canvas.DrawRect(rect, paint);
                    paint.Style = SKPaintStyle.Stroke;
                    paint.Color = SKColors.White;
                    canvas.DrawRect(rect, paint);
                    break;
                case ComponentShape.Triangle:
                    paint.Style = SKPaintStyle.Fill;
                    paint.Color = SKColors.Blue;
                    var path = new SKPath();
                    path.MoveTo(X, Y - Radius);
                    path.LineTo(X + Radius, Y + Radius);
                    path.LineTo(X - Radius, Y + Radius);
                    path.Close();
                    canvas.DrawPath(path, paint);
                    paint.Style = SKPaintStyle.Stroke;
                    paint.Color = SKColors.White;
                    canvas.DrawPath(path, paint);
                    break;
                default:
                    // Handle unknown shape
                    break;
            }

            // Draw the component name
            paint.Style = SKPaintStyle.Fill;
            paint.Color = SKColors.Black;
            paint.TextAlign = SKTextAlign.Center;
            paint.TextSize = 10;
            canvas.DrawText(Name, X, Y + Radius / 2 + 10, paint);

            // Draw the connection points
            paint.Style = SKPaintStyle.Fill;
            paint.Color = SKColors.Red;

            // Draw the connection points
            paint.Style = SKPaintStyle.Fill;
            paint.Color = SKColors.Red;

            switch (_shape)
            {
                case ComponentShape.Circle:
                    foreach (var point in InConnectionPoints)
                    {
                        // Position connection point at left (270 degrees)
                        point.Position = new SKPoint(X - Radius, Y);
                        DrawConnectionPoint(canvas, paint, point);
                    }
                    foreach (var point in OutConnectionPoints)
                    {
                        // Position connection point at right (90 degrees)
                        point.Position = new SKPoint(X + Radius, Y);
                        DrawConnectionPoint(canvas, paint, point);
                    }
                    break;

                case ComponentShape.Square:
                    foreach (var point in InConnectionPoints)
                    {
                        // Position connection point in the middle of left side
                        point.Position = new SKPoint(X - Radius, Y);
                        DrawConnectionPoint(canvas, paint, point);
                    }
                    foreach (var point in OutConnectionPoints)
                    {
                        // Position connection point in the middle of right side
                        point.Position = new SKPoint(X + Radius, Y);
                        DrawConnectionPoint(canvas, paint, point);
                    }
                    break;

                case ComponentShape.Triangle:
                    foreach (var point in InConnectionPoints)
                    {
                        // Position connection point at bottom vertex
                        point.Position = new SKPoint(X, Y + Radius);
                        DrawConnectionPoint(canvas, paint, point);
                    }
                    foreach (var point in OutConnectionPoints)
                    {
                        // Position connection point at top vertex
                        point.Position = new SKPoint(X, Y - Radius);
                        DrawConnectionPoint(canvas, paint, point);
                    }
                    break;

                default:
                    // Handle unknown shape
                    break;
            }


        }
        private void DrawConnectionPoint(SKCanvas canvas, SKPaint paint, IConnectionPoint point)
        {
            SKRect bounds = new SKRect(
                point.Position.X - point.Radius / 4,
                point.Position.Y - point.Radius / 4,
                point.Position.X + point.Radius / 4,
                point.Position.Y + point.Radius / 4);
            point.Bounds = bounds;
            canvas.DrawCircle(point.Position, point.Radius / 4, paint);
        }
       public void Animate()
        {
            if (IsRunning)
            {
                // Rotate the component by 5 degrees on each animation frame
                _rotationAngle += 5;
                if (_rotationAngle >= 360)
                {
                    _rotationAngle = 0;
                }

                // Move the component slightly up and down on each animation frame
                var amplitude = Radius / 5; // adjust this value to change the amplitude of the animation
                var frequency = 10; // adjust this value to change the frequency of the animation
                var offsetY = amplitude * Math.Sin(_rotationAngle * Math.PI / 180 * frequency);
                Y += (float)offsetY;

                // Invalidate the component's bounds to trigger a redraw
                var bounds = Bounds;
                bounds.Inflate(Radius, Radius); // add some padding to ensure the component is fully redrawn
                OnBoundsChanged(bounds);
            }
        }
        protected virtual void OnBoundsChanged(SKRect bounds)
        {
            BoundsChanged?.Invoke(this, new SKRectEventArgs(bounds));
        }
        public bool HitTest(SKPoint point)
        {
            var distance = SKPoint.Distance(new SKPoint(X, Y), point);
            return distance <= Radius;
        }
        public void Move(SKPoint offset)
        {
            // Update the position of the component
            X += offset.X;
            Y += offset.Y;

            // Update the position of all connection points
            foreach (var point in InConnectionPoints.Concat(OutConnectionPoints))
            {
                point.Position += offset;
                point.Rect = new SKRect(point.Position.X - point.Radius, point.Position.Y - point.Radius, point.Position.X + point.Radius, point.Position.Y + point.Radius);
            }

            // Raise the BoundsChanged event to trigger a redraw of the affected area
            var bounds = Bounds;
            bounds.Inflate(Radius, Radius); // add some padding to ensure the component is fully redrawn
            OnBoundsChanged(bounds);
        }
        public bool Click(SKPoint point)
        {
            // Test if the point is inside the bounds of the component
            if (HitTest(point))
            {
                // Raise the Clicked event
                Clicked?.Invoke(this, EventArgs.Empty);
                return true;
            }
            else
            {
                return false;
            }
        }
        public void ConnectTo(ISkiaWorkFlowComponent target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target), "Target component cannot be null.");
            }

            // Check if this component is already connected to another component
            if (EndComponent != null)
            {
                throw new InvalidOperationException("This component is already connected to another component.");
            }

            // Find an available input connection point on the target component
            var inputConnectionPoint = target.InConnectionPoints.FirstOrDefault(p => p.IsAvailable);
            if (inputConnectionPoint == null)
            {
                throw new InvalidOperationException("Target component has no available input connection points.");
            }

            // Mark the input connection point as unavailable
            inputConnectionPoint.IsAvailable = false;

            // Update the end component of this component to the target component
            EndComponent = target;

            // Find an available output connection point on this component
            var outputConnectionPoint = OutConnectionPoints.FirstOrDefault(p => p.IsAvailable);
            if (outputConnectionPoint == null)
            {
                throw new InvalidOperationException("This component has no available output connection points.");
            }

            // Mark the output connection point as unavailable
            outputConnectionPoint.IsAvailable = false;

            // Mark the target input connection point as unavailable
            target.InConnectionPoints[inputConnectionPoint.Index].IsAvailable = false;

            // Raise the Connected event
            Connected?.Invoke(this, new ConnectionEventArgs(outputConnectionPoint, inputConnectionPoint));
        }
        public void DisconnectFrom(ISkiaWorkFlowComponent target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target), "Target component cannot be null.");
            }

            // Check if this component is connected to the specified target component
            if (EndComponent != target)
            {
                throw new InvalidOperationException("This component is not connected to the specified target component.");
            }

            // Find the output connection point that is connected to the specified target component
            var outputConnectionPoint = OutConnectionPoints.FirstOrDefault(p => !p.IsAvailable && p.Connection.Component == target);
            if (outputConnectionPoint == null)
            {
                throw new InvalidOperationException("This component does not have an active connection to the specified target component.");
            }

            // Find the input connection point on the target component that is connected to the output connection point
            var inputConnectionPoint = target.InConnectionPoints[outputConnectionPoint.Connection.Index];

            // Mark the output and input connection points as available
            outputConnectionPoint.IsAvailable = true;
            inputConnectionPoint.IsAvailable = true;

            // Clear the end component of this component
            EndComponent = null;

            // Raise the Disconnected event
            Disconnected?.Invoke(this, new ConnectionEventArgs(outputConnectionPoint, inputConnectionPoint));
        }
        public bool IsConnectedTo(ISkiaWorkFlowComponent otherComponent)
        {
            // Check if the specified component is connected to this component
            if (EndComponent == otherComponent)
            {
                return true;
            }

            // Check if any of the connected components of this component is the specified component
            if (ConnectedComponents != null && ConnectedComponents.Contains(otherComponent))
            {
                return true;
            }

            // The specified component is not connected to this component
            return false;
        }
        public bool Intersects(ISkiaWorkFlowComponent other)
        {
            if (other is SkiaWorkFlowComponents circle)
            {
                // Calculate the distance between the centers of the two circles
                var distance = SKPoint.Distance(new SKPoint(X, Y), new SKPoint(circle.X, circle.Y));

                // Calculate the sum of the radii of the two circles
                var radiusSum = Radius + circle.Radius;

                // Check if the distance is less than or equal to the sum of the radii
                return distance <= radiusSum;
            }
            else if (other is SkiaWorkFlowComponents square)
            {
                // Calculate the center of the square
                var squareCenter = new SKPoint(square.X, square.Y);

                // Calculate the distance between the centers of the two shapes
                var distance = SKPoint.Distance(new SKPoint(X, Y), squareCenter);

                // Calculate the sum of the half-diagonal of the square and the radius of this circle
                var halfDiagonal = (float)Math.Sqrt(2) * square.Radius / 2;
                var radiusSum = Radius + halfDiagonal;

                // Check if the distance is less than or equal to the sum of the half-diagonal and the radius
                return distance <= radiusSum;
            }
            else if (other is SkiaWorkFlowComponents triangle)
            {
                // Calculate the center of the triangle
                var triangleCenter = new SKPoint(triangle.X, triangle.Y);

                // Calculate the distance between the centers of the two shapes
                var distance = SKPoint.Distance(new SKPoint(X, Y), triangleCenter);

                // Calculate the sum of the radius of this circle and the distance from its center to the nearest edge of the triangle
                var distanceToEdge = GetDistanceToTriangleEdge(triangleCenter, triangle.Radius);
                var radiusSum = Radius + distanceToEdge;

                // Check if the distance is less than or equal to the sum of the radius and the distance to the nearest edge of the triangle
                return distance <= radiusSum;
            }

            // The other component is not a circle, square, or triangle, so it cannot intersect with this component
            return false;
        }
        private float GetDistanceToTriangleEdge(SKPoint triangleCenter, float triangleRadius)
        {
            // Calculate the vertices of the equilateral triangle
            var topVertex = new SKPoint(triangleCenter.X, triangleCenter.Y - triangleRadius);
            var leftVertex = new SKPoint(triangleCenter.X - triangleRadius * (float)Math.Cos(Math.PI / 6), triangleCenter.Y + triangleRadius / 2);
            var rightVertex = new SKPoint(triangleCenter.X + triangleRadius * (float)Math.Cos(Math.PI / 6), triangleCenter.Y + triangleRadius / 2);

            // Calculate the distance from the center of the circle to each edge of the triangle
            var distanceToTopEdge = GetDistanceToLineSegment(new SKPoint(X, Y), topVertex, leftVertex);
            var distanceToLeftEdge = GetDistanceToLineSegment(new SKPoint(X, Y), leftVertex, rightVertex);
            var distanceToRightEdge = GetDistanceToLineSegment(new SKPoint(X, Y), rightVertex, topVertex);

            // Return the minimum distance
            return Math.Min(distanceToTopEdge, Math.Min(distanceToLeftEdge, distanceToRightEdge));
        }
        private float GetDistanceToLineSegment(SKPoint start, SKPoint end, SKPoint point)
        {
            // Calculate the direction vector from the start point to the end point
            var direction = end - start;

            // Calculate the distance between the start and end points
            var distance = SKPoint.Distance(start, end);

            // Calculate the dot product of the direction vector and the vector from the start point to the test point
            var dotProduct = direction.X * (point.X - start.X) + direction.Y * (point.Y - start.Y);

            if (dotProduct < 0)
            {
                // The test point is closest to the start point
                return SKPoint.Distance(point, start);
            }
            else if (dotProduct > distance * distance)
            {
                // The test point is closest to the end point
                return SKPoint.Distance(point, end);
            }
            else
            {
                // The test point is closest to the line segment
                var projection = new SKPoint(start.X + direction.X * dotProduct / (distance * distance), start.Y + direction.Y * dotProduct / (distance * distance));
                return SKPoint.Distance(point, projection);
            }
        }
        public void MouseDown(SKPoint point)
        {
            if (HitTest(point))
            {
                _isDragging = true;
                _dragOffset = point - new SKPoint(X, Y);
            }
        }
        public void MouseUp(SKPoint point)
        {
            _isDragging = false;
        }
        public void MouseMove(SKPoint point)
        {
            if (_isDragging)
            {
                Move(point - _dragOffset);
            }
        }
        public SKRect Bounds
        {
            get
            {
                return new SKRect(X - Radius, Y - Radius, X + Radius, Y + Radius);
            }
           
        }
    }

}
