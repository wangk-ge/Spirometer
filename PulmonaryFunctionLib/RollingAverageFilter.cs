
using System.Collections.Generic;

namespace PulmonaryFunctionLib
{
	/* 滑动平均滤波器 */
    class RollingAverageFilter
    {
        private readonly int WINDOW_SIZE; // 滑动窗口大小
        private double[] m_filterWindowQue; // 滑动窗口队列
        private int m_queHead = 0; // 队列头
        private int m_queTail = 0; // 队列尾
        private int m_queLen = 0; // 队列长度

        private double m_dataSum = 0.0; // 数据求和

        public RollingAverageFilter(int windowSize)
        {
			WINDOW_SIZE = windowSize;
            m_filterWindowQue = new double[WINDOW_SIZE];
            ClearQue();
            m_dataSum = 0.0;
        }

        /* 清空队列 */
        private void ClearQue()
        {
            m_queHead = 0;
            m_queTail = 0;
            m_queLen = 0;
        }

        /* 队列长度 */
        private int QueLen()
        {
            return m_queLen;
        }

        /* 入队 */
        private void Enqueue(double data)
        {
            if (m_queLen >= m_filterWindowQue.Length)
            { // 已满
                return;
            }

            m_filterWindowQue[m_queHead] = data;
            ++m_queHead;
            if (m_queHead >= m_filterWindowQue.Length)
            {
                m_queHead = 0;
            }
            ++m_queLen;
        }

        /* 出队 */
        private double Dequeue()
        {
            if (m_queLen <= 0)
            { // 已空
                return 0.0;
            }

            double data = m_filterWindowQue[m_queTail];
            ++m_queTail;
            if (m_queTail >= m_filterWindowQue.Length)
            {
                m_queTail = 0;
            }
            --m_queLen;

            return data;
        }

        /* 重置滤波器 */
        public void Reset()
        {
            ClearQue();
            m_dataSum = 0.0;
        }

        /* 执行滤波 */
        public double Input(double val)
        {
            /* 更新滑动窗口 */
            if (QueLen() >= WINDOW_SIZE)
            {
                double data = Dequeue();
                m_dataSum -= data;
            }
            Enqueue(val);
            m_dataSum += val;

            /* 计算窗口内样本的平均值 */
            double avg = m_dataSum / QueLen();

            return avg;
        }
    }
}
