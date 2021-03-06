﻿using MathNet.Numerics;
using System;
using System.Collections.Generic;

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
        public bool StartSampling { get { return m_waveAnalyzer.StartSampling; } } // 当前是否已启动采样状态

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
        private readonly int POLYNOMIAL_ORDER = 17; // 全局拟合多项式阶数
        private readonly int POLYNOMIAL_P_ORDER = 3; // 正方向拟合多项式阶数
        private readonly int POLYNOMIAL_N_ORDER = 3; // 负方向拟合多项式阶数

        /* 阈值(用于检测采样启动和停止条件) */
        private readonly int START_SAMPLE_COUNT = 2; // 启动检测,波动统计采样次数
        private readonly int STOP_SAMPLE_COUNT = 20; // 停止检测,波动统计采样次数
        private readonly double START_PRESURE_DELTA = 0.0005; // 斜度绝对值超过该阈值将识别为启动测试(斜度为正表示吸气启动、为负表示吹气开始)
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

        /* 样本的平均流量 */
        public double SampleFlowAvg(uint sampleIndex)
        {
            /* ms */
            double sampleTime = m_waveAnalyzer.SampleTime(sampleIndex);
            if (sampleTime < 0.00000001)
            {
                return 0.0;
            }

            /* 转换单位为S */
            sampleTime /= 1000;

            /* 定标桶容积 */
            double calVol = CalVolume;
            double sum = m_waveAnalyzer.SampleDataSum(sampleIndex);
            if (sum < 0)
            {
                calVol = -calVol;
            }

            /* 平均流量 */
            return calVol / sampleTime;
        }

        /* 样本的Presure平均值 */
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

        /* 样本是否有效(自动判断样本有效性) */
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

        /* 统计压差范围[min,max] */
        private void PresureRange(out double minPresure, out double maxPresure, List<uint> sampleIndexList)
        {
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

            minPresure = minPresureInAll;
            maxPresure = maxPresureInAll;
        }

        /* 计算多项式拟合参数(校准参数列表通过函数参数返回) */
        private bool CalcPolynomialParams(List<double> paramList, List<double> paramListP, List<double> paramListN, List<uint> sampleIndexList)
        {
            /* 确保结果列表清空状态 */
            paramList.Clear();
            paramListP.Clear();
            paramListN.Clear();

            /* 样本数量 */
            uint sampleNum = (uint)sampleIndexList.Count;

            /* 构造方程组参数矩阵 */
            List<double[]> matrixAList = new List<double[]>(); // 全局matrixA
            List<double> vectorBList = new List<double>(); // 全局vectorB
            List<double[]> matrixAListP = new List<double[]>(); // 正方向matrixA
            List<double> vectorBListP = new List<double>(); // 正方向vectorB
            List<double[]> matrixAListN = new List<double[]>(); // 负方向matrixA
            List<double> vectorBListN = new List<double>(); // 负方向vectorB
            for (int i = 0; i < sampleNum; ++i)
            {
                var sampleIndex = sampleIndexList[i];

                /*
                 * 拟合多项式参数
                 * F=a*P^0+b*P^1+c*P^2+...
                 * V/t=V*sampleRate=a*(P1^0+P2^0+...+Pn^0)+b*(P1^1+P2^1+...+Pn^1)+c*(P1^2+P2^2+...+Pn^2)+... 
                 * 求参数:a b c ...
                 */
                var samplePresureAvg = SamplePresureAvg(sampleIndex); // 压差均值
#if false
                if (samplePresureAvg > 0)
                { // 正方向
                    double[] matrixAi = new double[POLYNOMIAL_ORDER];
                    double[] matrixAiP = new double[POLYNOMIAL_P_ORDER];
                    var sampleDataIterator = m_waveAnalyzer.SampleDataIterator(sampleIndex);
                    foreach (double presure in sampleDataIterator)
                    {
                        double pn = 1.0; // P^0
                        for (int n = 0; n < Math.Max(POLYNOMIAL_P_ORDER, POLYNOMIAL_ORDER); n++)
                        {
                            pn *= presure; // P^n

                            if (n < matrixAi.Length)
                            {
                                matrixAi[n] += pn;
                            }

                            if (n < matrixAiP.Length)
                            {
                                matrixAiP[n] += pn;
                            }
                        }
                    }
                    double y = CalVolume * SAMPLE_RATE;

                    matrixAListP.Add(matrixAiP);
                    vectorBListP.Add(y);

                    matrixAList.Add(matrixAi);
                    vectorBList.Add(y);
                }
                else //if (samplePresureAvg <= 0)
                { // 负方向
                    double[] matrixAi = new double[POLYNOMIAL_ORDER];
                    double[] matrixAiN = new double[POLYNOMIAL_N_ORDER];
                    var sampleDataIterator = m_waveAnalyzer.SampleDataIterator(sampleIndex);
                    foreach (double presure in sampleDataIterator)
                    {
                        double pn = 1.0; // P^0
                        for (int n = 0; n < Math.Max(POLYNOMIAL_N_ORDER, POLYNOMIAL_ORDER); n++)
                        {
                            pn *= presure; // P^n

                            if (n < matrixAi.Length)
                            {
                                matrixAi[n] += pn;
                            }

                            if (n < matrixAiN.Length)
                            {
                                matrixAiN[n] += pn;
                            }
                        }
                    }
                    double y = -CalVolume * SAMPLE_RATE;

                    matrixAListN.Add(matrixAiN);
                    vectorBListN.Add(y);

                    matrixAList.Add(matrixAi);
                    vectorBList.Add(y);
                }
#else
                if (samplePresureAvg > 0)
                { // 正方向
                    double[] matrixAi = new double[POLYNOMIAL_ORDER + 1];
                    double[] matrixAiP = new double[POLYNOMIAL_P_ORDER + 1];
                    var sampleDataIterator = m_waveAnalyzer.SampleDataIterator(sampleIndex);
                    foreach (double presure in sampleDataIterator)
                    {
                        double pn = 1.0; // P^0
                        for (int n = 0; n < Math.Max(POLYNOMIAL_P_ORDER, POLYNOMIAL_ORDER) + 1; n++)
                        {
                            if (n <= POLYNOMIAL_ORDER)
                            {
                                matrixAi[n] += pn;
                            }

                            if (n <= POLYNOMIAL_P_ORDER)
                            {
                                matrixAiP[n] += pn;
                            }

                            pn *= presure; // P^n
                        }
                    }
                    double y = CalVolume * SAMPLE_RATE;

                    matrixAListP.Add(matrixAiP);
                    vectorBListP.Add(y);

                    matrixAList.Add(matrixAi);
                    vectorBList.Add(y);
                }
                else //if (samplePresureAvg <= 0)
                { // 负方向
                    double[] matrixAi = new double[POLYNOMIAL_ORDER + 1];
                    double[] matrixAiN = new double[POLYNOMIAL_N_ORDER + 1];
                    var sampleDataIterator = m_waveAnalyzer.SampleDataIterator(sampleIndex);
                    foreach (double presure in sampleDataIterator)
                    {
                        double pn = 1.0; // P^0
                        for (int n = 0; n < Math.Max(POLYNOMIAL_N_ORDER, POLYNOMIAL_ORDER) + 1; n++)
                        {
                            if (n <= POLYNOMIAL_ORDER)
                            {
                                matrixAi[n] += pn;
                            }

                            if (n <= POLYNOMIAL_N_ORDER)
                            {
                                matrixAiN[n] += pn;
                            }

                            pn *= presure; // P^n
                        }
                    }
                    double y = -CalVolume * SAMPLE_RATE;

                    matrixAListN.Add(matrixAiN);
                    vectorBListN.Add(y);

                    matrixAList.Add(matrixAi);
                    vectorBList.Add(y);
                }
#endif
            }
            bool bRet = false;
            try
            {
                /* 使用多元线性最小二乘法（linear least squares）拟合最优参数集 */

                /* 全局 */
                double[][] matrixA = matrixAList.ToArray();
                double[] vectorB = vectorBList.ToArray() ;
                double[] result = Fit.MultiDim(matrixA, vectorB, false,
                    MathNet.Numerics.LinearRegression.DirectRegressionMethod.Svd);
                paramList.AddRange(result);

                /* 正方向 */
                double[][] matrixAP = matrixAListP.ToArray();
                double[] vectorBP = vectorBListP.ToArray();
                double[] resultP = Fit.MultiDim(matrixAP, vectorBP, false,
                    MathNet.Numerics.LinearRegression.DirectRegressionMethod.Svd);
                paramListP.AddRange(resultP);

                /* 负方向 */
                double[][] matrixAN = matrixAListN.ToArray();
                double[] vectorBN = vectorBListN.ToArray();
                double[] resultN = Fit.MultiDim(matrixAN, vectorBN, false,
                    MathNet.Numerics.LinearRegression.DirectRegressionMethod.Svd);
                paramListN.AddRange(resultN);

                bRet = true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception: {e.Message}");

                bRet = false;
            }

            return bRet;
        }

        /* 计算校准参数(校准参数列表通过函数参数返回) */
        public bool CalcCalibrationParams(List<double> paramList, List<double> paramListP, List<double> paramListN,
            out double minPresure, out double maxPresure, List<uint> sampleIndexList = null)
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

            /* 统计压差范围 */
            PresureRange(out minPresure, out maxPresure, sampleIndexList);

            /* 确保结果列表清空状态 */
            paramList.Clear();
            paramListP.Clear();
            paramListN.Clear();

            /* 计算多项式拟合参数 */
            bool bRet = CalcPolynomialParams(paramList, paramListP, paramListN, sampleIndexList);

            return bRet;
        }

        /* 根据指定校准参数列表,执行压差转流量(单位:L/S) */
        public static double PresureToFlow(double presure, List<double> paramList)
        {
            if (paramList.Count <= 0)
            {
                return presure;
            }

#if false
            // y = k1 * x^1 + k2 * x^2 + ...
            double x = presure;
            double y = 0.0;
            double xn = 1.0; // x^n
            for (int i = 0; i < paramList.Count; ++i)
            {
                xn *= x;
                double k = paramList[i];
                y += k * xn;
            }
#else
            // y = k0 * x^0 + k1 * x^1 + ...
            double x = presure;
            double y = 0.0;
            double xn = 1.0; // x^n
            for (int i = 0; i < paramList.Count; ++i)
            {
                double k = paramList[i];
                y += k * xn;
                xn *= x;
            }
#endif

            return y;
        }

        /* 根据指定校准参数列表,执行压差转流量(单位:L/S) */
        public static double PresureToFlow(double presure, List<double> paramList,
            List<double> paramListP, List<double> paramListN,
            double minPresure, double maxPresure)
        {
            if (presure > 0.3)
            {
                paramList = paramListP;
            }

            if (presure < -0.3)
            {
                paramList = paramListN;
            }

            return PresureToFlow(presure, paramList);
        }
    }
}
