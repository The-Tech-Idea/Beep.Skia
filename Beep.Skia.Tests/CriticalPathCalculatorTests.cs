using System.Linq;
using Beep.Skia;
using Beep.Skia.PM;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for <see cref="CriticalPathCalculator"/>, including DependencyNode lag consumption.
    /// </summary>
    public class CriticalPathCalculatorTests
    {
        private static DrawingManager BuildChain(int aDays, int bDays, int cDays, out TaskNode a, out TaskNode b, out TaskNode c)
        {
            var manager = new DrawingManager();
            a = new TaskNode { X = 0, Y = 0, Title = "A", DurationDays = aDays };
            b = new TaskNode { X = 0, Y = 100, Title = "B", DurationDays = bDays };
            c = new TaskNode { X = 0, Y = 200, Title = "C", DurationDays = cDays };
            manager.AddComponent(a);
            manager.AddComponent(b);
            manager.AddComponent(c);
            manager.ConnectComponents(a, b, 0, 0);
            manager.ConnectComponents(b, c, 0, 0);
            return manager;
        }

        [Fact]
        public void Chain_ComputesProjectDurationWithoutLag()
        {
            var manager = BuildChain(3, 4, 2, out var a, out var b, out var c);
            var calc = new CriticalPathCalculator();
            calc.Calculate(manager.GetComponents(), manager.GetLines());

            // 3 + 4 + 2 = 9
            Assert.Equal(9, calc.ProjectDuration);
            Assert.Equal(3, calc.CriticalPath.Count);
        }

        [Fact]
        public void DependencyNode_LagDelaysSuccessor()
        {
            var manager = BuildChain(3, 4, 2, out var a, out var b, out var c);

            var dependency = new DependencyNode { X = 300, Y = 100, FromTask = "A", ToTask = "B", LagDays = 5 };
            manager.AddComponent(dependency);

            var calc = new CriticalPathCalculator();
            calc.Calculate(manager.GetComponents(), manager.GetLines());

            // A finishes day 3; B starts 3 + 1 + 5 = 9, finishes 12; C starts 13, finishes 14.
            Assert.Equal(14, calc.ProjectDuration);
        }

        [Fact]
        public void LineLabelLag_IsUsedWhenNoDependencyNode()
        {
            var manager = BuildChain(3, 4, 2, out var a, out var b, out var c);
            manager.GetLines().First().Label1 = "5";

            var calc = new CriticalPathCalculator();
            calc.Calculate(manager.GetComponents(), manager.GetLines());

            Assert.Equal(14, calc.ProjectDuration);
        }

        [Fact]
        public void NegativeLag_LeadsSuccessor()
        {
            var manager = BuildChain(5, 4, 1, out var a, out var b, out var c);
            var dependency = new DependencyNode { X = 300, Y = 100, FromTask = "A", ToTask = "B", LagDays = -2 };
            manager.AddComponent(dependency);

            var calc = new CriticalPathCalculator();
            calc.Calculate(manager.GetComponents(), manager.GetLines());

            // A finishes 5; B starts 5 + 1 - 2 = 4, finishes 7; C starts 8, finishes 8.
            Assert.Equal(8, calc.ProjectDuration);
        }
    }
}
