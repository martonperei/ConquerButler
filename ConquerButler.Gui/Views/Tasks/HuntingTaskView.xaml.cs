using ConquerButler.Tasks;
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
            Interval = 100,
            TaskType = HuntingTask.TASK_TYPE_NAME
        };

        public HuntingTaskView()
        {
            InitializeComponent();
        }

        public ConquerTask CreateTask(ConquerProcess process)
        {
            return new HuntingTask(process)
            {
                Interval = Model.Interval,
                Priority = Model.Priority,
                NeedsUserFocus = Model.NeedsUserFocus,
                NeedsToBeConnected = Model.NeedsToBeConnected,
                TaskType = Model.TaskType
            };
        }
    }
}
