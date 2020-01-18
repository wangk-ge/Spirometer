using System;
using System.Collections.Generic;

namespace PulmonaryFunctionLib
{
    /* 肺功能参数计算 */
    public class PulmonaryFunction
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
        public bool IsStoped { get { return (State.Stop == m_state); } } // 是否已进入停止状态
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
        public double FVC  // 用力肺活量(L)
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
        public double FEV1  // 1秒量(L)
        {
            get
            {
                if (ForceExpirationStartIndex > 0)
                {
                    /* 用力呼气点之后的1秒的Index */
                    uint oneSecIndex = ForceExpirationStartIndex + (uint)((1000 / SAMPLE_TIME) + 0.5);
                    /* 是否已完成一秒量的采集 */
                    if (oneSecIndex < m_listFV.Count)
                    {
                        return GetExVolume(oneSecIndex);
                    }
                }
                return 0.0;
            }
        }
        public double PEF // 峰值呼气流速(peak expiratory flow)
        {
            get
            {
                /* 最小吸气流速点就是最大呼气流速点 */
                uint maxExFlowIndex = m_minFlowIndex;
                if (maxExFlowIndex < m_listFV.Count)
                {
                    return GetExFlow(maxExFlowIndex);
                }
                return 0.0;
            }
        }
        public double FEF25 // 用力呼出25%肺活量的呼气流速
        {
            get
            {
                /* 已完成用力呼气测试 */
                if ((ForceExpirationStartIndex > 0)
                    && (ForceExpirationEndIndex > ForceExpirationStartIndex))
                {
                    /* 计算用力呼气肺活量的25% */
                    double percent25FVC = FVC * 0.25;

                    for (uint i = (uint)ForceExpirationStartIndex; i < ForceExpirationEndIndex; ++i)
                    {
                        double exVolume = GetExVolume(i);
                        if (exVolume >= percent25FVC)
                        {
                            /* 返回该点的呼气流速 */
                            return GetExFlow(i);
                        }
                    }
                }
                return 0.0;
            }
        }
        public double FEF50 // 用力呼出50%肺活量的呼气流速
        {
            get
            {
                /* 已完成用力呼气测试 */
                if ((ForceExpirationStartIndex > 0)
                    && (ForceExpirationEndIndex > ForceExpirationStartIndex))
                {
                    /* 计算用力呼气肺活量的50% */
                    double percent50FVC = FVC * 0.50;

                    for (uint i = (uint)ForceExpirationStartIndex; i < ForceExpirationEndIndex; ++i)
                    {
                        double exVolume = GetExVolume(i);
                        if (exVolume >= percent50FVC)
                        {
                            /* 返回该点的呼气流速 */
                            return GetExFlow(i);
                        }
                    }
                }
                return 0.0;
            }
        }
        public double FEF75 // 用力呼出75%肺活量的呼气流速
        {
            get
            {
                /* 已完成用力呼气测试 */
                if ((ForceExpirationStartIndex > 0)
                    && (ForceExpirationEndIndex > ForceExpirationStartIndex))
                {
                    /* 计算用力呼气肺活量的75% */
                    double percent75FVC = FVC * 0.75;

                    for (uint i = (uint)ForceExpirationStartIndex; i < ForceExpirationEndIndex; ++i)
                    {
                        double exVolume = GetExVolume(i);
                        if (exVolume >= percent75FVC)
                        {
                            /* 返回该点的呼气流速 */
                            return GetExFlow(i);
                        }
                    }
                }
                return 0.0;
            }
        }
        public double TVLowerAvg // 残气量下界平均值(L)
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

        public delegate void MeasureStartHandler(uint sampleIndex, bool inspiration); // 测试启动事件代理
        public event MeasureStartHandler MeasureStarted; // 测试启动事件
        public delegate void InspirationStartHandler(uint sampleIndex, uint peekFlowIndex); // 吸气开始事件代理
        public event InspirationStartHandler InspirationStarted; // 吸气开始事件
        public delegate void ExpirationStartHandler(uint sampleIndex, uint peekFlowIndex); // 呼气开始事件代理
        public event ExpirationStartHandler ExpirationStarted; // 呼气开始事件
        public delegate void ForceExpirationStartHandler(uint sampleIndex, uint peekFlowIndex); // 用力呼气开始事件代理
        public event ForceExpirationStartHandler ForceExpirationStarted; // 用力呼气开始事件
        public delegate void MeasureStopHandler(uint sampleIndex); // 测试停止事件代理
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
            WaitStart, // 等待启动测试状态
            Inspiration, // 正在吸气状态
            Expiration, // 正在呼气状态
            Stop, // 测试已停止状态
        }

        private State m_state = State.Reset; // 工作状态
        private readonly double SAMPLE_TIME = 3.0; // 采样时间(ms)
        private WaveStatistician m_waveStatistician = new WaveStatistician(); // 用于统计波动数据
        private readonly int START_SAMPLE_COUNT = 2; // 启动检测,波动统计采样次数
        private readonly int STOP_SAMPLE_COUNT = 100; // 停止检测,波动统计采样次数
        private readonly double START_FLOW_DELTA = 0.01; // 斜度绝对值超过该阈值将识别为启动测试(斜度为正表示吸气启动、为负表示吹气开始)
        private readonly double STOP_FLOW_THRESHOLD = 0.01; // 停止检测流量阈值,当启动测试后如果流量对值小于阈值,则开始检测停止条件

        /* 流量(Flow)-时间(Time)曲线极值点 */
        private uint m_peekFlowIndex = 0U; // 跟踪流量曲线当前极值点Index

        /* 容积(Volume)-时间(Time)曲线极值点 */
        private uint m_peekVolumeIndex = 0U; // 跟踪容积曲线当前极值点Index
        private uint m_peekVolumeKeepCount = 0U; // 跟踪当前极值点不变的次数
        private readonly uint PEEK_VOLUME_KEEP_COUNT = 100; // 如果极值点保持指定的采样次数不变化则认为是稳定的
        private List<uint> m_peekMaxVolumeIndexList = new List<uint>(); // Volume极大值点列表
        private List<uint> m_peekMinVolumeIndexList = new List<uint>(); // Volume极小值点列表

        /* 阈值 */
        private readonly double VOLUME_DELTA_THRESHOLD = 0.05; // Volume变化量阈值,在过极值点指定采样次数后如果Volume变化量超过阈值则确认极值点有效
        private readonly double FORCE_EXPIRATION_FLOW = 2.0; // 用力呼气Flow阈值,达到该Flow值判断为用力呼气

        /* 测试启动/结束点 */
        private uint m_measureStartIndex = 0U; // 测试启动点Index
        private bool m_measureStartInspiration = true; // 是否启动测试时为吸气状态
        private uint m_measureEndIndex = 0U; // 测试结束点Index

        /* 容积参数 */
        private uint m_maxVolumeIndex = 0U; // 容积最大值点Index
        private uint m_minVolumeIndex = 0U; // 容积最小值点Index

        /* 流速参数 */
        private uint m_maxFlowIndex = 0U; // 流速最大值点Index
        private uint m_minFlowIndex = 0U; // 流速最小值点Index

        /* 潮气量上下界信息(用于求TV和FRC) */
        private double m_tvUpperSum = 0.0; // 潮气量上界Volume求和值
        private double m_tvUpperAvg = 0.0; // 潮气量上界Volume平均值
        private double m_tvLowerSum = 0.0; // 潮气量下界Volume求和值
        private double m_tvLowerAvg = 0.0; // 潮气量下界Volume平均值

        private readonly bool m_autoStop = true; // 是否自动检测停止状态

        public PulmonaryFunction(double sampleTime, bool autoStop = true)
        {
            SAMPLE_TIME = sampleTime;
            m_autoStop = autoStop;
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
            m_waveStatistician.Reset();
            m_peekFlowIndex = 0U;
            m_peekVolumeIndex = 0U;
            m_peekVolumeKeepCount = 0U;
            m_peekMaxVolumeIndexList.Clear();
            m_peekMinVolumeIndexList.Clear();
            m_measureStartIndex = 0U;
            m_measureStartInspiration = true;
            m_measureEndIndex = 0U;
            RespiratoryCycleCount = 0U;
            ForceExpirationStartIndex = 0U;
            ForceExpirationEndIndex = 0U;
            m_maxVolumeIndex = 0U;
            m_minVolumeIndex = 0U;
            m_maxFlowIndex = 0U;
            m_minFlowIndex = 0U;
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

            /* 当前吸气流量 */
            InFlow = flow;

            if ((m_state > State.WaitStart)
                && (m_state < State.Stop))
            {
                /* 更新当前吸气容积 */
                InVolume += InFlow * SAMPLE_TIME / 1000;

                /* 是否开启了自动停止检测 */
                if (m_autoStop)
                {
                    /* 检测停止条件 */
                    if (Math.Abs(flow) < STOP_FLOW_THRESHOLD)
                    {
                        /* 持续统计波动范围 */
                        m_waveStatistician.Input(flow);

                        if (m_waveStatistician.SampleCount >= STOP_SAMPLE_COUNT)
                        {
                            /* 检测停止条件 */
                            double delta = m_waveStatistician.Delta(flow);
                            if (Math.Abs(delta) < START_FLOW_DELTA)
                            {
                                /* 重置波动统计器 */
                                m_waveStatistician.Reset();

                                /* 记录结束测试点Index */
                                m_measureEndIndex = sampleIndex;

                                /* 进入测试停止状态 */
                                SetState(State.Stop);

                                /* 触发测结束动事件 */
                                MeasureStoped?.Invoke(m_measureEndIndex);
                            }
                            else
                            {
                                /* 统计波动范围 */
                                m_waveStatistician.Input(flow);
                            }
                        }
                        else
                        {
                            /* 统计波动范围 */
                            m_waveStatistician.Input(flow);
                        }
                    }
                    else
                    {
                        /* 重置波动统计器 */
                        m_waveStatistician.Reset();
                    }
                }
            }

            /* 记录Flow-Volume数据到队列 */
            m_listFV.Add(new FlowVolume { inFlow = InFlow, inVolume = InVolume});

            switch (m_state)
            {
                case State.Reset: // 复位状态
                    {
                        /* 进入等待启动测试状态 */
                        SetState(State.WaitStart);

                        /* 开始统计波动范围 */
                        m_waveStatistician.Input(flow);
                        break;
                    }
                case State.WaitStart: // 等待启动测试状态
                    {
                        if (m_waveStatistician.SampleCount >= START_SAMPLE_COUNT)
                        {
                            /* 检测启动条件 */
                            double delta = m_waveStatistician.Delta(flow);
                            if (Math.Abs(delta) > START_FLOW_DELTA)
                            {
                                /* 初始化容积 */
                                InVolume = (m_waveStatistician.AvgVal + flow) * (SAMPLE_TIME / 1000);

                                /* 重置波动统计器 */
                                m_waveStatistician.Reset();

                                /* 呼吸周期数清零 */
                                RespiratoryCycleCount = 0U;

                                /* 记录启动测试点Index */
                                m_measureStartIndex = sampleIndex;

                                /* 记录启动类型(是否吸气启动) */
                                m_measureStartInspiration = (delta > START_FLOW_DELTA);

                                /* 进入正在吸气/呼气状态 */
                                SetState(m_measureStartInspiration ? State.Inspiration : State.Expiration);

                                /* 触发测试启动事件 */
                                MeasureStarted?.Invoke(m_measureStartIndex, m_measureStartInspiration);

                                /* 初始化流量极值点 */
                                m_peekFlowIndex = sampleIndex;

                                /* 初始化容积极值点 */
                                m_peekVolumeIndex = sampleIndex;
                                m_peekVolumeKeepCount = 1;
                            }
                            else
                            {
                                /* 统计波动范围 */
                                m_waveStatistician.Input(flow);
                            }
                        }
                        else
                        {
                            /* 统计波动范围 */
                            m_waveStatistician.Input(flow);
                        }
                        break;
                    }
                case State.Inspiration: // 正在吸气状态
                    {
                        /* 统计流量极大值 */
                        if (flow > m_listFV[(int)m_peekFlowIndex].inFlow)
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
                                    //Console.WriteLine($"流量极大值点:{m_peekFlowIndex}, {m_listFV[(int)m_peekFlowIndex].inFlow}");

                                    /* 统计流量最大值点 */
                                    if (m_listFV[(int)m_peekFlowIndex].inFlow > m_listFV[(int)m_maxFlowIndex].inFlow)
                                    {
                                        m_maxFlowIndex = m_peekFlowIndex;
                                    }

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
                                    double exFlow = -flow;
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
                        if (flow < m_listFV[(int)m_peekFlowIndex].inFlow)
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
                                    //Console.WriteLine($"流量极小值点:{m_peekFlowIndex}, {m_listFV[(int)m_peekFlowIndex].inFlow}");

                                    /* 统计流量最小值点 */
                                    if (m_listFV[(int)m_peekFlowIndex].inFlow < m_listFV[(int)m_minFlowIndex].inFlow)
                                    {
                                        m_minFlowIndex = m_peekFlowIndex;
                                    }

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
