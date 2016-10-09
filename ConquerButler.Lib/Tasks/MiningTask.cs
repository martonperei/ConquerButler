﻿using Accord.Extensions.Imaging.Algorithms.LINE2D;
using AForge.Imaging;
using log4net;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using TemplatePyramid = Accord.Extensions.Imaging.Algorithms.LINE2D.ImageTemplatePyramid<Accord.Extensions.Imaging.Algorithms.LINE2D.ImageTemplate>;

namespace ConquerButler.Tasks
{
    public class MiningTask : ConquerTask
    {
        private static ILog log = LogManager.GetLogger(typeof(MiningTask));

        public static string TASK_TYPE_NAME = "Mining";

        private readonly TemplatePyramid _copperoreTemplate;
        private readonly TemplatePyramid _ironoreTemplate;

        public int OreCount { get; protected set; }

        public override string ResultDisplayInfo { get { return $"{OreCount}"; } }

        public MiningTask(ConquerProcess process)
            : base(TASK_TYPE_NAME, process)
        {
            _copperoreTemplate = LoadTemplate("images/copperore.png");
            _ironoreTemplate = LoadTemplate("images/ironore.png");

            Interval = 240;
            IntervalVariance = 60;
        }

        public override async Task DoTick()
        {
            List<Match> matches = FindMatches(0.95f, ConquerControls.INVENTORY, _ironoreTemplate, _copperoreTemplate);

            OreCount = matches.Count;

            List<Task> taskList = new List<Task>();

            for (int i = matches.Count - 1; i >= 0 && i >= matches.Count - 19; i--)
            {
                Match m = matches[i];

                taskList.Add(RequestInputFocus(() =>
                {
                    Process.MoveToPoint(MatchToPoint(m));
                    Process.LeftClickOnPoint(MatchToPoint(m));
                    Scheduler.Wait(125);
                    Process.LeftClickOnPoint(new Point(700, 100), 20);
                    Scheduler.Wait(125);

                    OreCount--;
                }, i));
            }

            await Task.WhenAll(taskList);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
