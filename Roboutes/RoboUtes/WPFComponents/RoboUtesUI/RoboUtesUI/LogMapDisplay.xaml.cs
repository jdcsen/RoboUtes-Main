using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Maps.MapControl.WPF;


namespace RoboUtes
{
    /// <summary>
    /// Interaction logic for LogMapDisplay.xaml
    /// </summary>
    public partial class LogMapDisplay : UserControl
    {
        public LogMapDisplay()
        {
            InitializeComponent();
            Center = new double[] { 29.564795, -95.081550 };
            RoverLocation = Center;
            //  map.Focus();
        }

        private double[] _center;
        private double[] _roverLocation;
        private MapPolygon _roverPolygon = new MapPolygon() { Fill = Brushes.White, Stroke = Brushes.Black, StrokeThickness = 3 };
        Pushpin pin;

        public double[] Center
        {
            get { return _center; }
            set
            {
                _center = value;
                map.Center = new Location(value[0], value[1]);
            }
        }

        public double[] RoverLocation
        {
            get { return _roverLocation; }
            set
            {
                _roverLocation = value;
                map.Children.Remove(_roverPolygon);
                _roverPolygon.Locations = new LocationCollection() {
                                            new Location(value[0], value[1]),
                                            new Location(value[0] + 0.00003, value[1]),
                                            new Location(value[0] + 0.00003, value[1] + 0.00003),
                                            new Location(value[0], value[1] + 0.00003)
                };
                map.Children.Add(_roverPolygon);
            }
        }

        private void AddPushpins(object sender, MouseButtonEventArgs e)
        {

            e.Handled = true;
            Point mousePosition = e.GetPosition(this);

            Location pinLocation = map.ViewportPointToLocation(mousePosition);

            pin = new Pushpin();
            pin.Location = pinLocation;
            pin.Tag = "p1";
            map.Children.Add(pin);
        }

        private void RemovePushpins(object sender, MouseButtonEventArgs e)
        {
            List<UIElement> remove = new List<UIElement>();
            foreach (UIElement element in map.Children)
            {
                if (element.GetType() == typeof(Pushpin))
                {
                    Pushpin pin = (Pushpin)element;
                    if (pin != null)
                    {

                        if ((string)pin.Tag == "p1")
                            remove.Add(element);
                    }
                }
            }
            foreach (UIElement element in remove)
            {
                map.Children.Remove(element);
            }

        }
    }
}
