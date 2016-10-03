using PropertyChanged;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ConquerButler.Gui
{
    [ImplementPropertyChanged]
    public class GrabWindowModel
    {
        public ConquerProcess Process { get; set; }
        public BitmapSource InkCanvasSource { get; set; }
    }

    public partial class GrabWindow : Window
    {
        public GrabWindowModel Model { get; } = new GrabWindowModel();

        public GrabWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            using (var bitmap = NativeMethods.PrintWindow(Model.Process.Process))
            {
                Model.InkCanvasSource = bitmap.ToBitmapSource();
            }
        }

        private void SaveGrab_OnClick(object sender, RoutedEventArgs e)
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
                    Bitmap bitmap = NativeMethods.CropBitmap(Model.Process.Screenshot,
                        new System.Drawing.Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height));

                    bitmap.Save(dlg.FileName);
                }

                stroke.DrawingAttributes.Color = Colors.Transparent;
            }

            GrabCanvas.Strokes.Clear();

            Close();
        }

        private void ClearStrokes_OnClick(object sender, RoutedEventArgs e)
        {
            GrabCanvas.Strokes.Clear();
            Model.InkCanvasSource = Imaging.CreateBitmapSourceFromHBitmap(Model.Process.Screenshot.GetHbitmap(),
                IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }

        private void ShowStrokes_OnClick(object sender, RoutedEventArgs e)
        {
            Bitmap bitmap = new Bitmap(Model.Process.Screenshot.Width, Model.Process.Screenshot.Height);

            foreach (Stroke stroke in GrabCanvas.Strokes)
            {
                Rect rect = stroke.GetBounds();

                Rectangle tr = new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);

                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.DrawImage(Model.Process.Screenshot, new Rectangle(tr.X, tr.Y, tr.Width, tr.Height),
                                     tr, GraphicsUnit.Pixel);
                }

                stroke.DrawingAttributes.Color = Colors.Transparent;
            }

            Model.InkCanvasSource = Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(),
                      IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }
    }
}
