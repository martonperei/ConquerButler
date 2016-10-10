namespace ConquerButler.Gui.Tasks
{
    public interface ConquerTaskViewBase<out T>
    {
        T Model { get; }

        ConquerTask CreateTask(ConquerProcess process);
    }
}
