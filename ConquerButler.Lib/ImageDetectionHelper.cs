using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using AForge.Imaging;

namespace ConquerButler
{
    public class ImageDetectionHelper
    {


        public static void HighlightMatches(Bitmap sourceImage, TemplateMatch[] matches)
        {
            BitmapData data = sourceImage.LockBits(
                new Rectangle(0, 0, sourceImage.Width, sourceImage.Height),
                ImageLockMode.ReadWrite, sourceImage.PixelFormat);
            foreach (TemplateMatch m in matches)
            {
                Drawing.Rectangle(data, m.Rectangle, Color.White);
            }
            sourceImage.UnlockBits(data);
        }
    }
}
