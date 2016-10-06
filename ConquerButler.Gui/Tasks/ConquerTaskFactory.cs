namespace ConquerButler.Gui.Tasks
{
    public interface ConquerTaskFactory
    {
        ConquerTask CreateTask(ConquerProcess process);
    }
}
