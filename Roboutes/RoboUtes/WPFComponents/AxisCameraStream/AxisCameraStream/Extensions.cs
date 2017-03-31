using System;
using System.Drawing;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Axis
{
    public static class Extensions
    {
        public static BitmapSource GetBitmapSource(this Bitmap source)
        {
            var bitmapData = source.LockBits(
                new System.Drawing.Rectangle(0, 0, source.Width, source.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, source.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height, 96, 96, PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            source.UnlockBits(bitmapData);
            return bitmapSource;
        }
    }
}
