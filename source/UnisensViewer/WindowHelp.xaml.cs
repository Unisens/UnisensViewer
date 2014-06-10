using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Xps.Packaging;

namespace UnisensViewer
{
	/// <summary>
	/// Interaction logic for WindowHelp.xaml
	/// </summary>
	public partial class WindowHelp : Window
	{
		public WindowHelp()
		{
			InitializeComponent();
            try
            {
                string currentCulture = Thread.CurrentThread.CurrentUICulture.ToString().Substring(0,2).ToUpper();
                if (currentCulture != "DE") currentCulture = "EN"; //All other languages see the english one.
                XpsDocument xps = new XpsDocument(Folders.UnisensViewer + @"\Documentation\UnisensViewerHelp_" + currentCulture + ".xps", System.IO.FileAccess.Read);
                helpDocumentViewer.Document = xps.GetFixedDocumentSequence();
            }
            catch
            {
                // open the Unisens website, if local help not avaliable
                System.Diagnostics.Process.Start("http://unisens.org/documentation.php");
            }
        }
	}
}
