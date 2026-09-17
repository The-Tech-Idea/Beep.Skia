using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using Beep.Skia.Model;

namespace Beep.Skia.Components
{
    /// <summary>
    /// A swimlane / pool / lane container for diagramming.
    /// Supports horizontal (row) and vertical (column) layouts with resizable header areas.
    /// Components added as children are automatically positioned within their assigned lane.
    /// </summary>
    public class SwimlaneContainer : MaterialControl
    {
        private readonly List<Swimlane> _lanes = new List<Swimlane>();
        private float _headerSize = 32f;
        private bool _orientationHorizontal = true;
        private SKRect _lastBounds;

        /// <summary>
        /// Gets or sets the orientation of the lanes. True = horizontal rows, false = vertical columns.
        /// </summary>
        public bool OrientationHorizontal
        {
            get => _orientationHorizontal;
            set { _orientationHorizontal = value; MarkPortsDirty(); }
        }

        /// <summary>
        /// Gets or sets the header size (height for horizontal, width for vertical).
        /// </summary>
        public float HeaderSize
        {
            get => _headerSize;
            set { _headerSize = Math.Max(20f, value); MarkPortsDirty(); }
        }

        /// <summary>
        /// Gets the list of lanes in this container.
        /// </summary>
        public IReadOnlyList<Swimlane> Lanes => _lanes;

        /// <summary>
        /// Gets or sets whether lane headers are collapsible.
        /// </summary>
        public bool AllowCollapse { get; set; } = true;

        /// <summary>
        /// Gets or sets the separation between lanes in pixels.
        /// </summary>
        public float LaneSpacing { get; set; } = 2f;

        public SwimlaneContainer()
        {
            Name = "SwimlaneContainer";
            Width = 600f;
            Height = 300f;
            _lanes.Add(new Swimlane { Title = "Lane 1" });
            _lanes.Add(new Swimlane { Title = "Lane 2" });
        }

        /// <summary>
        /// Adds a new lane with the given title.
        /// </summary>
        public Swimlane AddLane(string title)
        {
            var lane = new Swimlane { Title = title ?? $"Lane {_lanes.Count + 1}" };
            _lanes.Add(lane);
            MarkPortsDirty();
            return lane;
        }

        /// <summary>
        /// Removes a lane and all its children.
        /// </summary>
        public void RemoveLane(Swimlane lane)
        {
            if (lane == null) return;
            foreach (var child in lane.Children.ToList())
                RemoveChild(child);
            _lanes.Remove(lane);
            MarkPortsDirty();
        }

        /// <summary>
        /// Adds a component to a specific lane.
        /// </summary>
        public void AddToLane(SkiaComponent component, Swimlane lane)
        {
            if (component == null || lane == null) return;
            lane.Children.Add(component);
            AddChild(component);
        }

        /// <summary>
        /// Gets the lane that contains the given point (in component-local coordinates).
        /// </summary>
        public Swimlane GetLaneAtPoint(SKPoint localPoint)
        {
            var layout = CalculateLaneLayout();
            foreach (var kvp in layout)
            {
                if (kvp.Value.Contains(localPoint))
                    return kvp.Key;
            }
            return null;
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            _lastBounds = new SKRect(0, 0, Width, Height);
            var layout = CalculateLaneLayout();

            using var headerPaint = new SKPaint
            {
                Color = MaterialColors.SurfaceContainerHigh,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            using var headerTextPaint = new SKPaint
            {
                Color = MaterialColors.OnSurface,
                IsAntialias = true
            };
            using var headerFont = new SKFont(
                TypefaceCache.Get("Segoe UI", SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
                12f);
            using var borderPaint = new SKPaint
            {
                Color = MaterialColors.Outline,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1f,
                IsAntialias = true
            };
            using var laneBgPaint = new SKPaint
            {
                Color = new SKColor(0xFA, 0xFA, 0xFA),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            foreach (var lane in _lanes)
            {
                if (!layout.TryGetValue(lane, out var rect)) continue;

                // Lane background
                canvas.DrawRect(rect, laneBgPaint);

                // Header area
                SKRect headerRect;
                if (_orientationHorizontal)
                    headerRect = new SKRect(rect.Left, rect.Top, rect.Right, rect.Top + _headerSize);
                else
                    headerRect = new SKRect(rect.Left, rect.Top, rect.Left + _headerSize, rect.Bottom);

                canvas.DrawRect(headerRect, headerPaint);

                // Header text
                float textX, textY;
                if (_orientationHorizontal)
                {
                    textX = headerRect.Left + 8f;
                    textY = headerRect.Top + (_headerSize + headerFont.Size) / 2f - 2f;
                }
                else
                {
                    canvas.Save();
                    canvas.RotateDegrees(-90, headerRect.Left + _headerSize / 2f, headerRect.Top + headerRect.Height / 2f);
                    textX = headerRect.Left + 8f;
                    textY = headerRect.Top + headerRect.Height / 2f + headerFont.Size / 3f;
                }

                canvas.DrawText(lane.Title, textX, textY, SKTextAlign.Left, headerFont, headerTextPaint);

                if (!_orientationHorizontal)
                    canvas.Restore();

                // Lane border
                canvas.DrawRect(rect, borderPaint);
            }
        }

        private Dictionary<Swimlane, SKRect> CalculateLaneLayout()
        {
            var layout = new Dictionary<Swimlane, SKRect>();
            float totalHeight = Height;
            float totalWidth = Width;

            int count = _lanes.Count;
            if (count == 0) return layout;

            for (int i = 0; i < count; i++)
            {
                var lane = _lanes[i];
                if (lane.IsCollapsed)
                    continue;
            }

            var activeLanes = _lanes.Where(l => !l.IsCollapsed).ToList();
            if (activeLanes.Count == 0) return layout;

            if (_orientationHorizontal)
            {
                float laneHeight = (totalHeight - (activeLanes.Count - 1) * LaneSpacing) / activeLanes.Count;
                float y = 0;
                foreach (var lane in _lanes)
                {
                    if (lane.IsCollapsed)
                    {
                        layout[lane] = new SKRect(0, y, totalWidth, y);
                        continue;
                    }
                    var rect = new SKRect(0, y, totalWidth, y + laneHeight);
                    layout[lane] = rect;
                    y += laneHeight + LaneSpacing;
                }
            }
            else
            {
                float laneWidth = (totalWidth - (activeLanes.Count - 1) * LaneSpacing) / activeLanes.Count;
                float x = 0;
                foreach (var lane in _lanes)
                {
                    if (lane.IsCollapsed)
                    {
                        layout[lane] = new SKRect(x, 0, x, totalHeight);
                        continue;
                    }
                    var rect = new SKRect(x, 0, x + laneWidth, totalHeight);
                    layout[lane] = rect;
                    x += laneWidth + LaneSpacing;
                }
            }

            return layout;
        }

        public override Dictionary<string, object> GetProperties(bool includeCommon = true, bool includeNodeProperties = true)
        {
            var props = base.GetProperties(includeCommon, includeNodeProperties);
            props["OrientationHorizontal"] = _orientationHorizontal;
            props["HeaderSize"] = _headerSize;
            props["AllowCollapse"] = AllowCollapse;
            props["LaneSpacing"] = LaneSpacing;
            props["LaneCount"] = _lanes.Count;
            return props;
        }

        public override void SetPropperties(IDictionary<string, object> properties, bool updateNodeProperties = true, bool applyToPublicSetters = true)
        {
            base.SetPropperties(properties, updateNodeProperties, applyToPublicSetters);
            if (properties.TryGetValue("OrientationHorizontal", out var oh) && oh is bool bOh) _orientationHorizontal = bOh;
            if (properties.TryGetValue("HeaderSize", out var hs) && hs != null) _headerSize = Convert.ToSingle(hs);
            if (properties.TryGetValue("AllowCollapse", out var ac) && ac is bool bAc) AllowCollapse = bAc;
            if (properties.TryGetValue("LaneSpacing", out var ls) && ls != null) LaneSpacing = Convert.ToSingle(ls);
        }
    }

    /// <summary>
    /// Represents a single lane (row or column) within a SwimlaneContainer.
    /// </summary>
    public class Swimlane
    {
        /// <summary>
        /// Lane header title displayed in the header bar.
        /// </summary>
        public string Title { get; set; } = "";

        /// <summary>
        /// Whether this lane is currently collapsed.
        /// </summary>
        public bool IsCollapsed { get; set; } = false;

        /// <summary>
        /// Gets the list of components that belong to this lane.
        /// </summary>
        public List<SkiaComponent> Children { get; } = new List<SkiaComponent>();

        /// <summary>
        /// Optional tag for lane identification.
        /// </summary>
        public object Tag { get; set; }

        public override string ToString() => Title ?? "(unnamed)";
    }
}
