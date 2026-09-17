using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using Beep.Skia.Model;

namespace Beep.Skia.ECAD
{
    /// <summary>
    /// Electrical Rules Checker for ECAD schematic diagrams.
    /// Validates pin compatibility, floating nets, output conflicts, and missing power connections.
    /// </summary>
    public class ElectricalRulesChecker
    {
        public List<ElectricalRuleViolation> Violations { get; } = new List<ElectricalRuleViolation>();

        /// <summary>
        /// Runs all ERC checks on the given components and connections.
        /// </summary>
        public void RunChecks(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines)
        {
            Violations.Clear();
            CheckFloatingNets(components, lines);
            CheckOutputConflicts(components, lines);
            CheckMissingPowerGround(components, lines);
            CheckUnconnectedPorts(components, lines);
            CheckShortCircuits(components, lines);
        }

        /// <summary>
        /// Detects direct power-to-ground connections (short circuits).
        /// </summary>
        private void CheckShortCircuits(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines)
        {
            foreach (var line in lines)
            {
                if (!(line?.Start?.Component is SkiaComponent startComponent)) continue;
                if (!(line?.End?.Component is SkiaComponent endComponent)) continue;

                var startType = ElectricalPinModel.GetPinType(startComponent, line.Start);
                var endType = ElectricalPinModel.GetPinType(endComponent, line.End);

                if (!ElectricalPinModel.IsShortCircuit(startType, endType)) continue;

                Violations.Add(new ElectricalRuleViolation
                {
                    Message = $"Short circuit: '{startComponent.Name}' ({startType}) is directly connected to '{endComponent.Name}' ({endType})",
                    Severity = ElectricalViolationSeverity.Error,
                    Category = "ShortCircuit",
                    Component = startComponent,
                    Line = line,
                    FixSuggestion = "Insert a load (resistor, regulator, or component) between power and ground"
                });
            }
        }

        /// <summary>
        /// Detects nets (sets of connected ports) that have only one or zero endpoints — floating.
        /// </summary>
        private void CheckFloatingNets(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines)
        {
            // Build adjacency: which connection points connect to which others
            var connected = new HashSet<IConnectionPoint>();
            foreach (var line in lines)
            {
                if (line?.Start != null) connected.Add(line.Start);
                if (line?.End != null) connected.Add(line.End);
            }

            foreach (var c in components.Where(c => c is ECADControl))
            {
                var ecad = (ECADControl)c;
                // If a component has active circuitry (IC, OpAmp, Transistor, etc.) but no power pin connected
                // we'll catch that in CheckMissingPowerGround

                foreach (var cp in c.InConnectionPoints.Concat(c.OutConnectionPoints))
                {
                    if (cp == null) continue;
                    if (!connected.Contains(cp))
                    {
                        Violations.Add(new ElectricalRuleViolation
                        {
                            Message = $"Unconnected port on '{c.Name}' — floating net",
                            Component = c,
                            Port = cp,
                            Severity = ElectricalViolationSeverity.Warning,
                            Category = "FloatingNet",
                            FixSuggestion = "Connect this port to another component or a terminal"
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Detects when two output ports are connected together (short circuit / driver conflict).
        /// </summary>
        private void CheckOutputConflicts(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines)
        {
            var outputConnections = new Dictionary<IConnectionPoint, List<IConnectionLine>>();

            foreach (var line in lines)
            {
                if (line?.Start == null || line.End == null) continue;

                // Check if Start is an output port connected to another output port
                var startComp = line.Start.Component as SkiaComponent;
                var endComp = line.End.Component as SkiaComponent;
                if (startComp == null || endComp == null) continue;

                bool startIsOutput = startComp.OutConnectionPoints.Contains(line.Start);
                bool endIsOutput = endComp.OutConnectionPoints.Contains(line.End);

                if (startIsOutput && endIsOutput)
                {
                    Violations.Add(new ElectricalRuleViolation
                    {
                        Message = $"Two output ports connected: '{startComp.Name}' → '{endComp.Name}' (driver conflict)",
                        Component = startComp,
                        Line = line,
                        Severity = ElectricalViolationSeverity.Error,
                        Category = "OutputConflict",
                        FixSuggestion = "Connect output to input, not output to output"
                    });
                }
            }
        }

        /// <summary>
        /// Detects active components (IC, OpAmp, Microcontroller, Transistor) without power or ground connections.
        /// </summary>
        private void CheckMissingPowerGround(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines)
        {
            var activeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ECADICNode", "ECADOpAmpNode", "ECADMicrocontrollerNode",
                "ECADTransistorNode", "ECADLogicGateNode", "ECADMemoryNode",
                "ECADVoltageRegulatorNode", "ECADTransformerNode"
            };

            var powerSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ECADBatteryNode", "ECADPowerSupplyNode", "ECADVoltageRegulatorNode"
            };

            // Check if any active components exist that aren't connected to power
            bool hasActive = components.Any(c => activeTypes.Contains(c.GetType().Name));
            bool hasPower = components.Any(c => powerSources.Contains(c.GetType().Name))
                         || components.Any(c => c is ECADBatteryNode || c is ECADPowerSupplyNode);

            if (hasActive && !hasPower)
            {
                foreach (var c in components.Where(c => activeTypes.Contains(c.GetType().Name)))
                {
                    Violations.Add(new ElectricalRuleViolation
                    {
                        Message = $"Active component '{c.Name}' has no power source in the circuit",
                        Component = c,
                        Severity = ElectricalViolationSeverity.Warning,
                        Category = "MissingPower",
                        FixSuggestion = "Add a battery or power supply and connect it to VCC/VDD pins"
                    });
                }
            }

            // Check for ground
            bool hasGround = components.Any(c => c is ECADGroundNode);
            if (hasActive && !hasGround)
            {
                foreach (var c in components.Where(c => activeTypes.Contains(c.GetType().Name)))
                {
                    Violations.Add(new ElectricalRuleViolation
                    {
                        Message = $"Circuit has active components but no ground reference",
                        Component = c,
                        Severity = ElectricalViolationSeverity.Info,
                        Category = "MissingGround",
                        FixSuggestion = "Add a ground node and connect GND pins"
                    });
                }
                // Only report once
                return;
            }
        }

        /// <summary>
        /// Detects ECAD components whose input ports are all unconnected (un-driven inputs).
        /// </summary>
        private void CheckUnconnectedPorts(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines)
        {
            var connectedPorts = new HashSet<IConnectionPoint>();
            foreach (var line in lines)
            {
                if (line?.Start != null) connectedPorts.Add(line.Start);
                if (line?.End != null) connectedPorts.Add(line.End);
            }

            foreach (var c in components.Where(c => c is ECADControl))
            {
                int unconnectedInputs = c.InConnectionPoints.Count(cp => cp != null && !connectedPorts.Contains(cp));
                int unconnectedOutputs = c.OutConnectionPoints.Count(cp => cp != null && !connectedPorts.Contains(cp));

                if (unconnectedInputs > 0 && unconnectedOutputs > 0)
                {
                    Violations.Add(new ElectricalRuleViolation
                    {
                        Message = $"'{c.Name}' has {unconnectedInputs} unconnected input(s) and {unconnectedOutputs} unconnected output(s)",
                        Component = c,
                        Severity = ElectricalViolationSeverity.Info,
                        Category = "UnconnectedPorts",
                        FixSuggestion = "Connect floating ports or mark them as no-connect"
                    });
                }
                else if (unconnectedInputs > 0 && c.OutConnectionPoints.Count > 0)
                {
                    Violations.Add(new ElectricalRuleViolation
                    {
                        Message = $"'{c.Name}' has {unconnectedInputs} unconnected input(s)",
                        Component = c,
                        Severity = ElectricalViolationSeverity.Warning,
                        Category = "FloatingInputs",
                        FixSuggestion = "Floating inputs may cause undefined behavior — connect or pull-up/down"
                    });
                }
            }
        }
    }

    /// <summary>
    /// Represents a single ERC violation.
    /// </summary>
    public class ElectricalRuleViolation
    {
        public string Message { get; set; } = string.Empty;
        public ElectricalViolationSeverity Severity { get; set; } = ElectricalViolationSeverity.Warning;
        public string Category { get; set; } = string.Empty;
        public string? FixSuggestion { get; set; }
        public SkiaComponent? Component { get; set; }
        public IConnectionPoint? Port { get; set; }
        public IConnectionLine? Line { get; set; }

        public override string ToString() => $"[{Severity}] {Category}: {Message}";
    }

    public enum ElectricalViolationSeverity { Info, Warning, Error }
}
