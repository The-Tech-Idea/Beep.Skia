namespace Beep.Skia.Winform
{
    partial class Beep_Skia_Control
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            skControl1 = new SkiaSharp.Views.Desktop.SKControl();
            SuspendLayout();
            // 
            // skControl1
            // 
            skControl1.Dock = DockStyle.Fill;
            skControl1.Location = new Point(0, 0);
            skControl1.Name = "skControl1";
            skControl1.Size = new Size(800, 450);
            skControl1.TabIndex = 0;
            skControl1.Text = "skControl1";
            // 
            // Beep_Skia_Control
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(skControl1);
            Name = "Beep_Skia_Control";
            Size = new Size(800, 450);
            ResumeLayout(false);
        }

        #endregion

        private SkiaSharp.Views.Desktop.SKControl skControl1;
    }
}