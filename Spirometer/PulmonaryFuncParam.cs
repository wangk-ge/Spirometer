using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spirometer
{
    /* 肺功能参数计算 */
    class PulmonaryFuncParam
    {
        private double m_minFlow = double.MaxValue; // 跟踪最小流量值
        private double m_maxFlow = double.MinValue; // 跟踪最大流量值
        private readonly double m_rangeThreshold = 0.05; // 波动范围阈值,超过阈值则认为达到起始条件

        public enum State { Stop, Start }
        public State m_state = State.Stop;

        public delegate void StateChangeHandler(State state, double time); // 状态改变事件代理
        public event StateChangeHandler StateChanged; // 状态改变事件

        public PulmonaryFuncParam()
        {

        }

        /* 状态重置 */
        public void Reset()
        {
            m_state = State.Stop;
            m_minFlow = double.MaxValue;
            m_maxFlow = double.MinValue;
        }

        /* 输入流量数据 */
        public void Input(double flow, double time)
        {
            /* 统计最值 */
            if (flow < m_minFlow)
            {
                m_minFlow = flow;
            }
            if (flow > m_maxFlow)
            {
                m_maxFlow = flow;
            }

            switch (m_state)
            {
                case State.Stop:
                    {
                        /* 检测开始条件 */
                        double range = m_maxFlow - m_minFlow;
                        if (range > m_rangeThreshold)
                        { // 波动范围超过阈值
                            m_state = State.Start; // 变为启动状态
                            StateChanged?.Invoke(m_state, time); // 触发状态改变事件
                        }
                    
                        break;
                    }
                case State.Start:
                    {
                        /* 检测停止条件 */
                        // TODO

                        break;
                    }
            }
        }
    }
}
