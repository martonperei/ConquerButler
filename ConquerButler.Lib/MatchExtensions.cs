using Accord.Extensions.Imaging.Algorithms.LINE2D;
using System.Drawing;

namespace ConquerButler
{
    public static class MatchExtensions
    {
        public static Point Center(this Match m)
        {
            return new Point(m.BoundingRect.X + m.BoundingRect.Width / 2, m.BoundingRect.Y + m.BoundingRect.Height / 2);
        }
    }
}
