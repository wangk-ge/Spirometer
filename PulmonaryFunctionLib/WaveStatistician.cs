using System.Collections.Generic;

namespace PulmonaryFunctionLib
{
    /* 波动数据统计器 */
    class WaveStatistician
    {
        public double AvgVal { get { return (SampleCount > 0) ? (m_sumVal / SampleCount) : 0; } } // 波动平均值
        public uint SampleCount { get; private set; } = 0U; // 已采样次数
        public uint SlidingWindowSize { get; private set; } = uint.MaxValue; // 滑动窗口大小

        private double m_sumVal = 0.0; // 滑动窗口求和值
        private Queue<double> m_slidingWindowList; // 滑动窗口队列

        public WaveStatistician(uint slidingWindowSize = uint.MaxValue)
        {
            SlidingWindowSize = slidingWindowSize;
            if (SlidingWindowSize < uint.MaxValue)
            {
                m_slidingWindowList = new Queue<double>();
            }
        }

        /* 输入数据 */
        public void Input(double data)
        {
            /* 累加采样次数 */
            ++SampleCount;

            /* 累加和值 */
            m_sumVal += data;

            /* 如果设置了滑动窗口,则处理滑动窗口 */
            if (SlidingWindowSize < uint.MaxValue)
            {
                /* 在滑动窗口队列中记录数据 */
                m_slidingWindowList.Enqueue(data);
                /* 处理窗口滑动 */
                if (m_slidingWindowList.Count > SlidingWindowSize)
                {
                    double oldVal = m_slidingWindowList.Dequeue();
                    m_sumVal -= oldVal;
                    SampleCount = SlidingWindowSize;
                }
            }
        }

        /* 状态重置 */
        public void Reset()
        {
            SampleCount = 0U;
            m_sumVal = 0.0;
            m_slidingWindowList?.Clear();
        }

        /* 数据与平均值的差值 */
        public double Delta(double data)
        {
            return data - AvgVal;
        }
    }
}
