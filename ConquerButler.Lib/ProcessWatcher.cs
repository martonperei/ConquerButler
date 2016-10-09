using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ConquerButler
{
    public class ProcessWatcher : IDisposable
    {
        private const string DIALOG_CLASS_NAME = "#32770";
        private const string DISCONNECTED_TEXT = "Error: Disconnected with game server. Please login the game again!";

        public bool IsRunning { get; protected set; }

        public event Action<Process, bool> ProcessStarted;
        public event Action<Process> ProcessEnded;

        public event Action<Process, bool> ProcessStateChanged;

        public readonly HashSet<int> Processes;
        public readonly HashSet<int> DisconnectedProcesses;

        public string ProcessName { get; protected set; }

        private Task _watcherTask;
        private CancellationTokenSource _cancellationSource;

        public int CheckInterval { get; set; } = 10000;

        public ProcessWatcher(string name)
        {
            ProcessName = name;
            Processes = new HashSet<int>();
            DisconnectedProcesses = new HashSet<int>();
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

        private bool IsDisconnected(Process process)
        {
            bool isDisconnected = false;

            if (DIALOG_CLASS_NAME.Equals(Helpers.GetClassName(process.MainWindowHandle)))
            {
                isDisconnected = DISCONNECTED_TEXT.Equals(Helpers.GetDialogText(process.MainWindowHandle, 0xFFFF));
            }
            else
            {
                List<IntPtr> childHandles = Helpers.GetAllChildHandles(process);

                foreach (IntPtr handle in childHandles)
                {
                    if (DIALOG_CLASS_NAME.Equals(Helpers.GetClassName(handle)))
                    {
                        string text = Helpers.GetDialogText(handle, 0xFFFF);

                        if (text != null)
                        {
                            isDisconnected = DISCONNECTED_TEXT.Equals(text);
                        }
                    }
                }
            }

            return isDisconnected;
        }

        public void Main()
        {
            IsRunning = true;

            while (!_cancellationSource.IsCancellationRequested)
            {
                foreach (Process process in Helpers.GetProcesses(ProcessName))
                {
                    bool isDisconnected = IsDisconnected(process);

                    if (!Processes.Contains(process.Id))
                    {
                        process.EnableRaisingEvents = true;
                        process.Exited += Process_Exited;

                        Processes.Add(process.Id);

                        ProcessStarted?.Invoke(process, isDisconnected);
                    }
                    else
                    {
                        if (isDisconnected && !DisconnectedProcesses.Contains(process.Id))
                        {
                            DisconnectedProcesses.Add(process.Id);

                            ProcessStateChanged?.Invoke(process, isDisconnected);
                        }
                        else if (!isDisconnected && DisconnectedProcesses.Contains(process.Id))
                        {
                            DisconnectedProcesses.Remove(process.Id);

                            ProcessStateChanged?.Invoke(process, isDisconnected);
                        }
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
