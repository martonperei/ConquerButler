using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
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
                return Imaging.CreateBitmapSourceFromHBitmap(
                    ScreenshotHelper.CropBitmap(Screenshot, new Rectangle(108, 130, 100, 11)).GetHbitmap(),
                    IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
        }

        public BitmapSource Thumbnail
        {
            get {
                return Imaging.CreateBitmapSourceFromHBitmap(Screenshot.GetHbitmap(),
                          IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
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
        public ObservableCollection<string> TaskTypes { get; } = new ObservableCollection<string>();

        public ConquerProcess SelectedProcess { get; set; }
        public BitmapSource InkCanvasSource { get; set; }
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

            refreshProcesses.Tick += RefreshProcessesOnTick;
            refreshProcesses.Interval = new TimeSpan(0, 0, 5);

            refreshMouse.Tick += RefreshMouseOnTick;
            refreshMouse.Interval = new TimeSpan(0, 0, 0, 0, 100);

            Model.TaskTypes.Add("mine");
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

        private void RefreshMouseOnTick(object sender, EventArgs eventArgs)
        {
            foreach (ConquerProcess process in Model.Processes) {
                if (ProcessHelper.IsForegroundWindow(process.Process))
                {
                    process.MousePosition = CursorHelper.GetCursorPosition(process.Process);
                } else
                {
                    process.MousePosition = new Point(0, 0);
                }
            }
        }

        private void RefreshProcessesOnTick(object sender, System.EventArgs e)
        {
            RefreshProcesses();
        }

        private void RefreshProcesses()
        {
            foreach (Process process in ProcessHelper.GetProcesses("Zonquer"))
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

                var bitmap = ScreenshotHelper.PrintWindow(process);
                conquerProcess.Screenshot = bitmap;
            }
        }

        private void ConquerProcessOnExited(object sender, EventArgs eventArgs)
        {
            Process process = (Process) sender;

            ConquerProcess conquerProcess = Model.Processes.Single(c => c.Process.Id == process.Id);

            conquerProcess.CancelTasks();

            Application.Current.Dispatcher.Invoke(() => { Model.Processes.Remove(conquerProcess); });
        }

        private void ShowWindow_OnClick(object sender, RoutedEventArgs e)
        {
            ConquerProcess process = (sender as Button).DataContext as ConquerProcess;
            ProcessHelper.SetForegroundWindow(process.Process);

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

            var bitmap = ScreenshotHelper.PrintWindow(Model.SelectedProcess.Process);
            Model.SelectedProcess.Screenshot = bitmap;

            Model.InkCanvasSource = Imaging.CreateBitmapSourceFromHBitmap(Model.SelectedProcess.Screenshot.GetHbitmap(),
                IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            
            GrabPopup.IsOpen = true;
        }

        private void CloseGrab_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (Stroke stroke in GrabCanvas.Strokes)
            {
                stroke.DrawingAttributes.Color = Colors.Red;

                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = "Template",
                    DefaultExt = ".png",
                    Filter = "Image (.png)|*.png"
                };

                bool? result = dlg.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    Rect rect = stroke.GetBounds();
                    Bitmap bitmap = ScreenshotHelper.CropBitmap(Model.SelectedProcess.Screenshot,
                        new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height));

                    bitmap.Save(dlg.FileName);
                }

                stroke.DrawingAttributes.Color = Colors.Transparent;
            }

            GrabCanvas.Strokes.Clear();
            GrabPopup.IsOpen = false;
        }

        private void ClearStrokes_OnClick(object sender, RoutedEventArgs e)
        {
            GrabCanvas.Strokes.Clear();
            Model.InkCanvasSource = Imaging.CreateBitmapSourceFromHBitmap(Model.SelectedProcess.Screenshot.GetHbitmap(),
                IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }

        private void ShowStrokes_OnClick(object sender, RoutedEventArgs e)
        {
            Bitmap bitmap = new Bitmap(Model.SelectedProcess.Screenshot.Width, Model.SelectedProcess.Screenshot.Height);

            foreach (Stroke stroke in GrabCanvas.Strokes)
            {
                Rect rect = stroke.GetBounds();

                Rectangle tr = new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);

                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.DrawImage(Model.SelectedProcess.Screenshot, new Rectangle(tr.X, tr.Y, tr.Width, tr.Height),
                                     tr, GraphicsUnit.Pixel);
                }

                stroke.DrawingAttributes.Color = Colors.Transparent;
            }

            Model.InkCanvasSource = Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(),
                      IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }

        private void processList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Model.SelectedProcess != null)
            {
                ProcessHelper.SetForegroundWindow(Model.SelectedProcess.Process);
            }
        }
    }
}
