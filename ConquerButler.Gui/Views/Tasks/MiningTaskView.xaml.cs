using ConquerButler.Tasks;
using System.Windows.Controls;

namespace ConquerButler.Gui.Views.Tasks
{
    public class MiningTaskViewModel : ConquerTaskViewModel
    {
        public int HealthThreshold { get; set; }
    }

    public partial class MiningTaskView : UserControl, ConquerTaskViewBase<MiningTaskViewModel>
    {
        public MiningTaskViewModel Model { get; set; } = new MiningTaskViewModel()
        {
            Interval = 120000,
            TaskType = MiningTask.TASK_TYPE_NAME,
            HealthThreshold = 50
        };

        public MiningTaskView()
        {
            InitializeComponent();
        }

        public ConquerTask CreateTask(ConquerProcess process)
        {
            return new MiningTask(process)
            {
                Interval = Model.Interval,
                Priority = Model.Priority,
                NeedsUserFocus = Model.NeedsUserFocus,
                NeedsToBeConnected = Model.NeedsToBeConnected,
                TaskType = Model.TaskType,
                HealthThreshold = Model.HealthThreshold
            };
        }
    }
}
