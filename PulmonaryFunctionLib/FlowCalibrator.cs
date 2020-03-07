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
        public double Time { get { return m_waveAnalyzer.Time; } } // 当前采样时间点(ms)
        public double Presure { get { return m_waveAnalyzer.Data; } } // 当前最新采集的Presure值
        public uint PresureCount { get { return m_waveAnalyzer.DataCount; } } // 当前已采集的所有(Presure数据)总个数
        public uint SampleCount { get { return m_waveAnalyzer.SampleCount; } } // 已采集的样本个数
        public double CurrSamplePresureSum { get { return m_waveAnalyzer.CurrSampleDataSum; } } // 当前正在采集的样本Presure求和值

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

        /* 波形数据分析器 */
        private WaveAnalyzer m_waveAnalyzer;

        /* 参数 */
        private readonly double SAMPLE_TIME = 3.03; // 采样时间(ms)
        private readonly double SAMPLE_RATE = 330; // 采样率

        /* 阈值(用于检测采样启动和停止条件) */
        private readonly int START_SAMPLE_COUNT = 2; // 启动检测,波动统计采样次数
        private readonly int STOP_SAMPLE_COUNT = 20; // 停止检测,波动统计采样次数
        private readonly double START_PRESURE_DELTA = 0.0015; // 斜度绝对值超过该阈值将识别为启动测试(斜度为正表示吸气启动、为负表示吹气开始)
        private readonly double STOP_PRESURE_THRESHOLD = 0.002; // 停止检测压差阈值,当启动测试后如果压差绝对值小于阈值,则开始检测停止条件

        public FlowCalibrator(double sampleRate, double calVolume = 1.0)
        {
            SAMPLE_RATE = sampleRate;
            SAMPLE_TIME = 1000 / sampleRate;

            m_waveAnalyzer = new WaveAnalyzer(sampleRate,
                START_SAMPLE_COUNT, START_PRESURE_DELTA,
                STOP_SAMPLE_COUNT, STOP_PRESURE_THRESHOLD);

            CalVolume = calVolume;

            m_waveAnalyzer.SampleStarted += (uint dataIndex, WaveSampleDirection direction) =>
            {
                /* 转换为呼吸方向 */
                RespireDirection respireDir = ToRespireDirection(direction);
                SampleStarted?.Invoke(dataIndex, respireDir);
            };

            m_waveAnalyzer.SampleStoped += (uint dataIndex, WaveSampleDirection direction, uint sampleIndex) =>
            {
                /* 转换为呼吸方向 */
                RespireDirection respireDir = ToRespireDirection(direction);
                SampleStoped?.Invoke(dataIndex, respireDir, sampleIndex);
            };
        }

        /* 状态重置 */
        public void Reset()
        {
            m_waveAnalyzer.Reset();
        }

        /* 清除 */
        public void Clear()
        {
            m_waveAnalyzer.Clear();
        }

        /* 转换为呼吸方向 */
        private static RespireDirection ToRespireDirection(WaveSampleDirection waveDir)
        {
            RespireDirection respireDir = RespireDirection.None;

            switch (waveDir)
            {
                case WaveSampleDirection.PositiveSample: // 正方向样本
                    respireDir = RespireDirection.Inspiration; // 吸气
                    break;
                case WaveSampleDirection.NegativeSample: // 负方向样本
                    respireDir = RespireDirection.Expiration; // 呼气
                    break;
                default:
                    break;
            }

            return respireDir;
        }

        /* 样本的呼吸方向 */
        public RespireDirection SamplePresureDirection(uint sampleIndex)
        {
            /* 取得样本方向 */
            WaveSampleDirection waveDir = m_waveAnalyzer.SampleDataDirection(sampleIndex);

            /* 转换为呼吸方向 */
            RespireDirection respireDir = ToRespireDirection(waveDir);

            return respireDir;
        }

        /* 样本的Presure求和值 */
        public double SamplePresureSum(uint sampleIndex)
        {
            return m_waveAnalyzer.SampleDataSum(sampleIndex);
        }

        /* 样本的Presure最小值 */
        public double SampleMinPresure(uint sampleIndex)
        {
            return m_waveAnalyzer.SampleMinData(sampleIndex);
        }

        /* 样本的Presure最大值 */
        public double SampleMaxPresure(uint sampleIndex)
        {
            return m_waveAnalyzer.SampleMaxData(sampleIndex);
        }

        /* 样本的Presure个数 */
        public uint SamplePresureCount(uint sampleIndex)
        {
            return m_waveAnalyzer.SampleDataCount(sampleIndex);
        }

        /* 样本的Presure平均值(通过样本索引) */
        public double SamplePresureAvg(uint sampleIndex)
        {
            return m_waveAnalyzer.SampleDataAvg(sampleIndex);
        }

        /* 样本的Presure方差(代表Presure的离散程度,越小表示越集中) */
        public double SamplePresureVariance(uint sampleIndex)
        {
            return m_waveAnalyzer.SampleDataVariance(sampleIndex);
        }

        /* 样本的Presure转换成Flow的比例系数(均值) */
        public double SamplePresureAvgToFlowScale(uint sampleIndex)
        {
            double sum = m_waveAnalyzer.SampleDataSum(sampleIndex);

            double absSum = Math.Abs(sum);
            if (absSum < 0.00000001)
            {
                return 0.0;
            }

            return (SAMPLE_RATE * CalVolume) / absSum;
        }

        /* 样本是否有效(自动判断校准结果有效性) */
        public bool SampleIsValid(uint sampleIndex)
        {
            if (!m_waveAnalyzer.SampleIsValid(sampleIndex))
            {
                return false;
            }

            double sum = m_waveAnalyzer.SampleDataSum(sampleIndex);

            return (Math.Abs(sum) > (40 * CalVolume));
        }

        /* 输入压差数据 */
        public void Input(double presure)
        {
            m_waveAnalyzer.Input(presure);
        }

        /* 返回索引值对应的Presure(没有做参数有效性检查) */
        public double GetPresure(uint presureIendex)
        {
            return m_waveAnalyzer.GetData(presureIendex);
        }

        /* 返回索引值对应的Time(没有做参数有效性检查) */
        public double GetTime(uint presureIendex)
        {
            return m_waveAnalyzer.GetTime(presureIendex);
        }

        /* 自动对Presure进行分段 */
        private uint AutoDividePresureSections(double minPresure, double maxPresure, List<double> presureSectionList)
        {
            /* 先清空 */
            presureSectionList.Clear();

#if true
            /* 分段步进长度 */
            double sectionStep = 0.02;
            double stepStep = 0.002;

            /* 进行负方向分段 */
            sectionStep = 0.02;
            double sectionPos = 0.0; // [0,minPresure)
            for (; sectionPos > (minPresure + sectionStep); sectionPos -= sectionStep)
            {
                presureSectionList.Add(sectionPos);
                sectionStep += stepStep;
            }
            /* 进行正方向分段 */
            sectionStep = 0.02;
            sectionPos = sectionStep; // (0,maxPresure)
            for (; sectionPos < (maxPresure - sectionStep); sectionPos += sectionStep)
            {
                presureSectionList.Add(sectionPos);
                sectionStep += stepStep;
            }
            presureSectionList.Add(maxPresure);
#else
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
#endif

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
                for (uint i = 0; i < m_waveAnalyzer.SampleCount; ++i)
                {
                    /* 只选择有效的样本 */
                    if (m_waveAnalyzer.SampleIsValid(i))
                    {
                        /* 加入选择列表 */
                        sampleIndexList.Add(i);
                    }
                }
            }
            // else /* 用已选择的的样本进行计算 */

            /* 全局Presure最大/最小值(所有已选样本范围内) */
            double minPresureInAll = double.MaxValue;
            double maxPresureInAll = double.MinValue;

            /* 统计全局最大/最小值(所有已选样本范围内) */
            foreach (var sampleIndex in sampleIndexList)
            {
                double minPresureInSample = m_waveAnalyzer.SampleMinData(sampleIndex);
                if (minPresureInSample < minPresureInAll)
                {
                    minPresureInAll = minPresureInSample;
                }
                double maxPresureInSample = m_waveAnalyzer.SampleMaxData(sampleIndex);
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
                var sampleIndex = sampleIndexList[index];
                var sampleDataSum = m_waveAnalyzer.SampleDataSum(sampleIndex);
                /* 遍历处理样本所有数据 */
                var sampleDataIterator = m_waveAnalyzer.SampleDataIterator(sampleIndex);
                foreach(double presure in sampleDataIterator)
                {
                    /* 找到所属分段Index */
                    int sectionIndex = sectionKeyList.BinarySearch(presure);
                    if (sectionIndex < 0)
                    {
                        sectionIndex = ~sectionIndex;
                    }

                    /* 按正负方向分别处理 */
                    if (sampleDataSum > 0)
                    { // 正方向
                        vectorB[index] = CalVolume * SAMPLE_RATE;
                    }
                    else
                    { // 负方向
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
