﻿using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ConquerButler.Tasks;
using PropertyChanged;
using System.Windows.Threading;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Collections.Specialized;
using System.Linq;
using System.ComponentModel;
using log4net;
using System.Reactive.Linq;

namespace ConquerButler.Gui
{
    [ImplementPropertyChanged]
    public class ConquerTaskModel : INotifyPropertyChanged
    {
        public ConquerTask ConquerTask { get; set; }

        public string DisplayInfo
        {
            get
            {
                return ConquerTask.DisplayInfo;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ConquerTaskModel(ConquerTask task)
        {
            ConquerTask = task;

            Observable.FromEventPattern<PropertyChangedEventArgs>(ConquerTask, "PropertyChanged")
                .Where((e) => e.EventArgs.PropertyName.Equals("DisplayInfo"))
                .Buffer(TimeSpan.FromSeconds(0.1))
                .Subscribe(e =>
                {
                    if (e.Count > 0)
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(e.First().EventArgs.PropertyName));
                    }
                });
        }
    }

    [ImplementPropertyChanged]
    public class ConquerProcessModel : INotifyPropertyChanged
    {
        public ConquerProcess ConquerProcess { get; set; }

        public ObservableCollection<ConquerTaskModel> Tasks { get; set; } = new ObservableCollection<ConquerTaskModel>();

        public System.Drawing.Point MousePosition
        {
            get
            {
                return ConquerProcess.MousePosition;
            }
        }

        public Bitmap Screenshot { get; set; }

        public BitmapSource Name
        {
            get
            {
                if (Screenshot != null)
                {
                    using (var cropped = Helpers.CropBitmap(Screenshot, new Rectangle(108, 130, 100, 11)))
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

            Observable.FromEventPattern<PropertyChangedEventArgs>(ConquerProcess, "PropertyChanged")
                .Where((e) => e.EventArgs.PropertyName.Equals("MousePosition"))
                .Buffer(TimeSpan.FromSeconds(0.1))
                .Subscribe(e =>
                {
                    if (e.Count > 0)
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(e.First().EventArgs.PropertyName));
                    }
                });

            process.Tasks.CollectionChanged += Tasks_CollectionChanged;

            Refresh();
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

            Screenshot = Helpers.PrintWindow(ConquerProcess.InternalProcess);
        }
    }

    [ImplementPropertyChanged]
    public class ConquerButlerModel
    {
        public ObservableCollection<ConquerProcessModel> Processes { get; set; } = new ObservableCollection<ConquerProcessModel>();

        public ConquerProcessModel SelectedProcess { get; set; }
    }

    public partial class MainWindow : Window
    {
        private static ILog log = LogManager.GetLogger(typeof(MainWindow));

        public ConquerButlerModel Model { get; } = new ConquerButlerModel();

        private readonly DispatcherTimer updateScreenshot = new DispatcherTimer();
        private ConquerInputScheduler scheduler;

        private GlobalHotkey globalHotkey;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            globalHotkey = new GlobalHotkey(this);
            globalHotkey.HotkeyPressed += GlobalHotkeyOnHotkeyPressed;

            scheduler = new ConquerInputScheduler();
            scheduler.Processes.CollectionChanged += Processes_CollectionChanged;
            scheduler.Start();

            updateScreenshot.Tick += (s, o) =>
            {
                foreach (ConquerProcessModel process in Model.Processes)
                {
                    process.Refresh();
                }
            };
            updateScreenshot.Interval = new TimeSpan(0, 0, 0, 10);
            updateScreenshot.Start();

            log.Info("Initialized Window");
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

        private void GlobalHotkeyOnHotkeyPressed(object sender, EventArgs eventArgs)
        {
            scheduler.CancelRunning();
        }

        private void StartTasks_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (ConquerProcessModel process in Model.Processes)
            {
                if (process.Tasks.Count == 0)
                {
                    var task2 = new MiningTask(scheduler, process.ConquerProcess);
                    scheduler.Add(task2);
                }
                else
                {
                    process.ConquerProcess.Resume();
                }
            }
        }

        private void StopTasks_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (ConquerProcessModel process in Model.Processes)
            {
                process.ConquerProcess.Pause();
            }
        }

        private void GrabArea_OnClick(object sender, RoutedEventArgs e)
        {
            if (Model.SelectedProcess == null)
            {
                return;
            }

            Model.SelectedProcess.Refresh();

            GrabWindow grabWindow = new GrabWindow();
            grabWindow.Model.ScreenshotCopy = Model.SelectedProcess.Screenshot.Clone() as Bitmap;
            grabWindow.Show();
        }

        private void processList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Model.SelectedProcess != null)
            {
                Helpers.SetForegroundWindow(Model.SelectedProcess.ConquerProcess.InternalProcess);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            scheduler.Dispose();
            scheduler = null;
        }
    }
}
