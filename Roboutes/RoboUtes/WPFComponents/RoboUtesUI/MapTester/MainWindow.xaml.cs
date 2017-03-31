using System;
using System.Windows;
using System.Windows.Data;

namespace MapTester
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void slider_Latitude_Longitude_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(slider_Longitude != null & slider_Latitude != null)
                mapDisplay.RoverLocation = new double[] { slider_Latitude.Value, slider_Longitude.Value + -95.081550 };
        }
    }
}
