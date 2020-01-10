﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spirometer
{
    /* 肺功能参数计算 */
    class PulmonaryFunction
    {
        public double Time { get; private set; } = 0.0; // 时间(ms)
        public double Flow { get; private set; } = 0.0; // 流量(L/S)
        public double Volume { get; private set; } = 0.0; // 容积(L)

        public delegate void ZeroingCompleteHandler(uint sampleIndex); // 归零完成事件代理
        public event ZeroingCompleteHandler ZeroingCompleted; // 归零完成事件
        public delegate void InspirationStartHandler(uint sampleIndex); // 吸气开始事件代理
        public event InspirationStartHandler InspirationStarted; // 吸气开始事件
        public delegate void ExpirationStartHandler(uint sampleIndex); // 呼气开始事件代理
        public event ExpirationStartHandler ExpirationStarted; // 呼气开始事件
        public delegate void MeasureStopHandler(uint sampleIndex); // 测试停止事件代理
        public event MeasureStopHandler MeasureStoped; // 测试停止事件

        private enum State
        {
            Reset, // 复位状态
            Zeroing, // 正在归零状态
            WaitStart, // 等待启动测试状态
            Inspiration, // 正在吸气状态
            Expiration, // 正在呼气状态
            Stop, // 测试已停止状态
        }

        private State m_state = State.Reset; // 工作状态
        private double m_flowZeroOffset = 0.0; // 流量零点偏移值
        private uint m_sampleCount = 0U; // 统计已采样数据个数
        private readonly double SAMPLE_TIME = 0.0; // 采样时间(ms)
        private WaveStatistician m_waveStatistician = new WaveStatistician(); // 用于统计波动数据
        private readonly int ZEROING_SAMPLE_COUNT = 300; // 归零过程采样次数
        private readonly double ZEROING_ALLOW_RANGE = 0.04; // 归零过程允许的波动范围
        private readonly double INSPIRATION_START_FLOW = 0.02; // 吸气启动流速阈值(L/S),连续N次采样流速大于该值则判断为开始吸气
        private readonly double INSPIRATION_START_SAMPLE_COUNT = 100; // 吸气启动检测采样次数
        private uint m_inspirationSampleCnt = 0U; // 统计持续吸气样本数,用于检测吸气启动条件
        private double m_inspirationVolume = 0.0; // 用于检测吸气启动条件的样本吸气容积
        private readonly double EXPIRATION_START_FLOW = -0.02; // 呼气启动流速阈值(L/S),连续N次采样流速小于该值则判断为开始呼气
        private readonly double EXPIRATION_START_SAMPLE_COUNT = 100; // 呼气启动检测采样次数
        private uint m_expirationSampleCnt = 0U; // 统计持续呼气样本数,用于检测呼气启动条件
        private double m_expirationVolume = 0.0; // 用于检测呼气启动条件的样本呼气容积
        private readonly double DEFAULT_RV = 2.55; // 默认残气量(RV),单位: L


        public PulmonaryFunction(double sampleTime)
        {
            SAMPLE_TIME = sampleTime;
            Volume = DEFAULT_RV;
        }

        /* 状态重置 */
        public void Reset()
        {
            Time = 0.0;
            Flow = 0.0;
            Volume = DEFAULT_RV;
            m_state = State.Reset;
            m_flowZeroOffset = 0.0;
            m_sampleCount = 0U;
            m_waveStatistician.Reset();
            m_inspirationSampleCnt = 0U;
            m_inspirationVolume = 0.0;
            m_expirationSampleCnt = 0U;
            m_expirationVolume = 0.0;
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

        /* 输入流量数据 */
        public void Input(double flow)
        {
            /* 统计样本个数 */
            ++m_sampleCount;
            
            /* 统计样本时间戳 */
            Time += SAMPLE_TIME;

            /* 当前流量 */
            Flow = flow - m_flowZeroOffset; // 执行零点校正

            switch (m_state)
            {
                case State.Reset: // 复位状态
                    {
                        /* 进入正在归零状态 */
                        SetState(State.Zeroing);

                        /* 开始统计波动范围 */
                        m_waveStatistician.Input(flow);
                        break;
                    }
                case State.Zeroing: // 正在归零状态
                    {
                        /* 继续统计波动范围 */
                        m_waveStatistician.Input(flow);

                        /* 是否已达到所需采样次数 */
                        if (m_waveStatistician.SampleCount < ZEROING_SAMPLE_COUNT)
                        {
                            /* 保持正在归零状态 */
                            break;
                        }

                        /* 波动范围是否在允许范围内 */
                        if (m_waveStatistician.Range < ZEROING_ALLOW_RANGE)
                        {
                            /* 数据稳定度达标,将统计平均值作为零点校准值 */
                            m_flowZeroOffset = m_waveStatistician.AvgVal;

                            /* 进入等待启动测试状态 */
                            SetState(State.WaitStart);

                            /* 触发归零完成事件 */
                            ZeroingCompleted?.Invoke(m_sampleCount);
                        }
                        else
                        {
                            /* 重置波动统计器,重新开始评估输入数据 */
                            m_waveStatistician.Reset();
                            /* 保持正在归零状态 */

                            /* 重新开始统计波动范围 */
                            m_waveStatistician.Input(flow);
                        }
                        break;
                    }
                case State.WaitStart: // 等待启动测试状态
                    {
                        /* 流量是否达到吸气阈值 */
                        if (Flow > INSPIRATION_START_FLOW)
                        {
                            /* 重置呼气样本数 */
                            m_expirationSampleCnt = 0U;
                            /* 重置呼气样本容积 */
                            m_expirationVolume = 0.0;

                            /* 统计持续吸气样本数 */
                            ++m_inspirationSampleCnt;
                            /* 统计吸气样本容积 */
                            m_inspirationVolume += Flow * SAMPLE_TIME / 1000;

                            /* 是否已监测到吸气启动条件 */
                            if (m_inspirationSampleCnt >= INSPIRATION_START_SAMPLE_COUNT)
                            {
                                /* 更新当前容积 */
                                Volume += m_inspirationVolume;

                                /* 进入正在吸气状态 */
                                SetState(State.Inspiration);

                                /* 触发吸气开始事件 */
                                uint sampleIndex = m_sampleCount - m_inspirationSampleCnt;
                                InspirationStarted?.Invoke(sampleIndex);

                                /* 重置吸气样本数 */
                                m_inspirationSampleCnt = 0;
                                /* 重置吸气样本容积 */
                                m_inspirationVolume = 0.0;
                            }
                        }
                        /* 流量是否达到呼气阈值 */
                        else if (Flow < EXPIRATION_START_FLOW)
                        {
                            /* 重置吸气样本数 */
                            m_inspirationSampleCnt = 0U;
                            /* 重置吸气样本容积 */
                            m_inspirationVolume = 0.0;

                            /* 统计持续呼气样本数 */
                            ++m_expirationSampleCnt;
                            /* 统计呼气样本容积 */
                            m_expirationVolume += Flow * SAMPLE_TIME / 1000;

                            /* 是否已监测到呼气启动条件 */
                            if (m_expirationSampleCnt >= EXPIRATION_START_SAMPLE_COUNT)
                            {
                                /* 更新当前容积 */
                                Volume += m_expirationVolume;

                                /* 进入正在呼气状态 */
                                SetState(State.Expiration);

                                /* 触发呼气开始事件 */
                                uint sampleIndex = m_sampleCount - m_expirationSampleCnt;
                                ExpirationStarted?.Invoke(sampleIndex);

                                /* 重置呼气样本数 */
                                m_expirationSampleCnt = 0;
                                /* 重置呼气样本容积 */
                                m_expirationVolume = 0.0;
                            }
                        }
                        break;
                    }
                case State.Inspiration: // 正在吸气状态
                    {
                        /* 更新当前容积 */
                        Volume += Flow * SAMPLE_TIME / 1000;

                        /* 流量达不到吸气阈值? */
                        if (Flow < INSPIRATION_START_FLOW)
                        {
                            /* 流量是否达到呼气阈值 */
                            if (Flow < EXPIRATION_START_FLOW)
                            {
                                /* 重置吸气样本数 */
                                m_inspirationSampleCnt = 0U;

                                /* 统计持续呼气样本数 */
                                ++m_expirationSampleCnt;

                                /* 是否已监测到呼气启动条件 */
                                if (m_expirationSampleCnt >= EXPIRATION_START_SAMPLE_COUNT)
                                {
                                    /* 进入正在呼气状态 */
                                    SetState(State.Expiration);

                                    /* 触发呼气开始事件 */
                                    uint sampleIndex = m_sampleCount - m_expirationSampleCnt;
                                    ExpirationStarted?.Invoke(sampleIndex);

                                    /* 重置呼气样本数 */
                                    m_expirationSampleCnt = 0;
                                }
                            }
                        }
                        break;
                    }
                case State.Expiration: // 正在呼气状态
                    {
                        /* 更新当前容积 */
                        Volume += Flow * SAMPLE_TIME / 1000;

                        /* 流量达不到呼气阈值 */
                        if (Flow > EXPIRATION_START_FLOW)
                        {
                            /* 流量是否达到吸气阈值 */
                            if (Flow > INSPIRATION_START_FLOW)
                            {
                                /* 重置呼气样本数 */
                                m_expirationSampleCnt = 0U;

                                /* 统计持续吸气样本数 */
                                ++m_inspirationSampleCnt;

                                /* 是否已监测到吸气启动条件 */
                                if (m_inspirationSampleCnt >= INSPIRATION_START_SAMPLE_COUNT)
                                {
                                    /* 进入正在吸气状态 */
                                    SetState(State.Inspiration);

                                    /* 触发吸气开始事件 */
                                    uint sampleIndex = m_sampleCount - m_inspirationSampleCnt;
                                    InspirationStarted?.Invoke(sampleIndex);

                                    /* 重置吸气样本数 */
                                    m_inspirationSampleCnt = 0;
                                }
                            }
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
    }
}
