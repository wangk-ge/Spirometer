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
        /* 吸气(正压差)流量校准参数列表,按presure升序排列 */
        private List<CalibrationParam> m_inCalibrationParams = new List<CalibrationParam>();
        /* 呼气(负压差)流量校准参数列表,按presure降序排列 */
        private List<CalibrationParam> m_exCalibrationParams = new List<CalibrationParam>();

        /* 采样率,单位:HZ */
        public readonly double SAMPLE_RATE = 330;
        /* 采样时间,单位:MS */
        public double SAMPLE_TIME { get { return (1000 / SAMPLE_RATE); } }

        /* 校准参数类型定义 */
        public struct CalibrationParam
        {
            public double presureAvg; // 校准压差数据的平均值(inH2O)
            public double presureFlowScale; // 压差转流量比例系数(Flow=presureFlowScale*Presure)
            public double presureSum; // 校准压差数据的求和值
            public double peekPresure; // 校准压差数据的极值
            public double presureVariance; // 校准压差数据的方差
        }

        public delegate void PresureRecvHandler(byte channel, double presure); // 压差接收代理
        public event PresureRecvHandler PresureRecved; // 压差收取事件

        public FlowSensor()
        {
            //FrameDecoder.Test();

            /* 初始化使用默认校准参数 */
            m_inCalibrationParams.Add(new CalibrationParam() { presureAvg = 213510.197103527, presureFlowScale = SAMPLE_RATE / 446022801.749268 });
            m_inCalibrationParams.Add(new CalibrationParam() { presureAvg = 604662.05452032, presureFlowScale = SAMPLE_RATE / 442612623.908875 });
            m_inCalibrationParams.Add(new CalibrationParam() { presureAvg = 1136892.5545729, presureFlowScale = SAMPLE_RATE / 427471600.519409 });
            m_inCalibrationParams.Add(new CalibrationParam() { presureAvg = 1343683.27862993, presureFlowScale = SAMPLE_RATE / 424603916.047058 });
            m_inCalibrationParams.Add(new CalibrationParam() { presureAvg = 2727034.35010543, presureFlowScale = SAMPLE_RATE / 428144392.966553 });

            m_exCalibrationParams.Add(new CalibrationParam() { presureAvg = -265858.391053973, presureFlowScale = SAMPLE_RATE / 460466733.305481 });
            m_exCalibrationParams.Add(new CalibrationParam() { presureAvg = -516188.103814442, presureFlowScale = SAMPLE_RATE / 455794095.668152 });
            m_exCalibrationParams.Add(new CalibrationParam() { presureAvg = -1062427.31172462, presureFlowScale = SAMPLE_RATE / 448344325.547791 });
            m_exCalibrationParams.Add(new CalibrationParam() { presureAvg = -1220163.68925883, presureFlowScale = SAMPLE_RATE / 452680728.7150274 });
            m_exCalibrationParams.Add(new CalibrationParam() { presureAvg = -2030649.94736844, presureFlowScale = SAMPLE_RATE / 456896238.157898 });

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

                PresureRecved?.Invoke(channel, presure); // 触发压差收取事件
            });
        }

        /* 添加校准参数(自动插入到对应的校准参数列表中) */
        public void AddCalibrationParam(CalibrationParam calParam)
        {
            if (calParam.presureAvg > 0)
            { // 吸气
                /* 插入m_inCalibrationParams且保证列表仍按presure升序排列 */
                int i = 0;
                for (; i < m_inCalibrationParams.Count; ++i)
                {
                    if (calParam.presureAvg < m_inCalibrationParams[i].presureAvg)
                    {
                        /* 按序插入 */
                        m_inCalibrationParams.Insert(i, calParam);
                        break;
                    }
                    else if (calParam.presureAvg == m_inCalibrationParams[i].presureAvg)
                    {
                        /* 替换 */
                        m_inCalibrationParams[i] = calParam;
                        break;
                    }
                }

                if (i >= m_inCalibrationParams.Count)
                {
                    m_inCalibrationParams.Add(calParam);
                }
            }
            else
            { // 呼气
                /* 插入m_exCalibrationParams且保证列表仍按presure降序排列 */
                int i = 0;
                for (; i < m_exCalibrationParams.Count; ++i)
                {
                    if (calParam.presureAvg > m_exCalibrationParams[i].presureAvg)
                    {
                        /* 按序插入 */
                        m_inCalibrationParams.Insert(i, calParam);
                        break;
                    }
                    else if (calParam.presureAvg == m_exCalibrationParams[i].presureAvg)
                    {
                        /* 替换 */
                        m_exCalibrationParams[i] = calParam;
                        break;
                    }
                }

                if (i >= m_exCalibrationParams.Count)
                {
                    m_exCalibrationParams.Add(calParam);
                }
            }
        }

        /* 清空校准参数列表 */
        public void ClearCalibrationParams()
        {
            m_inCalibrationParams.Clear();
            m_exCalibrationParams.Clear();
        }

        /* 添加校准参数 */
        public void AddCalibrationParams(params CalibrationParam[] calParams)
        {
            /* 添加新的校准参数 */
            foreach (var p in calParams)
            {
                AddCalibrationParam(p);
            }
        }

        /* 枚举吸气校准参数列表 */
        public IEnumerable<CalibrationParam> InCalibrationParams()
        {
            foreach(var p in m_inCalibrationParams)
            {
                yield return p;
            }
        }

        /* 枚举呼气校准参数列表 */
        public IEnumerable<CalibrationParam> EnCalibrationParams()
        {
            foreach (var p in m_exCalibrationParams)
            {
                yield return p;
            }
        }

        /* 获取压差对应的压差转流量比例系数 */
        private double GetPresureFlowScale(double presure)
        {
            if (presure > 0)
            { // 吸气
                if (m_inCalibrationParams.Count <= 0)
                {
                    return 1.0;
                }

                /* 搜索presure所属区间(i-1, i) */
                int i = 0;
                for (; i < m_inCalibrationParams.Count; ++i)
                {
                    var param = m_inCalibrationParams[i];
                    if (presure <= param.presureAvg)
                    {
                        break;
                    }
                }

                /* 计算presureFlowScale值 */
                double presureFlowScale = 1.0;
                if (i <= 0)
                {
                    var param = m_inCalibrationParams[i];
                    presureFlowScale = param.presureFlowScale;
                }
                else if (i >= m_inCalibrationParams.Count)
                {
                    var param = m_inCalibrationParams[m_inCalibrationParams.Count - 1];
                    presureFlowScale = param.presureFlowScale;
                }
                else
                {
                    /* 使用相邻的k值线性插值 */
                    var paramPrev = m_inCalibrationParams[i - 1];
                    var param = m_inCalibrationParams[i];
                    presureFlowScale = ((param.presureFlowScale - paramPrev.presureFlowScale) * (presure - paramPrev.presureAvg) / (param.presureAvg - paramPrev.presureAvg)) + paramPrev.presureFlowScale;
                }
                
                return presureFlowScale;
            }
            else
            { // 呼气
                if (m_exCalibrationParams.Count <= 0)
                {
                    return 1.0;
                }

                /* 搜索presure所属区间(i-1, i) */
                int i = 0;
                for (; i < m_exCalibrationParams.Count; ++i)
                {
                    var param = m_exCalibrationParams[i];
                    if (presure >= param.presureAvg)
                    {
                        break;
                    }
                }

                /* 计算K值 */
                double presureFlowScale = 1.0;
                if (i <= 0)
                {
                    var param = m_exCalibrationParams[i];
                    presureFlowScale = param.presureFlowScale;
                }
                else if (i >= m_exCalibrationParams.Count)
                {
                    var param = m_exCalibrationParams[m_exCalibrationParams.Count - 1];
                    presureFlowScale = param.presureFlowScale;
                }
                else
                {
                    /* 使用相邻的k值线性插值 */
                    var paramPrev = m_exCalibrationParams[i - 1];
                    var param = m_exCalibrationParams[i];
                    presureFlowScale = ((param.presureFlowScale - paramPrev.presureFlowScale) * (presure - paramPrev.presureAvg) / (param.presureAvg - paramPrev.presureAvg)) + paramPrev.presureFlowScale;
                }
                
                return presureFlowScale;
            }
        }

        /* 压差转流量,单位:L/S */
        public double PresureToFlow(double presure)
        {
            //presure = m_kalmanFilter.Input((float)presure); // 执行滤波
            /* 压差转流量 */
            double presureFlowScale = GetPresureFlowScale(presure);
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
