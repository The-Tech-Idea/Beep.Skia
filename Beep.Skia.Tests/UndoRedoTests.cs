using System.Linq;
using Beep.Skia;
using Beep.Skia.Model;
using SkiaSharp;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Regression tests for the undo/redo pipeline: callers apply a change and record an action;
    /// Undo reverses it and Redo must re-apply it (previously Redo was a no-op for every action).
    /// </summary>
    public class UndoRedoTests
    {
        private sealed class TestNode : SkiaComponent
        {
            protected override void DrawContent(SKCanvas canvas, DrawingContext context) { }
        }

        [Fact]
        public void AddComponent_Undo_Redo_RestoresComponent()
        {
            var manager = new DrawingManager();
            var node = new TestNode { X = 10, Y = 20, Width = 100, Height = 50, Name = "n1" };

            manager.AddComponent(node);
            Assert.Single(manager.GetComponents());
            Assert.True(manager.CanUndo);

            manager.Undo();
            Assert.Empty(manager.GetComponents());
            Assert.True(manager.CanRedo);

            manager.Redo();
            Assert.Single(manager.GetComponents());
            Assert.Same(node, manager.GetComponents()[0]);
        }

        [Fact]
        public void DeleteSelected_Undo_Redo_RestoresComponent()
        {
            var manager = new DrawingManager();
            var node = new TestNode { Width = 50, Height = 50, Name = "n1" };
            manager.AddComponent(node);
            manager.SelectionManager.AddToSelection(node);

            manager.DeleteSelectedComponents();
            Assert.Empty(manager.GetComponents());

            manager.Undo();
            Assert.Single(manager.GetComponents());

            manager.Redo();
            Assert.Empty(manager.GetComponents());
        }

        [Fact]
        public void MoveSelected_Undo_Redo_RestoresPosition()
        {
            var manager = new DrawingManager();
            var node = new TestNode { X = 100, Y = 100, Width = 50, Height = 50, Name = "n1" };
            manager.AddComponent(node);
            manager.SelectionManager.AddToSelection(node);

            manager.MoveSelectedComponents(new SKPoint(25, -10));
            Assert.Equal(125f, node.X);
            Assert.Equal(90f, node.Y);

            manager.Undo();
            Assert.Equal(100f, node.X);
            Assert.Equal(100f, node.Y);

            manager.Redo();
            Assert.Equal(125f, node.X);
            Assert.Equal(90f, node.Y);
        }

        [Fact]
        public void Undo_DoesNotPolluteHistoryStacks()
        {
            var manager = new DrawingManager();
            var node = new TestNode { Width = 50, Height = 50, Name = "n1" };
            manager.AddComponent(node);

            Assert.Equal(1, manager.HistoryManager.UndoCount);
            manager.Undo();

            // Undo must move the action to the redo stack without recording new actions.
            Assert.Equal(0, manager.HistoryManager.UndoCount);
            Assert.Equal(1, manager.HistoryManager.RedoCount);
        }

        [Fact]
        public void HistoryDepth_IsCapped()
        {
            var manager = new DrawingManager();
            manager.HistoryManager.MaxHistoryDepth = 5;

            for (int i = 0; i < 10; i++)
            {
                manager.AddComponent(new TestNode { Name = $"n{i}" });
            }

            Assert.Equal(5, manager.HistoryManager.UndoCount);
        }
    }
}
