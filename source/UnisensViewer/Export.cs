using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace UnisensViewer
{
    public class Export
    {
        public static void ToPNG(string path, FrameworkElement obj)
        {
            int height = (int)obj.ActualHeight;
            int width = (int)obj.ActualWidth;

            Matrix m = PresentationSource.FromVisual(obj).CompositionTarget.TransformToDevice;
            double dpiX = m.M11;
            double dpiY = m.M22;

            RenderTargetBitmap bmp = new RenderTargetBitmap(width, height, dpiX * 96, dpiY * 96, PixelFormats.Pbgra32);

            bmp.Render(obj);

            BitmapEncoder encoder = new PngBitmapEncoder();

            encoder.Frames.Add(BitmapFrame.Create(bmp));

            using (Stream stm = File.Create(path))
            {
                encoder.Save(stm);
            }
        }

        public static void Print(FrameworkElement obj)
        {
            PrintDialog printDlg = new System.Windows.Controls.PrintDialog();
            if (printDlg.ShowDialog() == true)
            {
                Transform transform = obj.LayoutTransform;

                // get selected printer capabilities
                System.Printing.PrintCapabilities capabilities = printDlg.PrintQueue.GetPrintCapabilities(printDlg.PrintTicket);

                // get scale of the print wrt to screen of WPF visual
                double scale = Math.Min(capabilities.PageImageableArea.ExtentWidth / obj.ActualWidth, capabilities.PageImageableArea.ExtentHeight / obj.ActualHeight);

                // Transform the Visual to scale
                obj.LayoutTransform = new ScaleTransform(scale, scale);

                // get the size of the printer page
                Size sz = new Size(capabilities.PageImageableArea.ExtentWidth, capabilities.PageImageableArea.ExtentHeight);

                // update the layout of the visual to the printer page size.
                obj.Measure(sz);
                obj.Arrange(new Rect(new Point(capabilities.PageImageableArea.OriginWidth, capabilities.PageImageableArea.OriginHeight), sz));

                // now print the visual to printer to fit on the one page.
                printDlg.PrintVisual(obj, "UnisensViewer");

                obj.LayoutTransform = transform;
            }
        }
    }
}
