using log4net;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RTCMClient.helper
{
    public class TcpClientHelper
    {
        private static readonly ILog logger = LogManager.GetLogger(nameof(TcpClientHelper));
        private readonly string ipAddress;
        private readonly int port;
        private TcpClient tcpClient = null;
        private NetworkStream networkStream = null;
        private CancellationTokenSource cts = null;
        private readonly Action<byte[], int> doAfterGetPacket = null;

        public bool IsConnet
        {
            get; set;
        }

        public TcpClientHelper(string ip, int port, Action<byte[], int> action)
        {
            ipAddress = ip;
            this.port = port;
            doAfterGetPacket = action;
        }

        private DateTime lastPacketTime = DateTime.Now;
        private readonly byte[] buffer = new byte[2000];
        private int dateLen = 0;

        public void Start()
        {
            cts = new CancellationTokenSource();
            try
            {
                tcpClient = new TcpClient();
                var task1 = tcpClient.ConnectAsync(ipAddress, port);//给1s的连接时间，超过时间则超时,认为连接失败
                var task2 = Task.Delay(1000);
                var tasks = new Task[] { task1, task2 };
                var result = Task.WhenAny(tasks);
                if (!tcpClient.Connected)
                {
                    IsConnet = false;
                    return;
                }
                logger.Info("网络连接成功");
                networkStream = tcpClient.GetStream();
                _ = Task.Run(async () =>
                  {
                      while (true)
                      {
                          lastPacketTime = DateTime.Now;
                          cts.Token.ThrowIfCancellationRequested();
                          int receDataLen = await networkStream.ReadAsync(buffer, dateLen, 1024);
                          if (DateTime.Now.Subtract(lastPacketTime).TotalMilliseconds > 200)//认为两次包间隔大于200ms，上一包为完整的包
                          {
                              lastPacketTime = DateTime.Now;
                              doAfterGetPacket(buffer, dateLen - receDataLen);
                              dateLen = receDataLen;
                          }
                          else
                          {
                              lastPacketTime = DateTime.Now;
                              dateLen += receDataLen;
                          }
                      }
                  }, cts.Token);
            }
            catch (Exception ex)
            {
                logger.Error($"连接远程主机失败", ex);
            }
        }

        public void Stop()
        {
            try
            {
                cts.Cancel();
                if (networkStream != null)
                    networkStream.Close();
                if (tcpClient != null)
                    tcpClient.Close();
                logger.Info("网络关闭成功");
            }
            catch (Exception e)
            {
                logger.Error($"关闭网络连接失败", e);
            }
        }
    }
}