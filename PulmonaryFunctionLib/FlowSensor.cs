using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading.Tasks;

namespace PulmonaryFunctionLib
{
    /* 流速传感器 */
    public class FlowSensor
    {
        private string m_portName = null;
        private SerialPort m_serialPort = null;
        private ConcurrentQueue<TaskCompletionSource<string>> m_cmdRespTaskCompQue = new ConcurrentQueue<TaskCompletionSource<string>>();
        private FrameDecoder m_frameDecoder = new FrameDecoder(); // 串口数据帧解码器
        //private KalmanFilter m_kalmanFilter = new KalmanFilter(0.01f/*Q*/, 0.1f/*R*/, 10.0f/*P*/, 0); // 卡尔曼滤波器
        private RollingAverageFilter m_avgFilter; // 滑动平均滤波器

        /* 采样率,单位:HZ */
        public readonly double SAMPLE_RATE = 330;
        /* 采样时间,单位:MS */
        public double SAMPLE_TIME { get { return (1000 / SAMPLE_RATE); } }

        public delegate void DataRecvHandler(byte[] data); // 数据接收代理
        public event DataRecvHandler DataRecved; // 数据收取事件

        public delegate void PresureRecvHandler(byte channel, double presure); // 压差接收代理
        public event PresureRecvHandler PresureRecved; // 压差收取事件

        /* 校准参数列表 */
        private List<double> m_calParamValList = new List<double>(); // 全局
        private List<double> m_calParamValListP = new List<double>(); // 正方向
        private List<double> m_calParamValListN = new List<double>(); // 负方向
        private double m_minPresure = 0.0; // 最小压差
        private double m_maxPresure = 0.0; // 最大压差

        public FlowSensor()
        {
            m_avgFilter = new RollingAverageFilter((int)(16.0 / SAMPLE_TIME)); // 滑动平均滤波器(16ms)

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

                //presure = m_kalmanFilter.Input((float)presure); // 执行滤波
                presure = m_avgFilter.Input((float)presure); // 执行滤波

                PresureRecved?.Invoke(channel, presure); // 触发压差收取事件
            });
        }

        public void SetCalibrationParamList(List<double> paramValList, List<double> paramValListP, List<double> paramValListN, double minPresure, double maxPresure)
        {
            m_calParamValList.Clear();
            m_calParamValList.AddRange(paramValList);

            m_calParamValListP.Clear();
            m_calParamValListP.AddRange(paramValListP);

            m_calParamValListN.Clear();
            m_calParamValListN.AddRange(paramValListN);

            m_minPresure = minPresure;
            m_maxPresure = maxPresure;
        }

        /* 执行压差转流量(单位:L/S) */
        public double PresureToFlow(double presure)
        {
            /* 压差转流量 */
            double flow = FlowCalibrator.PresureToFlow(presure, m_calParamValList, 
                m_calParamValListP, m_calParamValListN,
                m_minPresure, m_maxPresure);
            return flow;
        }

        public bool Open(string portName = null)
        {
            try
            {
                m_serialPort?.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            m_serialPort?.Dispose();

            try
            {
                if (portName == null)
                {
                    portName = m_portName;
                }
                else
                {
                    m_portName = portName;
                }
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

            DataRecved?.Invoke(dataBuf);

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

        /* 发送数据 */
        public void SendData(byte[] data)
        {
            m_serialPort?.Write(data, 0, data.Length);
        }

        /* 创建CMD Task */
        private Task<string> ExcuteCmdTask(string cmd)
        {
            Console.WriteLine($"ExcuteCmdTask: {cmd}");

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
        public async Task<bool> StartAsync(int timeOut = 2000)
        {
            string cmd = "[ADC_START]"; // 启动采集
            string cmdResp = await ExcuteCmdAsync(cmd, timeOut);
            return cmdResp == "[OK]";
        }

        /* 请求启动采集(同步版本) */
        public bool Start(int timeOut = 2000)
        {
            string cmd = "[ADC_START]"; // 启动采集
            string cmdResp = ExcuteCmd(cmd, timeOut);
            return cmdResp == "[OK]";
        }

        /* 请求停止采集(异步版本) */
        public async Task<bool> StopAsync(int timeOut = 2000)
        {
            string cmd = "[ADC_STOP]"; // 启动采集
            string cmdResp = await ExcuteCmdAsync(cmd, timeOut);
            return cmdResp == "[OK]";
        }

        /* 请求停止采集(同步版本) */
        public bool Stop(int timeOut = 2000)
        {
            string cmd = "[ADC_STOP]"; // 启动采集
            string cmdResp = ExcuteCmd(cmd, timeOut);
            return cmdResp == "[OK]";
        }

        /* 请求执行归零(异步版本) */
        public async Task<bool> ZeroingAsync(int timeOut = 2000)
        {
            string cmd = "[ADC_CAL]"; // 归零
            string cmdResp = await ExcuteCmdAsync(cmd, timeOut);
            return cmdResp == "[OK]";
        }

        /* 请求执行归零(同步版本) */
        public bool Zeroing(int timeOut = 2000)
        {
            string cmd = "[ADC_CAL]"; // 归零
            string cmdResp = ExcuteCmd(cmd, timeOut);
            return cmdResp == "[OK]";
        }

        /* 取得下位机版本号(异步版本) */
        public async Task<string> BoardVersionAsync(int timeOut = 2000)
        {
            string cmd = "[VER]"; // 取得版本号
            string cmdResp = await ExcuteCmdAsync(cmd, timeOut);
            return cmdResp;
        }

        /* 取得下位机版本号(同步版本) */
        public string BoardVersion(int timeOut = 2000)
        {
            string cmd = "[VER]"; // 取得版本号
            string cmdResp = ExcuteCmd(cmd, timeOut);
            return cmdResp;
        }

        /* 取得下位机固件编译时间(异步版本) */
        public async Task<string> BoardBuildTimeAsync(int timeOut = 2000)
        {
            string cmd = "[BUILD]"; // 取得固件编译时间
            string cmdResp = await ExcuteCmdAsync(cmd, timeOut);
            return cmdResp;
        }

        /* 取得下位机固件编译时间(同步版本) */
        public string BoardBuildTime(int timeOut = 2000)
        {
            string cmd = "[BUILD]"; // 取得固件编译时间
            string cmdResp = ExcuteCmd(cmd, timeOut);
            return cmdResp;
        }

        /* 取得环境温度(异步版本) */
        public async Task<double> AmbientTemperatureAsync(int timeOut = 2000)
        {
            string cmd = "[BME280_TEMP]"; // 取得环境温度
            string cmdResp = await ExcuteCmdAsync(cmd, timeOut);
            if (cmdResp != string.Empty)
            {
                return Convert.ToDouble(cmdResp);
            }
            return 0.0;
        }

        /* 取得环境温度(同步版本) */
        public double AmbientTemperature(int timeOut = 2000)
        {
            string cmd = "[BME280_TEMP]"; // 取得环境温度
            string cmdResp = ExcuteCmd(cmd, timeOut);
            if (cmdResp != string.Empty)
            {
                return Convert.ToDouble(cmdResp);
            }
            return 0.0;
        }

        /* 取得环境湿度(异步版本) */
        public async Task<double> AmbientHumidityAsync(int timeOut = 2000)
        {
            string cmd = "[BME280_HUMI]"; // 取得环境湿度
            string cmdResp = await ExcuteCmdAsync(cmd, timeOut);
            if (cmdResp != string.Empty)
            {
                return Convert.ToDouble(cmdResp);
            }
            return 0.0;
        }

        /* 取得环境湿度(同步版本) */
        public double AmbientHumidity(int timeOut = 2000)
        {
            string cmd = "[BME280_HUMI]"; // 取得环境湿度
            string cmdResp = ExcuteCmd(cmd, timeOut);
            if (cmdResp != string.Empty)
            {
                return Convert.ToDouble(cmdResp);
            }
            return 0.0;
        }

        /* 取得大气压(异步版本) */
        public async Task<double> BarometricAsync(int timeOut = 2000)
        {
            string cmd = "[BME280_BARO]"; // 取得大气压
            string cmdResp = await ExcuteCmdAsync(cmd, timeOut);
            if (cmdResp != string.Empty)
            {
                return Convert.ToDouble(cmdResp);
            }
            return 0.0;
        }

        /* 取得大气压(同步版本) */
        public double Barometric(int timeOut = 2000)
        {
            string cmd = "[BME280_BARO]"; // 取得大气压
            string cmdResp = ExcuteCmd(cmd, timeOut);
            if (cmdResp != string.Empty)
            {
                return Convert.ToDouble(cmdResp);
            }
            return 0.0;   
        }

        /* 启动OTA下载(异步版本) */
        public async Task<bool> StartOTADownloadAsync(int timeOut = 2000)
        {
            string cmd = "[OTA_DOWNLOAD]"; // 启动OTA下载
            string cmdResp = await ExcuteCmdAsync(cmd, timeOut);
            return cmdResp == "[OK]";
        }

        /* 启动OTA下载(同步版本) */
        public bool StartOTADownload(int timeOut = 2000)
        {
            string cmd = "[OTA_DOWNLOAD]"; // 启动OTA下载
            string cmdResp = ExcuteCmd(cmd, timeOut);
            return cmdResp == "[OK]";
        }

        /* 启动OTA重启(异步版本) */
        public async Task<bool> StartOTARebootAsync(int timeOut = 2000)
        {
            string cmd = "[OTA_REBOOT]"; // 启动OTA重启
            string cmdResp = await ExcuteCmdAsync(cmd, timeOut);
            return cmdResp == "[OK]";
        }

        /* 启动OTA重启(同步版本) */
        public bool StartOTAReboot(int timeOut = 2000)
        {
            string cmd = "[OTA_REBOOT]"; // 启动OTA重启
            string cmdResp = ExcuteCmd(cmd, timeOut);
            return cmdResp == "[OK]";
        }
    }
}
