using System;
using System.Linq;
using System.Windows.Forms;
using Beep.Skia;
using Beep.Skia.Automation;

namespace Beep.Skia.Sample.WinForms
{
    /// <summary>
    /// Demonstrates the workflow execution service: publishes the current diagram as a workflow,
    /// submits jobs, and shows live job state with pause/resume/cancel controls.
    /// </summary>
    public class ExecutionServiceForm : Form
    {
        private readonly DrawingManager _manager;
        private readonly WorkflowExecutionService _service = new WorkflowExecutionService(maxConcurrency: 2);
        private ListView _jobs = null!;
        private Label _status = null!;
        private Button _submit = null!;
        private Button _pause = null!;
        private Button _resume = null!;
        private Button _cancel = null!;
        private System.Windows.Forms.Timer _timer;
        private string _workflowId = null!;

        public ExecutionServiceForm(DrawingManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            BuildUi();
            PublishCurrent();

            _timer = new System.Windows.Forms.Timer { Interval = 400 };
            _timer.Tick += (s, e) => RefreshJobs();
            _timer.Start();
            RefreshJobs();
        }

        private void BuildUi()
        {
            Text = "Execution Service";
            Width = 820;
            Height = 480;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;

            var top = new Panel { Dock = DockStyle.Top, Height = 46 };
            _submit = new Button { Text = "Submit Job", Left = 10, Top = 9, Width = 100 };
            _submit.Click += (s, e) => SubmitJob();

            _pause = new Button { Text = "Pause", Left = 118, Top = 9, Width = 70 };
            _pause.Click += (s, e) => ActOnSelected(jobId => _service.Pause(jobId), "Paused");

            _resume = new Button { Text = "Resume", Left = 194, Top = 9, Width = 70 };
            _resume.Click += (s, e) => ActOnSelected(jobId => _service.Resume(jobId), "Resumed");

            _cancel = new Button { Text = "Cancel", Left = 270, Top = 9, Width = 70 };
            _cancel.Click += (s, e) => ActOnSelected(jobId => _service.Cancel(jobId), "Cancelled");

            _status = new Label { Left = 356, Top = 14, Width = 440, Text = "Publishing…" };

            top.Controls.Add(_submit);
            top.Controls.Add(_pause);
            top.Controls.Add(_resume);
            top.Controls.Add(_cancel);
            top.Controls.Add(_status);

            _jobs = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false
            };
            _jobs.Columns.Add("Job", 90);
            _jobs.Columns.Add("State", 90);
            _jobs.Columns.Add("Submitted", 140);
            _jobs.Columns.Add("Duration", 90);
            _jobs.Columns.Add("Error", 300);

            Controls.Add(_jobs);
            Controls.Add(top);
        }

        private void PublishCurrent()
        {
            try
            {
                var workflow = _manager.ToWorkflowDefinition("Current Diagram");
                if (workflow.Nodes.Count == 0)
                {
                    _submit.Enabled = false;
                    _status.Text = "No automation nodes in the current diagram.";
                    return;
                }

                _service.Publish(workflow);
                _workflowId = workflow.Id;
                _status.Text = $"Published \"{workflow.Name}\" — {workflow.Nodes.Count} node(s), {workflow.Connections.Count} connection(s).";
            }
            catch (Exception ex)
            {
                _submit.Enabled = false;
                _status.Text = "Publish failed: " + ex.Message;
            }
        }

        private void SubmitJob()
        {
            if (string.IsNullOrEmpty(_workflowId)) return;
            var job = _service.Submit(_workflowId, "sample-user");
            _status.Text = job == null ? "Submit failed." : $"Submitted job {job.Id}.";
            RefreshJobs();
        }

        private void ActOnSelected(Func<string, bool> action, string verb)
        {
            if (_jobs.SelectedItems.Count == 0) return;
            var jobId = _jobs.SelectedItems[0].Tag as string;
            if (string.IsNullOrEmpty(jobId)) return;

            _status.Text = action(jobId) ? $"Job {jobId} {verb.ToLowerInvariant()}." : $"Job {jobId} could not be {verb.ToLowerInvariant()}.";
            RefreshJobs();
        }

        private void RefreshJobs()
        {
            var selected = _jobs.SelectedItems.Count > 0 ? _jobs.SelectedItems[0].Tag as string : null;

            _jobs.BeginUpdate();
            try
            {
                _jobs.Items.Clear();
                foreach (var job in _service.GetJobs(limit: 100))
                {
                    var item = new ListViewItem(job.Id);
                    item.SubItems.Add(job.State.ToString());
                    item.SubItems.Add(job.SubmittedAt.ToLocalTime().ToString("HH:mm:ss"));
                    item.SubItems.Add(job.Duration.HasValue ? $"{job.Duration.Value.TotalMilliseconds:F0} ms" : "—");
                    item.SubItems.Add(job.Error ?? string.Empty);
                    item.Tag = job.Id;
                    if (job.State == JobState.Failed) item.ForeColor = System.Drawing.Color.Firebrick;
                    else if (job.State == JobState.Completed) item.ForeColor = System.Drawing.Color.SeaGreen;
                    _jobs.Items.Add(item);
                    if (job.Id == selected) item.Selected = true;
                }
            }
            finally
            {
                _jobs.EndUpdate();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try { _timer?.Stop(); _timer?.Dispose(); } catch { }
            try { _service.Dispose(); } catch { }
            base.OnFormClosed(e);
        }
    }
}