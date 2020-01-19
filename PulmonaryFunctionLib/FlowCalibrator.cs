using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulmonaryFunctionLib
{
    /* 流量校准器 */
    public class FlowCalibrator
    {
        public double Time { get; private set; } = 0.0; // 时间(ms)
        public double Presure { get; private set; } = 0.0; // 当前压差
        public uint SampleCount { get { return (uint)m_listPresure.Count; } } // 已采集的样本数
        public double PresureSum { get; private set; } = 0.0; // 采集的Presure求和值
        public double PresureAvg { get; private set; } = 0.0; // 采集的Presure平均值
        public double PresureVariance { get; private set; } = 0.0; // 采集的Presure方差(代表压差的离散程度,越小表示越集中)
        public double CalVolume { get; private set; } = 0.0; // 定标桶容积(单位: L)
        public double PeekPresure { get { return (m_peekPresureIndex < m_listPresure.Count) ? m_listPresure[(int)m_peekPresureIndex] : 0.0; } } // Presure极值
        public double PresureFlowScale { get { return (PresureSum != 0) ? (SAMPLE_RATE / Math.Abs(PresureSum)) : 0.0; } } // Presure转换成Flow的比例系数
        public bool IsValid { get { return true; } } // 本次结果是否有效(自动判断校准结果有效性)[TODO]

        public delegate void InspirationStartHandler(uint sampleIndex); // 吸气开始事件代理
        public event InspirationStartHandler InspirationStarted; // 吸气开始事件
        public delegate void ExpirationStartHandler(uint sampleIndex); // 呼气开始事件代理
        public event ExpirationStartHandler ExpirationStarted; // 呼气开始事件
        public delegate void MeasureStopHandler(uint sampleIndex, uint peekPresureIndex); // 测试停止事件代理
        public event MeasureStopHandler MeasureStoped; // 测试停止事件

        private List<double> m_listPresure = new List<double>(); // 压差数据列表

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
        private readonly double SAMPLE_RATE = 330; // 采样率
        private WaveStatistician m_waveStatistician = new WaveStatistician(); // 用于统计波动数据
        private readonly int START_SAMPLE_COUNT = 2; // 启动检测,波动统计采样次数
        private readonly int STOP_SAMPLE_COUNT = 20; // 停止检测,波动统计采样次数
        private readonly double START_PRESURE_DELTA = 0.0015; // 斜度绝对值超过该阈值将识别为启动测试(斜度为正表示吸气启动、为负表示吹气开始)
        private readonly double STOP_PRESURE_THRESHOLD = 0.002; // 停止检测压差阈值,当启动测试后如果压差绝对值小于阈值,则开始检测停止条件

        private uint m_peekPresureIndex = 0U; // 跟踪峰值压差点Index

        /* 测试启动/结束点 */
        private uint m_measureStartIndex = 0U; // 测试启动点Index
        private uint m_measureEndIndex = 0U; // 测试结束点Index

        public FlowCalibrator(double sampleRate, double calVolume = 1.0)
        {
            SAMPLE_RATE = sampleRate;
            SAMPLE_TIME = 1000 / sampleRate;
            CalVolume = calVolume;
        }

        /* 状态重置 */
        public void Reset()
        {
            //Time = 0.0;
            //Presure = 0.0;
            PresureSum = 0.0;
            PresureAvg = 0.0;
            PresureVariance = 0.0;
            //m_listPresure.Clear();
            m_state = State.Reset;
            m_waveStatistician.Reset();
            m_peekPresureIndex = 0U;
            m_measureStartIndex = 0U;
            m_measureEndIndex = 0U;
        }

        /* 计算Presure数据均值 */
        private double CalcPresureAvg()
        {
            if (m_measureEndIndex <= m_measureStartIndex)
            {
                return 0.0;
            }

            uint n = m_measureEndIndex - m_measureStartIndex;
            double avg = PresureSum / n;
            return avg;
        }

        /* 计算Presure数据方差值 */
        private double CalcPresureVariance()
        {
            double varianceSum = 0.0;
            uint i = 0;
            for (i = m_measureStartIndex; i < m_measureEndIndex; ++i)
            {
                double d = m_listPresure[(int)i] - PresureAvg;
                varianceSum += (d * d);
            }
            double variance = 0.0;
            if (i > 0)
            {
                variance = varianceSum / i;
            }
            return variance;
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

        /* 输入压差数据 */
        public void Input(double presure)
        {
            /* Presure样本索引值 */
            uint sampleIndex = (uint)m_listPresure.Count;

            /* 样本时间戳 */
            Time = sampleIndex * SAMPLE_TIME;

            /* 当前压差 */
            Presure = presure;

            if ((m_state > State.WaitStart)
                && (m_state < State.Stop))
            {
                /* 统计和值 */
                PresureSum += presure;

                /* 检测停止条件 */
                if (Math.Abs(presure) < STOP_PRESURE_THRESHOLD)
                {
                    if (m_waveStatistician.SampleCount >= STOP_SAMPLE_COUNT)
                    {
                        /* 检测停止条件 */
                        double delta = m_waveStatistician.Delta(presure);
                        if (Math.Abs(delta) < START_PRESURE_DELTA)
                        {
                            /* 重置波动统计器 */
                            m_waveStatistician.Reset();

                            /* 记录结束测试点Index */
                            m_measureEndIndex = sampleIndex;

                            /* 进入测试停止状态 */
                            SetState(State.Stop);

                            /* 计算均值 */
                            PresureAvg = CalcPresureAvg();

                            /* 计算方差 */
                            PresureVariance = CalcPresureVariance();

                            /* 触发测量结束事件 */
                            MeasureStoped?.Invoke(m_measureEndIndex, m_peekPresureIndex);
                        }
                        else
                        {
                            /* 统计波动范围 */
                            m_waveStatistician.Input(presure);
                        }
                    }
                    else
                    {
                        /* 统计波动范围 */
                        m_waveStatistician.Input(presure);
                    }
                }
                else
                {
                    /* 重置波动统计器 */
                    m_waveStatistician.Reset();
                }
            }

            /* 记录Presure数据到队列 */
            m_listPresure.Add(presure);

            switch (m_state)
            {
                case State.Reset: // 复位状态
                    {
                        /* 进入等待启动测试状态 */
                        SetState(State.WaitStart);

                        /* 开始统计波动范围 */
                        m_waveStatistician.Input(presure);
                        break;
                    }
                case State.WaitStart: // 等待启动测试状态
                    {
                        if (m_waveStatistician.SampleCount >= START_SAMPLE_COUNT)
                        {
                            /* 检测启动条件 */
                            double delta = m_waveStatistician.Delta(presure);
                            if (Math.Abs(delta) > START_PRESURE_DELTA)
                            {
                                /* 初始化和值 */
                                PresureSum = (m_waveStatistician.AvgVal + presure);

                                /* 重置波动统计器 */
                                m_waveStatistician.Reset();

                                /* 记录启动测试点Index */
                                m_measureStartIndex = sampleIndex;

                                /* 记录启动类型(是否吸气启动) */
                                if (delta > START_PRESURE_DELTA)
                                { // 吸气开始
                                    /* 进入正在吸气 */
                                    SetState(State.Inspiration);

                                    /* 触发吸气开始事件 */
                                    InspirationStarted?.Invoke(m_measureStartIndex);
                                }
                                else
                                { // 呼气开始
                                    /* 进入正在呼气状态 */
                                    SetState(State.Expiration);

                                    /* 触发呼气开始事件 */
                                    ExpirationStarted?.Invoke(m_measureStartIndex);
                                }
                            }
                            else
                            {
                                /* 统计波动范围 */
                                m_waveStatistician.Input(presure);
                            }
                        }
                        else
                        {
                            /* 统计波动范围 */
                            m_waveStatistician.Input(presure);
                        }
                        break;
                    }
                case State.Inspiration: // 正在吸气状态
                    {
                        /* 统计流量极大值 */
                        if (presure > m_listPresure[(int)m_peekPresureIndex])
                        {
                            m_peekPresureIndex = sampleIndex;
                        }
                        break;
                    }
                case State.Expiration: // 正在呼气状态
                    {
                        /* 统计流量极小值 */
                        if (presure < m_listPresure[(int)m_peekPresureIndex])
                        {
                            m_peekPresureIndex = sampleIndex;
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
    }
}
