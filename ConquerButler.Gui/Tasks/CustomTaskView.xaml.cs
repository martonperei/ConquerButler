using ConquerButler.Tasks;
using PropertyChanged;
using System.Windows.Controls;

namespace ConquerButler.Gui.Tasks
{
    [ImplementPropertyChanged]
    public class CustomTaskViewModel
    {
    }

    public partial class CustomTaskView : UserControl, ConquerTaskFactory
    {
        public CustomTaskViewModel Model { get; set; } = new CustomTaskViewModel();

        public CustomTaskView()
        {
            InitializeComponent();
        }

        public ConquerTask CreateTask(ConquerProcess process)
        {
            return new CustomTask(process);
        }
    }
}
