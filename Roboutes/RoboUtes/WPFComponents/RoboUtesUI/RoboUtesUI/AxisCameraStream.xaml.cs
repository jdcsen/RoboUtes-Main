using RoboUtes.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RoboUtes
{
    public partial class AxisCameraStream : UserControl, INotifyPropertyChanged
    {
        private static MJPEGStreamer _axisStream;
        private static ImageSource _imageSource;
        private uint _measuredFPS;

        public bool StreamIsOpen
        {
            get { if (_axisStream != null) return _axisStream.IsOpen; else return false; }
        }
        public bool IsDebugging
        {
            get { return (bool)GetValue(IsDebuggingProperty); }
            set { SetValue(IsDebuggingProperty, value); }
        }
        public string IpAddress
        {
            get { return (string)GetValue(IpAddressProperty); }
            set { SetValue(IpAddressProperty, value); }
        }

        public int Port
        {
            get { return (int)GetValue(PortProperty); }
            set { SetValue(PortProperty, value); }
        }

        public string Username
        {
            get { return (string)GetValue(UsernameProperty); }
            set { SetValue(UsernameProperty, value); }
        }

        public string Password
        {
            get { return (string)GetValue(PasswordProperty); }
            set { SetValue(PasswordProperty, value); }
        }

        public AxisCamera Camera
        {
            get { return (AxisCamera)GetValue(CameraProperty); }
            set { SetValue(CameraProperty, value); }
        }

        public AxisResolution Resolution
        {
            get { return (AxisResolution)GetValue(ResolutionProperty); }
            set { SetValue(ResolutionProperty, value); }
        }

        public int Compression
        {
            get { return (int)GetValue(CompressionProperty); }
            set { SetValue(CompressionProperty, value); }
        }

        public int FPS
        {
            get { return (int)GetValue(FPSProperty); }
            set { SetValue(FPSProperty, value); }
        }

        #region Register Properties as a DependencyProperty
        private static readonly DependencyProperty IsDebuggingProperty =
          DependencyProperty.Register("IsDebugging", typeof(bool),
            typeof(AxisCameraStream), new PropertyMetadata(false, PropertyChangedCallback));

        private static readonly DependencyProperty IpAddressProperty =
          DependencyProperty.Register("IpAddress", typeof(string),
            typeof(AxisCameraStream), new PropertyMetadata("", PropertyChangedCallback));

        private static readonly DependencyProperty PortProperty =
          DependencyProperty.Register("Port", typeof(int),
            typeof(AxisCameraStream), new PropertyMetadata(0, PropertyChangedCallback));

        private static readonly DependencyProperty UsernameProperty =
          DependencyProperty.Register("Username", typeof(string),
            typeof(AxisCameraStream), new PropertyMetadata("", PropertyChangedCallback));

        private static readonly DependencyProperty PasswordProperty =
          DependencyProperty.Register("Password", typeof(string),
            typeof(AxisCameraStream), new PropertyMetadata("", PropertyChangedCallback));

        private static readonly DependencyProperty CameraProperty =
          DependencyProperty.Register("Camera", typeof(AxisCamera),
            typeof(AxisCameraStream), new PropertyMetadata(AxisCamera.ONE, PropertyChangedCallback));

        private static readonly DependencyProperty ResolutionProperty =
          DependencyProperty.Register("Resolution", typeof(AxisResolution),
            typeof(AxisCameraStream), new PropertyMetadata(AxisResolution.LOW_360, PropertyChangedCallback));

        private static readonly DependencyProperty CompressionProperty =
          DependencyProperty.Register("Compression", typeof(int),
            typeof(AxisCameraStream), new PropertyMetadata(0, PropertyChangedCallback));

        private static readonly DependencyProperty FPSProperty =
          DependencyProperty.Register("FPS", typeof(int),
            typeof(AxisCameraStream), new PropertyMetadata(0, PropertyChangedCallback));
        #endregion

        public uint MeasuredFPS
        {
            get { return _measuredFPS; }
            set
            {
                if (_measuredFPS < 0) return;
                _measuredFPS = value;
                NotifyPropertyChanged("MeasuredFPS");
            }
        }

        public ImageSource ImageSource
        {
            get { return _imageSource; }
            set { _imageSource = value; NotifyPropertyChanged("ImageSource"); }
        }

        private string _StreamUrl
        {
            get
            {
                return string.Format("http://{0}:{1}/axis-cgi/mjpg/video.cgi?{2}", IpAddress, Port, _StreamParameters);
            }
        }

        private string _StreamParameters
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                if (Camera != AxisCamera.UNKNOWN)
                {
                    sb.Append(string.Format("camera={0}&", (int)Camera));
                }

                if (Resolution != AxisResolution.UNKNOWN)
                {
                    int width = 0;
                    int height = 0;

                    switch (Resolution)
                    {
                        case AxisResolution.LOW_360:
                            width = 640;
                            height = 360;
                            break;
                        case AxisResolution.MED_480:
                            width = 854;
                            height = 480;
                            break;
                        case AxisResolution.HIGH_720:
                            width = 1280;
                            height = 720;
                            break;
                        case AxisResolution.EXTREME_1080:
                            width = 1920;
                            height = 1080;
                            break;
                    }

                    sb.Append(string.Format("resolution={0}x{1}&", width, height));
                }

                if (Compression >= 0 && Compression <= 100)
                {
                    sb.Append(string.Format("compression={0}&", Compression));
                }

                if (FPS > 0)
                {
                    sb.Append(string.Format("fps={0}&", FPS));
                }

                return sb.ToString();
            }
        }

        public AxisCameraStream()
        {
            InitializeComponent();
            MainViewbox.DataContext = this;
            //videoImage.Source = new BitmapImage(new Uri("grid.bmp", UriKind.Relative));
        }

        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //Console.WriteLine("Prop changed: {0}={1}", e.Property.Name, e.NewValue);
        }

        public bool OpenFeed()
        {
            string url = _StreamUrl;
            _axisStream = new MJPEGStreamer(url);
            _axisStream.ImageReady += _axisStream_NewFrame;

            _axisStream.StartProcessing();

            frameReceivedStopwatch.Start();
            lastFrameReceivedTime = 0;

            return true;
        }

        public void CloseFeed()
        {
            if (_axisStream.IsOpen)
            {
                _axisStream.StopProcessing();
                frameReceivedStopwatch.Stop();
                lastFrameReceivedTime = 0;
                Thread.Sleep(100);
            }
        }

        private Stopwatch frameReceivedStopwatch = new Stopwatch();
        private long lastFrameReceivedTime = 0;
        private object imageLocker = new object();

        private void _axisStream_NewFrame(object sender, ImageReadyEventArsgs eventArgs)
        {
            if (!_axisStream.IsOpen) return;

            lock(imageLocker)
            {
                long currentTime = frameReceivedStopwatch.ElapsedMilliseconds;
                if (currentTime < lastFrameReceivedTime) return;

                videoImage.Source = eventArgs.Image;
                MeasuredFPS = (uint)(1.0 / ((currentTime - lastFrameReceivedTime) / 1000.0));
                lastFrameReceivedTime = currentTime;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public enum AxisCamera
    {
        UNKNOWN = 0,
        ONE = 1,
        TWO = 2,
        THREE = 3,
        FOUR = 4
    }

    public enum AxisResolution
    {
        UNKNOWN = 0,
        LOW_360 = 1,
        MED_480 = 2,
        HIGH_720 = 3,
        EXTREME_1080 = 4
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(System.Windows.Visibility))
                throw new InvalidOperationException("The target must be a boolean");

            return (bool)value ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
