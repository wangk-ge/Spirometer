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
        private PlotModel m_plotModelFV; // 流量(Flow)-容积(Volume)图Model
        private PlotModel m_plotModelVT; // 容积(Volume)-时间(Time)图Model
        private PlotModel m_plotModelFT; // 流量(Flow)-时间(Time)图Model

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

            /* 流量(Flow)-容积(Volume)图 */
            m_plotModelFV = new PlotModel()
            {
                Title = "流量(Flow)-容积(Volume)",
                LegendTitle = "图例",
                LegendOrientation = LegendOrientation.Horizontal,
                LegendPlacement = LegendPlacement.Inside,
                LegendPosition = LegendPosition.TopRight,
                LegendBackground = OxyColors.Beige,
                LegendBorder = OxyColors.Black,
                IsLegendVisible = false // 隐藏图例
            };

            //X轴,Volume
            var xAxisFV = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Bottom,
                //Minimum = 0,
                //Maximum = 1000,
                Title = "Volume(L)"
            };
            m_plotModelFV.Axes.Add(xAxisFV);

            //Y轴,Flow
            var yAxisFV = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Left,
                Title = "Flow(L/S)"
            };
            m_plotModelFV.Axes.Add(yAxisFV);

            // 数据
            var seriesFV = new LineSeries()
            {
                Color = OxyColors.Blue,
                StrokeThickness = 1,
                //MarkerSize = 1,
                //MarkerStroke = OxyColors.DarkBlue,
                //MarkerType = MarkerType.Circle,
                Title = "Data"
            };
            m_plotModelFV.Series.Add(seriesFV);

            plotViewFV.Model = m_plotModelFV;

            /* 容积(Volume)-时间(Time)图 */
            m_plotModelVT = new PlotModel()
            {
                Title = "容积(Volume)-时间(Time)",
                LegendTitle = "图例",
                LegendOrientation = LegendOrientation.Horizontal,
                LegendPlacement = LegendPlacement.Inside,
                LegendPosition = LegendPosition.TopRight,
                LegendBackground = OxyColors.Beige,
                LegendBorder = OxyColors.Black,
                IsLegendVisible = false // 隐藏图例
            };

            //X轴,Time
            var xAxisVT = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Bottom,
                //Minimum = 0,
                //Maximum = 60 * 1000, // 1分钟
                Title = "Time(MS)"
            };
            m_plotModelVT.Axes.Add(xAxisVT);

            //Y轴,Volume
            var yAxisVT = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Left,
                Title = "Volume(L)"
            };
            m_plotModelVT.Axes.Add(yAxisVT);

            // 数据
            var seriesVT = new LineSeries()
            {
                Color = OxyColors.Blue,
                StrokeThickness = 1,
                //MarkerSize = 1,
                //MarkerStroke = OxyColors.DarkBlue,
                //MarkerType = MarkerType.Circle,
                Title = "Data"
            };
            m_plotModelVT.Series.Add(seriesVT);

            plotViewVT.Model = m_plotModelVT;

            /* 流量(Flow)-时间(Time)图 */
            m_plotModelFT = new PlotModel()
            {
                Title = "流量(Flow)-时间(Time)",
                LegendTitle = "图例",
                LegendOrientation = LegendOrientation.Horizontal,
                LegendPlacement = LegendPlacement.Inside,
                LegendPosition = LegendPosition.TopRight,
                LegendBackground = OxyColors.Beige,
                LegendBorder = OxyColors.Black,
                IsLegendVisible = false // 隐藏图例
            };

            //X轴,Time
            var xAxisFT = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Bottom,
                //Minimum = 0,
                //Maximum = 60 * 1000, // 1分钟
                Title = "Time(MS)"
            };
            m_plotModelFT.Axes.Add(xAxisFT);

            //Y轴,Flow
            var yAxisFT = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Left,
                Title = "Flow(L/S)"
            };
            m_plotModelFT.Axes.Add(yAxisFT);

            // 数据
            var seriesFT = new LineSeries()
            {
                Color = OxyColors.Blue,
                StrokeThickness = 1,
                //MarkerSize = 1,
                //MarkerStroke = OxyColors.DarkBlue,
                //MarkerType = MarkerType.Circle,
                Title = "Data"
            };
            m_plotModelFT.Series.Add(seriesFT);

            plotViewFT.Model = m_plotModelFT;
        }

        /* 压差转流量 */
        private double PresureToFlow(double presure)
        {
            return presure / (m_presureFlowRatio * 1000);
        }

        private void AddTimeFlow(double time, double flow)
        {
            var serieVT = plotViewFT.Model.Series[0] as LineSeries;
            serieVT.Points.Add(new DataPoint(time, flow));
        }

        private void AddVolumeFlow(double volume, double flow)
        {
            var serieFV = plotViewFV.Model.Series[0] as LineSeries;
            serieFV.Points.Add(new DataPoint(volume, flow));
        }

        private void AddTimeVolume(double time, double volume)
        {
            var serieVT = plotViewVT.Model.Series[0] as LineSeries;
            serieVT.Points.Add(new DataPoint(time, volume));
        }

        private void ClearAll()
        {
            /* Clear Flow-Volume Plot */
            var serieFV = plotViewFV.Model.Series[0] as LineSeries;
            serieFV.Points.Clear();
            plotViewFV.InvalidatePlot(true);
            var xAxisFV = m_plotModelFV.Axes[0];
            xAxisFV.Reset();

            /* Clear Volume-Time Plot */
            var serieVT = plotViewVT.Model.Series[0] as LineSeries;
            serieVT.Points.Clear();
            plotViewVT.InvalidatePlot(true);
            var xAxisVT = m_plotModelVT.Axes[0];
            xAxisVT.Reset();

            /* Clear Flow-Time Plot */
            var serieFT = plotViewFT.Model.Series[0] as LineSeries;
            serieFT.Points.Clear();
            plotViewFT.InvalidatePlot(true);
            var xAxisFT = m_plotModelFT.Axes[0];
            xAxisFT.Reset();
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
                    var serieVT = plotViewFT.Model.Series[0] as LineSeries;
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

                    double volume = 923;
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
                        volume += flow; // 流量积分得容积

                        AddTimeFlow(time, flow);
                        AddTimeVolume(time, volume);
                        AddVolumeFlow(volume, flow);
                        Console.WriteLine($"({volume},{flow})");

                        time += (1000 / m_sampleRate);
                    }

                    this.BeginInvoke(new Action<Form1>((obj) => { toolStripButtonLoad.Enabled = true; }), this);

                    plotViewFT.InvalidatePlot(true);
                    plotViewVT.InvalidatePlot(true);
                    plotViewFV.InvalidatePlot(true);
                });
            }
        }

        private void toolStripButtonClear_Click(object sender, EventArgs e)
        {
            ClearAll();
        }
    }
}
