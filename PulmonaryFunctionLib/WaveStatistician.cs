
namespace PulmonaryFunctionLib
{
    /* 波动数据统计器 */
    class WaveStatistician
    {
        public double MaxVal { get; private set; } = double.MinValue; // 波动最大值
        public double MinVal { get; private set; } = double.MaxValue; // 波动最小值
        public double AvgVal { get { return (SampleCount > 0) ? (m_sumVal / SampleCount) : 0; } } // 波动平均值
        public double Range { get { return (MaxVal > MinVal) ? (MaxVal - MinVal) : 0; } } // 波动范围
        public uint SampleCount { get; private set; } = 0U; // 采样次数

        private double m_sumVal = 0.0; // 求和值

        public WaveStatistician(double minVal = double.MaxValue, double maxVal = double.MinValue)
        {
            MaxVal = maxVal;
            MinVal = minVal;
            SampleCount = 0U;
            m_sumVal = 0.0;
        }

        /* 输入数据 */
        public void Input(double data)
        {
            /* 统计最值 */
            if (data < MinVal)
            {
                MinVal = data;
            }
            if (data > MaxVal)
            {
                MaxVal = data;
            }
            /* 累加采样次数 */
            ++SampleCount;
            /* 累加和值 */
            m_sumVal += data;
        }

        /* 重置 */
        public void Reset()
        {
            MaxVal = double.MinValue;
            MinVal = double.MaxValue;
            SampleCount = 0U;
            m_sumVal = 0.0;
        }

        /* 检查数据是否在波动范围内 */
        public bool Check(double data)
        {
            return (MinVal < data) && (data < MaxVal);
        }
    }
}
