using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConquerButler
{
    public class ConquerInputAction
    {
        public ConquerTask Task { get; set; }
        public Func<Task> Action { get; set; }
        public long Priority { get; set; }
        public TaskCompletionSource<bool> ActionCompletion { get; } = new TaskCompletionSource<bool>();

        public async Task Execute()
        {
            try
            {
                await Action();

                ActionCompletion.SetResult(true);
            }
            catch (Exception e)
            {
                ActionCompletion.SetException(e);
            }
        }

        public void Cancel()
        {
            ActionCompletion.SetResult(true);
        }
    }
}
