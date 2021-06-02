using log4net;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RTCMClient.helper
{
    public class TcpClientHelper
    {
        private static readonly ILog logger = LogManager.GetLogger(nameof(TcpClientHelper));
        private string ipAddress;
        private int port;
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
            IsConnet = false;
        }

        public void Start()
        {
            cts = new CancellationTokenSource();
            try
            {
                tcpClient = new TcpClient(ipAddress, port);
                IsConnet = true;
                logger.Info("网络连接成功");
                networkStream = tcpClient.GetStream();
                Task.Run(async () =>
                {
                    while (true)
                    {
                        cts.Token.ThrowIfCancellationRequested();
                        byte[] data = new byte[1024];
                        int receDataLen = await networkStream.ReadAsync(data, 0, data.Length);
                        //byte[] recvDataBuffer = new byte[receDataLen];
                        //Array.Copy(data, recvDataBuffer, receDataLen);
                        doAfterGetPacket(data, receDataLen);
                    }
                }, cts.Token);
            }
            catch (Exception ex)
            {
                logger.Error($"连接远程主机失败", ex);
                MessageBox.Show($"连接远程主机失败，请检查相关设置和网络连接！");
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