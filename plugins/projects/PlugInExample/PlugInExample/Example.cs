using System.Collections.Generic;           // IEnumerable
using System.ComponentModel.Composition;    // Export (System.ComponentModel.Composition)
using System.IO;                            // Stream 
using System.Windows.Media.Imaging;         // BitmapImage (PresentaionCore)
using System.Xml.Linq;                      // XElement, XDocument (System.Xml.Linq)
using System.Reflection;                    // Assembly
using UnisensViewerLibrary;                 // IDspPlugin (UnisensViewerLibrary)
using System.Windows.Forms;                 // MessageBox (System.Windows.Forms)


namespace PlugInExample
{
    [Export(typeof(IDspPlugin))]
    public class Example : IDspPlugin
    {
        
        public string Name
        {
            get { return "Example"; }
        }

        public string Description
        {
            get { return "Here is an example for a short description"; }
        }

        public string Help
        {
            get { return "http://de.wikipedia.org/wiki/Example"; }
        }

        public string Group
        {
            get { return "Misc"; }
        }

        public BitmapImage SmallRibbonIcon
        {
            get
            {
                Assembly myAssembly = Assembly.GetExecutingAssembly();
                Stream myStream = myAssembly.GetManifestResourceStream("PlugInExample.SmallIcon_misc_example.png");

                BitmapImage bImg = new BitmapImage();
                bImg.BeginInit();
                bImg.StreamSource = myStream;
                bImg.EndInit();

                return bImg;
            }
        }

        public BitmapImage LargeRibbonIcon
        {
            get
            {
                Assembly myAssembly = Assembly.GetExecutingAssembly();
                Stream myStream = myAssembly.GetManifestResourceStream("PlugInExample.LargeIcon_misc_example.png");

                BitmapImage bImg = new BitmapImage();
                bImg.BeginInit();
                bImg.StreamSource = myStream;
                bImg.EndInit();

                return bImg;
            }
        }

        public BitmapImage OrganizationIcon
        {
            get
            {
                Assembly myAssembly = Assembly.GetExecutingAssembly();
                Stream myStream = myAssembly.GetManifestResourceStream("PlugInExample.OrganizationIcon_fzi.png");

                BitmapImage bImg = new BitmapImage();
                bImg.BeginInit();
                bImg.StreamSource = myStream;
                bImg.EndInit();

                return bImg;
            }
        }

        public string CopyrightInfo
        {
            get { return "(c) 2009-2010 FZI Forschungszentrum Informatik"; }
        }

        public IEnumerable<XElement> Main(XDocument unisensxml, IEnumerable<XElement> selectedsignals, double time_cursor, double time_start, double time_end, string parameter)
        {
            // Gather all entry-ids of the selected signals.
            string entryNames = "";
            foreach (XElement xe in selectedsignals)
            {
                entryNames += "\n - " + Entry.GetId(xe);
            }

            // Show all method parameter
            MessageBox.Show("Entries: " + entryNames + "\n\n" +
                            "Parameter: " + parameter + "\n\n" +
                            "Start Time: " + time_start + "s\n" +
                            "End Time: " + time_end + "s\n" +
                            "Cursor Time: " + time_cursor + "s\n", 
                            "Duplicate Example");

            return null;
        }
    }
}
