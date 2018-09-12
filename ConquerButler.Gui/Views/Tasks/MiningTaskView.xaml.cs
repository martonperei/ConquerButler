using ConquerButler.Tasks;
using PropertyChanged;
using System.Windows.Controls;

namespace ConquerButler.Gui.Views.Tasks
{
    public class MiningTaskViewModel : ConquerTaskViewModel
    {
    }

    public partial class MiningTaskView : UserControl, ConquerTaskViewBase<MiningTaskViewModel>
    {
        public MiningTaskViewModel Model { get; set; } = new MiningTaskViewModel()
        {
            Interval = 60,
            TaskType = MiningTask.TASK_TYPE_NAME
        };

        public MiningTaskView()
        {
            InitializeComponent();
        }

        public ConquerTask CreateTask(ConquerProcess process)
        {
            var task = new MiningTask(process);

            task.Interval = Model.Interval;
            task.Priority = Model.Priority;
            task.NeedsUserFocus = Model.NeedsUserFocus;
            task.NeedsToBeConnected = Model.NeedsToBeConnected;
            task.TaskType = Model.TaskType;

            return task;
        }
    }
}
