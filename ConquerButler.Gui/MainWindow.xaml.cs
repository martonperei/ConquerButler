using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ConquerButler.Tasks;
using Point = System.Drawing.Point;
using PropertyChanged;

namespace ConquerButler.Gui
{
    [ImplementPropertyChanged]
    public class ConquerProcess
    {
        public Process Process { get; set; }
        public Bitmap Screenshot { get; set; }

        public BitmapSource Name
        {
            get
            {
                using (var cropped = NativeMethods.CropBitmap(Screenshot, new Rectangle(108, 130, 100, 11)))
                {
                    return cropped.ToBitmapSource();
                }
            }
        }

        public BitmapSource Thumbnail
        {
            get
            {
                return Screenshot.ToBitmapSource();
            }
        }

        public Point MousePosition { get; set; }
        public ObservableCollection<ConquerTask> Tasks { get; } = new ObservableCollection<ConquerTask>();

        public void CancelTasks()
        {
            foreach (ConquerTask task in Tasks)
            {
                task.Cancel();
            }

            Application.Current.Dispatcher.Invoke(() => { Tasks.Clear(); });
        }
    }

    [ImplementPropertyChanged]
    public class ConquerButlerModel
    {
        public ObservableCollection<ConquerProcess> Processes { get; } = new ObservableCollection<ConquerProcess>();

        public ConquerProcess SelectedProcess { get; set; }
    }

    public partial class MainWindow : Window
    {
        public ConquerButlerModel Model { get; } = new ConquerButlerModel();

        private readonly DispatcherTimer refreshProcesses = new DispatcherTimer();
        private readonly DispatcherTimer refreshMouse = new DispatcherTimer();

        private readonly ConquerInputScheduler scheduler = new ConquerInputScheduler();
        private GlobalHotkey globalHotkey;

        public MainWindow()
        {
            InitializeComponent();

            refreshProcesses.Tick += (s, o) => RefreshProcesses();
            refreshProcesses.Interval = new TimeSpan(0, 0, 5);

            refreshMouse.Tick += (s, o) => RefreshMouseOnTick();
            refreshMouse.Interval = new TimeSpan(0, 0, 0, 0, 100);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            globalHotkey = new GlobalHotkey(this);
            globalHotkey.HotkeyPressed += GlobalHotkeyOnHotkeyPressed;

            scheduler.Start();

            refreshMouse.Start();
            refreshProcesses.Start();

            RefreshProcesses();
        }

        private void GlobalHotkeyOnHotkeyPressed(object sender, EventArgs eventArgs)
        {
            foreach (ConquerProcess process in Model.Processes)
            {
                process.CancelTasks();
            }
        }

        private void RefreshMouseOnTick()
        {
            foreach (ConquerProcess process in Model.Processes)
            {
                if (NativeMethods.IsForegroundWindow(process.Process))
                {
                    process.MousePosition = NativeMethods.GetCursorPosition(process.Process);
                }
                else
                {
                    process.MousePosition = new Point(0, 0);
                }
            }
        }

        private void RefreshProcesses()
        {
            foreach (Process process in NativeMethods.GetProcesses("Zonquer"))
            {
                ConquerProcess conquerProcess = Model.Processes.SingleOrDefault(c => c.Process.Id == process.Id);

                if (conquerProcess == null)
                {
                    process.EnableRaisingEvents = true;
                    process.Exited += ConquerProcessOnExited;

                    conquerProcess = new ConquerProcess()
                    {
                        Process = process
                    };

                    Model.Processes.Add(conquerProcess);
                }

                var bitmap = NativeMethods.PrintWindow(process);
                conquerProcess.Screenshot = bitmap;
            }
        }

        private void ConquerProcessOnExited(object sender, EventArgs eventArgs)
        {
            Process process = (Process)sender;

            ConquerProcess conquerProcess = Model.Processes.Single(c => c.Process.Id == process.Id);

            conquerProcess.CancelTasks();

            Application.Current.Dispatcher.Invoke(() => { Model.Processes.Remove(conquerProcess); });
        }

        private void ShowWindow_OnClick(object sender, RoutedEventArgs e)
        {
            ConquerProcess process = (sender as Button).DataContext as ConquerProcess;
            NativeMethods.SetForegroundWindow(process.Process);

            RefreshProcesses();
        }

        private void StartTasks_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (ConquerProcess process in Model.Processes)
            {
                if (process.Tasks.Count == 0)
                {
                    var task2 = new HuntTask(scheduler, process.Process);
                    process.Tasks.Add(task2);

                    scheduler.Add(task2);
                }
            }
        }

        private void StopTasks_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (ConquerProcess process in Model.Processes)
            {
                process.CancelTasks();
            }
        }

        private void GrabArea_OnClick(object sender, RoutedEventArgs e)
        {
            if (Model.SelectedProcess == null)
            {
                return;
            }

            GrabWindow grabWindow = new GrabWindow();
            grabWindow.Model.Process = Model.SelectedProcess;
            grabWindow.Show();
        }

        private void processList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Model.SelectedProcess != null)
            {
                NativeMethods.SetForegroundWindow(Model.SelectedProcess.Process);
            }
        }
    }
}
