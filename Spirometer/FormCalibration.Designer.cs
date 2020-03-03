namespace Spirometer
{
    partial class FormCalibration
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormCalibration));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.plotViewPS = new OxyPlot.WindowsForms.PlotView();
            this.plotViewKP = new OxyPlot.WindowsForms.PlotView();
            this.plotViewPT = new OxyPlot.WindowsForms.PlotView();
            this.panel1 = new System.Windows.Forms.Panel();
            this.dataGridViewSampleInfo = new System.Windows.Forms.DataGridView();
            this.Column0 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column6 = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.dataGridViewResult = new System.Windows.Forms.DataGridView();
            this.Column7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column8 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButtonStart = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonSavePresure = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonLoadPresure = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonClear = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonCalcCaliParam = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonApply = new System.Windows.Forms.ToolStripButton();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabelSampleCount = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabelParamCount = new System.Windows.Forms.ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewSampleInfo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewResult)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 25);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.plotViewPT);
            this.splitContainer1.Size = new System.Drawing.Size(1372, 486);
            this.splitContainer1.SplitterDistance = 229;
            this.splitContainer1.SplitterWidth = 2;
            this.splitContainer1.TabIndex = 0;
            // 
            // splitContainer2
            // 
            this.splitContainer2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.plotViewPS);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.plotViewKP);
            this.splitContainer2.Size = new System.Drawing.Size(1372, 229);
            this.splitContainer2.SplitterDistance = 698;
            this.splitContainer2.SplitterWidth = 1;
            this.splitContainer2.TabIndex = 1;
            // 
            // plotViewPS
            // 
            this.plotViewPS.Dock = System.Windows.Forms.DockStyle.Fill;
            this.plotViewPS.Location = new System.Drawing.Point(0, 0);
            this.plotViewPS.Name = "plotViewPS";
            this.plotViewPS.PanCursor = System.Windows.Forms.Cursors.Hand;
            this.plotViewPS.Size = new System.Drawing.Size(694, 225);
            this.plotViewPS.TabIndex = 0;
            this.plotViewPS.Text = "plotViewFV";
            this.plotViewPS.ZoomHorizontalCursor = System.Windows.Forms.Cursors.SizeWE;
            this.plotViewPS.ZoomRectangleCursor = System.Windows.Forms.Cursors.SizeNWSE;
            this.plotViewPS.ZoomVerticalCursor = System.Windows.Forms.Cursors.SizeNS;
            // 
            // plotViewKP
            // 
            this.plotViewKP.Dock = System.Windows.Forms.DockStyle.Fill;
            this.plotViewKP.Location = new System.Drawing.Point(0, 0);
            this.plotViewKP.Name = "plotViewKP";
            this.plotViewKP.PanCursor = System.Windows.Forms.Cursors.Hand;
            this.plotViewKP.Size = new System.Drawing.Size(669, 225);
            this.plotViewKP.TabIndex = 0;
            this.plotViewKP.Text = "plotView1";
            this.plotViewKP.ZoomHorizontalCursor = System.Windows.Forms.Cursors.SizeWE;
            this.plotViewKP.ZoomRectangleCursor = System.Windows.Forms.Cursors.SizeNWSE;
            this.plotViewKP.ZoomVerticalCursor = System.Windows.Forms.Cursors.SizeNS;
            // 
            // plotViewPT
            // 
            this.plotViewPT.Dock = System.Windows.Forms.DockStyle.Fill;
            this.plotViewPT.Location = new System.Drawing.Point(0, 0);
            this.plotViewPT.Name = "plotViewPT";
            this.plotViewPT.PanCursor = System.Windows.Forms.Cursors.Hand;
            this.plotViewPT.Size = new System.Drawing.Size(1368, 251);
            this.plotViewPT.TabIndex = 0;
            this.plotViewPT.Text = "plotViewVFT";
            this.plotViewPT.ZoomHorizontalCursor = System.Windows.Forms.Cursors.SizeWE;
            this.plotViewPT.ZoomRectangleCursor = System.Windows.Forms.Cursors.SizeNWSE;
            this.plotViewPT.ZoomVerticalCursor = System.Windows.Forms.Cursors.SizeNS;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.dataGridViewSampleInfo);
            this.panel1.Controls.Add(this.dataGridViewResult);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 511);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1372, 216);
            this.panel1.TabIndex = 1;
            // 
            // dataGridViewSampleInfo
            // 
            this.dataGridViewSampleInfo.AllowUserToAddRows = false;
            this.dataGridViewSampleInfo.AllowUserToDeleteRows = false;
            this.dataGridViewSampleInfo.BackgroundColor = System.Drawing.SystemColors.ControlLightLight;
            this.dataGridViewSampleInfo.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewSampleInfo.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column0,
            this.Column1,
            this.Column2,
            this.Column3,
            this.Column4,
            this.Column5,
            this.Column6});
            this.dataGridViewSampleInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewSampleInfo.Location = new System.Drawing.Point(0, 0);
            this.dataGridViewSampleInfo.MultiSelect = false;
            this.dataGridViewSampleInfo.Name = "dataGridViewSampleInfo";
            this.dataGridViewSampleInfo.RowTemplate.Height = 23;
            this.dataGridViewSampleInfo.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridViewSampleInfo.Size = new System.Drawing.Size(865, 216);
            this.dataGridViewSampleInfo.TabIndex = 0;
            // 
            // Column0
            // 
            this.Column0.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Column0.HeaderText = "方向";
            this.Column0.Name = "Column0";
            this.Column0.ReadOnly = true;
            // 
            // Column1
            // 
            this.Column1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Column1.HeaderText = "平均系数";
            this.Column1.Name = "Column1";
            this.Column1.ReadOnly = true;
            // 
            // Column2
            // 
            this.Column2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Column2.HeaderText = "压差平均值";
            this.Column2.Name = "Column2";
            this.Column2.ReadOnly = true;
            // 
            // Column3
            // 
            this.Column3.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Column3.HeaderText = "压差求和值";
            this.Column3.Name = "Column3";
            this.Column3.ReadOnly = true;
            // 
            // Column4
            // 
            this.Column4.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Column4.HeaderText = "压差最大值";
            this.Column4.Name = "Column4";
            this.Column4.ReadOnly = true;
            // 
            // Column5
            // 
            this.Column5.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Column5.HeaderText = "样本方差";
            this.Column5.Name = "Column5";
            this.Column5.ReadOnly = true;
            // 
            // Column6
            // 
            this.Column6.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Column6.HeaderText = "使用样本";
            this.Column6.Name = "Column6";
            // 
            // dataGridViewResult
            // 
            this.dataGridViewResult.AllowUserToAddRows = false;
            this.dataGridViewResult.AllowUserToDeleteRows = false;
            this.dataGridViewResult.BackgroundColor = System.Drawing.SystemColors.ControlLightLight;
            this.dataGridViewResult.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewResult.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column7,
            this.Column8});
            this.dataGridViewResult.Dock = System.Windows.Forms.DockStyle.Right;
            this.dataGridViewResult.Location = new System.Drawing.Point(865, 0);
            this.dataGridViewResult.MultiSelect = false;
            this.dataGridViewResult.Name = "dataGridViewResult";
            this.dataGridViewResult.ReadOnly = true;
            this.dataGridViewResult.RowTemplate.Height = 23;
            this.dataGridViewResult.Size = new System.Drawing.Size(507, 216);
            this.dataGridViewResult.TabIndex = 1;
            // 
            // Column7
            // 
            this.Column7.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Column7.HeaderText = "压差";
            this.Column7.Name = "Column7";
            this.Column7.ReadOnly = true;
            // 
            // Column8
            // 
            this.Column8.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Column8.HeaderText = "系数";
            this.Column8.Name = "Column8";
            this.Column8.ReadOnly = true;
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonStart,
            this.toolStripSeparator1,
            this.toolStripButtonSavePresure,
            this.toolStripButtonLoadPresure,
            this.toolStripButtonClear,
            this.toolStripSeparator2,
            this.toolStripButtonCalcCaliParam,
            this.toolStripButtonApply});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1372, 25);
            this.toolStrip1.TabIndex = 2;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButtonStart
            // 
            this.toolStripButtonStart.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonStart.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonStart.Image")));
            this.toolStripButtonStart.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonStart.Name = "toolStripButtonStart";
            this.toolStripButtonStart.Size = new System.Drawing.Size(36, 22);
            this.toolStripButtonStart.Text = "开始";
            this.toolStripButtonStart.Click += new System.EventHandler(this.toolStripButtonStart_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonSavePresure
            // 
            this.toolStripButtonSavePresure.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonSavePresure.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonSavePresure.Image")));
            this.toolStripButtonSavePresure.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSavePresure.Name = "toolStripButtonSavePresure";
            this.toolStripButtonSavePresure.Size = new System.Drawing.Size(84, 22);
            this.toolStripButtonSavePresure.Text = "保存压差数据";
            this.toolStripButtonSavePresure.Click += new System.EventHandler(this.toolStripButtonSavePresure_Click);
            // 
            // toolStripButtonLoadPresure
            // 
            this.toolStripButtonLoadPresure.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonLoadPresure.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonLoadPresure.Image")));
            this.toolStripButtonLoadPresure.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonLoadPresure.Name = "toolStripButtonLoadPresure";
            this.toolStripButtonLoadPresure.Size = new System.Drawing.Size(84, 22);
            this.toolStripButtonLoadPresure.Text = "加载压差数据";
            this.toolStripButtonLoadPresure.Click += new System.EventHandler(this.toolStripButtonLoadPresure_Click);
            // 
            // toolStripButtonClear
            // 
            this.toolStripButtonClear.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonClear.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonClear.Image")));
            this.toolStripButtonClear.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonClear.Name = "toolStripButtonClear";
            this.toolStripButtonClear.Size = new System.Drawing.Size(84, 22);
            this.toolStripButtonClear.Text = "清除校准数据";
            this.toolStripButtonClear.ToolTipText = "清除数据";
            this.toolStripButtonClear.Click += new System.EventHandler(this.toolStripButtonClear_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonCalcCaliParam
            // 
            this.toolStripButtonCalcCaliParam.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonCalcCaliParam.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonCalcCaliParam.Image")));
            this.toolStripButtonCalcCaliParam.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonCalcCaliParam.Name = "toolStripButtonCalcCaliParam";
            this.toolStripButtonCalcCaliParam.Size = new System.Drawing.Size(84, 22);
            this.toolStripButtonCalcCaliParam.Text = "计算校准参数";
            this.toolStripButtonCalcCaliParam.Click += new System.EventHandler(this.toolStripButtonCalcCaliParam_Click);
            // 
            // toolStripButtonApply
            // 
            this.toolStripButtonApply.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonApply.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonApply.Image")));
            this.toolStripButtonApply.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonApply.Name = "toolStripButtonApply";
            this.toolStripButtonApply.Size = new System.Drawing.Size(84, 22);
            this.toolStripButtonApply.Text = "应用校准参数";
            this.toolStripButtonApply.Click += new System.EventHandler(this.toolStripButtonApply_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripStatusLabelSampleCount,
            this.toolStripStatusLabel2,
            this.toolStripStatusLabelParamCount});
            this.statusStrip1.Location = new System.Drawing.Point(0, 727);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1372, 22);
            this.statusStrip1.TabIndex = 3;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(59, 17);
            this.toolStripStatusLabel1.Text = "样本数量:";
            // 
            // toolStripStatusLabelSampleCount
            // 
            this.toolStripStatusLabelSampleCount.Name = "toolStripStatusLabelSampleCount";
            this.toolStripStatusLabelSampleCount.Size = new System.Drawing.Size(15, 17);
            this.toolStripStatusLabelSampleCount.Text = "0";
            // 
            // toolStripStatusLabel2
            // 
            this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            this.toolStripStatusLabel2.Size = new System.Drawing.Size(59, 17);
            this.toolStripStatusLabel2.Text = "参数个数:";
            // 
            // toolStripStatusLabelParamCount
            // 
            this.toolStripStatusLabelParamCount.Name = "toolStripStatusLabelParamCount";
            this.toolStripStatusLabelParamCount.Size = new System.Drawing.Size(15, 17);
            this.toolStripStatusLabelParamCount.Text = "0";
            // 
            // FormCalibration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1372, 749);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.statusStrip1);
            this.Name = "FormCalibration";
            this.Text = "校准";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormCalibration_FormClosing);
            this.Load += new System.EventHandler(this.FormCalibration_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewSampleInfo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewResult)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Panel panel1;
        private OxyPlot.WindowsForms.PlotView plotViewPS;
        private OxyPlot.WindowsForms.PlotView plotViewPT;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton toolStripButtonStart;
        private System.Windows.Forms.DataGridView dataGridViewSampleInfo;
        private System.Windows.Forms.ToolStripButton toolStripButtonApply;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton toolStripButtonLoadPresure;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton toolStripButtonClear;
        private System.Windows.Forms.DataGridView dataGridViewResult;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column7;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column8;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column0;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column2;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column3;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column4;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column5;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Column6;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelSampleCount;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelParamCount;
        private System.Windows.Forms.ToolStripButton toolStripButtonCalcCaliParam;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private OxyPlot.WindowsForms.PlotView plotViewKP;
        private System.Windows.Forms.ToolStripButton toolStripButtonSavePresure;
    }
}