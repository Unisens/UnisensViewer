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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace UnisensViewer
{
    /// <summary>
    /// Interaktionslogik für DialogVideo.xaml
    /// </summary>
    public partial class DialogVideo : Window
    {
        public DialogVideo()
        {
            InitializeComponent();

        }

        public void SetVideoFile(Uri video)
        {
            Title = "Video: " + System.IO.Path.GetFileName(video.LocalPath);

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
    }
}
