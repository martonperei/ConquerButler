using ConquerButler.Tasks;
using PropertyChanged;
using System;
using System.Windows.Controls;

namespace ConquerButler.Gui.Views.Tasks
{
    public class StatsWatcherTaskViewModel : ConquerTaskViewModel
    {
    }

    public partial class StatsWatcherTaskView : UserControl, ConquerTaskViewBase<StatsWatcherTaskViewModel>
    {
        public StatsWatcherTaskViewModel Model { get; set; } = new StatsWatcherTaskViewModel()
        {
            Interval = 1000,
            TaskType = StatsWatcherTask.TASK_TYPE_NAME
        };

        public StatsWatcherTaskView()
        {
            InitializeComponent();
        }

        public ConquerTask CreateTask(ConquerProcess process)
        {
            var task = new StatsWatcherTask(process);

            task.Interval = Model.Interval;
            task.Priority = Model.Priority;
            task.NeedsUserFocus = Model.NeedsUserFocus;
            task.TaskType = Model.TaskType;

            task.NeedsToBeConnected = Model.NeedsToBeConnected;

            return task;
        }
    }
}
