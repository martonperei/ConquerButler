using ConquerButler.Tasks;
using PropertyChanged;
using System.Collections.Generic;
using System.Windows.Controls;

namespace ConquerButler.Gui.Views.Tasks
{
    public class XPSkillTaskViewModel : ConquerTaskViewModel
    {
        public string XPSkillName { get; set; }
    }

    public partial class XPSkillTaskView : UserControl, ConquerTaskViewBase<XPSkillTaskViewModel>
    {
        public XPSkillTaskViewModel Model { get; set; } = new XPSkillTaskViewModel()
        {
            XPSkillName = "roar",
            Interval = 2,
            TaskType = XPSkillTask.TASK_TYPE_NAME
        };

        public XPSkillTaskView()
        {
            InitializeComponent();

            SkillNameChooser.ItemsSource = new List<string>()
            {
                "roar", "fishy", "fly", "superman"
            };
        }

        public ConquerTask CreateTask(ConquerProcess process)
        {
            var task = new XPSkillTask(process);

            task.Interval = Model.Interval;
            task.Priority = Model.Priority;
            task.NeedsUserFocus = Model.NeedsUserFocus;
            task.NeedsToBeConnected = Model.NeedsToBeConnected;
            task.TaskType = Model.TaskType;

            task.XPSkillName = Model.XPSkillName;

            return task;
        }
    }
}
