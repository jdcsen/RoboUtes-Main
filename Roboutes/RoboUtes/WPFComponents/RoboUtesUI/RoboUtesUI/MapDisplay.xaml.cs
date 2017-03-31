using Microsoft.Maps.MapControl.WPF;
using System.Windows.Controls;
using System.Windows.Media;

namespace RoboUtes
{
    public partial class MapDisplay : UserControl
    {
        private double[] _center;
        private double[] _roverLocation;
        private MapPolygon _roverPolygon = new MapPolygon() { Fill = Brushes.White, Stroke = Brushes.Black, StrokeThickness = 3 };

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

        public MapDisplay()
        {
            InitializeComponent();
            Center = new double[] { 29.564795, -95.081550 };
            RoverLocation = Center;
        }
    }
}
