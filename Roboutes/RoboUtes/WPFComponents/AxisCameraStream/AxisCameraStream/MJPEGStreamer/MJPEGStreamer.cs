using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace AxisCameraStream.MJPEGStreamer
{
    public class MJPEGStreamer
    {
        private string _url;
        private HttpClient _client;
        private AutomaticMultiPartReader _reader;
        private BitmapImage _currentFrame;
        public bool IsOpen { get; private set; }


        public MJPEGStreamer(string url)
        {
            _url = url;
            WebRequestHandler handler = new WebRequestHandler();
            _client = new HttpClient(handler);
            _client.BaseAddress = new Uri(_url);
            _client.Timeout = TimeSpan.FromMilliseconds(-1);
        }

        private void _reader_PartReady(object sender, PartReadyEventArgs e)
        {
            Stream frameStream = new MemoryStream(e.Part);
            System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                try
                {
                    _currentFrame = new BitmapImage();
                    _currentFrame.BeginInit();
                    _currentFrame.StreamSource = frameStream;
                    _currentFrame.EndInit();
                    OnImageReady();
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Video streaming error");
                }
            }));
        }

        protected void OnImageReady()
        {
            if (ImageReady != null)
            {
                ImageReady(this, new ImageReadyEventArsgs() { Image = _currentFrame });
            }
        }


        public async void StartProcessing()
        {
            HttpResponseMessage resultMessage = await _client.GetAsync(_url, HttpCompletionOption.ResponseHeadersRead);
            resultMessage.EnsureSuccessStatusCode();

            if (!resultMessage.Content.Headers.ContentType.MediaType.Contains("multipart"))
            {
                throw new ArgumentException("The camera did not return a mjpeg stream");
            }
            else
            {
                _reader = new AutomaticMultiPartReader(new MultiPartStream(await resultMessage.Content.ReadAsStreamAsync()));
                _reader.PartReady += _reader_PartReady;
                _reader.StartProcessing();
                IsOpen = true;
            }
        }

        public void StopProcessing()
        {
            if (_reader != null)
            {
                _reader.StopProcessing();
                IsOpen = false;
            }
        }

        public event EventHandler<ImageReadyEventArsgs> ImageReady;

    }

    public class ImageReadyEventArsgs : EventArgs
    {
        public BitmapImage Image { get; set; }
    }
}
