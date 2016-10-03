using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using AForge.Imaging;

namespace ConquerButler
{
    public class ImageDetectionHelper
    {
        public static Bitmap LoadImage(string fileName)
        {
            return new Bitmap(fileName).ConvertToFormat(PixelFormat.Format24bppRgb);
        }

        public static List<TemplateMatch> Detect(float similiarityThreshold, Bitmap sourceImage, params Bitmap[] templates)
        {
            ExhaustiveTemplateMatching tm = new ExhaustiveTemplateMatching(similiarityThreshold);

            List<TemplateMatch> matches = new List<TemplateMatch>();

            foreach (Bitmap template in templates)
            {
                TemplateMatch[] match = tm.ProcessImage(sourceImage, template);

                matches.AddRange(match);
            }

            return matches;
        }

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
