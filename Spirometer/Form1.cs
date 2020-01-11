using OxyPlot;
using OxyPlot.Annotations;
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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Spirometer
{
    public partial class Form1 : Form
    {
        private ConcurrentQueue<double> m_dataQueue = new ConcurrentQueue<double>(); // 数据队列
        private FlowSensor m_flowSensor; // 流量传感器
        private PulmonaryFunction m_pulmonaryFunc; // 肺功能参数计算器
        private PlotModel m_plotModelFV; // 流量(Flow)-容积(Volume)图Model
        private PlotModel m_plotModelVT; // 容积(Volume)-时间(Time)图Model
        private PlotModel m_plotModelFT; // 流量(Flow)-时间(Time)图Model

        private System.Windows.Forms.Timer m_refreshTimer = new System.Windows.Forms.Timer(); // 波形刷新定时器
        private readonly int m_fps = 24; // 帧率

        private List<DataPoint> m_pointsFV; // 流量(Flow)-容积(Volume)数据
        private List<DataPoint> m_pointsVT; // 容积(Volume)-时间(Time)数据
        private List<DataPoint> m_pointsFT; // 流量(Flow)-时间(Time)数据

        public Form1()
        {
            /* 流量传感器 */
            m_flowSensor = new FlowSensor();
            /* 肺功能参数计算器 */
            m_pulmonaryFunc = new PulmonaryFunction(m_flowSensor.SampleTime);

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
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Bottom,
                Minimum = 0,
                Maximum = 6,
                Title = "Volume(L)"
            };
            m_plotModelFV.Axes.Add(xAxisFV);

            //Y轴,Flow
            var yAxisFV = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Dot,
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
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Bottom,
                Minimum = 0,
                Maximum = 50 * 1000,
                Title = "Time(MS)"
            };
            m_plotModelVT.Axes.Add(xAxisVT);

            //Y轴,Volume
            var yAxisVT = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Dot,
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
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Bottom,
                Minimum = 0,
                Maximum = 50 * 1000,
                Title = "Time(MS)"
            };
            m_plotModelFT.Axes.Add(xAxisFT);

            //Y轴,Flow
            var yAxisFT = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Dot,
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

            /* 归零已完成 */
            m_pulmonaryFunc.ZeroingCompleted += new PulmonaryFunction.ZeroingCompleteHandler((uint sampleIndex, double zeroOffset) =>
            {
                Console.WriteLine($"ZeroingCompleted: {sampleIndex} {zeroOffset}");
                /*
                var annotation = new LineAnnotation()
                {
                    Color = OxyColors.Red,
                    X = sampleIndex * m_flowSensor.SampleTime,
                    LineStyle = LineStyle.Dash,
                    Type = LineAnnotationType.Vertical,
                    Text = "归零"
                };
                m_plotModelFT.Annotations.Add(annotation);
                */
            });

            /* 测量启动 */
            m_pulmonaryFunc.MeasureStarted += new PulmonaryFunction.MeasureStartHandler((uint sampleIndex, bool inspiration) =>
            {
                Console.WriteLine($"MeasureStarted: {sampleIndex} {inspiration}");
                var annotation = new LineAnnotation()
                {
                    Color = OxyColors.Red,
                    X = sampleIndex * m_flowSensor.SampleTime,
                    LineStyle = LineStyle.Dash,
                    Type = LineAnnotationType.Vertical,
                    Text = $"开始-{(inspiration? "吸气" : "呼气")}"
                };
                m_plotModelFT.Annotations.Add(annotation);
                annotation = new LineAnnotation()
                {
                    Color = OxyColors.Red,
                    X = sampleIndex * m_flowSensor.SampleTime,
                    LineStyle = LineStyle.Dash,
                    Type = LineAnnotationType.Vertical,
                    Text = $"开始-{(inspiration ? "吸气" : "呼气")}"
                };
                m_plotModelVT.Annotations.Add(annotation);
            });

            /* 开始吸气 */
            m_pulmonaryFunc.InspirationStarted += new PulmonaryFunction.InspirationStartHandler((uint sampleIndex, uint peekFlowIndex) =>
            {
                Console.WriteLine($"InspirationStarted: {sampleIndex} {peekFlowIndex} {m_pulmonaryFunc.RespiratoryRate}");
                var annotation = new LineAnnotation()
                {
                    Color = OxyColors.Red,
                    X = sampleIndex * m_flowSensor.SampleTime,
                    LineStyle = LineStyle.Dash,
                    Type = LineAnnotationType.Vertical,
                    Text = "吸气"
                };
                m_plotModelFT.Annotations.Add(annotation);
                annotation = new LineAnnotation()
                {
                    Color = OxyColors.Red,
                    X = sampleIndex * m_flowSensor.SampleTime,
                    LineStyle = LineStyle.Dash,
                    Type = LineAnnotationType.Vertical,
                    Text = "吸气"
                };
                m_plotModelVT.Annotations.Add(annotation);

                toolStripStatusLabelRespiratoryRate.Text = m_pulmonaryFunc.RespiratoryRate.ToString();
                toolStripStatusLabelVC.Text = m_pulmonaryFunc.VC.ToString();
                toolStripStatusLabelVC.Text = m_pulmonaryFunc.VC.ToString();
                toolStripStatusLabelTLC.Text = m_pulmonaryFunc.TLC.ToString();
                toolStripStatusLabelRV.Text = m_pulmonaryFunc.RV.ToString();
                toolStripStatusLabelTV.Text = m_pulmonaryFunc.TV.ToString();
                toolStripStatusLabelFRC.Text = m_pulmonaryFunc.FRC.ToString();
            });

            /* 开始吹气 */
            m_pulmonaryFunc.ExpirationStarted += new PulmonaryFunction.ExpirationStartHandler((uint sampleIndex, uint peekFlowIndex) =>
            {
                Console.WriteLine($"ExpirationStarted: {sampleIndex} {peekFlowIndex} {m_pulmonaryFunc.RespiratoryRate}");
                var annotation = new LineAnnotation()
                {
                    Color = OxyColors.Violet,
                    X = sampleIndex * m_flowSensor.SampleTime,
                    LineStyle = LineStyle.Dash,
                    Type = LineAnnotationType.Vertical,
                    Text = $"呼气"
                };
                m_plotModelFT.Annotations.Add(annotation);
                annotation = new LineAnnotation()
                {
                    Color = OxyColors.Violet,
                    X = sampleIndex * m_flowSensor.SampleTime,
                    LineStyle = LineStyle.Dash,
                    Type = LineAnnotationType.Vertical,
                    Text = $"呼气"
                };
                m_plotModelVT.Annotations.Add(annotation);

                toolStripStatusLabelRespiratoryRate.Text = m_pulmonaryFunc.RespiratoryRate.ToString();
                toolStripStatusLabelVC.Text = m_pulmonaryFunc.VC.ToString();
                toolStripStatusLabelTLC.Text = m_pulmonaryFunc.TLC.ToString();
                toolStripStatusLabelRV.Text = m_pulmonaryFunc.RV.ToString();
                toolStripStatusLabelTV.Text = m_pulmonaryFunc.TV.ToString();
                toolStripStatusLabelFRC.Text = m_pulmonaryFunc.FRC.ToString();
            });

            /* 测量结束 */
            m_pulmonaryFunc.MeasureStoped += new PulmonaryFunction.MeasureStopHandler((uint sampleIndex, bool inspiration) =>
            {
                Console.WriteLine($"MeasureStoped: {sampleIndex} {inspiration}");
                var annotation = new LineAnnotation()
                {
                    Color = OxyColors.Red,
                    X = sampleIndex * m_flowSensor.SampleTime,
                    LineStyle = LineStyle.Dash,
                    Type = LineAnnotationType.Vertical,
                    Text = "停止"
                };
                m_plotModelFT.Annotations.Add(annotation);
                toolStripStatusLabelVC.Text = m_pulmonaryFunc.VC.ToString();
                toolStripStatusLabelVC.Text = m_pulmonaryFunc.VC.ToString();
                toolStripStatusLabelTLC.Text = m_pulmonaryFunc.TLC.ToString();
                toolStripStatusLabelRV.Text = m_pulmonaryFunc.RV.ToString();
                toolStripStatusLabelTV.Text = m_pulmonaryFunc.TV.ToString();
                toolStripStatusLabelFRC.Text = m_pulmonaryFunc.FRC.ToString();
            });

            /* 通过传感器获取数据 */
            m_flowSensor.FlowRecved += new FlowSensor.FlowRecvHandler((byte channel, double flow) => {
                //Console.WriteLine($"FlowRecved: {channel} {flow}");

                m_dataQueue.Enqueue(flow);
            });

            m_refreshTimer.Interval = 1000 / m_fps;
            m_refreshTimer.Tick += new EventHandler((timer, arg) => {

                double xBegin = m_pointsVT.Count > 0 ? m_pointsVT.Last().X : 0;
                while (m_dataQueue.Count > 0)
                {
                    bool bRet = m_dataQueue.TryDequeue(out double flow); // 流量
                    if (!bRet)
                    {
                        break;
                    }
                    //Console.WriteLine(flow);
                    AddFlow(flow);
                }

                double xEnd = m_pointsVT.Count > 0 ? m_pointsVT.Last().X : 0;
                double xDelta = xEnd - xBegin;
                if (xDelta > 0)
                {
                    InvalidatePlot(true);

                    //var xAxisFT = m_plotModelFT.Axes[0];
                    if (xEnd > xAxisFT.Maximum)
                    {
                        double panStep = xAxisFT.Transform(-xDelta + xAxisFT.Offset);
                        xAxisFT.Pan(panStep);
                    }

                    //var xAxisVT = m_plotModelVT.Axes[0];
                    if (xEnd > xAxisVT.Maximum)
                    {
                        double panStep = xAxisVT.Transform(-xDelta + xAxisVT.Offset);
                        xAxisVT.Pan(panStep);
                    }
                }
            });
        }

        /* 加入一个流量采集数据 */
        private void AddFlow(double flow)
        {
            /* 执行肺功能参数计算 */
            m_pulmonaryFunc.Input(flow);

            m_pointsFT.Add(new DataPoint(m_pulmonaryFunc.Time, m_pulmonaryFunc.Flow));
            m_pointsVT.Add(new DataPoint(m_pulmonaryFunc.Time, m_pulmonaryFunc.Volume));
            m_pointsFV.Add(new DataPoint(m_pulmonaryFunc.Volume, m_pulmonaryFunc.Flow));
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

        /* 清除状态栏 */
        private void ClearStatusBar()
        {
            this.BeginInvoke(new Action<Form1>((obj) => {
                toolStripStatusLabelRespiratoryRate.Text = "0.0";
                toolStripStatusLabelVC.Text = "0.0";
                toolStripStatusLabelVC.Text = "0.0";
                toolStripStatusLabelTLC.Text = "0.0";
                toolStripStatusLabelRV.Text = "0.0";
                toolStripStatusLabelTV.Text = "0.0";
                toolStripStatusLabelFRC.Text = "0.0";
            }), this);
        }

        /* 清空所有图表数据和缓存数据,并刷新显示 */
        private void ClearAll()
        {
            /* 尝试清空数据队列 */
            TryClearDataQueue();

            /* 重置状态 */
            m_pulmonaryFunc.Reset();

            /* Clear Flow-Volume Plot */
            var serieFV = m_plotModelFV.Series[0] as LineSeries;
            serieFV.Points.Clear();
            m_plotModelFV.Annotations.Clear();
            var xAxisFV = m_plotModelFV.Axes[0];
            xAxisFV.Reset();

            /* Clear Volume-Time Plot */
            var serieVT = m_plotModelVT.Series[0] as LineSeries;
            serieVT.Points.Clear();
            m_plotModelVT.Annotations.Clear();
            var xAxisVT = m_plotModelVT.Axes[0];
            xAxisVT.Reset();

            /* Clear Flow-Time Plot */
            var serieFT = m_plotModelFT.Series[0] as LineSeries;
            serieFT.Points.Clear();
            m_plotModelFT.Annotations.Clear();
            var xAxisFT = m_plotModelFT.Axes[0];
            xAxisFT.Reset();

            InvalidatePlot(true);

            ClearStatusBar();
        }

        /* 请求所有图表刷新显示 */
        private void InvalidatePlot(bool updateData)
        {
            plotViewFT.InvalidatePlot(updateData);
            plotViewVT.InvalidatePlot(updateData);
            plotViewFV.InvalidatePlot(updateData);
        }

        /* 发送命令到FlowSensor(异步响应) */
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

        /* 显示加载对话框,加载Flow数据或Preaure数据 */
        private void ShowLoadCSVDialog(bool isFlow)
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

                toolStripButtonLoadPresure.Enabled = false;
                toolStripButtonLoadFlow.Enabled = false;
                toolStripButtonSaveFlow.Enabled = false;

                Task.Factory.StartNew(() =>
                {
                    string strData = String.Empty;

                    using (StreamReader reader = new StreamReader(openCSVDialog.FileName, Encoding.UTF8))
                    {
                        strData = reader.ReadToEnd();
                        reader.Close();
                    }

                    ClearAll();

                    if (isFlow)
                    {
                        string[] strDataArray = strData.Split(new char[] { ',' });
                        foreach (var strVal in strDataArray)
                        {
                            if (String.Empty == strVal)
                            {
                                continue;
                            }

                            double flow = Convert.ToDouble(strVal); // 流量

                            //Console.WriteLine(flow);

                            AddFlow(flow);
                        }
                    }
                    else
                    {
                        string[] strDataArray = strData.Split(new char[] { ',' });
                        foreach (var strVal in strDataArray)
                        {
                            if (String.Empty == strVal)
                            {
                                continue;
                            }

                            double presure = Convert.ToDouble(strVal); // 压差

                            double flow = m_flowSensor.PresureToFlow(presure); // 流量

                            //Console.WriteLine(flow);

                            AddFlow(flow);
                        }
                    }
                    

                    this.BeginInvoke(new Action<Form1>((obj) => { 
                        toolStripButtonLoadPresure.Enabled = true; 
                        toolStripButtonLoadFlow.Enabled = true;
                        toolStripButtonSaveFlow.Enabled = true;
                    }), this);

                    InvalidatePlot(true);
                });
            }
        }

        /* 显示保存对话框,保存Flow数据为CSV文件 */
        private void ShowSaveCSVDialog()
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

                toolStripButtonLoadPresure.Enabled = false;
                toolStripButtonLoadFlow.Enabled = false;
                toolStripButtonSaveFlow.Enabled = false;

                Task.Factory.StartNew(() =>
                {
                    var serieVT = m_plotModelFT.Series[0] as LineSeries;
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

                    this.BeginInvoke(new Action<Form1>((obj) => {
                        toolStripButtonLoadPresure.Enabled = true;
                        toolStripButtonLoadFlow.Enabled = true;
                        toolStripButtonSaveFlow.Enabled = true;
                    }), this);
                });
            }
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
                toolStripButtonLoadPresure.Enabled = true;
                toolStripButtonSaveFlow.Enabled = true;
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
                        toolStripButtonLoadPresure.Enabled = false;
                        toolStripButtonSaveFlow.Enabled = false;
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
                    toolStripButtonLoadPresure.Enabled = true;
                    toolStripButtonSaveFlow.Enabled = true;
                    /* 停止刷新定时器 */
                    m_refreshTimer.Stop();
                    /* 尝试清空数据队列 */
                    TryClearDataQueue();
                }
            }
        }

        private void toolStripButtonSaveFlow_Click(object sender, EventArgs e)
        {
            ShowSaveCSVDialog();
        }

        private void toolStripButtonLoadPresure_Click(object sender, EventArgs e)
        {
            ShowLoadCSVDialog(false);
        }

        private void toolStripButtonLoadFlow_Click(object sender, EventArgs e)
        {
            ShowLoadCSVDialog(true);
        }

        private void toolStripButtonClear_Click(object sender, EventArgs e)
        {
            ClearAll();
        }
    }
}
