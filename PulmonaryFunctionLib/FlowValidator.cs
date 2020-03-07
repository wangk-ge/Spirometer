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
    /* 流量验证器 */
    public class FlowValidator
    {
        public double CalVolume { get; private set; } = 0.0; // 定标桶容积(单位: L)

        /* 实时更新属性 */
        public double Time { get { return m_waveAnalyzer.Time; } } // 当前采样时间点(ms)
        public double Flow { get { return m_waveAnalyzer.Data; } } // 当前最新采集的Flow值
        public uint FlowCount { get { return m_waveAnalyzer.DataCount; } } // 当前已采集的所有(Flow数据)总个数
        public uint SampleCount { get { return m_waveAnalyzer.SampleCount; } } // 已采集的样本个数
        public double CurrSampleVolume { get { return m_waveAnalyzer.CurrSampleDataSum * (SAMPLE_TIME / 1000); } } // 当前正在采集的样本容积

        /* 呼吸方向类型 */
        public enum RespireDirection
        {
            None = 0,
            Inspiration, // 吸气
            Expiration, // 呼气
        }
        /* 代理/事件 */
        public delegate void SampleStartHandler(uint flowIndex, RespireDirection direction); // 采样开始事件代理
        public event SampleStartHandler SampleStarted; // 采样开始事件
        public delegate void SampleStopHandler(uint flowIndex, RespireDirection direction, uint sampleIndex); // 采样停止事件代理
        public event SampleStopHandler SampleStoped; // 采样停止事件

        /* 波形数据分析器 */
        private WaveAnalyzer m_waveAnalyzer;

        /* 参数 */
        private readonly double SAMPLE_TIME = 3.03; // 采样时间(ms)
        private readonly double SAMPLE_RATE = 330; // 采样率

        /* 阈值(用于检测采样启动和停止条件) */
        private readonly int START_SAMPLE_COUNT = 2; // 启动检测,波动统计采样次数
        private readonly int STOP_SAMPLE_COUNT = 20; // 停止检测,波动统计采样次数
        private readonly double START_FLOW_DELTA = 0.01; // 斜度绝对值超过该阈值将识别为启动测试(斜度为正表示吸气启动、为负表示吹气开始)
        private readonly double STOP_FLOW_THRESHOLD = 0.01; // 停止检测流量阈值,当启动测试后如果流量对值小于阈值,则开始检测停止条件

        public FlowValidator(double sampleRate, double calVolume = 1.0)
        {
            SAMPLE_RATE = sampleRate;
            SAMPLE_TIME = 1000 / sampleRate;

            m_waveAnalyzer = new WaveAnalyzer(sampleRate,
                START_SAMPLE_COUNT, START_FLOW_DELTA,
                STOP_SAMPLE_COUNT, STOP_FLOW_THRESHOLD);

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
        public RespireDirection SampleFlowDirection(uint sampleIndex)
        {
            /* 取得样本方向 */
            WaveSampleDirection waveDir = m_waveAnalyzer.SampleDataDirection(sampleIndex);

            /* 转换为呼吸方向 */
            RespireDirection respireDir = ToRespireDirection(waveDir);

            return respireDir;
        }

        /* 样本的容积 */
        public double SampleVolume(uint sampleIndex)
        {
            return m_waveAnalyzer.SampleDataSum(sampleIndex) * (SAMPLE_TIME / 1000);
        }

        /* 样本的Flow最小值 */
        public double SampleMinFlow(uint sampleIndex)
        {
            return m_waveAnalyzer.SampleMinData(sampleIndex);
        }

        /* 样本的Flow最大值 */
        public double SampleMaxFlow(uint sampleIndex)
        {
            return m_waveAnalyzer.SampleMaxData(sampleIndex);
        }

        /* 样本的Flow个数 */
        public uint SampleFlowCount(uint sampleIndex)
        {
            return m_waveAnalyzer.SampleDataCount(sampleIndex);
        }

        /* 样本的Flow平均值(通过样本索引) */
        public double SampleFlowAvg(uint sampleIndex)
        {
            return m_waveAnalyzer.SampleDataAvg(sampleIndex);
        }

        /* 样本的Flow方差(代表Flow的离散程度,越小表示越集中) */
        public double SampleFlowVariance(uint sampleIndex)
        {
            return m_waveAnalyzer.SampleDataVariance(sampleIndex);
        }

        /* 样本容积误差(L) */
        public double SampleVolumeError(uint sampleIndex)
        {
            var sampleVolume = SampleVolume(sampleIndex);
            var error = 0.0; // 误差
            if (sampleVolume > 0)
            {
                error = sampleVolume - CalVolume;
            }
            else
            {
                error = sampleVolume - (-CalVolume);
            }
            return error;
        }

        /* 样本容积误差率(%) */
        public double SampleVolumeErrorRate(uint sampleIndex)
        {
            var sampleVolume = SampleVolume(sampleIndex);
            var error = 0.0; // 误差
            var errorRate = 0.0; // 误差率
            if (sampleVolume > 0)
            {
                error = sampleVolume - CalVolume;
                errorRate = error * 100 / CalVolume;
            }
            else
            {
                error = sampleVolume - (-CalVolume);
                errorRate = error * 100 / (-CalVolume);
            }
            return errorRate;
        }

        /* 输入流量数据 */
        public void Input(double flow)
        {
            m_waveAnalyzer.Input(flow);
        }

        /* 返回索引值对应的Flow(没有做参数有效性检查) */
        public double GetFlow(uint flowIndex)
        {
            return m_waveAnalyzer.GetData(flowIndex);
        }

        /* 返回索引值对应的Time(没有做参数有效性检查) */
        public double GetTime(uint flowIndex)
        {
            return m_waveAnalyzer.GetTime(flowIndex);
        }
    }
}
