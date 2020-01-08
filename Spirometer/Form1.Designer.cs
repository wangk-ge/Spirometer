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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.plotViewCV = new OxyPlot.WindowsForms.PlotView();
            this.plotViewCT = new OxyPlot.WindowsForms.PlotView();
            this.plotViewVT = new OxyPlot.WindowsForms.PlotView();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
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
            this.splitContainer1.Size = new System.Drawing.Size(1372, 550);
            this.splitContainer1.SplitterDistance = 325;
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
            this.splitContainer2.Size = new System.Drawing.Size(1372, 325);
            this.splitContainer2.SplitterDistance = 668;
            this.splitContainer2.TabIndex = 0;
            // 
            // plotViewCV
            // 
            this.plotViewCV.Dock = System.Windows.Forms.DockStyle.Fill;
            this.plotViewCV.Location = new System.Drawing.Point(0, 0);
            this.plotViewCV.Name = "plotViewCV";
            this.plotViewCV.PanCursor = System.Windows.Forms.Cursors.Hand;
            this.plotViewCV.Size = new System.Drawing.Size(664, 321);
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
            this.plotViewCT.Size = new System.Drawing.Size(696, 321);
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
            this.plotViewVT.Size = new System.Drawing.Size(1368, 217);
            this.plotViewVT.TabIndex = 0;
            this.plotViewVT.Text = "plotView1";
            this.plotViewVT.ZoomHorizontalCursor = System.Windows.Forms.Cursors.SizeWE;
            this.plotViewVT.ZoomRectangleCursor = System.Windows.Forms.Cursors.SizeNWSE;
            this.plotViewVT.ZoomVerticalCursor = System.Windows.Forms.Cursors.SizeNS;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1372, 550);
            this.Controls.Add(this.splitContainer1);
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
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private OxyPlot.WindowsForms.PlotView plotViewCV;
        private OxyPlot.WindowsForms.PlotView plotViewCT;
        private OxyPlot.WindowsForms.PlotView plotViewVT;
    }
}

