using IrisSS;
using SharpDX.DirectInput;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Media;

namespace RoboUtes.Terminal
{
    public partial class ArmTerminal : Window
    {
        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;
        public static void LeftMouseClick(int xpos, int ypos)
        {
            SetCursorPos(xpos, ypos);
            mouse_event(MOUSEEVENTF_LEFTDOWN, xpos, ypos, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, xpos, ypos, 0, 0);
        }

        private ArmTermianlViewModel viewModel;
        private IrisClient irisClient;
        private System.Timers.Timer videoDetails = new System.Timers.Timer(5000);
        private Joystick joystick;
        private System.Timers.Timer joystickPoller = new System.Timers.Timer(100);
        private double DpiWidthFactor;
        private double DpiHeightFactor;

        public ArmTerminal()
        {
            InitializeComponent();

            Dispatcher.Invoke(() =>
            {
                Window MainWindow = Application.Current.MainWindow;
                PresentationSource MainWindowPresentationSource = PresentationSource.FromVisual(MainWindow);
                Matrix m = MainWindowPresentationSource.CompositionTarget.TransformToDevice;
                DpiWidthFactor = m.M11;
                DpiHeightFactor = m.M22;
            });

            DataContext = viewModel = new ArmTermianlViewModel();

            viewModel.PropertyChanged += ViewModel_PropertyChanged;

            irisClient = new IrisClient(200, IPAddress.Parse("192.168.1.151"));
            irisClient.dataReceivedFromServer += IrisClient_dataReceivedFromServer;
            irisClient.connectionStatusChanged += IrisClient_connectionStatusChanged;
            irisClient.Connect();

            videoDetails.Elapsed += (s, e) => SendUpdate();
            videoDetails.Start();

            try
            {
                DirectInput directInput = new DirectInput();
                joystick = new Joystick(new DirectInput(), FindJoystickId(directInput));
                joystick.Acquire();

                joystickPoller.Elapsed += JoystickPoller_Elapsed;
                joystickPoller.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error loading joystick: " + ex.Message);
            }
        }

        private void IrisClient_connectionStatusChanged(bool connected)
        {
            viewModel.ConnectedToIris = connected;
        }

        private void JoystickPoller_Elapsed(object sender, ElapsedEventArgs e)
        {
            var state = joystick.GetCurrentState();

            double xState = state.X / 65535.0;
            double yState = state.Y / 65535.0;
            double zState = state.RotationZ / 65535.0;

            if (state.Buttons[8]) deployArm();

            if (state.Buttons[9]) storeArm();

            if (viewModel.MenuShown)
            {
                double ScreenHeight = SystemParameters.PrimaryScreenHeight * DpiHeightFactor;
                double ScreenWidth = SystemParameters.PrimaryScreenWidth * DpiWidthFactor;

                if (xState > 0.6)
                {
                    if(yState > 0.6)
                    {
                        SetCursorPos((int)(3.0 * ScreenWidth / 4.0), (int)(3.0 * ScreenHeight / 4.0));
                        if (state.Buttons[0]) LeftMouseClick((int)(3.0 * ScreenWidth / 4.0), (int)(3.0 * ScreenHeight / 4.0));
                    }
                    if(yState < 0.4)
                    {
                        SetCursorPos((int)(3.0 * ScreenWidth / 4.0), (int)(ScreenHeight / 4.0));
                        if (state.Buttons[0]) LeftMouseClick((int)(3.0 * ScreenWidth / 4.0), (int)(ScreenHeight / 4.0));
                    }
                }
                else if(yState < 0.4)
                {
                    SetCursorPos((int)(ScreenWidth / 2.0), (int)(ScreenHeight / 4.0));
                    if (state.Buttons[0]) LeftMouseClick((int)(ScreenWidth / 2.0), (int)(ScreenHeight / 4.0));
                }
                    
            }
            else
            {
                double movementScalar = ((65535.0 - state.Sliders[0]) / 65535.0) * 3.0;

                bool? fingerMovement = null;
                if (state.Buttons[0] && !state.Buttons[1])
                    fingerMovement = true;
                else if (!state.Buttons[0] && state.Buttons[1])
                    fingerMovement = false;
                else
                    fingerMovement = null;

                if (xState > 0.6 || xState < 0.4)
                {
                    viewModel.AngleOne += (0.5 - xState) * movementScalar;
                }

                if (yState > 0.6 || yState < 0.4)
                {
                    viewModel.AngleTwo += -(0.5 - yState) * movementScalar;
                    viewModel.AngleThree += (0.5 - yState) * movementScalar;
                }

                if (zState > 0.6 || zState < 0.4)
                {
                    viewModel.AngleThree += -(0.5 - zState) * movementScalar;
                }

                if (fingerMovement != null)
                {
                    bool actual = fingerMovement ?? false;
                    if (actual) // Close fingers
                        viewModel.AngleFive += -0.5 * movementScalar;
                    else // Open Fingers
                        viewModel.AngleFive += 0.5 * movementScalar;
                }
            }

            if (state.Buttons[6])
                viewModel.MenuShown = !viewModel.MenuShown;
        }

        private Guid FindJoystickId(DirectInput directInput)
        {
            Guid jsGuid = Guid.Empty;
            foreach (DeviceInstance deviceInstance in directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices))
            {
                jsGuid = deviceInstance.InstanceGuid;
            }

            if (jsGuid == Guid.Empty)
            {
                throw new Exception("Joystick not found");
            }
            return jsGuid;
        }

        private void IrisClient_dataReceivedFromServer(string ID, string data)
        {
            switch (ID)
            {
                case "GPS_Loc":
                    viewModel.RoverLatitude = double.Parse(data.Split(',')[0]);
                    viewModel.RoverLongitude = double.Parse(data.Split(',')[1]);
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

                case "ALOCK":
                    bool locked = bool.Parse(data);
                    viewModel.Locked = locked;
                    break;

                case "PB_Current":
                    Console.WriteLine("Power Board Current: {0}", data);
                    break;

                case "VS_C":
                    var videoSettings = VideoSettings.Parse(data);
                    if (TerminalEnum.Arm != videoSettings.Terminal) break;
                    Dispatcher.Invoke(() =>
                    {
                        axisCameraStream.Camera = videoSettings.Camera;
                        axisCameraStream.Resolution = videoSettings.Resolution;
                        axisCameraStream.FPS = videoSettings.FPS;
                        axisCameraStream.Compression = videoSettings.Compression;
                        OpenVideoFeed();
                    });
                    break;

                case "VS_R":
                    var videoSettings2 = VideoSettings.Parse(data);
                    break;

                default:
                    Console.WriteLine("Unknown command: {0}|{1}", ID, data);
                    break;
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "AngleOne":
                case "AngleTwo":
                case "AngleThree":
                case "AngleFour":
                case "AngleFive":
                    if (irisClient.Connected) irisClient.sendData("ARM", string.Format("{0:0},{1:0},{2:0},{3:0},{4:0}", viewModel.AngleOne, viewModel.AngleTwo, viewModel.AngleThree, viewModel.AngleFour, viewModel.AngleFive));
                    Console.WriteLine("Arm Angles Changed (RAW: {0})", string.Format("{0:0},{1:0},{2:0},{3:0},{4:0}", viewModel.AngleOne, viewModel.AngleTwo, viewModel.AngleThree, viewModel.AngleFour, viewModel.AngleFive));
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
            string msg = string.Format("{0},{1},{2},{3},{4},{5}", (int)TerminalEnum.Arm, (int)axisCameraStream.Camera, (int)axisCameraStream.Resolution, axisCameraStream.FPS, axisCameraStream.Compression, axisCameraStream.MeasuredFPS);
            if (irisClient.Connected) irisClient.sendData("VS_R", msg);
            Console.WriteLine("Opening Feed (RAW: {0})", msg);
        }

        private void SendUpdate()
        {
            Dispatcher.Invoke(() =>
            {
                string msg = string.Format("{0},{1},{2},{3},{4},{5}", (int)TerminalEnum.Arm, (int)axisCameraStream.Camera, (int)axisCameraStream.Resolution, axisCameraStream.FPS, axisCameraStream.Compression, axisCameraStream.MeasuredFPS);
                if (irisClient.Connected) irisClient.sendData("VS_R", msg);
                Console.WriteLine("Sent Update on Video Feed Control: {0}", msg);
            });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            OpenVideoFeed();
        }

        private void mapDisplay_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.Q:
                    if(!viewModel.MenuShown) viewModel.AngleOne++;
                    break;
                case System.Windows.Input.Key.A:
                    if (!viewModel.MenuShown) viewModel.AngleOne--;
                    break;
                case System.Windows.Input.Key.W:
                    if (!viewModel.MenuShown) viewModel.AngleTwo++;
                    break;
                case System.Windows.Input.Key.S:
                    if (!viewModel.MenuShown) viewModel.AngleTwo--;
                    break;
                case System.Windows.Input.Key.E:
                    if (!viewModel.MenuShown) viewModel.AngleThree++;
                    break;
                case System.Windows.Input.Key.D:
                    if (!viewModel.MenuShown) viewModel.AngleThree--;
                    break;
                case System.Windows.Input.Key.R:
                    if (!viewModel.MenuShown) viewModel.AngleFour++;
                    break;
                case System.Windows.Input.Key.F:
                    if (!viewModel.MenuShown) viewModel.AngleFour--;
                    break;
                case System.Windows.Input.Key.T:
                    if (!viewModel.MenuShown) viewModel.AngleFive++;
                    break;
                case System.Windows.Input.Key.G:
                    if (!viewModel.MenuShown) viewModel.AngleFive--;
                    break;
                case System.Windows.Input.Key.M:
                    viewModel.MenuShown = !viewModel.MenuShown;
                    break;
            }
        }

        private void txtblk_Overlay1_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            viewModel.MenuShown = false;
            viewModel.Overlay1Color = Brushes.White;
        }

        private void txtblk_Overlay1_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            viewModel.Overlay1Color = UIColors.Caution;
        }

        private void txtblk_Overlay1_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            viewModel.Overlay1Color = Brushes.White;
        }

        private void txtblk_Overlay2_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            viewModel.MenuShown = false;
            viewModel.Overlay2Color = Brushes.White;
        }

        private void txtblk_Overlay2_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            viewModel.Overlay2Color = UIColors.Caution;
        }

        private void txtblk_Overlay2_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            viewModel.Overlay2Color = Brushes.White;
        }

        private void txtblk_Overlay3_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            viewModel.MenuShown = false;
            viewModel.Overlay3Color = Brushes.White;
        }

        private void txtblk_Overlay3_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            viewModel.Overlay3Color = UIColors.Caution;
        }

        private void txtblk_Overlay3_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            viewModel.Overlay3Color = Brushes.White;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            viewModel.MenuShown = false;
            deployArm();
            storeArm();
        }

        private void deployArm()
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, ev) =>
            {
                while (viewModel.AngleOne != 0)
                {
                    if (viewModel.AngleOne > 0)
                        viewModel.AngleOne--;
                    else
                        viewModel.AngleOne++;

                    Thread.Sleep(50);
                }

                while (viewModel.AngleTwo != -33)
                {
                    if (viewModel.AngleTwo < -33)
                        viewModel.AngleTwo++;
                    else
                        viewModel.AngleTwo--;

                    Thread.Sleep(50);
                }

                while (viewModel.AngleThree != 75)
                {
                    if (viewModel.AngleThree < 75)
                        viewModel.AngleThree++;
                    else
                        viewModel.AngleThree--;
                    Thread.Sleep(50);
                }
            };

            worker.RunWorkerAsync();
        }

        private void storeArm()
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, ev) =>
            {
                while (viewModel.AngleOne != 30)
                {
                    if (viewModel.AngleOne > 30)
                        viewModel.AngleOne--;
                    else
                        viewModel.AngleOne++;

                    Thread.Sleep(50);
                }

                while (viewModel.AngleTwo != 30)
                {
                    if (viewModel.AngleTwo < 30)
                        viewModel.AngleTwo++;
                    else
                        viewModel.AngleTwo--;

                    Thread.Sleep(50);
                }

                while (viewModel.AngleThree != -20)
                {
                    if (viewModel.AngleThree < -20)
                        viewModel.AngleThree++;
                    else
                        viewModel.AngleThree--;
                    Thread.Sleep(50);
                }
            };

            worker.RunWorkerAsync();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            viewModel.MenuShown = false;
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {

        }
    }

    public class ArmTermianlViewModel : INotifyPropertyChanged
    {
        private double _angleOne = 0.0;
        private double _angleTwo = 0.0;
        private double _angleThree = 0.0;
        private double _angleFour = 0.0;
        private double _angleFive = 0.0;

        private double _roll = 0.0;
        private double _leftPitch = 0.0;
        private double _rightPitch = 0.0;
        private double _yaw = 0.0;

        private double _roverLatitude = 0.0;
        private double _roverLongitude = 0.0;

        private AxisCamera _streamCamera = AxisCamera.TWO;
        private int _streamWidth = 0;
        private int _streamHeight = 0;
        private int _streamFPS = 0;
        private int _streamCompression = 0;

        private bool _connectedToIris = false;
        private bool _locked = false;
        private bool _menuShown = false;

        private Brush _overlay1Color = Brushes.White;
        private Brush _overlay2Color = Brushes.White;
        private Brush _overlay3Color = Brushes.White;

        private string _ipAddress = "192.168.1.151";

        public double AngleOne
        {
            get { return _angleOne; }
            set { _angleOne = value; NotifyPropertyChanged("AngleOne"); }
        }

        public double AngleTwo
        {
            get { return _angleTwo; }
            set { _angleTwo = value; NotifyPropertyChanged("AngleTwo"); }
        }

        public double AngleThree
        {
            get { return _angleThree; }
            set { _angleThree = value; NotifyPropertyChanged("AngleThree"); }
        }

        public double AngleFour
        {
            get { return _angleFour; }
            set { _angleFour = value; NotifyPropertyChanged("AngleFour"); }
        }

        public double AngleFive
        {
            get { return _angleFive; }
            set { _angleFive = value; NotifyPropertyChanged("AngleFive"); }
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

        public bool ConnectedToIris
        {
            get { return _connectedToIris; }
            set { _connectedToIris = value; NotifyPropertyChanged("ConnectedToIris"); }
        }

        public bool Locked
        {
            get { return _locked; }
            set { _locked = value; NotifyPropertyChanged("Locked"); }
        }

        public bool MenuShown
        {
            get { return _menuShown; }
            set { _menuShown = value; NotifyPropertyChanged("MenuShown"); }
        }

        public Brush Overlay1Color
        {
            get { return _overlay1Color; }
            set { _overlay1Color = value; NotifyPropertyChanged("Overlay1Color"); }
        }

        public Brush Overlay2Color
        {
            get { return _overlay2Color; }
            set { _overlay2Color = value; NotifyPropertyChanged("Overlay2Color"); }
        }

        public Brush Overlay3Color
        {
            get { return _overlay3Color; }
            set { _overlay3Color = value; NotifyPropertyChanged("Overlay3Color"); }
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
