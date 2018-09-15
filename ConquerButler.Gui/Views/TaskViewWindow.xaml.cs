using ConquerButler.Tasks;
using PropertyChanged;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System;
using ConquerButler.Gui.Views.Tasks;

namespace ConquerButler.Gui.Views
{
    public delegate ConquerTask ConquerTaskFactoryFunc(ConquerProcess process);

    [AddINotifyPropertyChangedInterface]
    public class TaskTypeModel
    {
        public string TaskType { get; set; }

        public bool IsSelected { get; set; }

        public UserControl Content
        {
            get
            {
                return Factory as UserControl;
            }
        }

        public ConquerTaskViewBase<ConquerTaskViewModel> Factory { get; set; }
    }

    [AddINotifyPropertyChangedInterface]
    public class TaskViewWindowModel
    {
        public List<ConquerProcessModel> Processes { get; set; }

        public List<TaskTypeModel> TaskTypes { get; set; } = new List<TaskTypeModel>();

        public TaskTypeModel SelectedTaskType { get; set; }

        public ConquerTaskViewModel TaskViewModel
        {
            get
            {
                return SelectedTaskType?.Factory?.Model;
            }
        }
    }

    public class DefaultTaskFactory<T> : ConquerTaskViewBase<CustomTaskViewModel>
        where T: ConquerTask
    {
        public CustomTaskViewModel Model { get; } = new CustomTaskViewModel();

        public DefaultTaskFactory()
        {

        }

        public ConquerTask CreateTask(ConquerProcess process)
        {
            T task = (T)Activator.CreateInstance(typeof(T), process);

            task.Interval = Model.Interval;
            task.Priority = Model.Priority;
            task.NeedsUserFocus = Model.NeedsUserFocus;
            task.NeedsToBeConnected = Model.NeedsToBeConnected;

            if (Model.TaskType != null)
            {
                task.TaskType = Model.TaskType;
            }

            return task;
        }
    }

    public partial class TaskViewWindow : Window
    {
        public TaskViewWindowModel Model { get; set; } = new TaskViewWindowModel();

        public TaskViewWindow()
        {
            InitializeComponent();

            Model.TaskTypes.Add(new TaskTypeModel() { TaskType = MiningTask.TASK_TYPE_NAME, Factory = new MiningTaskView() });
            Model.TaskTypes.Add(new TaskTypeModel() { TaskType = HuntingTask.TASK_TYPE_NAME, Factory = new HuntingTaskView() });
            Model.TaskTypes.Add(new TaskTypeModel() { TaskType = StatsWatcherTask.TASK_TYPE_NAME, Factory = new StatsWatcherTaskView() });
            Model.TaskTypes.Add(new TaskTypeModel() { TaskType = XPSkillTask.TASK_TYPE_NAME, Factory = new XPSkillTaskView() });
            Model.TaskTypes.Add(new TaskTypeModel() { TaskType = ClickTask.TASK_TYPE_NAME, Factory = new ClickTaskView() });
            //Model.TaskTypes.Add(new TaskTypeModel() { TaskType = CustomTask.TASK_TYPE_NAME, Content = new CustomTaskView() });

            Model.TaskTypes.Add(new TaskTypeModel() { TaskType = ItemFindPauseTask.TASK_TYPE_NAME, Factory = new DefaultTaskFactory<ItemFindPauseTask>() });
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
                    ConquerTask task = taskType.Factory.CreateTask(process.ConquerProcess);
                    process.ConquerProcess.Scheduler.AddTask(task);
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