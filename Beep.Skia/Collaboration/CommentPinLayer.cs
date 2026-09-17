using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SkiaSharp;

namespace Beep.Skia.Collaboration
{
    /// <summary>
    /// A comment pin anchored to a component in the diagram.
    /// </summary>
    public class CommentPin
    {
        /// <summary>Component this pin is anchored to.</summary>
        public string ComponentId { get; set; } = string.Empty;

        /// <summary>1-based display number.</summary>
        public int Index { get; set; }

        /// <summary>Center of the pin in world coordinates.</summary>
        public SKPoint Position { get; set; }

        /// <summary>Bounds of the anchored component.</summary>
        public SKRect ComponentBounds { get; set; }

        /// <summary>Unresolved comment ids anchored to this component.</summary>
        public List<string> CommentIds { get; } = new List<string>();

        public int CommentCount => CommentIds.Count;

        public bool Contains(SKPoint point, float radius = CommentPinLayer.PinRadius)
        {
            var dx = point.X - Position.X;
            var dy = point.Y - Position.Y;
            return dx * dx + dy * dy <= radius * radius;
        }

        public override string ToString() => $"#{Index} on {ComponentId} ({CommentCount} comment(s))";
    }

    /// <summary>
    /// Computes and renders comment pins for unresolved comments anchored to components.
    /// Pins sit on the top-right corner of their component, like review badges.
    /// </summary>
    public class CommentPinLayer
    {
        public const float PinRadius = 11f;
        public const float PinOffset = 4f;

        private static readonly SKColor DefaultAccent = new SKColor(0xF5, 0x9E, 0x0B);

        private readonly List<CommentPin> _pins = new List<CommentPin>();

        public IReadOnlyList<CommentPin> Pins => _pins.AsReadOnly();

        /// <summary>
        /// Rebuilds pins from unresolved comments and diagram components.
        /// A comment anchors to a component when <see cref="DiagramComment.ComponentId"/>
        /// matches the component's <see cref="SkiaComponent.Name"/> (case-insensitive) or id.
        /// </summary>
        public void Rebuild(IEnumerable<SkiaComponent> components, IEnumerable<DiagramComment> comments, int maxPins = 100)
        {
            _pins.Clear();
            if (components == null || comments == null) return;

            var componentList = components.ToList();
            var byName = new Dictionary<string, SkiaComponent>(StringComparer.OrdinalIgnoreCase);
            foreach (var component in componentList)
            {
                if (component == null) continue;
                if (!string.IsNullOrWhiteSpace(component.Name)) byName[component.Name] = component;
                byName[component.Id.ToString()] = component;
            }

            var grouped = new Dictionary<SkiaComponent, List<string>>();
            foreach (var comment in comments)
            {
                if (comment == null || comment.Resolved) continue;
                if (string.IsNullOrWhiteSpace(comment.ComponentId)) continue;
                if (!byName.TryGetValue(comment.ComponentId.Trim(), out var component)) continue;

                if (!grouped.TryGetValue(component, out var ids))
                {
                    ids = new List<string>();
                    grouped[component] = ids;
                }
                ids.Add(comment.Id);
            }

            foreach (var pair in grouped.Take(Math.Max(0, maxPins)))
            {
                var bounds = pair.Key.Bounds;
                var pin = new CommentPin
                {
                    ComponentId = pair.Key.Name ?? pair.Key.Id.ToString(),
                    Index = _pins.Count + 1,
                    ComponentBounds = bounds,
                    Position = new SKPoint(bounds.Right + PinOffset, bounds.Top - PinOffset)
                };
                pin.CommentIds.AddRange(pair.Value);
                _pins.Add(pin);
            }
        }

        /// <summary>Returns the pin under a world-space point, or null.</summary>
        public CommentPin HitTest(SKPoint point)
            => _pins.LastOrDefault(pin => pin.Contains(point));

        /// <summary>Clears all pins.</summary>
        public void Clear() => _pins.Clear();

        /// <summary>Draws the pins onto the canvas (world-space transform expected).</summary>
        public void Draw(SKCanvas canvas, SKColor? accent = null)
        {
            if (canvas == null || _pins.Count == 0) return;

            var color = accent ?? DefaultAccent;
            using var shadowPaint = new SKPaint
            {
                Color = new SKColor(0, 0, 0, 40),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            using var fillPaint = new SKPaint
            {
                Color = color,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            using var borderPaint = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2f
            };
            using var textPaint = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            using var font = new SKFont { Size = 12f, Embolden = true };

            foreach (var pin in _pins)
            {
                canvas.DrawCircle(pin.Position.X, pin.Position.Y + 1f, PinRadius, shadowPaint);
                canvas.DrawCircle(pin.Position, PinRadius, fillPaint);
                canvas.DrawCircle(pin.Position, PinRadius, borderPaint);

                var label = pin.CommentCount > 1
                    ? pin.CommentCount.ToString(CultureInfo.InvariantCulture)
                    : pin.Index.ToString(CultureInfo.InvariantCulture);

                var metrics = font.Metrics;
                var textY = pin.Position.Y - (metrics.Ascent + metrics.Descent) / 2f;
                canvas.DrawText(label, pin.Position.X, textY, SKTextAlign.Center, font, textPaint);
            }
        }
    }
}
