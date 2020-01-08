namespace Spirometer
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.plotViewCV = new OxyPlot.WindowsForms.PlotView();
            this.plotViewCT = new OxyPlot.WindowsForms.PlotView();
            this.plotViewVT = new OxyPlot.WindowsForms.PlotView();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripComboBoxCom = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripButtonConnect = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonScan = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonStart = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonSave = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonLoad = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonClear = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.toolStrip1.SuspendLayout();
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
            this.splitContainer1.Panel2.Controls.Add(this.plotViewVT);
            this.splitContainer1.Size = new System.Drawing.Size(1372, 587);
            this.splitContainer1.SplitterDistance = 377;
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
            this.splitContainer2.Panel1.Controls.Add(this.plotViewCV);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.plotViewCT);
            this.splitContainer2.Size = new System.Drawing.Size(1372, 377);
            this.splitContainer2.SplitterDistance = 667;
            this.splitContainer2.SplitterWidth = 2;
            this.splitContainer2.TabIndex = 0;
            // 
            // plotViewCV
            // 
            this.plotViewCV.Dock = System.Windows.Forms.DockStyle.Fill;
            this.plotViewCV.Location = new System.Drawing.Point(0, 0);
            this.plotViewCV.Name = "plotViewCV";
            this.plotViewCV.PanCursor = System.Windows.Forms.Cursors.Hand;
            this.plotViewCV.Size = new System.Drawing.Size(663, 373);
            this.plotViewCV.TabIndex = 0;
            this.plotViewCV.Text = "plotView1";
            this.plotViewCV.ZoomHorizontalCursor = System.Windows.Forms.Cursors.SizeWE;
            this.plotViewCV.ZoomRectangleCursor = System.Windows.Forms.Cursors.SizeNWSE;
            this.plotViewCV.ZoomVerticalCursor = System.Windows.Forms.Cursors.SizeNS;
            // 
            // plotViewCT
            // 
            this.plotViewCT.Dock = System.Windows.Forms.DockStyle.Fill;
            this.plotViewCT.Location = new System.Drawing.Point(0, 0);
            this.plotViewCT.Name = "plotViewCT";
            this.plotViewCT.PanCursor = System.Windows.Forms.Cursors.Hand;
            this.plotViewCT.Size = new System.Drawing.Size(699, 373);
            this.plotViewCT.TabIndex = 0;
            this.plotViewCT.Text = "plotView1";
            this.plotViewCT.ZoomHorizontalCursor = System.Windows.Forms.Cursors.SizeWE;
            this.plotViewCT.ZoomRectangleCursor = System.Windows.Forms.Cursors.SizeNWSE;
            this.plotViewCT.ZoomVerticalCursor = System.Windows.Forms.Cursors.SizeNS;
            // 
            // plotViewVT
            // 
            this.plotViewVT.Dock = System.Windows.Forms.DockStyle.Fill;
            this.plotViewVT.Location = new System.Drawing.Point(0, 0);
            this.plotViewVT.Name = "plotViewVT";
            this.plotViewVT.PanCursor = System.Windows.Forms.Cursors.Hand;
            this.plotViewVT.Size = new System.Drawing.Size(1368, 204);
            this.plotViewVT.TabIndex = 0;
            this.plotViewVT.Text = "plotView1";
            this.plotViewVT.ZoomHorizontalCursor = System.Windows.Forms.Cursors.SizeWE;
            this.plotViewVT.ZoomRectangleCursor = System.Windows.Forms.Cursors.SizeNWSE;
            this.plotViewVT.ZoomVerticalCursor = System.Windows.Forms.Cursors.SizeNS;
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripComboBoxCom,
            this.toolStripButtonConnect,
            this.toolStripButtonScan,
            this.toolStripSeparator1,
            this.toolStripButtonStart,
            this.toolStripSeparator2,
            this.toolStripButtonSave,
            this.toolStripButtonLoad,
            this.toolStripButtonClear});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1372, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripComboBoxCom
            // 
            this.toolStripComboBoxCom.Name = "toolStripComboBoxCom";
            this.toolStripComboBoxCom.Size = new System.Drawing.Size(121, 25);
            // 
            // toolStripButtonConnect
            // 
            this.toolStripButtonConnect.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonConnect.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonConnect.Image")));
            this.toolStripButtonConnect.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonConnect.Name = "toolStripButtonConnect";
            this.toolStripButtonConnect.Size = new System.Drawing.Size(36, 22);
            this.toolStripButtonConnect.Text = "连接";
            this.toolStripButtonConnect.Click += new System.EventHandler(this.toolStripButtonConnect_Click);
            // 
            // toolStripButtonScan
            // 
            this.toolStripButtonScan.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonScan.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonScan.Image")));
            this.toolStripButtonScan.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonScan.Name = "toolStripButtonScan";
            this.toolStripButtonScan.Size = new System.Drawing.Size(36, 22);
            this.toolStripButtonScan.Text = "刷新";
            this.toolStripButtonScan.Click += new System.EventHandler(this.toolStripButtonScan_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
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
            // toolStripButtonSave
            // 
            this.toolStripButtonSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonSave.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonSave.Image")));
            this.toolStripButtonSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSave.Name = "toolStripButtonSave";
            this.toolStripButtonSave.Size = new System.Drawing.Size(36, 22);
            this.toolStripButtonSave.Text = "保存";
            this.toolStripButtonSave.Click += new System.EventHandler(this.toolStripButtonSave_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonLoad
            // 
            this.toolStripButtonLoad.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonLoad.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonLoad.Image")));
            this.toolStripButtonLoad.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonLoad.Name = "toolStripButtonLoad";
            this.toolStripButtonLoad.Size = new System.Drawing.Size(36, 22);
            this.toolStripButtonLoad.Text = "加载";
            this.toolStripButtonLoad.Click += new System.EventHandler(this.toolStripButtonLoad_Click);
            // 
            // toolStripButtonClear
            // 
            this.toolStripButtonClear.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonClear.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonClear.Image")));
            this.toolStripButtonClear.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonClear.Name = "toolStripButtonClear";
            this.toolStripButtonClear.Size = new System.Drawing.Size(36, 22);
            this.toolStripButtonClear.Text = "清除";
            this.toolStripButtonClear.Click += new System.EventHandler(this.toolStripButtonClear_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1372, 612);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private OxyPlot.WindowsForms.PlotView plotViewCV;
        private OxyPlot.WindowsForms.PlotView plotViewCT;
        private OxyPlot.WindowsForms.PlotView plotViewVT;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripComboBox toolStripComboBoxCom;
        private System.Windows.Forms.ToolStripButton toolStripButtonConnect;
        private System.Windows.Forms.ToolStripButton toolStripButtonScan;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton toolStripButtonStart;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton toolStripButtonSave;
        private System.Windows.Forms.ToolStripButton toolStripButtonLoad;
        private System.Windows.Forms.ToolStripButton toolStripButtonClear;
    }
}

