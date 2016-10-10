using ConquerButler.Tasks;
using PropertyChanged;
using System.Windows.Controls;

namespace ConquerButler.Gui.Tasks
{
    [ImplementPropertyChanged]
    public class HuntingTaskViewModel : ConquerTaskViewModel
    {
    }

    public partial class HuntingTaskView : UserControl, ConquerTaskViewBase<HuntingTaskViewModel>
    {
        public HuntingTaskViewModel Model { get; set; } = new HuntingTaskViewModel()
        {
            NeedsUserFocus = true,
            Interval = 0
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

            return task;
        }
    }
}
