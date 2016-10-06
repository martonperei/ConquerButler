using ConquerButler.Tasks;
using PropertyChanged;
using System.Windows.Controls;

namespace ConquerButler.Gui.Tasks
{
    [ImplementPropertyChanged]
    public class HealthWatcherTaskViewModel
    {
    }

    public partial class HealthWatcherTaskView : UserControl, ConquerTaskFactory
    {
        public HealthWatcherTaskViewModel Model { get; set; } = new HealthWatcherTaskViewModel();

        public HealthWatcherTaskView()
        {
            InitializeComponent();
        }

        public ConquerTask CreateTask(ConquerProcess process)
        {
            return new HealthWatcherTask(process);
        }
    }
}
