using System.Collections.Generic;
using System.Linq;
using Beep.Skia;
using Beep.Skia.Model;

namespace Beep.Skia.MindMap
{
    /// <summary>
    /// Applies collapse/expand visibility across a mind map: descendants of collapsed nodes
    /// (and their connecting lines) are hidden; everything else is restored to visible.
    /// </summary>
    public static class MindMapVisibility
    {
        /// <summary>
        /// Recomputes node and line visibility from <see cref="MindMapControl.IsCollapsed"/>.
        /// </summary>
        /// <returns>The number of components/lines whose visibility changed.</returns>
        public static int Apply(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines)
        {
            if (components == null) return 0;

            var nodes = components
                .Where(c => c is MindMapControl && !c.IsStatic)
                .ToList();
            if (nodes.Count == 0) return 0;

            var children = nodes.ToDictionary(n => n, _ => new List<SkiaComponent>());
            foreach (var line in lines ?? Enumerable.Empty<IConnectionLine>())
            {
                if (!(line?.Start?.Component is SkiaComponent parent)) continue;
                if (!(line?.End?.Component is SkiaComponent child)) continue;
                if (children.TryGetValue(parent, out var list) && children.ContainsKey(child))
                    list.Add(child);
            }

            var hidden = new HashSet<SkiaComponent>();
            void HideDescendants(SkiaComponent parent)
            {
                if (!children.TryGetValue(parent, out var kids)) return;
                foreach (var child in kids)
                {
                    if (hidden.Add(child)) HideDescendants(child);
                }
            }

            foreach (var collapsed in nodes.OfType<MindMapControl>().Where(n => n.IsCollapsed))
            {
                HideDescendants(collapsed);
            }

            int changes = 0;
            foreach (var node in nodes)
            {
                bool visible = !hidden.Contains(node);
                if (node.IsVisible != visible)
                {
                    node.IsVisible = visible;
                    changes++;
                }
            }

            foreach (var line in lines ?? Enumerable.Empty<IConnectionLine>())
            {
                if (!(line is ConnectionLine concrete)) continue;

                var source = line.Start?.Component as SkiaComponent;
                var target = line.End?.Component as SkiaComponent;
                bool visible = (source == null || source.IsVisible) && (target == null || target.IsVisible);

                if (concrete.IsVisible != visible)
                {
                    concrete.IsVisible = visible;
                    changes++;
                }
            }

            return changes;
        }
    }
}
