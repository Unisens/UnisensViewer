using System.Reflection;
using System.Windows;
using System.Diagnostics;
using System.Windows.Documents;

namespace UnisensViewer
{
	public partial class DialogAbout : Window
	{
		public DialogAbout()
		{
			this.InitializeComponent();

			Assembly a = Assembly.GetEntryAssembly();
			this.DataContext = a.GetName();
		}

        private void MyHyperlink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(((Hyperlink)sender).NavigateUri.ToString());
        }
	}
}
