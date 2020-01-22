using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double.Solvers;
using MathNet.Numerics.LinearAlgebra.Solvers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulmonaryFunctionLib
{
    /* 流量校准器 */
    public class FlowCalibrator
    {
        public double CalVolume { get; private set; } = 0.0; // 定标桶容积(单位: L)

        /* 实时Presure数据属性 */
        public double Time { get { return (m_listPresure.Count > 0) ? GetTime((uint)(m_listPresure.Count - 1)) : 0.0; } } // 当前采样时间点(ms)
        public double Presure { get { return (m_listPresure.Count > 0) ? m_listPresure[m_listPresure.Count - 1] : 0.0; } } // 当前最新采集的Presure值
        public uint PresureCount { get { return (uint)m_listPresure.Count; } } // 当前已采集的所有(Presure数据)总个数

        /* 最新采集样本的属性 */
        public double SamplePresureSum { get; private set; } = 0.0; // 最新采集样本的Presure求和值
        public double SamplePresureAvg { get; private set; } = 0.0; // 最新采集样本的Presure平均值
        public double SamplePresureVariance { get; private set; } = 0.0; // 最新采集样本的Presure方差(代表Presure的离散程度,越小表示越集中)
        public double SamplePeekPresure { get { return (m_peekPresureIndex < m_listPresure.Count) ? m_listPresure[(int)m_peekPresureIndex] : 0.0; } } // 最新采集样本的Presure极值
        public double SamplePresureAvgToFlowScale { get { return (SamplePresureSum != 0) ? ((SAMPLE_RATE * CalVolume) / Math.Abs(SamplePresureSum)) : 0.0; } } // 最新采集样本的Presure转换成Flow的比例系数(均值)
        public bool SampleIsValid { get { return (Math.Abs(SamplePresureSum) > (40 * CalVolume)); } } // 最新采集样本是否有效(自动判断校准结果有效性)

        /* 代理/事件 */
        public delegate void InspirationStartHandler(uint sampleIndex); // 吸气开始事件代理
        public event InspirationStartHandler InspirationStarted; // 吸气开始事件
        public delegate void ExpirationStartHandler(uint sampleIndex); // 呼气开始事件代理
        public event ExpirationStartHandler ExpirationStarted; // 呼气开始事件
        public delegate void MeasureStopHandler(uint sampleIndex, uint peekPresureIndex); // 测试停止事件代理
        public event MeasureStopHandler MeasureStoped; // 测试停止事件

        /* 私有成员 */
        private List<double> m_listPresure = new List<double>(); // 压差数据列表

        /* 样本信息类型(一个样本为一起起始和结束之间的所有压差数据) */
        private struct SampleListInfo
        {
            public uint startIndex; // 起始下标
            public uint endIndex; // 结束下标
            public double sum; // 和值（为正表示吸气/为负表示呼气）
        }
        private List<SampleListInfo> m_sampleInfoList = new List<SampleListInfo>(); // 样本信息列表

        /* 状态类型 */
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
        private WaveStatistician m_waveStatistician = new WaveStatistician(100); // 用于统计波动数据
        private readonly int START_SAMPLE_COUNT = 2; // 启动检测,波动统计采样次数
        private readonly int STOP_SAMPLE_COUNT = 20; // 停止检测,波动统计采样次数
        private readonly double START_PRESURE_DELTA = 0.0015; // 斜度绝对值超过该阈值将识别为启动测试(斜度为正表示吸气启动、为负表示吹气开始)
        private readonly double STOP_PRESURE_THRESHOLD = 0.002; // 停止检测压差阈值,当启动测试后如果压差绝对值小于阈值,则开始检测停止条件

        private uint m_peekPresureIndex = 0U; // 跟踪峰值压差点Index

        /* 测试启动/结束点 */
        private uint m_startIndex = 0U; // 测试启动点Index
        private uint m_endIndex = 0U; // 测试结束点Index

        private double m_minPresaure = double.MaxValue; // 整个测量范围内的最小值(考察所有样本)
        private double m_maxPresaure = double.MinValue; // 整个测量范围内的最大值(考察所有样本)

        public FlowCalibrator(double sampleRate, double calVolume = 1.0)
        {
            SAMPLE_RATE = sampleRate;
            SAMPLE_TIME = 1000 / sampleRate;
            CalVolume = calVolume;
        }

        /* 状态重置 */
        public void Reset()
        {
            SamplePresureSum = 0.0;
            SamplePresureAvg = 0.0;
            SamplePresureVariance = 0.0;
            //m_listPresure.Clear();
            m_state = State.Reset;
            m_waveStatistician.Reset();
            m_peekPresureIndex = 0U;
            m_startIndex = 0U;
            m_endIndex = 0U;
            //m_sampleListInfos.Clear();
            //m_minPresaure = double.MaxValue;
            //m_maxPresaure = double.MinValue;
        }

        /* 清除 */
        public void Clear()
        {
            Reset();
            m_listPresure.Clear();
            m_sampleInfoList.Clear();
            m_minPresaure = double.MaxValue;
            m_maxPresaure = double.MinValue;
        }

        /* 计算Presure数据均值 */
        private double CalcPresureAvg()
        {
            if (m_endIndex <= m_startIndex)
            {
                return 0.0;
            }

            uint n = m_endIndex - m_startIndex;
            double avg = SamplePresureSum / n;
            return avg;
        }

        /* 计算Presure数据方差值 */
        private double CalcPresureVariance()
        {
            double varianceSum = 0.0;
            uint i = 0;
            for (i = m_startIndex; i < m_endIndex; ++i)
            {
                double d = m_listPresure[(int)i] - SamplePresureAvg;
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
            /* 最新Presure数据索引值 */
            uint sampleIndex = (uint)m_listPresure.Count;

            /* 记录Presure数据到队列 */
            m_listPresure.Add(presure);

            if ((m_state > State.WaitStart)
                && (m_state < State.Stop))
            {
                /* 统计和值 */
                SamplePresureSum += presure;

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
                            m_endIndex = sampleIndex;

                            /* 统计最大/最小值 */
                            if (presure < m_minPresaure)
                            {
                                m_minPresaure = presure;
                            }
                            if (presure > m_maxPresaure)
                            {
                                m_maxPresaure = presure;
                            }

                            /* 进入测试停止状态 */
                            SetState(State.Stop);

                            /* 计算均值 */
                            SamplePresureAvg = CalcPresureAvg();

                            /* 计算方差 */
                            SamplePresureVariance = CalcPresureVariance();

                            /* 如果当前样本有效 */
                            if (SampleIsValid)
                            {
                                /* 记录当前样本信息 */
                                SampleListInfo info = new SampleListInfo() { startIndex = m_startIndex, endIndex = m_endIndex, sum = SamplePresureSum };
                                m_sampleInfoList.Add(info);
                            }

                            /* 触发测量结束事件 */
                            MeasureStoped?.Invoke(m_endIndex, m_peekPresureIndex);
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
                                /* 记录启动测试点Index */
                                m_startIndex = sampleIndex - 1;

                                /* 初始化和值 */
                                SamplePresureSum = (m_listPresure[(int)m_startIndex] + presure);

                                /* 重置波动统计器 */
                                m_waveStatistician.Reset();

                                /* 记录启动类型(是否吸气启动) */
                                if (delta > START_PRESURE_DELTA)
                                { // 吸气开始
                                    /* 进入正在吸气 */
                                    SetState(State.Inspiration);

                                    /* 触发吸气开始事件 */
                                    InspirationStarted?.Invoke(m_startIndex);
                                }
                                else
                                { // 呼气开始
                                    /* 进入正在呼气状态 */
                                    SetState(State.Expiration);

                                    /* 触发呼气开始事件 */
                                    ExpirationStarted?.Invoke(m_startIndex);
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

                        /* 统计最大/最小值 */
                        if (presure < m_minPresaure)
                        {
                            m_minPresaure = presure;
                        }
                        if (presure > m_maxPresaure)
                        {
                            m_maxPresaure = presure;
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

                        /* 统计最大/最小值 */
                        if (presure < m_minPresaure)
                        {
                            m_minPresaure = presure;
                        }
                        if (presure > m_maxPresaure)
                        {
                            m_maxPresaure = presure;
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

        /* 计算分段数 */
        private uint CalcSectionNum(double sectionStep)
        {
            /* 负方向分段数 */
            uint sectionNumN = (uint)((0 - m_minPresaure) / sectionStep) + 1;

            /* 正方向分段数 */
            uint sectionNumP = (uint)((m_maxPresaure - 0) / sectionStep) + 1;

            /* 总分段数 */
            uint sectionNum = sectionNumN + sectionNumP;

            return sectionNum;
        }

        /* 计算校准参数(校准参数列表通过函数参数返回) */
        public bool CalcCalibrationParams(List<double> sectionKeyList, List<double> paramValList)
        {
            /* 自动分段步进长度 */
            double sectionStep = 0.1;

            /* 计算分段数量 */
            uint sectionNum = CalcSectionNum(sectionStep);
            
            /* 样本数必须大于参数个数(分段个数) */
            if (m_sampleInfoList.Count <= sectionNum)
            {
                return false;
            }

            /* 确保结果列表清空状态 */
            sectionKeyList.Clear();
            paramValList.Clear();

            /* 进行负方向分段 */
            double sectionKey = 0;
            for (; sectionKey > m_minPresaure; sectionKey -= sectionStep)
            {
                sectionKeyList.Add(sectionKey);
            }
            /* 进行正方向分段 */
            sectionKey = sectionStep;
            for (; sectionKey < m_maxPresaure; sectionKey += sectionStep)
            {
                sectionKeyList.Add(sectionKey);
            }
            sectionKeyList.Add(m_maxPresaure);

            /* 确保分段算法无误 */
            Trace.Assert(sectionNum == sectionKeyList.Count);

            /* 分段Key按升序排序 */
            sectionKeyList.Sort();

            /* 构造方程组参数矩阵 */
            double[][] matrixA = new double[m_sampleInfoList.Count][];
            double[] vectorB = new double[matrixA.GetLength(0)];
            for (int infoIndex = 0; infoIndex < m_sampleInfoList.Count; ++infoIndex)
            {
                SampleListInfo info = m_sampleInfoList[infoIndex];
                int startIndex = (int)info.startIndex;
                int endIndex = (int)info.endIndex;
                
                for (int i = startIndex; i <= endIndex; ++i)
                {
                    double presure = m_listPresure[i];
                    /* 找到所属分段Index */
                    int sectionIndex = sectionKeyList.BinarySearch(presure);
                    if (sectionIndex < 0)
                    {
                        sectionIndex = ~sectionIndex;
                    }
                    
                    if (info.sum > 0)
                    {
                        vectorB[infoIndex] = CalVolume * SAMPLE_RATE;
                    }
                    else
                    {
                        vectorB[infoIndex] = -CalVolume * SAMPLE_RATE;
                    }
                    if (null == matrixA[infoIndex])
                    {
                        matrixA[infoIndex] = new double[sectionKeyList.Count];
                    }
                    matrixA[infoIndex][sectionIndex] += presure; // 累加到对应分段
                }
            }

            bool bRet = false;
            try
            {
                /* 使用多元线性最小二乘法（linear least squares）拟合最优参数集 */
                double[] result = Fit.MultiDim(matrixA, vectorB, false, MathNet.Numerics.LinearRegression.DirectRegressionMethod.Svd);

                paramValList.AddRange(result);
                for (int i = 0; i < paramValList.Count; ++i)
                {
                    Console.WriteLine($"{sectionKeyList[i]}, {paramValList[i]}");
                }

                bRet = true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception: {e.Message}");

                sectionKeyList.Clear();
                paramValList.Clear();
                bRet = false;
            }

            return bRet;
        }
    }
}
