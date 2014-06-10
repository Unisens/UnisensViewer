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

namespace UnisensViewer
{
    /// <summary>
    /// Interaktionslogik für DialogsArtifacts.xaml
    /// </summary>
    public partial class DialogsArtifacts : Window
    {
        public static bool artifact;
        public static string artifact_comment;
        public DialogsArtifacts()
        {
            InitializeComponent();
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            artifact_comment = textBox_Comment.Text.ToString();
            artifact = true;
            Close();
        }
    }
}
