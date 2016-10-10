using System.Drawing;
using log4net;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows;

namespace ConquerButler.Gui
{
    [ImplementPropertyChanged]
    public class ConquerTaskModel : INotifyPropertyChanged
    {
        public bool IsSelected { get; set; }

        public ConquerTask ConquerTask { get; set; }

        public string StateDisplayInfo
        {
            get
            {
                return $"{ConquerTask.TaskType,-15} {(ConquerTask.IsRunning ? ">>>" : "---")} {ConquerTask.NextRun,7:F2}";
            }
        }

        public string ResultDisplayInfo
        {
            get
            {
                return ConquerTask.ResultDisplayInfo;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ConquerTaskModel(ConquerTask task)
        {
            ConquerTask = task;

            string[] events = new string[] { "IsRunning", "NextRun", "ResultDisplayInfo" };

            Observable.FromEventPattern<PropertyChangedEventArgs>(ConquerTask, "PropertyChanged")
                .Where((e) => events.Contains(e.EventArgs.PropertyName))
                .Sample(TimeSpan.FromSeconds(0.1))
                .Subscribe(e =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("StateDisplayInfo"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ResultDisplayInfo"));
                });
        }
    }

    [ImplementPropertyChanged]
    public class ConquerProcessModel : INotifyPropertyChanged
    {
        public bool IsSelected { get; set; }

        public bool Disconnected { get; set; }

        public ConquerProcess ConquerProcess { get; set; }

        public ObservableCollection<ConquerTaskModel> Tasks { get; set; } = new ObservableCollection<ConquerTaskModel>();

        public Bitmap Screenshot { get; set; }

        private static Rectangle NAME_RECT = new Rectangle(108, 130, 100, 11);

        public BitmapSource Name
        {
            get
            {
                if (Screenshot != null)
                {
                    using (var cropped = Helpers.CropBitmap(Screenshot, NAME_RECT))
                    {
                        return cropped.ToBitmapSource();
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        public BitmapSource Thumbnail
        {
            get
            {
                return Screenshot?.ToBitmapSource();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ConquerProcessModel(ConquerProcess process)
        {
            ConquerProcess = process;

            process.Tasks.CollectionChanged += Tasks_CollectionChanged;

            Disconnected = process.Disconnected;

            process.ProcessStateChange += Process_ProcessStateChange;

            Refresh();
        }

        private void Process_ProcessStateChange(bool isDisconnected)
        {
            Application.Current.Dispatcher.Invoke(() => Disconnected = isDisconnected);
        }

        private void Tasks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (ConquerTask task in e.NewItems)
                        {
                            Tasks.Add(new ConquerTaskModel(task));
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        foreach (ConquerTask task in e.OldItems)
                        {
                            Tasks.Remove(Tasks.First(m => m.ConquerTask == task));
                        }
                        break;
                }
            });
        }

        public void Refresh()
        {
            Screenshot?.Dispose();

            Screenshot = ConquerProcess.Screenshot();
        }
    }

    [ImplementPropertyChanged]
    public class ConquerButlerModel
    {
        public ObservableCollection<ConquerProcessModel> Processes { get; set; } = new ObservableCollection<ConquerProcessModel>();

        public List<ConquerProcessModel> SelectedProcesses { get; set; }
    }

    public partial class MainWindow : Window
    {
        private static ILog log = LogManager.GetLogger(typeof(MainWindow));

        public ConquerButlerModel Model { get; } = new ConquerButlerModel();

        private readonly DispatcherTimer updateScreenshot = new DispatcherTimer();
        private ConquerScheduler scheduler;

        private GlobalHotkey globalHotkey;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            globalHotkey = new GlobalHotkey(this);
            globalHotkey.HotkeyPressed += (s, e2) => scheduler.CancelRunning();

            scheduler = new ConquerScheduler();
            scheduler.Processes.CollectionChanged += Processes_CollectionChanged;
            scheduler.Start();

            updateScreenshot.Tick += (s, o) =>
            {
                foreach (ConquerProcessModel process in Model.Processes)
                {
                    process.Refresh();
                }
            };
            updateScreenshot.Interval = new TimeSpan(0, 0, 0, 30);
            updateScreenshot.Start();

            log.Info("Initialized Window");
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            scheduler.Dispose();
            scheduler = null;
        }

        private void Processes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (ConquerProcess process in e.NewItems)
                        {
                            Model.Processes.Add(new ConquerProcessModel(process));
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        foreach (ConquerProcess process in e.OldItems)
                        {
                            Model.Processes.Remove(Model.Processes.First(m => m.ConquerProcess == process));
                        }
                        break;
                }
            });
        }

        private void ForceRunTasks_OnClick(object sender, RoutedEventArgs e)
        {
            IEnumerable<ConquerProcessModel> processes = Model.Processes.Where(p => p.IsSelected);

            foreach (ConquerProcessModel process in processes)
            {
                foreach (ConquerTaskModel task in process.Tasks)
                {
                    task.ConquerTask.ForceRun();
                }
            }
        }

        private void ResumeTasks_OnClick(object sender, RoutedEventArgs e)
        {
            IEnumerable<ConquerProcessModel> processes = Model.Processes.Where(p => p.IsSelected);

            foreach (ConquerProcessModel process in processes)
            {
                process.ConquerProcess.Resume();
            }
        }

        private void PauseTasks_OnClick(object sender, RoutedEventArgs e)
        {
            IEnumerable<ConquerProcessModel> processes = Model.Processes.Where(p => p.IsSelected);

            foreach (ConquerProcessModel process in processes)
            {
                process.ConquerProcess.Pause();
            }
        }

        private void AddTasks_Click(object sender, RoutedEventArgs e)
        {
            List<ConquerProcessModel> processes = Model.Processes.Where(p => p.IsSelected).ToList();

            if (processes.Count > 0)
            {
                Button addTaskButton = (sender as Button);

                TaskViewWindow window = new TaskViewWindow();
                window.Owner = this;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                window.Model.Processes = processes;

                window.Show();
            }
        }

        private void RemoveTasks_Click(object sender, RoutedEventArgs e)
        {
            List<ConquerTaskModel> tasks = Model.Processes.SelectMany(p => p.Tasks).Where(t => t.IsSelected).ToList();

            if (tasks.Count > 0)
            {
                foreach (ConquerTaskModel task in tasks)
                {
                    task.ConquerTask.Remove();
                }
            }
            else
            {
                IEnumerable<ConquerProcessModel> processes = Model.Processes.Where(p => p.IsSelected);

                foreach (ConquerProcessModel process in processes)
                {
                    foreach (ConquerTaskModel task in process.Tasks)
                    {
                        task.ConquerTask.Remove();
                    }
                }
            }
        }

        private void ScreenshotSelect_OnClick(object sender, RoutedEventArgs e)
        {
            ConquerProcessModel process = Model.Processes.Where(p => p.IsSelected).FirstOrDefault();

            if (process != null)
            {
                process.Refresh();

                ScreenshotSelectWindow window = new ScreenshotSelectWindow();
                window.Owner = this;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                window.Model.ScreenshotCopy = process.Screenshot.Clone() as Bitmap;
                window.Show();
            }
        }

        private void ShowProcess_OnClick(object sender, RoutedEventArgs e)
        {
            ConquerProcessModel process = (sender as Button).DataContext as ConquerProcessModel;

            if (process != null)
            {
                Helpers.SetForegroundWindow(process.ConquerProcess.InternalProcess);
            }
        }
    }
}
