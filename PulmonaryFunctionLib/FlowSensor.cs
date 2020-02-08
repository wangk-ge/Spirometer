﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading.Tasks;

namespace PulmonaryFunctionLib
{
    /* 流速传感器 */
    public class FlowSensor
    {
        private SerialPort m_serialPort = null;
        private ConcurrentQueue<TaskCompletionSource<string>> m_cmdRespTaskCompQue = new ConcurrentQueue<TaskCompletionSource<string>>();
        private FrameDecoder m_frameDecoder = new FrameDecoder(); // 串口数据帧解码器
        private KalmanFilter m_kalmanFilter = new KalmanFilter(0.01f/*Q*/, 0.1f/*R*/, 10.0f/*P*/, 0); // 卡尔曼滤波器

        /* 采样率,单位:HZ */
        public readonly double SAMPLE_RATE = 330;
        /* 采样时间,单位:MS */
        public double SAMPLE_TIME { get { return (1000 / SAMPLE_RATE); } }

        public delegate void PresureRecvHandler(byte channel, double presure); // 压差接收代理
        public event PresureRecvHandler PresureRecved; // 压差收取事件

        /* 校准参数列表 */
        private List<double> m_calParamSectionKeyList = new List<double>();
        private List<double> m_calParamValList = new List<double>();

        public FlowSensor()
        {
            //FrameDecoder.Test();

            m_frameDecoder.CmdRespRecved += new FrameDecoder.CmdRespRecvHandler((string cmdResp) => {
                Console.WriteLine($"CmdRespRecved: {cmdResp}");
                if (m_cmdRespTaskCompQue.Count > 0)
                {
                    /* 通知CMD Task执行结果 */
                    TaskCompletionSource<string> taskComp;
                    m_cmdRespTaskCompQue.TryDequeue(out taskComp);
                    taskComp?.SetResult(cmdResp);
                }
            });

            m_frameDecoder.WaveDataRecved += new FrameDecoder.WaveDataRecvHandler((byte channel, double presure) => {
                //Console.WriteLine($"WaveDataRespRecved: {channel} {presure}");

                PresureRecved?.Invoke(channel, presure / (10 * 1000 * 1000)); // 触发压差收取事件
            });
        }

        public void SetCalibrationParamList(List<double> sectionKeyList, List<double> paramValList)
        {
            m_calParamSectionKeyList.Clear();
            m_calParamValList.Clear();
            m_calParamSectionKeyList.AddRange(sectionKeyList);
            m_calParamValList.AddRange(paramValList);
        }

        /* 根据指定校准参数列表,获取压差对应的压差转流量比例系数 */
        private static double GetPresureFlowScale(double presure, List<double> calParamSectionKeyList, List<double> calParamValList)
        {
            if (calParamSectionKeyList.Count <= 0)
            {
                return 1.0;
            }

            /* 找到所属分段Index */
            int sectionIndex = calParamSectionKeyList.BinarySearch(presure);
            if (sectionIndex < 0)
            {
                /*
                 如果找到 item，则为已排序的 List<T> 中 item 的从零开始的索引；
                 否则为一个负数，该负数是大于 item 的下一个元素的索引的按位求补。
                 如果没有更大的元素，则为 Count 的按位求补。
                 */
                sectionIndex = ~sectionIndex;
            }
            if (sectionIndex >= calParamSectionKeyList.Count)
            {
                sectionIndex = (calParamSectionKeyList.Count - 1);
            }
            return calParamValList[sectionIndex];
        }

        /* 根据指定校准参数列表,执行压差转流量(单位:L/S) */
        public static double PresureToFlow(double presure, List<double> calParamSectionKeyList, List<double> calParamValList)
        {
            /* 压差转流量 */
            double presureFlowScale = GetPresureFlowScale(presure, calParamSectionKeyList, calParamValList);
            double flow = presureFlowScale * presure;
            return flow;
        }

        /* 执行压差转流量(单位:L/S) */
        public double PresureToFlow(double presure)
        {
            //presure = m_kalmanFilter.Input((float)presure); // 执行滤波
            /* 压差转流量 */
            double presureFlowScale = GetPresureFlowScale(presure, m_calParamSectionKeyList, m_calParamValList);
            double flow = presureFlowScale * presure;
            return flow;
        }

        public bool Open(string portName)
        {
            try
            {
                m_serialPort?.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                return false;
            }

            m_serialPort?.Dispose();

            try
            {
                m_serialPort = new SerialPort(portName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                return false;
            }

            m_serialPort.BaudRate = 115200;
            m_serialPort.Parity = Parity.None;
            m_serialPort.StopBits = StopBits.One;
            m_serialPort.DataBits = 8;
            m_serialPort.Handshake = Handshake.None;
            m_serialPort.RtsEnable = true;
            m_serialPort.DtrEnable = true;

            m_serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

            try
            {
                m_serialPort.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                m_serialPort.Dispose();
                m_serialPort = null;

                return false;
            }

            return true;
        }

        private void DataReceivedHandler(
                        object sender,
                        SerialDataReceivedEventArgs e)
        {
            SerialPort sp = sender as SerialPort;
            int dataLen = sp.BytesToRead;
            byte[] dataBuf = new byte[dataLen];
            sp.Read(dataBuf, 0, dataLen);
            m_frameDecoder.FrameDecode(dataBuf);
        }

        public bool IsOpen()
        {
            if (null == m_serialPort)
            {
                return false;
            }

            return m_serialPort.IsOpen;
        }

        public void Close()
        {
            if (null == m_serialPort)
            {
                return;
            }

            try 
            {
                m_serialPort.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
            m_serialPort.Dispose();
            m_serialPort = null;

            /* 清空队列 */
            TaskCompletionSource<string> taskComp;
            while (m_cmdRespTaskCompQue.Count > 0)
            {
                bool bRet = m_cmdRespTaskCompQue.TryDequeue(out taskComp);
                if (!bRet)
                {
                    break;
                }
            }
        }

        /* 创建CMD Task */
        private Task<string> ExcuteCmdTask(string cmd)
        {
            /* 将CMD Task记录到完成队列 */
            var cmdRespTaskComp = new TaskCompletionSource<string>();
            m_cmdRespTaskCompQue.Enqueue(cmdRespTaskComp);

            /* 发送CMD */
            m_serialPort.Write(cmd);

            /* 返回Task */
            var task = cmdRespTaskComp.Task;
            return task;
        }

        /* 执行命令(异步版本) */
        public async Task<string> ExcuteCmdAsync(string cmd, int timeOut)
        {
            if (null == m_serialPort)
            {
                return string.Empty;
            }

            /* 创建CMD Task */
            var cmdTask = ExcuteCmdTask(cmd);

            /* 异步等待执行完毕或超时 */
            var task = await Task.WhenAny(cmdTask, Task.Delay(timeOut));
            if (task == cmdTask)
            { // CMD Task执行完毕
                return cmdTask.Result;
            }

            /* 超时 */
            /* 删除完成队列中的记录 */
            TaskCompletionSource<string> taskComp;
            m_cmdRespTaskCompQue.TryDequeue(out taskComp);

            return string.Empty;
        }

        /* 执行命令(同步版本) */
        public string ExcuteCmd(string cmd, int timeOut)
        {
            if (null == m_serialPort)
            {
                return string.Empty;
            }

            /* 创建CMD Task */
            var cmdTask = ExcuteCmdTask(cmd);

            /* 同步等待执行完毕或超时 */
            var compTask = Task.WhenAny(cmdTask, Task.Delay(timeOut));
            var task = compTask.Result;
            if (task == cmdTask)
            { // CMD Task执行完毕
                return cmdTask.Result;
            }

            /* 超时 */
            /* 删除完成队列中的记录 */
            TaskCompletionSource<string> taskComp;
            m_cmdRespTaskCompQue.TryDequeue(out taskComp);

            return string.Empty;
        }

        /* 请求启动采集(异步版本) */
        public async Task<bool> StartAsync()
        {
            string cmd = "[ADC_START]"; // 启动采集
            string cmdResp = await ExcuteCmdAsync(cmd, 2000);
            return cmdResp == "[OK]";
        }

        /* 请求启动采集(同步版本) */
        public bool Start()
        {
            string cmd = "[ADC_START]"; // 启动采集
            string cmdResp = ExcuteCmd(cmd, 2000);
            return cmdResp == "[OK]";
        }

        /* 请求停止采集(异步版本) */
        public async Task<bool> StopAsync()
        {
            string cmd = "[ADC_STOP]"; // 启动采集
            string cmdResp = await ExcuteCmdAsync(cmd, 2000);
            return cmdResp == "[OK]";
        }

        /* 请求停止采集(同步版本) */
        public bool Stop()
        {
            string cmd = "[ADC_STOP]"; // 启动采集
            string cmdResp = ExcuteCmd(cmd, 2000);
            return cmdResp == "[OK]";
        }

        /* 请求执行归零(异步版本) */
        public async Task<bool> ZeroingAsync()
        {
            string cmd = "[ADC_CAL]"; // 归零
            string cmdResp = await ExcuteCmdAsync(cmd, 2000);
            return cmdResp == "[OK]";
        }

        /* 请求执行归零(同步版本) */
        public bool Zeroing()
        {
            string cmd = "[ADC_CAL]"; // 归零
            string cmdResp = ExcuteCmd(cmd, 2000);
            return cmdResp == "[OK]";
        }

        /* 取得下位机版本号(异步版本) */
        public async Task<string> BoardVersionAsync()
        {
            string cmd = "[VER]"; // 取得版本号
            string cmdResp = await ExcuteCmdAsync(cmd, 2000);
            return cmdResp;
        }

        /* 取得下位机版本号(同步版本) */
        public string BoardVersion()
        {
            string cmd = "[VER]"; // 取得版本号
            string cmdResp = ExcuteCmd(cmd, 2000);
            return cmdResp;
        }

        /* 取得下位机固件编译时间(异步版本) */
        public async Task<string> BoardBuildTimeAsync()
        {
            string cmd = "[BUILD]"; // 取得固件编译时间
            string cmdResp = await ExcuteCmdAsync(cmd, 2000);
            return cmdResp;
        }

        /* 取得下位机固件编译时间(同步版本) */
        public string BoardBuildTime()
        {
            string cmd = "[BUILD]"; // 取得固件编译时间
            string cmdResp = ExcuteCmd(cmd, 2000);
            return cmdResp;
        }

        /* 取得环境温度(异步版本) */
        public async Task<double> AmbientTemperatureAsync()
        {
            string cmd = "[BME280_TEMP]"; // 取得环境温度
            string cmdResp = await ExcuteCmdAsync(cmd, 2000);
            if (cmdResp != string.Empty)
            {
                return Convert.ToDouble(cmdResp);
            }
            return 0.0;
        }

        /* 取得环境温度(同步版本) */
        public double AmbientTemperature()
        {
            string cmd = "[BME280_TEMP]"; // 取得环境温度
            string cmdResp = ExcuteCmd(cmd, 2000);
            if (cmdResp != string.Empty)
            {
                return Convert.ToDouble(cmdResp);
            }
            return 0.0;
        }

        /* 取得环境湿度(异步版本) */
        public async Task<double> AmbientHumidityAsync()
        {
            string cmd = "[BME280_HUMI]"; // 取得环境湿度
            string cmdResp = await ExcuteCmdAsync(cmd, 2000);
            if (cmdResp != string.Empty)
            {
                return Convert.ToDouble(cmdResp);
            }
            return 0.0;
        }

        /* 取得环境湿度(同步版本) */
        public double AmbientHumidity()
        {
            string cmd = "[BME280_HUMI]"; // 取得环境湿度
            string cmdResp = ExcuteCmd(cmd, 2000);
            if (cmdResp != string.Empty)
            {
                return Convert.ToDouble(cmdResp);
            }
            return 0.0;
        }

        /* 取得大气压(异步版本) */
        public async Task<double> BarometricAsync()
        {
            string cmd = "[BME280_BARO]"; // 取得大气压
            string cmdResp = await ExcuteCmdAsync(cmd, 2000);
            if (cmdResp != string.Empty)
            {
                return Convert.ToDouble(cmdResp);
            }
            return 0.0;
        }

        /* 取得大气压(同步版本) */
        public double Barometric()
        {
            string cmd = "[BME280_BARO]"; // 取得大气压
            string cmdResp = ExcuteCmd(cmd, 2000);
            if (cmdResp != string.Empty)
            {
                return Convert.ToDouble(cmdResp);
            }
            return 0.0;   
        }
    }
}
