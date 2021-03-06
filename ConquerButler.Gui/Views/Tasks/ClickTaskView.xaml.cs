using ConquerButler.Tasks;
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
            return new ClickTask(process)
            {
                Interval = Model.Interval,
                Priority = Model.Priority,
                NeedsUserFocus = Model.NeedsUserFocus,
                NeedsToBeConnected = Model.NeedsToBeConnected,
                Wait = Model.Wait,
                MouseButton = Model.MouseButton,
                HoldCtrl = Model.HoldCtrl
            };
        }
    }
}
