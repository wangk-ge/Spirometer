using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Annotations;
using PulmonaryFunctionLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Spirometer
{
    public partial class FormCalibration : Form
    {
        private ConcurrentQueue<double> m_dataQueue = new ConcurrentQueue<double>(); // 数据队列
        private FlowSensor m_flowSensor; // 流量传感器
        private FlowCalibrator m_flowCalibrator; // 流量校准器
        private PlotModel m_plotModelPS; // 压差(Presure)-和值(Sum)图Model
        private PlotModel m_plotModelPT; // 压差(Presure)-时间(Time)图Model

        private System.Windows.Forms.Timer m_refreshTimer = new System.Windows.Forms.Timer(); // 波形刷新定时器
        private readonly int m_fps = 24; // 帧率

        private List<DataPoint> m_pointsPS; // 压差(Presure)-和值(Sum)数据
        private List<DataPoint> m_pointsPT; // 压差(Presure)-时间(Time)数据

        public FormCalibration(FlowSensor flowSensor)
        {
            m_flowSensor = flowSensor;
            /* 流量校准器 */
            m_flowCalibrator = new FlowCalibrator(m_flowSensor.SAMPLE_TIME);

            InitializeComponent();
        }

        private void FormCalibration_Load(object sender, EventArgs e)
        {
            /* 压差(Presure)-和值(Sum)图 */
            m_plotModelPS = new PlotModel()
            {
                Title = "压差(Presure)-和值(Sum)",
                LegendTitle = "图例",
                LegendOrientation = LegendOrientation.Horizontal,
                LegendPlacement = LegendPlacement.Inside,
                LegendPosition = LegendPosition.TopRight,
                LegendBackground = OxyColors.Beige,
                LegendBorder = OxyColors.Blue,
                IsLegendVisible = false // 隐藏图例
            };

            //X轴,Sum
            var xAxisPS = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Bottom,
                Minimum = -50,
                Maximum = 50,
                Title = "Sum"
            };
            m_plotModelPS.Axes.Add(xAxisPS);

            //Y轴,Presure
            var yAxisPS = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Left,
                Title = "Presure(inH2O)"
            };
            m_plotModelPS.Axes.Add(yAxisPS);

            // 数据
            var seriesPS = new LineSeries()
            {
                Color = OxyColors.DimGray,
                StrokeThickness = 1,
                //MarkerSize = 1,
                //MarkerStroke = OxyColors.DarkBlue,
                //MarkerType = MarkerType.Circle,
                Title = "Data"
            };
            m_plotModelPS.Series.Add(seriesPS);

            // 设置View对应的Model
            plotViewPS.Model = m_plotModelPS;
            // 保存数据点列表引用
            m_pointsPS = seriesPS.Points;

            /* 压差(Presure)-时间(Time)图 */
            m_plotModelPT = new PlotModel()
            {
                Title = "压差(Presure)-时间(Time)",
                LegendTitle = "图例",
                LegendOrientation = LegendOrientation.Horizontal,
                LegendPlacement = LegendPlacement.Inside,
                LegendPosition = LegendPosition.TopRight,
                LegendBackground = OxyColors.Beige,
                LegendBorder = OxyColors.Blue,
                IsLegendVisible = false // 隐藏图例
            };

            //X轴,Time
            var xAxisPT = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Bottom,
                Minimum = 0,
                Maximum = 5 * 1000,
                Title = "Time(MS)"
            };
            m_plotModelPT.Axes.Add(xAxisPT);

            //Y轴,Presure
            var yAxisPT = new LinearAxis()
            {
                //MajorGridlineStyle = LineStyle.Dot,
                //MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Left,
                Title = "Presure(inH2O)"
            };
            m_plotModelPT.Axes.Add(yAxisPT);

            // 数据
            var seriesPT = new LineSeries()
            {
                Color = OxyColors.DimGray,
                StrokeThickness = 1,
                //MarkerSize = 1,
                //MarkerStroke = OxyColors.DarkBlue,
                //MarkerType = MarkerType.Circle,
                Title = "Data"
            };
            m_plotModelPT.Series.Add(seriesPT);

            // 设置View对应的Model
            plotViewPT.Model = m_plotModelPT;
            // 保存数据点列表引用
            m_pointsPT = seriesPT.Points;

            /* 开始吸气 */
            m_flowCalibrator.InspirationStarted += new FlowCalibrator.InspirationStartHandler((uint sampleIndex) =>
            {
                Console.WriteLine($"InspirationStarted: {sampleIndex}");
                var annotation = new LineAnnotation()
                {
                    Color = OxyColors.Red,
                    X = m_flowCalibrator.GetTime(sampleIndex),
                    LineStyle = LineStyle.Dash,
                    Type = LineAnnotationType.Vertical,
                    Text = "吸气"
                };
                m_plotModelPT.Annotations.Add(annotation);
            });

            /* 开始呼气 */
            m_flowCalibrator.ExpirationStarted += new FlowCalibrator.ExpirationStartHandler((uint sampleIndex) =>
            {
                Console.WriteLine($"ExpirationStarted: {sampleIndex}");
                var annotation = new LineAnnotation()
                {
                    Color = OxyColors.Violet,
                    X = m_flowCalibrator.GetTime(sampleIndex),
                    LineStyle = LineStyle.Dash,
                    Type = LineAnnotationType.Vertical,
                    Text = $"呼气"
                };
                m_plotModelPT.Annotations.Add(annotation);
            });

            /* 测量结束 */
            m_flowCalibrator.MeasureStoped += new FlowCalibrator.MeasureStopHandler((uint sampleIndex, uint peekPresureIndex) =>
            {
                Console.WriteLine($"MeasureStoped: {sampleIndex}");
                var annotation = new LineAnnotation()
                {
                    Color = OxyColors.Red,
                    X = m_flowCalibrator.GetTime(sampleIndex),
                    LineStyle = LineStyle.Dash,
                    Type = LineAnnotationType.Vertical,
                    Text = "停止"
                };
                m_plotModelPT.Annotations.Add(annotation);

                /* 测量已停止 */
                OnMeasureStoped();
            });

            /* 通过传感器获取数据 */
            m_flowSensor.PresureRecved += new FlowSensor.PresureRecvHandler((byte channel, double presure) => {
                //Console.WriteLine($"PresureRecved: {channel} {presure}");

                /* 数据存入队列,将在刷新定时器中读取 */
                m_dataQueue.Enqueue(presure);
            });

            /* 刷新定时器 */
            m_refreshTimer.Interval = 1000 / m_fps; // 设置定时器超时时间为帧间隔
            m_refreshTimer.Tick += new EventHandler((timer, arg) => {
                // 保存数据添加前的曲线最右端X坐标的位置(用于实现自动滚屏)
                double xBegin = m_pointsPT.Count > 0 ? m_pointsPT.Last().X : 0;
                // 尝试读取队列中的数据并添加到曲线
                while (m_dataQueue.Count > 0)
                {
                    /* 尝试读取队列中的数 */
                    bool bRet = m_dataQueue.TryDequeue(out double presure); // 压差
                    if (!bRet)
                    {
                        break;
                    }

                    /* 已接收到一个压差采集数据 */
                    OnPresureRecved(presure / 10000000);
                }

                /* 在必要时刷新曲线显示并执行自动滚屏 */
                UpdatePTPlot(xBegin);
            });
        }

        /* 测量已停止 */
        private void OnMeasureStoped()
        {
            Console.WriteLine($"PresureSum: {m_flowCalibrator.PresureSum} \t PeekPresure: {m_flowCalibrator.PeekPresure} \t PresureAvg: {m_flowCalibrator.PresureAvg} \t K: {m_flowSensor.SAMPLE_RATE / m_flowCalibrator.PresureSum} \t PresureVariance: {m_flowCalibrator.PresureVariance}");

            m_flowCalibrator.Reset();
        }

        /* 已接收到一个压差采集数据 */
        private void OnPresureRecved(double presure)
        {
            /* 执行肺功能参数计算 */
            m_flowCalibrator.Input(presure);

            /* 加入数据到对应的曲线 */
            m_pointsPT.Add(new DataPoint(m_flowCalibrator.Time, m_flowCalibrator.Presure));
            m_pointsPS.Add(new DataPoint(m_flowCalibrator.PresureSum, m_flowCalibrator.Presure));
        }

        /* 更新 Presure-Time Plot,并执行自动滚屏 */
        private void UpdatePTPlot(double xBegin)
        {
            /* 在必要时刷新曲线显示并执行自动滚屏 */
            double xEnd = m_pointsPT.Count > 0 ? m_pointsPT.Last().X : 0;
            double xDelta = xEnd - xBegin;
            if (xDelta > 0)
            {
                /* 刷新曲线显示 */
                plotViewPT.InvalidatePlot(true);
                plotViewPS.InvalidatePlot(true);

                var xAxisPT = m_plotModelPT.Axes[0];
                if (xEnd > xAxisPT.Maximum)
                {
                    /* 自动滚屏 */
                    double panStep = xAxisPT.Transform(-xDelta + xAxisPT.Offset);
                    xAxisPT.Pan(panStep);
                }
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

        private async void toolStripButtonStart_Click(object sender, EventArgs e)
        {
            string cmd = "[ADC_START]"; // 启动
            if ("停止" == toolStripButtonStart.Text)
            {
                cmd = "[ADC_STOP]"; // 停止
            }
            Console.WriteLine($"Sned: {cmd}");
            string cmdResp = await m_flowSensor.ExcuteCmdAsync(cmd, 2000);
            Console.WriteLine($"Revc: {cmdResp}");

            if ("[OK]" == cmdResp)
            {
                if ("开始" == toolStripButtonStart.Text)
                {
                    cmd = "[ADC_CAL]"; // 归零

                    Console.WriteLine($"Sned: {cmd}");
                    cmdResp = await m_flowSensor.ExcuteCmdAsync(cmd, 2000);
                    Console.WriteLine($"Revc: {cmdResp}");
                    if ("[OK]" == cmdResp)
                    { // 归零成功
                        toolStripButtonStart.Text = "停止";
                        //ClearAll();
                        /* 清除旧的校准数据 */
                        m_flowSensor.ClearCalibrationParams();
                        /* 尝试清空数据队列 */
                        TryClearDataQueue();
                        /* 启动刷新定时器 */
                        m_refreshTimer.Start();
                    }
                }
                else // if ("停止" == toolStripButtonStart.Text)
                {
                    toolStripButtonStart.Text = "开始";
                    /* 停止刷新定时器 */
                    m_refreshTimer.Stop();
                    /* 尝试清空数据队列 */
                    TryClearDataQueue();
                }
            }
        }
    }
}
