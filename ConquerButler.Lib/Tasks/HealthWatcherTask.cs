using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using AForge.Imaging;

namespace ConquerButler.Tasks
{
    public enum HealthState
    {
        Unknown,
        Low,
        Full
    }

    public class HealthWatcherTask : ConquerTask
    {
        private readonly Bitmap lowhpTemplate;
        private readonly Bitmap fullhpTemplate;

        private HealthState healthState;

        public override string DisplayInfo { get { return $"{TaskType} State: {healthState} | Running: {IsRunning} Next run: {NextRun} ms"; } }

        public HealthWatcherTask(ConquerInputScheduler scheduler, ConquerProcess process)
            : base("HealthWatcher", scheduler, process)
        {
            lowhpTemplate = LoadImage("images/lowhp.png");
            fullhpTemplate = LoadImage("images/fullhp.png");

            healthState = HealthState.Unknown;

            Interval = 1000;
        }

        public override Task DoTick()
        {
            List<TemplateMatch> isFullHp = Process.FindMatches(0.95f, ConquerControls.HEALTH, fullhpTemplate);

            if (isFullHp.Count > 0)
            {
                healthState = HealthState.Full;
            }
            else
            {
                List<TemplateMatch> isLowHp = Process.FindMatches(0.95f, ConquerControls.HEALTH, lowhpTemplate);

                if (isLowHp.Count > 0)
                {
                    if (healthState == HealthState.Full)
                    {
                        Process.InternalProcess.CloseMainWindow();
                    }
                    else
                    {
                        healthState = HealthState.Low;
                    }
                }
                else
                {
                    healthState = HealthState.Unknown;
                }
            }

            return Task.FromResult(true);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            lowhpTemplate.Dispose();
            fullhpTemplate.Dispose();
        }
    }
}
