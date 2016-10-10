using ConquerButler.Tasks;
using PropertyChanged;
using System.Windows.Controls;

namespace ConquerButler.Gui.Tasks
{
    [ImplementPropertyChanged]
    public class CustomTaskViewModel : ConquerTaskViewModel
    {
    }

    public partial class CustomTaskView : UserControl, ConquerTaskViewBase<CustomTaskViewModel>
    {
        public CustomTaskViewModel Model { get; set; } = new CustomTaskViewModel();

        public CustomTaskView()
        {
            InitializeComponent();
        }

        public ConquerTask CreateTask(ConquerProcess process)
        {
            var task = new CustomTask(process);

            task.Interval = Model.Interval;
            task.Priority = Model.Priority;
            task.NeedsUserFocus = Model.NeedsUserFocus;
            task.NeedsToBeConnected = Model.NeedsToBeConnected;

            return task;
        }
    }
}
