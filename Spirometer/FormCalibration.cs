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
using System.IO;
using System.Threading;

namespace Spirometer
{
    public partial class FormCalibration : Form
    {
        private ConcurrentQueue<double> m_dataQueue = new ConcurrentQueue<double>(); // 数据队列
        private FlowSensor m_flowSensor; // 流量传感器
        private FlowCalibrator m_flowCalibrator; // 流量校准器
        private PlotModel m_plotModelPS; // 压差(Presure)-和值(Sum)图Model
        private PlotModel m_plotModelAVGPK; // 平均压差(Presure)-K值图Model
        private PlotModel m_plotModelPT; // 压差(Presure)-时间(Time)图Model

        private System.Windows.Forms.Timer m_refreshTimer = new System.Windows.Forms.Timer(); // 波形刷新定时器
        private readonly int m_fps = 24; // 帧率

        private List<DataPoint> m_pointsPS; // 压差(Presure)-和值(Sum)数据
        private List<DataPoint> m_pointsAVGPK; // 平均压差(Presure)-K值数据
        private List<DataPoint> m_pointsPT; // 压差(Presure)-时间(Time)数据

        private TaskCompletionSource<bool> m_dataPlotTaskComp; // 用于监控数据输出到Plot数据完成事件

        private List<double> m_calParamSectionKeyList = new List<double>();
        private List<double> m_calParamValList = new List<double>();

        public FormCalibration(FlowSensor flowSensor, double calVolume = 1.0)
        {
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

            /* 平均压差(Presure)-K值图 */
            m_plotModelAVGPK = new PlotModel()
            {
                Title = "平均压差(Presure)-K值",
                LegendTitle = "图例",
                LegendOrientation = LegendOrientation.Horizontal,
                LegendPlacement = LegendPlacement.Inside,
                LegendPosition = LegendPosition.TopRight,
                LegendBackground = OxyColors.Beige,
                LegendBorder = OxyColors.Blue,
                IsLegendVisible = false // 隐藏图例
            };

            //X轴,AVG Presure
            var xAxisAVGPK = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Bottom,
                //Minimum = -55 * m_flowCalibrator.CalVolume,
                //Maximum = 55 * m_flowCalibrator.CalVolume,
                Title = "AVG Presure(inH2O)"
            };
            m_plotModelAVGPK.Axes.Add(xAxisAVGPK);

            //Y轴,K值
            var yAxisAVGPK = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Left,
                Title = "系数"
            };
            m_plotModelAVGPK.Axes.Add(yAxisAVGPK);

            // 数据
            var seriesAVGPK = new LineSeries()
            {
                Color = OxyColors.DimGray,
                StrokeThickness = 1,
                //MarkerSize = 1,
                //MarkerStroke = OxyColors.DarkBlue,
                //MarkerType = MarkerType.Circle,
                Title = "Data"
            };
            m_plotModelAVGPK.Series.Add(seriesAVGPK);

            // 设置View对应的Model
            plotViewAVGPK.Model = m_plotModelAVGPK;
            // 保存数据点列表引用
            m_pointsAVGPK = seriesAVGPK.Points;

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

        /* 清空结果列表 */
        private void ClearResultDataGridView()
        {
            dataGridViewSampleInfo.Rows.Clear();
        }

        /* 设置校准参数到列表显示 */
        private void SetCaliParamToDataGridView(List<double> calParamSectionKeyList, List<double> calParamValList)
        {
            dataGridViewResult.Rows.Clear();
            for (int i = 0; i < calParamSectionKeyList.Count; ++i)
            {
                var presure = calParamSectionKeyList[i];
                var param = calParamValList[i];
                int index = dataGridViewResult.Rows.Add();
                dataGridViewResult.Rows[index].Cells[0].Value = presure;
                dataGridViewResult.Rows[index].Cells[1].Value = param;
            }
        }

        /* 尝试计算校准参数并更新显示 */
        private bool TryCalcAndUpdateCaliParam()
        {
            /* 取得挑选的样本列表 */
            List<uint> sampleList = new List<uint>();
            for (int i = 0; i < dataGridViewSampleInfo.Rows.Count; ++i)
            {
                bool bApply = (bool)dataGridViewSampleInfo.Rows[i].Cells[6].Value;
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
            bool bRet = m_flowCalibrator.CalcCalibrationParams(m_calParamSectionKeyList, m_calParamValList, sampleList);
            if (bRet)
            {
                /* 设置校准参数到列表 */
                SetCaliParamToDataGridView(m_calParamSectionKeyList, m_calParamValList);

                /* 显示分段标记 */
                m_plotModelAVGPK.Annotations.Clear();
                m_plotModelPS.Annotations.Clear();
                foreach (var presure in m_calParamSectionKeyList)
                {
                    var annotation = new LineAnnotation()
                    {
                        Color = OxyColors.Red,
                        X = presure,
                        LineStyle = LineStyle.Dash,
                        Type = LineAnnotationType.Vertical,
                        Text = $"{presure}"
                    };
                    m_plotModelAVGPK.Annotations.Add(annotation);
                    annotation = new LineAnnotation()
                    {
                        Color = OxyColors.Red,
                        Y = presure,
                        LineStyle = LineStyle.Dash,
                        Type = LineAnnotationType.Horizontal,
                        Text = $"{presure}"
                    };
                    m_plotModelPS.Annotations.Add(annotation);
                }
                m_plotModelAVGPK.InvalidatePlot(false);
                m_plotModelPS.InvalidatePlot(false);

                /* 更新状态栏(参数个数) */
                toolStripStatusLabelParamCount.Text = m_calParamSectionKeyList.Count.ToString();
            }

            return bRet;
        }

        /* 添加数据点到平均压差(Presure)-K值图并刷新显示 */
        private void AddPointToPlotAVGPK(double avgPresure, double k)
        {
            /* 插入对应位置，使列表保持按avgPresure递增顺序 */
            int i = 0;
            for (; i < m_pointsAVGPK.Count; ++i)
            {
                if (avgPresure <= m_pointsAVGPK[i].X)
                {
                    break;
                }
            }

            if (i < m_pointsAVGPK.Count)
            {
                m_pointsAVGPK.Insert(i, new DataPoint(avgPresure, k));
            }
            else
            {
                m_pointsAVGPK.Add(new DataPoint(avgPresure, k));
            }

            /* 刷新显示 */
            m_plotModelAVGPK.InvalidatePlot(true);
        }

        /* 采样已停止 */
        private void OnSampleStoped(uint sampleIndex, FlowCalibrator.RespireDirection direction)
        {
            double presureAvg = m_flowCalibrator.SamplePresureAvg(sampleIndex);
            double presureFlowScale = m_flowCalibrator.SamplePresureAvgToFlowScale(sampleIndex);
            double presureSum = m_flowCalibrator.SamplePresureSum(sampleIndex);
            double peekPresure = (direction == FlowCalibrator.RespireDirection.Inspiration) ? m_flowCalibrator.SampleMaxPresure(sampleIndex) : m_flowCalibrator.SampleMinPresure(sampleIndex);
            double presureVariance = m_flowCalibrator.SamplePresureVariance(sampleIndex);
            bool bIsSampleValid = m_flowCalibrator.SampleIsValid(sampleIndex);

            /* 添加样本信息到列表 */
            int index = dataGridViewSampleInfo.Rows.Add();
            dataGridViewSampleInfo.Rows[index].Cells[0].Value = (presureAvg > 0) ? "吸气" : "呼气";
            dataGridViewSampleInfo.Rows[index].Cells[1].Value = presureFlowScale;
            dataGridViewSampleInfo.Rows[index].Cells[2].Value = presureAvg;
            dataGridViewSampleInfo.Rows[index].Cells[3].Value = presureSum;
            dataGridViewSampleInfo.Rows[index].Cells[4].Value = peekPresure;
            dataGridViewSampleInfo.Rows[index].Cells[5].Value = presureVariance;
            dataGridViewSampleInfo.Rows[index].Cells[6].Value = bIsSampleValid;

            if (bIsSampleValid)
            {
                /* 添加数据点到平均压差(Presure)-K值图并刷新显示 */
                AddPointToPlotAVGPK(peekPresure, presureFlowScale);
            }

            /* 尝试计算校准参数并更新显示 */
            TryCalcAndUpdateCaliParam();

            /* 重置校准器,开始检测下一次校准启动 */
            m_flowCalibrator.Reset();
        }

        /* 已接收到一个压差采集数据 */
        private void OnPresureRecved(double presure)
        {
            /* 执行肺功能参数计算 */
            m_flowCalibrator.Input(presure);

            /* 加入数据到对应的曲线 */
            m_pointsPT.Add(new DataPoint(m_flowCalibrator.Time, m_flowCalibrator.Presure));
            m_pointsPS.Add(new DataPoint(m_flowCalibrator.CurrSamplePresureSum, m_flowCalibrator.Presure));
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

            /* 清空结果列表 */
            ClearResultDataGridView();

            /* Clear Presure-Time Plot */
            m_pointsPT.Clear();
            m_plotModelPT.Annotations.Clear();
            var xAxisFV = m_plotModelPT.Axes[0];
            xAxisFV.Reset();

            /* Clear Presure-Sum Plot */
            m_pointsPS.Clear();
            //m_plotModelPS.Annotations.Clear();
            var xAxisVT = m_plotModelPS.Axes[0];
            xAxisVT.Reset();

            /* 刷新曲线显示 */
            plotViewPT.InvalidatePlot(true);
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

                    /* 压差数据存入队列,将在刷新定时器中读取 */
                    m_dataQueue.Enqueue(presure);

                    /* 模拟采样率 */
                    //Thread.Sleep((int)m_flowSensor.SAMPLE_TIME);
                }

                /* 数据已加载完毕 */

                /* 建Task完成事件对象(用于监测数据是否已全部输出到Plot) */
                m_dataPlotTaskComp = new TaskCompletionSource<bool>();
            }));

            /* 异步等待完成对象被触发(数据加载完毕并且已全部输出到Plot) */
            await loadTask; // 先确保数据已加载完毕(只有此时m_dataPlotTaskComp才非空)
            if (null != m_dataPlotTaskComp)
            {
                await m_dataPlotTaskComp.Task; // 然后确保数据已全部输出到Plot
            }

            /* 清除完成对象 */
            m_dataPlotTaskComp = null;
        }

        /* 显示保存对话框,保存Flow数据为CSV文件 */
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
                toolStripButtonStart.Enabled = false;
                toolStripButtonSavePresure.Enabled = false;
                toolStripButtonLoadPresure.Enabled = false;
                toolStripButtonClear.Enabled = false;
                toolStripButtonCalcCaliParam.Enabled = false;
                toolStripButtonApply.Enabled = false;

                /* 启动任务执行异步保存(防止阻塞UI线程) */
                Task.Factory.StartNew(() =>
                {
                    /* 在内存中将Flow数据组装称CSV格式字符串 */
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

                    /* 保存完毕恢复工具按钮使能状态(确保在UI线程执行) */
                    this.BeginInvoke(new Action<FormCalibration>((obj) => {
                        toolStripButtonStart.Enabled = true;
                        toolStripButtonSavePresure.Enabled = true;
                        toolStripButtonLoadPresure.Enabled = true;
                        toolStripButtonClear.Enabled = true;
                        toolStripButtonCalcCaliParam.Enabled = true;
                        toolStripButtonApply.Enabled = true;
                    }), this);
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
                toolStripButtonStart.Enabled = false;
                toolStripButtonSavePresure.Enabled = false;
                toolStripButtonLoadPresure.Enabled = false;
                toolStripButtonClear.Enabled = false;
                toolStripButtonCalcCaliParam.Enabled = false;
                toolStripButtonApply.Enabled = false;

                await LoadCSVFileAsync(openCSVDialog.FileName);

                /* 加载完毕恢复工具按钮使能状态 */
                toolStripButtonStart.Enabled = true;
                toolStripButtonSavePresure.Enabled = true;
                toolStripButtonLoadPresure.Enabled = true;
                toolStripButtonClear.Enabled = true;
                toolStripButtonCalcCaliParam.Enabled = true;
                toolStripButtonApply.Enabled = true;
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
                    /* 取消监听流量传感器数据收取事件 */
                    m_flowSensor.PresureRecved -= OnPresureRecved;
                    /* 停止刷新定时器 */
                    m_refreshTimer.Stop();
                    /* 尝试清空数据队列 */
                    TryClearDataQueue();
                }
            }
        }

        private void FormCalibration_FormClosed(object sender, FormClosedEventArgs e)
        {
            /* 取消监听流量传感器数据收取事件 */
            m_flowSensor.PresureRecved -= OnPresureRecved;
        }

        private void toolStripButtonApply_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("将替换现有校准参数,是否继续？", "选择", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            if (dialogResult != DialogResult.Yes)
            {
                return;
            }

            bool bRet = TryCalcAndUpdateCaliParam();
            if (bRet)
            {
                if (m_calParamSectionKeyList.Count > 0)
                {
                    m_flowSensor.SetCalibrationParamList(m_calParamSectionKeyList, m_calParamValList);
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

        private void toolStripButtonCalcCaliParam_Click(object sender, EventArgs e)
        {
            /* 尝试计算校准参数并更新显示 */
            TryCalcAndUpdateCaliParam();
        }
    }
}
