using System.Windows;

namespace CameraStreamExample
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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

        }

        private void sldr_FPS_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

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
