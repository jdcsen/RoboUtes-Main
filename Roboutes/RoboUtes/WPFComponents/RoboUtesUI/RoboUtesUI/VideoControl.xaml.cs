using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Windows.Controls;

namespace RoboUtes
{
    public partial class VideoControl : UserControl
    {
        public delegate void VideoSettingChangedHandler(object sender, VideoControlEventArgs e);

        public event VideoSettingChangedHandler VideoSettingsChanged;

        private VideoSettings driveVideoSettings = new VideoSettings();
        private VideoSettings armVideoSettings = new VideoSettings();
        private VideoSettings logisticsVideoSettings = new VideoSettings();

        private List<int> armReportedFpss = new List<int>();
        private List<int> driveReportedFpss = new List<int>();
        private List<int> logisticsReportedFpss = new List<int>();

        private Timer adjustVideoTimer = new Timer(30000);

        public VideoControl()
        {
            InitializeComponent();
            adjustVideoTimer.Elapsed += AdjustVideoTimer_Elapsed;
            adjustVideoTimer.Start();
        }

        public void SetDriveVideoSettings(VideoSettings settings)
        {
            Dispatcher.Invoke(() => 
            {
                driveVideoSettings = settings;
                if (settings.Camera > 0) comboBox_Drive_Camera.SelectedItem = (int)settings.Camera - 1;
                comboBox_Drive_Compression.SelectedIndex = settings.Compression / 10;
                if (settings.Resolution > 0) comboBox_Drive_Resolution.SelectedIndex = (int)settings.Resolution - 1;
                comboBox_Drive_FPS.SelectedIndex = settings.FPS / 10;
            });
        }
        public void SetArmVideoSettings(VideoSettings settings)
        {
            Dispatcher.Invoke(() => 
            {
                armVideoSettings = settings;
                if(settings.Camera >0) comboBox_Arm_Camera.SelectedIndex = (int)settings.Camera - 1;
                comboBox_Arm_Compression.SelectedIndex = settings.Compression / 10;
                if(settings.Resolution > 0) comboBox_Arm_Resolution.SelectedIndex = (int)settings.Resolution - 1;
                comboBox_Arm_FPS.SelectedIndex = settings.FPS / 10;
            });
        }
        public void SetLogisticsVideoSettings(VideoSettings settings)
        {
            Dispatcher.Invoke(() => 
            {
                logisticsVideoSettings = settings;
                comboBox_Logistics_Camera.SelectedItem = (int)settings.Camera - 1;
                comboBox_Logistics_Compression.SelectedIndex = settings.Compression / 10;
                if (settings.Resolution > 0) comboBox_Logistics_Resolution.SelectedIndex = (int)settings.Resolution - 1;
                comboBox_Logistics_FPS.SelectedIndex = settings.FPS / 10;
            });
        }

        public void UpdateFromTerminal(string message)
        {
            var receivedSettings = VideoSettings.Parse(message);

            switch(receivedSettings.Terminal)
            {
                case TerminalEnum.Arm:
                    if (armVideoSettings.FPS == 0) armVideoSettings = receivedSettings;
                    armReportedFpss.Add(receivedSettings.ActualFPS);
                    break;
                case TerminalEnum.Drive:
                    if (driveVideoSettings.FPS == 0) driveVideoSettings = receivedSettings;
                    driveReportedFpss.Add(receivedSettings.ActualFPS);
                    break;
                case TerminalEnum.Logistics:
                    if (logisticsVideoSettings.FPS == 0) logisticsVideoSettings = receivedSettings;
                    logisticsReportedFpss.Add(receivedSettings.ActualFPS);
                    break;
                default:
                    throw new Exception("Unknown Terminal");
            }
        }

        private void AdjustVideoTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            doBalancing(ref armVideoSettings, ref armReportedFpss);
            doBalancing(ref driveVideoSettings, ref driveReportedFpss);
            doBalancing(ref logisticsVideoSettings, ref logisticsReportedFpss);
        }

        private void doBalancing(ref VideoSettings terminalSettings, ref List<int> reports)
        {
            int averageFps = reports.Count > 0 ? (int)reports.Average() : 0;
            int targetFps = armVideoSettings.FPS;
            int resultCompression = terminalSettings.Compression;

            if (averageFps + 2 < targetFps)
            {
                resultCompression += targetFps - averageFps;
            }
            else
            {
                resultCompression -= averageFps - targetFps;
            }

            resultCompression = resultCompression < 0 ? 0 : resultCompression;
            
            terminalSettings.Compression = resultCompression;
            if (VideoSettingsChanged != null) VideoSettingsChanged(this, new VideoControlEventArgs(terminalSettings));

            reports.Clear();
        }

        #region Combobox Event Handlers
        private void comboBox_Drive_Camera_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            driveVideoSettings.Camera = (AxisCamera)comboBox_Drive_Camera.SelectedIndex + 1;
        }

        private void comboBox_Drive_Resolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            driveVideoSettings.Resolution = (AxisResolution)comboBox_Drive_Resolution.SelectedIndex + 1;
        }

        private void comboBox_Drive_Compression_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            driveVideoSettings.Compression = comboBox_Drive_Compression.SelectedIndex * 10;
        }

        private void comboBox_Drive_FPS_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            driveVideoSettings.FPS = comboBox_Drive_FPS.SelectedIndex * 10;
        }

        private void comboBox_Arm_Camera_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            armVideoSettings.Camera = (AxisCamera)comboBox_Arm_Camera.SelectedIndex + 1;
        }

        private void comboBox_Arm_Resolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            armVideoSettings.Resolution = (AxisResolution)comboBox_Arm_Resolution.SelectedIndex + 1;
        }

        private void comboBox_Arm_Compression_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            armVideoSettings.Compression = comboBox_Arm_Compression.SelectedIndex * 10;
        }

        private void comboBox_Arm_FPS_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            armVideoSettings.FPS = comboBox_Arm_FPS.SelectedIndex * 10;
        }

        private void comboBox_Logistics_Camera_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            logisticsVideoSettings.Camera = (AxisCamera)comboBox_Logistics_Camera.SelectedIndex + 1;
        }

        private void comboBox_Logistics_Resolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            logisticsVideoSettings.Resolution = (AxisResolution)comboBox_Logistics_Resolution.SelectedIndex + 1;
        }

        private void comboBox_Logistics_Compression_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            logisticsVideoSettings.Compression = comboBox_Logistics_Compression.SelectedIndex * 10;
        }

        private void comboBox_Logistics_FPS_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            logisticsVideoSettings.FPS = comboBox_Logistics_FPS.SelectedIndex * 10;
        }
        #endregion
    }

    public class VideoSettings
    {
        public TerminalEnum Terminal { get; set; }
        public AxisCamera Camera { get; set; }
        public AxisResolution Resolution { get; set; }
        public int FPS { get; set; }
        public int Compression { get; set; }
        public int ActualFPS { get; set; }

        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3},{4},{5}", (int)Terminal, (int)Camera, (int)Resolution, FPS, Compression, ActualFPS);
        }

        public static VideoSettings Parse(string data)
        {
            return new VideoSettings()
            {
                Terminal = (TerminalEnum)Enum.Parse(typeof(TerminalEnum), data.Split(',')[0]),
                Camera = (AxisCamera)Enum.Parse(typeof(AxisCamera), data.Split(',')[1]),
                Resolution = (AxisResolution)Enum.Parse(typeof(AxisResolution), data.Split(',')[2]),
                FPS = int.Parse(data.Split(',')[3]),
                Compression = int.Parse(data.Split(',')[4]),
                ActualFPS = int.Parse(data.Split(',')[5])
            };
        }
    }

    public class VideoControlEventArgs : EventArgs
    {
        public TerminalEnum Terminal { get; private set; }
        public AxisCamera Camera { get; private set; }
        public AxisResolution Resolution { get; private set; }
        public int FPS { get; private set; }
        public int Compression { get; private set; }
        public int ActualFPS { get; set; }

        public VideoControlEventArgs(VideoSettings settings)
        {
            Terminal = settings.Terminal;
            Camera = settings.Camera;
            Resolution = settings.Resolution;
            FPS = settings.FPS;
            Compression = settings.Compression;
            ActualFPS = settings.ActualFPS;
        }

        public VideoControlEventArgs(TerminalEnum terminal, AxisCamera camera, AxisResolution resolution, int fps, int compression, int actualFps)
        {
            Terminal = terminal;
            Camera = camera;
            Resolution = resolution;
            FPS = fps;
            Compression = compression;
            ActualFPS = actualFps;
        }

        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3},{4},{5}", (int)Terminal, (int)Camera, (int)Resolution, FPS, Compression, ActualFPS);
        }
    }

    public enum TerminalEnum
    {
        Drive = 0,
        Arm = 1,
        Logistics = 2
    }
}
