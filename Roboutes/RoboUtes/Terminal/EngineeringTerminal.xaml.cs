using IrisSS;
using System;
using System.ComponentModel;
using System.Net;
using System.Windows;
using System.Windows.Media;

namespace RoboUtes.Terminal
{
    public partial class EngineeringTerminal : Window
    {private IrisClient _irisClient;

        private mainWindowViewModel viewModel;

        public EngineeringTerminal()
        {
            InitializeComponent();
            this.DataContext = viewModel = new mainWindowViewModel();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _irisClient = new IrisClient(400, IPAddress.Parse(viewModel._targetIP));
            _irisClient.dataReceivedFromServer += IrisClient_dataReceivedFromServer;
            _irisClient.connectionStatusChanged += irisClient_connectionStatusChanged;
            _videoController.VideoSettingsChanged += videoSettingsChanged;
        }

        private void videoSettingsChanged(object sender, VideoControlEventArgs e)
        {
            _irisClient.sendData("VS_C", e.ToString());
        }

        private void irisClient_connectionStatusChanged(bool connected)
        {
            if (connected)
            {
                viewModel.EngTermConnected = true;
            }
            else
            {
                viewModel.EngTermConnected = false;
            }
        }
    

        private void IrisClient_dataReceivedFromServer(string ID, string data)
        {
            switch (ID)
            {
                case "GAINED":
                    if (data == "100")
                        viewModel.DriveTermConnected = true;
                    else if (data == "200")
                        viewModel.ArmTermConnected = true;
                    else if (data == "300")
                        viewModel.LogTermConnected = true;

                    break;
                case "LOST":
                    if (data == "100")
                        viewModel.DriveTermConnected = false;
                    else if (data == "200")
                        viewModel.ArmTermConnected = false;
                    else if (data == "300")
                        viewModel.LogTermConnected = false;

                    break;

                case "VS_R":
                    var videoSettings = VideoSettings.Parse(data);
                    switch(videoSettings.Terminal)
                    {
                        case TerminalEnum.Arm:
                            _videoController.UpdateFromTerminal(data);
                            break;
                        case TerminalEnum.Drive:
                            _videoController.UpdateFromTerminal(data);
                            break;
                        case TerminalEnum.Logistics:
                            _videoController.UpdateFromTerminal(data);
                            break;
                    }
                    break;
                case "ATT_R":

                    break;
                case "ARR_L":
                    break;

                case "JKWarn":
                    if (Boolean.Parse(data))
                        buttonJackKnife.Background = UIColors.Bad;
                    else
                        buttonJackKnife.Background = UIColors.Grey;
                    break;
            }

        }

        private void buttonDriveSTOP_Click(object sender, RoutedEventArgs e)
        {
                
        }

        private void armLock_Click(object sender, RoutedEventArgs e)
        {
            viewModel.ArmLock = !viewModel.ArmLock;
            _irisClient.sendData("ALOCK", viewModel.ArmLock.ToString());
            if (viewModel.ArmLock)
                Dispatcher.Invoke(() => armLock.BorderBrush = UIColors.Bad);
            else
                Dispatcher.Invoke(() => armLock.BorderBrush = UIColors.Grey);
        }

        private void buttonParkBreak_Click(object sender, RoutedEventArgs e)
        {
            viewModel.DriveLock = !viewModel.DriveLock;
            _irisClient.sendData("DLOCK", viewModel.DriveLock.ToString());
            if (viewModel.DriveLock)
                Dispatcher.Invoke(() => buttonParkBreak.BorderBrush = UIColors.Bad);
            else
                Dispatcher.Invoke(() => buttonParkBreak.BorderBrush = UIColors.Grey);
        }

    }

    public class mainWindowViewModel : INotifyPropertyChanged
    {
        public String _targetIP = "127.0.0.1";

        //public String _targetIP = "192.168.1.151";

        public enum timerButtonStatus
        {
            START_TIMER, STOP_TIMER, START_M, STOP_M, TIMEOUT
        }
        private timerButtonStatus _timerButtonStatus = timerButtonStatus.START_TIMER;

        private TimeSpan _timer = new TimeSpan(1,0,0);
        private TimeSpan _mulliganTimer = new TimeSpan(0, 10, 0);

        private Boolean _engTermConnected = false;
        private Boolean _driveTermConnected = false;
        private Boolean _armTermConnected = false;
        private Boolean _logTermConnected = false;

        private bool _armBossConnected = false;
        private bool _driveBossConnected = false;
        private bool _axisStatus = false;

        private int _batteryLevel = -1;
        private int _modemStrength = -1;

        private String _imuLeft = "180,0,0";
        public String _imuRight = "180,0,0";

        private int _ptPan = -1;
        private int _ptTilt = -1;

        private double _enviroTempSensor = -1;
        private double _enviroHumSensor = -1;
        private double _enviroPpmSensor = -1;
        private double _enviroBarSensor = -1;

        private double _fl_Frmm_RPM = -1;
        private double _fl_Frmm_Temp = -1;
        private double _fl_Frmm_Current = -1;

        private double _fr_Frmm_RPM = -1;
        private double _fr_Frmm_Temp = -1;
        private double _fr_Frmm_Current = -1;

        private double _bl_Frmm_RPM = -1;
        private double _bl_Frmm_Temp = -1;
        private double _bl_Frmm_Current = -1;

        private double _br_Frmm_RPM = -1;
        private double _br_Frmm_Temp = -1;
        private double _br_Frmm_Current = -1;

        private double _armTT = -1;
        private double _armShoulder = -1;
        private double _armElbow = -1;
        private double _armWrist = -1;
        private double _armHand = -1;

        private Boolean _driveLock = false;
        private Boolean _armLock = false;

        public event PropertyChangedEventHandler PropertyChanged;

        //binding all of these properties
        public int Battery
        {
            get { return _batteryLevel; }
            set { _batteryLevel = value; NotifyPropertyChanged("Battery"); }
        }

        public int ModemStrength
        {
            get { return _modemStrength; }
            set { _modemStrength = value; NotifyPropertyChanged("ModemStrength"); }
        }

        public int PtPan
        {
            get { return _ptPan; }
            set { _ptPan = value; NotifyPropertyChanged("PtPan"); }
        }
        public int PtTilt
        {
            get { return _ptTilt; }
            set { _ptTilt = value; NotifyPropertyChanged("PtTilt"); }
        }

        public string ImuLeft
        {
            get { return _imuLeft; }
            set { _imuLeft = value; NotifyPropertyChanged("ImuLeft"); }
        }

        public string ImuRight
        {
            get { return _imuRight; }
            set { _imuRight = value; NotifyPropertyChanged("ImuRight"); }
        }

        public Boolean DriveLock
        {
            get { return _driveLock; }
            set { _driveLock = value; NotifyPropertyChanged("DriveLock"); }
        }

        public Boolean ArmLock
        {
            get { return _armLock; }
            set { _armLock = value; NotifyPropertyChanged("ArmLock"); }
        }

        public double EnviroTemp
        {
            get { return _enviroTempSensor; }
            set { _enviroTempSensor = value; NotifyPropertyChanged("EnviroTemp"); }
        }

        public double EnviroHumidity
        {
            get { return _enviroHumSensor; }
            set { _enviroHumSensor = value; NotifyPropertyChanged("EnviroHumidity"); }
        }

        public double EnviroPPM
        {
            get { return _enviroPpmSensor; }
            set { _enviroPpmSensor = value; NotifyPropertyChanged("EnviroPPM"); }
        }

        public double EnviroBar
        {
            get { return _enviroBarSensor; }
            set { _enviroBarSensor = value; NotifyPropertyChanged("EnviroBar"); }
        }

        public double FL_RPM
        {
            get { return _fl_Frmm_RPM; }
            set { _fl_Frmm_RPM = value; NotifyPropertyChanged("FL_RPM"); }
        }
        public double FL_TEMP
        {
            get { return _fl_Frmm_Temp; }
            set { _fl_Frmm_Temp = value; NotifyPropertyChanged("FL_TEMP"); }
        }
        public double FL_CURRENT
        {
            get { return _fl_Frmm_Current; }
            set { _fl_Frmm_Current = value; NotifyPropertyChanged("FL_CURRENT"); }
        }

        public double FR_RPM
        {
            get { return _fr_Frmm_RPM; }
            set { _fr_Frmm_RPM = value; NotifyPropertyChanged("FR_RPM"); }
        }
        public double FR_TEMP
        {
            get { return _fr_Frmm_Temp; }
            set { _fr_Frmm_Temp = value; NotifyPropertyChanged("FR_TEMP"); }
        }
        public double FR_CURRENT
        {
            get { return _fr_Frmm_Current; }
            set { _fr_Frmm_Current = value; NotifyPropertyChanged("FR_CURRENT"); }
        }

        public double BL_RPM
        {
            get { return _bl_Frmm_RPM; }
            set { _bl_Frmm_RPM = value; NotifyPropertyChanged("BL_RPM"); }
        }
        public double BL_TEMP
        {
            get { return _bl_Frmm_Temp; }
            set { _bl_Frmm_Temp = value; NotifyPropertyChanged("BL_TEMP"); }
        }
        public double BL_CURRENT
        {
            get { return _bl_Frmm_Current; }
            set { _bl_Frmm_Current = value; NotifyPropertyChanged("BL_CURRENT"); }
        }

        public double BR_RPM
        {
            get { return _br_Frmm_RPM; }
            set { _br_Frmm_RPM = value; NotifyPropertyChanged("BR_RPM"); }
        }
        public double BR_TEMP
        {
            get { return _br_Frmm_Temp; }
            set { _br_Frmm_Temp = value; NotifyPropertyChanged("BR_TEMP"); }
        }
        public double BR_CURRENT
        {
            get { return _br_Frmm_Current; }
            set { _br_Frmm_Current = value; NotifyPropertyChanged("BR_CURRENT"); }
        }

        public double ARM_TT
        {
            get { return _armTT; }
            set { _armTT = value; NotifyPropertyChanged("ARM_TT"); }
        }
        public double ARM_SHOULDER
        {
            get { return _armShoulder; }
            set { _armShoulder = value; NotifyPropertyChanged("ARM_SHOULDER"); }
        }
        public double ARM_ELBOW
        {
            get { return _armElbow; }
            set { _armElbow = value; NotifyPropertyChanged("ARM_ELBOW"); }
        }
        public double ARM_WRIST
        {
            get { return _armWrist; }
            set { _armWrist = value; NotifyPropertyChanged("ARM_WRIST"); }
        }
        public double ARM_HAND
        {
            get { return _armHand; }
            set { _armHand = value; NotifyPropertyChanged("ARM_HAND"); }
        }

        public bool LogTermConnected
        {
            get{ return _logTermConnected; }
            set{_logTermConnected = value;}
        }

        public bool EngTermConnected
        {
            get { return _engTermConnected; }
            set { _engTermConnected = value; }
        }

        public bool ArmTermConnected
        {
            get{return _armTermConnected;}
            set{_armTermConnected = value;}
        }

        public bool DriveTermConnected
        {
            get { return _driveTermConnected; }
            set { _driveTermConnected = value; }
        }

        public bool DriveBossConnected
        {
            get { return _driveBossConnected; }
            set { _driveBossConnected = value; }
          
        }

        public bool ArmBossConnected
        {
            get { return _armBossConnected; }
            set { _armBossConnected = value; }
        }

        public bool AxisStatus
        {
            get
            { return _axisStatus;}
            set{ _axisStatus = value; }
        }

        public void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
    }
}
