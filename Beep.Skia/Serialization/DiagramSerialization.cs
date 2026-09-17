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

        public List<ComponentDto> Components { get; set; } = new List<ComponentDto>();
        public List<LineDto> Lines { get; set; } = new List<LineDto>();
    }

    public class ComponentDto
    {
        public string Type { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public string Name { get; set; }
        // Optional property bag for lightweight values
        public Dictionary<string, string> PropertyBag { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // Typed property bag for complex values (lists, dictionaries, nested objects) that do not survive string round-trips
        public Dictionary<string, JsonElement> TypedPropertyBag { get; set; } = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        // Optional persisted connection point IDs for deterministic identity across sessions
        public List<Guid> InPointIds { get; set; } = new List<Guid>();
        public List<Guid> OutPointIds { get; set; } = new List<Guid>();
    }

    public class LineDto
    {
        public Guid StartPointId { get; set; }
        public Guid EndPointId { get; set; }

        /// <summary>
        /// Optional component indices used by templates (before GUIDs exist).
        /// When both are >= 0, template loading connects these components directly.
        /// </summary>
        public int StartComponentIndex { get; set; } = -1;
        public int EndComponentIndex { get; set; } = -1;
        // Optional style/label fields
        public bool ShowStartArrow { get; set; } = true;
        public bool ShowEndArrow { get; set; } = true;
        public string Label1 { get; set; }
        public string Label2 { get; set; }
        public string Label3 { get; set; }
        public string DataTypeLabel { get; set; }

        // State-machine transition semantics
        public string GuardCondition { get; set; }
        public string TransitionAction { get; set; }
        public string TriggerEvent { get; set; }
        public uint LineColor { get; set; } // SKColor as RGBA uint

        // Extended style and behavior properties
        public int RoutingMode { get; set; } // Beep.Skia.Model.LineRoutingMode
        public int FlowDirection { get; set; } // Beep.Skia.Model.DataFlowDirection

        // Label placement persistence
        public int Label1Placement { get; set; } // Beep.Skia.Model.LabelPlacement
        public int Label2Placement { get; set; }
        public int Label3Placement { get; set; }
        public int DataLabelPlacement { get; set; }

        // Arrow and stroke styling
        public float ArrowSize { get; set; }
        public float[] DashPattern { get; set; }

        // Status indicator
        public bool ShowStatusIndicator { get; set; }
        public int Status { get; set; } // Beep.Skia.Model.LineStatus
        public uint StatusColor { get; set; }

        // Animation and data flow visuals (specific to ConnectionLine implementation)
        public bool IsAnimated { get; set; }
        public bool IsDataFlowAnimated { get; set; }
        public float DataFlowSpeed { get; set; }
        public float DataFlowParticleSize { get; set; }
        public uint DataFlowColor { get; set; }

        // ERD multiplicity markers
        public int StartMultiplicity { get; set; } // Beep.Skia.Model.ERDMultiplicity
        public int EndMultiplicity { get; set; }   // Beep.Skia.Model.ERDMultiplicity

        // Schema persistence
        public string SchemaJson { get; set; }
        public string ExpectedSchemaJson { get; set; }
    }
}
