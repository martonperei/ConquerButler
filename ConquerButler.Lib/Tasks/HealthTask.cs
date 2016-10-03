using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using AForge.Imaging;

namespace ConquerButler.Tasks
{
    public enum HealthState
    {
        Unknown,
        Low,
        Normal
    }

    public class HealthTask : ConquerTask
    {
        private readonly Bitmap hpTemplate;

        private HealthState healthState;

        public override string DisplayInfo { get { return $"{TaskType} State: {healthState} | Running: {IsRunning} Next run: {NextRun} ms"; } }

        public HealthTask(ConquerInputScheduler scheduler, Process process)
            : base("Health", scheduler, process)
        {
            hpTemplate = LoadImage("images/lowhp.png");

            healthState = HealthState.Unknown;

            Interval = 1000;
        }

        public override Task DoTick()
        {
            List<TemplateMatch> matches = FindMatches(0.95f, Snapshot(ConquerControls.HEALTH), hpTemplate);

            if (matches.Count > 0)
            {
                if (healthState == HealthState.Normal)
                {
                    Process.CloseMainWindow();
                }
                else
                {
                    healthState = HealthState.Low;
                }
            }
            else
            {
                healthState = HealthState.Normal;
            }

            return Task.FromResult(true);
        }
    }
}
