using ConquerButler.Tasks;
using PropertyChanged;
using System.Windows.Controls;

namespace ConquerButler.Gui.Tasks
{
    [ImplementPropertyChanged]
    public class MiningTaskViewModel
    {
    }

    public partial class MiningTaskView : UserControl, ConquerTaskFactory
    {
        public MiningTaskViewModel Model { get; set; } = new MiningTaskViewModel();

        public MiningTaskView()
        {
            InitializeComponent();
        }

        public ConquerTask CreateTask(ConquerProcess process)
        {
            return new MiningTask(process);
        }
    }
}
