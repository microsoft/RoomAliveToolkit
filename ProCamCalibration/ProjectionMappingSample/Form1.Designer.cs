namespace RoomAliveToolkit
{
    partial class Form1
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.videoPanel1 = new RoomAliveToolkit.VideoPanel();
            this.SuspendLayout();
            // 
            // videoPanel1
            // 
            this.videoPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.videoPanel1.Location = new System.Drawing.Point(0, 0);
            this.videoPanel1.Name = "videoPanel1";
            this.videoPanel1.Size = new System.Drawing.Size(810, 564);
            this.videoPanel1.TabIndex = 0;
            this.videoPanel1.SizeChanged += new System.EventHandler(this.videoPanel1_SizeChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(810, 564);
            this.Controls.Add(this.videoPanel1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        public VideoPanel videoPanel1;

    }
}

