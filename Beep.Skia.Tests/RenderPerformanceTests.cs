using System;
using System.Diagnostics;
using Beep.Skia;
using Beep.Skia.Model;
using SkiaSharp;
using Xunit;
using Xunit.Abstractions;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Render-loop performance smoke test: renders a large diagram repeatedly and reports
    /// frame time and per-frame allocations. Guards against gross render-path regressions
    /// (per-frame LINQ/collection churn on hot paths).
    /// </summary>
    public class RenderPerformanceTests
    {
        private readonly ITestOutputHelper _output;

        public RenderPerformanceTests(ITestOutputHelper output) => _output = output;

        private sealed class PerfNode : SkiaComponent
        {
            public PerfNode()
            {
                InConnectionPoints.Add(new ConnectionPoint { Component = this, Index = 0, Position = new SKPoint(0.5f, 0f) });
                OutConnectionPoints.Add(new ConnectionPoint { Component = this, Index = 0, Position = new SKPoint(0.5f, 1f) });
            }

            protected override void DrawContent(SKCanvas canvas, DrawingContext context)
            {
                using var paint = new SKPaint { Color = SKColors.SteelBlue, Style = SKPaintStyle.Fill, IsAntialias = true };
                canvas.DrawRoundRect(new SKRect(X, Y, X + Width, Y + Height), 4, 4, paint);
            }
        }

        private static DrawingManager BuildLargeDiagram(int nodes, bool connect = true)
        {
            var manager = new DrawingManager();
            var created = new System.Collections.Generic.List<SkiaComponent>(nodes);
            for (var i = 0; i < nodes; i++)
            {
                var node = new PerfNode
                {
                    X = 40 + (i % 20) * 130,
                    Y = 40 + (i / 20) * 90,
                    Width = 110,
                    Height = 50,
                    Name = "node" + i
                };
                manager.AddComponent(node);
                created.Add(node);
            }

            if (connect)
            {
                for (var i = 0; i + 1 < created.Count; i++)
                {
                    manager.ConnectComponents(created[i], created[i + 1]);
                }
            }

            return manager;
        }

        private static (double MsPerFrame, double KbPerFrame) Measure(DrawingManager manager, int frames, int repetitions = 5)
        {
            using var surface = SKSurface.Create(new SKImageInfo(1600, 1000));
            var canvas = surface.Canvas;

            // Warm up (JIT + caches).
            for (var i = 0; i < 5; i++)
            {
                canvas.Clear(SKColors.White);
                manager.Draw(canvas);
            }

            var times = new System.Collections.Generic.List<double>(repetitions);
            var allocations = new System.Collections.Generic.List<double>(repetitions);

            for (var r = 0; r < repetitions; r++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                var stopwatch = Stopwatch.StartNew();
                for (var i = 0; i < frames; i++)
                {
                    canvas.Clear(SKColors.White);
                    manager.Draw(canvas);
                }
                stopwatch.Stop();
                var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

                times.Add(stopwatch.Elapsed.TotalMilliseconds / frames);
                allocations.Add(allocated / 1024.0 / frames);
            }

            times.Sort();
            allocations.Sort();

            // Median across repetitions: resistant to a single noisy run on a shared machine.
            return (times[repetitions / 2], allocations[repetitions / 2]);
        }

        [Fact]
        public void TypefaceLookup_CacheIsFasterThanFontMatching()
        {
            const int iterations = 2000;

            // Warm up both paths.
            for (var i = 0; i < 50; i++)
            {
                SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal);
                TypefaceCache.Get("Segoe UI", SKFontStyle.Normal);
            }

            var uncached = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                using var typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal);
            }
            uncached.Stop();

            var cached = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                TypefaceCache.Get("Segoe UI", SKFontStyle.Normal);
            }
            cached.Stop();

            var uncachedUs = uncached.Elapsed.TotalMilliseconds * 1000 / iterations;
            var cachedUs = cached.Elapsed.TotalMilliseconds * 1000 / iterations;

            _output.WriteLine($"font matching: {uncachedUs:F2} us/lookup");
            _output.WriteLine($"cached lookup: {cachedUs:F2} us/lookup");

            Assert.True(cachedUs < uncachedUs,
                $"cached lookup ({cachedUs:F2} us) should beat font matching ({uncachedUs:F2} us)");
        }

        [Fact]
        public void RenderLargeDiagram_ReportsFrameCostAndAllocations()
        {
            const int nodeCount = 200;
            const int frames = 60;

            var nodesOnly = BuildLargeDiagram(nodeCount, connect: false);
            var nodesAndLines = BuildLargeDiagram(nodeCount, connect: true);

            var (msNodes, kbNodes) = Measure(nodesOnly, frames);
            var (msLines, kbLines) = Measure(nodesAndLines, frames);

            _output.WriteLine($"nodes={nodeCount} frames={frames}");
            _output.WriteLine($"nodes only ({nodesOnly.GetComponents().Count} nodes, {nodesOnly.GetLines().Count} lines): {msNodes:F2} ms/frame, {kbNodes:F2} KB/frame");
            _output.WriteLine($"nodes+lines ({nodesAndLines.GetComponents().Count} nodes, {nodesAndLines.GetLines().Count} lines): {msLines:F2} ms/frame, {kbLines:F2} KB/frame");

            // Guard the benchmark itself: the two configurations must actually differ, otherwise the
            // measurement is comparing a workload against itself. Allocations are deterministic, so
            // they are asserted; frame *time* ordering is not asserted because parallel test
            // execution makes wall-clock comparisons flaky.
            Assert.Empty(nodesOnly.GetLines());
            Assert.NotEmpty(nodesAndLines.GetLines());
            Assert.True(kbLines > kbNodes, "drawing connection lines must allocate more than drawing nodes alone");

            Assert.True(msLines < 100, $"frame time regressed: {msLines:F2} ms/frame");
            Assert.True(kbLines < 512, $"per-frame allocations regressed: {kbLines:F1} KB/frame");
        }
    }
}
