using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using SkiaSharp;
using Beep.Skia.Model;

namespace Beep.Skia.Serialization
{
    /// <summary>
    /// Serializable DTOs to persist a diagram: components and connection lines.
    /// Keeps per-component basic geometry and type, and connection endpoints by connection point GUIDs.
    /// </summary>
    public class DiagramDto
    {
        /// <summary>
        /// Schema version of the serialized payload. Increment when the DTO shape changes.
        /// </summary>
        public const int CurrentSchemaVersion = 2;

        /// <summary>
        /// Gets or sets the schema version of this payload.
        /// </summary>
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        /// <summary>
        /// Gets or sets the name of the theme active when the diagram was saved.
        /// </summary>
        public string ThemeName { get; set; }

        /// <summary>
        /// Gets or sets the components.
        /// </summary>
        public List<ComponentDto> Components { get; set; } = new List<ComponentDto>();
        /// <summary>
        /// Gets or sets the lines.
        /// </summary>
        public List<LineDto> Lines { get; set; } = new List<LineDto>();
    }

    public class ComponentDto
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        public string Type { get; set; }
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
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }
        // Optional property bag for lightweight values
        public Dictionary<string, string> PropertyBag { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // Typed property bag for complex values (lists, dictionaries, nested objects) that do not survive string round-trips
        public Dictionary<string, JsonElement> TypedPropertyBag { get; set; } = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        // Optional persisted connection point IDs for deterministic identity across sessions
        /// <summary>
        /// Gets or sets the in point ids.
        /// </summary>
        public List<Guid> InPointIds { get; set; } = new List<Guid>();
        /// <summary>
        /// Gets or sets the out point ids.
        /// </summary>
        public List<Guid> OutPointIds { get; set; } = new List<Guid>();
    }

    public class LineDto
    {
        /// <summary>
        /// Gets or sets the start point id.
        /// </summary>
        public Guid StartPointId { get; set; }
        /// <summary>
        /// Gets or sets the end point id.
        /// </summary>
        public Guid EndPointId { get; set; }

        /// <summary>
        /// Optional component indices used by templates (before GUIDs exist).
        /// When both are >= 0, template loading connects these components directly.
        /// </summary>
        public int StartComponentIndex { get; set; } = -1;
        /// <summary>
        /// Gets or sets the end component index.
        /// </summary>
        public int EndComponentIndex { get; set; } = -1;
        // Optional style/label fields
        /// <summary>
        /// Gets or sets the show start arrow.
        /// </summary>
        public bool ShowStartArrow { get; set; } = true;
        /// <summary>
        /// Gets or sets the show end arrow.
        /// </summary>
        public bool ShowEndArrow { get; set; } = true;
        /// <summary>
        /// Gets or sets the label1.
        /// </summary>
        public string Label1 { get; set; }
        /// <summary>
        /// Gets or sets the label2.
        /// </summary>
        public string Label2 { get; set; }
        /// <summary>
        /// Gets or sets the label3.
        /// </summary>
        public string Label3 { get; set; }
        /// <summary>
        /// Gets or sets the data type label.
        /// </summary>
        public string DataTypeLabel { get; set; }

        // State-machine transition semantics
        /// <summary>
        /// Gets or sets the guard condition.
        /// </summary>
        public string GuardCondition { get; set; }
        /// <summary>
        /// Gets or sets the transition action.
        /// </summary>
        public string TransitionAction { get; set; }
        /// <summary>
        /// Gets or sets the trigger event.
        /// </summary>
        public string TriggerEvent { get; set; }
        /// <summary>
        /// Gets or sets the line color.
        /// </summary>
        public uint LineColor { get; set; } // SKColor as RGBA uint

        // Extended style and behavior properties
        /// <summary>
        /// Gets or sets the routing mode.
        /// </summary>
        public int RoutingMode { get; set; } // Beep.Skia.Model.LineRoutingMode
        /// <summary>
        /// Gets or sets the flow direction.
        /// </summary>
        public int FlowDirection { get; set; } // Beep.Skia.Model.DataFlowDirection

        // Label placement persistence
        /// <summary>
        /// Gets or sets the label1 placement.
        /// </summary>
        public int Label1Placement { get; set; } // Beep.Skia.Model.LabelPlacement
        /// <summary>
        /// Gets or sets the label2 placement.
        /// </summary>
        public int Label2Placement { get; set; }
        /// <summary>
        /// Gets or sets the label3 placement.
        /// </summary>
        public int Label3Placement { get; set; }
        /// <summary>
        /// Gets or sets the data label placement.
        /// </summary>
        public int DataLabelPlacement { get; set; }

        // Arrow and stroke styling
        /// <summary>
        /// Gets or sets the arrow size.
        /// </summary>
        public float ArrowSize { get; set; }
        /// <summary>
        /// Gets or sets the dash pattern.
        /// </summary>
        public float[] DashPattern { get; set; }

        // Status indicator
        /// <summary>
        /// Gets or sets the show status indicator.
        /// </summary>
        public bool ShowStatusIndicator { get; set; }
        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        public int Status { get; set; } // Beep.Skia.Model.LineStatus
        /// <summary>
        /// Gets or sets the status color.
        /// </summary>
        public uint StatusColor { get; set; }

        // Animation and data flow visuals (specific to ConnectionLine implementation)
        /// <summary>
        /// Gets or sets the is animated.
        /// </summary>
        public bool IsAnimated { get; set; }
        /// <summary>
        /// Gets or sets the is data flow animated.
        /// </summary>
        public bool IsDataFlowAnimated { get; set; }
        /// <summary>
        /// Gets or sets the data flow speed.
        /// </summary>
        public float DataFlowSpeed { get; set; }
        /// <summary>
        /// Gets or sets the data flow particle size.
        /// </summary>
        public float DataFlowParticleSize { get; set; }
        /// <summary>
        /// Gets or sets the data flow color.
        /// </summary>
        public uint DataFlowColor { get; set; }

        // ERD multiplicity markers
        /// <summary>
        /// Gets or sets the start multiplicity.
        /// </summary>
        public int StartMultiplicity { get; set; } // Beep.Skia.Model.ERDMultiplicity
        /// <summary>
        /// Gets or sets the end multiplicity.
        /// </summary>
        public int EndMultiplicity { get; set; }   // Beep.Skia.Model.ERDMultiplicity

        // Schema persistence
        /// <summary>
        /// Gets or sets the schema json.
        /// </summary>
        public string SchemaJson { get; set; }
        /// <summary>
        /// Gets or sets the expected schema json.
        /// </summary>
        public string ExpectedSchemaJson { get; set; }
    }
}
