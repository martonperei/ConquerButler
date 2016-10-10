using PropertyChanged;
using System.ComponentModel;

namespace ConquerButler.Gui.Tasks
{
    [ImplementPropertyChanged]
    public class ConquerTaskViewModel : INotifyPropertyChanged
    {
        public int Interval { get; set; } = 10;
        public int Priority { get; set; } = ConquerTask.DEFAULT_PRIORITY;
        public bool NeedsUserFocus { get; set; } = false;
        public bool NeedsToBeConnected { get; set; } = true;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
