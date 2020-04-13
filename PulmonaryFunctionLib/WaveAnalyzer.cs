using System;
using System.Collections.Generic;

namespace PulmonaryFunctionLib
{
    /* 样本方向类型 */
    public enum WaveSampleDirection
    {
        None = 0,
        PositiveSample, // 正方向样本
        NegativeSample, // 负方向样本
    }

    /* 代理 */
    public delegate void WaveSampleStartHandler(uint dataIndex, WaveSampleDirection direction); // 采样开始事件代理
    public delegate void WaveSampleStopHandler(uint dataIndex, WaveSampleDirection direction, uint sampleIndex); // 采样停止事件代理

    /* 波形数据分析器 */
    public class WaveAnalyzer
    {
        /* 实时更新属性 */
        public double Time { get { return (m_listData.Count > 0) ? GetTime((uint)(m_listData.Count - 1)) : 0.0; } } // 当前采样时间点(ms)
        public double Data { get { return (m_listData.Count > 0) ? m_listData[m_listData.Count - 1] : 0.0; } } // 当前最新采集的[数据]值
        public uint DataCount { get { return (uint)m_listData.Count; } } // 当前已采集的所有[数据]总个数
        public uint SampleCount { get { return (uint)m_sampleInfoList.Count; } } // 已采集的[样本]个数
        public double CurrSampleDataSum { get { return m_dataSum; } } // 当前正在采集[样本的数据]求和值
        public bool StartSampling { get { return (m_state > State.WaitStartSample) && (m_state < State.SampleStop); } } // 当前是否已启动采样状态

        /* 事件 */
        public event WaveSampleStartHandler SampleStarted; // 采样开始事件
        public event WaveSampleStopHandler SampleStoped; // 采样停止事件

        /* 样本信息类型(一个样本为一起起始和结束之间的所有数据) */
        private struct WaveSampleInfo
        {
            public double sum; // 和值
            public uint startIndex; // 起始点索引
            public uint endIndex; // 结束点索引
            public double minIndex; // 最小值点索引
            public double maxIndex; // 最大值点索引
        }
        /* 样本信息列表 */
        private List<WaveSampleInfo> m_sampleInfoList = new List<WaveSampleInfo>();

        /* 数据列表 */
        private List<double> m_listData = new List<double>();

        /* 状态类型 */
        private enum State
        {
            Reset, // [复位]状态
            WaitStartSample, // [等待启动采样]状态
            PositiveSample, // [正方向采样]状态
            NegativeSample, // [负方向采样]状态
            SampleStop, // [采样停止]状态
        }
        private State m_state = State.Reset; // 当前状态

        private WaveStatistician m_waveStatistician = new WaveStatistician(100); // 数据波动统计器

        /* 参数 */
        private readonly double SAMPLE_TIME = 3.03; // 采样时间(ms)
        private readonly double SAMPLE_RATE = 330; // 采样率

        /* 阈值(用于检测采样启动和停止条件) */
        private readonly int START_SAMPLE_COUNT = 2; // 启动采样检测, 波动统计采样次数
        private readonly int STOP_SAMPLE_COUNT = 20; // 停止采样检测, 波动统计采样次数
        private readonly double START_DELTA = 0.0015; // 斜度绝对值超过该阈值将识别为启动采样(斜度为正表示正方向采样启动、为负表示负方向采样启动)
        private readonly double STOP_THRESHOLD = 0.002; // 停止检测阈值,当启动采样后如果数据绝对值小于阈值,则开始检测停止条件

        /* 当前样本特征点统计信息 */
        private uint m_startIndex = 0U; // 当前样本采集启动点Index
        private uint m_endIndex = 0U; // 当前样本采集结束点Index
        private uint m_minDataIndex = 0U; // 当前样本最小值点Index
        private uint m_maxDataIndex = 0U; // 当前样本最大值点Index
        private double m_dataSum = 0.0; // 当前样本Data求和值

        public WaveAnalyzer(double sampleRate,
            int startSampleCount = 2, // 启动采样检测, 波动统计采样次数
            double startDelta = 0.0015, // 斜度绝对值超过该阈值将识别为启动采样(斜度为正表示正方向采样启动、为负表示负方向采样启动)
            int stopSampleCount = 20, // 停止采样检测, 波动统计采样次数
            double stopThreshold = 0.002) // 停止检测阈值,当启动采样后如果数据绝对值小于阈值,则开始检测停止条件
        {
            SAMPLE_RATE = sampleRate;
            SAMPLE_TIME = 1000 / sampleRate;
            
            /* 阈值参数 */
            START_SAMPLE_COUNT = startSampleCount;
            START_DELTA = startDelta;
            STOP_SAMPLE_COUNT = stopSampleCount;
            STOP_THRESHOLD = stopThreshold;
        }

        /* 状态重置(保留数据+样本信息) */
        public void Reset()
        {
            /* 进入[复位]状态 */
            m_state = State.Reset;

            /* 复位波动统计器 */
            m_waveStatistician.Reset();

            /* 复位当前样本特征点统计信息 */
            m_startIndex = 0U;
            m_endIndex = 0U;
            m_minDataIndex = 0U;
            m_maxDataIndex = 0U;
            m_dataSum = 0.0;
        }

        /* 清除(重置状态并清除数据和样本信息) */
        public void Clear()
        {
            /* 重置状态 */
            Reset();
            
            /* 清空数据和样本信息 */
            m_listData.Clear();
            m_sampleInfoList.Clear();
        }

        /* 检测样本是否有效 */
        private bool SampleIsValid(WaveSampleInfo info)
        {
            if ((info.startIndex >= info.endIndex)
                || (info.startIndex >= m_listData.Count)
                || (info.endIndex >= m_listData.Count)
                || (info.minIndex >= m_listData.Count)
                || (info.maxIndex >= m_listData.Count))
            {
                return false;
            }

            return true;
        }

        /* 检测样本是否有效(通过样本索引) */
        public bool SampleIsValid(uint sampleIndex)
        {
            if (sampleIndex >= m_sampleInfoList.Count)
            {
                return false;
            }
            var info = m_sampleInfoList[(int)sampleIndex];
            return SampleIsValid(info);
        }

        /* 样本的数据方向(通过样本索引) */
        public WaveSampleDirection SampleDataDirection(uint sampleIndex)
        {
            if (sampleIndex >= m_sampleInfoList.Count)
            {
                return WaveSampleDirection.None;
            }

            var info = m_sampleInfoList[(int)sampleIndex];

            WaveSampleDirection direction = WaveSampleDirection.None;
            if (info.sum > 0)
            {
                direction = WaveSampleDirection.PositiveSample;
            }
            else if (info.sum < 0)
            {
                direction = WaveSampleDirection.NegativeSample;
            }

            return direction;
        }

        /* 样本的数据求和值(通过样本索引) */
        public double SampleDataSum(uint sampleIndex)
        {
            if (sampleIndex >= m_sampleInfoList.Count)
            {
                return 0.0;
            }
            var info = m_sampleInfoList[(int)sampleIndex];
            return info.sum;
        }

        /* 样本的数据最小值 */
        private double SampleMinData(WaveSampleInfo info)
        {
            if (info.minIndex >= m_listData.Count)
            {
                return 0.0;
            }
            return m_listData[(int)info.minIndex];
        }

        /* 样本的数据最小值(通过样本索引) */
        public double SampleMinData(uint sampleIndex)
        {
            if (sampleIndex >= m_sampleInfoList.Count)
            {
                return 0.0;
            }
            var info = m_sampleInfoList[(int)sampleIndex];
            return SampleMinData(info);
        }

        /* 样本的数据最大值 */
        private double SampleMaxData(WaveSampleInfo info)
        {
            if (info.maxIndex >= m_listData.Count)
            {
                return 0.0;
            }
            return m_listData[(int)info.maxIndex];
        }

        /* 样本的数据最大值(通过样本索引) */
        public double SampleMaxData(uint sampleIndex)
        {
            if (sampleIndex >= m_sampleInfoList.Count)
            {
                return 0.0;
            }
            var info = m_sampleInfoList[(int)sampleIndex];
            return SampleMaxData(info);
        }

        /* 样本的数据个数 */
        private uint SampleDataCount(WaveSampleInfo info)
        {
            if (info.startIndex >= info.endIndex)
            {
                return 0U;
            }
            uint dataNum = info.endIndex - info.startIndex + 1;
            return dataNum;
        }

        /* 样本的数据个数(通过样本索引) */
        public uint SampleDataCount(uint sampleIndex)
        {
            if (sampleIndex >= m_sampleInfoList.Count)
            {
                return 0U;
            }
            var info = m_sampleInfoList[(int)sampleIndex];
            return SampleDataCount(info);
        }

        /* 样本时间(ms) */
        private double SampleTime(WaveSampleInfo info)
        {
            if (info.startIndex >= info.endIndex)
            {
                return 0U;
            }
            uint dataNum = info.endIndex - info.startIndex + 1;
            return SAMPLE_TIME * dataNum;
        }

        /* 样本时间(ms)(通过样本索引) */
        public double SampleTime(uint sampleIndex)
        {
            if (sampleIndex >= m_sampleInfoList.Count)
            {
                return 0U;
            }
            var info = m_sampleInfoList[(int)sampleIndex];
            return SampleTime(info);
        }

        /* 样本的平均值 */
        private double SampleDataAvg(WaveSampleInfo info)
        {
            uint dataNum = SampleDataCount(info);
            if (dataNum <= 0)
            {
                return 0.0;
            }
            return info.sum / dataNum;
        }

        /* 样本的平均值(通过样本索引) */
        public double SampleDataAvg(uint sampleIndex)
        {
            if (sampleIndex >= m_sampleInfoList.Count)
            {
                return 0.0;
            }
            var info = m_sampleInfoList[(int)sampleIndex];
            return SampleDataAvg(info);
        }

        /* 样本的方差(代表样本的离散程度,越小表示越集中) */
        private double SampleDataVariance(WaveSampleInfo info)
        {
            /* 取得样本均值 */
            double sampleAvg = SampleDataAvg(info);

            /* 计算方差 */
            double varianceSum = 0.0;
            uint i = 0;
            for (i = info.startIndex; i <= info.endIndex; ++i)
            {
                double d = m_listData[(int)i] - sampleAvg;
                varianceSum += (d * d);
            }
            double variance = 0.0;
            if (i > 0)
            {
                variance = varianceSum / i;
            }
            return variance;
        }

        /* 样本的方差(代表样本的离散程度,越小表示越集中)(通过样本索引) */
        public double SampleDataVariance(uint sampleIndex)
        {
            if (sampleIndex >= m_sampleInfoList.Count)
            {
                return 0.0;
            }
            var info = m_sampleInfoList[(int)sampleIndex];
            double variance = SampleDataVariance(info);
            return variance;
        }

        /* 样本数据迭代器 */
        private IEnumerable<double> SampleDataIterator(WaveSampleInfo info)
        {
            uint i = 0;
            for (i = info.startIndex; i <= info.endIndex; ++i)
            {
                yield return m_listData[(int)i];
            }
        }

        /* 样本数据迭代器(通过样本索引) */
        public IEnumerable<double> SampleDataIterator(uint sampleIndex)
        {
            if (sampleIndex >= m_sampleInfoList.Count)
            {
                yield return 0.0;
            }
            var info = m_sampleInfoList[(int)sampleIndex];
            uint i = 0;
            for (i = info.startIndex; i <= info.endIndex; ++i)
            {
                yield return m_listData[(int)i];
            }
        }

        /* 切换状态 */
        private void SetState(State state)
        {
            /* 检查是否发生状态更新 */
            if (state == m_state)
            {
                return;
            }

            /* 切换状态 */
            m_state = state;
        }

        /* 输入数据 */
        public void Input(double data)
        {
            /* 最新数据索引值 */
            uint dataIndex = (uint)m_listData.Count;

            /* 记录数据到队列 */
            m_listData.Add(data);

            /* 如果是正在采样状态 */
            if ((m_state > State.WaitStartSample)
                && (m_state < State.SampleStop))
            {
                /* 统计和值 */
                m_dataSum += data;

                /* 检测采样停止条件 */
                if (Math.Abs(data) < STOP_THRESHOLD)
                {
                    if (m_waveStatistician.SampleCount >= STOP_SAMPLE_COUNT)
                    {
                        /* 检测采样停止条件 */
                        double delta = m_waveStatistician.Delta(data);
                        if (Math.Abs(delta) < START_DELTA)
                        {
                            /* 重置波动统计器 */
                            m_waveStatistician.Reset();

                            /* 记录结束采样点Index */
                            m_endIndex = dataIndex;

                            /* 统计最大/最小值点(索引) */
                            if (data < m_listData[(int)m_minDataIndex])
                            {
                                m_minDataIndex = dataIndex;
                            }
                            if (data > m_listData[(int)m_maxDataIndex])
                            {
                                m_maxDataIndex = dataIndex;
                            }

                            /* 样本方向 */
                            WaveSampleDirection direction = (m_state == State.PositiveSample) ?
                                WaveSampleDirection.PositiveSample : WaveSampleDirection.NegativeSample;

                            /* 进入[采样停止]状态 */
                            SetState(State.SampleStop);

                            /* 记录当前样本信息 */
                            WaveSampleInfo info = new WaveSampleInfo() {
                                startIndex = m_startIndex, 
                                endIndex = m_endIndex, 
                                minIndex = m_minDataIndex, 
                                maxIndex = m_maxDataIndex,
                                sum = m_dataSum
                            };
                            m_sampleInfoList.Add(info);

                            /* 触发采样结束事件 */
                            uint sampleIndex = (uint)(m_sampleInfoList.Count - 1);
                            SampleStoped?.Invoke(m_endIndex, direction, sampleIndex);
                        }
                        else
                        {
                            /* 统计波动范围 */
                            m_waveStatistician.Input(data);
                        }
                    }
                    else
                    {
                        /* 统计波动范围 */
                        m_waveStatistician.Input(data);
                    }
                }
                else
                {
                    /* 重置波动统计器 */
                    m_waveStatistician.Reset();
                }
            }

            switch (m_state)
            {
                case State.Reset: // [复位]状态
                    {
                        /* 进入[等待启动采样]状态 */
                        SetState(State.WaitStartSample);

                        /* 开始统计波动范围 */
                        m_waveStatistician.Input(data);
                        break;
                    }
                case State.WaitStartSample: // [等待启动采样]状态
                    {
                        if (m_waveStatistician.SampleCount >= START_SAMPLE_COUNT)
                        {
                            /* 检测启动条件 */
                            double delta = m_waveStatistician.Delta(data);
                            if (Math.Abs(delta) > START_DELTA)
                            {
                                /* 记录启动采样点Index */
                                m_startIndex = dataIndex - 1;

                                /* 初始化最小/最大值点 */
                                m_minDataIndex = m_startIndex;
                                m_maxDataIndex = m_startIndex;
                                if (data < m_listData[(int)m_minDataIndex])
                                {
                                    m_minDataIndex = dataIndex;
                                }
                                if (data > m_listData[(int)m_maxDataIndex])
                                {
                                    m_maxDataIndex = dataIndex;
                                }

                                /* 初始化和值 */
                                m_dataSum = (m_listData[(int)m_startIndex] + data);

                                /* 重置波动统计器 */
                                m_waveStatistician.Reset();

                                /* 记录启动类型(是否正方向启动) */
                                if (delta > START_DELTA)
                                { //正方向采样开始
                                    /* 进入[正方向采样]状态 */
                                    SetState(State.PositiveSample);

                                    /* 方向: 正方向采样 */
                                    WaveSampleDirection direction = WaveSampleDirection.PositiveSample;

                                    /* 触发(正方向)采样开始事件 */
                                    SampleStarted?.Invoke(m_startIndex, direction);
                                }
                                else
                                { // 负方向采样开始
                                    /* 进入[负方向采样]状态 */
                                    SetState(State.NegativeSample);

                                    /* 方向: 负方向采样 */
                                    WaveSampleDirection direction = WaveSampleDirection.NegativeSample;

                                    /* 触发(负方向)采样开始事件 */
                                    SampleStarted?.Invoke(m_startIndex, direction);
                                }
                            }
                            else
                            {
                                /* 统计波动范围 */
                                m_waveStatistician.Input(data);
                            }
                        }
                        else
                        {
                            /* 统计波动范围 */
                            m_waveStatistician.Input(data);
                        }
                        break;
                    }
                case State.PositiveSample: // [正方向采样]状态
                    {
                        /* 统计最大/最小值点 */
                        if (data < m_listData[(int)m_minDataIndex])
                        {
                            m_minDataIndex = dataIndex;
                        }
                        if (data > m_listData[(int)m_maxDataIndex])
                        {
                            m_maxDataIndex = dataIndex;
                        }
                        break;
                    }
                case State.NegativeSample: // [负方向采样]状态
                    {
                        /* 统计最大/最小值点 */
                        if (data < m_listData[(int)m_minDataIndex])
                        {
                            m_minDataIndex = dataIndex;
                        }
                        if (data > m_listData[(int)m_maxDataIndex])
                        {
                            m_maxDataIndex = dataIndex;
                        }
                        break;
                    }
                case State.SampleStop: // [采样停止]状态
                    {
                        // do nothing
                        break;
                    }
            }
        }

        /* 返回索引值对应的Data(没有做参数有效性检查) */
        public double GetData(uint dataIndex)
        {
            return m_listData[(int)dataIndex];
        }

        /* 返回索引值对应的Time(没有做参数有效性检查) */
        public double GetTime(uint dataIndex)
        {
            return dataIndex * SAMPLE_TIME;
        }
    }
}
