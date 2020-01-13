using OxyPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spirometer
{
    /* 肺功能参数计算 */
    class PulmonaryFunction
    {
        public double Time { get; private set; } = 0.0; // 时间(ms)
        public double InFlow { get; private set; } = 0.0; // 吸气流量(L/S)
        public double InVolume { get; private set; } = 0.0; // 吸气容积(L)
        public double ExFlow { get { return -InFlow; } } // 呼气流量(L/S)
        public double ExVolume { get { return -InVolume; } } // 呼气容积(L)
        public double RespiratoryRate { get; private set; } = 0.0; // 呼吸频率(次/min)
        public uint RespiratoryCycleCount { get; private set; } = 0U; // 呼吸周期数(次)
        public uint ForceExpirationStartIndex { get; private set; } = 0U; // 用力呼气起点Index
        public uint ForceExpirationEndIndex { get; private set; } = 0U; // 用力呼气终点Index
        public uint SampleCount { get { return (uint)m_listFV.Count; } } // 已采集的样本数
        public double TLC // 肺总量(L)
        {
            get
            {
                if (m_maxVolumeIndex < m_listFV.Count)
                {
                    double minVolume = 0.0;
                    if (m_minVolumeIndex < m_listFV.Count)
                    {
                        minVolume = m_listFV[(int)m_minVolumeIndex].inVolume;
                    }

                    return m_listFV[(int)m_maxVolumeIndex].inVolume - minVolume;
                }
                return 0.0;
            }
        }
        public double VC  // 肺活量(L)
        {
            get
            {
                if ((m_maxVolumeIndex < m_listFV.Count)
                    && (m_minVolumeIndex < m_listFV.Count))
                {
                    return m_listFV[(int)m_maxVolumeIndex].inVolume - m_listFV[(int)m_minVolumeIndex].inVolume;
                }
                return 0.0;
            }
        }
        public double FRC // 功能残气量(L)
        {
            get
            {
                return m_tvLowerAvg;
            }
        }
        public double TV // 潮气量(L)
        {
            get
            {
                return (m_tvUpperAvg - m_tvLowerAvg);
            }
        }

        public delegate void ZeroingCompleteHandler(uint sampleIndex, double flowZeroOffset); // 归零完成事件代理
        public event ZeroingCompleteHandler ZeroingCompleted; // 归零完成事件
        public delegate void MeasureStartHandler(uint sampleIndex, bool inspiration); // 测试启动事件代理
        public event MeasureStartHandler MeasureStarted; // 测试启动事件
        public delegate void InspirationStartHandler(uint sampleIndex, uint peekFlowIndex); // 吸气开始事件代理
        public event InspirationStartHandler InspirationStarted; // 吸气开始事件
        public delegate void ExpirationStartHandler(uint sampleIndex, uint peekFlowIndex); // 呼气开始事件代理
        public event ExpirationStartHandler ExpirationStarted; // 呼气开始事件
        public delegate void ForceExpirationStartHandler(uint sampleIndex, uint peekFlowIndex); // 用力呼气开始事件代理
        public event ForceExpirationStartHandler ForceExpirationStarted; // 用力呼气开始事件
        public delegate void MeasureStopHandler(uint sampleIndex, bool inspiration); // 测试停止事件代理
        public event MeasureStopHandler MeasureStoped; // 测试停止事件

        public struct FlowVolume
        {
            public double inFlow; // 吸气流量(L/S)
            public double inVolume; // 吸气容积(L)
        }
        private List<FlowVolume> m_listFV = new List<FlowVolume>(); // Flow-Volume数据列表

        private enum State
        {
            Reset, // 复位状态
            Zeroing, // 正在归零状态
            WaitStart, // 等待启动测试状态
            Inspiration, // 正在吸气状态
            Expiration, // 正在呼气状态
            Stop, // 测试已停止状态
        }

        private State m_state = State.Reset; // 工作状态
        private double m_flowZeroOffset = 0.0; // 流量零点偏移值
        private readonly double SAMPLE_TIME = 0.0; // 采样时间(ms)
        private WaveStatistician m_waveStatistician = new WaveStatistician(); // 用于统计波动数据
        private readonly int ZEROING_SAMPLE_COUNT = 100; // 归零过程采样次数
        private readonly double ZEROING_ALLOW_RANGE = 0.04; // 归零过程允许的波动范围
        private readonly double INSPIRATION_START_FLOW = 0.01; // 吸气启动流速阈值(L/S),连续N次采样流速大于该值则判断为开始吸气
        private readonly double INSPIRATION_START_SAMPLE_COUNT = 10; // 吸气启动检测采样次数
        private uint m_inspirationSampleCnt = 0U; // 统计持续吸气样本数,用于检测吸气启动条件
        private double m_inspirationVolume = 0.0; // 用于检测吸气启动条件的样本吸气容积
        private readonly double EXPIRATION_START_FLOW = -0.01; // 呼气启动流速阈值(L/S),连续N次采样流速小于该值则判断为开始呼气
        private readonly double EXPIRATION_START_SAMPLE_COUNT = 10; // 呼气启动检测采样次数
        private uint m_expirationSampleCnt = 0U; // 统计持续呼气样本数,用于检测呼气启动条件
        private double m_expirationVolume = 0.0; // 用于检测呼气启动条件的样本呼气容积
        private readonly double DEFAULT_FRC = 2.55; // 默认功能残气量(FRC),单位: L

        /* 流量(Flow)-时间(Time)曲线极值点 */
        private uint m_peekFlowIndex = 0U; // 跟踪流量曲线当前极值点Index

        /* 容积(Volume)-时间(Time)曲线极值点 */
        private uint m_peekVolumeIndex = 0U; // 跟踪容积曲线当前极值点Index
        private uint m_peekVolumeKeepCount = 0U; // 跟踪当前极值点不变的次数
        private readonly uint PEEK_VOLUME_KEEP_COUNT = 100; // 如果极值点保持指定的采样次数不变化则认为是稳定的
        private List<uint> m_peekMaxVolumeIndexList = new List<uint>(); // Volume极大值点列表
        private List<uint> m_peekMinVolumeIndexList = new List<uint>(); // Volume极小值点列表

        /* 阈值 */
        private readonly double FLOW_DELTA_THRESHOLD = 0.15; // Flow变化量阈值,在指定采样次数后如果Flow变化量超过阈值则确认开始吸气/吹气
        private readonly double VOLUME_DELTA_THRESHOLD = 0.05; // Volume变化量阈值,在过极值点指定采样次数后如果Volume变化量超过阈值则确认极值点有效
        private readonly double FORCE_EXPIRATION_FLOW = 2.0; // 用力呼气Flow阈值,达到该Flow值判断为用力呼气

        /* 测试启动点 */
        private uint m_measureStartIndex = 0U; // 测试启动点Index
        private bool m_measureStartInspiration = true; // 是否启动测试时为吸气状态

        /* 容积参数 */
        private uint m_maxVolumeIndex = 0U; // 容积最大值点Index
        private uint m_minVolumeIndex = 0U; // 容积最小值点Index

        /* 潮气量上下界信息(用于求TV和FRC) */
        private double m_tvUpperSum = 0.0; // 潮气量上界Volume求和值
        private double m_tvUpperAvg = 0.0; // 潮气量上界Volume平均值
        private double m_tvLowerSum = 0.0; // 潮气量下界Volume求和值
        private double m_tvLowerAvg = 0.0; // 潮气量下界Volume平均值

        public PulmonaryFunction(double sampleTime)
        {
            SAMPLE_TIME = sampleTime;
            InVolume = 0.0;
        }

        /* 状态重置 */
        public void Reset()
        {
            Time = 0.0;
            InFlow = 0.0;
            InVolume = 0.0;
            RespiratoryRate = 0.0;
            m_state = State.Reset;
            m_flowZeroOffset = 0.0;
            m_waveStatistician.Reset();
            m_inspirationSampleCnt = 0U;
            m_inspirationVolume = 0.0;
            m_expirationSampleCnt = 0U;
            m_expirationVolume = 0.0;
            m_peekFlowIndex = 0U;
            m_peekVolumeIndex = 0U;
            m_peekVolumeKeepCount = 0U;
            m_peekMaxVolumeIndexList.Clear();
            m_peekMinVolumeIndexList.Clear();
            m_measureStartIndex = 0U;
            m_measureStartInspiration = true;
            RespiratoryCycleCount = 0U;
            ForceExpirationStartIndex = 0U;
            ForceExpirationEndIndex = 0U;
            m_maxVolumeIndex = 0U;
            m_minVolumeIndex = 0U;
            m_tvUpperSum = 0.0;
            m_tvUpperAvg = 0.0;
            m_tvLowerSum = 0.0;
            m_tvLowerAvg = 0.0;
            m_listFV.Clear();
        }

        /* 切换状态 */
        private void SetState(State state)
        {
            /* 检查是否发生状态切换 */
            if (state == m_state)
            {
                return;
            }

            /* 切换状态 */
            m_state = state;
        }

        /* 输入流量数据 */
        public void Input(double flow)
        {
            /* Flow样本索引值 */
            uint sampleIndex = (uint)m_listFV.Count;
            
            /* 统计样本时间戳 */
            Time = sampleIndex * SAMPLE_TIME;

            /* 当前流量 */
            double flowZeroCorrection = flow - m_flowZeroOffset; // 执行零点校正

            if ((m_state > State.WaitStart)
                && (m_state < State.Stop))
            {
                /* 当前吸气流量 */
                InFlow = flowZeroCorrection;

                /* 更新当前吸气容积 */
                InVolume += InFlow * SAMPLE_TIME / 1000;
            }
            else
            {
                /* 当前吸气流量 */
                InFlow = 0.0;
            }

            /* 记录Flow-Volume数据到队列 */
            m_listFV.Add(new FlowVolume { inFlow = InFlow, inVolume = InVolume});

            switch (m_state)
            {
                case State.Reset: // 复位状态
                    {
                        /* 进入正在归零状态 */
                        SetState(State.Zeroing);

                        /* 开始统计波动范围 */
                        m_waveStatistician.Input(flow);
                        break;
                    }
                case State.Zeroing: // 正在归零状态
                    {
                        /* 继续统计波动范围 */
                        m_waveStatistician.Input(flow);

                        /* 是否已达到所需采样次数 */
                        if (m_waveStatistician.SampleCount < ZEROING_SAMPLE_COUNT)
                        {
                            /* 保持正在归零状态 */
                            break;
                        }

                        /* 波动范围是否在允许范围内 */
                        if (m_waveStatistician.Range < ZEROING_ALLOW_RANGE)
                        {
                            /* 数据稳定度达标,将统计平均值作为零点校准值 */
                            m_flowZeroOffset = m_waveStatistician.AvgVal;

                            /* 进入等待启动测试状态 */
                            SetState(State.WaitStart);

                            /* 触发归零完成事件 */
                            ZeroingCompleted?.Invoke(sampleIndex, m_flowZeroOffset);
                        }
                        else
                        {
                            /* 重置波动统计器,重新开始评估输入数据 */
                            m_waveStatistician.Reset();
                            /* 保持正在归零状态 */

                            /* 重新开始统计波动范围 */
                            m_waveStatistician.Input(flow);
                        }
                        break;
                    }
                case State.WaitStart: // 等待启动测试状态
                    {
                        /* 流量是否达到吸气阈值 */
                        if (flowZeroCorrection > INSPIRATION_START_FLOW)
                        {
                            /* 重置呼气样本数 */
                            m_expirationSampleCnt = 0U;
                            /* 重置呼气样本容积 */
                            m_expirationVolume = 0.0;

                            /* 统计持续吸气样本数 */
                            ++m_inspirationSampleCnt;
                            /* 统计吸气样本容积 */
                            m_inspirationVolume += flowZeroCorrection * SAMPLE_TIME / 1000;

                            /* 是否已监测到吸气启动条件 */
                            if (m_inspirationSampleCnt >= INSPIRATION_START_SAMPLE_COUNT) 
                            {
                                /* 是否有明显的加快趋势 */
                                if ((flowZeroCorrection - INSPIRATION_START_FLOW) > FLOW_DELTA_THRESHOLD)
                                {
                                    /* 更新当前容积 */
                                    InVolume += m_inspirationVolume;

                                    /* 进入正在吸气状态 */
                                    SetState(State.Inspiration);

                                    /* 触发测试启动事件(吸气启动) */
                                    RespiratoryCycleCount = 0U;
                                    m_measureStartInspiration = true;
                                    m_measureStartIndex = SampleCount - m_inspirationSampleCnt;
                                    MeasureStarted?.Invoke(m_measureStartIndex, m_measureStartInspiration);

                                    /* 重置吸气样本数 */
                                    m_inspirationSampleCnt = 0;
                                    /* 重置吸气样本容积 */
                                    m_inspirationVolume = 0.0;

                                    /* 初始化流量极值点 */
                                    m_peekFlowIndex = sampleIndex;

                                    /* 初始化容积极值点 */
                                    m_peekVolumeIndex = sampleIndex;
                                    m_peekVolumeKeepCount = 1;
                                }
                            }
                        }
                        /* 流量是否达到呼气阈值 */
                        else if (flowZeroCorrection < EXPIRATION_START_FLOW)
                        {
                            /* 重置吸气样本数 */
                            m_inspirationSampleCnt = 0U;
                            /* 重置吸气样本容积 */
                            m_inspirationVolume = 0.0;

                            /* 统计持续呼气样本数 */
                            ++m_expirationSampleCnt;
                            /* 统计呼气样本容积 */
                            m_expirationVolume += flowZeroCorrection * SAMPLE_TIME / 1000;

                            /* 是否已监测到呼气启动条件 */
                            if (m_expirationSampleCnt >= EXPIRATION_START_SAMPLE_COUNT)
                            {
                                /* 是否有明显的加快趋势 */
                                if ((EXPIRATION_START_FLOW - flowZeroCorrection) > FLOW_DELTA_THRESHOLD)
                                {
                                    /* 更新当前容积 */
                                    InVolume += m_expirationVolume;

                                    /* 进入正在呼气状态 */
                                    SetState(State.Expiration);

                                    /* 触发测试启动事件(呼气启动) */
                                    RespiratoryCycleCount = 0U;
                                    m_measureStartInspiration = false;
                                    m_measureStartIndex = SampleCount - m_expirationSampleCnt;
                                    MeasureStarted?.Invoke(m_measureStartIndex, m_measureStartInspiration);

                                    /* 重置呼气样本数 */
                                    m_expirationSampleCnt = 0;
                                    /* 重置呼气样本容积 */
                                    m_expirationVolume = 0.0;

                                    /* 初始化流量极值点 */
                                    m_peekFlowIndex = sampleIndex;

                                    /* 初始化容积极值点 */
                                    m_peekVolumeIndex = sampleIndex;
                                    m_peekVolumeKeepCount = 1;
                                }
                            }
                        }
                        break;
                    }
                case State.Inspiration: // 正在吸气状态
                    {
                        /* 统计流量极大值 */
                        if (flowZeroCorrection > m_listFV[(int)m_peekFlowIndex].inFlow)
                        {
                            m_peekFlowIndex = sampleIndex;
                        }

                        /* 统计容积极大值 */
                        if (InVolume > m_listFV[(int)m_peekVolumeIndex].inVolume)
                        {
                            m_peekVolumeIndex = sampleIndex;
                            m_peekVolumeKeepCount = 1;
                        }
                        else
                        {
                            ++m_peekVolumeKeepCount;
                            /* 极值点是否保持稳定 */
                            if (m_peekVolumeKeepCount >= PEEK_VOLUME_KEEP_COUNT)
                            {
                                /* 过极值点后是否变化明显 */
                                if ((m_listFV[(int)m_peekVolumeIndex].inVolume - InVolume) > VOLUME_DELTA_THRESHOLD)
                                {
                                    /* 流量极大值点确认 */
                                    //Console.WriteLine($"流量极大值点:{m_peekFlowIndex}, {m_listFV[(int)m_peekFlowIndex].flow}");

                                    /* 进入正在呼气状态 */
                                    SetState(State.Expiration);

                                    /* Volume极大值点存入列表 */
                                    m_peekMaxVolumeIndexList.Add(m_peekVolumeIndex);

                                    /* 更新潮气量上界统计信息(只统计6次以内的极大值) */
                                    if (m_peekMaxVolumeIndexList.Count <= 6)
                                    {
                                        m_tvUpperSum += m_listFV[(int)m_peekVolumeIndex].inVolume;
                                        m_tvUpperAvg = m_tvUpperSum / m_peekMaxVolumeIndexList.Count;
                                    }

                                    /* 统计容积最大值点 */
                                    if (m_listFV[(int)m_peekVolumeIndex].inVolume > m_listFV[(int)m_maxVolumeIndex].inVolume)
                                    {
                                        m_maxVolumeIndex = m_peekVolumeIndex;
                                    }

                                    /* 是否启动测试时为呼气状态 */
                                    if (!m_measureStartInspiration)
                                    {
                                        /* 完成一个呼吸周期,更新呼吸周期数 */
                                        ++RespiratoryCycleCount;

                                        /* 统计6次以内的呼吸频率 */
                                        if (RespiratoryCycleCount <= 6)
                                        {
                                            /* 更新统计呼吸频率 */
                                            double respiratoryTime = (sampleIndex - m_measureStartIndex) * SAMPLE_TIME; // 总时间(ms)
                                            double respiratoryCycleTime = respiratoryTime / RespiratoryCycleCount; // 呼吸周期(ms)
                                            RespiratoryRate = (1000 * 60) / respiratoryCycleTime; // 呼吸频率(次/min)
                                        }
                                    }

                                    /* 呼气流量是否达到用力呼气阈值 */
                                    double exFlow = -flowZeroCorrection;
                                    if (exFlow >= FORCE_EXPIRATION_FLOW)
                                    {
                                        ForceExpirationStartIndex = m_peekVolumeIndex; // 记录用力呼气起点
                                        ForceExpirationStarted?.Invoke(m_peekVolumeIndex, m_peekFlowIndex);
                                    }
                                    else
                                    {
                                        /* 触发呼气开始事件 */
                                        ExpirationStarted?.Invoke(m_peekVolumeIndex, m_peekFlowIndex);
                                    }

                                    /* 重新初始化 */
                                    m_peekVolumeIndex = sampleIndex;
                                    m_peekVolumeKeepCount = 1;
                                }
                            }
                        }
                        break;
                    }
                case State.Expiration: // 正在呼气状态
                    {
                        /* 统计流量极小值 */
                        if (flowZeroCorrection < m_listFV[(int)m_peekFlowIndex].inFlow)
                        {
                            m_peekFlowIndex = sampleIndex;
                        }

                        /* 统计容积极小值 */
                        if (InVolume < m_listFV[(int)m_peekVolumeIndex].inVolume)
                        {
                            m_peekVolumeIndex = sampleIndex;
                            m_peekVolumeKeepCount = 1;
                        }
                        else
                        {
                            ++m_peekVolumeKeepCount;
                            /* 极值点是否保持稳定 */
                            if (m_peekVolumeKeepCount >= PEEK_VOLUME_KEEP_COUNT)
                            {
                                /* 过极值点后是否变化明显 */
                                if (((InVolume - m_listFV[(int)m_peekVolumeIndex].inVolume) > VOLUME_DELTA_THRESHOLD))
                                {
                                    /* 流量极小值点确认 */
                                    //Console.WriteLine($"流量极小值点:{m_peekFlowIndex}, {m_listFV[(int)m_peekFlowIndex].flow}");

                                    /* 进入正在吸气状态 */
                                    SetState(State.Inspiration);

                                    /* Volume极小值点存入列表 */
                                    m_peekMinVolumeIndexList.Add(m_peekVolumeIndex);

                                    /* 更新潮气量下界统计信息(只统计6次以内的极小值) */
                                    if (m_peekMinVolumeIndexList.Count <= 6)
                                    {
                                        m_tvLowerSum += m_listFV[(int)m_peekVolumeIndex].inVolume;
                                        m_tvLowerAvg = m_tvLowerSum / m_peekMinVolumeIndexList.Count;
                                    }

                                    /* 统计容积最小值点 */
                                    if (m_listFV[(int)m_peekVolumeIndex].inVolume < m_listFV[(int)m_minVolumeIndex].inVolume)
                                    {
                                        m_minVolumeIndex = m_peekVolumeIndex;
                                    }

                                    /* 是否启动测试时为吸气状态 */
                                    if (m_measureStartInspiration)
                                    {
                                        /* 完成一个呼吸周期,更新呼吸周期数 */
                                        ++RespiratoryCycleCount;

                                        /* 只统计6次以内的呼吸频率 */
                                        if (RespiratoryCycleCount <= 6)
                                        {
                                            /* 更新统计呼吸频率 */
                                            double respiratoryTime = (sampleIndex - m_measureStartIndex) * SAMPLE_TIME; // 总时间(ms)
                                            double respiratoryCycleTime = respiratoryTime / RespiratoryCycleCount; // 呼吸周期(ms)
                                            RespiratoryRate = (1000 * 60) / respiratoryCycleTime; // 呼吸频率(次/min)
                                        }
                                    }

                                    /* 是否已识别到用力呼气起点 */
                                    if (ForceExpirationStartIndex > 0)
                                    {
                                        /* 是否未识别到用力呼气终点 */
                                        if (ForceExpirationEndIndex <= ForceExpirationStartIndex)
                                        {
                                            /* 本次呼气结束点就是用力呼气终点 */
                                            ForceExpirationEndIndex = m_peekVolumeIndex;
                                        }
                                    }

                                    /* 触发吸气开始事件 */
                                    InspirationStarted?.Invoke(m_peekVolumeIndex, m_peekFlowIndex);

                                    /* 重新初始化 */
                                    m_peekVolumeIndex = sampleIndex;
                                    m_peekVolumeKeepCount = 1;
                                }
                            }
                        }
                        break;
                    }
                case State.Stop: // 测试已停止状态
                    {
                        // do nothing
                        break;
                    }
            }
        }

        /* 返回索引值对应的Time(没有做参数有效性检查) */
        public double GetTime(uint sampleIendex)
        {
            return sampleIendex * SAMPLE_TIME;
        }

        /* 返回索引值对应的吸气Flow(没有做参数有效性检查) */
        public double GetInFlow(uint sampleIendex)
        {
            return m_listFV[(int)sampleIendex].inFlow;
        }

        /* 返回索引值对应的吸气Volume(没有做参数有效性检查) */
        public double GetInVolume(uint sampleIendex)
        {
            return m_listFV[(int)sampleIendex].inVolume;
        }

        /* 返回索引值对应的呼气Flow(没有做参数有效性检查) */
        public double GetExFlow(uint sampleIendex)
        {
            return -GetInFlow(sampleIendex);
        }

        /* 返回索引值对应的呼气Volume,平移到Volume从0开始(没有做参数有效性检查) */
        public double GetExVolume(uint sampleIendex)
        {
            double minExVolume = 0.0;
            if (m_maxVolumeIndex < m_listFV.Count)
            {
                minExVolume = -m_listFV[(int)m_maxVolumeIndex].inVolume;
            }
            double exVolume = -GetInVolume(sampleIendex);
            return exVolume - minExVolume; // 平移到Volume从0开始
        }
    }
}
