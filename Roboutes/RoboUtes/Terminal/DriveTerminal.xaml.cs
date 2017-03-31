using IrisSS;
using SharpDX.DirectInput;
using System;
using System.ComponentModel;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace RoboUtes.Terminal
{
    public partial class DriveTerminal : Window
    {
        private DriveTerminalViewModel viewModel;
        private IrisClient irisClient;
        String target = "192.168.1.151";
        int port = 3670;


        logitechX3D testJoystick;

        readonly double xRange = 500;
        readonly double yRange = 1300;
        readonly double zRange = 500;

        double throttlePer = 0;
        double xPer = 0;
        double yPer = 0;
        double zPer = 0;

        int xVal = 0;
        int yVal = 0;
        int zVal = 0;

        object lockObject = 0;


        public DriveTerminal()
        {
            InitializeComponent();
            DataContext = viewModel = new DriveTerminalViewModel();
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            axisCameraStream.OpenFeed();

            irisClient = new IrisClient(100, IPAddress.Parse(target), port);
            irisClient.dataReceivedFromServer += IrisClient_dataReceivedFromServer;
            irisClient.connectionStatusChanged += testClient_connectionStatusChanged;
            irisClient.Connect();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            bool worked;
            testJoystick = logitechX3D.getInstance(out worked);
            if (!worked)
            {
                MessageBox.Show("Couldnt find any joysticks...");
                return;
            }
            testJoystick.throttleChanged += testJoystick_throttleChanged;
            testJoystick.axisInputChanged += testJoystick_axisInputChanged;
        }

        private void Window_Closing(object sender, EventArgs e)
        {
            //irisClient.Dispose();
        }


        private void Chatter_MessageReceived(string ID, string data)
        {
            Console.WriteLine("REC-->\nID: " + ID + " Data: " + data + "\n");
        }



        private void testJoystick_axisInputChanged(logitechX3D.axisID ID, double percentage)
        {

            lock (lockObject)
            {
                switch (ID)
                {
                    case logitechX3D.axisID.X:
                        if (Math.Abs(percentage - xPer) < .05)
                        {
                            return;
                        }
                        xPer = percentage * xRange;
                        xVal = (int)(xPer * throttlePer);
                        break;
                    case logitechX3D.axisID.Y:
                        if (Math.Abs(percentage - yPer) < .05)
                        {
                            return;
                        }
                        yPer = percentage * yRange;
                        yVal = (int)(yPer * throttlePer);
                        break;
                    case logitechX3D.axisID.Twist:
                        if (Math.Abs(percentage - zPer) < .05)
                        {
                            return;
                        }
                        zPer = percentage * zRange;
                        zVal = (int)(zPer * throttlePer);
                        break;
                }



                String toSend = xVal + "," + yVal + "," + zVal;
                Console.WriteLine("Sending: " + toSend);

                irisClient.sendData("jxyz", toSend);

            }
        }


        void testJoystick_throttleChanged(double percentage)
        {
            Console.WriteLine("Throttle at: " + percentage);
            throttlePer = percentage;
            xVal = (int)(xPer * throttlePer);
            yVal = (int)(yPer * throttlePer);
            zVal = (int)(zPer * throttlePer);
            String toSend = xVal + "," + yVal + "," + zVal;
            Console.WriteLine(toSend);
            irisClient.sendData("jxyz", toSend);
        }

        private void testClient_connectionStatusChanged(bool connected)
        {
            //if (connected)
            //{
            //    Dispatcher.Invoke(() => connectionindicator.Fill = new SolidColorBrush(Colors.Green));
            //}
            //else
            //{
            //    Dispatcher.Invoke(() => connectionindicator.Fill = new SolidColorBrush(Colors.Red));
            //}

            viewModel.ConnectedToIris = connected;
        }

        private void IrisClient_dataReceivedFromServer(string ID, string data)
        {
            switch (ID)
            {
                case "heading":
                    headingInd.Heading = Int32.Parse(data);

                    // Dispatcher.Invoke(() => pitchLabel_Copy3.Content = Math.Abs(Int32.Parse(data)));
                    viewModel.AbsHeading = Math.Abs(Int32.Parse(data));
                    break;
                case "roll":
                    // attitudeInd.Roll = float.Parse(data);
                    viewModel.Roll = float.Parse(data);
                    break;

                case "GPS_Loc":
                    //   RoverLongitude = double.Parse(data.Split(',')[1]);
                    //   RoverLatitude = double.Parse(data.Split(',')[0]);
                    viewModel.RoverLatitude = double.Parse(data.Split(',')[0]);
                    viewModel.RoverLongitude = double.Parse(data.Split(',')[1]);
                    Dispatcher.Invoke(() => mapDisplay.RoverLocation = new double[] { viewModel.RoverLatitude, viewModel.RoverLongitude });
                    Dispatcher.Invoke(() => mapDisplay.Center = new double[] { viewModel.RoverLatitude, viewModel.RoverLongitude });
                    Dispatcher.Invoke(() => mapDisplay2.RoverLocation = new double[] { viewModel.RoverLatitude, viewModel.RoverLongitude });
                    Dispatcher.Invoke(() => mapDisplay2.Center = new double[] { viewModel.RoverLatitude, viewModel.RoverLongitude });
                    break;

                case "ATT_L":
                    //attitudeInd.LeftPitch = float.Parse(data.Split(',')[1]);
                    //attitudeInd.RightPitch = float.Parse(data.Split(',')[1]);
                    //attitudeInd.Roll = float.Parse(data.Split(',')[2]);
                    viewModel.Yaw = double.Parse(data.Split(',')[0]);
                    viewModel.LeftPitch = double.Parse(data.Split(',')[1]);
                    viewModel.RightPitch = double.Parse(data.Split(',')[1]);
                    viewModel.Roll = double.Parse(data.Split(',')[2]);
                    break;

                case "PB_Current":
                    Console.WriteLine("Power Board Current: {0}", data);
                    break;

                case "VS_C":
                    if (TerminalEnum.Drive != (TerminalEnum)int.Parse(data.Split(',')[0])) break;
                    axisCameraStream.Camera = (AxisCamera)int.Parse(data.Split(',')[1]);
                    axisCameraStream.Resolution = (AxisResolution)int.Parse(data.Split(',')[2]);
                    axisCameraStream.FPS = int.Parse(data.Split(',')[3]);
                    axisCameraStream.Compression = int.Parse(data.Split(',')[4]);
                    axisCameraStream.CloseFeed();
                    axisCameraStream.OpenFeed();
                    break;

                case "DLOCK":
                    try
                    {
                        bool locked = bool.Parse(data);


                        //if (locked)
                        //{
                        //    //   engage = true;
                        //    Dispatcher.Invoke(() => connectionindicator_Copy.Fill = new SolidColorBrush(Colors.Green));
                        //}
                        //else
                        //{
                        //    //   engage = false;
                        //    Dispatcher.Invoke(() => connectionindicator_Copy.Fill = new SolidColorBrush(Colors.Red));
                        //}
                        viewModel.Locked = locked;
                    }
                    catch (FormatException)
                    {

                    }
                    break;
                case "vel":
                    string[] velocityData = data.Split(',');
                    wheelViz.FLSpeed = float.Parse(velocityData[0]) / 1000.0;  // front left wheel speed
                    wheelViz.FRSpeed = float.Parse(velocityData[1]) / 1000.0;  // front right wheel speed
                    wheelViz.BLSpeed = float.Parse(velocityData[2]) / 1000.0;  // back left wheel speed
                    wheelViz.BRSpeed = float.Parse(velocityData[3]) / 1000.0;  // back right wheel speed
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

            }

        }

        private void mapDisplay_Loaded(object sender, RoutedEventArgs e)
        {

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
            //  Console.WriteLine("Overlay3 CLICKED");
            viewModel.MenuShown = false;
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

        public class logitechX3D
        {
            private static logitechX3D instance;
            private static volatile bool foundJoystick = false;

            BackgroundWorker joyStickThread = new BackgroundWorker();

            static DirectInput DI = new DirectInput();
            static Joystick JS;
            static Guid joystickGUID;
            static JoystickState oldJState;


            public enum buttonState { Released, Pressed };
            public enum axisID { X, Y, Twist };
            public enum hatDirection { Centered = -1, North = 0, NorthEast = 4500, East = 9000, SouthEast = 13500, South = 18000, SouthWest = 22500, West = 27000, NorthWest = 31500 }
            private readonly double throttleMax = 65535; //This is actually the "0" position

            private readonly double axisMax = 65535; //This is actually the "back-right-clockwise" position
            private readonly double axisCenter = 32767.5; //half of axisMax

            Object JSThreadSync = 1;

            //JOYSTICK EVENTS - START
            public delegate void buttonChangedEventHandler(int ID, buttonState state);
            public event buttonChangedEventHandler buttonChanged;

            public delegate void throttleChangedEventHandler(double percentage);
            public event throttleChangedEventHandler throttleChanged;

            public delegate void axisInputChangedEventHandler(axisID ID, double percentage);
            public event axisInputChangedEventHandler axisInputChanged;

            public delegate void hatInputChangedEventHandler(hatDirection direction);
            public event hatInputChangedEventHandler hatDirectionChanged;

            //JOYSTICK EVENTS - END

            public static logitechX3D getInstance(out bool success)
            {
                if (instance == null)
                {
                    Guid temp = new Guid();
                    if (!getLogitechX3DGUID(out temp)) //checks to see if there are any joysticks available before creating an instance.
                    {
                        success = false;
                        return null;
                    }
                    instance = new logitechX3D();
                }
                success = foundJoystick;
                return instance;
            }

            private logitechX3D()
            {
                if (!getLogitechX3DGUID(out joystickGUID))
                {
                    return; //this should never happen... Theres a check run before any of this...
                }
                JS = new Joystick(DI, joystickGUID);
                JS.Acquire();
                oldJState = JS.GetCurrentState();

                joyStickThread.WorkerSupportsCancellation = true;
                joyStickThread.DoWork += new DoWorkEventHandler(monitorJoystick);
                joyStickThread.RunWorkerAsync();
            }

            private void monitorJoystick(object sender, DoWorkEventArgs e)
            {
                while (true)
                {
                    JoystickState state;
                    lock (JSThreadSync)
                    {
                        state = JS.GetCurrentState();
                    }
                    evaluateData(state);
                    Thread.Sleep(30);//This will make it poll ~33 times per second...
                }
            }

            private void evaluateData(JoystickState joystickState)
            {
                try
                {
                    //Update buttons
                    bool[] newButtons = joystickState.Buttons;
                    for (int i = 0; i < oldJState.Buttons.Length; i++)
                    {
                        if (buttonChanged != null)
                        {
                            if (oldJState.Buttons[i] != newButtons[i])
                            {
                                buttonChanged(i + 1, newButtons[i] ? buttonState.Pressed : buttonState.Released); //the i+1 makes the text on the joystick match the button number here
                            }
                        }
                    }

                    //Update throttles
                    if (throttleChanged != null)
                    {
                        if (oldJState.Sliders[0] != joystickState.Sliders[0])
                        {
                            double percent = 1.0 - ((double)joystickState.Sliders[0] / throttleMax);
                            throttleChanged(percent);
                        }
                    }

                    //Update Axis input
                    if (axisInputChanged != null)
                    {
                        //X axis
                        if (oldJState.X != joystickState.X)
                        {

                            double fromCenter = joystickState.X - axisCenter;
                            double percent = Math.Abs(fromCenter) / axisCenter;
                            if (fromCenter < 0)
                            {
                                percent *= -1;
                            }
                            axisInputChanged(axisID.X, percent);
                        }

                        //Y axis
                        if (oldJState.Y != joystickState.Y)
                        {
                            double fromCenter = joystickState.Y - axisCenter;
                            double percent = Math.Abs(fromCenter) / axisCenter;
                            if (fromCenter > 0)
                            {
                                percent *= -1;
                            }
                            axisInputChanged(axisID.Y, percent);
                        }

                        //Twist axis
                        if (oldJState.RotationZ != joystickState.RotationZ)
                        {
                            double fromCenter = joystickState.RotationZ - axisCenter;
                            double percent = Math.Abs(fromCenter) / axisCenter;
                            if (fromCenter < 0)
                            {
                                percent *= -1;
                            }
                            axisInputChanged(axisID.Twist, percent);
                        }
                    }

                    if (hatDirectionChanged != null)
                    {
                        if (oldJState.PointOfViewControllers[0] != joystickState.PointOfViewControllers[0])
                        {
                            hatDirectionChanged((hatDirection)joystickState.PointOfViewControllers[0]);
                        }
                    }

                    lock (JSThreadSync)
                    {
                        oldJState = joystickState;//update the recorded state
                    }
                }
                catch
                {
                    //do nothing...
                }
            }

            /// <summary>
            /// Only returns the GUIDs of Logitech Joysticks
            /// </summary>
            /// <returns></returns>
            private static bool getLogitechX3DGUID(out Guid result)
            {
                Guid jsGuid = Guid.Empty;
                string targetProductName = "Logitech Extreme 3D";
                foreach (DeviceInstance deviceInstance in DI.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices))
                {
                    if (deviceInstance.ProductName.Contains(targetProductName))
                    {
                        result = deviceInstance.InstanceGuid;
                        foundJoystick = true;
                        return true;
                    }
                }
                result = Guid.Empty;
                return false;
            }
        }
    }

    public class DriveTerminalViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _menuShown = false;
        private bool _connectedToIris = false;
        private double _roll = 0.0;
        private double _leftPitch = 0.0;
        private double _rightPitch = 0.0;
        private double _yaw = 0.0;
        private Brush _overlay1Color = Brushes.White;
        private Brush _overlay2Color = Brushes.White;
        private Brush _overlay3Color = Brushes.White;
        private double _roverLatitude = 0.0;
        private double _roverLongitude = 0.0;
        private string _ipAddress = "192.168.1.151";
        // private string _ipAddress = target;
        private int _abs_heading = 0;
        private bool _locked = false;
        private int _speed = 0;

        public bool MenuShown
        {
            get { return _menuShown; }
            set { _menuShown = value; NotifyPropertyChanged("MenuShown"); }
        }

        public bool ConnectedToIris
        {
            get { return _connectedToIris; }
            set { _connectedToIris = value; NotifyPropertyChanged("ConnectedToIris"); }
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

        public string IpAddress
        {
            get { return _ipAddress; }
            set { _ipAddress = value; NotifyPropertyChanged("IpAddress"); }
        }

        public int AbsHeading
        {
            get { return _abs_heading; }
            set { _abs_heading = value; NotifyPropertyChanged("AbsHeading"); }
        }

        public bool Locked
        {
            get { return _locked; }
            set { _locked = value; NotifyPropertyChanged("Locked"); }
        }

        public int Speed
        {
            get { return _speed; }
            set { _speed = value; NotifyPropertyChanged("Speed"); }
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

        private void NotifyPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
    }
}