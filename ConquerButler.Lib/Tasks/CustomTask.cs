using log4net;
using System.Threading.Tasks;

namespace ConquerButler.Tasks
{
    public class CustomTask : ConquerTask
    {
        private static ILog log = LogManager.GetLogger(typeof(CustomTask));

        public static string TASK_TYPE_NAME = "Custom";

        public CustomTask(ConquerProcess process)
            : base(TASK_TYPE_NAME, process)
        {
        }

        public override Task DoTick()
        {
            return Task.FromResult(true);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
