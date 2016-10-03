using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ConquerButler
{
    public static class BitmapExtensions
    {
        public static Bitmap ConvertToFormat(this Image image, PixelFormat format)
        {
            Bitmap copy = new Bitmap(image.Width, image.Height, format);
            using (Graphics gr = Graphics.FromImage(copy))
            {
                gr.DrawImage(image, new Rectangle(0, 0, copy.Width, copy.Height));
            }
            return copy;
        }

        public static BitmapSource ToBitmapSource(this Bitmap bitmap)
        {
            IntPtr hbitmap = bitmap.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(hbitmap,
                    IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                NativeMethods.DeleteObject(hbitmap);
            }
        }
    }
}
