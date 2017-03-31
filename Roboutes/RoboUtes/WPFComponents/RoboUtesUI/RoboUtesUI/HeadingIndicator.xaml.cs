using System.Windows.Controls;
using System.Windows.Media;

namespace RoboUtes
{
    public partial class HeadingIndicator : UserControl
    {
        private int _Heading = 0;
        public int Heading
        {
            get
            {
                return _Heading;
            }
            set
            {
                _Heading = value;
                Dispatcher.Invoke(() => digitsImage.RenderTransform = new RotateTransform(-_Heading));
            }
        }

        public HeadingIndicator()
        {
            InitializeComponent();
        }
    }
}
