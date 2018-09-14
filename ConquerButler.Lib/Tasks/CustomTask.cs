using log4net;
using System.Threading.Tasks;

namespace ConquerButler.Tasks
{
    public class CustomTask : ConquerTask
    {
        private static ILog log = LogManager.GetLogger(typeof(CustomTask));

        public static string TASK_TYPE_NAME = "Custom";

        public override string ResultDisplayInfo { get; protected set; }

        public CustomTask(ConquerProcess process)
            : base(TASK_TYPE_NAME, process)
        {
        }

        protected async override Task DoTick()
        {
            await Task.FromResult(true);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
