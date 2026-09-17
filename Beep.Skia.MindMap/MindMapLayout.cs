using System;
using System.Collections.Generic;
using System.Linq;
using Beep.Skia.Layout;
using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.MindMap
{
    /// <summary>
    /// Radial mind-map auto-layout. The central node is placed at <see cref="Center"/>;
    /// level-1 topics are distributed around it, and deeper levels fan out within each
    /// parent's angular sector. Components not connected to the tree are placed in a row below.
    /// </summary>
    public class MindMapLayout : IAutoLayout
    {
        /// <summary>Distance from the central node to level-1 topics.</summary>
        public float LevelSpacing { get; set; } = 200f;

        /// <summary>Distance from a parent to its child for levels deeper than one.</summary>
        public float SubLevelSpacing { get; set; } = 160f;

        /// <summary>Center point of the radial arrangement.</summary>
        public SKPoint Center { get; set; } = new SKPoint(420, 320);

        /// <summary>Vertical gap used when parking unconnected components.</summary>
        public float UnconnectedSpacing { get; set; } = 90f;

        public void Arrange(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines)
        {
            if (components == null) return;

            var nodes = components
                .Where(c => c is MindMapControl && !c.IsStatic && c.IsVisible)
                .ToList();
            if (nodes.Count == 0) return;

            var children = nodes.ToDictionary(n => n, _ => new List<SkiaComponent>());
            var hasParent = new HashSet<SkiaComponent>();

            foreach (var line in lines ?? Array.Empty<IConnectionLine>())
            {
                if (!(line?.Start?.Component is SkiaComponent parent)) continue;
                if (!(line?.End?.Component is SkiaComponent child)) continue;
                if (ReferenceEquals(parent, child)) continue;
                if (!children.ContainsKey(parent) || !children.ContainsKey(child)) continue;

                children[parent].Add(child);
                hasParent.Add(child);
            }

            var central = nodes.OfType<CentralNode>().FirstOrDefault() ?? nodes[0];

            central.X = Center.X - central.Width / 2f;
            central.Y = Center.Y - central.Height / 2f;

            var placed = new HashSet<SkiaComponent> { central };
            PlaceChildren(central, children, placed, depth: 0, parentAngle: -Math.PI / 2, sector: 2 * Math.PI);

            // Park unconnected nodes in a row under the center.
            var unconnected = nodes.Where(n => !placed.Contains(n) && !hasParent.Contains(n)).ToList();
            float y = Center.Y + LevelSpacing + UnconnectedSpacing;
            float totalWidth = unconnected.Sum(n => n.Width + 20f);
            float x = Center.X - totalWidth / 2f;
            foreach (var node in unconnected)
            {
                node.X = x;
                node.Y = y;
                x += node.Width + 20f;
            }
        }

        private void PlaceChildren(
            SkiaComponent parent,
            Dictionary<SkiaComponent, List<SkiaComponent>> children,
            HashSet<SkiaComponent> placed,
            int depth,
            double parentAngle,
            double sector)
        {
            var kids = children[parent]
                .Where(k => !placed.Contains(k))
                .Distinct()
                .ToList();
            if (kids.Count == 0) return;

            float distance = depth == 0 ? LevelSpacing : SubLevelSpacing;

            if (depth == 0)
            {
                // Level 1 spreads around the full circle.
                double step = 2 * Math.PI / kids.Count;
                for (int i = 0; i < kids.Count; i++)
                {
                    double angle = parentAngle + step * i;
                    PlaceNode(parent, kids[i], angle, distance);
                    placed.Add(kids[i]);
                    PlaceChildren(kids[i], children, placed, depth + 1, angle, step);
                }
                return;
            }

            // Deeper levels fan out inside the parent's sector.
            double spread = Math.Min(sector, Math.PI / 2);
            double start = parentAngle - spread / 2;
            double childStep = kids.Count > 1 ? spread / (kids.Count - 1) : 0;

            for (int i = 0; i < kids.Count; i++)
            {
                double angle = kids.Count > 1 ? start + childStep * i : parentAngle;
                PlaceNode(parent, kids[i], angle, distance);
                placed.Add(kids[i]);
                PlaceChildren(kids[i], children, placed, depth + 1, angle, Math.Max(Math.PI / 6, spread / Math.Max(1, kids.Count)));
            }
        }

        private static void PlaceNode(SkiaComponent parent, SkiaComponent child, double angle, float distance)
        {
            float parentCenterX = parent.X + parent.Width / 2f;
            float parentCenterY = parent.Y + parent.Height / 2f;

            float centerX = parentCenterX + (float)(Math.Cos(angle) * distance);
            float centerY = parentCenterY + (float)(Math.Sin(angle) * distance);

            child.X = centerX - child.Width / 2f;
            child.Y = centerY - child.Height / 2f;
        }
    }
}
