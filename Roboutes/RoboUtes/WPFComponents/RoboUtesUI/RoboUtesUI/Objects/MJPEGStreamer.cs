using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace RoboUtes.Objects
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
            _client.Timeout = TimeSpan.FromMilliseconds(30000);
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
                catch (Exception ex)
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
            try
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
            catch(Exception ex)
            {
                Console.WriteLine("Error starting streaming: {0}", ex.Message);
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
