using Accord.Extensions.Imaging.Algorithms.LINE2D;
using log4net;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using TemplatePyramid = Accord.Extensions.Imaging.Algorithms.LINE2D.ImageTemplatePyramid<Accord.Extensions.Imaging.Algorithms.LINE2D.ImageTemplate>;

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

        private readonly TemplatePyramid _lowhpTemplate;
        private readonly TemplatePyramid _fullhpTemplate;

        public HealthState HealthState { get; protected set; }

        public event Action<HealthState, HealthState> HealthChanged;

        public HealthChangeAction HealthChangeAction { get; set; } = HealthChangeAction.Exit;

        public override string ResultDisplayInfo { get { return $"{HealthState}"; } }

        public HealthWatcherTask(ConquerProcess process)
            : base(TASK_TYPE_NAME, process)
        {
            _lowhpTemplate = LoadTemplate("images/lowhp.png");
            _fullhpTemplate = LoadTemplate("images/fullhp.png");

            HealthState = HealthState.None;

            Interval = 2;
            IntervalVariance = 0;
        }

        public override Task DoTick()
        {
            List<Match> isFullHp = FindMatches(0.95f, ConquerControls.HEALTH, _fullhpTemplate);

            HealthState newHealthState = HealthState.Unknown;

            if (isFullHp.Count > 0)
            {
                newHealthState = HealthState.Full;
            }
            else
            {
                List<Match> isLowHp = FindMatches(0.95f, ConquerControls.HEALTH, _lowhpTemplate);

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
                    switch (HealthChangeAction)
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
        }
    }
}
