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
    public partial class DialogsArtifactsNew : Window
    {
        public string ArtifactsName
        {
            get
            {
                return textBox_Comment.Text;
            }
        }

        public DialogsArtifactsNew()
        {
            InitializeComponent();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            textBox_Comment.SelectAll();
            textBox_Comment.Focus();
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            if(!string.IsNullOrEmpty(textBox_Comment.Text))
                DialogResult = true;
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
