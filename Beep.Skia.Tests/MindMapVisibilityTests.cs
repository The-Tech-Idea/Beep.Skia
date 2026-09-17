using System.Linq;
using Beep.Skia;
using Beep.Skia.MindMap;
using SkiaSharp;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for mind-map collapse/expand visibility and icon persistence.
    /// </summary>
    public class MindMapVisibilityTests
    {
        private static DrawingManager BuildTree(out CentralNode central, out TopicNode topic, out SubTopicNode sub, out TopicNode sibling)
        {
            var manager = new DrawingManager();
            central = new CentralNode { Width = 140, Height = 60, Name = "central" };
            topic = new TopicNode { Width = 100, Height = 40, Name = "topic" };
            sub = new SubTopicNode { Width = 90, Height = 36, Name = "sub" };
            sibling = new TopicNode { Width = 100, Height = 40, Name = "sibling" };
            foreach (var c in new SkiaComponent[] { central, topic, sub, sibling }) manager.AddComponent(c);
            manager.ConnectComponents(central, topic, 0, 0);
            manager.ConnectComponents(topic, sub, 0, 0);
            manager.ConnectComponents(central, sibling, 0, 0);
            return manager;
        }

        [Fact]
        public void Collapse_HidesDescendantsAndTheirLines()
        {
            var manager = BuildTree(out var central, out var topic, out var sub, out var sibling);
            topic.IsCollapsed = true;

            var changes = MindMapVisibility.Apply(manager.GetComponents(), manager.GetLines());

            Assert.True(changes > 0);
            Assert.False(sub.IsVisible);
            Assert.True(topic.IsVisible);
            Assert.True(sibling.IsVisible);
            Assert.True(central.IsVisible);

            var topicToSub = manager.GetLines().Single(l => l.Start?.Component == topic && l.End?.Component == sub);
            Assert.False(((ConnectionLine)topicToSub).IsVisible);

            var centralToTopic = manager.GetLines().Single(l => l.Start?.Component == central && l.End?.Component == topic);
            Assert.True(((ConnectionLine)centralToTopic).IsVisible);
        }

        [Fact]
        public void Expand_RestoresVisibility()
        {
            var manager = BuildTree(out _, out var topic, out var sub, out _);
            topic.IsCollapsed = true;
            MindMapVisibility.Apply(manager.GetComponents(), manager.GetLines());

            topic.IsCollapsed = false;
            MindMapVisibility.Apply(manager.GetComponents(), manager.GetLines());

            Assert.True(sub.IsVisible);
            Assert.All(manager.GetLines(), l => Assert.True(((ConnectionLine)l).IsVisible));
        }

        [Fact]
        public void NestedCollapse_HidesAllDescendants()
        {
            var manager = new DrawingManager();
            var central = new CentralNode { Width = 140, Height = 60 };
            var t1 = new TopicNode { Width = 100, Height = 40 };
            var t2 = new SubTopicNode { Width = 90, Height = 36 };
            var t3 = new SubTopicNode { Width = 90, Height = 36 };
            foreach (var c in new SkiaComponent[] { central, t1, t2, t3 }) manager.AddComponent(c);
            manager.ConnectComponents(central, t1, 0, 0);
            manager.ConnectComponents(t1, t2, 0, 0);
            manager.ConnectComponents(t2, t3, 0, 0);

            t1.IsCollapsed = true;
            MindMapVisibility.Apply(manager.GetComponents(), manager.GetLines());

            Assert.True(t1.IsVisible);
            Assert.False(t2.IsVisible);
            Assert.False(t3.IsVisible);
        }

        [Fact]
        public void Layout_SkipsHiddenNodes()
        {
            var manager = BuildTree(out _, out var topic, out var sub, out _);
            topic.IsCollapsed = true;
            MindMapVisibility.Apply(manager.GetComponents(), manager.GetLines());

            float hiddenX = sub.X;
            float hiddenY = sub.Y;

            manager.ArrangeDiagram(new MindMapLayout { Center = new SKPoint(400, 300) });

            Assert.Equal(hiddenX, sub.X);
            Assert.Equal(hiddenY, sub.Y);
        }

        [Fact]
        public void IconAndCollapse_RoundTripThroughSerialization()
        {
            var manager = BuildTree(out _, out var topic, out _, out _);
            topic.Icon = "★";
            topic.IsCollapsed = true;

            var manager2 = new DrawingManager();
            manager2.LoadFromDto(manager.ToDto());

            var loaded = manager2.GetComponents().OfType<TopicNode>().Single(t => t.Name == "topic");
            Assert.Equal("★", loaded.Icon);
            Assert.True(loaded.IsCollapsed);
        }
    }
}
