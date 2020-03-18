using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Annotations;
using PulmonaryFunctionLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Spirometer
{
    public partial class FormCalibration : Form
    {
        private ConcurrentQueue<double> m_dataQueue = new ConcurrentQueue<double>(); // 数据队列
        private FlowSensor m_flowSensor; // 流量传感器
        private FlowCalibrator m_flowCalibrator; // 流量校准器
        private PlotModel m_plotModelPS; // 压差(Presure)-和值(Sum)图Model
        private PlotModel m_plotModelFP; // 流量(Flow)-压差(Presure)图Model
        private PlotModel m_plotModelPT; // 压差(Presure)-时间(Time)图Model

        private System.Windows.Forms.Timer m_refreshTimer = new System.Windows.Forms.Timer(); // 波形刷新定时器
        private readonly int PLOT_REFRESH_FPS = 24; // 帧率

        private List<DataPoint> m_pointsPS; // 压差(Presure)-和值(Sum)数据
        private List<DataPoint> m_pointsFP; // 流量(Flow)-压差(Presure)数据
        private List<DataPoint> m_pointsFPResult; // 流量(Flow)-压差(Presure)数据(校准结果全局)
        private List<DataPoint> m_pointsFPResultP; // 流量(Flow)-压差(Presure)数据(校准结果正方向)
        private List<DataPoint> m_pointsFPResultN; // 流量(Flow)-压差(Presure)数据(校准结果负方向)
        private List<DataPoint> m_pointsPT; // 压差(Presure)-时间(Time)数据

        private TaskCompletionSource<bool> m_dataPlotTaskComp; // 用于监控数据输出到Plot数据完成事件

        private bool m_calParamAviable = false; // 是否已计算得到校准参数

        private List<double> m_calParamValList = new List<double>(); // 全局拟合参数
        private List<double> m_calParamValListP = new List<double>(); // 正方向拟合参数
        private List<double> m_calParamValListN = new List<double>(); // 负方向拟合参数
        private double m_minPresure = 0.0; // 最小压差
        private double m_maxPresure = 0.0; // 最大压差

        public FormCalibration(FlowSensor flowSensor, double calVolume = 1.0)
        {
            /* 流量传感器 */
            m_flowSensor = flowSensor;

            /* 流量校准器 */
            m_flowCalibrator = new FlowCalibrator(m_flowSensor.SAMPLE_RATE, calVolume);

            InitializeComponent();

            this.Text = $"校准-{calVolume}L定标桶";
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
                Minimum = -55 * m_flowCalibrator.CalVolume,
                Maximum = 55 * m_flowCalibrator.CalVolume,
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

            //标记线+10%
            var annotationPS1 = new LineAnnotation()
            {
                Color = OxyColors.Red,
                X = -44 * m_flowCalibrator.CalVolume - 10,
                LineStyle = LineStyle.Dash,
                Type = LineAnnotationType.Vertical,
                Text = "+10%"
            };
            m_plotModelPS.Annotations.Add(annotationPS1);

            //标记线-10%
            var annotationPS2 = new LineAnnotation()
            {
                Color = OxyColors.Red,
                X = -44 * m_flowCalibrator.CalVolume + 10,
                LineStyle = LineStyle.Dash,
                Type = LineAnnotationType.Vertical,
                Text = "-10%"
            };
            m_plotModelPS.Annotations.Add(annotationPS2);

            //标记线-10%
            var annotationPS3 = new LineAnnotation()
            {
                Color = OxyColors.Red,
                X = 44 * m_flowCalibrator.CalVolume - 10,
                LineStyle = LineStyle.Dash,
                Type = LineAnnotationType.Vertical,
                Text = "-10%"
            };
            m_plotModelPS.Annotations.Add(annotationPS3);

            //标记线+10%
            var annotationPS4 = new LineAnnotation()
            {
                Color = OxyColors.Red,
                X = 44 * m_flowCalibrator.CalVolume + 10,
                LineStyle = LineStyle.Dash,
                Type = LineAnnotationType.Vertical,
                Text = "+10%"
            };
            m_plotModelPS.Annotations.Add(annotationPS4);

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

            /* 流量(Flow)-压差(Presure)图 */
            m_plotModelFP = new PlotModel()
            {
                Title = "流量(Flow)-压差(Presure)",
                LegendTitle = "图例",
                LegendOrientation = LegendOrientation.Horizontal,
                LegendPlacement = LegendPlacement.Inside,
                LegendPosition = LegendPosition.TopRight,
                LegendBackground = OxyColors.Beige,
                LegendBorder = OxyColors.Blue,
                IsLegendVisible = false // 隐藏图例
            };

            //X轴,Presure
            var xAxisFP = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Bottom,
                //Minimum = -55 * m_flowCalibrator.CalVolume,
                //Maximum = 55 * m_flowCalibrator.CalVolume,
                Title = "Presure(inH2O)"
            };
            m_plotModelFP.Axes.Add(xAxisFP);

            //Y轴,Flow
            var yAxisFP = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Left,
                Title = "Flow(L/S)"
            };
            m_plotModelFP.Axes.Add(yAxisFP);

            // 数据
            var seriesFP = new LineSeries()
            {
                Color = OxyColors.DimGray,
                StrokeThickness = 0,
                MarkerSize = 1,
                MarkerStroke = OxyColors.DimGray,
                MarkerType = MarkerType.Circle,
                Title = "Data"
            };
            m_plotModelFP.Series.Add(seriesFP);

            // 校准结果(全局)
            var seriesFPResult = new LineSeries()
            {
                Color = OxyColors.Red,
                StrokeThickness = 1,
                //MarkerSize = 1,
                //MarkerStroke = OxyColors.DarkRed,
                //MarkerType = MarkerType.Circle,
                Title = "Result"
            };
            m_plotModelFP.Series.Add(seriesFPResult);

            // 校准结果(正方向)
            var seriesFPResultP = new LineSeries()
            {
                Color = OxyColors.Blue,
                StrokeThickness = 1,
                //MarkerSize = 1,
                //MarkerStroke = OxyColors.DarkRed,
                //MarkerType = MarkerType.Circle,
                Title = "ResultP"
            };
            m_plotModelFP.Series.Add(seriesFPResultP);

            // 校准结果(负方向)
            var seriesFPResultN = new LineSeries()
            {
                Color = OxyColors.Green,
                StrokeThickness = 1,
                //MarkerSize = 1,
                //MarkerStroke = OxyColors.DarkRed,
                //MarkerType = MarkerType.Circle,
                Title = "ResultN"
            };
            m_plotModelFP.Series.Add(seriesFPResultN);

            // 设置View对应的Model
            plotViewFP.Model = m_plotModelFP;
            // 保存数据点列表引用
            m_pointsFP = seriesFP.Points;
            m_pointsFPResult = seriesFPResult.Points;
            m_pointsFPResultP = seriesFPResultP.Points;
            m_pointsFPResultN = seriesFPResultN.Points;

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
                Maximum = 30 * 1000,
                Title = "Time(MS)"
            };
            m_plotModelPT.Axes.Add(xAxisPT);

            //Y轴,Presure
            var yAxisPT = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.Dot,
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

            /* 采样开始 */
            m_flowCalibrator.SampleStarted += new FlowCalibrator.SampleStartHandler((uint presureIndex, FlowCalibrator.RespireDirection direction) =>
            {
                Console.WriteLine($"SampleStarted: {presureIndex} {direction}");
                var annotation = new LineAnnotation()
                {
                    Color = OxyColors.Red,
                    X = m_flowCalibrator.GetTime(presureIndex),
                    LineStyle = LineStyle.Dash,
                    Type = LineAnnotationType.Vertical,
                    Text = (direction == FlowCalibrator.RespireDirection.Inspiration) ? "吸气" : "呼气",
                };
                m_plotModelPT.Annotations.Add(annotation);
            });

            /* 采样结束 */
            m_flowCalibrator.SampleStoped += new FlowCalibrator.SampleStopHandler((uint presureIndex, FlowCalibrator.RespireDirection direction, uint sampleIndex) =>
            {
                Console.WriteLine($"SampleStoped: {presureIndex} {direction} {sampleIndex}");
                var annotation = new LineAnnotation()
                {
                    Color = OxyColors.Red,
                    X = m_flowCalibrator.GetTime(presureIndex),
                    LineStyle = LineStyle.Dash,
                    Type = LineAnnotationType.Vertical,
                    Text = "停止"
                };
                m_plotModelPT.Annotations.Add(annotation);

                /* 采样已停止 */
                OnSampleStoped(sampleIndex, direction);
            });

            /* 刷新定时器 */
            m_refreshTimer.Interval = 1000 / PLOT_REFRESH_FPS; // 设置定时器超时时间为帧间隔
            m_refreshTimer.Tick += new EventHandler((timer, arg) => {
                // 保存数据添加前的曲线最右端X坐标的位置(用于实现自动滚屏)
                double xBegin = m_pointsPT.Count > 0 ? m_pointsPT.Last().X : 0;
                // 尝试读取队列中的数据并添加到曲线
                while (m_dataQueue.Count > 0)
                {
                    /* 尝试读取队列中的数据 */
                    bool bRet = m_dataQueue.TryDequeue(out double presure); // 压差
                    if (!bRet)
                    {
                        break;
                    }

                    /* 已接收到一个压差采集数据 */
                    OnPresureRecved(presure);
                }

                /* 在必要时刷新曲线显示并执行自动滚屏 */
                UpdatePTPlot(xBegin);

                /* 通知数据已输出到Plot完毕 */
                m_dataPlotTaskComp?.SetResult(true);
            });
        }

        /* 收到传感器数据事件处理 */
        private void OnPresureRecved(byte channel, double presure)
        {
            //Console.WriteLine($"OnPresureRecved: {channel} {presure}");

            /* 压差数据存入队列,将在刷新定时器中读取 */
            m_dataQueue.Enqueue(presure);
        }

        /* 样本信息列表 */
        private void ClearSampleInfoDataGridView()
        {
            dataGridViewSampleInfo.Rows.Clear();
        }

        /* 清空结果列表 */
        private void ClearResultDataGridView()
        {
            dataGridViewResult.Rows.Clear();
        }

        /* 设置校准参数到列表显示 */
        private void SetCaliParamToDataGridView(List<double> calParamValList, List<double> calParamValListP, List<double> calParamValListN)
        {
            dataGridViewResult.Rows.Clear();
            for (int i = 0; i < calParamValList.Count; ++i)
            {
                var param = calParamValList[i];
                int index = dataGridViewResult.Rows.Add();
                dataGridViewResult.Rows[index].Cells[0].Value = param;
                if (i < calParamValListP.Count)
                {
                    var paramP = calParamValListP[i];
                    dataGridViewResult.Rows[index].Cells[1].Value = paramP;
                }
                if (i < calParamValListN.Count)
                {
                    var paramN = calParamValListN[i];
                    dataGridViewResult.Rows[index].Cells[2].Value = paramN;
                }
            }
        }

        /* 更新Flow-Presure Plot */
        private void UpdateFPPlot(double presure, double flow)
        {
            double x = presure;
            double y = flow;
            /* 按presure升序插入(presure重复项,flow求均值) */
            int i = 0;
            for (i = 0; i < m_pointsFP.Count; ++i)
            {
                if (x >= m_pointsFP[i].X)
                {
                    break;
                }
            }
            if (i < m_pointsFP.Count)
            {
                if (x == m_pointsFP[i].X)
                {
                    y = (y + m_pointsFP[i].Y) / 2.0;
                    m_pointsFP[i] = new DataPoint(x, y); // 替换
                }
                else
                {
                    m_pointsFP.Insert(i, new DataPoint(x, y)); // 插入
                }
            }
            else
            {
                m_pointsFP.Add(new DataPoint(x, y));
            }

            /* 更新校准结果 */
            if (m_calParamAviable)
            {
                m_pointsFPResult.Clear();
                m_pointsFPResultP.Clear();
                m_pointsFPResultN.Clear();

                for (double p = m_minPresure - 1; p <= m_maxPresure + 1; p += 0.001)
                {
#if true
                    double f = FlowCalibrator.PresureToFlow(p, m_calParamValList, 
                        m_calParamValListP, m_calParamValListN, 
                        m_minPresure, m_maxPresure);
                    m_pointsFPResult.Add(new DataPoint(p, f));
#else
                    double f = FlowCalibrator.PresureToFlow(p, m_calParamValList);
                    m_pointsFPResult.Add(new DataPoint(p, f));

                    f = FlowCalibrator.PresureToFlow(p, m_calParamValListP);
                    m_pointsFPResultP.Add(new DataPoint(p, f));

                    f = FlowCalibrator.PresureToFlow(p, m_calParamValListN);
                    m_pointsFPResultN.Add(new DataPoint(p, f));
#endif
                }
            }

            plotViewFP.InvalidatePlot(true);
        }

        /* 尝试计算校准参数并更新显示 */
        private bool TryCalcAndUpdateCaliParam()
        {
            /* 取得挑选的样本列表 */
            List<uint> sampleList = new List<uint>();
            for (int i = 0; i < dataGridViewSampleInfo.Rows.Count; ++i)
            {
                bool bApply = (bool)dataGridViewSampleInfo.Rows[i].Cells[7].Value;
                if (bApply)
                {
                    sampleList.Add((uint)i);
                }
            }

            /* 更新状态栏(样本个数) */
            toolStripStatusLabelSampleCount.Text = sampleList.Count.ToString();

            /* 如果没有选择任何样本 */
            if (sampleList.Count <= 0)
            {
                return false;
            }

            /* 尝试计算校准参数 */
            bool bRet = m_flowCalibrator.CalcCalibrationParams(m_calParamValList, m_calParamValListP, m_calParamValListN,
                out m_minPresure, out m_maxPresure, sampleList);
            if (bRet)
            {
                /* 设置校准参数到列表 */
                SetCaliParamToDataGridView(m_calParamValList, m_calParamValListP, m_calParamValListN);

                /* 更新状态栏(参数个数) */
                toolStripStatusLabelParamCount.Text = m_calParamValList.Count.ToString();
            }

            return bRet;
        }

        /* 采样已停止 */
        private void OnSampleStoped(uint sampleIndex, FlowCalibrator.RespireDirection direction)
        {
            double flowAvg = m_flowCalibrator.SampleFlowAvg(sampleIndex);
            double presureAvg = m_flowCalibrator.SamplePresureAvg(sampleIndex);
            double presureFlowScale = m_flowCalibrator.SamplePresureAvgToFlowScale(sampleIndex);
            double presureSum = m_flowCalibrator.SamplePresureSum(sampleIndex);
            double peekPresure = (direction == FlowCalibrator.RespireDirection.Inspiration) ? 
                m_flowCalibrator.SampleMaxPresure(sampleIndex) : m_flowCalibrator.SampleMinPresure(sampleIndex);
            double presureVariance = m_flowCalibrator.SamplePresureVariance(sampleIndex);
            bool bIsSampleValid = m_flowCalibrator.SampleIsValid(sampleIndex);

            /* 添加样本信息到列表 */
            int index = dataGridViewSampleInfo.Rows.Add();
            dataGridViewSampleInfo.Rows[index].Cells[0].Value = (direction == FlowCalibrator.RespireDirection.Inspiration) ? "吸气" : "呼气";
            dataGridViewSampleInfo.Rows[index].Cells[1].Value = presureFlowScale;
            dataGridViewSampleInfo.Rows[index].Cells[2].Value = flowAvg;
            dataGridViewSampleInfo.Rows[index].Cells[3].Value = presureAvg;
            dataGridViewSampleInfo.Rows[index].Cells[4].Value = presureSum;
            dataGridViewSampleInfo.Rows[index].Cells[5].Value = peekPresure;
            dataGridViewSampleInfo.Rows[index].Cells[6].Value = presureVariance;
            dataGridViewSampleInfo.Rows[index].Cells[7].Value = bIsSampleValid;

            /* 尝试计算校准参数并更新显示 */
            m_calParamAviable = TryCalcAndUpdateCaliParam();

            /* 数据加入Flow-Presure Plot并更新显示 */
            if (bIsSampleValid)
            {
                UpdateFPPlot(presureAvg, flowAvg);
            }

            /* 重置校准器,开始检测下一次校准启动 */
            m_flowCalibrator.Reset();
        }

        /* 已接收到一个压差采集数据 */
        private void OnPresureRecved(double presure)
        {
            /* 输入到流量校准器 */
            m_flowCalibrator.Input(presure);

            /* 加入数据到对应的曲线 */
            m_pointsPT.Add(new DataPoint(m_flowCalibrator.Time, m_flowCalibrator.Presure));
            if (m_flowCalibrator.StartSampling)
            {
                m_pointsPS.Add(new DataPoint(m_flowCalibrator.CurrSamplePresureSum, m_flowCalibrator.Presure));
            }
        }

        /* 更新 Presure-Time Plot,并执行自动滚屏 */
        private void UpdatePTPlot(double xBegin)
        {
            /* 在必要时刷新曲线显示并执行自动滚屏 */
            double xEnd = (m_pointsPT.Count > 0) ? m_pointsPT.Last().X : 0;
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

        /* 清空所有图表数据和缓存数据,并刷新显示 */
        private void ClearAll()
        {
            /* 尝试清空数据队列 */
            TryClearDataQueue();

            /* 清除 */
            m_flowCalibrator.Clear();
            m_calParamAviable = false;

            /* 清空结果列表 */
            ClearResultDataGridView();

            /* 清空样本信息列表 */
            ClearSampleInfoDataGridView();

            /* Clear Presure-Time Plot */
            m_pointsPT.Clear();
            m_plotModelPT.Annotations.Clear();
            var xAxisPT = m_plotModelPT.Axes[0];
            xAxisPT.Reset();
            var yAxisPT = m_plotModelPT.Axes[1];
            yAxisPT.Reset();

            /* Clear Flow-Presure Plot */
            m_pointsFP.Clear();
            m_pointsFPResult.Clear();
            m_pointsFPResultP.Clear();
            m_pointsFPResultN.Clear();
            m_plotModelFP.Annotations.Clear();
            var xAxisFP = m_plotModelFP.Axes[0];
            xAxisFP.Reset();
            var yAxisFP = m_plotModelFP.Axes[1];
            yAxisFP.Reset();

            /* Clear Presure-Sum Plot */
            m_pointsPS.Clear();
            m_plotModelPS.Annotations.Clear();
            var xAxisPS = m_plotModelPS.Axes[0];
            xAxisPS.Reset();
            var yAxisPS = m_plotModelPS.Axes[1];
            yAxisPS.Reset();

            /* 刷新曲线显示 */
            plotViewPT.InvalidatePlot(true);
            plotViewFP.InvalidatePlot(true);
            plotViewPS.InvalidatePlot(true);
        }

        private async Task LoadCSVFileAsync(string filePath)
        {
            /* 先清空所有图表数据和缓存数据,并刷新显示 */
            ClearAll();

            /* 启动刷新定时器 */
            m_refreshTimer.Start();

            /* 启动任务执行异步加载(防止阻塞UI线程) */
            Task loadTask = Task.Factory.StartNew((Action)(() =>
            {
                try
                {
                    /* 所有数据先加载到内存 */
                    string strData = string.Empty;
                    using (StreamReader reader = new StreamReader(filePath, Encoding.UTF8))
                    {
                        strData = reader.ReadToEnd();
                        reader.Close();
                    }

                    /* 解析CSV中的压差数据 */
                    string[] strDataArray = strData.Split(new char[] { ',' });
                    foreach (var strVal in strDataArray)
                    {
                        if (string.Empty == strVal)
                        {
                            continue;
                        }

                        double presure = Convert.ToDouble(strVal); // 压差

                        OnPresureRecved(0, presure);

                        /* 模拟采样率 */
                        //Thread.Sleep((int)m_flowSensor.SAMPLE_TIME);
                    }

                    /* 数据已加载完毕 */

                    /* 建Task完成事件对象(用于监测数据是否已全部输出到Plot) */
                    m_dataPlotTaskComp = new TaskCompletionSource<bool>();
                }
                catch (Exception e)
                {
                    MessageBox.Show($"加载失败!{e.ToString()}");
                }
            }));

            /* 异步等待完成对象被触发(数据加载完毕并且已全部输出到Plot) */
            await loadTask; // 先确保数据已加载完毕(只有此时m_dataPlotTaskComp才非空)
            if (null != m_dataPlotTaskComp)
            {
                await m_dataPlotTaskComp.Task; // 然后确保数据已全部输出到Plot

                /* 清除完成对象 */
                m_dataPlotTaskComp = null;
            }
        }

        /* 显示保存对话框,保存Presure数据为CSV文件 */
        private void ShowSaveCSVDialog()
        {
            /* 弹出文件保存对话框 */
            SaveFileDialog saveCSVDialog = new SaveFileDialog();
            saveCSVDialog.Filter = "CSV File (*.csv;)|*.csv";
            //saveCSVDialog.Multiselect = false;
            saveCSVDialog.FileName = $"calibbration_{DateTime.Now.ToString("yyyyMMdd_hhmmss")}.csv";

            if (saveCSVDialog.ShowDialog() == DialogResult.OK)
            {
                if (String.IsNullOrEmpty(saveCSVDialog.FileName))
                {
                    return;
                }

                /* 保存过程中暂时不允许点击工具按钮 */
                bool toolStripButtonStartEnabled = toolStripButtonStart.Enabled;
                toolStripButtonStart.Enabled = false;

                bool toolStripButtonSavePresureEnabled = toolStripButtonSavePresure.Enabled;
                toolStripButtonSavePresure.Enabled = false;

                bool toolStripButtonLoadPresureEnabled = toolStripButtonLoadPresure.Enabled;
                toolStripButtonLoadPresure.Enabled = false;

                bool toolStripButtonClearEnabled = toolStripButtonClear.Enabled;
                toolStripButtonClear.Enabled = false;

                bool toolStripButtonApplyEnabled = toolStripButtonApply.Enabled;
                toolStripButtonApply.Enabled = false;

                /* 启动任务执行异步保存(防止阻塞UI线程) */
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        /* 在内存中将Presure数据组装称CSV格式字符串 */
                        StringBuilder strData = new StringBuilder();
                        foreach (var point in m_pointsPT)
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
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show($"保存失败！{e.ToString()}");
                    }
                    finally
                    {
                        /* 恢复工具按钮使能状态(确保在UI线程执行) */
                        this.BeginInvoke(new Action<FormCalibration>((obj) => {
                            toolStripButtonStart.Enabled = toolStripButtonStartEnabled;
                            toolStripButtonSavePresure.Enabled = toolStripButtonSavePresureEnabled;
                            toolStripButtonLoadPresure.Enabled = toolStripButtonLoadPresureEnabled;
                            toolStripButtonClear.Enabled = toolStripButtonClearEnabled;
                            toolStripButtonApply.Enabled = toolStripButtonApplyEnabled;
                        }), this);
                    }
                });
            }
        }

        /* 显示加载对话框,加载Preaure数据 */
        private async void ShowLoadCSVDialog()
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

                /* 加载过程中暂时不允许点击工具按钮 */
                bool toolStripButtonStartEnabled = toolStripButtonStart.Enabled;
                toolStripButtonStart.Enabled = false;

                bool toolStripButtonSavePresureEnabled = toolStripButtonSavePresure.Enabled;
                toolStripButtonSavePresure.Enabled = false;

                bool toolStripButtonLoadPresureEnabled = toolStripButtonLoadPresure.Enabled;
                toolStripButtonLoadPresure.Enabled = false;

                bool toolStripButtonClearEnabled = toolStripButtonClear.Enabled;
                toolStripButtonClear.Enabled = false;

                bool toolStripButtonApplyEnabled = toolStripButtonApply.Enabled;
                toolStripButtonApply.Enabled = false;

                /* 加载CSV文件中的数据 */
                await LoadCSVFileAsync(openCSVDialog.FileName);

                /* 加载完毕恢复工具按钮使能状态 */
                toolStripButtonStart.Enabled = toolStripButtonStartEnabled;
                toolStripButtonSavePresure.Enabled = toolStripButtonSavePresureEnabled;
                toolStripButtonLoadPresure.Enabled = toolStripButtonLoadPresureEnabled;
                toolStripButtonClear.Enabled = toolStripButtonClearEnabled;
                toolStripButtonApply.Enabled = toolStripButtonApplyEnabled;
            }
        }

        private async void toolStripButtonStart_Click(object sender, EventArgs e)
        {
            bool bRet = false;

            if ("开始" == toolStripButtonStart.Text)
            {
                bRet = await m_flowSensor.StartAsync(); // 启动
            }
            else //if ("停止" == toolStripButtonStart.Text)
            {
                bRet = await m_flowSensor.StopAsync(); // 停止
            }

            if (bRet)
            {
                if ("开始" == toolStripButtonStart.Text)
                {
                    bRet = await m_flowSensor.ZeroingAsync(); // 归零

                    if (bRet)
                    { // 归零成功
                        toolStripButtonStart.Text = "停止";
                        toolStripButtonSavePresure.Enabled = false;
                        toolStripButtonLoadPresure.Enabled = false;
                        toolStripButtonClear.Enabled = false;
                        toolStripButtonApply.Enabled = false;
                        //ClearAll();
                        /* 尝试清空数据队列 */
                        TryClearDataQueue();
                        /* 监听流量传感器数据收取事件 */
                        m_flowSensor.PresureRecved += OnPresureRecved;
                        /* 启动刷新定时器 */
                        m_refreshTimer.Start();
                    }
                }
                else // if ("停止" == toolStripButtonStart.Text)
                {
                    toolStripButtonStart.Text = "开始";
                    toolStripButtonSavePresure.Enabled = true;
                    toolStripButtonLoadPresure.Enabled = true;
                    toolStripButtonClear.Enabled = true;
                    toolStripButtonApply.Enabled = m_calParamAviable;
                    /* 取消监听流量传感器数据收取事件 */
                    m_flowSensor.PresureRecved -= OnPresureRecved;
                    /* 停止刷新定时器 */
                    m_refreshTimer.Stop();
                    /* 尝试清空数据队列 */
                    TryClearDataQueue();
                }
            }
        }

        private void toolStripButtonApply_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("将替换现有校准参数,是否继续？", "确认", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            if (dialogResult != DialogResult.Yes)
            {
                return;
            }

            if (m_calParamAviable)
            {
                if (m_calParamValList.Count > 0)
                {
                    m_flowSensor.SetCalibrationParamList(m_calParamValList, m_calParamValListP, m_calParamValListN, m_minPresure, m_maxPresure);
                    /* 保存校准参数 */
                    Properties.Settings.Default.caliValList = string.Join(",", m_calParamValList.ToArray());
                    Properties.Settings.Default.caliValListP = string.Join(",", m_calParamValListP.ToArray());
                    Properties.Settings.Default.caliValListN = string.Join(",", m_calParamValListN.ToArray());
                    Properties.Settings.Default.minPresure = m_minPresure.ToString();
                    Properties.Settings.Default.maxPresure = m_maxPresure.ToString();
                    Properties.Settings.Default.Save();
                    MessageBox.Show("应用校准参数【成功】!");
                }
                else
                {
                    MessageBox.Show("应用校准参数【失败】!");
                }
            }
            else
            {
                MessageBox.Show("应用校准参数【失败】!");
            }
        }

        private void toolStripButtonSavePresure_Click(object sender, EventArgs e)
        {
            ShowSaveCSVDialog();
        }

        private void toolStripButtonLoadPresure_Click(object sender, EventArgs e)
        {
            ShowLoadCSVDialog();
        }

        private void toolStripButtonClear_Click(object sender, EventArgs e)
        {
            ClearAll();
        }

        private void FormCalibration_FormClosing(object sender, FormClosingEventArgs e)
        {
            /* 停止传感器 */
            m_flowSensor.Stop();

            /* 取消监听流量传感器数据收取事件 */
            m_flowSensor.PresureRecved -= OnPresureRecved;
        }
    }
}
