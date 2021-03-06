using ConquerButler.Gui.Input;
using ConquerButler.Native;
using log4net;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ConquerButler.Gui.Views
{
    public class ConquerTaskModel : INotifyPropertyChanged, IDisposable
    {
        public bool IsSelected { get; set; }

        public ConquerTask ConquerTask { get; private set; }

        public string StateDisplayInfo
        {
            get
            {
                return $"{ConquerTask.TaskType,-15} {(ConquerTask.Running ? ">>>" : "---")} {ConquerTask.NextRun / 1000f,7:F2}";
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

        private readonly IDisposable _updater;

        public ConquerTaskModel(ConquerTask task)
        {
            ConquerTask = task;

            _updater = Observable.Interval(TimeSpan.FromMilliseconds(100))
                .Subscribe(e =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ResultDisplayInfo"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("StateDisplayInfo"));
                });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ConquerTaskModel()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _updater.Dispose();
            }
        }
    }

    [AddINotifyPropertyChangedInterface]
    public class ConquerProcessModel : IDisposable
    {
        public bool IsSelected { get; set; }

        public bool Disconnected { get { return ConquerProcess.Disconnected; } }

        public ConquerProcess ConquerProcess { get; private set; }

        public ObservableCollection<ConquerTaskModel> Tasks { get; } = new ObservableCollection<ConquerTaskModel>();

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

        public ConquerProcessModel(ConquerProcess process)
        {
            ConquerProcess = process;

            Refresh();
        }

        public void Refresh()
        {
            Screenshot?.Dispose();

            Screenshot = ConquerProcess.Screenshot();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ConquerProcessModel()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Screenshot != null)
                {
                    Screenshot.Dispose();
                }
            }
        }
    }

    [AddINotifyPropertyChangedInterface]
    public class ConquerButlerModel
    {
        public ObservableCollection<ConquerProcessModel> Processes { get; set; } = new ObservableCollection<ConquerProcessModel>();
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
            globalHotkey.HotkeyPressed += (s, e2) =>
            {
                if (scheduler.Running)
                {
                    scheduler.Stop();
                } else
                {
                    scheduler.Start();
                }
            };

            scheduler = new ConquerScheduler();
            scheduler.ProcessAdded += Scheduler_ProcessAdded;
            scheduler.ProcessRemoved += Scheduler_ProcessRemoved;

            scheduler.TaskAdded += Scheduler_TaskAdded;
            scheduler.TaskRemoved += Scheduler_TaskRemoved;

            scheduler.Start();

            updateScreenshot.Tick += (s, o) =>
            {
                foreach (ConquerProcessModel process in Model.Processes)
                {
                    process.Refresh();
                }
            };
            updateScreenshot.Interval = new TimeSpan(0, 0, 0, 5);
            updateScreenshot.Start();

            log.Info("Initialized Window");
        }

        private void Scheduler_TaskRemoved(ConquerTask task)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ConquerProcessModel model = Model.Processes.Where(p => p.ConquerProcess.Equals(task.Process)).Single();
                model.Tasks.Remove(model.Tasks.Single(m => m.ConquerTask == task));
            });
        }

        private void Scheduler_TaskAdded(ConquerTask task)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ConquerProcessModel model = Model.Processes.Where(p => p.ConquerProcess.Equals(task.Process)).Single();
                model.Tasks.Add(new ConquerTaskModel(task));
            });
        }

        private void Scheduler_ProcessRemoved(ConquerProcess process)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Model.Processes.Remove(Model.Processes.Single(m => m.ConquerProcess == process));
            });
        }

        private void Scheduler_ProcessAdded(ConquerProcess process)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Model.Processes.Add(new ConquerProcessModel(process));
            });
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            updateScreenshot.Stop();

            scheduler.Dispose();
            scheduler = null;
        }

        private void StartTasks_OnClick(object sender, RoutedEventArgs e)
        {
            IEnumerable<ConquerProcessModel> processes = Model.Processes.Where(p => p.IsSelected);

            foreach (ConquerProcessModel process in processes)
            {
                foreach (ConquerTaskModel task in process.Tasks)
                {
                    task.ConquerTask.Start();
                }
            }
        }

        private void StopTasks_OnClick(object sender, RoutedEventArgs e)
        {
            IEnumerable<ConquerProcessModel> processes = Model.Processes.Where(p => p.IsSelected);

            foreach (ConquerProcessModel process in processes)
            {
                foreach (ConquerTaskModel task in process.Tasks)
                {
                    task.ConquerTask.Stop();
                }
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
            List<ConquerTaskModel> tasks = Model.Processes.Where(p => p.IsSelected).SelectMany(p => p.Tasks).Where(t => t.IsSelected).ToList();

            if (tasks.Count > 0)
            {
                foreach (ConquerTaskModel task in tasks.ToList())
                {
                    scheduler.RemoveTask(task.ConquerTask);
                }
            }
            else
            {
                IEnumerable<ConquerProcessModel> processes = Model.Processes.Where(p => p.IsSelected);

                foreach (ConquerProcessModel process in processes)
                {
                    foreach (ConquerTaskModel task in process.Tasks.ToList())
                    {
                        scheduler.RemoveTask(task.ConquerTask);
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
