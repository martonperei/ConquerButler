namespace ConquerButler.Gui.Views
{
    public interface ConquerTaskViewBase<out T>
    {
        T Model { get; }

        ConquerTask CreateTask(ConquerProcess process);
    }
}
