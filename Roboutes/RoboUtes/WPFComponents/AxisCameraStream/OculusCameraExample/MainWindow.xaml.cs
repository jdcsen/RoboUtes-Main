using Axis;
using BarrelShaderLibrary;
using OculusDisplay;
using System;
using System.Windows;

namespace OculusCameraExample
{
    public partial class MainWindow : Window
    {
        private Oculus oculus;
        private BarrelDistortion shaderForOculus;
        public MainWindow()
        {
            DataContext = this;
            //oculus = new Oculus();
            shaderForOculus = new BarrelDistortion();
            //oculus.MovementPrecision = 1.0;
            //oculus.PositionChanged += Oculus_PositionChanged;

            InitializeComponent();
        }

        private void Oculus_PositionChanged(object sender, OculusPositionChangedEventArgs e)
        {
            Console.WriteLine("Looking at ({0},{1},{2})", e.EulerAngles.X, e.EulerAngles.Y, e.EulerAngles.Z);

            // Send command to server to adjust webcam
            // client.UpdatePanTilt(e.X, e.Y, e.Z);
        }

        private void btn_OpenFeed_Click(object sender, RoutedEventArgs e)
        {
            if (btn_OpenFeed.Content as string == "Open Feed")
            {
                if (acs_ExampleVideo.OpenFeed())
                {
                    btn_OpenFeed.Content = "Close Feed";
                }
            }
            else
            {
                acs_ExampleVideo.CloseFeed();
                btn_OpenFeed.Content = "Open Feed";
            }
        }

        private void sldr_Resolution_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            acs_ExampleVideo.Resolution = (Axis.AxisResolution)e.NewValue;
        }

        private void sldr_Compression_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            acs_ExampleVideo.Compression = (int)e.NewValue;
        }

        private void sldr_FPS_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            acs_ExampleVideo.FPS = (int)e.NewValue;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            acs_ExampleVideo.Effect = shaderForOculus;
            image_Right.Effect = shaderForOculus;
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
    }
}
