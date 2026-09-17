using System;
using System.Linq;
using System.Windows.Forms;
using Beep.Skia.Collaboration;
using Beep.Skia.Winform.Controls;

namespace Beep.Skia.Sample.WinForms
{
    /// <summary>
    /// Comment panel for the collaboration demo: lists comments, adds a comment
    /// anchored to the current selection, and resolves comments.
    /// </summary>
    public class CommentsForm : Form
    {
        private readonly SkiaHostControl _host;
        private ListView _list = null!;
        private TextBox _input = null!;
        private Label _anchorLabel = null!;

        public CommentsForm(SkiaHostControl host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            BuildUi();
            RefreshComments();
        }

        private void BuildUi()
        {
            Text = "Comments";
            Width = 720;
            Height = 460;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;

            _list = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false
            };
            _list.Columns.Add("Status", 70);
            _list.Columns.Add("Anchor", 140);
            _list.Columns.Add("Author", 90);
            _list.Columns.Add("Comment", 360);
            _list.DoubleClick += (s, e) => ResolveSelected();

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 86 };
            _anchorLabel = new Label { Left = 8, Top = 6, Width = 680, Text = "Anchor: (no selection)" };

            _input = new TextBox { Left = 8, Top = 28, Width = 560 };

            var add = new Button { Text = "Add", Left = 576, Top = 26, Width = 60 };
            add.Click += (s, e) => AddComment();

            var resolve = new Button { Text = "Resolve", Left = 640, Top = 26, Width = 60 };
            resolve.Click += (s, e) => ResolveSelected();

            bottom.Controls.Add(_anchorLabel);
            bottom.Controls.Add(_input);
            bottom.Controls.Add(add);
            bottom.Controls.Add(resolve);

            Controls.Add(_list);
            Controls.Add(bottom);

            UpdateAnchorLabel();
        }

        private void UpdateAnchorLabel()
        {
            try
            {
                var selected = _host.DrawingManager?.SelectionManager?.SelectedComponents;
                var component = selected?.FirstOrDefault();
                _anchorLabel.Text = component == null
                    ? "Anchor: (select a component to comment on it)"
                    : $"Anchor: {component.Name ?? component.Id.ToString()}";
            }
            catch
            {
                _anchorLabel.Text = "Anchor: (unavailable)";
            }
        }

        private void AddComment()
        {
            var text = _input.Text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show(this, "Enter a comment first.", "Comments");
                return;
            }

            var comment = _host.AddCommentToSelection(text);
            if (comment == null)
            {
                MessageBox.Show(this, "Select a component in the diagram first.", "Comments");
                return;
            }

            _input.Clear();
            RefreshComments();
        }

        private void ResolveSelected()
        {
            if (_list.SelectedItems.Count == 0) return;
            var id = _list.SelectedItems[0].Tag as string;
            if (string.IsNullOrEmpty(id)) return;

            if (_host.ResolveComment(id))
                RefreshComments();
        }

        private void RefreshComments()
        {
            _list.BeginUpdate();
            try
            {
                _list.Items.Clear();
                foreach (var comment in _host.Collaboration.GetComments(_host.DocumentId, includeResolved: true)
                             .OrderBy(c => c.Resolved)
                             .ThenBy(c => c.CreatedAt))
                {
                    var item = new ListViewItem(comment.Resolved ? "Resolved" : "Open");
                    item.SubItems.Add(comment.ComponentId ?? "(document)");
                    item.SubItems.Add(comment.AuthorId);
                    item.SubItems.Add(comment.Text);
                    item.Tag = comment.Id;
                    if (comment.Resolved)
                        item.ForeColor = System.Drawing.Color.Gray;
                    _list.Items.Add(item);
                }
            }
            finally
            {
                _list.EndUpdate();
            }

            UpdateAnchorLabel();
        }
    }
}