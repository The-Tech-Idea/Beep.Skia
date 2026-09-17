using Beep.Skia;
using Beep.Skia.Model;
using SkiaSharp;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for view panning: middle-button drag moves the view without touching the diagram.
    /// </summary>
    public class PanInteractionTests
    {
        private sealed class TestNode : SkiaComponent
        {
            protected override void DrawContent(SKCanvas canvas, DrawingContext context) { }
        }

        private static (DrawingManager Manager, TestNode Node) Build()
        {
            var manager = new DrawingManager();
            var node = new TestNode { X = 100, Y = 100, Width = 80, Height = 40, Name = "node" };
            manager.AddComponent(node);
            return (manager, node);
        }

        [Fact]
        public void MiddleButtonDrag_PansTheView()
        {
            var (manager, _) = Build();
            var helper = manager.InteractionHelper;

            helper.HandleMouseDown(new SKPoint(200, 150), SKKeyModifiers.None, mouseButton: 2);
            Assert.True(helper.IsPanning);

            helper.HandleMouseMove(new SKPoint(240, 180));
            Assert.Equal(new SKPoint(40, 30), manager.PanOffset);

            helper.HandleMouseMove(new SKPoint(190, 140));
            Assert.Equal(new SKPoint(-10, -10), manager.PanOffset);

            helper.HandleMouseUp(new SKPoint(190, 140), SKKeyModifiers.None, mouseButton: 2);
            Assert.False(helper.IsPanning);
        }

        [Fact]
        public void MiddleButtonDrag_DoesNotMoveComponentsOrSelection()
        {
            var (manager, node) = Build();
            var helper = manager.InteractionHelper;

            var startX = node.X;
            var startY = node.Y;

            helper.HandleMouseDown(new SKPoint(100, 100), SKKeyModifiers.None, mouseButton: 2);
            helper.HandleMouseMove(new SKPoint(180, 160));
            helper.HandleMouseUp(new SKPoint(180, 160), SKKeyModifiers.None, mouseButton: 2);

            Assert.Equal(startX, node.X);
            Assert.Equal(startY, node.Y);
            Assert.Empty(manager.SelectionManager.SelectedComponents);
            Assert.False(manager.InteractionHelper.IsDrawingLine);
        }

        [Fact]
        public void PanOffset_StopsChangingAfterMouseUp()
        {
            var (manager, _) = Build();
            var helper = manager.InteractionHelper;

            helper.HandleMouseDown(new SKPoint(50, 50), SKKeyModifiers.None, mouseButton: 2);
            helper.HandleMouseMove(new SKPoint(70, 60));
            var afterDrag = manager.PanOffset;

            helper.HandleMouseUp(new SKPoint(70, 60), SKKeyModifiers.None, mouseButton: 2);
            helper.HandleMouseMove(new SKPoint(200, 200));

            Assert.Equal(afterDrag, manager.PanOffset);
        }

        [Fact]
        public void LeftClick_StillSelectsComponents()
        {
            var (manager, node) = Build();
            var helper = manager.InteractionHelper;

            helper.HandleMouseDown(new SKPoint(140, 120), SKKeyModifiers.None, mouseButton: 0);
            helper.HandleMouseUp(new SKPoint(140, 120), SKKeyModifiers.None, mouseButton: 0);

            Assert.False(helper.IsPanning);
            Assert.Contains(node, manager.SelectionManager.SelectedComponents);
        }

        [Fact]
        public void ZoomToCursor_KeepsCursorPointStable()
        {
            var (manager, _) = Build();
            var helper = manager.InteractionHelper;

            var cursor = new SKPoint(300, 200);
            var worldBefore = new SKPoint(
                (cursor.X - manager.PanOffset.X) / manager.Zoom,
                (cursor.Y - manager.PanOffset.Y) / manager.Zoom);

            helper.HandleMouseWheel(cursor, 120);

            var worldAfter = new SKPoint(
                (cursor.X - manager.PanOffset.X) / manager.Zoom,
                (cursor.Y - manager.PanOffset.Y) / manager.Zoom);

            Assert.True(manager.Zoom > 1f);
            Assert.Equal(worldBefore.X, worldAfter.X, 2);
            Assert.Equal(worldBefore.Y, worldAfter.Y, 2);
        }
    }
}
