namespace RoomAliveToolkit
{
    partial class MainForm
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.saveToOBJToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.setupToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.discoverCamerasToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.discoverProjectorsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.displayProjectorDisplayIndexesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.displayProjectorNamesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.calibrateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.acquireToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.solveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.acquireDepthAndColorOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.renderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.liveViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.wireframeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cameraFrustumsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.projectorFrustumsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.perspectiveAtOriginToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.videoPanel1 = new RoomAliveToolkit.VideoPanel();
            this.consoleTextBox1 = new RoomAliveToolkit.ConsoleTextBox();
            this.solveStepsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.decodeGrayCodesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.calibrateProjectorGroupsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optimizePoseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.setupToolStripMenuItem,
            this.calibrateToolStripMenuItem,
            this.renderToolStripMenuItem,
            this.viewToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1483, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem,
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.reloadToolStripMenuItem,
            this.toolStripSeparator4,
            this.saveToOBJToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // newToolStripMenuItem
            // 
            this.newToolStripMenuItem.Name = "newToolStripMenuItem";
            this.newToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.newToolStripMenuItem.Text = "New";
            this.newToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.openToolStripMenuItem.Text = "Open...";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.saveToolStripMenuItem.Text = "Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.saveAsToolStripMenuItem.Text = "Save As...";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
            // 
            // reloadToolStripMenuItem
            // 
            this.reloadToolStripMenuItem.Name = "reloadToolStripMenuItem";
            this.reloadToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.reloadToolStripMenuItem.Text = "Reload";
            this.reloadToolStripMenuItem.Click += new System.EventHandler(this.reloadToolStripMenuItem_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(143, 6);
            // 
            // saveToOBJToolStripMenuItem
            // 
            this.saveToOBJToolStripMenuItem.Name = "saveToOBJToolStripMenuItem";
            this.saveToOBJToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.saveToOBJToolStripMenuItem.Text = "Save To OBJ...";
            this.saveToOBJToolStripMenuItem.Click += new System.EventHandler(this.saveToOBJToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(143, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // setupToolStripMenuItem
            // 
            this.setupToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.discoverCamerasToolStripMenuItem,
            this.discoverProjectorsToolStripMenuItem,
            this.displayProjectorDisplayIndexesToolStripMenuItem,
            this.displayProjectorNamesToolStripMenuItem});
            this.setupToolStripMenuItem.Name = "setupToolStripMenuItem";
            this.setupToolStripMenuItem.Size = new System.Drawing.Size(49, 20);
            this.setupToolStripMenuItem.Text = "Setup";
            // 
            // discoverCamerasToolStripMenuItem
            // 
            this.discoverCamerasToolStripMenuItem.Name = "discoverCamerasToolStripMenuItem";
            this.discoverCamerasToolStripMenuItem.Size = new System.Drawing.Size(296, 22);
            this.discoverCamerasToolStripMenuItem.Text = "Discover Cameras";
            this.discoverCamerasToolStripMenuItem.Click += new System.EventHandler(this.discoverCamerasToolStripMenuItem_Click);
            // 
            // discoverProjectorsToolStripMenuItem
            // 
            this.discoverProjectorsToolStripMenuItem.Name = "discoverProjectorsToolStripMenuItem";
            this.discoverProjectorsToolStripMenuItem.Size = new System.Drawing.Size(296, 22);
            this.discoverProjectorsToolStripMenuItem.Text = "Discover Projectors";
            this.discoverProjectorsToolStripMenuItem.Click += new System.EventHandler(this.discoverProjectorsToolStripMenuItem_Click);
            // 
            // displayProjectorDisplayIndexesToolStripMenuItem
            // 
            this.displayProjectorDisplayIndexesToolStripMenuItem.Name = "displayProjectorDisplayIndexesToolStripMenuItem";
            this.displayProjectorDisplayIndexesToolStripMenuItem.Size = new System.Drawing.Size(296, 22);
            this.displayProjectorDisplayIndexesToolStripMenuItem.Text = "Show Projector Server Connected Displays";
            this.displayProjectorDisplayIndexesToolStripMenuItem.Click += new System.EventHandler(this.displayProjectorDisplayIndexesToolStripMenuItem_Click);
            // 
            // displayProjectorNamesToolStripMenuItem
            // 
            this.displayProjectorNamesToolStripMenuItem.Name = "displayProjectorNamesToolStripMenuItem";
            this.displayProjectorNamesToolStripMenuItem.Size = new System.Drawing.Size(296, 22);
            this.displayProjectorNamesToolStripMenuItem.Text = "Show Projector Names";
            this.displayProjectorNamesToolStripMenuItem.Click += new System.EventHandler(this.displayProjectorNamesToolStripMenuItem_Click);
            // 
            // calibrateToolStripMenuItem
            // 
            this.calibrateToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.acquireToolStripMenuItem,
            this.solveToolStripMenuItem,
            this.toolStripSeparator5,
            this.acquireDepthAndColorOnlyToolStripMenuItem,
            this.solveStepsToolStripMenuItem});
            this.calibrateToolStripMenuItem.Name = "calibrateToolStripMenuItem";
            this.calibrateToolStripMenuItem.Size = new System.Drawing.Size(66, 20);
            this.calibrateToolStripMenuItem.Text = "Calibrate";
            // 
            // acquireToolStripMenuItem
            // 
            this.acquireToolStripMenuItem.Name = "acquireToolStripMenuItem";
            this.acquireToolStripMenuItem.Size = new System.Drawing.Size(233, 22);
            this.acquireToolStripMenuItem.Text = "Acquire";
            this.acquireToolStripMenuItem.Click += new System.EventHandler(this.acquireToolStripMenuItem_Click);
            // 
            // solveToolStripMenuItem
            // 
            this.solveToolStripMenuItem.Name = "solveToolStripMenuItem";
            this.solveToolStripMenuItem.Size = new System.Drawing.Size(233, 22);
            this.solveToolStripMenuItem.Text = "Solve";
            this.solveToolStripMenuItem.Click += new System.EventHandler(this.solveToolStripMenuItem_Click);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(230, 6);
            // 
            // acquireDepthAndColorOnlyToolStripMenuItem
            // 
            this.acquireDepthAndColorOnlyToolStripMenuItem.Name = "acquireDepthAndColorOnlyToolStripMenuItem";
            this.acquireDepthAndColorOnlyToolStripMenuItem.Size = new System.Drawing.Size(233, 22);
            this.acquireDepthAndColorOnlyToolStripMenuItem.Text = "Acquire Depth and Color Only";
            this.acquireDepthAndColorOnlyToolStripMenuItem.Click += new System.EventHandler(this.acquireDepthAndColorOnlyToolStripMenuItem_Click);
            // 
            // renderToolStripMenuItem
            // 
            this.renderToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.liveViewToolStripMenuItem,
            this.wireframeToolStripMenuItem,
            this.cameraFrustumsToolStripMenuItem,
            this.projectorFrustumsToolStripMenuItem,
            this.toolStripSeparator2});
            this.renderToolStripMenuItem.Name = "renderToolStripMenuItem";
            this.renderToolStripMenuItem.Size = new System.Drawing.Size(56, 20);
            this.renderToolStripMenuItem.Text = "Render";
            // 
            // liveViewToolStripMenuItem
            // 
            this.liveViewToolStripMenuItem.Name = "liveViewToolStripMenuItem";
            this.liveViewToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.liveViewToolStripMenuItem.Text = "Live View";
            this.liveViewToolStripMenuItem.Click += new System.EventHandler(this.liveViewToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(177, 6);
            // 
            // wireframeToolStripMenuItem
            // 
            this.wireframeToolStripMenuItem.Name = "wireframeToolStripMenuItem";
            this.wireframeToolStripMenuItem.Size = new System.Drawing.Size(74, 20);
            this.wireframeToolStripMenuItem.Text = "Wireframe";
            this.wireframeToolStripMenuItem.Click += new System.EventHandler(this.wireframeViewToolStripMenuItem_Click);
            // 
            // cameraFrustumsToolStripMenuItem
            // 
            this.cameraFrustumsToolStripMenuItem.Name = "cameraFrustumsToolStripMenuItem";
            this.cameraFrustumsToolStripMenuItem.Size = new System.Drawing.Size(112, 20);
            this.cameraFrustumsToolStripMenuItem.Text = "Camera Frustums";
            this.cameraFrustumsToolStripMenuItem.Click += new System.EventHandler(this.cameraFrustumToolStripMenuItem_Click);
            // 
            // projectorFrustumsToolStripMenuItem
            // 
            this.projectorFrustumsToolStripMenuItem.Name = "projectorFrustumsToolStripMenuItem";
            this.projectorFrustumsToolStripMenuItem.Size = new System.Drawing.Size(119, 20);
            this.projectorFrustumsToolStripMenuItem.Text = "Projector Frustums";
            this.projectorFrustumsToolStripMenuItem.Click += new System.EventHandler(this.projectorFrustumToolStripMenuItem_Click);
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.perspectiveAtOriginToolStripMenuItem,
            this.toolStripSeparator3});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "View";
            // 
            // perspectiveAtOriginToolStripMenuItem
            // 
            this.perspectiveAtOriginToolStripMenuItem.Name = "perspectiveAtOriginToolStripMenuItem";
            this.perspectiveAtOriginToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
            this.perspectiveAtOriginToolStripMenuItem.Text = "Perspective";
            this.perspectiveAtOriginToolStripMenuItem.Click += new System.EventHandler(this.perspectiveToolStripMenuItem_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(131, 6);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 24);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.videoPanel1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.consoleTextBox1);
            this.splitContainer1.Size = new System.Drawing.Size(1483, 723);
            this.splitContainer1.SplitterDistance = 988;
            this.splitContainer1.SplitterWidth = 6;
            this.splitContainer1.TabIndex = 2;
            this.splitContainer1.TabStop = false;
            // 
            // videoPanel1
            // 
            this.videoPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.videoPanel1.Location = new System.Drawing.Point(0, 0);
            this.videoPanel1.Name = "videoPanel1";
            this.videoPanel1.Size = new System.Drawing.Size(988, 723);
            this.videoPanel1.TabIndex = 0;
            this.videoPanel1.SizeChanged += new System.EventHandler(this.videoPanel1_SizeChanged);
            // 
            // consoleTextBox1
            // 
            this.consoleTextBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.consoleTextBox1.Location = new System.Drawing.Point(0, 0);
            this.consoleTextBox1.Name = "consoleTextBox1";
            this.consoleTextBox1.Size = new System.Drawing.Size(489, 723);
            this.consoleTextBox1.TabIndex = 0;
            // 
            // solveStepsToolStripMenuItem
            // 
            this.solveStepsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.decodeGrayCodesToolStripMenuItem,
            this.calibrateProjectorGroupsToolStripMenuItem,
            this.optimizePoseToolStripMenuItem});
            this.solveStepsToolStripMenuItem.Name = "solveStepsToolStripMenuItem";
            this.solveStepsToolStripMenuItem.Size = new System.Drawing.Size(233, 22);
            this.solveStepsToolStripMenuItem.Text = "Solve Steps";
            // 
            // decodeGrayCodesToolStripMenuItem
            // 
            this.decodeGrayCodesToolStripMenuItem.Name = "decodeGrayCodesToolStripMenuItem";
            this.decodeGrayCodesToolStripMenuItem.Size = new System.Drawing.Size(213, 22);
            this.decodeGrayCodesToolStripMenuItem.Text = "Decode Gray Code Images";
            this.decodeGrayCodesToolStripMenuItem.Click += new System.EventHandler(this.decodeGrayCodesToolStripMenuItem_Click);
            // 
            // calibrateProjectorGroupsToolStripMenuItem
            // 
            this.calibrateProjectorGroupsToolStripMenuItem.Name = "calibrateProjectorGroupsToolStripMenuItem";
            this.calibrateProjectorGroupsToolStripMenuItem.Size = new System.Drawing.Size(213, 22);
            this.calibrateProjectorGroupsToolStripMenuItem.Text = "Calibrate Projector Groups";
            this.calibrateProjectorGroupsToolStripMenuItem.Click += new System.EventHandler(this.calibrateProjectorGroupsToolStripMenuItem_Click);
            // 
            // optimizePoseToolStripMenuItem
            // 
            this.optimizePoseToolStripMenuItem.Name = "optimizePoseToolStripMenuItem";
            this.optimizePoseToolStripMenuItem.Size = new System.Drawing.Size(213, 22);
            this.optimizePoseToolStripMenuItem.Text = "Optimize Pose";
            this.optimizePoseToolStripMenuItem.Click += new System.EventHandler(this.optimizePoseToolStripMenuItem_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1483, 747);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "CalibrateEnsemble";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem calibrateToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem acquireToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem solveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem reloadToolStripMenuItem;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private ConsoleTextBox consoleTextBox1;
        private VideoPanel videoPanel1;
        private System.Windows.Forms.ToolStripMenuItem renderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem setupToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem discoverCamerasToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem discoverProjectorsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem displayProjectorDisplayIndexesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem displayProjectorNamesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem liveViewToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem perspectiveAtOriginToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem saveToOBJToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripMenuItem acquireDepthAndColorOnlyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem solveStepsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem decodeGrayCodesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem calibrateProjectorGroupsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem optimizePoseToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem wireframeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cameraFrustumsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem projectorFrustumsToolStripMenuItem;
    }
}