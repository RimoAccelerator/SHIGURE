namespace ShigureController
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            btnRefreshDevices = new Button();
            lstDevices = new ListBox();
            lstPlanned = new ListBox();
            lstProcessing = new ListBox();
            lstDone = new ListBox();
            txtCmd = new TextBox();
            btnParse = new Button();
            colorDialog1 = new ColorDialog();
            txtLogs = new RichTextBox();
            btnClearLog = new Button();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            btnStart = new Button();
            btnStop = new Button();
            timer = new System.Windows.Forms.Timer(components);
            SuspendLayout();
            // 
            // btnRefreshDevices
            // 
            btnRefreshDevices.Location = new Point(14, 332);
            btnRefreshDevices.Name = "btnRefreshDevices";
            btnRefreshDevices.Size = new Size(75, 23);
            btnRefreshDevices.TabIndex = 1;
            btnRefreshDevices.Text = "Refresh";
            btnRefreshDevices.UseVisualStyleBackColor = true;
            btnRefreshDevices.Click += btnRefreshDevices_Click_1;
            // 
            // lstDevices
            // 
            lstDevices.FormattingEnabled = true;
            lstDevices.ItemHeight = 17;
            lstDevices.Location = new Point(12, 33);
            lstDevices.Name = "lstDevices";
            lstDevices.Size = new Size(195, 293);
            lstDevices.TabIndex = 2;
            // 
            // lstPlanned
            // 
            lstPlanned.FormattingEnabled = true;
            lstPlanned.ItemHeight = 17;
            lstPlanned.Location = new Point(233, 31);
            lstPlanned.Name = "lstPlanned";
            lstPlanned.Size = new Size(239, 89);
            lstPlanned.TabIndex = 3;
            // 
            // lstProcessing
            // 
            lstProcessing.FormattingEnabled = true;
            lstProcessing.ItemHeight = 17;
            lstProcessing.Location = new Point(233, 148);
            lstProcessing.Name = "lstProcessing";
            lstProcessing.Size = new Size(239, 55);
            lstProcessing.TabIndex = 4;
            // 
            // lstDone
            // 
            lstDone.FormattingEnabled = true;
            lstDone.ItemHeight = 17;
            lstDone.Location = new Point(233, 237);
            lstDone.Name = "lstDone";
            lstDone.Size = new Size(239, 89);
            lstDone.TabIndex = 5;
            // 
            // txtCmd
            // 
            txtCmd.Location = new Point(503, 31);
            txtCmd.Multiline = true;
            txtCmd.Name = "txtCmd";
            txtCmd.ScrollBars = ScrollBars.Both;
            txtCmd.Size = new Size(192, 293);
            txtCmd.TabIndex = 6;
            // 
            // btnParse
            // 
            btnParse.Location = new Point(701, 31);
            btnParse.Name = "btnParse";
            btnParse.Size = new Size(75, 23);
            btnParse.TabIndex = 7;
            btnParse.Text = "Parse";
            btnParse.UseVisualStyleBackColor = true;
            btnParse.Click += btnParse_Click;
            // 
            // txtLogs
            // 
            txtLogs.Location = new Point(14, 361);
            txtLogs.Name = "txtLogs";
            txtLogs.ReadOnly = true;
            txtLogs.Size = new Size(681, 129);
            txtLogs.TabIndex = 9;
            txtLogs.Text = "";
            // 
            // btnClearLog
            // 
            btnClearLog.Location = new Point(701, 361);
            btnClearLog.Name = "btnClearLog";
            btnClearLog.Size = new Size(75, 23);
            btnClearLog.TabIndex = 10;
            btnClearLog.Text = "Clear";
            btnClearLog.UseVisualStyleBackColor = true;
            btnClearLog.Click += btnClearLog_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(14, 9);
            label1.Name = "label1";
            label1.Size = new Size(52, 17);
            label1.TabIndex = 11;
            label1.Text = "Devices";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(233, 9);
            label2.Name = "label2";
            label2.Size = new Size(95, 17);
            label2.TabIndex = 12;
            label2.Text = "Planned events";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(233, 128);
            label3.Name = "label3";
            label3.Size = new Size(112, 17);
            label3.TabIndex = 13;
            label3.Text = "Processing events";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(233, 217);
            label4.Name = "label4";
            label4.Size = new Size(96, 17);
            label4.TabIndex = 14;
            label4.Text = "Finished events";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(503, 11);
            label5.Name = "label5";
            label5.Size = new Size(74, 17);
            label5.TabIndex = 15;
            label5.Text = "Commands";
            // 
            // btnStart
            // 
            btnStart.Location = new Point(701, 60);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(75, 23);
            btnStart.TabIndex = 17;
            btnStart.Text = "Start";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            // 
            // btnStop
            // 
            btnStop.Location = new Point(701, 93);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(75, 23);
            btnStop.TabIndex = 18;
            btnStop.Text = "Stop";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;
            // 
            // timer
            // 
            timer.Interval = 500;
            timer.Tick += timer_Tick;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 502);
            Controls.Add(btnStop);
            Controls.Add(btnStart);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(btnClearLog);
            Controls.Add(txtLogs);
            Controls.Add(btnParse);
            Controls.Add(txtCmd);
            Controls.Add(lstDone);
            Controls.Add(lstProcessing);
            Controls.Add(lstPlanned);
            Controls.Add(lstDevices);
            Controls.Add(btnRefreshDevices);
            Name = "Form1";
            Text = "ShigureController";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Button btnRefreshDevices;
        private ListBox lstDevices;
        private ListBox lstPlanned;
        private ListBox lstProcessing;
        private ListBox lstDone;
        private TextBox txtCmd;
        private Button btnParse;
        private ColorDialog colorDialog1;
        private RichTextBox txtLogs;
        private Button btnClearLog;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private Button btnStart;
        private Button btnStop;
        private System.Windows.Forms.Timer timer;
    }
}
