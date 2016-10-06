using ConquerButler.Gui.Tasks;
using ConquerButler.Tasks;
using PropertyChanged;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ConquerButler.Gui
{
    public delegate ConquerTask ConquerTaskFactoryFunc(ConquerProcess process);

    [ImplementPropertyChanged]
    public class TaskTypeModel
    {
        public string TaskType { get; set; }

        public bool IsSelected { get; set; }

        public UserControl Content { get; set; }

        private ConquerTaskFactoryFunc _factory;

        public ConquerTaskFactoryFunc Factory
        {
            get
            {
                return _factory ?? (Content as ConquerTaskFactory).CreateTask;
            }
            set
            {
                _factory = value;
            }
        }
    }

    [ImplementPropertyChanged]
    public class TaskViewWindowModel
    {
        public List<ConquerProcessModel> Processes { get; set; }

        public List<TaskTypeModel> TaskTypes { get; set; } = new List<TaskTypeModel>();

        public TaskTypeModel SelectedTaskType { get; set; }
    }

    public partial class TaskViewWindow : Window
    {
        public TaskViewWindowModel Model { get; set; } = new TaskViewWindowModel();

        public TaskViewWindow()
        {
            InitializeComponent();

            Model.TaskTypes.Add(new TaskTypeModel() { TaskType = MiningTask.TASK_TYPE_NAME, Content = new MiningTaskView() });
            Model.TaskTypes.Add(new TaskTypeModel() { TaskType = HuntingTask.TASK_TYPE_NAME, Content = new HuntingTaskView() });
            Model.TaskTypes.Add(new TaskTypeModel() { TaskType = HealthWatcherTask.TASK_TYPE_NAME, Content = new HealthWatcherTaskView() });
            Model.TaskTypes.Add(new TaskTypeModel() { TaskType = CustomTask.TASK_TYPE_NAME, Content = new CustomTaskView() });

            Model.TaskTypes.Add(new TaskTypeModel() { TaskType = FlyTask.TASK_TYPE_NAME, Factory = p => new FlyTask(p) });
            Model.TaskTypes.Add(new TaskTypeModel() { TaskType = ItemFindPauseTask.TASK_TYPE_NAME, Factory = p => new ItemFindPauseTask(p) });
        }

        private void TaskTypeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            List<TaskTypeModel> selectedTasks = Model.TaskTypes.Where(t => t.IsSelected).ToList();

            foreach (TaskTypeModel taskType in selectedTasks)
            {
                foreach (ConquerProcessModel process in Model.Processes)
                {
                    ConquerTask task = taskType.Factory(process.ConquerProcess);
                    task.Add();
                }
            }

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}