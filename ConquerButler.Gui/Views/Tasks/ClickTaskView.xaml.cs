using ConquerButler.Tasks;
using PropertyChanged;
using System.Windows.Controls;

namespace ConquerButler.Gui.Views.Tasks
{
    
    public class ClickTaskViewModel : ConquerTaskViewModel
    {
        public MouseButton MouseButton { get; set; }
        public bool HoldCtrl { get; set; }
        public int Wait { get; set; }
    }

    public partial class ClickTaskView : UserControl, ConquerTaskViewBase<ClickTaskViewModel>
    {
        public ClickTaskViewModel Model { get; set; } = new ClickTaskViewModel()
        {
            MouseButton = MouseButton.Left,
            NeedsUserFocus = true,
            Wait = 500,
            Interval = 100,
            HoldCtrl = false,
            TaskType = ClickTask.TASK_TYPE_NAME
        };

        public ClickTaskView()
        {
            InitializeComponent();
        }

        public ConquerTask CreateTask(ConquerProcess process)
        {
            var task = new ClickTask(process);

            task.Interval = Model.Interval;
            task.Priority = Model.Priority;
            task.NeedsUserFocus = Model.NeedsUserFocus;
            task.NeedsToBeConnected = Model.NeedsToBeConnected;
            task.Wait = Model.Wait;
            task.MouseButton = Model.MouseButton;
            task.HoldCtrl = Model.HoldCtrl;

            return task;
        }
    }
}
