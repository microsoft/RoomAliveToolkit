namespace KinectV2Server
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.panelDisplay = new System.Windows.Forms.Panel();
            this.comboBoxDisplay = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.labelDepthFPS = new System.Windows.Forms.Label();
            this.labelBodies = new System.Windows.Forms.Label();
            this.checkBoxSkeleton = new System.Windows.Forms.CheckBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.labelConfig = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.buttonKillAll = new System.Windows.Forms.Button();
            this.labelAudio = new System.Windows.Forms.Label();
            this.labelSkeleton = new System.Windows.Forms.Label();
            this.labelColor = new System.Windows.Forms.Label();
            this.labelDepth = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label22 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.labelColorFPS = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.comboBoxStreamType = new System.Windows.Forms.ComboBox();
            this.consoleTextBox1 = new ConsoleTextBox.ConsoleTextBox();
            this.trackBarThreshold = new System.Windows.Forms.TrackBar();
            this.labelThreshold = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.checkBoxStreamColor = new System.Windows.Forms.CheckBox();
            this.checkBoxStreamAudio = new System.Windows.Forms.CheckBox();
            this.label12 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.labelUpTime = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.comboBoxColorCompression = new System.Windows.Forms.ComboBox();
            this.checkBoxEncoderTiming = new System.Windows.Forms.CheckBox();
            this.checkBoxProcessRAW = new System.Windows.Forms.CheckBox();
            this.checkBoxRenderFaceTracking = new System.Windows.Forms.CheckBox();
            this.checkBoxFlip = new System.Windows.Forms.CheckBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.backgroundToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.acquireBackgroundToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveBackgroundToOBJFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.label13 = new System.Windows.Forms.Label();
            this.checkBoxBlur = new System.Windows.Forms.CheckBox();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarThreshold)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelDisplay
            // 
            this.panelDisplay.Location = new System.Drawing.Point(15, 162);
            this.panelDisplay.Name = "panelDisplay";
            this.panelDisplay.Size = new System.Drawing.Size(512, 424);
            this.panelDisplay.TabIndex = 1;
            // 
            // comboBoxDisplay
            // 
            this.comboBoxDisplay.FormattingEnabled = true;
            this.comboBoxDisplay.Items.AddRange(new object[] {
            "Depth",
            "Color",
            "InfraRed",
            "Depth Foreground",
            "Depth Background",
            "Body Index"});
            this.comboBoxDisplay.Location = new System.Drawing.Point(14, 58);
            this.comboBoxDisplay.Name = "comboBoxDisplay";
            this.comboBoxDisplay.Size = new System.Drawing.Size(165, 21);
            this.comboBoxDisplay.TabIndex = 2;
            this.comboBoxDisplay.Text = "Depth";
            this.comboBoxDisplay.SelectedIndexChanged += new System.EventHandler(this.comboBoxDisplay_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(47, 30);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(76, 16);
            this.label1.TabIndex = 3;
            this.label1.Text = "Depth FPS ";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(47, 67);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(51, 16);
            this.label4.TabIndex = 6;
            this.label4.Text = "Bodies";
            // 
            // labelDepthFPS
            // 
            this.labelDepthFPS.AutoSize = true;
            this.labelDepthFPS.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDepthFPS.Location = new System.Drawing.Point(150, 29);
            this.labelDepthFPS.Name = "labelDepthFPS";
            this.labelDepthFPS.Size = new System.Drawing.Size(15, 16);
            this.labelDepthFPS.TabIndex = 7;
            this.labelDepthFPS.Text = "0";
            // 
            // labelBodies
            // 
            this.labelBodies.AutoSize = true;
            this.labelBodies.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelBodies.Location = new System.Drawing.Point(150, 67);
            this.labelBodies.Name = "labelBodies";
            this.labelBodies.Size = new System.Drawing.Size(15, 16);
            this.labelBodies.TabIndex = 10;
            this.labelBodies.Text = "0";
            // 
            // checkBoxSkeleton
            // 
            this.checkBoxSkeleton.AutoSize = true;
            this.checkBoxSkeleton.Checked = true;
            this.checkBoxSkeleton.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxSkeleton.Location = new System.Drawing.Point(15, 85);
            this.checkBoxSkeleton.Name = "checkBoxSkeleton";
            this.checkBoxSkeleton.Size = new System.Drawing.Size(106, 17);
            this.checkBoxSkeleton.TabIndex = 12;
            this.checkBoxSkeleton.Text = "Render Skeleton";
            this.checkBoxSkeleton.UseVisualStyleBackColor = true;
            this.checkBoxSkeleton.CheckedChanged += new System.EventHandler(this.checkBoxSkeleton_CheckedChanged);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.labelConfig);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.buttonKillAll);
            this.panel1.Controls.Add(this.labelAudio);
            this.panel1.Controls.Add(this.labelSkeleton);
            this.panel1.Controls.Add(this.labelColor);
            this.panel1.Controls.Add(this.labelDepth);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.label16);
            this.panel1.Controls.Add(this.label10);
            this.panel1.Controls.Add(this.label22);
            this.panel1.Controls.Add(this.label9);
            this.panel1.Controls.Add(this.label8);
            this.panel1.Controls.Add(this.labelColorFPS);
            this.panel1.Controls.Add(this.label7);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.labelBodies);
            this.panel1.Controls.Add(this.labelDepthFPS);
            this.panel1.Location = new System.Drawing.Point(15, 587);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(512, 121);
            this.panel1.TabIndex = 14;
            // 
            // labelConfig
            // 
            this.labelConfig.AutoSize = true;
            this.labelConfig.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelConfig.Location = new System.Drawing.Point(396, 94);
            this.labelConfig.Name = "labelConfig";
            this.labelConfig.Size = new System.Drawing.Size(15, 16);
            this.labelConfig.TabIndex = 27;
            this.labelConfig.Text = "0";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(322, 94);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(46, 16);
            this.label3.TabIndex = 26;
            this.label3.Text = "Config";
            // 
            // buttonKillAll
            // 
            this.buttonKillAll.Location = new System.Drawing.Point(399, 4);
            this.buttonKillAll.Name = "buttonKillAll";
            this.buttonKillAll.Size = new System.Drawing.Size(79, 23);
            this.buttonKillAll.TabIndex = 25;
            this.buttonKillAll.Text = "Kill all clients";
            this.buttonKillAll.UseVisualStyleBackColor = true;
            this.buttonKillAll.Click += new System.EventHandler(this.buttonKillAll_Click);
            // 
            // labelAudio
            // 
            this.labelAudio.AutoSize = true;
            this.labelAudio.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelAudio.Location = new System.Drawing.Point(396, 78);
            this.labelAudio.Name = "labelAudio";
            this.labelAudio.Size = new System.Drawing.Size(15, 16);
            this.labelAudio.TabIndex = 24;
            this.labelAudio.Text = "0";
            // 
            // labelSkeleton
            // 
            this.labelSkeleton.AutoSize = true;
            this.labelSkeleton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelSkeleton.Location = new System.Drawing.Point(396, 62);
            this.labelSkeleton.Name = "labelSkeleton";
            this.labelSkeleton.Size = new System.Drawing.Size(15, 16);
            this.labelSkeleton.TabIndex = 22;
            this.labelSkeleton.Text = "0";
            // 
            // labelColor
            // 
            this.labelColor.AutoSize = true;
            this.labelColor.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelColor.Location = new System.Drawing.Point(396, 46);
            this.labelColor.Name = "labelColor";
            this.labelColor.Size = new System.Drawing.Size(15, 16);
            this.labelColor.TabIndex = 21;
            this.labelColor.Text = "0";
            // 
            // labelDepth
            // 
            this.labelDepth.AutoSize = true;
            this.labelDepth.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDepth.Location = new System.Drawing.Point(396, 30);
            this.labelDepth.Name = "labelDepth";
            this.labelDepth.Size = new System.Drawing.Size(15, 16);
            this.labelDepth.TabIndex = 20;
            this.labelDepth.Text = "0";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(322, 78);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(43, 16);
            this.label2.TabIndex = 19;
            this.label2.Text = "Audio";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label16.Location = new System.Drawing.Point(322, 62);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(61, 16);
            this.label16.TabIndex = 17;
            this.label16.Text = "Skeleton";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.Location = new System.Drawing.Point(322, 46);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(40, 16);
            this.label10.TabIndex = 16;
            this.label10.Text = "Color";
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label22.Location = new System.Drawing.Point(322, 30);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(47, 16);
            this.label22.TabIndex = 15;
            this.label22.Text = "Depth ";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(322, 7);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(55, 16);
            this.label9.TabIndex = 14;
            this.label9.Text = "Clients";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.Location = new System.Drawing.Point(48, 7);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(50, 16);
            this.label8.TabIndex = 13;
            this.label8.Text = "Kinect";
            // 
            // labelColorFPS
            // 
            this.labelColorFPS.AutoSize = true;
            this.labelColorFPS.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelColorFPS.Location = new System.Drawing.Point(150, 48);
            this.labelColorFPS.Name = "labelColorFPS";
            this.labelColorFPS.Size = new System.Drawing.Size(15, 16);
            this.labelColorFPS.TabIndex = 12;
            this.labelColorFPS.Text = "0";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(47, 48);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(72, 16);
            this.label7.TabIndex = 11;
            this.label7.Text = "Color FPS ";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(230, 39);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(78, 16);
            this.label5.TabIndex = 15;
            this.label5.Text = "Streaming";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(12, 39);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(80, 16);
            this.label6.TabIndex = 16;
            this.label6.Text = "Rendering";
            // 
            // comboBoxStreamType
            // 
            this.comboBoxStreamType.FormattingEnabled = true;
            this.comboBoxStreamType.Items.AddRange(new object[] {
            "Foreground Depth",
            "Full Depth",
            "Body Index Depth"});
            this.comboBoxStreamType.Location = new System.Drawing.Point(233, 58);
            this.comboBoxStreamType.Name = "comboBoxStreamType";
            this.comboBoxStreamType.Size = new System.Drawing.Size(137, 21);
            this.comboBoxStreamType.TabIndex = 17;
            this.comboBoxStreamType.SelectedIndexChanged += new System.EventHandler(this.comboBoxStreamType_SelectedIndexChanged);
            // 
            // consoleTextBox1
            // 
            this.consoleTextBox1.Location = new System.Drawing.Point(533, 162);
            this.consoleTextBox1.Name = "consoleTextBox1";
            this.consoleTextBox1.Size = new System.Drawing.Size(640, 514);
            this.consoleTextBox1.TabIndex = 0;
            // 
            // trackBarThreshold
            // 
            this.trackBarThreshold.Location = new System.Drawing.Point(796, 77);
            this.trackBarThreshold.Maximum = 300;
            this.trackBarThreshold.Name = "trackBarThreshold";
            this.trackBarThreshold.Size = new System.Drawing.Size(372, 45);
            this.trackBarThreshold.TabIndex = 19;
            this.trackBarThreshold.TickFrequency = 5;
            this.trackBarThreshold.Scroll += new System.EventHandler(this.trackBarThreshold_Scroll);
            // 
            // labelThreshold
            // 
            this.labelThreshold.AutoSize = true;
            this.labelThreshold.Location = new System.Drawing.Point(881, 60);
            this.labelThreshold.Name = "labelThreshold";
            this.labelThreshold.Size = new System.Drawing.Size(35, 13);
            this.labelThreshold.TabIndex = 20;
            this.labelThreshold.Text = "label7";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.Location = new System.Drawing.Point(793, 39);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(194, 16);
            this.label11.TabIndex = 21;
            this.label11.Text = "Background Segmentation:";
            // 
            // checkBoxStreamColor
            // 
            this.checkBoxStreamColor.AutoSize = true;
            this.checkBoxStreamColor.Location = new System.Drawing.Point(433, 85);
            this.checkBoxStreamColor.Name = "checkBoxStreamColor";
            this.checkBoxStreamColor.Size = new System.Drawing.Size(128, 17);
            this.checkBoxStreamColor.TabIndex = 22;
            this.checkBoxStreamColor.Text = "Process Color Frames";
            this.checkBoxStreamColor.UseVisualStyleBackColor = true;
            this.checkBoxStreamColor.CheckedChanged += new System.EventHandler(this.checkBoxStreamColor_CheckedChanged);
            // 
            // checkBoxStreamAudio
            // 
            this.checkBoxStreamAudio.AutoSize = true;
            this.checkBoxStreamAudio.Checked = true;
            this.checkBoxStreamAudio.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxStreamAudio.Location = new System.Drawing.Point(233, 85);
            this.checkBoxStreamAudio.Name = "checkBoxStreamAudio";
            this.checkBoxStreamAudio.Size = new System.Drawing.Size(89, 17);
            this.checkBoxStreamAudio.TabIndex = 23;
            this.checkBoxStreamAudio.Text = "Stream Audio";
            this.checkBoxStreamAudio.UseVisualStyleBackColor = true;
            this.checkBoxStreamAudio.CheckedChanged += new System.EventHandler(this.checkBoxStreamAudio_CheckedChanged);
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(793, 60);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(82, 13);
            this.label12.TabIndex = 24;
            this.label12.Text = "Threshold (mm):";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(993, 684);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(84, 13);
            this.label14.TabIndex = 32;
            this.label14.Text = "Server Up Time:";
            // 
            // labelUpTime
            // 
            this.labelUpTime.AutoSize = true;
            this.labelUpTime.Location = new System.Drawing.Point(1083, 684);
            this.labelUpTime.Name = "labelUpTime";
            this.labelUpTime.Size = new System.Drawing.Size(28, 13);
            this.labelUpTime.TabIndex = 33;
            this.labelUpTime.Text = "error";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label15.Location = new System.Drawing.Point(430, 39);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(86, 16);
            this.label15.TabIndex = 34;
            this.label15.Text = "Processing";
            // 
            // comboBoxColorCompression
            // 
            this.comboBoxColorCompression.FormattingEnabled = true;
            this.comboBoxColorCompression.Items.AddRange(new object[] {
            "NONE",
            "JPEG"});
            this.comboBoxColorCompression.Location = new System.Drawing.Point(533, 57);
            this.comboBoxColorCompression.Name = "comboBoxColorCompression";
            this.comboBoxColorCompression.Size = new System.Drawing.Size(121, 21);
            this.comboBoxColorCompression.TabIndex = 35;
            this.comboBoxColorCompression.SelectedIndexChanged += new System.EventHandler(this.comboBoxColorCompression_SelectedIndexChanged);
            // 
            // checkBoxEncoderTiming
            // 
            this.checkBoxEncoderTiming.AutoSize = true;
            this.checkBoxEncoderTiming.Location = new System.Drawing.Point(533, 680);
            this.checkBoxEncoderTiming.Name = "checkBoxEncoderTiming";
            this.checkBoxEncoderTiming.Size = new System.Drawing.Size(142, 17);
            this.checkBoxEncoderTiming.TabIndex = 36;
            this.checkBoxEncoderTiming.Text = "Show Timing Information";
            this.checkBoxEncoderTiming.UseVisualStyleBackColor = true;
            this.checkBoxEncoderTiming.CheckedChanged += new System.EventHandler(this.checkBoxEncoderTiming_CheckedChanged);
            // 
            // checkBoxProcessRAW
            // 
            this.checkBoxProcessRAW.AutoSize = true;
            this.checkBoxProcessRAW.Location = new System.Drawing.Point(233, 105);
            this.checkBoxProcessRAW.Name = "checkBoxProcessRAW";
            this.checkBoxProcessRAW.Size = new System.Drawing.Size(115, 17);
            this.checkBoxProcessRAW.TabIndex = 37;
            this.checkBoxProcessRAW.Text = "Stream RAW Color";
            this.checkBoxProcessRAW.UseVisualStyleBackColor = true;
            this.checkBoxProcessRAW.CheckedChanged += new System.EventHandler(this.checkBoxProcessRAW_CheckedChanged);
            // 
            // checkBoxRenderFaceTracking
            // 
            this.checkBoxRenderFaceTracking.AutoSize = true;
            this.checkBoxRenderFaceTracking.Checked = true;
            this.checkBoxRenderFaceTracking.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxRenderFaceTracking.Location = new System.Drawing.Point(15, 105);
            this.checkBoxRenderFaceTracking.Name = "checkBoxRenderFaceTracking";
            this.checkBoxRenderFaceTracking.Size = new System.Drawing.Size(133, 17);
            this.checkBoxRenderFaceTracking.TabIndex = 38;
            this.checkBoxRenderFaceTracking.Text = "Render Face Tracking";
            this.checkBoxRenderFaceTracking.UseVisualStyleBackColor = true;
            this.checkBoxRenderFaceTracking.CheckedChanged += new System.EventHandler(this.checkBoxRenderFaceTracking_CheckedChanged);
            // 
            // checkBoxFlip
            // 
            this.checkBoxFlip.AutoSize = true;
            this.checkBoxFlip.Location = new System.Drawing.Point(433, 105);
            this.checkBoxFlip.Name = "checkBoxFlip";
            this.checkBoxFlip.Size = new System.Drawing.Size(79, 17);
            this.checkBoxFlip.TabIndex = 39;
            this.checkBoxFlip.Text = "Flip Images";
            this.checkBoxFlip.UseVisualStyleBackColor = true;
            this.checkBoxFlip.CheckedChanged += new System.EventHandler(this.checkBoxFlip_CheckedChanged);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.backgroundToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1180, 24);
            this.menuStrip1.TabIndex = 40;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveSettingsToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // saveSettingsToolStripMenuItem
            // 
            this.saveSettingsToolStripMenuItem.Name = "saveSettingsToolStripMenuItem";
            this.saveSettingsToolStripMenuItem.Size = new System.Drawing.Size(143, 22);
            this.saveSettingsToolStripMenuItem.Text = "Save Settings";
            this.saveSettingsToolStripMenuItem.Click += new System.EventHandler(this.saveSettingsToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(143, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // backgroundToolStripMenuItem
            // 
            this.backgroundToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.acquireBackgroundToolStripMenuItem,
            this.saveBackgroundToOBJFileToolStripMenuItem});
            this.backgroundToolStripMenuItem.Name = "backgroundToolStripMenuItem";
            this.backgroundToolStripMenuItem.Size = new System.Drawing.Size(83, 20);
            this.backgroundToolStripMenuItem.Text = "Background";
            // 
            // acquireBackgroundToolStripMenuItem
            // 
            this.acquireBackgroundToolStripMenuItem.Name = "acquireBackgroundToolStripMenuItem";
            this.acquireBackgroundToolStripMenuItem.Size = new System.Drawing.Size(223, 22);
            this.acquireBackgroundToolStripMenuItem.Text = "Acquire Background";
            this.acquireBackgroundToolStripMenuItem.Click += new System.EventHandler(this.buttonBackground_Click);
            // 
            // saveBackgroundToOBJFileToolStripMenuItem
            // 
            this.saveBackgroundToOBJFileToolStripMenuItem.Name = "saveBackgroundToOBJFileToolStripMenuItem";
            this.saveBackgroundToOBJFileToolStripMenuItem.Size = new System.Drawing.Size(223, 22);
            this.saveBackgroundToOBJFileToolStripMenuItem.Text = "Save Background to OBJ File";
            this.saveBackgroundToOBJFileToolStripMenuItem.Click += new System.EventHandler(this.saveToOBJToolStripMenuItem_Click);
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(430, 61);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(97, 13);
            this.label13.TabIndex = 41;
            this.label13.Text = "Color Compression:";
            // 
            // checkBoxBlur
            // 
            this.checkBoxBlur.AutoSize = true;
            this.checkBoxBlur.Location = new System.Drawing.Point(433, 126);
            this.checkBoxBlur.Name = "checkBoxBlur";
            this.checkBoxBlur.Size = new System.Drawing.Size(133, 17);
            this.checkBoxBlur.TabIndex = 42;
            this.checkBoxBlur.Text = "Blur Foreground Depth";
            this.checkBoxBlur.UseVisualStyleBackColor = true;
            this.checkBoxBlur.CheckedChanged += new System.EventHandler(this.checkBoxBlur_CheckedChanged);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1180, 713);
            this.Controls.Add(this.checkBoxBlur);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.checkBoxFlip);
            this.Controls.Add(this.checkBoxRenderFaceTracking);
            this.Controls.Add(this.checkBoxProcessRAW);
            this.Controls.Add(this.checkBoxEncoderTiming);
            this.Controls.Add(this.comboBoxColorCompression);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.labelUpTime);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.checkBoxStreamAudio);
            this.Controls.Add(this.checkBoxStreamColor);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.labelThreshold);
            this.Controls.Add(this.trackBarThreshold);
            this.Controls.Add(this.comboBoxStreamType);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.checkBoxSkeleton);
            this.Controls.Add(this.comboBoxDisplay);
            this.Controls.Add(this.panelDisplay);
            this.Controls.Add(this.consoleTextBox1);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "KinectV2Server";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarThreshold)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ConsoleTextBox.ConsoleTextBox consoleTextBox1;
        private System.Windows.Forms.Panel panelDisplay;
        private System.Windows.Forms.ComboBox comboBoxDisplay;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label labelDepthFPS;
        private System.Windows.Forms.Label labelBodies;
        private System.Windows.Forms.CheckBox checkBoxSkeleton;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox comboBoxStreamType;
        private System.Windows.Forms.TrackBar trackBarThreshold;
        private System.Windows.Forms.Label labelThreshold;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label labelColorFPS;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.CheckBox checkBoxStreamColor;
        private System.Windows.Forms.CheckBox checkBoxStreamAudio;
        //private System.Windows.Forms.CheckBox checkBoxTrackGlasses;
        private System.Windows.Forms.Label label12;
        //private System.Windows.Forms.Button CaptureFirstCameraImage;
        //private System.Windows.Forms.Button CaptureSecondCameraImage;
        //private System.Windows.Forms.Button CalibrateTwoCamerasRelativePose;
        //private System.Windows.Forms.Button VerifyCalibratedFacingCameras;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label labelUpTime;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.ComboBox comboBoxColorCompression;
        private System.Windows.Forms.Label labelAudio;
        private System.Windows.Forms.Label labelSkeleton;
        private System.Windows.Forms.Label labelColor;
        private System.Windows.Forms.Label labelDepth;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.CheckBox checkBoxEncoderTiming;
        private System.Windows.Forms.CheckBox checkBoxProcessRAW;
        private System.Windows.Forms.Button buttonKillAll;
        private System.Windows.Forms.Label labelConfig;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox checkBoxRenderFaceTracking;
        private System.Windows.Forms.CheckBox checkBoxFlip;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveSettingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem backgroundToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem acquireBackgroundToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveBackgroundToOBJFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.CheckBox checkBoxBlur;
        //private System.Windows.Forms.Button ClickToTrack;
        //private System.Windows.Forms.CheckBox checkBoxStreamGlassesPose;
    }
}

