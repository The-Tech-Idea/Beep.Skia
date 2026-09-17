using System;
using System.Linq;
using Beep.Skia;
using Beep.Skia.MindMap;
using SkiaSharp;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for <see cref="MindMapLayout"/> radial arrangement.
    /// </summary>
    public class MindMapLayoutTests
    {
        private static float Distance(SkiaComponent a, SkiaComponent b)
        {
            float ax = a.X + a.Width / 2f;
            float ay = a.Y + a.Height / 2f;
            float bx = b.X + b.Width / 2f;
            float by = b.Y + b.Height / 2f;
            return (float)Math.Sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by));
        }

        [Fact]
        public void Arrange_PlacesTopicsAroundCentralAtLevelSpacing()
        {
            var manager = new DrawingManager();
            var central = new CentralNode { X = 0, Y = 0, Width = 140, Height = 60, Name = "central" };
            var t1 = new TopicNode { Width = 100, Height = 40, Name = "t1" };
            var t2 = new TopicNode { Width = 100, Height = 40, Name = "t2" };
            var t3 = new TopicNode { Width = 100, Height = 40, Name = "t3" };
            foreach (var c in new SkiaComponent[] { central, t1, t2, t3 }) manager.AddComponent(c);
            manager.ConnectComponents(central, t1, 0, 0);
            manager.ConnectComponents(central, t2, 0, 0);
            manager.ConnectComponents(central, t3, 0, 0);

            var center = new SKPoint(400, 300);
            manager.ArrangeDiagram(new MindMapLayout { Center = center, LevelSpacing = 200f });

            Assert.Equal(center.X, central.X + central.Width / 2f, 1);
            Assert.Equal(center.Y, central.Y + central.Height / 2f, 1);

            foreach (var topic in new[] { t1, t2, t3 })
            {
                Assert.Equal(200f, Distance(central, topic), 0);
            }

            // Topics must not overlap each other.
            Assert.True(Distance(t1, t2) > 50f);
            Assert.True(Distance(t2, t3) > 50f);
        }

        [Fact]
        public void Arrange_PlacesSubTopicsRelativeToParent()
        {
            var manager = new DrawingManager();
            var central = new CentralNode { X = 0, Y = 0, Width = 140, Height = 60 };
            var topic = new TopicNode { Width = 100, Height = 40 };
            var sub = new SubTopicNode { Width = 90, Height = 36 };
            foreach (var c in new SkiaComponent[] { central, topic, sub }) manager.AddComponent(c);
            manager.ConnectComponents(central, topic, 0, 0);
            manager.ConnectComponents(topic, sub, 0, 0);

            manager.ArrangeDiagram(new MindMapLayout
            {
                Center = new SKPoint(400, 300),
                LevelSpacing = 200f,
                SubLevelSpacing = 150f
            });

            Assert.Equal(150f, Distance(topic, sub), 0);
        }

        [Fact]
        public void Arrange_ParksUnconnectedNodesBelow()
        {
            var manager = new DrawingManager();
            var central = new CentralNode { X = 0, Y = 0, Width = 140, Height = 60 };
            var orphan = new TopicNode { Width = 100, Height = 40, Name = "orphan" };
            manager.AddComponent(central);
            manager.AddComponent(orphan);

            var center = new SKPoint(400, 300);
            manager.ArrangeDiagram(new MindMapLayout { Center = center, LevelSpacing = 200f });

            Assert.True(orphan.Y > center.Y + 200f);
        }

        [Fact]
        public void Arrange_IsDeterministic()
        {
            var manager = new DrawingManager();
            var central = new CentralNode { X = 0, Y = 0, Width = 140, Height = 60 };
            var t1 = new TopicNode { Width = 100, Height = 40 };
            var t2 = new TopicNode { Width = 100, Height = 40 };
            foreach (var c in new SkiaComponent[] { central, t1, t2 }) manager.AddComponent(c);
            manager.ConnectComponents(central, t1, 0, 0);
            manager.ConnectComponents(central, t2, 0, 0);

            var layout = new MindMapLayout();
            manager.ArrangeDiagram(layout);
            var first = new[] { (t1.X, t1.Y), (t2.X, t2.Y) };

            manager.ArrangeDiagram(layout);
            var second = new[] { (t1.X, t1.Y), (t2.X, t2.Y) };

            Assert.Equal(first, second);
        }
    }
}
