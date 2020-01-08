using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Spirometer
{
    public partial class Form1 : Form
    {
        private const double m_sampleRate = 330; // 采样率,单位HZ
        private const double m_presureFlowRatio = 1200; // 压差转流量系数
        private PlotModel m_plotModelCV; // 容积-流量图Model
        private PlotModel m_plotModelCT; // 容积-时间图Model
        private PlotModel m_plotModelVT; // 流量-时间图Model

        public Form1()
        {
            InitializeComponent();
        }

        /* 枚举可用串口并更新列表控件 */
        private void EnumSerialPorts()
        {
            toolStripComboBoxCom.Items.Clear();

            SerialPort _tempPort;
            String[] Portname = SerialPort.GetPortNames();

            //create a loop for each string in SerialPort.GetPortNames
            foreach (string str in Portname)
            {
                try
                {
                    _tempPort = new SerialPort(str);
                    _tempPort.Open();

                    //if the port exist and we can open it
                    if (_tempPort.IsOpen)
                    {
                        toolStripComboBoxCom.Items.Add(str);
                        _tempPort.Close();
                    }
                }
                //else we have no ports or can't open them display the 
                //precise error of why we either don't have ports or can't open them
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.ToString(), "Error - No Ports available", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine(ex);
                }
            }

            toolStripComboBoxCom.SelectedIndex = 0;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            /* 枚举可用串口并更新列表控件 */
            EnumSerialPorts();

            /* 容积-流量图 */
            m_plotModelCV = new PlotModel()
            {
                Title = "容积-流量图",
                LegendTitle = "图例",
                LegendOrientation = LegendOrientation.Horizontal,
                LegendPlacement = LegendPlacement.Inside,
                LegendPosition = LegendPosition.TopRight,
                LegendBackground = OxyColors.Beige,
                LegendBorder = OxyColors.Black
            };

            //X轴
            var xAxisCV = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Bottom,
                Minimum = 0,
                Maximum = 1000
            };
            m_plotModelCV.Axes.Add(xAxisCV);

            //Y轴
            var yAxisCV = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Left
            };
            m_plotModelCV.Axes.Add(yAxisCV);

            // 数据
            var seriesCV = new LineSeries()
            {
                Color = OxyColors.Green,
                StrokeThickness = 1,
                MarkerSize = 1,
                MarkerStroke = OxyColors.DarkGreen,
                MarkerType = MarkerType.Circle,
                Title = "Data"
            };
            m_plotModelCV.Series.Add(seriesCV);

            plotViewCV.Model = m_plotModelCV;

            /* 容积-时间图 */
            m_plotModelCT = new PlotModel()
            {
                Title = "容积-时间图",
                LegendTitle = "图例",
                LegendOrientation = LegendOrientation.Horizontal,
                LegendPlacement = LegendPlacement.Inside,
                LegendPosition = LegendPosition.TopRight,
                LegendBackground = OxyColors.Beige,
                LegendBorder = OxyColors.Black
            };

            //X轴
            var xAxisCT = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Bottom,
                Minimum = 0,
                Maximum = 60 * 1000 // 1分钟
            };
            m_plotModelCT.Axes.Add(xAxisCT);

            //Y轴
            var yAxisCT = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Left
            };
            m_plotModelCT.Axes.Add(yAxisCT);

            // 数据
            var seriesCT = new LineSeries()
            {
                Color = OxyColors.Green,
                StrokeThickness = 1,
                MarkerSize = 1,
                MarkerStroke = OxyColors.DarkGreen,
                MarkerType = MarkerType.Circle,
                Title = "Data"
            };
            m_plotModelCT.Series.Add(seriesCT);

            plotViewCT.Model = m_plotModelCT;

            /* 流量-时间图 */
            m_plotModelVT = new PlotModel()
            {
                Title = "流量-时间图",
                LegendTitle = "图例",
                LegendOrientation = LegendOrientation.Horizontal,
                LegendPlacement = LegendPlacement.Inside,
                LegendPosition = LegendPosition.TopRight,
                LegendBackground = OxyColors.Beige,
                LegendBorder = OxyColors.Black
            };

            //X轴
            var xAxisVT = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Bottom,
                Minimum = 0,
                Maximum = 60 * 1000 // 1分钟
            };
            m_plotModelVT.Axes.Add(xAxisVT);

            //Y轴
            var yAxisVT = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Left
            };
            m_plotModelVT.Axes.Add(yAxisVT);

            // 数据
            var seriesVT = new LineSeries()
            {
                Color = OxyColors.Green,
                StrokeThickness = 1,
                MarkerSize = 1,
                MarkerStroke = OxyColors.DarkGreen,
                MarkerType = MarkerType.Circle,
                Title = "Data"
            };
            m_plotModelVT.Series.Add(seriesVT);

            plotViewVT.Model = m_plotModelVT;
        }

        /* 压差转流量 */
        private double PresureToFlow(double presure)
        {
            return presure / m_presureFlowRatio;
        }

        private void AddFlowPoint(double time, double flow)
        {
            var serieVT = plotViewVT.Model.Series[0] as LineSeries;
            serieVT.Points.Add(new DataPoint(time, flow));
        }

        private void ClearAll()
        {
            var serieCV = plotViewCV.Model.Series[0] as LineSeries;
            serieCV.Points.Clear();
            plotViewCV.InvalidatePlot(true);
            var xAxisCV = m_plotModelCV.Axes[0];
            xAxisCV.Reset();

            var serieCT = plotViewCT.Model.Series[0] as LineSeries;
            serieCT.Points.Clear();
            plotViewCT.InvalidatePlot(true);
            var xAxisCT = m_plotModelCT.Axes[0];
            xAxisCT.Reset();

            var serieVT = plotViewVT.Model.Series[0] as LineSeries;
            serieVT.Points.Clear();
            plotViewVT.InvalidatePlot(true);
            var xAxisVT = m_plotModelVT.Axes[0];
            xAxisVT.Reset();
        }

        private void toolStripButtonConnect_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButtonScan_Click(object sender, EventArgs e)
        {
            /* 枚举可用串口并更新列表控件 */
            EnumSerialPorts();
        }

        private void toolStripButtonStart_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButtonSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveCSVDialog = new SaveFileDialog();
            saveCSVDialog.Filter = "CSV File (*.csv;)|*.csv";
            //saveCSVDialog.Multiselect = false;

            if (saveCSVDialog.ShowDialog() == DialogResult.OK)
            {
                if (String.IsNullOrEmpty(saveCSVDialog.FileName))
                {
                    return;
                }

                toolStripButtonSave.Enabled = false;

                Task.Factory.StartNew(() =>
                {
                    var serieVT = plotViewVT.Model.Series[0] as LineSeries;
                    StringBuilder strData = new StringBuilder();
                    foreach (var point in serieVT.Points)
                    {
                        strData.Append(point.Y);
                        strData.Append(",");
                    }

                    using (StreamWriter writer = new StreamWriter(saveCSVDialog.FileName, false, Encoding.UTF8))
                    {
                        writer.Write(strData);
                        writer.Close();

                        MessageBox.Show("保存成功.");
                    }

                    this.BeginInvoke(new Action<Form1>((obj) => { toolStripButtonSave.Enabled = true; }), this);
                });
            }
        }

        private void toolStripButtonLoad_Click(object sender, EventArgs e)
        {
            OpenFileDialog openCSVDialog = new OpenFileDialog();
            openCSVDialog.Filter = "CSV File (*.csv;)|*.csv";
            openCSVDialog.Multiselect = false;

            if (openCSVDialog.ShowDialog() == DialogResult.OK)
            {
                if (String.IsNullOrEmpty(openCSVDialog.FileName))
                {
                    return;
                }

                toolStripButtonLoad.Enabled = false;

                Task.Factory.StartNew(() =>
                {
                    string strData = String.Empty;

                    using (StreamReader reader = new StreamReader(openCSVDialog.FileName, Encoding.UTF8))
                    {
                        strData = reader.ReadToEnd();
                        reader.Close();
                    }

                    ClearAll();

                    double time = 0;
                    string[] strDataArray = strData.Split(new char[] { ',' });
                    foreach (var strVal in strDataArray)
                    {
                        if (String.Empty == strVal)
                        {
                            continue;
                        }

                        double presure = Convert.ToDouble(strVal); // 压差
                        double flow = PresureToFlow(presure); // 流量

                        AddFlowPoint(time, flow);
                        time += (1000 / m_sampleRate);
                    }

                    this.BeginInvoke(new Action<Form1>((obj) => { toolStripButtonLoad.Enabled = true; }), this);

                    plotViewVT.InvalidatePlot(true);
                    plotViewCT.InvalidatePlot(true);
                    plotViewCV.InvalidatePlot(true);
                });
            }
        }

        private void toolStripButtonClear_Click(object sender, EventArgs e)
        {
            ClearAll();
        }
    }
}
