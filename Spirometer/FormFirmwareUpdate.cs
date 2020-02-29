using PulmonaryFunctionLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Spirometer
{
    public partial class FormFirmwareUpdate : Form
    {
        private static readonly int ReTryMax = 10; 
        private static readonly int PacketLen = 1024;

        private DateTime startTime;

        private FlowSensor m_flowSensor; // 流量传感器

        private IFileTramsmit FileTransProtocol;
        private int fileIndex = 0;
        private int packetNo = 0;
        private byte[] PacketBuff = new byte[PacketLen];

        public event EventHandler StartTransmitFile;
        public event EventHandler EndTransmitFile;
        public event SendToUartEventHandler SendToUartEvent;

        public FormFirmwareUpdate(FlowSensor flowSensor)
        {
            m_flowSensor = flowSensor;

            InitializeComponent();
        }

        public void LoadConfig()
        {
            textBoxFilePath.Text = Properties.Settings.Default.filePath;
        }

        public void SaveConfig()
        {
            Properties.Settings.Default.filePath = textBoxFilePath.Text;

            Properties.Settings.Default.Save();
        }

        private async void FormFirmwareUpdate_Load(object sender, EventArgs e)
        {
            labelInfo.Text = "";

            LoadConfig();

            /* 获取固件版本号 */
            string ver = await m_flowSensor.BoardVersionAsync();
            ShowTextReprot($"固件版本号：{ver}");
        }

        private void FormFirmwareUpdate_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_flowSensor.DataRecved -= OnUartDataRecved;

            SaveConfig();
        }

        public async Task<bool> Start()
        {
            if (textBoxFilePath.Text == string.Empty)
            {
                return false;
            }

            /* 启动OTA下载 */
            bool bRet = await m_flowSensor.StartOTADownloadAsync();
            if (!bRet)
            {
                return false;
            }

            m_flowSensor.DataRecved += OnUartDataRecved;

            FileTransProtocol = new YModem(TransmitMode.Send, YModemType.YModem_1K, ReTryMax);
            FileTransProtocol.EndOfTransmit += FileTransProtocol_EndOfTransmit;
            FileTransProtocol.AbortTransmit += FileTransProtocol_AbortTransmit;
            FileTransProtocol.ReSendPacket += FileTransProtocol_ReSendPacket;
            FileTransProtocol.SendNextPacket += FileTransProtocol_SendNextPacket;
            FileTransProtocol.TransmitTimeOut += FileTransProtocol_TransmitTimeOut;
            FileTransProtocol.StartSend += FileTransProtocol_StartSend;
            FileTransProtocol.SendToUartEvent += FileTransProtocol_SendToUartEvent;

            packetNo = 1;
            fileIndex = 0;
            FileTransProtocol.Start();

            if (StartTransmitFile != null)
            {
                StartTransmitFile(this, null);
            }

            return true;
        }

        public void Stop()
        {

            FileTransProtocol.Abort();

            SetEndTransmit();

            FileTransProtocol.EndOfTransmit -= FileTransProtocol_EndOfTransmit;
            FileTransProtocol.AbortTransmit -= FileTransProtocol_AbortTransmit;
            FileTransProtocol.ReSendPacket -= FileTransProtocol_ReSendPacket;
            FileTransProtocol.SendNextPacket -= FileTransProtocol_SendNextPacket;
            FileTransProtocol.TransmitTimeOut -= FileTransProtocol_TransmitTimeOut;
            FileTransProtocol.StartSend -= FileTransProtocol_StartSend;
            FileTransProtocol.SendToUartEvent -= FileTransProtocol_SendToUartEvent;
            FileTransProtocol = null;

            m_flowSensor.DataRecved -= OnUartDataRecved;
        }

        public void ReceivedFromUart(byte[] data)
        {
            FileTransProtocol?.ReceivedFromUart(data);
        }

        private void ShowTextReprot(string text)
        {
            if (labelInfo.InvokeRequired)
            {
                labelInfo.BeginInvoke(new MethodInvoker(delegate
                {
                    ShowTextReprot(text);
                }));
            }
            else
            {
                labelInfo.Text = text;
            }
        }

        private void ShowProgressReport(bool IsShow, int value, int max)
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.BeginInvoke(new MethodInvoker(delegate
                {
                    ShowProgressReport(IsShow, value, max);
                }));
            }
            else
            {
                progressBar1.Visible = IsShow;
                progressBar1.Maximum = max;
                progressBar1.Value = value;
            }
        }

        private void SetEndTransmit()
        {
            ShowProgressReport(false, 0, 100);

            if (EndTransmitFile != null)
            {
                EndTransmitFile(this, null);
            }
        }

        private int ReadPacketFromFile(int filePos, byte[] data, int packetLen)
        {

            FileStream fs = null;
            BinaryReader br = null;
            try
            {
                fs = new FileStream(textBoxFilePath.Text, FileMode.Open, FileAccess.Read);
                br = new BinaryReader(fs);

                if (filePos < fs.Length)
                {
                    fs.Seek(filePos, SeekOrigin.Begin);
                    int len = br.Read(data, 0, packetLen);

                    ShowProgressReport(true, filePos, (int)fs.Length);

                    return len;
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception ex)
            {
                throw ex;
                //MessageBox.Show(ex.Message, oFileDlg.Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (br != null) br.Close();
                if (fs != null) fs.Close();
            }
        }

        /* 收到串口数据 */
        private void OnUartDataRecved(byte[] data)
        {
            ReceivedFromUart(data);
        }

        void FileTransProtocol_SendToUartEvent(object sender, SendToUartEventArgs e)
        {
            /* 发送数据到串口 */
            m_flowSensor.SendData(e.Data);

            if (SendToUartEvent != null)
            {
                SendToUartEvent(sender, e);
            }
        }

        void FileTransProtocol_StartSend(object sender, EventArgs e)
        {
            packetNo = 0;

            FileInfo fileInfo = new FileInfo(textBoxFilePath.Text);

            byte[] fileNameBytes = System.Text.ASCIIEncoding.Default.GetBytes(fileInfo.Name);

            int index = 0;
            Array.Copy(fileNameBytes, 0, PacketBuff, 0, fileNameBytes.Length);
            index += fileNameBytes.Length;
            PacketBuff[index] = 0;
            index++;
            byte[] fileSizeBytes = System.Text.ASCIIEncoding.Default.GetBytes(fileInfo.Length.ToString());
            Array.Copy(fileSizeBytes, 0, PacketBuff, index, fileSizeBytes.Length);

            FileTransProtocol?.SendPacket(new PacketEventArgs(0, PacketBuff));

            fileIndex = 0;
            packetNo = 0;

            //fileInfo.Name;
            startTime = DateTime.Now;

            ShowTextReprot(string.Format("开始发送数据"));
        }

        void FileTransProtocol_TransmitTimeOut(object sender, EventArgs e)
        {
            ShowTextReprot(string.Format("传输超时"));
            SetEndTransmit();
        }

        void FileTransProtocol_SendNextPacket(object sender, EventArgs e)
        {
            ShowTextReprot(string.Format("开始传输第{0}包数据", packetNo));

            packetNo++;
            fileIndex += PacketLen;

            if (packetNo == 1)
            {
                fileIndex = 0;
            }

            int readBytes = ReadPacketFromFile(fileIndex, PacketBuff, PacketLen);
            if (readBytes <= 0)
            {
                FileTransProtocol?.Stop();
            }
            else
            {
                if (readBytes < PacketLen)
                {
                    for (int i = readBytes; i < PacketLen; i++)
                    {
                        PacketBuff[i] = 0x1A;
                    }
                }
                FileTransProtocol?.SendPacket(new PacketEventArgs(packetNo, PacketBuff));
            }
        }

        void FileTransProtocol_ReSendPacket(object sender, EventArgs e)
        {
            FileTransProtocol?.SendPacket(new PacketEventArgs(packetNo, PacketBuff));
            ShowTextReprot(string.Format("重发第{0}包数据", packetNo));
        }

        void FileTransProtocol_AbortTransmit(object sender, EventArgs e)
        {
            SetEndTransmit();
            ShowTextReprot(string.Format("传输中止"));
        }

        void FileTransProtocol_EndOfTransmit(object sender, EventArgs e)
        {
            SetEndTransmit();

            TimeSpan ts = DateTime.Now - startTime;
            ShowTextReprot(string.Format("传输完成，用时{0:D2}:{1:D2}:{2:D2}.{3:D3}. 正在自动重启下位机...", 
                ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds));

            this.BeginInvoke(new Action<FormFirmwareUpdate>(async (obj) => {

                m_flowSensor.DataRecved -= OnUartDataRecved;

                bool bRet = await m_flowSensor.StartOTARebootAsync();
                if (bRet)
                {
                    /* 重启前关闭串口 */
                    m_flowSensor.Close();

                    /* 等待重启完成 */
                    await Task.Delay(10000);

                    /* 重新开启串口 */
                    bRet = m_flowSensor.Open();
                    if (bRet)
                    {
                        /* 获取固件版本号 */
                        string ver = await m_flowSensor.BoardVersionAsync();

                        ShowTextReprot(string.Format("传输完成，用时{0:D2}:{1:D2}:{2:D2}.{3:D3}. 下位机重启完成，固件版本号: " + ver,
                            ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds));
                    }
                    else
                    {
                        ShowTextReprot(string.Format("传输完成，用时{0:D2}:{1:D2}:{2:D2}.{3:D3}. 获取下位机版本号失败!",
                            ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds));
                    }

                    buttonStart.Text = "关闭";
                }
                else
                {
                    ShowTextReprot(string.Format("传输完成，用时{0:D2}:{1:D2}:{2:D2}.{3:D3}. 自动重启下位机失败！",
                        ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds));
                }
            }), this);
        }

        private void buttonSelectFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog oFileDlg = new OpenFileDialog();

            oFileDlg.Filter = "固件包(*.rbl)|*.rbl|所有文件(*.*)|*.*";
            if (oFileDlg.ShowDialog() == DialogResult.OK)
            {
                textBoxFilePath.Text = oFileDlg.FileName;
            }
        }

        private async void buttonStart_Click(object sender, EventArgs e)
        {
            if (buttonStart.Text == "开始")
            {
                if (textBoxFilePath.Text == string.Empty)
                {
                    MessageBox.Show("未选择任何文件，请选择文件！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    bool bRet = await Start();
                    if (!bRet)
                    {
                        MessageBox.Show("启动固件升级失败!", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        buttonStart.Text = "停止";
                    }
                }
            }
            else if (buttonStart.Text == "停止")
            {
                Stop();
                buttonStart.Text = "开始";
            }
            else if (buttonStart.Text == "关闭")
            {
                this.Close();
            }
        }
    }
}
