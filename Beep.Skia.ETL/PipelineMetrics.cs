using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Beep.Skia.ETL
{
    /// <summary>
    /// Runtime metrics for a single pipeline node.
    /// </summary>
    public class NodeRunMetrics
    {
        /// <summary>
        /// Gets or sets the node name.
        /// </summary>
        public string NodeName { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the rows in.
        /// </summary>
        public int RowsIn { get; set; }
        /// <summary>
        /// Gets or sets the rows out.
        /// </summary>
        public int RowsOut { get; set; }
        /// <summary>
        /// Gets or sets the elapsed.
        /// </summary>
        public TimeSpan Elapsed { get; set; }
        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        public string Status { get; set; } = "Completed";
        /// <summary>
        /// Gets or sets the recorded at.
        /// </summary>
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the rows per second.
        /// </summary>
        public double RowsPerSecond => Elapsed.TotalSeconds > 0 ? RowsOut / Elapsed.TotalSeconds : 0d;
    }

    /// <summary>
    /// Collects per-node runtime metrics for an ETL pipeline run.
    /// </summary>
    public class PipelineMetrics
    {
        /// <summary>
        /// Gets or sets the nodes.
        /// </summary>
        public List<NodeRunMetrics> Nodes { get; } = new List<NodeRunMetrics>();
        /// <summary>
        /// Gets or sets the started at.
        /// </summary>
        public DateTime? StartedAt { get; private set; }
        /// <summary>
        /// Gets or sets the completed at.
        /// </summary>
        public DateTime? CompletedAt { get; private set; }

        /// <summary>
        /// Gets or sets the is running.
        /// </summary>
        public bool IsRunning => StartedAt.HasValue && !CompletedAt.HasValue;

        /// <summary>
        /// Gets or sets the total elapsed.
        /// </summary>
        public TimeSpan TotalElapsed
            => StartedAt.HasValue && CompletedAt.HasValue
                ? CompletedAt.Value - StartedAt.Value
                : TimeSpan.Zero;

        /// <summary>
        /// Marks the start of a pipeline run.
        /// </summary>
        public void Start()
        {
            StartedAt = DateTime.UtcNow;
            CompletedAt = null;
        }

        /// <summary>
        /// Marks the end of a pipeline run.
        /// </summary>
        public void Complete()
        {
            CompletedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Records a completed node execution.
        /// </summary>
        public NodeRunMetrics Record(string nodeName, int rowsIn, int rowsOut, TimeSpan elapsed, string status = "Completed")
        {
            var metrics = new NodeRunMetrics
            {
                NodeName = nodeName ?? string.Empty,
                RowsIn = rowsIn,
                RowsOut = rowsOut,
                Elapsed = elapsed,
                Status = status
            };
            Nodes.Add(metrics);
            return metrics;
        }

        /// <summary>
        /// Measures a node action and records its metrics. Returns the action's row count.
        /// </summary>
        public int Measure(string nodeName, int rowsIn, Func<int> action)
        {
            if (action == null) return 0;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                int rowsOut = action();
                stopwatch.Stop();
                Record(nodeName, rowsIn, rowsOut, stopwatch.Elapsed);
                return rowsOut;
            }
            catch
            {
                stopwatch.Stop();
                Record(nodeName, rowsIn, 0, stopwatch.Elapsed, "Failed");
                throw;
            }
        }

        /// <summary>
        /// Clears all metrics and timing.
        /// </summary>
        public void Reset()
        {
            Nodes.Clear();
            StartedAt = null;
            CompletedAt = null;
        }

        /// <summary>
        /// Human-readable metrics report.
        /// </summary>
        public string Summary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Pipeline run: {(StartedAt.HasValue ? StartedAt.Value.ToString("u", CultureInfo.InvariantCulture) : "(not started)")}");
            sb.AppendLine($"Total elapsed: {TotalElapsed.TotalMilliseconds:0} ms");
            sb.AppendLine($"Nodes: {Nodes.Count}, failed: {Nodes.Count(n => n.Status == "Failed")}");
            sb.AppendLine(new string('-', 78));
            sb.AppendLine($"{"Node",-24} {"Status",-10} {"Rows In",8} {"Rows Out",9} {"Elapsed",10} {"Rows/s",10}");
            sb.AppendLine(new string('-', 78));

            foreach (var node in Nodes)
            {
                sb.AppendLine($"{Truncate(node.NodeName, 24),-24} {node.Status,-10} {node.RowsIn,8} {node.RowsOut,9} {node.Elapsed.TotalMilliseconds,8:0} ms {node.RowsPerSecond,10:0.##}");
            }
            return sb.ToString().TrimEnd();
        }

        private static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= max) return text ?? string.Empty;
            return text.Substring(0, Math.Max(0, max - 1)) + "…";
        }
    }
}
