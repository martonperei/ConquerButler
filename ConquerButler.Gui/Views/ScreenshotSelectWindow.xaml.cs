using PropertyChanged;
using System;
using System.IO;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ConquerButler.Native;

namespace ConquerButler.Gui.Views
{
    [AddINotifyPropertyChangedInterface]
    public class ScreenshotSelectModel
    {
        public System.Drawing.Bitmap ScreenshotCopy { get; set; }
        public BitmapSource CanvasSource { get; set; }

        public Point MousePosition { get; set; }

        public string MousePositionInfo
        {
            get
            {
                return $"X {MousePosition.X,-5:F0} Y {MousePosition.Y,-5:F0}";
            }
        }

        public string SelectedRectangleInfo
        {
            get
            {
                return $"X {SelectedRectangle.X,-5:F0} Y {SelectedRectangle.Y,-5:F0} Width {SelectedRectangle.Width,-5:F0} Height {SelectedRectangle.Height,-5:F0}";
            }
        }

        public Rect SelectedRectangle { get; set; }
    }

    public partial class ScreenshotSelectWindow : Window
    {
        public ScreenshotSelectModel Model { get; } = new ScreenshotSelectModel();

        private Point drawingStartPoint;
        private Stroke drawingRectangle;

        private DispatcherTimer _mouseUpdate = new DispatcherTimer();

        public ScreenshotSelectWindow()
        {
            InitializeComponent();

            _mouseUpdate = new DispatcherTimer();
            _mouseUpdate.Interval = new TimeSpan(0, 0, 0, 0, 100);
            _mouseUpdate.Tick += (s, e) => Model.MousePosition = Mouse.GetPosition(ScreenshotCanvas);
            _mouseUpdate.Start();

            Closed += ScreenshotSelectWindow_Closed;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            Model.CanvasSource = Model.ScreenshotCopy.ToBitmapSource();
        }

        private void ScreenshotSelectWindow_Closed(object sender, EventArgs e)
        {
            Model.ScreenshotCopy?.Dispose();
        }

        private void Close_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SwitchMode_OnClick(object sender, RoutedEventArgs e)
        {
            if (ScreenshotCanvas.EditingMode.Equals(System.Windows.Controls.InkCanvasEditingMode.None))
            {
                EditingModeChooser.Content = "Select";

                ScreenshotCanvas.EditingMode = System.Windows.Controls.InkCanvasEditingMode.EraseByStroke;
            }
            else
            {
                EditingModeChooser.Content = "Erase";

                ScreenshotCanvas.EditingMode = System.Windows.Controls.InkCanvasEditingMode.None;
            }
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (Stroke stroke in ScreenshotCanvas.Strokes)
            {
                stroke.DrawingAttributes.Color = Colors.Red;

                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
                {
                    InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images"),
                    FileName = "template",
                    DefaultExt = ".png",
                    Filter = "Image (.png)|*.png"
                };

                bool? result = dlg.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    Rect rect = stroke.GetBounds();

                    using (System.Drawing.Bitmap bitmap = Helpers.CropBitmap(Model.ScreenshotCopy,
                        new System.Drawing.Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height)))
                    {

                        bitmap.Save(dlg.FileName);
                    }
                }

                stroke.DrawingAttributes.Color = Colors.Green;
            }
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            drawingStartPoint = e.GetPosition(ScreenshotCanvas);

            var collection = new StylusPointCollection(5);
            SetPoints(collection, drawingStartPoint.X, drawingStartPoint.Y, 1, 1);

            drawingRectangle = new Stroke(collection);
            drawingRectangle.DrawingAttributes.Color = Colors.Green;

            ScreenshotCanvas.Strokes.Add(drawingRectangle);
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released || drawingRectangle == null)
            {
                return;
            }

            var pos = e.GetPosition(ScreenshotCanvas);

            var x = Math.Min(pos.X, drawingStartPoint.X);
            var y = Math.Min(pos.Y, drawingStartPoint.Y);

            var w = Math.Max(pos.X, drawingStartPoint.X) - x;
            var h = Math.Max(pos.Y, drawingStartPoint.Y) - y;

            SetPoints(drawingRectangle.StylusPoints, x, y, w, h);
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (drawingRectangle != null)
            {
                Model.SelectedRectangle = drawingRectangle.GetBounds();

                drawingRectangle = null;
            }
        }

        private void SetPoints(StylusPointCollection collection, double x, double y, double w, double h)
        {
            StylusPoint a = new StylusPoint(x, y);
            StylusPoint b = new StylusPoint(x + w, y);
            StylusPoint c = new StylusPoint(x + w, y + h);
            StylusPoint d = new StylusPoint(x, y + h);

            if (collection.Count == 0)
            {
                collection.Add(a);
                collection.Add(b);
                collection.Add(c);
                collection.Add(d);
                collection.Add(a);
            }
            else
            {
                collection[0] = a;
                collection[1] = b;
                collection[2] = c;
                collection[3] = d;
                collection[4] = a;
            }
        }
    }
}
