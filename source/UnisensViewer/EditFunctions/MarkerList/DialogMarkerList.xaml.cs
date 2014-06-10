using System;
using System.Collections.Generic;
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
using System.IO;

namespace UnisensViewer
{
    /// <summary>
    /// Interaktionslogik für DialogMarkerList.xaml
    /// </summary>
    public partial class DialogMarkerList : Window
    {
        public static bool markerlist;
        public static double sampleRate;
        public static string entryId;
        public static string textfeld;
        public static string comment;
        public DialogMarkerList()
        {
            InitializeComponent();
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            sampleRate = Convert.ToDouble(tb_SampleRate.Text);
            entryId = tb_EntryId.Text;
            textfeld = tb_textfeld.Text;
            comment = tb_Comment.Text;
            markerlist = true;
            Close();
        }
    }
}
