
using System.Collections.Generic;

namespace PulmonaryFunctionLib
{
	/* 滑动平均滤波器 */
    class RollingAverageFilter
    {
        private readonly int WINDOW_SIZE; // 滑动窗口大小
        private Queue<double> m_filterWindowQue;

        public RollingAverageFilter(int windowSize)
        {
			WINDOW_SIZE = windowSize;
            m_filterWindowQue = new Queue<double>(WINDOW_SIZE);
        }

        /* 执行滤波 */
        public double Input(double val)
        {
            /* 更新滑动窗口 */
            if (m_filterWindowQue.Count >= WINDOW_SIZE)
            {
                m_filterWindowQue.Dequeue();
            }
            m_filterWindowQue.Enqueue(val);

            /* 计算窗口内样本的平均值 */
            double sum = 0.0;
            foreach (var data in m_filterWindowQue)
            {
                sum += data;
            }
            double avg = sum / m_filterWindowQue.Count;

            return avg;
        }
    }
}
