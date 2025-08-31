namespace Beep.Skia.Sample.WinForms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton toolAddButton;
        private System.Windows.Forms.ToolStripButton toolAddLabel;
        private System.Windows.Forms.ToolStripButton toolAddMenu;
        private System.Windows.Forms.ToolStripButton toolSave;
        private System.Windows.Forms.ToolStripButton toolLoad;
        private Beep.Skia.Winform.Controls.SkiaHostControl skiaHostControl1;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolAddButton = new System.Windows.Forms.ToolStripButton();
            this.toolAddLabel = new System.Windows.Forms.ToolStripButton();
            this.toolAddMenu = new System.Windows.Forms.ToolStripButton();
            this.toolSave = new System.Windows.Forms.ToolStripButton();
            this.toolLoad = new System.Windows.Forms.ToolStripButton();
            this.skiaHostControl1 = new Beep.Skia.Winform.Controls.SkiaHostControl();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolAddButton,
            this.toolAddLabel,
            this.toolAddMenu,
            this.toolSave,
            this.toolLoad});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(800, 25);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolAddButton
            // 
            this.toolAddButton.Text = "Add Button";
            // 
            // toolAddLabel
            // 
            this.toolAddLabel.Text = "Add Label";
            // 
            // toolAddMenu
            // 
            this.toolAddMenu.Text = "Add Menu";
            // 
            // toolSave
            // 
            this.toolSave.Text = "Save";
            // 
            // toolLoad
            // 
            this.toolLoad.Text = "Load";
            // 
            // skiaHostControl1
            // 
            this.skiaHostControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.skiaHostControl1.Location = new System.Drawing.Point(0, 25);
            this.skiaHostControl1.Name = "skiaHostControl1";
            this.skiaHostControl1.Size = new System.Drawing.Size(800, 575);
            this.skiaHostControl1.TabIndex = 1;
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Controls.Add(this.skiaHostControl1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "MainForm";
            this.Text = "Beep.Skia Sample";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
