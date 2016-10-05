using ConquerButler.Tasks;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ConquerButler.Gui
{
    [ImplementPropertyChanged]
    public class TaskEditWindowModel
    {
        public ConquerProcessModel Process { get; set; }
    }

    public partial class TaskEditWindow : Window
    {
        public TaskEditWindowModel Model { get; set; } = new TaskEditWindowModel();

        public TaskEditWindow()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var task = new MiningTask(Model.Process.ConquerProcess);
            task.Add();

            var task2 = new HealthWatcherTask(Model.Process.ConquerProcess);
            task2.Add();

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
