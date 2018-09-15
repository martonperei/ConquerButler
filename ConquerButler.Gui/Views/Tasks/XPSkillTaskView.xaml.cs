using ConquerButler.Tasks;
using System.Collections.Generic;
using System.Windows.Controls;

namespace ConquerButler.Gui.Views.Tasks
{
    public class XPSkillTaskViewModel : ConquerTaskViewModel
    {
        public string XPSkillName { get; set; }
        public List<string> Skills { get; set; } = new List<string>();
    }

    public partial class XPSkillTaskView : UserControl, ConquerTaskViewBase<XPSkillTaskViewModel>
    {
        public XPSkillTaskViewModel Model { get; set; } = new XPSkillTaskViewModel()
        {
            XPSkillName = "fly",
            Skills = { "roar", "fishy", "fly", "superman" },
            Interval = 5000,
            TaskType = XPSkillTask.TASK_TYPE_NAME
        };

        public XPSkillTaskView()
        {
            InitializeComponent();
        }

        public ConquerTask CreateTask(ConquerProcess process)
        {
            return new XPSkillTask(process)
            {
                Interval = Model.Interval,
                Priority = Model.Priority,
                NeedsUserFocus = Model.NeedsUserFocus,
                NeedsToBeConnected = Model.NeedsToBeConnected,
                TaskType = Model.TaskType,
                XPSkillName = Model.XPSkillName
            };
        }
    }
}
