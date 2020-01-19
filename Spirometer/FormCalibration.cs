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
        private PlotModel m_plotModelPT; // 压差(Presure)-时间(Time)图Model

        private System.Windows.Forms.Timer m_refreshTimer = new System.Windows.Forms.Timer(); // 波形刷新定时器
        private readonly int m_fps = 24; // 帧率

        private List<DataPoint> m_pointsPS; // 压差(Presure)-和值(Sum)数据
        private List<DataPoint> m_pointsPT; // 压差(Presure)-时间(Time)数据

        private TaskCompletionSource<bool> m_dataPlotTaskComp; // 用于监控数据输出到Plot数据完成事件

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
                Minimum = -55,
                Maximum = 55,
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
                X = -50,
                LineStyle = LineStyle.Dash,
                Type = LineAnnotationType.Vertical,
                Text = "+10%"
            };
            m_plotModelPS.Annotations.Add(annotationPS1);

            //标记线-10%
            var annotationPS2 = new LineAnnotation()
            {
                Color = OxyColors.Red,
                X = -40,
                LineStyle = LineStyle.Dash,
                Type = LineAnnotationType.Vertical,
                Text = "-10%"
            };
            m_plotModelPS.Annotations.Add(annotationPS2);

            //标记线-10%
            var annotationPS3 = new LineAnnotation()
            {
                Color = OxyColors.Red,
                X = 40,
                LineStyle = LineStyle.Dash,
                Type = LineAnnotationType.Vertical,
                Text = "-10%"
            };
            m_plotModelPS.Annotations.Add(annotationPS3);

            //标记线+10%
            var annotationPS4 = new LineAnnotation()
            {
                Color = OxyColors.Red,
                X = 50,
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

            /* 加载现有的吸气校准参数到结果列表 */
            foreach (var p in m_flowSensor.InCalibrationParams())
            {
                AddParamToResultDataGridView(p, true);
            }
            /* 加载现有的呼气校准参数到结果列表 */
            foreach (var p in m_flowSensor.EnCalibrationParams())
            {
                AddParamToResultDataGridView(p, true);
            }

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
            dataGridViewResult.Rows.Clear();
        }

        /* 添加校准参数到结果列表 */
        private void AddParamToResultDataGridView(FlowSensor.CalibrationParam p, bool bApply)
        {
            int index = dataGridViewResult.Rows.Add();
            dataGridViewResult.Rows[index].Cells[0].Value = (p.presureAvg > 0) ? "吸气" : "呼气";
            dataGridViewResult.Rows[index].Cells[1].Value = p.presureFlowScale;
            dataGridViewResult.Rows[index].Cells[2].Value = p.presureAvg;
            dataGridViewResult.Rows[index].Cells[3].Value = p.presureSum;
            dataGridViewResult.Rows[index].Cells[4].Value = p.peekPresure;
            dataGridViewResult.Rows[index].Cells[5].Value = p.presureVariance;
            dataGridViewResult.Rows[index].Cells[6].Value = bApply;
        }

        /* 测量已停止 */
        private void OnMeasureStoped()
        {
            Console.WriteLine($"PresureSum: {m_flowCalibrator.PresureSum} \t PeekPresure: {m_flowCalibrator.PeekPresure} \t PresureAvg: {m_flowCalibrator.PresureAvg} \t K: {m_flowSensor.SAMPLE_RATE / m_flowCalibrator.PresureSum} \t PresureVariance: {m_flowCalibrator.PresureVariance}");

            /* 添加校准参数到结果列表 */
            FlowSensor.CalibrationParam p = new FlowSensor.CalibrationParam()
            {
                presureAvg = m_flowCalibrator.PresureAvg,
                presureFlowScale = m_flowCalibrator.PresureFlowScale,
                presureSum = m_flowCalibrator.PresureSum,
                peekPresure = m_flowCalibrator.PeekPresure,
                presureVariance = m_flowCalibrator.PresureVariance
            };
            AddParamToResultDataGridView(p, false);

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

        /* 清空所有图表数据和缓存数据,并刷新显示 */
        private void ClearAll()
        {
            /* 尝试清空数据队列 */
            TryClearDataQueue();

            /* 重置状态 */
            m_flowCalibrator.Reset();

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

                /* 加载过程中暂时不允许再次点击 */
                toolStripButtonLoadPresure.Enabled = false;
                toolStripButtonStart.Enabled = false;
                toolStripButtonApply.Enabled = false;

                await LoadCSVFileAsync(openCSVDialog.FileName);

                /* 加载完毕,执行UI相关操作(确保在UI线程执行) */
                this.BeginInvoke(new Action<FormCalibration>((obj) => {
                    toolStripButtonLoadPresure.Enabled = true;
                    toolStripButtonStart.Enabled = true;
                    toolStripButtonApply.Enabled = true;
                }), this);
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
                        /* 清除旧的校准数据 */
                        m_flowSensor.ClearCalibrationParams();
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

            uint inCalCount = 0; // 吸气校准项目个数
            uint exCalCount = 0; // 呼气校准项目个数

            /* 取得挑选的校准结果列表 */
            List<FlowSensor.CalibrationParam> calParamsList = new List<FlowSensor.CalibrationParam>();
            for (int i = 0; i < dataGridViewResult.Rows.Count; ++i)
            {
                bool bApply = (bool)dataGridViewResult.Rows[i].Cells[6].Value;
                if (bApply)
                {
                    double presureFlowScale = (double)dataGridViewResult.Rows[i].Cells[1].Value;
                    double presureAvg = (double)dataGridViewResult.Rows[i].Cells[2].Value;
                    double presureSum = (double)dataGridViewResult.Rows[i].Cells[3].Value;
                    double peekPresure = (double)dataGridViewResult.Rows[i].Cells[4].Value;
                    double presureVariance = (double)dataGridViewResult.Rows[i].Cells[5].Value;

                    FlowSensor.CalibrationParam p = new FlowSensor.CalibrationParam() { 
                        presureAvg = presureAvg, 
                        presureFlowScale = presureFlowScale,
                        presureSum = presureSum,
                        peekPresure = peekPresure,
                        presureVariance = presureVariance
                    };
                    calParamsList.Add(p);

                    if (presureAvg > 0)
                    {
                        ++inCalCount;
                    }
                    else if (presureAvg < 0)
                    {
                        ++exCalCount;
                    }
                }
            }

            /* 挑选的校准结果列表中必须要同时包含吸气和呼气的校准条目 */
            if ((inCalCount > 0) 
                && (exCalCount > 0))
            {
                /* 将挑选的校准结果设置到流量传感器 */
                m_flowSensor.ClearCalibrationParams();
                m_flowSensor.AddCalibrationParams(calParamsList.ToArray());

                MessageBox.Show("应用校准参数成功");
            }
            else
            {
                MessageBox.Show($"应用校准参数失败：请确保至少包含一项吸气校准和一项呼气校准。吸气参数{inCalCount}项、呼气参数{exCalCount}项！");
            }
        }

        private void toolStripButtonLoadPresure_Click(object sender, EventArgs e)
        {
            ShowLoadCSVDialog();
        }
    }
}
