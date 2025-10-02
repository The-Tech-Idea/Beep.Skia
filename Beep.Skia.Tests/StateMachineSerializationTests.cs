using Xunit;
using Beep.Skia;
using Beep.Skia.StateMachine;
using SkiaSharp;

namespace Beep.Skia.Tests
{
    public class StateMachineSerializationTests
    {
        [Fact]
        public void StateMachine_Nodes_RoundTrip_With_Connections()
        {
            var mgr = new DrawingManager();

            // Create nodes
            var initial = new InitialStateNode { X = 20, Y = 30 };
            var state = new StateNode { X = 180, Y = 24 };
            var final = new FinalStateNode { X = 360, Y = 30 };
            mgr.AddComponent(initial);
            mgr.AddComponent(state);
            mgr.AddComponent(final);

            // Ensure at least 1 output on initial and 1 input on state
            Assert.True(initial.OutConnectionPoints.Count >= 1);
            Assert.True(state.InConnectionPoints.Count >= 1);

            // Connect initial -> state, state -> final
            var l1 = new ConnectionLine(initial.OutConnectionPoints[0], state.InConnectionPoints[0], () => { });
            // Add via ConnectComponents to stay within public API and ensure history/presets
            mgr.ConnectComponents(initial, state);

            // Make sure final has an input
            Assert.True(final.InConnectionPoints.Count >= 1);
            var l2 = new ConnectionLine(state.OutConnectionPoints[0], final.InConnectionPoints[0], () => { });
            mgr.ConnectComponents(state, final);

            // Serialize to DTO
            var dto = mgr.ToDto();
            Assert.NotNull(dto);
            Assert.Equal(3, dto.Components.Count);
            Assert.Equal(2, dto.Lines.Count);

            // Round-trip through JSON to simulate persistence
            var json = System.Text.Json.JsonSerializer.Serialize(dto);
            var dto2 = System.Text.Json.JsonSerializer.Deserialize<Beep.Skia.Serialization.DiagramDto>(json);

            // Load into a new manager
            var mgr2 = new DrawingManager();
            mgr2.LoadFromDto(dto2);

            // Validate nodes exist
            var comps = mgr2.GetComponents();
            Assert.Equal(3, comps.Count);

            // Validate that connection points were restored and lines reconnected
            // We expect two lines after load
            var toDtoAgain = mgr2.ToDto();
            Assert.Equal(2, toDtoAgain.Lines.Count);

            // Sanity: Each line has valid start/end point IDs in registry
            foreach (var line in toDtoAgain.Lines)
            {
                Assert.NotEqual(System.Guid.Empty, line.StartPointId);
                Assert.NotEqual(System.Guid.Empty, line.EndPointId);
            }
        }
    }
}
