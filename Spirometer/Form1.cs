using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PulmonaryFunctionLib;

namespace Spirometer
{
    public partial class Form1 : Form
    {
        private ConcurrentQueue<double> m_dataQueue = new ConcurrentQueue<double>(); // 数据队列
        private FlowSensor m_flowSensor; // 流量传感器
        private PulmonaryFunction m_pulmonaryFunc; // 肺功能参数计算器
        private PlotModel m_plotModelFV; // 流量(Flow)-容积(Volume)图Model
        private PlotModel m_plotModelVT; // 用力呼气 容积(Volume)-时间(Time)图Model
        private PlotModel m_plotModelVFT; // 容积(Volume)/流量(Flow)-时间(Time)图Model

        private System.Windows.Forms.Timer m_refreshTimer = new System.Windows.Forms.Timer(); // 波形刷新定时器
        private readonly int m_fps = 24; // 帧率

        private List<DataPoint> m_pointsFV; // 流量(Flow)-容积(Volume)数据
        private List<DataPoint> m_pointsVT; // 容积(Volume)-时间(Time)数据
        private List<DataPoint> m_pointsVFTFlow; // 容积(Volume)/流量(Flow)-时间(Time)图 Flow数据
        private List<DataPoint> m_pointsVFTVolume; // 容积(Volume)/流量(Flow)-时间(Time)图 Volume数据

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
            /* 先清空串口列表 */
            toolStripComboBoxCom.Items.Clear();

            /* 取的系统所有串口名称列表 */
            String[] Portname = SerialPort.GetPortNames();

            /* 通过尝试开启串口来筛选未被占用串口名列表 */
            foreach (string str in Portname)
            {
                try
                {
                    SerialPort tempPort = new SerialPort(str);
                    tempPort.Open();

                    //if the port exist and we can open it
                    if (tempPort.IsOpen)
                    {
                        toolStripComboBoxCom.Items.Add(str);
                        tempPort.Close();
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

            /* 默认选中第一个 */
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

            // 设置View对应的Model
            plotViewFV.Model = m_plotModelFV;
            // 保存数据点列表引用
            m_pointsFV = seriesFV.Points;

            /* 【用力呼气 容积(Volume)-时间(Time)图】 */
            m_plotModelVT = new PlotModel()
            {
                Title = "用力呼气 容积(Volume)-时间(Time)",
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
                //Minimum = 0,
                //Maximum = 50 * 1000,
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

            // 数据,Volume
            var seriesVT = new LineSeries()
            {
                Color = OxyColors.Blue,
                StrokeThickness = 1,
                //MarkerSize = 1,
                //MarkerStroke = OxyColors.DarkBlue,
                //MarkerType = MarkerType.Circle,
                Title = "Volume"
            };
            m_plotModelVT.Series.Add(seriesVT);

            // 设置View对应的Model
            plotViewVT.Model = m_plotModelVT;
            // 保存数据点列表引用
            m_pointsVT = seriesVT.Points;

            /* 【容积(Volume)/流量(Flow)-时间(Time)图】 */
            m_plotModelVFT = new PlotModel()
            {
                Title = "容积(Volume)/流量(Flow)-时间(Time)",
                LegendTitle = "图例",
                LegendOrientation = LegendOrientation.Horizontal,
                LegendPlacement = LegendPlacement.Inside,
                LegendPosition = LegendPosition.TopRight,
                LegendBackground = OxyColors.Beige,
                LegendBorder = OxyColors.Black,
                //IsLegendVisible = false // 隐藏图例
            };

            //X轴,Time
            var xAxisVFT = new LinearAxis()
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
            m_plotModelVFT.Axes.Add(xAxisVFT);

            //左侧Y轴,Volume
            var yAxisVFTLeft = new LinearAxis()
            {
                //MajorGridlineStyle = LineStyle.Dot,
                //MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Left,
                Title = "Volume(L)",
                Key = "yAxisVFTLeft"
            };
            m_plotModelVFT.Axes.Add(yAxisVFTLeft);

            //右侧Y轴,Flow
            var yAxisVFTRight = new LinearAxis()
            {
                //MajorGridlineStyle = LineStyle.Dot,
                //MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Right,
                Title = "Flow(L/S)",
                Key = "yAxisVFTRight"
            };
            m_plotModelVFT.Axes.Add(yAxisVFTRight);

            // 数据Volume
            var seriesVFTVolume = new LineSeries()
            {
                Color = OxyColors.Blue,
                StrokeThickness = 1,
                //MarkerSize = 1,
                //MarkerStroke = OxyColors.DarkBlue,
                //MarkerType = MarkerType.Circle,
                Title = "Volume",
                YAxisKey = yAxisVFTLeft.Key
            };
            m_plotModelVFT.Series.Add(seriesVFTVolume);

            // 数据Flow
            var seriesVFTFlow = new LineSeries()
            {
                Color = OxyColors.YellowGreen,
                StrokeThickness = 1,
                //MarkerSize = 1,
                //MarkerStroke = OxyColors.DarkBlack,
                //MarkerType = MarkerType.Circle,
                Title = "Flow",
                YAxisKey = yAxisVFTRight.Key
            };
            m_plotModelVFT.Series.Add(seriesVFTFlow);

            // 设置View对应的Model
            plotViewVFT.Model = m_plotModelVFT;
            // 保存数据点列表引用
            m_pointsVFTFlow = seriesVFTFlow.Points;
            m_pointsVFTVolume = seriesVFTVolume.Points;

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
                m_plotModelVFT.Annotations.Add(annotation);
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
                m_plotModelVFT.Annotations.Add(annotation);
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
                m_plotModelVFT.Annotations.Add(annotation);
                if (6 == m_pulmonaryFunc.RespiratoryCycleCount)
                {
                    annotation = new LineAnnotation()
                    {
                        Color = OxyColors.Red,
                        Y = m_pulmonaryFunc.TVLowerAvg,
                        LineStyle = LineStyle.Dash,
                        Type = LineAnnotationType.Horizontal,
                        //Text = ""
                    };
                    m_plotModelVFT.Annotations.Add(annotation);
                }

                toolStripStatusLabelRespiratoryRate.Text = m_pulmonaryFunc.RespiratoryRate.ToString();
                toolStripStatusLabelVC.Text = m_pulmonaryFunc.VC.ToString();
                toolStripStatusLabelVC.Text = m_pulmonaryFunc.VC.ToString();
                toolStripStatusLabelTLC.Text = m_pulmonaryFunc.TLC.ToString();
                toolStripStatusLabelTV.Text = m_pulmonaryFunc.TV.ToString();
            });

            /* 开始呼气 */
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
                m_plotModelVFT.Annotations.Add(annotation);
                if (6 == m_pulmonaryFunc.RespiratoryCycleCount)
                {
                    annotation = new LineAnnotation()
                    {
                        Color = OxyColors.Red,
                        Y = m_pulmonaryFunc.TVLowerAvg,
                        LineStyle = LineStyle.Dash,
                        Type = LineAnnotationType.Horizontal,
                        //Text = ""
                    };
                    m_plotModelVFT.Annotations.Add(annotation);
                }

                toolStripStatusLabelRespiratoryRate.Text = m_pulmonaryFunc.RespiratoryRate.ToString();
                toolStripStatusLabelVC.Text = m_pulmonaryFunc.VC.ToString();
                toolStripStatusLabelTLC.Text = m_pulmonaryFunc.TLC.ToString();
                toolStripStatusLabelTV.Text = m_pulmonaryFunc.TV.ToString();
            });

            /* 开始用力呼气 */
            m_pulmonaryFunc.ForceExpirationStarted += new PulmonaryFunction.ForceExpirationStartHandler((uint sampleIndex, uint peekFlowIndex) =>
            {
                Console.WriteLine($"ForceExpirationStarted: {sampleIndex} {peekFlowIndex}");
                var annotation = new LineAnnotation()
                {
                    Color = OxyColors.Violet,
                    X = sampleIndex * m_flowSensor.SampleTime,
                    LineStyle = LineStyle.Dash,
                    Type = LineAnnotationType.Vertical,
                    Text = $"用力呼气"
                };
                m_plotModelVFT.Annotations.Add(annotation);
                if (6 == m_pulmonaryFunc.RespiratoryCycleCount)
                {
                    annotation = new LineAnnotation()
                    {
                        Color = OxyColors.Red,
                        Y = m_pulmonaryFunc.TVLowerAvg,
                        LineStyle = LineStyle.Dash,
                        Type = LineAnnotationType.Horizontal,
                        //Text = ""
                    };
                    m_plotModelVFT.Annotations.Add(annotation);
                }

                toolStripStatusLabelRespiratoryRate.Text = m_pulmonaryFunc.RespiratoryRate.ToString();
                toolStripStatusLabelVC.Text = m_pulmonaryFunc.VC.ToString();
                toolStripStatusLabelTLC.Text = m_pulmonaryFunc.TLC.ToString();
                toolStripStatusLabelTV.Text = m_pulmonaryFunc.TV.ToString();
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
                m_plotModelVFT.Annotations.Add(annotation);
                toolStripStatusLabelVC.Text = m_pulmonaryFunc.VC.ToString();
                toolStripStatusLabelVC.Text = m_pulmonaryFunc.VC.ToString();
                toolStripStatusLabelTLC.Text = m_pulmonaryFunc.TLC.ToString();
                toolStripStatusLabelTV.Text = m_pulmonaryFunc.TV.ToString();
            });

            /* 通过传感器获取数据 */
            m_flowSensor.FlowRecved += new FlowSensor.FlowRecvHandler((byte channel, double flow) => {
                //Console.WriteLine($"FlowRecved: {channel} {flow}");

                /* 数据存入队列,将在刷新定时器中读取 */
                m_dataQueue.Enqueue(flow);
            });

            /* 刷新定时器 */
            m_refreshTimer.Interval = 1000 / m_fps; // 设置定时器超时时间为帧间隔
            m_refreshTimer.Tick += new EventHandler((timer, arg) => {
                // 保存数据添加前的曲线最右端X坐标的位置(用于实现自动滚屏)
                double xBegin = m_pointsVFTFlow.Count > 0 ? m_pointsVFTFlow.Last().X : 0;
                // 尝试读取队列中的数据并添加到曲线
                while (m_dataQueue.Count > 0)
                {
                    /* 尝试读取队列中的数 */
                    bool bRet = m_dataQueue.TryDequeue(out double flow); // 流量
                    if (!bRet)
                    {
                        break;
                    }
                    
                    /* 已接收到一个流量采集数据 */
                    OnFlowRecved(flow);
                }

                /* 在必要时刷新曲线显示并执行自动滚屏 */
                double xEnd = m_pointsVFTFlow.Count > 0 ? m_pointsVFTFlow.Last().X : 0;
                double xDelta = xEnd - xBegin;
                if (xDelta > 0)
                {
                    /* 刷新曲线显示 */
                    plotViewVFT.InvalidatePlot(true);
                    plotViewFV.InvalidatePlot(true);

                    //var xAxisVFT = m_plotModelVFT.Axes[0];
                    if (xEnd > xAxisVFT.Maximum)
                    {
                        /* 自动滚屏 */
                        double panStep = xAxisVFT.Transform(-xDelta + xAxisVFT.Offset);
                        xAxisVFT.Pan(panStep);
                    }
                }
            });
        }

        /* 已接收到一个流量采集数据 */
        private void OnFlowRecved(double flow)
        {
            /* 执行肺功能参数计算 */
            m_pulmonaryFunc.Input(flow);

            /* 加入数据到对应的曲线 */
            m_pointsVFTVolume.Add(new DataPoint(m_pulmonaryFunc.Time, m_pulmonaryFunc.InVolume));
            m_pointsVFTFlow.Add(new DataPoint(m_pulmonaryFunc.Time, m_pulmonaryFunc.InFlow));
            m_pointsFV.Add(new DataPoint(m_pulmonaryFunc.ExVolume, m_pulmonaryFunc.ExFlow));
        }

        /* 输出用力呼气 Volume-Time 曲线 */
        private void UpdateVTPlot()
        {
            m_pointsVT.Clear();

            /* 输出用力呼气 Volume-Time Plot */
            if (m_pulmonaryFunc.ForceExpirationStartIndex > 0)
            {
                uint index = 0;
                for (uint i = m_pulmonaryFunc.ForceExpirationStartIndex; i <= m_pulmonaryFunc.ForceExpirationEndIndex; ++i)
                {
                    m_pointsVT.Add(new DataPoint(m_pulmonaryFunc.GetTime(index), m_pulmonaryFunc.GetExVolume(i)));
                    ++index;
                }

                /* 刷新曲线显示 */
                plotViewVT.InvalidatePlot(true);
            }
        }

        /* 更新 Flow-Volume Plot(平移到Volume从0开始) */
        private void UpdateFVPlot()
        {
            if (m_pointsFV.Count > 0)
            {
                m_pointsFV.Clear();

                for (uint i = 0; i < m_pulmonaryFunc.SampleCount; ++i)
                {
                    m_pointsFV.Add(new DataPoint(m_pulmonaryFunc.GetExVolume(i), m_pulmonaryFunc.GetExFlow(i)));
                }

                /* 刷新曲线显示 */
                plotViewFV.InvalidatePlot(true);
            }
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
            /* 确保更新UI的操作在UI线程执行 */
            this.BeginInvoke(new Action<Form1>((obj) => {
                toolStripStatusLabelRespiratoryRate.Text = "0.0";
                toolStripStatusLabelVC.Text = "0.0";
                toolStripStatusLabelVC.Text = "0.0";
                toolStripStatusLabelTLC.Text = "0.0";
                toolStripStatusLabelTV.Text = "0.0";
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

            /* Clear Volume/Flow-Time Plot */
            var serieVFTVolume = m_plotModelVFT.Series[0] as LineSeries;
            serieVFTVolume.Points.Clear();
            var serieVFTFlow = m_plotModelVFT.Series[1] as LineSeries;
            serieVFTFlow.Points.Clear();
            m_plotModelVFT.Annotations.Clear();
            var xAxisVFT = m_plotModelVFT.Axes[0];
            xAxisVFT.Reset();

            /* 刷新曲线显示 */
            plotViewVFT.InvalidatePlot(true);
            plotViewVT.InvalidatePlot(true);
            plotViewFV.InvalidatePlot(true);

            /* 清除状态栏 */
            ClearStatusBar();
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
            /* 弹出文件打开对话框 */
            OpenFileDialog openCSVDialog = new OpenFileDialog();
            openCSVDialog.Filter = "CSV File (*.csv;)|*.csv";
            openCSVDialog.Multiselect = false;

            if (openCSVDialog.ShowDialog() == DialogResult.OK)
            {
                if (String.IsNullOrEmpty(openCSVDialog.FileName))
                {
                    return;
                }

                /* 加载过程中暂时不允许再次点击 */
                toolStripButtonLoadPresure.Enabled = false;
                toolStripButtonLoadFlow.Enabled = false;
                toolStripButtonSaveFlow.Enabled = false;

                /* 启动任务执行异步加载(防止阻塞UI线程) */
                Task.Factory.StartNew(() =>
                {
                    /* 所有数据先加载到内存 */
                    string strData = String.Empty;
                    using (StreamReader reader = new StreamReader(openCSVDialog.FileName, Encoding.UTF8))
                    {
                        strData = reader.ReadToEnd();
                        reader.Close();
                    }

                    /* 先清空所有图表数据和缓存数据,并刷新显示 */
                    ClearAll();

                    /* 是否是加载流量数据 */
                    if (isFlow)
                    {
                        /* 解析CSV中的流量数据 */
                        string[] strDataArray = strData.Split(new char[] { ',' });
                        foreach (var strVal in strDataArray)
                        {
                            if (String.Empty == strVal)
                            {
                                continue;
                            }

                            double flow = Convert.ToDouble(strVal); // 流量

                            /* 已接收到一个流量采集数据 */
                            OnFlowRecved(flow);
                        }
                    }
                    else
                    {
                        /* 解析CSV中的压差数据 */
                        string[] strDataArray = strData.Split(new char[] { ',' });
                        foreach (var strVal in strDataArray)
                        {
                            if (String.Empty == strVal)
                            {
                                continue;
                            }

                            double presure = Convert.ToDouble(strVal); // 压差

                            /* 压差转流量 */
                            double flow = m_flowSensor.PresureToFlow(presure); // 流量

                            /* 已接收到一个流量采集数据 */
                            OnFlowRecved(flow);
                        }
                    }

                    /* 加载完毕恢复按钮使能状态(确保在UI线程执行) */
                    this.BeginInvoke(new Action<Form1>((obj) => { 
                        toolStripButtonLoadPresure.Enabled = true; 
                        toolStripButtonLoadFlow.Enabled = true;
                        toolStripButtonSaveFlow.Enabled = true;
                    }), this);

                    /* 刷新曲线显示 */
                    plotViewVFT.InvalidatePlot(true);
                    plotViewFV.InvalidatePlot(true);

                    /* 输出用力呼气 Volume-Time Plot */
                    UpdateVTPlot();

                    /* 更新 Flow-Volume Plot(平移到Volume从0开始) */
                    UpdateFVPlot();
                });
            }
        }

        /* 显示保存对话框,保存Flow数据为CSV文件 */
        private void ShowSaveCSVDialog()
        {
            /* 弹出文件保存对话框 */
            SaveFileDialog saveCSVDialog = new SaveFileDialog();
            saveCSVDialog.Filter = "CSV File (*.csv;)|*.csv";
            //saveCSVDialog.Multiselect = false;

            if (saveCSVDialog.ShowDialog() == DialogResult.OK)
            {
                if (String.IsNullOrEmpty(saveCSVDialog.FileName))
                {
                    return;
                }

                /* 保存过程中暂时不允许再次点击 */
                toolStripButtonLoadPresure.Enabled = false;
                toolStripButtonLoadFlow.Enabled = false;
                toolStripButtonSaveFlow.Enabled = false;

                /* 启动任务执行异步保存(防止阻塞UI线程) */
                Task.Factory.StartNew(() =>
                {
                    /* 在内存中将Flow数据组装称CSV格式字符串 */
                    StringBuilder strData = new StringBuilder();
                    foreach (var point in m_pointsVFTFlow)
                    {
                        strData.Append(point.Y);
                        strData.Append(",");
                    }

                    /* 保存为CSV文件 */
                    using (StreamWriter writer = new StreamWriter(saveCSVDialog.FileName, false, Encoding.UTF8))
                    {
                        writer.Write(strData);
                        writer.Close();

                        MessageBox.Show("保存成功.");
                    }

                    /* 保存完毕恢复按钮使能状态(确保在UI线程执行) */
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
                /* 开启流速传感器 */
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
                /* 关闭流速传感器 */
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

                    /* 输出用力呼气 Volume-Time Plot */
                    UpdateVTPlot();

                    /* 更新 Flow-Volume Plot(平移到Volume从0开始) */
                    UpdateFVPlot();
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
