using Beep.Skia;
using Beep.Skia.Model;

namespace Beep.Skia.ECAD
{
    /// <summary>
    /// Electrical role of a connection point (pin) on an ECAD component.
    /// </summary>
    public enum ElectricalPinType
    {
        Input,
        Output,
        Power,
        Ground,
        Passive,
        Bidirectional
    }

    /// <summary>
    /// Infers electrical pin types for ECAD components from their class and port direction.
    /// Used by the electrical rules checker for compatibility and short-circuit analysis.
    /// </summary>
    public static class ElectricalPinModel
    {
        /// <summary>
        /// Gets the electrical pin type for a component port.
        /// </summary>
        public static ElectricalPinType GetPinType(SkiaComponent component, IConnectionPoint port)
        {
            bool isOutput = port?.Type == ConnectionPointType.Out;

            switch (component)
            {
                case ECADGroundNode _:
                    return ElectricalPinType.Ground;

                case ECADPowerSupplyNode _:
                case ECADBatteryNode _:
                    return isOutput ? ElectricalPinType.Power : ElectricalPinType.Passive;

                case ECADVoltageRegulatorNode _:
                    return isOutput ? ElectricalPinType.Power : ElectricalPinType.Input;

                case ECADResistorNode _:
                case ECADCapacitorNode _:
                case ECADInductorNode _:
                case ECADFuseNode _:
                case ECADTransformerNode _:
                case ECADTraceNode _:
                    return ElectricalPinType.Passive;

                case ECADDiodeNode _:
                    return isOutput ? ElectricalPinType.Output : ElectricalPinType.Input;

                case ECADLogicGateNode _:
                case ECADMicrocontrollerNode _:
                case ECADMemoryNode _:
                case ECADICNode _:
                case ECADOpAmpNode _:
                case ECADTransistorNode _:
                    return isOutput ? ElectricalPinType.Output : ElectricalPinType.Input;

                default:
                    return isOutput ? ElectricalPinType.Output : ElectricalPinType.Input;
            }
        }

        /// <summary>
        /// True when the pin supplies power.
        /// </summary>
        public static bool IsPower(ElectricalPinType type) => type == ElectricalPinType.Power;

        /// <summary>
        /// True when the pin is a ground reference.
        /// </summary>
        public static bool IsGround(ElectricalPinType type) => type == ElectricalPinType.Ground;

        /// <summary>
        /// True when a direct connection between the two pin types is a short circuit.
        /// </summary>
        public static bool IsShortCircuit(ElectricalPinType a, ElectricalPinType b)
            => (IsPower(a) && IsGround(b)) || (IsGround(a) && IsPower(b));
    }
}
