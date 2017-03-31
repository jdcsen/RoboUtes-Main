using System;
using System.Windows;

namespace VideoControlTester
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            videoControl.VideoSettingsChanged += VideoControl_VideoSettingsChanged;

            videoControl.SetArmVideoSettings(new RoboUtes.VideoSettings()
            {
                Camera = RoboUtes.AxisCamera.ONE,
                Terminal = RoboUtes.TerminalEnum.Arm,
                Compression = 50,
                FPS = 50,
                Resolution = RoboUtes.AxisResolution.MED_480
            });
        }

        private void VideoControl_VideoSettingsChanged(object sender, RoboUtes.VideoControlEventArgs e)
        {
            switch (e.Terminal)
            {
                case RoboUtes.TerminalEnum.Arm:
                    Console.WriteLine("Arm Video Changed: {0}", e.ToString());
                    break;
                case RoboUtes.TerminalEnum.Drive:
                    Console.WriteLine("Drive Video Changed: {0}", e.ToString());
                    break;
                case RoboUtes.TerminalEnum.Logistics:
                    Console.WriteLine("Logistics Video Changed: {0}", e.ToString());
                    break;
            }

            
        }
    }
}
