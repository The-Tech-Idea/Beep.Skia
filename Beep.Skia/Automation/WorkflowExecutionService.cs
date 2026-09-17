using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Beep.Skia.Model;

namespace Beep.Skia.Automation
{
    /// <summary>Lifecycle state of a submitted workflow execution job.</summary>
    public enum JobState
    {
        Queued,
        Running,
        Paused,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>A submitted workflow execution job with its inputs and result.</summary>
    public class ExecutionJob
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public string Id { get; } = Guid.NewGuid().ToString("N")[..8];
        /// <summary>
        /// Gets or sets the workflow id.
        /// </summary>
        public string WorkflowId { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the workflow name.
        /// </summary>
        public string WorkflowName { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the submitted by.
        /// </summary>
        public string SubmittedBy { get; set; } = "anonymous";
        /// <summary>
        /// Gets or sets the submitted at.
        /// </summary>
        public DateTime SubmittedAt { get; } = DateTime.UtcNow;
        /// <summary>
        /// Gets or sets the started at.
        /// </summary>
        public DateTime? StartedAt { get; set; }
        /// <summary>
        /// Gets or sets the completed at.
        /// </summary>
        public DateTime? CompletedAt { get; set; }
        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        public JobState State { get; internal set; } = JobState.Queued;

        /// <summary>Engine execution id once the job starts running.</summary>
        public string ExecutionId { get; internal set; }

        /// <summary>Failure message when <see cref="State"/> is Failed.</summary>
        public string Error { get; internal set; }

        /// <summary>Input variables for this execution.</summary>
        public Dictionary<string, object> Inputs { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Engine result once the job has finished.</summary>
        public WorkflowResult Result { get; internal set; }

        /// <summary>
        /// Gets or sets the duration.
        /// </summary>
        public TimeSpan? Duration => StartedAt.HasValue && CompletedAt.HasValue
            ? CompletedAt.Value - StartedAt.Value
            : null;

        /// <summary>
        /// Gets or sets the to string.
        /// </summary>
        public override string ToString() => $"{Id} {WorkflowName} [{State}]";
    }

    /// <summary>Raised when a job transitions between states.</summary>
    public class JobStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the JobStateChangedEventArgs class.
        /// </summary>
        public JobStateChangedEventArgs(ExecutionJob job, JobState oldState, JobState newState)
        {
            Job = job;
            OldState = oldState;
            NewState = newState;
        }

        /// <summary>
        /// Gets or sets the job.
        /// </summary>
        public ExecutionJob Job { get; }
        /// <summary>
        /// Gets or sets the old state.
        /// </summary>
        public JobState OldState { get; }
        /// <summary>
        /// Gets or sets the new state.
        /// </summary>
        public JobState NewState { get; }
    }

    /// <summary>
    /// Transport-agnostic execution service for published workflows: submit jobs with inputs,
    /// queue them with bounded concurrency, query status, pause/resume/cancel, and inspect results.
    /// A REST layer (or any other transport) can be placed on top of this service.
    /// </summary>
    public class WorkflowExecutionService : IDisposable
    {
        private readonly WorkflowEngine _engine;
        private readonly bool _ownsEngine;
        private readonly ConcurrentDictionary<string, ExecutionJob> _jobs = new ConcurrentDictionary<string, ExecutionJob>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, WorkflowDefinition> _workflows = new ConcurrentDictionary<string, WorkflowDefinition>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, Task> _tasks = new ConcurrentDictionary<string, Task>(StringComparer.Ordinal);
        private readonly SemaphoreSlim _slots;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the WorkflowExecutionService class.
        /// </summary>
        public WorkflowExecutionService(int maxConcurrency = 2, WorkflowEngine engine = null)
        {
            MaxConcurrency = Math.Max(1, maxConcurrency);
            _slots = new SemaphoreSlim(MaxConcurrency, MaxConcurrency);
            _engine = engine ?? new WorkflowEngine();
            _ownsEngine = engine == null;
        }

        /// <summary>Maximum number of jobs that may run at the same time.</summary>
        public int MaxConcurrency { get; }

        /// <summary>The underlying workflow engine.</summary>
        public WorkflowEngine Engine => _engine;

        /// <summary>Raised on every job state transition.</summary>
        public event EventHandler<JobStateChangedEventArgs> JobStateChanged;

        /// <summary>Published workflows available for submission.</summary>
        public IReadOnlyList<WorkflowDefinition> Workflows => _workflows.Values.ToList().AsReadOnly();

        /// <summary>Publishes (or replaces) a workflow so it can be submitted for execution.</summary>
        public void Publish(WorkflowDefinition workflow)
        {
            if (workflow == null) throw new ArgumentNullException(nameof(workflow));
            if (string.IsNullOrWhiteSpace(workflow.Id)) throw new ArgumentException("Workflow id is required.", nameof(workflow));
            _workflows[workflow.Id] = workflow;
        }

        /// <summary>
        /// Gets or sets the remove workflow.
        /// </summary>
        public bool RemoveWorkflow(string workflowId)
            => !string.IsNullOrWhiteSpace(workflowId) && _workflows.TryRemove(workflowId, out _);

        /// <summary>
        /// Gets or sets the get workflow.
        /// </summary>
        public WorkflowDefinition GetWorkflow(string workflowId)
            => workflowId != null && _workflows.TryGetValue(workflowId, out var workflow) ? workflow : null;

        /// <summary>
        /// Submits a job for the given published workflow. Returns null when the workflow is unknown.
        /// The job starts as soon as a concurrency slot is available.
        /// </summary>
        public ExecutionJob Submit(string workflowId, string submittedBy = "anonymous", IDictionary<string, object> inputs = null)
        {
            if (_disposed) return null;
            if (!_workflows.TryGetValue(workflowId ?? string.Empty, out var workflow)) return null;

            var job = new ExecutionJob
            {
                WorkflowId = workflow.Id,
                WorkflowName = workflow.Name,
                SubmittedBy = string.IsNullOrWhiteSpace(submittedBy) ? "anonymous" : submittedBy
            };
            if (inputs != null)
            {
                foreach (var pair in inputs) job.Inputs[pair.Key] = pair.Value;
            }

            _jobs[job.Id] = job;
            var task = Task.Run(() => RunJobAsync(job, workflow));
            _tasks[job.Id] = task;
            task.ContinueWith(_ => _tasks.TryRemove(job.Id, out _), TaskScheduler.Default);
            return job;
        }

        private async Task RunJobAsync(ExecutionJob job, WorkflowDefinition workflow)
        {
            await _slots.WaitAsync().ConfigureAwait(false);
            try
            {
                if (job.State == JobState.Cancelled)
                {
                    job.CompletedAt = DateTime.UtcNow;
                    return;
                }

                job.ExecutionId = $"{job.Id}-{Guid.NewGuid().ToString("N")[..8]}";
                job.StartedAt = DateTime.UtcNow;
                SetState(job, JobState.Running);

                await _engine.LoadWorkflowAsync(workflow).ConfigureAwait(false);
                var context = new Beep.Skia.Model.ExecutionContext(workflow.Id, job.ExecutionId)
                {
                    Data = new Dictionary<string, object>(job.Inputs, StringComparer.OrdinalIgnoreCase)
                };

                var result = await _engine.ExecuteWorkflowAsync(workflow.Id, context).ConfigureAwait(false);
                job.Result = result;

                switch (result.Status)
                {
                    case WorkflowStatus.Cancelled:
                        SetState(job, JobState.Cancelled);
                        break;
                    case WorkflowStatus.Failed:
                        job.Error = result.Errors?.FirstOrDefault() ?? "Workflow execution failed.";
                        SetState(job, JobState.Failed);
                        break;
                    default:
                        SetState(job, JobState.Completed);
                        break;
                }
            }
            catch (Exception ex)
            {
                job.Error = ex.Message;
                SetState(job, JobState.Failed);
            }
            finally
            {
                job.CompletedAt = DateTime.UtcNow;
                try { _slots.Release(); } catch (ObjectDisposedException) { }
            }
        }

        /// <summary>
        /// Gets or sets the get job.
        /// </summary>
        public ExecutionJob GetJob(string jobId)
            => jobId != null && _jobs.TryGetValue(jobId, out var job) ? job : null;

        /// <summary>Lists jobs, newest first, optionally filtered by state.</summary>
        public IReadOnlyList<ExecutionJob> GetJobs(JobState? state = null, int limit = 50)
            => _jobs.Values
                .Where(j => !state.HasValue || j.State == state.Value)
                .OrderByDescending(j => j.SubmittedAt)
                .Take(Math.Max(1, limit))
                .ToList()
                .AsReadOnly();

        /// <summary>Cancels a queued or running job. Returns false when it already finished.</summary>
        public bool Cancel(string jobId)
        {
            var job = GetJob(jobId);
            if (job == null) return false;
            if (job.State == JobState.Completed || job.State == JobState.Failed || job.State == JobState.Cancelled)
                return false;

            if (job.State == JobState.Queued)
            {
                job.CompletedAt = DateTime.UtcNow;
                SetState(job, JobState.Cancelled);
                return true;
            }

            if (string.IsNullOrEmpty(job.ExecutionId)) return false;
            var cancelled = _engine.CancelExecutionAsync(job.ExecutionId).GetAwaiter().GetResult();
            if (cancelled) SetState(job, JobState.Cancelled);
            return cancelled;
        }

        /// <summary>Pauses a running job.</summary>
        public bool Pause(string jobId)
        {
            var job = GetJob(jobId);
            if (job == null || job.State != JobState.Running || string.IsNullOrEmpty(job.ExecutionId)) return false;

            var paused = _engine.PauseExecutionAsync(job.ExecutionId).GetAwaiter().GetResult();
            if (paused) SetState(job, JobState.Paused);
            return paused;
        }

        /// <summary>Resumes a paused job.</summary>
        public bool Resume(string jobId)
        {
            var job = GetJob(jobId);
            if (job == null || job.State != JobState.Paused || string.IsNullOrEmpty(job.ExecutionId)) return false;

            var resumed = _engine.ResumeExecutionAsync(job.ExecutionId).GetAwaiter().GetResult();
            if (resumed) SetState(job, JobState.Running);
            return resumed;
        }

        /// <summary>Waits until a job reaches a terminal state, or the timeout elapses.</summary>
        public bool WaitForCompletion(string jobId, TimeSpan timeout)
        {
            var job = GetJob(jobId);
            if (job == null) return false;

            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                if (job.State == JobState.Completed || job.State == JobState.Failed || job.State == JobState.Cancelled)
                    return true;
                Thread.Sleep(15);
            }
            return false;
        }

        private void SetState(ExecutionJob job, JobState state)
        {
            var old = job.State;
            if (old == state) return;
            job.State = state;
            try { JobStateChanged?.Invoke(this, new JobStateChangedEventArgs(job, old, state)); } catch { }
        }

        /// <summary>
        /// Gets or sets the dispose.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var job in _jobs.Values.ToList())
            {
                try { Cancel(job.Id); } catch { }
            }

            if (_ownsEngine)
            {
                try { _engine.StopAsync(graceful: false).GetAwaiter().GetResult(); } catch { }
            }

            try { _slots.Dispose(); } catch { }
        }
    }
}
