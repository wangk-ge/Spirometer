using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Concurrent;
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
        private readonly double m_sampleRate = 330; // 采样率,单位HZ
        private readonly double m_presureFlowRatio = 1333; // 压差转流量系数(转出来的单位是ml/s)
        private readonly double m_defaultRV = 2.55; // 默认残气量(RV),单位: L
        private ConcurrentQueue<double> m_dataQueue = new ConcurrentQueue<double>(); // 数据队列
        private FlowSensor m_flowSensor = new FlowSensor(); // 流量传感器
        private KalmanFilter m_kalmanFilter = new KalmanFilter(0.01f/*Q*/, 0.01f/*R*/, 10.0f/*P*/, 0);
        private PlotModel m_plotModelFV; // 流量(Flow)-容积(Volume)图Model
        private PlotModel m_plotModelVT; // 容积(Volume)-时间(Time)图Model
        private PlotModel m_plotModelFT; // 流量(Flow)-时间(Time)图Model

        private Timer m_refreshTimer = new Timer(); // 波形刷新定时器
        private readonly int m_fps = 24; // 帧率

        private List<DataPoint> m_pointsFV; // 流量(Flow)-容积(Volume)数据
        private List<DataPoint> m_pointsVT; // 容积(Volume)-时间(Time)数据
        private List<DataPoint> m_pointsFT; // 流量(Flow)-时间(Time)数据

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
            m_pointsFV = seriesFV.Points;

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
            m_pointsVT = seriesVT.Points;

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
            m_pointsFT = seriesFT.Points;

            /* 通过传感器获取数据 */
            m_flowSensor.m_frameDecoder.WaveDataRespRecved += new FrameDecoder.WaveDataRecvHandler((byte channel, double value) => {
                //Console.WriteLine($"WaveDataRespRecved: {channel} {value}");

                m_dataQueue.Enqueue(value);
            });

            m_refreshTimer.Interval = 1000 / m_fps;
            m_refreshTimer.Tick += new EventHandler((timer, arg) => {

                double xBegin = m_pointsVT.Count > 0 ? m_pointsVT.Last().X : 0;
                while (m_dataQueue.Count > 0)
                {
                    bool bRet = m_dataQueue.TryDequeue(out double presure); // 压差
                    if (!bRet)
                    {
                        break;
                    }
                    presure = m_kalmanFilter.Input((float)presure); // 执行滤波
                    double flow = PresureToFlow(presure); // 流量
                    AddFlow(flow);
                }

                double xEnd = m_pointsVT.Count > 0 ? m_pointsVT.Last().X : 0;
                double xDelta = xEnd - xBegin;
                if (xDelta > 0)
                {
                    InvalidatePlot(true);
                }
            });
        }

        /* 压差转流量,单位:L/S */
        private double PresureToFlow(double presure)
        {
            return presure / (m_presureFlowRatio * 1000);
        }

        /* 加入一个流量采集数据 */
        private void AddFlow(double flow)
        {
            double time = 0;
            double volume = m_defaultRV + flow;
            if (m_pointsVT.Count > 0)
            {
                DataPoint lastPoint = m_pointsVT.Last();
                time = lastPoint.X + (1000 / m_sampleRate); // 单位: ms
                volume = lastPoint.Y + flow * (1 / m_sampleRate); // 流量积分得容积,单位: L
            }

            m_pointsFT.Add(new DataPoint(time, flow));
            m_pointsVT.Add(new DataPoint(time, volume));
            m_pointsFV.Add(new DataPoint(volume, flow));
        }

        /* 尝试清空数据队列 */
        private bool TryClearDataQueue()
        {
            bool bRet = true;

            /* 尝试清空队列 */
            while (m_dataQueue.Count > 0)
            {
                bRet = m_dataQueue.TryDequeue(out _);
                if (!bRet)
                {
                    break;
                }
            }

            return bRet;
        }

        private void ClearAll()
        {
            /* 尝试清空数据队列 */
            TryClearDataQueue();

            /* Clear Flow-Volume Plot */
            var serieFV = plotViewFV.Model.Series[0] as LineSeries;
            serieFV.Points.Clear();
            var xAxisFV = m_plotModelFV.Axes[0];
            xAxisFV.Reset();

            /* Clear Volume-Time Plot */
            var serieVT = plotViewVT.Model.Series[0] as LineSeries;
            serieVT.Points.Clear();
            var xAxisVT = m_plotModelVT.Axes[0];
            xAxisVT.Reset();

            /* Clear Flow-Time Plot */
            var serieFT = plotViewFT.Model.Series[0] as LineSeries;
            serieFT.Points.Clear();
            var xAxisFT = m_plotModelFT.Axes[0];
            xAxisFT.Reset();

            InvalidatePlot(true);
        }

        private void InvalidatePlot(bool updateData)
        {
            plotViewFT.InvalidatePlot(updateData);
            plotViewVT.InvalidatePlot(updateData);
            plotViewFV.InvalidatePlot(updateData);
        }

        private async void SendCmd(string cmd)
        {
            if (!m_flowSensor.IsOpen())
            {
                return;
            }

            Console.WriteLine($"Sned: {cmd} \r\n");
            string cmdResp = await m_flowSensor.ExcuteCmdAsync(cmd, 2000);
            Console.WriteLine($"Revc: {cmdResp} \r\n");
        }

        private void toolStripButtonConnect_Click(object sender, EventArgs e)
        {
            if ("连接" == toolStripButtonConnect.Text)
            {
                bool bRet = m_flowSensor.Open(toolStripComboBoxCom.Text);
                toolStripButtonStart.Enabled = bRet;
                //toolStripButtonLoad.Enabled = !bRet;
                //toolStripButtonSave.Enabled = !bRet;
                //toolStripButtonClear.Enabled = !bRet;
                toolStripButtonScan.Enabled = !bRet;
                toolStripComboBoxCom.Enabled = !bRet;
                toolStripButtonConnect.Text = bRet ? "断开" : "连接";
                /* 尝试清空数据队列 */
                TryClearDataQueue();
            }
            else
            {
                m_flowSensor.Close();
                toolStripButtonStart.Enabled = false;
                toolStripButtonLoad.Enabled = true;
                toolStripButtonSave.Enabled = true;
                toolStripButtonClear.Enabled = true;
                toolStripButtonScan.Enabled = true;
                toolStripComboBoxCom.Enabled = true;
                toolStripButtonConnect.Text = "连接";
                toolStripButtonStart.Text = "开始";
                /* 停止刷新定时器 */
                m_refreshTimer.Stop();
                /* 尝试清空数据队列 */
                TryClearDataQueue();
            }
        }

        private void toolStripButtonScan_Click(object sender, EventArgs e)
        {
            /* 枚举可用串口并更新列表控件 */
            EnumSerialPorts();
        }

        private async void toolStripButtonStart_Click(object sender, EventArgs e)
        {
            string cmd = "[ADC_START]"; // 启动
            if ("停止" == toolStripButtonStart.Text)
            {
                cmd = "[ADC_STOP]"; // 停止
            }
            Console.WriteLine($"Sned: {cmd} \r\n");
            string cmdResp = await m_flowSensor.ExcuteCmdAsync(cmd, 2000);
            Console.WriteLine($"Revc: {cmdResp} \r\n");

            if ("[OK]" == cmdResp)
            {
                if ("开始" == toolStripButtonStart.Text)
                {
                    cmd = "[ADC_CAL]"; // 归零

                    Console.WriteLine($"Sned: {cmd} \r\n");
                    cmdResp = await m_flowSensor.ExcuteCmdAsync(cmd, 2000);
                    Console.WriteLine($"Revc: {cmdResp} \r\n");
                    if ("[OK]" == cmdResp)
                    { // 归零成功
                        toolStripButtonStart.Text = "停止";
                        toolStripButtonClear.Enabled = false;
                        toolStripButtonLoad.Enabled = false;
                        toolStripButtonSave.Enabled = false;
                        //ClearAll();
                        /* 尝试清空数据队列 */
                        TryClearDataQueue();
                        /* 启动刷新定时器 */
                        m_refreshTimer.Start();
                    }
                }
                else // if ("停止" == toolStripButtonStart.Text)
                {
                    toolStripButtonStart.Text = "开始";
                    toolStripButtonClear.Enabled = true;
                    toolStripButtonLoad.Enabled = true;
                    toolStripButtonSave.Enabled = true;
                    /* 停止刷新定时器 */
                    m_refreshTimer.Stop();
                    /* 尝试清空数据队列 */
                    TryClearDataQueue();
                }
            }
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

                    string[] strDataArray = strData.Split(new char[] { ',' });
                    foreach (var strVal in strDataArray)
                    {
                        if (String.Empty == strVal)
                        {
                            continue;
                        }

                        double presure = Convert.ToDouble(strVal); // 压差
                        presure = m_kalmanFilter.Input((float)presure); // 执行滤波
                        double flow = PresureToFlow(presure); // 流量
                        //double filterFlow = m_kalmanFilter.Input((float)flow); // 执行滤波
                        //flow = filterFlow; // 使用滤波结果

                        AddFlow(flow);
                    }

                    this.BeginInvoke(new Action<Form1>((obj) => { toolStripButtonLoad.Enabled = true; }), this);

                    InvalidatePlot(true);
                });
            }
        }

        private void toolStripButtonClear_Click(object sender, EventArgs e)
        {
            ClearAll();
        }
    }
}
