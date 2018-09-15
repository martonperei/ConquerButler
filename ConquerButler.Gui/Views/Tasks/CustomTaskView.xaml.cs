using ConquerButler.Tasks;
//using ICSharpCode.AvalonEdit.Document;
using System;
using System.IO;
using System.Windows.Controls;

namespace ConquerButler.Gui.Views.Tasks
{
    public class CustomTaskViewModel : ConquerTaskViewModel
    {
        //public TextDocument Document { get; } = new TextDocument();
    }

    public partial class CustomTaskView : UserControl, ConquerTaskViewBase<CustomTaskViewModel>
    {
        public CustomTaskViewModel Model { get; set; } = new CustomTaskViewModel()
        {
            TaskType = CustomTask.TASK_TYPE_NAME
        };

        private const string EXAMPLE_CODE =
@"using System;

int runs = 0;

DoTick = async () => {
    await Task.RequestInputFocus(() =>
    {
        Process.Simulator.Mouse.RightButtonClick();
        Scheduler.Wait(1250);
    }, 1);

    runs++;
};

ResultDisplayInfo = () => runs.ToString();";

        public CustomTaskView()
        {
            InitializeComponent();

            //Model.Document.Text = EXAMPLE_CODE;
        }

        public ConquerTask CreateTask(ConquerProcess process)
        {
            return new CustomTask(process)
            {

                Interval = Model.Interval,
                Priority = Model.Priority,
                NeedsUserFocus = Model.NeedsUserFocus,
                NeedsToBeConnected = Model.NeedsToBeConnected,
                TaskType = Model.TaskType
            };
        }

        private void LoadSnippet_OnClick(object sender, System.Windows.RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "snippets"),
                FileName = "script",
                DefaultExt = ".csr",
                Filter = "CSharpScript (.csr)|*.csr"
            };

            bool? result = dlg.ShowDialog();

            if (result.HasValue && result.Value)
            {
                //Model.Document.Text = File.ReadAllText(dlg.FileName);
            }
        }
    }
}
