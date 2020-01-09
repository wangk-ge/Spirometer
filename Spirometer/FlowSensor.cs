using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Threading.Tasks;

namespace Spirometer
{
    /* 流速传感器 */
    class FlowSensor
    {
        private SerialPort m_serialPort = null;
        private ConcurrentQueue<TaskCompletionSource<string>> m_cmdRespTaskCompQue = new ConcurrentQueue<TaskCompletionSource<string>>();
        private readonly double m_presureFlowRatio = 1333; // 压差转流量系数(转出来的单位是ml/s)
        private readonly double m_sampleRate = 330; // 采样率,单位:HZ
        private FrameDecoder m_frameDecoder = new FrameDecoder(); // 串口数据帧解码器
        private KalmanFilter m_kalmanFilter = new KalmanFilter(0.01f/*Q*/, 0.1f/*R*/, 10.0f/*P*/, 0); // 卡尔曼滤波器

        public delegate void FlowRecvHandler(byte channel, double flow); // 流量接收代理
        public event FlowRecvHandler FlowRecved; // 流量收取事件

        /* 采样时间,单位:MS */
        public double SampleTime { get { return (1000 / m_sampleRate); } }

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

                double flow = PresureToFlow(presure); // 压差转流量
                FlowRecved?.Invoke(channel, flow); // 触发流量收取事件
            });
        }

        /* 压差转流量,单位:L/S */
        public double PresureToFlow(double presure)
        {
            //presure = m_kalmanFilter.Input((float)presure); // 执行滤波
            double flow = presure / (m_presureFlowRatio * 1000.0); // 压差转流量
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
    }
}
