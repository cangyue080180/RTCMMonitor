using log4net;
using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RTCMClient.helper
{
    //串口帮助类
    public class SerialPortHelper
    {
        private static readonly ILog logger = LogManager.GetLogger(nameof(SerialPortHelper));

        private bool portClosing = false;
        private bool listening = false;
        private readonly SerialPort serialPort1 = new SerialPort();
        private readonly Action<string> doAfterGetPacket = null;

        public string PortName { get; private set; }
        public bool IsPortOpen => serialPort1.IsOpen;
        private CancellationTokenSource cts;

        public SerialPortHelper(string com, int baudRate, bool receData = false, Action<string> doWithRec = null)
        {
            if (string.IsNullOrEmpty(com.Trim()))
            {
                PortName = "";
                return;
            }
            PortName = com.Trim();
            serialPort1.PortName = com.Trim();
            serialPort1.BaudRate = baudRate;
            //serialPort1.RtsEnable = true;
            if (receData)
            {
                doAfterGetPacket = doWithRec;
            }
        }

        private void StartRecvMsg()
        {
            logger.Info($"{PortName} 开始接收消息");
            cts = new CancellationTokenSource();
            Task.Run(() =>
            {
                while (true)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    try
                    {
                        string msg = serialPort1.ReadLine();
                        doAfterGetPacket(msg);
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"{PortName} 接收消息异常", ex);
                    }
                }
            }, cts.Token);
        }

        public void StopRecvMsg()
        {
            if (cts != null)
                cts.Cancel();
            logger.Info($"{PortName} 停止接收消息");
        }

        public void Start()
        {
            if (doAfterGetPacket != null)
            {
                StartRecvMsg();
            }
        }

        public void OpenSerialPort()
        {
            try
            {
                if (!this.serialPort1.IsOpen)
                {
                    this.serialPort1.Open();
                    portClosing = false;
                }

                //监听串口状态，尝试断线重连
                Task.Run(async () =>
                {
                    while (!portClosing)
                    {
                        try
                        {
                            if (!serialPort1.IsOpen)
                                serialPort1.Open();
                        }
                        catch (Exception e)
                        {
                            logger.Warn($"{PortName} 串口重连失败", e);
                        }
                        await Task.Delay(1000);
                    }
                });
                logger.Info($"{PortName} 打开成功");
            }
            catch (Exception ex)
            {
                logger.Error($"{PortName} 打开失败", ex);
                MessageBox.Show($"串口 {serialPort1.PortName} 打开失败");
            }
        }

        public void CloseSerialPort()
        {
            portClosing = true;
            StopRecvMsg();
            try
            {
                if (this.serialPort1.IsOpen)
                {
                    while (listening) DispatcherHelper.DoEvents();
                    this.serialPort1.Close();
                    logger.Info($"{PortName} 关闭成功");
                }
            }
            catch (Exception e)
            {
                logger.Warn($"{PortName} 串口关闭失败", e);
            }
        }

        public void SendMsg(string msg)
        {
            try
            {
                if (this.serialPort1.IsOpen)
                {
                    serialPort1.WriteLine(msg);
                }
            }
            catch (Exception ex)
            {
                logger.Warn($"{PortName} 串口发送数据失败", ex);
            }
        }

        public void SendData(byte[] data, int len)
        {
            try
            {
                if (this.serialPort1.IsOpen)
                {
                    serialPort1.Write(data, 0, len);

                    if (logger.IsDebugEnabled)
                    {
                        StringBuilder recedata = new StringBuilder();
                        for (int i = 0; i < len; i++)
                        {
                            recedata.Append(data[i].ToString("x"));
                            recedata.Append(' ');
                        }
                        logger.Debug($"send: {recedata?.ToString()}");
                    }
                }
            }
            catch (Exception e)
            {
                logger.Warn($"{PortName} 串口发送数据失败", e);
            }
        }

        private readonly byte[] receBuffer = new byte[30 * 1024];
        private int datalen = 0;

        private void SerialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (portClosing) return;//如果正关闭串口，不进行下一操作，尽快完成串口监听线程的循环
            listening = true;

            int readyToReadLen = serialPort1.BytesToRead;
            int lastIndex = datalen;

            try
            {
                datalen += serialPort1.Read(receBuffer, datalen, readyToReadLen);
            }
            catch (ArgumentException)//偏移量和长度超出数组的界限
            {
                //主动丢弃无效数据
                logger.Info("数据索引超出缓存，主动丢弃数据，请考虑扩展缓存空间。");
                datalen = 0;
            }
            logger.Debug($"{serialPort1.PortName} recv data ,now databufferLen: {datalen}, lastIndex:{lastIndex}");

            if (logger.IsDebugEnabled)
            {
                StringBuilder recedata = new StringBuilder();
                for (int i = lastIndex; i < datalen; i++)
                {
                    recedata.Append(receBuffer[i].ToString("x"));
                    recedata.Append(' ');
                }
                logger.Debug($"recv: {recedata?.ToString()}");
            }

            FindSlipPacket();

            listening = false;
            //Console.WriteLine($"readLen: {readLength}");//for debug
        }

        private bool isFindHeader = false;

        private void FindSlipPacket()
        {
            if (!isFindHeader)
            {
                //寻找包头
                for (int i = 0; i < datalen - 1; i++)
                {
                    if (receBuffer[i] == 0xfa && receBuffer[i + 1] == 0xf5)
                    {
                        logger.Debug($"{serialPort1.PortName} find packetHeader");
                        isFindHeader = true;
                        //移除包头之前的无效内容
                        datalen -= i;
                        Array.Copy(receBuffer, i, receBuffer, 0, datalen);

                        logger.Debug($"{serialPort1.PortName} databufferLen {datalen}");
                        break;
                    }
                }
                //遍历了所有缓存数据没有发现包头，则清除无效数据，避免下次重新遍历
                if (!isFindHeader)
                    return;
            }
            int packetLen;
            //寻找包尾
            if (datalen > 3)
            {
                packetLen = receBuffer[3] + 6;
                if (datalen >= packetLen)
                {
                    if (receBuffer[packetLen - 1] == 0xf0)
                    {
                        //完整包
                        byte[] sendDataBuffer = new byte[packetLen];
                        Array.Copy(receBuffer, 0, sendDataBuffer, 0, sendDataBuffer.Length);
                        //doAfterGetPacket(sendDataBuffer);
                        isFindHeader = false;

                        //从缓存区中移除被发现的完整包内容，重置缓存区数据存放位置
                        datalen -= packetLen;
                        Array.Copy(receBuffer, packetLen, receBuffer, 0, datalen);
                    }
                    else
                    {
                        //无效包
                        //从缓存区中移除被发现的完整包内容，重置缓存区数据存放位置
                        datalen -= packetLen;
                        Array.Copy(receBuffer, packetLen, receBuffer, 0, datalen);
                    }
                }
                else
                {
                    return;
                }
            }

            //寻找包尾
            //for (int i = 2; i < datalen; i++)
            //{
            //    if (receBuffer[i] == 0xf0)
            //    {
            //        logger.Debug($"{serialPort1.PortName} find end, databufferLen {datalen}");
            //        //找到包尾,将完整的包内容拷贝出来
            //        byte[] sendDataBuffer = new byte[i + 1];
            //        Array.Copy(receBuffer, 0, sendDataBuffer, 0, sendDataBuffer.Length);
            //        doAfterGetPacket(sendDataBuffer);
            //        isFindHeader = false;

            //        //从缓存区中移除被发现的完整包内容，重置缓存区数据存放位置
            //        datalen -= (i + 1);
            //        Array.Copy(receBuffer, i + 1, receBuffer, 0, datalen);
            //        logger.Debug($"{serialPort1.PortName} find end,after remove the find packet data, databufferLen {datalen}");
            //        break;
            //    }
            //}

            if (!isFindHeader && datalen > 3)//找到包尾，再遍历数据缓存查看是否还有新的数据包
            {
                FindSlipPacket();
            }
        }
    }
}