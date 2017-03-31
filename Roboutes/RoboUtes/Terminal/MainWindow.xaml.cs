using System.Windows;

namespace RoboUtes.Terminal
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btn_Logistics_Click(object sender, RoutedEventArgs e)
        {
            LogisticsTerminal lt = new LogisticsTerminal();
            lt.Show();
        }

        private void btn_Engineering_Click(object sender, RoutedEventArgs e)
        {
            EngineeringTerminal et = new EngineeringTerminal();
            et.Show();
        }

        private void btn_Drive_Click(object sender, RoutedEventArgs e)
        {
            DriveTerminal dt = new DriveTerminal();
            dt.Show();
        }

        private void btn_Arm_Click(object sender, RoutedEventArgs e)
        {
            ArmTerminal at = new ArmTerminal();
            at.Show();
        }
    }
}
