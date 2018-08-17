using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace UnisensViewer
{
    //http://stackoverflow.com/questions/4189660/why-does-wpf-mediaelement-not-work-on-secondary-monitor


    /// <summary>
    /// Interaktionslogik für DialogVideo.xaml
    /// </summary>
    public partial class DialogVideo : Window
    {
        public TimeSpan TotalTime;

        public DialogVideo()
        {
            InitializeComponent();
        }

        public void SoftwareRenderMode()
        {
            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            if (hwndSource != null)
            {
                var hwndTarget = hwndSource.CompositionTarget;
                if (hwndTarget != null) hwndTarget.RenderMode = RenderMode.SoftwareOnly;
            }
        }

        private void timeSlider_MouseMove(object sender, MouseEventArgs e)
        {
            RendererManager.Scroll(timeSlider.Value * RendererManager.TimeMax);
        }

        private void mePlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            TotalTime = mePlayer.NaturalDuration.TimeSpan;
        }

        private void mePlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("mePlayer_MediaEnded");
        }

        private void mePlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show("mePlayer_MediaFailed");
        }

        public void SetVideoFile(Uri video)
        {
            Title = System.IO.Path.GetFileName(video.LocalPath) + " - VideoViewer";

            mePlayer.Source = video; 
        }

        public void Play()
        {
            mePlayer.Play();
        }

        public void Pause()
        {
            mePlayer.Pause();
        }

        public void Stop()
        {
            mePlayer.Stop();
        }

        public void Seek(int milliseconds)
        {
            mePlayer.Position = new TimeSpan(0, 0, 0, 0, milliseconds);
        }


        private void Window_Closed(object sender, EventArgs e)
        {
            mePlayer.LoadedBehavior = MediaState.Close;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            //mePlayer.Stop();
            //mePlayer.Close();
            //mePlayer.Pause();
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            SoftwareRenderMode();
        }


     }

}
