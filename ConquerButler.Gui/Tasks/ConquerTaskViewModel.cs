using PropertyChanged;
using System.ComponentModel;

namespace ConquerButler.Gui.Tasks
{
    [AddINotifyPropertyChangedInterface]
    public class ConquerTaskViewModel
    {
        public int Interval { get; set; } = 10;
        public int Priority { get; set; } = ConquerTask.DEFAULT_PRIORITY;
        public bool NeedsUserFocus { get; set; } = false;
        public bool NeedsToBeConnected { get; set; } = true;
        public string TaskType { get; set; }
    }
}
