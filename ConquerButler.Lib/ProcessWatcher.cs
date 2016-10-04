using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ConquerButler
{
    public class ProcessWatcher : IDisposable
    {
        public bool IsRunning { get; protected set; }

        public event Action<Process> ProcessStarted;
        public event Action<Process> ProcessEnded;

        public HashSet<int> Processes;

        public string ProcessName { get; protected set; }
        public Task WatcherTask;
        public CancellationTokenSource CancellationSource;

        public int CheckInterval { get; set; } = 10000;

        public ProcessWatcher(string name)
        {
            ProcessName = name;
            Processes = new HashSet<int>();
        }

        public void Start()
        {
            CancellationSource = new CancellationTokenSource();
            WatcherTask = Task.Factory.StartNew(Main, CancellationSource.Token);
        }

        public void Stop()
        {
            CancellationSource.Cancel();
        }

        public void Main()
        {
            IsRunning = true;

            while (!CancellationSource.IsCancellationRequested)
            {
                foreach (Process process in Helpers.GetProcesses(ProcessName))
                {
                    if (!Processes.Contains(process.Id))
                    {
                        process.EnableRaisingEvents = true;
                        process.Exited += Process_Exited;

                        Processes.Add(process.Id);

                        ProcessStarted?.Invoke(process);
                    }
                }

                Thread.Sleep(CheckInterval);
            }

            IsRunning = false;
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            Process process = (Process)sender;

            Processes.Remove(process.Id);

            ProcessEnded?.Invoke(process);
        }

        public void Dispose()
        {
            CancellationSource.Cancel();
        }
    }
}
