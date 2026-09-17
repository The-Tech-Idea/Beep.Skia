using System.Linq;
using Beep.Skia;
using Beep.Skia.PM;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for <see cref="GanttTimelineNode"/> schedule mapping and persistence.
    /// </summary>
    public class GanttTimelineTests
    {
        private static DrawingManager BuildChain(out TaskNode a, out TaskNode b, out TaskNode c)
        {
            var manager = new DrawingManager();
            a = new TaskNode { X = 0, Y = 0, Title = "A", DurationDays = 3, PercentComplete = 50 };
            b = new TaskNode { X = 0, Y = 100, Title = "B", DurationDays = 4 };
            c = new TaskNode { X = 0, Y = 200, Title = "C", DurationDays = 2 };
            manager.AddComponent(a);
            manager.AddComponent(b);
            manager.AddComponent(c);
            manager.ConnectComponents(a, b, 0, 0);
            manager.ConnectComponents(b, c, 0, 0);
            return manager;
        }

        [Fact]
        public void SetSchedule_WithCalculator_MapsDatesAndCriticality()
        {
            var manager = BuildChain(out var a, out var b, out var c);
            var calc = new CriticalPathCalculator();
            calc.Calculate(manager.GetComponents(), manager.GetLines());

            var gantt = new GanttTimelineNode();
            gantt.SetSchedule(manager.GetComponents().OfType<TaskNode>(), calc);

            Assert.Equal(3, gantt.Rows.Count);

            var rowA = gantt.Rows.Single(r => r.Name == "A");
            Assert.Equal(1, rowA.StartDay);
            Assert.Equal(3, rowA.FinishDay);
            Assert.Equal(50f, rowA.PercentComplete);

            var rowC = gantt.Rows.Single(r => r.Name == "C");
            Assert.Equal(8, rowC.StartDay);
            Assert.Equal(9, rowC.FinishDay);

            Assert.All(gantt.Rows, r => Assert.True(r.IsCritical));
        }

        [Fact]
        public void SetSchedule_WithoutCalculator_LaysOutSequentially()
        {
            var manager = BuildChain(out var a, out var b, out var c);
            var gantt = new GanttTimelineNode();
            gantt.SetSchedule(manager.GetComponents().OfType<TaskNode>());

            Assert.Equal(3, gantt.Rows.Count);
            Assert.Equal((1, 3), (gantt.Rows[0].StartDay, gantt.Rows[0].FinishDay));
            Assert.Equal((4, 7), (gantt.Rows[1].StartDay, gantt.Rows[1].FinishDay));
            Assert.Equal((8, 9), (gantt.Rows[2].StartDay, gantt.Rows[2].FinishDay));
        }

        [Fact]
        public void Rows_PersistThroughSerialization()
        {
            var manager = BuildChain(out var a, out var b, out var c);
            var calc = new CriticalPathCalculator();
            calc.Calculate(manager.GetComponents(), manager.GetLines());

            var gantt = new GanttTimelineNode();
            gantt.SetSchedule(manager.GetComponents().OfType<TaskNode>(), calc);
            manager.AddComponent(gantt);

            var dto = manager.ToDto();
            var manager2 = new DrawingManager();
            manager2.LoadFromDto(dto);

            var loaded = manager2.GetComponents().OfType<GanttTimelineNode>().SingleOrDefault();
            Assert.NotNull(loaded);
            Assert.Equal(3, loaded.Rows.Count);
            Assert.Equal("A", loaded.Rows[0].Name);
            Assert.Equal(1, loaded.Rows[0].StartDay);
        }
    }
}
