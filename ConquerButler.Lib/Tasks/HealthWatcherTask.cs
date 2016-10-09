using AForge.Imaging;
using log4net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConquerButler.Tasks
{
    public enum HealthState
    {
        Low,
        Full,
        Unknown,
        None
    }

    public enum HealthChangeAction
    {
        Exit,
        Notify
    }

    public class HealthWatcherTask : ConquerTask
    {
        private static ILog log = LogManager.GetLogger(typeof(HealthWatcherTask));

        public static string TASK_TYPE_NAME = "HealthWatcher";

        private readonly Bitmap _lowhpTemplate;
        private readonly Bitmap _fullhpTemplate;

        public HealthState HealthState { get; protected set; }

        public event Action<HealthState, HealthState> HealthChanged;

        public HealthChangeAction OnHealthLow { get; set; } = HealthChangeAction.Notify;

        public override string ResultDisplayInfo { get { return $"{HealthState}"; } }

        public HealthWatcherTask(ConquerProcess process)
            : base(TASK_TYPE_NAME, process)
        {
            _lowhpTemplate = LoadImage("images/lowhp.png");
            _fullhpTemplate = LoadImage("images/fullhp.png");

            HealthState = HealthState.None;

            Interval = 5;
            IntervalVariance = 0;
        }

        public override Task DoTick()
        {
            List<TemplateMatch> isFullHp = FindMatches(0.95f, ConquerControls.HEALTH, _fullhpTemplate);

            HealthState newHealthState = HealthState.Unknown;

            if (isFullHp.Count > 0)
            {
                newHealthState = HealthState.Full;
            }
            else
            {
                List<TemplateMatch> isLowHp = FindMatches(0.95f, ConquerControls.HEALTH, _lowhpTemplate);

                if (isLowHp.Count > 0)
                {
                    newHealthState = HealthState.Low;
                }
            }

            if (!HealthState.Equals(newHealthState))
            {
                log.Info($"Process {Process.Id} - task {TaskType} - Health changed from {HealthState} to {newHealthState}");

                if (newHealthState == HealthState.Unknown || newHealthState == HealthState.Low)
                {
                    switch (OnHealthLow)
                    {
                        case HealthChangeAction.Exit:
                            Process.InternalProcess.CloseMainWindow();
                            break;
                        case HealthChangeAction.Notify:
                            MessageBox.Show($"Process {Process.Id} - task {TaskType} - Health changed from {HealthState} to {newHealthState}");
                            break;
                    }
                }

                HealthChanged?.Invoke(HealthState, newHealthState);

                HealthState = newHealthState;
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
