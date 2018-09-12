using ConquerButler.Tasks;
using PropertyChanged;
using System.Windows.Controls;

namespace ConquerButler.Gui.Views.Tasks
{
    public class HuntingTaskViewModel : ConquerTaskViewModel
    {
    }

    public partial class HuntingTaskView : UserControl, ConquerTaskViewBase<HuntingTaskViewModel>
    {
        public HuntingTaskViewModel Model { get; set; } = new HuntingTaskViewModel()
        {
            NeedsUserFocus = true,
            Interval = 0,
            TaskType = HuntingTask.TASK_TYPE_NAME
        };

        public HuntingTaskView()
        {
            InitializeComponent();
        }

        public ConquerTask CreateTask(ConquerProcess process)
        {
            var task = new HuntingTask(process);

            task.Interval = Model.Interval;
            task.Priority = Model.Priority;
            task.NeedsUserFocus = Model.NeedsUserFocus;
            task.NeedsToBeConnected = Model.NeedsToBeConnected;
            task.TaskType = Model.TaskType;

            return task;
        }
    }
}
