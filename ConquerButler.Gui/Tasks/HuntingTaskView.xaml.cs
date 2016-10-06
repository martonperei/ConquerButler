using ConquerButler.Tasks;
using PropertyChanged;
using System.Windows.Controls;

namespace ConquerButler.Gui.Tasks
{
    [ImplementPropertyChanged]
    public class HuntingTaskViewModel
    {
    }

    public partial class HuntingTaskView : UserControl, ConquerTaskFactory
    {
        public HuntingTaskViewModel Model { get; set; } = new HuntingTaskViewModel();

        public HuntingTaskView()
        {
            InitializeComponent();
        }

        public ConquerTask CreateTask(ConquerProcess process)
        {
            return new HuntingTask(process);
        }
    }
}
