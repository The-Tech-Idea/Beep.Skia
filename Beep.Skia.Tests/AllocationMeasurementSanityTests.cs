using System;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Validates the allocation-measurement technique used by the render performance tests:
    /// a known allocation must be detected, otherwise reported figures are meaningless.
    /// </summary>
    public class AllocationMeasurementSanityTests
    {
        private readonly ITestOutputHelper _output;

        public AllocationMeasurementSanityTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public void GetAllocatedBytesForCurrentThread_DetectsKnownAllocations()
        {
            // Warm up the measurement path.
            _ = GC.GetAllocatedBytesForCurrentThread();

            var before = GC.GetAllocatedBytesForCurrentThread();
            var payload = new byte[1024 * 1024];
            var after = GC.GetAllocatedBytesForCurrentThread();

            var detected = after - before;
            _output.WriteLine($"detected {detected} bytes for a 1 MB allocation");
            Assert.True(payload.Length == 1024 * 1024);
            Assert.True(detected >= 1024 * 1024, $"measurement missed the allocation (detected {detected})");
        }

        [Fact]
        public void GetAllocatedBytesForCurrentThread_DetectsPerIterationAllocations()
        {
            const int iterations = 1000;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var before = GC.GetAllocatedBytesForCurrentThread();
            var stopwatch = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                var list = new System.Collections.Generic.List<int>(16);
                list.Add(i);
            }
            stopwatch.Stop();
            var detected = GC.GetAllocatedBytesForCurrentThread() - before;

            _output.WriteLine($"detected {detected} bytes over {iterations} iterations ({detected / (double)iterations:F1} B/iteration)");
            Assert.True(detected >= 16 * 4 * iterations, "per-iteration allocations were not detected");
        }
    }
}
