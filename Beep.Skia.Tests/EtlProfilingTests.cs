using System;
using System.Collections.Generic;
using System.Linq;
using Beep.Skia.ETL;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for ETL data profiling, preview formatting, and pipeline runtime metrics.
    /// </summary>
    public class EtlProfilingTests
    {
        private static List<Dictionary<string, object>> SampleRows()
        {
            return new List<Dictionary<string, object>>
            {
                new() { ["id"] = 1, ["name"] = "alpha", ["amount"] = 10.5 },
                new() { ["id"] = 2, ["name"] = "beta", ["amount"] = 20.0 },
                new() { ["id"] = 3, ["name"] = null, ["amount"] = 5.25 },
                new() { ["id"] = 4, ["name"] = "alpha", ["amount"] = null }
            };
        }

        [Fact]
        public void Profiler_ComputesColumnStatistics()
        {
            var profile = new DataProfiler().Profile(SampleRows());

            Assert.Equal(4, profile.RowCount);
            Assert.Equal(3, profile.Columns.Count);

            var id = profile.Columns.Single(c => c.Name == "id");
            Assert.Equal(4, id.Count);
            Assert.Equal(0, id.NullCount);
            Assert.Equal(4, id.DistinctCount);
            Assert.True(id.IsNumeric);
            Assert.Equal(2.5, id.Average!.Value, 3);
            Assert.Equal(10.0, id.Sum!.Value, 3);

            var name = profile.Columns.Single(c => c.Name == "name");
            Assert.Equal(3, name.Count);
            Assert.Equal(1, name.NullCount);
            Assert.Equal(2, name.DistinctCount);
            Assert.False(name.IsNumeric);
            Assert.Equal(4, name.MinLength);
            Assert.Equal(5, name.MaxLength);

            var amount = profile.Columns.Single(c => c.Name == "amount");
            Assert.Equal(3, amount.Count);
            Assert.Equal(1, amount.NullCount);
            Assert.True(amount.IsNumeric);
        }

        [Fact]
        public void Profiler_Summary_ContainsColumnsAndRowCount()
        {
            var summary = new DataProfiler().Profile(SampleRows()).Summary();

            Assert.Contains("Rows: 4", summary);
            Assert.Contains("id", summary);
            Assert.Contains("name", summary);
            Assert.Contains("amount", summary);
        }

        [Fact]
        public void Preview_FormatsAlignedTableWithNulls()
        {
            var table = DataPreviewFormatter.ToTable(SampleRows(), maxRows: 3);

            Assert.Contains("id", table);
            Assert.Contains("name", table);
            Assert.Contains("alpha", table);
            Assert.Contains("NULL", table);
            Assert.Contains("and 1 more row", table);
        }

        [Fact]
        public void Preview_HandlesEmptyInput()
        {
            Assert.Equal("(no rows)", DataPreviewFormatter.ToTable(new List<Dictionary<string, object>>()));
            Assert.Equal("(no data)", DataPreviewFormatter.ToTable(null));
        }

        [Fact]
        public void Metrics_RecordsNodesAndSummarizes()
        {
            var metrics = new PipelineMetrics();
            metrics.Start();

            metrics.Measure("Extract", 0, () => 100);
            metrics.Measure("Transform", 100, () => 95);
            metrics.Complete();

            Assert.Equal(2, metrics.Nodes.Count);
            Assert.Equal(100, metrics.Nodes[0].RowsOut);
            Assert.Equal(95, metrics.Nodes[1].RowsOut);
            Assert.False(metrics.IsRunning);

            var summary = metrics.Summary();
            Assert.Contains("Extract", summary);
            Assert.Contains("Transform", summary);
            Assert.Contains("Nodes: 2", summary);
        }

        [Fact]
        public void Metrics_FailedNode_IsRecordedAndRethrown()
        {
            var metrics = new PipelineMetrics();
            metrics.Start();

            Assert.Throws<InvalidOperationException>(() =>
                metrics.Measure("Load", 10, () => throw new InvalidOperationException("boom")));

            Assert.Single(metrics.Nodes);
            Assert.Equal("Failed", metrics.Nodes[0].Status);
            Assert.Equal(10, metrics.Nodes[0].RowsIn);
        }

        [Fact]
        public void Metrics_Reset_ClearsEverything()
        {
            var metrics = new PipelineMetrics();
            metrics.Start();
            metrics.Record("Extract", 0, 5, TimeSpan.FromMilliseconds(2));
            metrics.Reset();

            Assert.Empty(metrics.Nodes);
            Assert.Null(metrics.StartedAt);
            Assert.Null(metrics.CompletedAt);
        }
    }
}
