using System.Linq;
using Beep.Skia;
using Beep.Skia.StateMachine;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for <see cref="StateMachineValidator"/>: initial/final states, reachability, and transition guards.
    /// </summary>
    public class StateMachineValidatorTests
    {
        [Fact]
        public void MissingInitialState_ReportsWarning()
        {
            var manager = new DrawingManager();
            var state = new StateNode { X = 0, Y = 0, Title = "A" };
            var final = new FinalStateNode { X = 0, Y = 100 };
            manager.AddComponent(state);
            manager.AddComponent(final);
            manager.ConnectComponents(state, final, 0, 0);

            var validator = new StateMachineValidator();
            var valid = validator.Validate(manager.GetComponents(), manager.GetLines());

            Assert.True(valid);
            Assert.Contains(validator.Issues, i => i.Message.Contains("no initial state"));
        }

        [Fact]
        public void UnreachableState_ReportsWarning()
        {
            var manager = new DrawingManager();
            var initial = new InitialStateNode { X = 0, Y = 0 };
            var reachable = new StateNode { X = 0, Y = 100, Title = "reachable" };
            var orphan = new StateNode { X = 300, Y = 100, Title = "orphan" };
            var final = new FinalStateNode { X = 0, Y = 200 };
            foreach (var c in new SkiaComponent[] { initial, reachable, orphan, final }) manager.AddComponent(c);

            manager.ConnectComponents(initial, reachable, 0, 0);
            manager.ConnectComponents(reachable, final, 0, 0);

            var validator = new StateMachineValidator();
            validator.Validate(manager.GetComponents(), manager.GetLines());

            Assert.Contains(validator.Issues, i => i.Message.Contains("'orphan'") && i.Message.Contains("unreachable"));
            Assert.DoesNotContain(validator.Issues, i => i.Message.Contains("'reachable'"));
        }

        [Fact]
        public void UnguardedChoiceTransitions_ReportWarnings()
        {
            var manager = new DrawingManager();
            var initial = new InitialStateNode { X = 0, Y = 0 };
            var choice = new ChoiceNode { X = 0, Y = 100 };
            var yes = new StateNode { X = -100, Y = 200, Title = "yes" };
            var no = new StateNode { X = 100, Y = 200, Title = "no" };
            foreach (var c in new SkiaComponent[] { initial, choice, yes, no }) manager.AddComponent(c);

            manager.ConnectComponents(initial, choice, 0, 0);
            manager.ConnectComponents(choice, yes, 0, 0);
            manager.ConnectComponents(choice, no, 1, 0);

            var validator = new StateMachineValidator();
            validator.Validate(manager.GetComponents(), manager.GetLines());

            var unguarded = validator.Issues.Count(i => i.Message.Contains("without a guard condition"));
            Assert.Equal(2, unguarded);
        }

        [Fact]
        public void GuardedChoiceTransitions_DoNotReportUnguardedWarnings()
        {
            var manager = new DrawingManager();
            var initial = new InitialStateNode { X = 0, Y = 0 };
            var choice = new ChoiceNode { X = 0, Y = 100 };
            var yes = new StateNode { X = -100, Y = 200, Title = "yes" };
            var no = new StateNode { X = 100, Y = 200, Title = "no" };
            foreach (var c in new SkiaComponent[] { initial, choice, yes, no }) manager.AddComponent(c);

            manager.ConnectComponents(initial, choice, 0, 0);
            manager.ConnectComponents(choice, yes, 0, 0);
            manager.ConnectComponents(choice, no, 1, 0);

            foreach (var line in manager.GetLines())
            {
                if (line.Start?.Component == choice)
                {
                    var guard = line.Start.Index == 0 ? "amount > 100" : "amount <= 100";
                    StateMachineValidator.SetTransition(line, "paymentReceived", guard, null);
                }
            }

            var validator = new StateMachineValidator();
            validator.Validate(manager.GetComponents(), manager.GetLines());

            Assert.DoesNotContain(validator.Issues, i => i.Message.Contains("without a guard condition"));
        }

        [Fact]
        public void CompositeState_RegionCount_ClampsAndRoundTrips()
        {
            var node = new CompositeStateNode { X = 0, Y = 0 };
            node.RegionCount = 9;
            Assert.Equal(4, node.RegionCount);
            node.RegionCount = 0;
            Assert.Equal(1, node.RegionCount);

            node.RegionCount = 3;
            var manager = new DrawingManager();
            manager.AddComponent(node);

            var manager2 = new DrawingManager();
            manager2.LoadFromDto(manager.ToDto());

            var loaded = manager2.GetComponents().OfType<CompositeStateNode>().Single();
            Assert.Equal(3, loaded.RegionCount);
        }

        [Fact]
        public void StateNode_Activities_RoundTripThroughSerialization()
        {
            var manager = new DrawingManager();
            var state = new StateNode { X = 0, Y = 0, Title = "Processing" };
            state.EntryAction = "open()";
            state.DoActivity = "process()";
            state.ExitAction = "close()";
            manager.AddComponent(state);

            var dto = manager.ToDto();
            var manager2 = new DrawingManager();
            manager2.LoadFromDto(dto);

            var loaded = manager2.GetComponents().OfType<StateNode>().Single();
            Assert.Equal("open()", loaded.EntryAction);
            Assert.Equal("process()", loaded.DoActivity);
            Assert.Equal("close()", loaded.ExitAction);
        }

        [Fact]
        public void GuardsAndTriggers_RoundTripThroughSerialization()
        {
            var manager = new DrawingManager();
            var a = new StateNode { X = 0, Y = 0, Title = "A" };
            var b = new StateNode { X = 0, Y = 100, Title = "B" };
            manager.AddComponent(a);
            manager.AddComponent(b);
            manager.ConnectComponents(a, b, 0, 0);

            var line = manager.GetLines().Single();
            StateMachineValidator.SetTransition(line, "start", "x > 0", "log()");

            var dto = manager.ToDto();
            var manager2 = new DrawingManager();
            manager2.LoadFromDto(dto);

            var restored = manager2.GetLines().Single();
            Assert.Equal("start", StateMachineValidator.GetTrigger(restored));
            Assert.Equal("x > 0", StateMachineValidator.GetGuard(restored));
            Assert.Equal("log()", (restored as ConnectionLine)?.TransitionAction);
        }
    }
}
