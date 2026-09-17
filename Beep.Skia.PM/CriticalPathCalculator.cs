using System;
using System.Collections.Generic;
using System.Linq;
using Beep.Skia.Model;

namespace Beep.Skia.PM
{
    /// <summary>
    /// Calculates the critical path for a project network diagram.
    /// Uses forward/backward pass to compute early/late dates and float (slack).
    /// </summary>
    public class CriticalPathCalculator
    {
        public class TaskSchedule
        {
            public TaskNode? Task { get; set; }
            public int EarlyStart { get; set; }
            public int EarlyFinish { get; set; }
            public int LateStart { get; set; }
            public int LateFinish { get; set; }
            public int TotalFloat { get; set; }
            public bool IsCritical => TotalFloat == 0;
            public bool HasValues { get; set; }
        }

        public List<TaskSchedule> Schedules { get; } = new List<TaskSchedule>();
        public int ProjectDuration { get; private set; }
        public List<TaskNode> CriticalPath { get; } = new List<TaskNode>();

        private List<DependencyNode> _dependencyNodes = new List<DependencyNode>();

        /// <summary>
        /// Computes the schedule from components and connection lines.
        /// Lines represent FS (Finish-to-Start) dependencies by default.
        /// DependencyNode with LagDays adjusts the dependency timing.
        /// </summary>
        public void Calculate(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines)
        {
            Schedules.Clear();
            CriticalPath.Clear();

            var tasks = components.OfType<TaskNode>().Where(t => !t.IsStatic).ToList();
            if (tasks.Count == 0) return;

            _dependencyNodes = components.OfType<DependencyNode>().Where(d => !d.IsStatic).ToList();

            foreach (var t in tasks)
            {
                Schedules.Add(new TaskSchedule { Task = t, HasValues = false });
            }

            var predecessors = BuildPredecessorMap(tasks, lines);

            // Forward pass
            var queue = new Queue<TaskNode>();
            var visited = new HashSet<TaskNode>();
            foreach (var t in tasks.Where(t => predecessors[t].Count == 0))
            {
                var sched = Schedules.Find(s => s.Task == t);
                if (sched != null)
                {
                    sched.EarlyStart = 1;
                    sched.EarlyFinish = sched.EarlyStart + t.DurationDays - 1;
                    sched.HasValues = true;
                }
                queue.Enqueue(t);
                visited.Add(t);
            }

            while (queue.Count > 0)
            {
                var task = queue.Dequeue();
                var sched = Schedules.Find(s => s.Task == task);
                if (sched == null || !sched.HasValues) continue;

                // Find successors via outgoing connections
                foreach (var line in lines)
                {
                    if (line?.Start?.Component == task && line?.End?.Component is TaskNode succ && tasks.Contains(succ))
                    {
                        int lag = GetLagDays(task, succ, lines);
                        int candidateStart = sched.EarlyFinish + 1 + lag;

                        var succSched = Schedules.Find(s => s.Task == succ);
                        if (succSched != null)
                        {
                            if (!succSched.HasValues || candidateStart > succSched.EarlyStart)
                            {
                                succSched.EarlyStart = candidateStart;
                                succSched.EarlyFinish = succSched.EarlyStart + succ.DurationDays - 1;
                                succSched.HasValues = true;
                            }
                        }
                        if (!visited.Contains(succ))
                        {
                            visited.Add(succ);
                            queue.Enqueue(succ);
                        }
                    }
                }
            }

            // Find project duration
            ProjectDuration = Schedules.Where(s => s.HasValues).Max(s => s.EarlyFinish);

            // Backward pass
            foreach (var s in Schedules.Where(s => s.HasValues))
            {
                s.LateFinish = ProjectDuration;
                s.LateStart = s.LateFinish - (s.Task?.DurationDays ?? 0) + 1;
            }

            // Backward pass from end nodes (no successors)
            var successors = BuildSuccessorMap(tasks, lines);
            var endNodes = tasks.Where(t => successors[t].Count == 0).ToList();

            var backQueue = new Queue<TaskNode>();
            foreach (var t in endNodes)
            {
                var sched = Schedules.Find(s => s.Task == t);
                if (sched != null && sched.HasValues)
                {
                    sched.LateFinish = ProjectDuration;
                    sched.LateStart = sched.LateFinish - t.DurationDays + 1;
                    sched.TotalFloat = sched.LateStart - sched.EarlyStart;
                }
                backQueue.Enqueue(t);
            }

            while (backQueue.Count > 0)
            {
                var task = backQueue.Dequeue();
                var sched = Schedules.Find(s => s.Task == task);
                if (sched == null || !sched.HasValues) continue;

                foreach (var line in lines)
                {
                    if (line?.End?.Component == task && line?.Start?.Component is TaskNode pred && tasks.Contains(pred))
                    {
                        int lag = GetLagDays(pred, task, lines);
                        int candidateLateFinish = sched.LateStart - 1 - lag;

                        var predSched = Schedules.Find(s => s.Task == pred);
                        if (predSched != null && predSched.HasValues)
                        {
                            if (candidateLateFinish < predSched.LateFinish)
                            {
                                predSched.LateFinish = candidateLateFinish;
                                predSched.LateStart = predSched.LateFinish - pred.DurationDays + 1;
                                predSched.TotalFloat = predSched.LateStart - predSched.EarlyStart;
                            }
                        }
                        backQueue.Enqueue(pred);
                    }
                }
            }

            // Compute total float and identify critical path
            foreach (var sched in Schedules.Where(s => s.HasValues))
            {
                sched.TotalFloat = sched.LateStart - sched.EarlyStart;
                if (sched.TotalFloat <= 0)
                    CriticalPath.Add(sched.Task!);
            }
        }

        private int GetLagDays(TaskNode from, TaskNode to, IReadOnlyList<IConnectionLine> lines)
        {
            // An explicit DependencyNode (matched by task names) wins over line labels.
            var dep = _dependencyNodes.FirstOrDefault(d => Matches(d.FromTask, from) && Matches(d.ToTask, to));
            if (dep != null)
                return dep.LagDays;

            foreach (var line in lines)
            {
                if (line?.Start?.Component == from && line?.End?.Component == to)
                {
                    // Lag is annotated on the connection label when numeric (days).
                    if (int.TryParse(line.Label1, out var lag))
                        return lag;
                }
            }
            return 0;
        }

        private static bool Matches(string value, TaskNode task)
        {
            if (string.IsNullOrWhiteSpace(value) || task == null) return false;
            return string.Equals(value, task.Title, StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, task.Name, StringComparison.OrdinalIgnoreCase);
        }

        private Dictionary<TaskNode, List<TaskNode>> BuildPredecessorMap(List<TaskNode> tasks, IReadOnlyList<IConnectionLine> lines)
        {
            var map = tasks.ToDictionary(t => t, _ => new List<TaskNode>());
            foreach (var line in lines)
            {
                if (line?.Start?.Component is TaskNode pred && tasks.Contains(pred)
                    && line?.End?.Component is TaskNode succ && tasks.Contains(succ))
                {
                    map[succ].Add(pred);
                }
            }
            return map;
        }

        private Dictionary<TaskNode, List<TaskNode>> BuildSuccessorMap(List<TaskNode> tasks, IReadOnlyList<IConnectionLine> lines)
        {
            var map = tasks.ToDictionary(t => t, _ => new List<TaskNode>());
            foreach (var line in lines)
            {
                if (line?.Start?.Component is TaskNode pred && tasks.Contains(pred)
                    && line?.End?.Component is TaskNode succ && tasks.Contains(succ))
                {
                    map[pred].Add(succ);
                }
            }
            return map;
        }
    }
}