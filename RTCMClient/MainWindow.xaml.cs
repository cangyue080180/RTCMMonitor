using log4net;
using RTCMClient.helper;
using System;
using System.ComponentModel;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace RTCMClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private static readonly ILog logger = LogManager.GetLogger(nameof(MainWindow));

        public event PropertyChangedEventHandler PropertyChanged;

        #region 属性和变量定义

        public string[] COMS
        {
            get
            {
                return SerialPort.GetPortNames();
            }
        }

        public int[] Baudrates
        {
            get
            {
                return new int[] { 4800, 9600, 19200, 115200 };
            }
        }

        private string _jieShouJiCom;

        public string JieShouJiCOM
        {
            get
            {
                return _jieShouJiCom;
            }
            set
            {
                if (value != _jieShouJiCom)
                {
                    _jieShouJiCom = value;
                    OnPropertyChanged(nameof(JieShouJiCOM));
                }
            }
        }

        private int _jieShouJiBaudrate;

        public int JieShouJiBaudrate
        {
            get
            {
                return _jieShouJiBaudrate;
            }
            set
            {
                if (value != _jieShouJiBaudrate)
                {
                    _jieShouJiBaudrate = value;
                    OnPropertyChanged(nameof(JieShouJiBaudrate));
                }
            }
        }

        private string _dianTaiCom;

        public string DianTaiCOM
        {
            get
            {
                return _dianTaiCom;
            }
            set
            {
                if (value != _dianTaiCom)
                {
                    _dianTaiCom = value;
                    OnPropertyChanged(nameof(DianTaiCOM));
                }
            }
        }

        private int _dianTaiBaudrate;

        public int DianTaiBaudrate
        {
            get
            {
                return _dianTaiBaudrate;
            }
            set
            {
                if (value != _dianTaiBaudrate)
                {
                    _dianTaiBaudrate = value;
                    OnPropertyChanged(nameof(DianTaiBaudrate));
                }
            }
        }

        private string _ipAddress;

        public string IpAddress
        {
            get
            {
                return _ipAddress;
            }
            set
            {
                if (value != _ipAddress)
                {
                    _ipAddress = value;
                    OnPropertyChanged(nameof(IpAddress));
                }
            }
        }

        private int _port;

        public int Port
        {
            get
            {
                return _port;
            }
            set
            {
                if (value != _port)
                {
                    _port = value;
                    OnPropertyChanged(nameof(Port));
                }
            }
        }

        private string _startBtnContent;

        public string StartBtnContent
        {
            get
            {
                return _startBtnContent;
            }
            set
            {
                if (value != _startBtnContent)
                {
                    _startBtnContent = value;
                    OnPropertyChanged(nameof(StartBtnContent));
                }
            }
        }

        private string _changeBtnContent;

        public string ChangeBtnContent
        {
            get
            {
                return _changeBtnContent;
            }
            set
            {
                if (value != _changeBtnContent)
                {
                    _changeBtnContent = value;
                    OnPropertyChanged(nameof(ChangeBtnContent));
                }
            }
        }

        private bool _editEnable = true;

        public bool EditEnable
        {
            get
            {
                return _editEnable;
            }
            set
            {
                if (value != _editEnable)
                {
                    _editEnable = value;
                    OnPropertyChanged(nameof(EditEnable));
                }
            }
        }

        private string _errorMsg;

        public string ErrorMsg
        {
            get
            {
                return _errorMsg;
            }
            set
            {
                if (value != _errorMsg)
                {
                    _errorMsg = value;
                    OnPropertyChanged(nameof(ErrorMsg));
                }
            }
        }

        private string _status = "White";

        public String Status
        {
            get
            {
                return _status;
            }
            set
            {
                if (value != _status)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        private double _baseLongitude;

        public double BaseLongitude
        {
            get
            {
                return _baseLongitude;
            }
            set
            {
                if (value != _baseLongitude)
                {
                    _baseLongitude = value;
                    OnPropertyChanged(nameof(BaseLongitude));
                }
            }
        }

        private double _baseLatitude;

        public double BaseLatitude
        {
            get
            {
                return _baseLatitude;
            }
            set
            {
                if (value != _baseLatitude)
                {
                    _baseLatitude = value;
                    OnPropertyChanged(nameof(BaseLatitude));
                }
            }
        }

        #endregion 属性和变量定义

        private SerialPortHelper jieShouJi = null;
        private SerialPortHelper dianTai = null;
        private TcpClientHelper tcpClient = null;
        private const string StartTag = "启动";
        private const string StopTag = "停止";
        private const string ChangeTag = "切换";
        private const string StopChangeTag = "停止切换";
        private int errorCount = 0;
        private bool isLastError = false;
        private int limitErrorCount = 0;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitConfig();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Stop();
        }

        private bool Start()
        {
            if (CheckInput())
            {
                SaveConfig();

                jieShouJi = new SerialPortHelper(JieShouJiCOM, JieShouJiBaudrate, true, (data) =>
                {
                    DoWithGGA(data);
                });
                jieShouJi.OpenSerialPort();
                if (!jieShouJi.IsPortOpen)//串口打开失败
                    return false;

                dianTai = new SerialPortHelper(DianTaiCOM, DianTaiBaudrate);
                dianTai.OpenSerialPort();
                if (!dianTai.IsPortOpen)
                    return false;

                tcpClient = new TcpClientHelper(IpAddress, Port, (data, dataLen) => { DoWithNetMsg(data, dataLen); });
                tcpClient.Start();
                if (!tcpClient.IsConnet)
                    return false;

                return true;
            }
            return false;
        }

        private bool isMonitorGGAStatus = true;

        private void DoWithGGA(string data)
        {
            if (!data.Contains("GGA"))
                return;

            //收到GGA数据马上转发网口收到的数据
            if (isUseNetChaFen)
            {
                lock (lockObject)
                {
                    if (bufferDataLen >= 600)
                    {
                        bufferDataLen = 0;
                    }
                    else
                    {
                        dianTai.SendData(globalDataBuffer, bufferDataLen);
                        bufferDataLen = 0;
                    }
                }
            }

            string[] dataArray = data.Split(',');
            string status = dataArray[6];
            //判断定位是否有效
            if (!(status == "1" || status == "2" || status == "4" || status == "5"))//定位无效
                return;
            string longitudeStr = dataArray[4];
            string latitudeStr = dataArray[2];

            double latitude = (int)(double.Parse(latitudeStr) / 100) + (double.Parse(latitudeStr) % 100) / 60;
            double longitude = (int)(double.Parse(longitudeStr) / 100) + (double.Parse(longitudeStr) % 100) / 60;

            if (isMonitorGGAStatus)
            {
                //计算定位误差，如果定位误差超过一定范围则发起切换提醒。
                double averageLatitude = (BaseLatitude + latitude) / 2;
                double latitudeSub = BaseLatitude - latitude;
                double y = CalHelper.CalY(averageLatitude, latitudeSub);

                double longitudeSub = BaseLongitude - longitude;
                double x = CalHelper.CalX(longitude, longitudeSub);

                double distance = Math.Sqrt(x * x + y * y);
                if (distance >= 20)
                {
                    errorCount++;
                    isLastError = true;

                    if (errorCount >= limitErrorCount)
                    {
                        Status = "Red";
                        ErrorMsg = "定位误差较大，请切换使用差分数据。";
                    }
                }
                else
                {
                    isLastError = false;
                }
                if (!isLastError)
                {
                    errorCount = 0;
                }
            }
        }

        private byte[] globalDataBuffer = new byte[2000];//用于缓存网口收到的数据
        private int bufferDataLen = 0;
        private bool isUseNetChaFen = false;
        private readonly object lockObject = new object();

        private void DoWithNetMsg(byte[] data, int dataLen)
        {
            Print("Recv", JionHex(data, dataLen));
            if (isUseNetChaFen)
            {
                lock (lockObject)
                {
                    Array.Copy(data, 0, globalDataBuffer, 0, dataLen);
                    bufferDataLen += dataLen;
                }
                //把从网络收到的差分数据转发给电台
                //dianTai.SendData(data);
            }
        }

        private void Stop()
        {
            ErrorMsg = "";
            isUseNetChaFen = false;
            if (tcpClient != null)
                tcpClient.Stop();
            if (jieShouJi != null)
                jieShouJi.CloseSerialPort();
            if (dianTai != null)
                dianTai.CloseSerialPort();
        }

        private void Change()
        {
            isUseNetChaFen = true;
            isMonitorGGAStatus = false;
            ErrorMsg = "数据转发中。。。";
            Status = "White";
        }

        private void StopChange()
        {
            ErrorMsg = "";
            isUseNetChaFen = false;
            isMonitorGGAStatus = true;
        }

        private void InitConfig()
        {
            JieShouJiCOM = AppConfigHelper.GetAppConfig(AppConfigHelper.JieShouJiCom);
            JieShouJiBaudrate = int.Parse(AppConfigHelper.GetAppConfig(AppConfigHelper.JieShouJiBaudrate));

            DianTaiCOM = AppConfigHelper.GetAppConfig(AppConfigHelper.DianTaiCom);
            DianTaiBaudrate = int.Parse(AppConfigHelper.GetAppConfig(AppConfigHelper.DianTaiBaudrate));

            IpAddress = AppConfigHelper.GetAppConfig(AppConfigHelper.IpAddress);
            Port = int.Parse(AppConfigHelper.GetAppConfig(AppConfigHelper.Port));

            BaseLongitude = double.Parse(AppConfigHelper.GetAppConfig(AppConfigHelper.BaseLongitude));
            BaseLatitude = double.Parse(AppConfigHelper.GetAppConfig(AppConfigHelper.BaseLatitude));
            limitErrorCount = int.Parse(AppConfigHelper.GetAppConfig(AppConfigHelper.ErrorCount));

            StartBtnContent = StartTag;
            ChangeBtnContent = ChangeTag;
            logger.Info("初始化配置完成");
        }

        private bool CheckInput()
        {
            bool result = true;
            Regex ipRegex = new Regex(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$");
            bool ipRight = IpAddress != "" && ipRegex.IsMatch(IpAddress.Trim());

            if (!ipRight)
            {
                result = false;
                MessageBox.Show("IP 地址输入不正确。");
            }

            logger.Info($"输入验证完成，结果{result}");
            return result;
        }

        private void SaveConfig()
        {
            AppConfigHelper.UpdateAppConfig(AppConfigHelper.JieShouJiCom, JieShouJiCOM);
            AppConfigHelper.UpdateAppConfig(AppConfigHelper.JieShouJiBaudrate, JieShouJiBaudrate.ToString());

            AppConfigHelper.UpdateAppConfig(AppConfigHelper.DianTaiCom, DianTaiCOM);
            AppConfigHelper.UpdateAppConfig(AppConfigHelper.DianTaiBaudrate, DianTaiBaudrate.ToString());

            AppConfigHelper.UpdateAppConfig(AppConfigHelper.IpAddress, IpAddress);
            AppConfigHelper.UpdateAppConfig(AppConfigHelper.Port, Port.ToString());

            AppConfigHelper.UpdateAppConfig(AppConfigHelper.BaseLongitude, BaseLongitude.ToString());
            AppConfigHelper.UpdateAppConfig(AppConfigHelper.BaseLatitude, BaseLatitude.ToString());

            logger.Info("保存化配置完成");
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (StartBtnContent == StartTag)
            {
                this.StartBtn.IsEnabled = false;//因为网口连接需要等待，可能连接不成功，为了避免用户在连接等待期间多次点击先禁用
                logger.Info("开始启动");
                if (!Start())
                {
                    this.StartBtn.IsEnabled = true;
                    Stop();
                    return;
                }
                this.StartBtn.IsEnabled = true;
                StartBtnContent = StopTag;
                EditEnable = false;
                this.ChangeBtn.IsEnabled = true;
            }
            else
            {
                logger.Info("开始停止");
                EditEnable = true;
                this.ChangeBtn.IsEnabled = false;
                StartBtnContent = StartTag;
                ChangeBtnContent = ChangeTag;
                Stop();
            }
        }

        private void ChangeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ChangeBtnContent == ChangeTag)
            {
                logger.Info("开始切换");
                ChangeBtnContent = StopChangeTag;
                Change();
            }
            else
            {
                logger.Info("停止切换");
                ChangeBtnContent = ChangeTag;
                StopChange();
            }
        }

        private string JionHex(byte[] data, int len)
        {
            StringBuilder stringBuilder = new StringBuilder();
            int i = 0;
            int j = 0;
            foreach (var byteData in data)
            {
                if (j++ == len - 1)
                {
                    break;
                }
                if (i++ != 0)
                {
                    stringBuilder.Append(' ');
                }
                stringBuilder.Append(byteData.ToString("x2"));
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// 输出记录到日志显示区域
        /// </summary>
        /// <param name="tag">recv 或者 send</param>
        /// <param name="data"></param>
        private void Print(string tag, string data)
        {
            string time = DateTime.Now.ToString("HH:mm:ss.fff");
            this.logTextBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (this.logTextBox.LineCount > 100)//每100行自动清空
                    this.logTextBox.Text = "";
                this.logTextBox.AppendText(time);
                this.logTextBox.AppendText(" ");
                this.logTextBox.AppendText(tag);
                this.logTextBox.AppendText(": ");
                this.logTextBox.AppendText(data);
                this.logTextBox.AppendText("\r\n");
                this.logTextBox.ScrollToLine(this.logTextBox.LineCount - 1);
            }));
            //log.Info($"{tag}{data}");
        }
    }
}