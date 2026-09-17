using System;
using System.Collections.Generic;
using System.Linq;

namespace Beep.Skia.Assist
{
    /// <summary>A parsed mind-map node with radial layout geometry.</summary>
    public class MindMapDslNode
    {
        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the depth.
        /// </summary>
        public int Depth { get; set; }
        /// <summary>
        /// Gets or sets the children.
        /// </summary>
        public List<MindMapDslNode> Children { get; } = new List<MindMapDslNode>();
        /// <summary>
        /// Gets or sets the x.
        /// </summary>
        public float X { get; set; }
        /// <summary>
        /// Gets or sets the y.
        /// </summary>
        public float Y { get; set; }
        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        public float Width { get; set; }
        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        public float Height { get; set; }

        /// <summary>Type keyword: central (depth 0), topic (depth 1), subtopic (depth 2+).</summary>
        public string TypeKeyword => Depth == 0 ? "central" : Depth == 1 ? "topic" : "subtopic";

        /// <summary>
        /// Gets or sets the to string.
        /// </summary>
        public override string ToString() => $"{new string(' ', Depth * 2)}{Title}";
    }

    /// <summary>
    /// Parses an indentation-based outline into a mind-map tree and lays it out radially.
    /// Tabs count as four spaces. Blank lines are ignored; lines starting with # or // are comments.
    /// </summary>
    public static class MindMapDslParser
    {
        /// <summary>Parses an outline and applies the radial layout.</summary>
        public static MindMapDslNode Parse(string text, out List<string> warnings, int maxNodes = 60)
        {
            warnings = new List<string>();
            var root = ParseTree(text, warnings, maxNodes);
            if (root != null) Layout(root, warnings);
            return root;
        }

        /// <summary>
        /// Maximum outline nesting depth. The radial layout walks the tree recursively, so deeper
        /// outlines are flattened onto the deepest allowed level instead of overflowing the stack.
        /// </summary>
        public const int MaxOutlineDepth = 100;

        /// <summary>Parses the outline without laying it out.</summary>
        public static MindMapDslNode ParseTree(string text, List<string> warnings = null, int maxNodes = 60)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                warnings?.Add("The description is empty.");
                return null;
            }

            var stack = new List<(int Indent, MindMapDslNode Node)>();
            MindMapDslNode root = null;
            var count = 0;
            var depthLimited = false;

            foreach (var raw in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                var line = raw.TrimEnd();
                if (line.Trim().Length == 0) continue;
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("#") || trimmed.StartsWith("//")) continue;

                var indent = 0;
                foreach (var c in line)
                {
                    if (c == ' ') indent++;
                    else if (c == '\t') indent += 4;
                    else break;
                }

                var title = trimmed.Trim().Trim('"', '\'');
                if (title.Length == 0) continue;

                if (count++ >= Math.Max(1, maxNodes))
                {
                    warnings?.Add($"Node limit of {maxNodes} reached; remaining lines were ignored.");
                    break;
                }

                var node = new MindMapDslNode { Title = title };
                if (root == null)
                {
                    root = node;
                    stack.Add((indent, node));
                    continue;
                }

                while (stack.Count > 1 && indent <= stack[stack.Count - 1].Indent)
                    stack.RemoveAt(stack.Count - 1);

                var parent = stack[stack.Count - 1].Node;

                // Flatten anything deeper than the layout can safely walk: attach the node to the
                // deepest allowed *ancestor* so the tree itself stays shallow (capping only the
                // Depth value would leave a 20k-deep child chain for the recursive layout to walk).
                if (parent.Depth + 1 > MaxOutlineDepth)
                {
                    if (!depthLimited)
                    {
                        warnings?.Add($"Outline is deeper than {MaxOutlineDepth} levels; deeper items were flattened.");
                        depthLimited = true;
                    }

                    var allowedIndex = Math.Min(MaxOutlineDepth, stack.Count - 1);
                    parent = stack[allowedIndex].Node;
                    if (stack.Count > allowedIndex + 1)
                        stack.RemoveRange(allowedIndex + 1, stack.Count - allowedIndex - 1);
                }

                node.Depth = parent.Depth + 1;
                parent.Children.Add(node);
                stack.Add((indent, node));
            }

            if (root == null) warnings?.Add("No content found.");
            return root;
        }

        /// <summary>Assigns radial positions (root at the center, children on expanding arcs).</summary>
        public static void Layout(MindMapDslNode root, List<string> warnings = null)
        {
            if (root == null) return;

            const float centerX = 420f;
            const float centerY = 320f;
            const float levelSpacing = 180f;

            Size(root);
            Place(root, centerX, centerY, levelSpacing, 0f, (float)(Math.PI * 2));

            var seen = new HashSet<MindMapDslNode>();
            var queue = new Queue<MindMapDslNode>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (!seen.Add(node)) continue;
                foreach (var child in node.Children) queue.Enqueue(child);
            }

            var nodes = seen.ToList();

            // The collision scan is quadratic; skip it for very large maps so layout time stays bounded.
            if (nodes.Count <= 2000)
            {
                var collisions = nodes.Count(n => nodes.Any(o =>
                    !ReferenceEquals(n, o) &&
                    Math.Abs(n.X - o.X) < 4f && Math.Abs(n.Y - o.Y) < 4f));
                if (collisions > 0)
                    warnings?.Add("Some nodes share the same position; consider shortening the outline.");
            }
        }

        private static void Size(MindMapDslNode node)
        {
            if (node.Depth == 0) { node.Width = 110f; node.Height = 110f; }
            else if (node.Depth == 1) { node.Width = 140f; node.Height = 50f; }
            else { node.Width = 130f; node.Height = 42f; }

            foreach (var child in node.Children) Size(child);
        }

        private static void Place(MindMapDslNode node, float centerX, float centerY, float levelSpacing, float angleStart, float angleEnd)
        {
            var mid = (angleStart + angleEnd) / 2f;
            var radius = node.Depth * levelSpacing;
            node.X = centerX + (float)Math.Cos(mid) * radius - node.Width / 2f;
            node.Y = centerY + (float)Math.Sin(mid) * radius - node.Height / 2f;

            var count = node.Children.Count;
            if (count == 0) return;

            var sector = angleEnd - angleStart;
            for (var i = 0; i < count; i++)
            {
                Place(node.Children[i], centerX, centerY, levelSpacing,
                    angleStart + sector * i / count,
                    angleStart + sector * (i + 1) / count);
            }
        }
    }
}
