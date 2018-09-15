using ConquerButler.Tasks;
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
            return new StatsWatcherTask(process)
            {
                Interval = Model.Interval,
                Priority = Model.Priority,
                NeedsUserFocus = Model.NeedsUserFocus,
                TaskType = Model.TaskType,
                NeedsToBeConnected = Model.NeedsToBeConnected
            };
        }
    }
}
