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

        public readonly HashSet<int> Processes;

        public string ProcessName { get; protected set; }

        private Task _watcherTask;
        private CancellationTokenSource _cancellationSource;

        public int CheckInterval { get; set; } = 10000;

        public ProcessWatcher(string name)
        {
            ProcessName = name;
            Processes = new HashSet<int>();
        }

        public void Start()
        {
            _cancellationSource = new CancellationTokenSource();
            _watcherTask = Task.Factory.StartNew(Main, _cancellationSource.Token);
        }

        public void Stop()
        {
            _cancellationSource.Cancel();
        }

        public void Main()
        {
            IsRunning = true;

            while (!_cancellationSource.IsCancellationRequested)
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
            _cancellationSource.Cancel();
        }
    }
}
