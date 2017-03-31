using IrisSS;
using System;
using System.ComponentModel;
using System.Net;
using System.Timers;
using System.Windows;

namespace RoboUtes.Terminal
{
    public partial class LogisticsTerminal : Window
    {
        private LogisticsTerminalVideoModel viewModel;
        private IrisClient irisClient;
        private Timer videoDetails = new Timer();


        public LogisticsTerminal()
        {
            InitializeComponent();
            DataContext = viewModel = new LogisticsTerminalVideoModel();
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            irisClient = new IrisClient(300, IPAddress.Parse("127.0.0.10"));

            //            irisClient = new IrisClient(300, IPAddress.Parse("192.168.1.151"));
            irisClient.dataReceivedFromServer += IrisClient_dataReceivedFromServer;
        }

        private void IrisClient_dataReceivedFromServer(string ID, string data)
        {
            switch (ID)
            {
                case "GPS_Loc":
                    Dispatcher.Invoke(() => viewModel.RoverLatitude = double.Parse(data.Split(',')[0]));
                    Dispatcher.Invoke(() => viewModel.RoverLongitude = double.Parse(data.Split(',')[1]));
                    Dispatcher.Invoke(() => mapDisplay.RoverLocation = new double[] { viewModel.RoverLatitude, viewModel.RoverLongitude });
                    Dispatcher.Invoke(() => mapDisplay.Center = new double[] { viewModel.RoverLatitude, viewModel.RoverLongitude });
                    Dispatcher.Invoke(() => mapDisplay2.RoverLocation = new double[] { viewModel.RoverLatitude, viewModel.RoverLongitude });
                    Dispatcher.Invoke(() => mapDisplay2.Center = new double[] { viewModel.RoverLatitude, viewModel.RoverLongitude });
                    break;

                case "ATT_L":
                    viewModel.Yaw = double.Parse(data.Split(',')[0]);
                    viewModel.LeftPitch = double.Parse(data.Split(',')[1]);
                    viewModel.RightPitch = double.Parse(data.Split(',')[1]);
                    viewModel.Roll = double.Parse(data.Split(',')[2]);
                    break;

                case "PB_Current":
                    Console.WriteLine("Power Board Current: {0}", data);
                    break;

                case "VS_C":
                    if (TerminalEnum.Logistics != (TerminalEnum)int.Parse(data.Split(',')[0])) break;
                    axisCameraStream.Camera = (AxisCamera)int.Parse(data.Split(',')[1]);
                    axisCameraStream.Resolution = (AxisResolution)int.Parse(data.Split(',')[2]);
                    axisCameraStream.FPS = int.Parse(data.Split(',')[3]);
                    axisCameraStream.Compression = int.Parse(data.Split(',')[4]);
                    OpenVideoFeed();
                    break;

                default:
                    Console.WriteLine("Unknown command: {0}|{1}", ID, data);
                    break;
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.M:
                    viewModel.MenuShown = !viewModel.MenuShown;
                    break;

            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Pan":
                case "Tilt":
                    irisClient.sendData("PT_UPDATE", string.Format("{0:0},{1:0}", viewModel.Pan, 180 - viewModel.Tilt));
                    Console.WriteLine("Pan & Tilt Changed to {0:0},{1:0} (RAW: {2})", viewModel.Pan, 180 - viewModel.Tilt, string.Format("PT_UPDATE:{0:0},{1:0}", viewModel.Pan, 180 - viewModel.Tilt));
                    break;
                case "Yaw":
                    break;
                case "RoverLatitude":
                case "RoverLongitude":
                    mapDisplay.RoverLocation = new double[] { viewModel.RoverLatitude, viewModel.RoverLongitude };
                    break;
            }
        }

        private void OpenVideoFeed()
        {
            if (axisCameraStream.StreamIsOpen) axisCameraStream.CloseFeed();
            axisCameraStream.OpenFeed();
            string msg = string.Format("{0},{1},{2},{3},{4}", (int)TerminalEnum.Logistics, (int)axisCameraStream.Camera, (int)axisCameraStream.Resolution, axisCameraStream.Compression, axisCameraStream.FPS);
            if (irisClient.Connected) irisClient.sendData("VS_R", msg);
            Console.WriteLine("Opening Feed (RAW: {0})", msg);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            OpenVideoFeed();
        }

        private void mapDisplay_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }

    public class LogisticsTerminalVideoModel : INotifyPropertyChanged
    {
        private double _pan = 90.0;
        private double _tilt = 90.0;

        private double _roll = 0.0;
        private double _leftPitch = 0.0;
        private double _rightPitch = 0.0;
        private double _yaw = 0.0;

        private double _roverLatitude = 0.0;
        private double _roverLongitude = 0.0;

        private AxisCamera _streamCamera = AxisCamera.ONE;
        private int _streamWidth = 0;
        private int _streamHeight = 0;
        private int _streamFPS = 0;
        private int _streamCompression = 0;
        private bool _menuShown = false;
        private string _ipAddress = "192.168.1.151";

        public double Pan
        {
            get { return _pan; }
            set { _pan = value; NotifyPropertyChanged("Pan"); }
        }

        public double Tilt
        {
            get { return _tilt; }
            set { _tilt = value; NotifyPropertyChanged("Tilt"); }
        }

        public double Roll
        {
            get { return _roll; }
            set { _roll = value; NotifyPropertyChanged("Roll"); }
        }

        public double LeftPitch
        {
            get { return _leftPitch; }
            set { _leftPitch = value; NotifyPropertyChanged("LeftPitch"); }
        }

        public double RightPitch
        {
            get { return _rightPitch; }
            set { _rightPitch = value; NotifyPropertyChanged("RightPitch"); }
        }

        public double Yaw
        {
            get { return _yaw; }
            set { _yaw = value; NotifyPropertyChanged("Yaw"); }
        }

        public double RoverLatitude
        {
            get { return _roverLatitude; }
            set { _roverLatitude = value; NotifyPropertyChanged("RoverLatitude"); }
        }

        public double RoverLongitude
        {
            get { return _roverLongitude; }
            set { _roverLongitude = value; NotifyPropertyChanged("RoverLongitude"); }
        }

        public AxisCamera StreamCamera
        {
            get { return _streamCamera; }
            set { _streamCamera = value; NotifyPropertyChanged("StreamCamera"); }
        }

        public int StreamWidth
        {
            get { return _streamWidth; }
            set { _streamWidth = value; NotifyPropertyChanged("StreamWidth"); }
        }

        public int StreamHeight
        {
            get { return _streamHeight; }
            set { _streamHeight = value; NotifyPropertyChanged("StreamHeight"); }
        }

        public int StreamFPS
        {
            get { return _streamFPS; }
            set { _streamFPS = value; NotifyPropertyChanged("StreamFPS"); }
        }

        public int StreamCompression
        {
            get { return _streamCompression; }
            set { _streamCompression = value; NotifyPropertyChanged("StreamCompression"); }
        }

        public bool MenuShown
        {
            get { return _menuShown; }
            set { _menuShown = value; NotifyPropertyChanged("MenuShown"); }
        }

        public string IpAddress
        {
            get { return _ipAddress; }
            set { _ipAddress = value; NotifyPropertyChanged("IpAddress"); }
        }



        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
    }
}
