using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnisensViewerLibrary;
using System.ComponentModel.Composition;
using System.Xml.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Reflection;
using System.Drawing;
using System.Windows;
using System.Windows.Resources;


namespace PlugInStandard
{

	/// <summary>
	/// Dieses Plug-in kopiert die Daten an der aktuellen Cursorposition bzw. des ausgewählten
	/// Bereichs in die Zwischenablage, so dass sie in Matlab eingefügt werden können.
	/// 
	/// Copyright 2010 FZI Forschungszentrum Informatik
	///                Malte Kirst (kirst@fzi.de)
	/// </summary>

	[Export(typeof(IDspPlugin))]
	public class CopyToClipboard : IDspPlugin
	{


		public string Name
		{
			get { return "Copy to Clipboard"; }
		}

		public string Description
		{
			get { return "Copies the cursor data (current cursor " +
                         "position or selection) to the clipboard."; }
		}

        public BitmapImage OrganizationIcon
        {
            get
            {
                Assembly myAssembly = Assembly.GetExecutingAssembly();
                Stream myStream = myAssembly.GetManifestResourceStream("PlugInStandard.OrganizationIcon_FZI.png");

                BitmapImage bImg = new BitmapImage();
                bImg.BeginInit();
                bImg.StreamSource = myStream;
                bImg.EndInit();

                return bImg;
            }
        }

        public string Help
        {
            get { return @"CopyToClipboard.txt"; }
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
                Stream myStream = myAssembly.GetManifestResourceStream("PlugInStandard.120px-Edit-copy.svg.png");

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
                Stream myStream = myAssembly.GetManifestResourceStream("PlugInStandard.120px-Edit-copy.svg.png");

                BitmapImage bImg = new BitmapImage();
                bImg.BeginInit();
                bImg.StreamSource = myStream;
                bImg.EndInit();

                return bImg;
            }
        }

        public string CopyrightInfo
        {
            get { return "Produktname: UnisensViewer\nProduktversion: 1.0.242.0\nCopyright: 2009-2010 FZI Forschungszentrum Informatik"; }
        }

		public IEnumerable<XElement> Main(XDocument unisensxml, IEnumerable<XElement> selectedsignals, double time_cursor, double time_start, double time_end, string parameter)
		{
            int sample_start = 0;
            int sample_end = 0;
            double sampleValueMin = 0;
            double sampleValueMax = 0;
            string clipboard = "";
            int i = 1;


            // When time_cursor is used (context menu or hot key), read data from cursor position. Otherwise read data from selection.
            if (time_cursor != 0)
            {
                time_start = time_cursor;
                time_end = time_cursor;
            }

            foreach (XElement xe in selectedsignals)
            {
                switch (xe.Name.LocalName)
                {
                    case "signalEntry":
                    case "valuesEntry":

                        sample_start = (int)Math.Floor(MeasurementEntry.GetSampleRate(xe) * time_start);
                        time_start = sample_start / MeasurementEntry.GetSampleRate(xe);
                        sample_end = (int)Math.Ceiling(MeasurementEntry.GetSampleRate(xe) * time_end);
                        time_end = sample_end / MeasurementEntry.GetSampleRate(xe);

                        clipboard += "unisensViewer(" + i + ").cursorTime = [datenum('" + time_start + "', 'SS.FFF'), datenum('" + time_end + "', 'SS.FFF'), ];\n";
                        clipboard += "unisensViewer(" + i + ").sampleIndex = ['" + sample_start + "', '" + sample_end + "'];\n";
                        clipboard += "unisensViewer(" + i + ").unit = '" + MeasurementEntry.GetUnit(xe) + "';\n";

                        //TODO: Read values at position positionSampleStart and positionSampleEnd
                        sampleValueMin = 1;
                        sampleValueMax = 2;
                        clipboard += "unisensViewer(" + i + ").physicalValue = [" + ((sampleValueMin + MeasurementEntry.GetBaseline(xe)) * MeasurementEntry.GetLsbValue(xe)) + ", " + ((sampleValueMax + MeasurementEntry.GetBaseline(xe)) * MeasurementEntry.GetLsbValue(xe)) + "];\n";
                        clipboard += "unisensViewer(" + i + ").sampleValue = [" + sampleValueMin + ", " + sampleValueMax + "];\n";

                        i++;
                        break;
                }
            }
            Clipboard.SetDataObject(clipboard, true);
            MessageBox.Show("Cursor data at sample " + sample_start + " copied to clipbaord (Matlab style).\n" + clipboard, "Copy to Clipboard");


            // Example (Matlab style):
            // unisensViewer(1).cursorTime = ['00:00:00.123', '00:00:01.123'];
            // unisensViewer(1).sampleIndex = [532, 2343];		
            // unisensViewer(1).physicalValue = [5.4384, 5.4385];
            // unisensViewer(1).Unit = 'g';
            // unisensViewer(1).sampleValue = [23452, 23453];

            // Example (Excel style):
            //     cursor time	sample index
            // from	00:00:00.123	532
            // to	00:00:01.123	2343
		    //
            //     physical value	sample value
            // min	5.4384g	23452
            // max	5.4385g	23453

            // Example (plain text):
            // cursor time
            // from 00:00:00.123
            // to 00:00:01.123
            //
            // sample index
            // from 532
            // to 2343
	        //
            // physical value
            // min 5.4384g	
            // max 5.4385g	
            //
            // sample value
            // min 23452
            // max 23453

			return null;
		}
	}
}

