using ConquerButler.Tasks;
using PropertyChanged;
using System;
using System.Windows.Controls;

namespace ConquerButler.Gui.Tasks
{
    [ImplementPropertyChanged]
    public class HealthWatcherTaskViewModel : ConquerTaskViewModel
    {
        public HealthChangeAction HealthChangeAction { get; set; }
    }

    public partial class HealthWatcherTaskView : UserControl, ConquerTaskViewBase<HealthWatcherTaskViewModel>
    {
        public HealthWatcherTaskViewModel Model { get; set; } = new HealthWatcherTaskViewModel()
        {
            HealthChangeAction = HealthChangeAction.Exit,
            Interval = 2
        };

        public HealthWatcherTaskView()
        {
            InitializeComponent();
        }

        public ConquerTask CreateTask(ConquerProcess process)
        {
            var task = new HealthWatcherTask(process);

            task.Interval = Model.Interval;
            task.Priority = Model.Priority;
            task.NeedsUserFocus = Model.NeedsUserFocus;
            task.NeedsToBeConnected = Model.NeedsToBeConnected;
            task.HealthChangeAction = Model.HealthChangeAction;

            return task;
        }
    }
}
