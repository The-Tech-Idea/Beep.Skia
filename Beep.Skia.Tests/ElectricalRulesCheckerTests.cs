using System.Linq;
using Beep.Skia;
using Beep.Skia.ECAD;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for the ECAD electrical pin model and short-circuit detection.
    /// </summary>
    public class ElectricalRulesCheckerTests
    {
        [Fact]
        public void PowerToGroundDirect_IsShortCircuit()
        {
            var manager = new DrawingManager();
            var power = new ECADPowerSupplyNode { Name = "PSU" };
            var ground = new ECADGroundNode { Name = "GND" };
            manager.AddComponent(power);
            manager.AddComponent(ground);
            manager.ConnectComponents(power, ground, 0, 0);

            var checker = new ElectricalRulesChecker();
            checker.RunChecks(manager.GetComponents(), manager.GetLines());

            Assert.Contains(checker.Violations, v =>
                v.Category == "ShortCircuit" && v.Severity == ElectricalViolationSeverity.Error);
        }

        [Fact]
        public void PowerThroughResistorToGround_IsNotShortCircuit()
        {
            var manager = new DrawingManager();
            var power = new ECADPowerSupplyNode { Name = "PSU" };
            var resistor = new ECADResistorNode { Name = "R1" };
            var ground = new ECADGroundNode { Name = "GND" };
            manager.AddComponent(power);
            manager.AddComponent(resistor);
            manager.AddComponent(ground);
            manager.ConnectComponents(power, resistor, 0, 0);
            manager.ConnectComponents(resistor, ground, 0, 0);

            var checker = new ElectricalRulesChecker();
            checker.RunChecks(manager.GetComponents(), manager.GetLines());

            Assert.DoesNotContain(checker.Violations, v => v.Category == "ShortCircuit");
        }

        [Fact]
        public void PinModel_InfersTypesFromComponentAndDirection()
        {
            var ground = new ECADGroundNode();
            Assert.Equal(ElectricalPinType.Ground, ElectricalPinModel.GetPinType(ground, ground.InConnectionPoints[0]));

            var power = new ECADPowerSupplyNode();
            Assert.Equal(ElectricalPinType.Power, ElectricalPinModel.GetPinType(power, power.OutConnectionPoints[0]));

            var resistor = new ECADResistorNode();
            Assert.Equal(ElectricalPinType.Passive, ElectricalPinModel.GetPinType(resistor, resistor.InConnectionPoints[0]));
            Assert.Equal(ElectricalPinType.Passive, ElectricalPinModel.GetPinType(resistor, resistor.OutConnectionPoints[0]));

            var ic = new ECADICNode();
            Assert.Equal(ElectricalPinType.Input, ElectricalPinModel.GetPinType(ic, ic.InConnectionPoints[0]));
            Assert.Equal(ElectricalPinType.Output, ElectricalPinModel.GetPinType(ic, ic.OutConnectionPoints[0]));
        }

        [Fact]
        public void ShortCircuitHelper_DetectsPowerGroundPairs()
        {
            Assert.True(ElectricalPinModel.IsShortCircuit(ElectricalPinType.Power, ElectricalPinType.Ground));
            Assert.True(ElectricalPinModel.IsShortCircuit(ElectricalPinType.Ground, ElectricalPinType.Power));
            Assert.False(ElectricalPinModel.IsShortCircuit(ElectricalPinType.Power, ElectricalPinType.Passive));
            Assert.False(ElectricalPinModel.IsShortCircuit(ElectricalPinType.Input, ElectricalPinType.Output));
        }

        [Fact]
        public void ExistingChecks_StillReportUnconnectedPorts()
        {
            var manager = new DrawingManager();
            manager.AddComponent(new ECADResistorNode { Name = "R1" });

            var checker = new ElectricalRulesChecker();
            checker.RunChecks(manager.GetComponents(), manager.GetLines());

            Assert.Contains(checker.Violations, v => v.Category == "FloatingNet");
        }
    }
}
