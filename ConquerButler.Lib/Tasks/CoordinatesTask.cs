using Accord.Extensions.Imaging.Algorithms.LINE2D;
using log4net;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TemplatePyramid = Accord.Extensions.Imaging.Algorithms.LINE2D.ImageTemplatePyramid<Accord.Extensions.Imaging.Algorithms.LINE2D.ImageTemplate>;

namespace ConquerButler.Tasks
{
    public class Match2 : IComparable<Match2>
    {
        public int Number;
        public int Position;

        public int CompareTo(Match2 other)
        {
            return other.Position.CompareTo(Position);
        }
    }

    public class CoordinatesTask : ConquerTask
    {
        private static ILog log = LogManager.GetLogger(typeof(CoordinatesTask));

        public static string TASK_TYPE_NAME = "Coordinates";

        private readonly List<TemplatePyramid> _templates;

        public CoordinatesTask(ConquerProcess process)
            : base(TASK_TYPE_NAME, process)
        {
            Interval = 100;

            _templates = new List<TemplatePyramid>() {
                LoadTemplate("images/0.png", "0"),
                LoadTemplate("images/1.png", "1"),
                LoadTemplate("images/2.png", "2"),
                LoadTemplate("images/3.png", "3"),
                LoadTemplate("images/4.png", "4"),
                LoadTemplate("images/5.png", "5"),
                LoadTemplate("images/6.png", "6"),
                LoadTemplate("images/7.png", "7"),
                LoadTemplate("images/8.png", "8"),
                LoadTemplate("images/9.png", "9")
            };
        }

        protected override Task DoTick()
        {
            List<Match2> numbers = new List<Match2>(6);

            List<Match> matches = FindMatches(0.9f, ConquerControlConstants.COORDINATES, _templates);

            foreach (Match match in matches)
            {
                numbers.Add(new Match2() { Position = match.X, Number = int.Parse(match.Template.ClassLabel) });
            }

            numbers.Sort();

            return Task.FromResult(true);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}