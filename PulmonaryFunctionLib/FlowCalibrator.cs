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

        /* 实时更新属性 */
        public double Time { get { return (m_listPresure.Count > 0) ? GetTime((uint)(m_listPresure.Count - 1)) : 0.0; } } // 当前采样时间点(ms)
        public double Presure { get { return (m_listPresure.Count > 0) ? m_listPresure[m_listPresure.Count - 1] : 0.0; } } // 当前最新采集的Presure值
        public uint PresureCount { get { return (uint)m_listPresure.Count; } } // 当前已采集的所有(Presure数据)总个数
        public uint SampleCount { get { return (uint)m_sampleInfoList.Count; } } // 已采集的样本个数
        public double CurrSamplePresureSum { get { return m_presureSum; } } // 当前正在采集的样本Presure求和值

        /* 呼吸方向类型 */
        public enum RespireDirection
        {
            None = 0,
            Inspiration, // 吸气
            Expiration, // 呼气
        }
        /* 代理/事件 */
        public delegate void SampleStartHandler(uint presureIndex, RespireDirection direction); // 采样开始事件代理
        public event SampleStartHandler SampleStarted; // 采样开始事件
        public delegate void SampleStopHandler(uint presureIndex, RespireDirection direction, uint sampleIndex); // 采样停止事件代理
        public event SampleStopHandler SampleStoped; // 采样停止事件

        /* Presure数据列表 */
        private List<double> m_listPresure = new List<double>(); // Presure数据列表

        /* 样本信息类型(一个样本为一起起始和结束之间的所有压差数据) */
        public struct SampleListInfo
        {
            public RespireDirection direction; // 呼吸方向
            public uint startIndex; // 起始点下标
            public uint endIndex; // 结束点下标
            public double minIndex; // 最小值点下标
            public double maxIndex; // 最大值点下标
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

        /* 状态变量和常量 */
        private State m_state = State.Reset; // 工作状态
        private readonly double SAMPLE_TIME = 3.0; // 采样时间(ms)
        private readonly double SAMPLE_RATE = 330; // 采样率
        private WaveStatistician m_waveStatistician = new WaveStatistician(100); // 用于统计波动数据
        private readonly int START_SAMPLE_COUNT = 2; // 启动检测,波动统计采样次数
        private readonly int STOP_SAMPLE_COUNT = 20; // 停止检测,波动统计采样次数
        private readonly double START_PRESURE_DELTA = 0.0015; // 斜度绝对值超过该阈值将识别为启动测试(斜度为正表示吸气启动、为负表示吹气开始)
        private readonly double STOP_PRESURE_THRESHOLD = 0.002; // 停止检测压差阈值,当启动测试后如果压差绝对值小于阈值,则开始检测停止条件

        /* 当前样本特征点 */
        private uint m_startIndex = 0U; // 当前样本采集启动点Index
        private uint m_endIndex = 0U; // 当前样本采集结束点Index
        private uint m_minPresureIndex = 0U; // 当前样本最小值点Index
        private uint m_maxPresureIndex = 0U; // 当前样本最大值点Index
        private double m_presureSum = 0.0; // 当前样本Presure求和值

        public FlowCalibrator(double sampleRate, double calVolume = 1.0)
        {
            SAMPLE_RATE = sampleRate;
            SAMPLE_TIME = 1000 / sampleRate;
            CalVolume = calVolume;
        }

        /* 状态重置 */
        public void Reset()
        {
            //m_listPresure.Clear();
            m_state = State.Reset;
            m_waveStatistician.Reset();
            m_startIndex = 0U;
            m_endIndex = 0U;
            //m_sampleListInfos.Clear();
            m_minPresureIndex = 0U;
            m_maxPresureIndex = 0U;
            m_presureSum = 0.0;
        }

        /* 清除 */
        public void Clear()
        {
            Reset();
            m_listPresure.Clear();
            m_sampleInfoList.Clear();
        }

        /* 样本的Presure求和值 */
        public double SamplePresureSum(SampleListInfo info)
        {
            return info.sum;
        }

        /* 样本的Presure求和值(通过样本索引) */
        public double SamplePresureSum(uint sampleIndex)
        {
            if (sampleIndex >= m_sampleInfoList.Count)
            {
                return 0.0;
            }
            var info = m_sampleInfoList[(int)sampleIndex];
            return SamplePresureSum(info);
        }

        /* 样本的Presure最小值 */
        public double SampleMinPresure(SampleListInfo info)
        {
            if (info.minIndex >= m_listPresure.Count)
            {
                return 0.0;
            }
            return m_listPresure[(int)info.minIndex];
        }

        /* 样本的Presure最小值(通过样本索引) */
        public double SampleMinPresure(uint sampleIndex)
        {
            if (sampleIndex >= m_sampleInfoList.Count)
            {
                return 0.0;
            }
            var info = m_sampleInfoList[(int)sampleIndex];
            return SampleMinPresure(info);
        }

        /* 样本的Presure最大值 */
        public double SampleMaxPresure(SampleListInfo info)
        {
            if (info.maxIndex >= m_listPresure.Count)
            {
                return 0.0;
            }
            return m_listPresure[(int)info.maxIndex];
        }

        /* 样本的Presure最大值(通过样本索引) */
        public double SampleMaxPresure(uint sampleIndex)
        {
            if (sampleIndex >= m_sampleInfoList.Count)
            {
                return 0.0;
            }
            var info = m_sampleInfoList[(int)sampleIndex];
            return SampleMaxPresure(info);
        }

        /* 样本的Presure个数 */
        public uint SamplePresureCount(SampleListInfo info)
        {
            if (info.startIndex >= info.endIndex)
            {
                return 0U;
            }
            uint presureNum = info.endIndex - info.startIndex + 1;
            return presureNum;
        }

        /* 样本的Presure个数(通过样本索引) */
        public uint SamplePresureCount(uint sampleIndex)
        {
            if (sampleIndex >= m_sampleInfoList.Count)
            {
                return 0U;
            }
            var info = m_sampleInfoList[(int)sampleIndex];
            return SamplePresureCount(info);
        }

        /* 样本的Presure平均值 */
        public double SamplePresureAvg(SampleListInfo info)
        {
            uint presureNum = SamplePresureCount(info);
            if (presureNum <= 0)
            {
                return 0.0;
            }
            return info.sum / presureNum;
        }

        /* 样本的Presure平均值(通过样本索引) */
        public double SamplePresureAvg(uint sampleIndex)
        {
            if (sampleIndex >= m_sampleInfoList.Count)
            {
                return 0.0;
            }
            var info = m_sampleInfoList[(int)sampleIndex];
            return SamplePresureAvg(info);
        }

        /* 样本的Presure方差(代表Presure的离散程度,越小表示越集中) */
        public double SamplePresureVariance(SampleListInfo info)
        {
            /* 取得样本均值 */
            double sampleAvg = SamplePresureAvg(info);

            /* 计算方差 */
            double varianceSum = 0.0;
            uint i = 0;
            for (i = info.startIndex; i < info.endIndex; ++i)
            {
                double d = m_listPresure[(int)i] - sampleAvg;
                varianceSum += (d * d);
            }
            double variance = 0.0;
            if (i > 0)
            {
                variance = varianceSum / i;
            }
            return variance;
        }

        /* 样本的Presure方差(代表Presure的离散程度,越小表示越集中)(通过样本索引) */
        public double SamplePresureVariance(uint sampleIndex)
        {
            if (sampleIndex >= m_sampleInfoList.Count)
            {
                return 0.0;
            }
            var info = m_sampleInfoList[(int)sampleIndex];
            double variance = SamplePresureVariance(info);
            return variance;
        }

        /* 样本的Presure转换成Flow的比例系数(均值) */
        public double SamplePresureAvgToFlowScale(SampleListInfo info)
        {
            double absSum = Math.Abs(info.sum);
            if (absSum < 0.00000001)
            {
                return 0.0;
            }
            return (SAMPLE_RATE * CalVolume) / absSum;
        }

        /* 样本的Presure转换成Flow的比例系数(均值)(通过样本索引) */
        public double SamplePresureAvgToFlowScale(uint sampleIndex)
        {
            if (sampleIndex >= m_sampleInfoList.Count)
            {
                return 0.0;
            }
            var info = m_sampleInfoList[(int)sampleIndex];
            return SamplePresureAvgToFlowScale(info);
        }

        /* 样本是否有效(自动判断校准结果有效性) */
        public bool SampleIsValid(SampleListInfo info)
        {
            if (info.startIndex >= info.endIndex)
            {
                return false;
            }

            return (Math.Abs(info.sum) > (40 * CalVolume));
        }

        /* 样本是否有效(自动判断校准结果有效性)(通过样本索引) */
        public bool SampleIsValid(uint sampleIndex)
        {
            if (sampleIndex >= m_sampleInfoList.Count)
            {
                return false;
            }
            
            var info = m_sampleInfoList[(int)sampleIndex];
            return SampleIsValid(info);
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
            uint presureIndex = (uint)m_listPresure.Count;

            /* 记录Presure数据到队列 */
            m_listPresure.Add(presure);

            if ((m_state > State.WaitStart)
                && (m_state < State.Stop))
            {
                /* 统计和值 */
                m_presureSum += presure;

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
                            m_endIndex = presureIndex;

                            /* 统计最大/最小值点 */
                            /* 统计最大/最小值点 */
                            if (presure < m_listPresure[(int)m_minPresureIndex])
                            {
                                m_minPresureIndex = presureIndex;
                            }
                            if (presure > m_listPresure[(int)m_maxPresureIndex])
                            {
                                m_maxPresureIndex = presureIndex;
                            }

                            /* 呼吸方向 */
                            RespireDirection direction = (m_state == State.Inspiration) ? RespireDirection.Inspiration : RespireDirection.Expiration;

                            /* 进入测试停止状态 */
                            SetState(State.Stop);

                            /* 记录当前样本信息 */
                            SampleListInfo info = new SampleListInfo() {
                                direction = direction,
                                startIndex = m_startIndex, 
                                endIndex = m_endIndex, 
                                minIndex = m_minPresureIndex, 
                                maxIndex = m_maxPresureIndex,
                                sum = m_presureSum
                            };
                            m_sampleInfoList.Add(info);

                            /* 触发测量结束事件 */
                            uint sampleIndex = (uint)(m_sampleInfoList.Count - 1);
                            SampleStoped?.Invoke(m_endIndex, direction, sampleIndex);
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
                                m_startIndex = presureIndex - 1;

                                /* 初始化最小/最大值点 */
                                m_minPresureIndex = m_startIndex;
                                m_maxPresureIndex = m_startIndex;
                                if (presure < m_listPresure[(int)m_minPresureIndex])
                                {
                                    m_minPresureIndex = presureIndex;
                                }
                                if (presure > m_listPresure[(int)m_maxPresureIndex])
                                {
                                    m_maxPresureIndex = presureIndex;
                                }

                                /* 初始化和值 */
                                m_presureSum = (m_listPresure[(int)m_startIndex] + presure);

                                /* 重置波动统计器 */
                                m_waveStatistician.Reset();

                                /* 记录启动类型(是否吸气启动) */
                                if (delta > START_PRESURE_DELTA)
                                { // 吸气开始
                                    /* 进入正在吸气 */
                                    SetState(State.Inspiration);

                                    /* 呼吸方向(吸气) */
                                    RespireDirection direction = RespireDirection.Inspiration;

                                    /* 触发采样(吸气)开始事件 */
                                    SampleStarted?.Invoke(m_startIndex, direction);
                                }
                                else
                                { // 呼气开始
                                    /* 进入正在呼气状态 */
                                    SetState(State.Expiration);

                                    /* 呼吸方向(呼气) */
                                    RespireDirection direction = RespireDirection.Expiration;

                                    /* 触发采样(呼气)开始事件 */
                                    SampleStarted?.Invoke(m_startIndex, direction);
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
                        /* 统计最大/最小值点 */
                        if (presure < m_listPresure[(int)m_minPresureIndex])
                        {
                            m_minPresureIndex = presureIndex;
                        }
                        if (presure > m_listPresure[(int)m_maxPresureIndex])
                        {
                            m_maxPresureIndex = presureIndex;
                        }
                        break;
                    }
                case State.Expiration: // 正在呼气状态
                    {
                        /* 统计最大/最小值点 */
                        if (presure < m_listPresure[(int)m_minPresureIndex])
                        {
                            m_minPresureIndex = presureIndex;
                        }
                        if (presure > m_listPresure[(int)m_maxPresureIndex])
                        {
                            m_maxPresureIndex = presureIndex;
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
        public double GetTime(uint presureIendex)
        {
            return presureIendex * SAMPLE_TIME;
        }

        /* 自动对Presure进行分段 */
        private uint AutoDividePresureSections(double minPresure, double maxPresure, List<double> presureSectionList)
        {
            /* 先清空 */
            presureSectionList.Clear();

            /* 分段步进长度 */
            double sectionStep1 = 0.1;
            double sectionStep2 = 0.15;

            /* 进行负方向分段 */
            double sectionPos = 0.0; // [0,-0.5)
            for (; sectionPos > Math.Max(minPresure + sectionStep1, -0.5); sectionPos -= sectionStep1)
            {
                presureSectionList.Add(sectionPos);
            }
            if (sectionPos > (minPresure + sectionStep1))
            {
                sectionPos = -0.5; // [-0.5, min)
                for (; sectionPos > (minPresure + sectionStep2); sectionPos -= sectionStep2)
                {
                    presureSectionList.Add(sectionPos);
                }
            }
            /* 进行正方向分段 */
            sectionPos = sectionStep1; // (0,0.5)
            for (; sectionPos < Math.Min(maxPresure - sectionStep1, 0.5); sectionPos += sectionStep1)
            {
                presureSectionList.Add(sectionPos);
            }
            if (sectionPos < (maxPresure - sectionStep1))
            {
                sectionPos = 0.5; // [0.5,max)
                for (; sectionPos < (maxPresure - sectionStep2); sectionPos += sectionStep2)
                {
                    presureSectionList.Add(sectionPos);
                }
            }
            presureSectionList.Add(maxPresure);

            /* 分段Key按升序排序 */
            presureSectionList.Sort();

            return (uint)presureSectionList.Count;
        }

        /* 计算校准参数(校准参数列表通过函数参数返回) */
        public bool CalcCalibrationParams(List<double> sectionKeyList, List<double> paramValList, List<uint> sampleIndexList = null)
        {
            /* 是否指定了样本列表 */
            if (sampleIndexList == null)
            {
                /* 自动选择样本进行计算 */
                sampleIndexList = new List<uint>();
                for (int i = 0; i < m_sampleInfoList.Count; ++i)
                {
                    var info = m_sampleInfoList[i];
                    /* 只选择有效的样本 */
                    if (SampleIsValid(info))
                    {
                        /* 加入选择列表 */
                        sampleIndexList.Add((uint)i);
                    }
                }
            }
            // else /* 用已选择的的样本进行计算 */

            /* 全局Presure最大/最小值(所有已选样本范围内) */
            double minPresureInAll = double.MaxValue;
            double maxPresureInAll = double.MinValue;

            /* 统计全局最大/最小值(所有已选样本范围内) */
            foreach (var infoIndex in sampleIndexList)
            {
                SampleListInfo info = m_sampleInfoList[(int)infoIndex];
                
                double minPresureInSample = m_listPresure[(int)info.minIndex];
                if (minPresureInSample < minPresureInAll)
                {
                    minPresureInAll = minPresureInSample;
                }
                double maxPresureInSample = m_listPresure[(int)info.maxIndex];
                if (maxPresureInSample > maxPresureInAll)
                {
                    maxPresureInAll = maxPresureInSample;
                }
            }

            /* 样本数量 */
            uint sampleNum = (uint)sampleIndexList.Count;

            /* 确保结果列表清空状态 */
            sectionKeyList.Clear();
            paramValList.Clear();

            /* 自动对Presure进行分段 */
            uint sectionNum = AutoDividePresureSections(minPresureInAll, maxPresureInAll, sectionKeyList);
            /* 样本数必须大于参数个数(分段个数) */
            if (sampleNum <= sectionNum)
            {
                return false;
            }

            /* 构造方程组参数矩阵 */
            double[][] matrixA = new double[sampleNum][];
            double[] vectorB = new double[sampleNum];
            for (int index = 0; index < sampleNum; ++index)
            {
                var infoIndex = sampleIndexList[index];
                SampleListInfo info = m_sampleInfoList[(int)infoIndex];
                int startIndex = (int)info.startIndex;
                int endIndex = (int)info.endIndex;
                
                for (int presureIndex = startIndex; presureIndex <= endIndex; ++presureIndex)
                {
                    double presure = m_listPresure[presureIndex];
                    /* 找到所属分段Index */
                    int sectionIndex = sectionKeyList.BinarySearch(presure);
                    if (sectionIndex < 0)
                    {
                        sectionIndex = ~sectionIndex;
                    }
                    
                    if (info.sum > 0)
                    {
                        vectorB[index] = CalVolume * SAMPLE_RATE;
                    }
                    else
                    {
                        vectorB[index] = -CalVolume * SAMPLE_RATE;
                    }
                    if (null == matrixA[index])
                    {
                        matrixA[index] = new double[sectionNum];
                    }
                    matrixA[index][sectionIndex] += presure; // 累加到对应分段
                }
            }

            bool bRet = false;
            try
            {
                /* 使用多元线性最小二乘法（linear least squares）拟合最优参数集 */
                double[] result = Fit.MultiDim(matrixA, vectorB, false, MathNet.Numerics.LinearRegression.DirectRegressionMethod.Svd);

                paramValList.AddRange(result);
                //for (int i = 0; i < paramValList.Count; ++i)
                //{
                //    Console.WriteLine($"{sectionKeyList[i]}, {paramValList[i]}");
                //}

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
