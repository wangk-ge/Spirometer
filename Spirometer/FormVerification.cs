using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using PulmonaryFunctionLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Spirometer
{
    public partial class FormVerification : Form
    {
        private List<double> m_presureList = new List<double>(); // 压差数据列表
        private ConcurrentQueue<double> m_flowQueue = new ConcurrentQueue<double>(); // 流量数据队列
        private FlowSensor m_flowSensor; // 流量传感器
        private FlowValidator m_flowValidator; // 流量验证器
        private PlotModel m_plotModelFV; // 流量(Flow)-容积(Volume)图Model
        private PlotModel m_plotModelFT; // 流量(Flow)-时间(Time)图Model

        private TaskCompletionSource<bool> m_dataPlotTaskComp; // 用于监控数据输出到Plot数据完成事件

        private System.Windows.Forms.Timer m_refreshTimer = new System.Windows.Forms.Timer(); // 波形刷新定时器
        private readonly int PLOT_REFRESH_FPS = 24; // 图表刷新率

        private List<DataPoint> m_pointsFV; // 流量(Flow)-容积(Volume)数据
        private List<DataPoint> m_pointsFT; // 流量(Flow)-时间(Time)数据

        private readonly double ALLOW_ERROR_RATE = 3.0; // 允许的误差率(+-3%)
        private uint m_sampleCount = 0; // 参与验证的样本个数
        private uint m_passCount = 0; // 通过验证的样本个数
        private double m_errorRateMaxP = 0.0; // 正误差率最大值
        private double m_errorRateMaxN = 0.0; // 负误差率最大值
        private double m_errorRateAbsSum = 0.0; // 误差率绝对值求和值

        public FormVerification(FlowSensor flowSensor, double calVolume = 1.0, double allowErrorRate = 3.0)
        {
            /* 允许的误差率 */
            ALLOW_ERROR_RATE = allowErrorRate;
            
            /* 流量传感器 */
            m_flowSensor = flowSensor;

            /* 流量验证器 */
            m_flowValidator = new FlowValidator(m_flowSensor.SAMPLE_RATE, calVolume);

            InitializeComponent();

            this.Text = $"验证-{calVolume}L定标桶";
        }

        private void FormVerification_Load(object sender, EventArgs e)
        {
            /* 流量(Flow)-容积(Volume)图 */
            m_plotModelFV = new PlotModel()
            {
                Title = "流量(Flow)-容积(Volume)",
                LegendTitle = "图例",
                LegendOrientation = LegendOrientation.Horizontal,
                LegendPlacement = LegendPlacement.Inside,
                LegendPosition = LegendPosition.TopRight,
                LegendBackground = OxyColors.Beige,
                LegendBorder = OxyColors.Blue,
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
                Minimum = -2 * m_flowValidator.CalVolume,
                Maximum = 2 * m_flowValidator.CalVolume,
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

            // (负方向)标记线+3%
            var annotationFV1 = new LineAnnotation()
            {
                Color = OxyColors.Red,
                X = -m_flowValidator.CalVolume * (1 + 0.03),
                LineStyle = LineStyle.Dash,
                Type = LineAnnotationType.Vertical,
                Text = "+3%"
            };
            m_plotModelFV.Annotations.Add(annotationFV1);

            // (负方向)标记线-3%
            var annotationFV2 = new LineAnnotation()
            {
                Color = OxyColors.Red,
                X = -m_flowValidator.CalVolume * (1 - 0.03),
                LineStyle = LineStyle.Dash,
                Type = LineAnnotationType.Vertical,
                Text = "-3%"
            };
            m_plotModelFV.Annotations.Add(annotationFV2);

            // (正方向)标记线-3%
            var annotationFV3 = new LineAnnotation()
            {
                Color = OxyColors.Red,
                X = m_flowValidator.CalVolume * (1 - 0.03),
                LineStyle = LineStyle.Dash,
                Type = LineAnnotationType.Vertical,
                Text = "-3%"
            };
            m_plotModelFV.Annotations.Add(annotationFV3);

            // (正方向)标记线+3%
            var annotationFV4 = new LineAnnotation()
            {
                Color = OxyColors.Red,
                X = m_flowValidator.CalVolume * (1 + 0.03),
                LineStyle = LineStyle.Dash,
                Type = LineAnnotationType.Vertical,
                Text = "+3%"
            };
            m_plotModelFV.Annotations.Add(annotationFV4);

            // 数据
            var seriesFV = new LineSeries()
            {
                Color = OxyColors.DimGray,
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

            /* 流量(Flow)-时间(Time)图 */
            m_plotModelFT = new PlotModel()
            {
                Title = "流量(Flow)-时间(Time)",
                LegendTitle = "图例",
                LegendOrientation = LegendOrientation.Horizontal,
                LegendPlacement = LegendPlacement.Inside,
                LegendPosition = LegendPosition.TopRight,
                LegendBackground = OxyColors.Beige,
                LegendBorder = OxyColors.Blue,
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
                Maximum = 30 * 1000,
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
                Color = OxyColors.DimGray,
                StrokeThickness = 1,
                //MarkerSize = 1,
                //MarkerStroke = OxyColors.DarkBlue,
                //MarkerType = MarkerType.Circle,
                Title = "Data"
            };
            m_plotModelFT.Series.Add(seriesFT);

            // 设置View对应的Model
            plotViewFT.Model = m_plotModelFT;
            // 保存数据点列表引用
            m_pointsFT = seriesFT.Points;

            /* 采样开始 */
            m_flowValidator.SampleStarted += new FlowValidator.SampleStartHandler((uint flowIndex, FlowValidator.RespireDirection direction) =>
            {
                Console.WriteLine($"SampleStarted: {flowIndex} {direction}");
                var annotation = new LineAnnotation()
                {
                    Color = OxyColors.Red,
                    X = m_flowValidator.GetTime(flowIndex),
                    LineStyle = LineStyle.Dash,
                    Type = LineAnnotationType.Vertical,
                    Text = (direction == FlowValidator.RespireDirection.Inspiration) ? "吸气" : "呼气",
                };
                m_plotModelFT.Annotations.Add(annotation);
            });

            /* 采样结束 */
            m_flowValidator.SampleStoped += new FlowValidator.SampleStopHandler((uint flowIndex, FlowValidator.RespireDirection direction, uint sampleIndex) =>
            {
                Console.WriteLine($"SampleStoped: {flowIndex} {direction} {sampleIndex}");
                var annotation = new LineAnnotation()
                {
                    Color = OxyColors.Red,
                    X = m_flowValidator.GetTime(flowIndex),
                    LineStyle = LineStyle.Dash,
                    Type = LineAnnotationType.Vertical,
                    Text = "停止"
                };
                m_plotModelFT.Annotations.Add(annotation);

                /* 采样已停止 */
                OnSampleStoped(sampleIndex, direction);
            });

            /* 刷新定时器 */
            m_refreshTimer.Interval = 1000 / PLOT_REFRESH_FPS; // 设置定时器超时时间为帧间隔
            m_refreshTimer.Tick += new EventHandler((timer, arg) => {
                // 保存数据添加前的曲线最右端X坐标的位置(用于实现自动滚屏)
                double xBegin = m_pointsFT.Count > 0 ? m_pointsFT.Last().X : 0;
                // 尝试读取队列中的数据并添加到曲线
                while (m_flowQueue.Count > 0)
                {
                    /* 尝试读取队列中的数据 */
                    bool bRet = m_flowQueue.TryDequeue(out double flow); // 流量
                    if (!bRet)
                    {
                        break;
                    }

                    /* 已接收到一个流量采集数据 */
                    OnFlowRecved(flow);
                }

                /* 在必要时刷新曲线显示并执行自动滚屏 */
                UpdateFTPlot(xBegin);

                /* 通知数据已输出到Plot完毕 */
                m_dataPlotTaskComp?.SetResult(true);
            });
        }

        /* 收到传感器数据事件处理 */
        private void OnPresureRecved(byte channel, double presure)
        {
            //Console.WriteLine($"OnPresureRecved: {channel} {presure}");

            /* 压差数据存入队列 */
            lock(m_presureList)
            {
                m_presureList.Add(presure);
            }

            /* 压差转流量 */
            double flow = m_flowSensor.PresureToFlow(presure); // 流量

            /* 流量数据存入队列,将在刷新定时器中读取 */
            m_flowQueue.Enqueue(flow);
        }

        /* 样本信息列表 */
        private void ClearSampleInfoDataGridView()
        {
            dataGridViewSampleInfo.Rows.Clear();
        }

        /* 统计误差数据(并更新状态栏信息显示) */
        private void StatisticalErrorInfo(double errRate)
        {
            m_sampleCount++;
            if (Math.Abs(errRate) < ALLOW_ERROR_RATE)
            { // 误差率在允许范围内
                m_passCount++;
            }

            /* 统计+-最大误差率 */
            if (errRate > 0)
            {
                if (errRate > m_errorRateMaxP)
                {
                    m_errorRateMaxP = errRate;
                }
            }
            else if (errRate < 0)
            {
                if (errRate < m_errorRateMaxN)
                {
                    m_errorRateMaxN = errRate;
                }
            }

            /* 统计误差率绝对值求和值 */
            m_errorRateAbsSum += Math.Abs(errRate);

            toolStripStatusLabelSampleCount.Text = m_sampleCount.ToString(); // 样本个数
            toolStripStatusLabelPassRate.Text = $"{(m_passCount * 100.0 / m_sampleCount).ToString("f2")}%"; // 通过率
            toolStripStatusLabelErrRateMaxP.Text = $"{m_errorRateMaxP.ToString("f2")}%"; // 最大误差率(正)
            toolStripStatusLabelErrRateMaxN.Text = $"{m_errorRateMaxN.ToString("f2")}%"; // 最大误差率(负)
            toolStripStatusLabelErrRateAbsAvg.Text = $"{(m_errorRateAbsSum / m_sampleCount).ToString("f2")}%"; // 误差率(绝对值)平均值
        }

        /* 采样已停止 */
        private void OnSampleStoped(uint sampleIndex, FlowValidator.RespireDirection direction)
        {
            double flowAvg = m_flowValidator.SampleFlowAvg(sampleIndex);
            double flowPeek = (direction == FlowValidator.RespireDirection.Inspiration) ? 
                m_flowValidator.SampleMaxFlow(sampleIndex) : m_flowValidator.SampleMinFlow(sampleIndex);
            double flowVariance = m_flowValidator.SampleFlowVariance(sampleIndex);
            double volume = m_flowValidator.SampleVolume(sampleIndex);
            double volumeError = m_flowValidator.SampleVolumeError(sampleIndex);
            double volumeErrorRate = m_flowValidator.SampleVolumeErrorRate(sampleIndex);
            bool bIsSampleValid = m_flowValidator.SampleIsValid(sampleIndex);

            /* 添加样本信息到列表 */
            int index = dataGridViewSampleInfo.Rows.Add();
            dataGridViewSampleInfo.Rows[index].Cells[0].Value = (direction == FlowValidator.RespireDirection.Inspiration) ? "吸气" : "呼气";
            dataGridViewSampleInfo.Rows[index].Cells[1].Value = flowAvg;
            dataGridViewSampleInfo.Rows[index].Cells[2].Value = flowPeek;
            dataGridViewSampleInfo.Rows[index].Cells[3].Value = flowVariance;
            dataGridViewSampleInfo.Rows[index].Cells[4].Value = volume;
            dataGridViewSampleInfo.Rows[index].Cells[5].Value = volumeError;
            dataGridViewSampleInfo.Rows[index].Cells[6].Value = volumeErrorRate;
            dataGridViewSampleInfo.Rows[index].Cells[7].Value = bIsSampleValid;

            /* 统计误差数据 */
            if (bIsSampleValid)
            {
                /* 统计误差数据(并更新状态栏信息显示) */
                StatisticalErrorInfo(volumeErrorRate);
            }

            /* 重置校准器,开始检测下一次校准启动 */
            m_flowValidator.Reset();
        }

        /* 已接收到一个流量采集数据 */
        private void OnFlowRecved(double flow)
        {
            /* 输入到流量验证器 */
            m_flowValidator.Input(flow);

            /* 加入数据到对应的曲线 */
            m_pointsFT.Add(new DataPoint(m_flowValidator.Time, m_flowValidator.Flow));
            if (m_flowValidator.StartSampling)
            {
                m_pointsFV.Add(new DataPoint(m_flowValidator.CurrSampleVolume, m_flowValidator.Flow));
            }
        }

        /* 更新 Flow-Time Plot,并执行自动滚屏 */
        private void UpdateFTPlot(double xBegin)
        {
            /* 在必要时刷新曲线显示并执行自动滚屏 */
            double xEnd = (m_pointsFT.Count > 0) ? m_pointsFT.Last().X : 0;
            double xDelta = xEnd - xBegin;
            if (xDelta > 0)
            {
                /* 刷新曲线显示 */
                plotViewFT.InvalidatePlot(true);
                plotViewFV.InvalidatePlot(true);

                var xAxisFT = m_plotModelFT.Axes[0];
                if (xEnd > xAxisFT.Maximum)
                {
                    /* 自动滚屏 */
                    double panStep = xAxisFT.Transform(-xDelta + xAxisFT.Offset);
                    xAxisFT.Pan(panStep);
                }
            }
        }

        /* 尝试清空数据队列 */
        private bool TryClearDataQueue()
        {
            bool bRet = true;

            /* 尝试清空流量队列 */
            while (m_flowQueue.Count > 0)
            {
                bRet = m_flowQueue.TryDequeue(out _);
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
            this.BeginInvoke(new Action<FormVerification>((obj) => {
                toolStripStatusLabelSampleCount.Text = "0";
                toolStripStatusLabelPassRate.Text = "0.00%";
                toolStripStatusLabelErrRateMaxP.Text = "0.00%";
                toolStripStatusLabelErrRateMaxN.Text = "0.00%";
                toolStripStatusLabelErrRateAbsAvg.Text = "0.00%";
            }), this);
        }

        /* 清空所有图表数据和缓存数据,并刷新显示 */
        private void ClearAll()
        {
            /* 尝试清空数据队列 */
            TryClearDataQueue();

            /* 清空压差数据 */
            lock(m_presureList)
            {
                m_presureList.Clear();
            }

            /* 清除 */
            m_flowValidator.Clear();

            /* 清空样本信息列表 */
            ClearSampleInfoDataGridView();

            /* Clear Flow-Time Plot */
            m_pointsFT.Clear();
            m_plotModelFT.Annotations.Clear();
            m_plotModelFT.ResetAllAxes();

            /* Clear Flow-Volume Plot */
            m_pointsFV.Clear();
            //m_plotModelFV.Annotations.Clear();
            m_plotModelFV.ResetAllAxes();

            /* 刷新曲线显示 */
            plotViewFT.InvalidatePlot(true);
            plotViewFV.InvalidatePlot(true);

            /* 清空结果 */
            m_sampleCount = 0;
            m_passCount = 0;
            m_errorRateMaxP = 0.0;
            m_errorRateMaxN = 0.0;
            m_errorRateAbsSum = 0.0;

            /* 清除状态栏 */
            ClearStatusBar();
        }

        /* 异步加载CSV文件 */
        private async Task LoadCSVFileAsync(string filePath)
        {
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

                /* 停止刷新定时器 */
                m_refreshTimer.Stop();
            }
        }

        /* 显示保存对话框,保存Presure数据为CSV文件 */
        private void ShowSaveCSVDialog()
        {
            /* 弹出文件保存对话框 */
            SaveFileDialog saveCSVDialog = new SaveFileDialog();
            saveCSVDialog.Filter = "CSV File (*.csv;)|*.csv";
            //saveCSVDialog.Multiselect = false;
            saveCSVDialog.FileName = $"verification_{DateTime.Now.ToString("yyyyMMdd_hhmmss")}.csv";

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

                /* 启动任务执行异步保存(防止阻塞UI线程) */
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        /* 在内存中将Presure数据组装称CSV格式字符串 */
                        StringBuilder strData = new StringBuilder();
                        lock(m_presureList)
                        {
                            foreach (var presure in m_presureList)
                            {
                                strData.Append(presure);
                                strData.Append(",");
                            }
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
                        this.BeginInvoke(new Action<FormVerification>((obj) => {
                            toolStripButtonStart.Enabled = toolStripButtonStartEnabled;
                            toolStripButtonSavePresure.Enabled = toolStripButtonSavePresureEnabled;
                            toolStripButtonLoadPresure.Enabled = toolStripButtonLoadPresureEnabled;
                            toolStripButtonClear.Enabled = toolStripButtonClearEnabled;
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
            openCSVDialog.Multiselect = true;

            if (openCSVDialog.ShowDialog() == DialogResult.OK)
            {
                if (openCSVDialog.FileNames.Length <= 0)
                {
                    return;
                }

                /* 加载过程中暂时不允许点击工具按钮 */
                bool toolStripButtonStartEnabled = toolStripButtonStart.Enabled;
                toolStripButtonStart.Enabled = false;

                bool toolStripButtonLoadPresureEnabled = toolStripButtonLoadPresure.Enabled;
                toolStripButtonLoadPresure.Enabled = false;

                bool toolStripButtonClearEnabled = toolStripButtonClear.Enabled;
                toolStripButtonClear.Enabled = false;

                /* 先清空所有图表数据和缓存数据,并刷新显示 */
                ClearAll();

                foreach (var fileName in openCSVDialog.FileNames)
                {
                    /* 加载CSV文件中的数据 */
                    await LoadCSVFileAsync(fileName);
                }

                /* 加载完毕恢复工具按钮使能状态 */
                toolStripButtonStart.Enabled = toolStripButtonStartEnabled;
                toolStripButtonLoadPresure.Enabled = toolStripButtonLoadPresureEnabled;
                toolStripButtonClear.Enabled = toolStripButtonClearEnabled;
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
                        toolStripButtonLoadPresure.Enabled = false;
                        toolStripButtonClear.Enabled = false;
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
                    toolStripButtonLoadPresure.Enabled = true;
                    toolStripButtonClear.Enabled = true;
                    /* 取消监听流量传感器数据收取事件 */
                    m_flowSensor.PresureRecved -= OnPresureRecved;
                    /* 停止刷新定时器 */
                    m_refreshTimer.Stop();
                    /* 尝试清空数据队列 */
                    TryClearDataQueue();
                }
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

        private void FormVerification_FormClosing(object sender, FormClosingEventArgs e)
        {
            /* 停止传感器 */
            m_flowSensor.Stop();

            /* 取消监听流量传感器数据收取事件 */
            m_flowSensor.PresureRecved -= OnPresureRecved;
        }
    }
}
