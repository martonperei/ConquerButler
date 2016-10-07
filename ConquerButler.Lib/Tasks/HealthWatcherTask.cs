using AForge.Imaging;
using log4net;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

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
        private static ILog log = LogManager.GetLogger(typeof(HealthWatcherTask));

        public static string TASK_TYPE_NAME = "HealthWatcher";

        private readonly Bitmap _lowhpTemplate;
        private readonly Bitmap _fullhpTemplate;

        public HealthState HealthState { get; protected set; }

        public override string ResultDisplayInfo { get { return $"{HealthState}"; } }

        public HealthWatcherTask(ConquerProcess process)
            : base(TASK_TYPE_NAME, process)
        {
            _lowhpTemplate = LoadImage("images/lowhp.png");
            _fullhpTemplate = LoadImage("images/fullhp.png");

            HealthState = HealthState.Unknown;
        }

        public override Task DoTick()
        {
            List<TemplateMatch> isFullHp = FindMatches(0.95f, ConquerControls.HEALTH, _fullhpTemplate);

            if (isFullHp.Count > 0)
            {
                HealthState = HealthState.Full;
            }
            else
            {
                List<TemplateMatch> isLowHp = FindMatches(0.95f, ConquerControls.HEALTH, _lowhpTemplate);

                if (isLowHp.Count > 0)
                {
                    if (HealthState == HealthState.Full)
                    {
                        Process.InternalProcess.CloseMainWindow();
                    }
                    else
                    {
                        HealthState = HealthState.Low;
                    }
                }
                else
                {
                    HealthState = HealthState.Unknown;
                }
            }

            return Task.FromResult(true);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _lowhpTemplate.Dispose();
            _fullhpTemplate.Dispose();
        }
    }
}
