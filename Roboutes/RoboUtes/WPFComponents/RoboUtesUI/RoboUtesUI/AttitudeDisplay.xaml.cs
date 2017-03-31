using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace RoboUtes
{
    public partial class AttitudeDisplay : UserControl
    {
        public float LeftPitch
        {
            get
            {
                return (float)GetValue(LeftPitchProperty);
            }
            set
            {
                SetValue(LeftPitchProperty, value);
            }
        }

        public float RightPitch
        {
            get
            {
                return (float)GetValue(RightPitchProperty);
            }
            set
            {
                SetValue(RightPitchProperty, value);
            }
        }

        public float Roll
        {
            get
            {
                return (float)GetValue(RollProperty);
            }
            set
            {
                SetValue(RollProperty, value);
            }
        }


        private static readonly DependencyProperty LeftPitchProperty =
          DependencyProperty.Register("LeftPitch", typeof(float),
            typeof(AttitudeDisplay), new PropertyMetadata(0.0f, PropertyChangedCallback));

        private static readonly DependencyProperty RightPitchProperty =
          DependencyProperty.Register("RightPitch", typeof(float),
            typeof(AttitudeDisplay), new PropertyMetadata(0.0f, PropertyChangedCallback));

        private static readonly DependencyProperty RollProperty =
          DependencyProperty.Register("Roll", typeof(float),
            typeof(AttitudeDisplay), new PropertyMetadata(0.0f, PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //Console.WriteLine("Prop changed: {0}={1}", e.Property.Name, e.NewValue);
        }

        public AttitudeDisplay()
        {
            InitializeComponent();
            Viewbox.DataContext = this;
        }
    }

    public class PitchToTopValueConverter : IValueConverter
    {
        private float canvasRadius = 170;
        private float pitchLimit = 30;

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(double))
                throw new InvalidOperationException("The target must be a double");

            float fvalue = (float)value;

            float newValue = pitchLimit;
            if (fvalue >= -pitchLimit && fvalue <= pitchLimit)
            {
                newValue = fvalue;
            }
            else if (fvalue < -pitchLimit)
            {
                newValue = -pitchLimit;
            }

            float calculatedValue = (newValue / pitchLimit) * canvasRadius;
            return calculatedValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class DegreeLimiterConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(double))
                throw new InvalidOperationException("The target must be a double");

            float newValue = (float)value;
            return newValue < -180 || newValue > 180 ? 0 : -1 * newValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
