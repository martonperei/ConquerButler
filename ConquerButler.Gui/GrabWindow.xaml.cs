using PropertyChanged;
using System;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ConquerButler.Gui
{
    [ImplementPropertyChanged]
    public class GrabWindowModel
    {
        public System.Drawing.Bitmap ScreenshotCopy { get; set; }
        public BitmapSource CanvasSource { get; set; }
    }

    public partial class GrabWindow : Window
    {
        public GrabWindowModel Model { get; } = new GrabWindowModel();

        private Point drawingStartPoint;
        private Stroke drawingRectangle;

        public GrabWindow()
        {
            InitializeComponent();

            Closed += GrabWindow_Closed;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            Model.CanvasSource = Model.ScreenshotCopy.ToBitmapSource();
        }

        private void GrabWindow_Closed(object sender, EventArgs e)
        {
            Model.ScreenshotCopy?.Dispose();
        }

        private void Close_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SwitchMode_OnClick(object sender, RoutedEventArgs e)
        {
            if (GrabCanvas.EditingMode.Equals(System.Windows.Controls.InkCanvasEditingMode.None))
            {
                EditingModeChooser.Content = "Select";

                GrabCanvas.EditingMode = System.Windows.Controls.InkCanvasEditingMode.EraseByStroke;
            }
            else
            {
                EditingModeChooser.Content = "Erase";

                GrabCanvas.EditingMode = System.Windows.Controls.InkCanvasEditingMode.None;
            }
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
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
            drawingStartPoint = e.GetPosition(GrabCanvas);

            var collection = new StylusPointCollection(5);
            SetPoints(collection, drawingStartPoint.X, drawingStartPoint.Y, 1, 1);

            drawingRectangle = new Stroke(collection);
            drawingRectangle.DrawingAttributes.Color = Colors.Green;

            GrabCanvas.Strokes.Add(drawingRectangle);
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released || drawingRectangle == null)
            {
                return;
            }

            var pos = e.GetPosition(GrabCanvas);

            var x = Math.Min(pos.X, drawingStartPoint.X);
            var y = Math.Min(pos.Y, drawingStartPoint.Y);

            var w = Math.Max(pos.X, drawingStartPoint.X) - x;
            var h = Math.Max(pos.Y, drawingStartPoint.Y) - y;

            SetPoints(drawingRectangle.StylusPoints, x, y, w, h);
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            drawingRectangle = null;
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

        private void SetPoints(Stroke stroke, double x, double y, double w, double h)
        {

        }
    }
}
