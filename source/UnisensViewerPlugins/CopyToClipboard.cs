//-----------------------------------------------------------------------
// <copyright file="CopyToClipboard.cs" company="FZI Forschungszentrum Informatik">
// Copyright 2011 FZI Forschungszentrum Informatik, movisens GmbH
// Malte Kirst (kirst@fzi.de)
// </copyright>
//-----------------------------------------------------------------------
namespace UnisensViewerPack1
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Resources;
    using System.Xml.Linq;
    using UnisensViewerLibrary;

    /// <summary>
    /// Dieses Plug-in kopiert die Daten an der aktuellen Cursorposition bzw. des ausgewählten
    /// Bereichs in die Zwischenablage, so dass sie in Matlab eingefügt werden können.
    /// <para>
    /// Copyright 2010 FZI Forschungszentrum Informatik
    /// Malte Kirst (kirst@fzi.de)
    /// </para>
    /// </summary>
    [Export(typeof(IDspPlugin1))]
    public class CopyToClipboard : IDspPlugin1
    {
        private bool isEnable = false;
        /// <summary>
        /// Gets full name of the plug-in.
        /// </summary>
        public string Name
        {
            get { return "Copy to Clipboard"; }
        }

        /// <summary>
        /// Gets short description of the plug-in.
        /// </summary>
        public string Description
        {
            get 
            {
                return "Copies the cursor data (current cursor " +
                         "position or selection) to the clipboard."; 
            }
        }

        /// <summary>
        /// Gets organization icon for the tooltip footer.
        /// </summary>
        public BitmapImage OrganizationIcon
        {
            get { return new BitmapImage(new Uri(@"Images\OrganizationIcon_FZI.png", UriKind.Relative)); }
        }

        /// <summary>
        /// Gets URL to the website with help information (e.g. http://www.unisens.org) or name of a help document contained in the Plugin folder.
        /// </summary>
        public string Help
        {
            get { return @"CopyToClipboard.txt"; }
        }

        /// <summary>
        /// Gets the group name of the corresponding group in the ribbon menu.
        /// </summary>
        public string Group
        {
            get { return "Misc"; }
        }

        /// <summary>
        /// Gets small image for the ribbon menu.
        /// </summary>
        public BitmapImage SmallRibbonIcon
        {
            get { return new BitmapImage(new Uri(@"Images\120px-Edit-copy.svg.png", UriKind.Relative)); }
        }

        /// <summary>
        /// Gets large image for the ribbon menu.
        /// </summary>
        public BitmapImage LargeRibbonIcon
        {
            get { return new BitmapImage(new Uri(@"Images\120px-Edit-copy.svg.png", UriKind.Relative)); }
        }

        /// <summary>
        /// Gets copyright information for the tooltip footer.
        /// </summary>
        public string CopyrightInfo
        {
            get { return "Produktname: UnisensViewer\nProduktversion: 1.0.242.0\nCopyright: 2009-2010 FZI Forschungszentrum Informatik"; }
        }

        /// <summary>
        /// Gets the supported content classes.
        /// </summary>
        public string[] SupportedContentClasses
        {
            get { return null; }
        }

        /// <summary>
        /// Gets or set character enable of the plug-in.
        /// </summary>
        public bool IsEnable
        {
            get { return isEnable; }
            set { isEnable = value; }
        }
        /// <summary>
        /// Main function for plug-ins, called by UnisensViewer.
        /// </summary>
        /// <param name="unisensxml">unisens.xml file.</param>
        /// <param name="selectedsignals">All information from unisens.xml of the selected signals.</param>
        /// <param name="path">Path of the current unisens.xml file.</param>
        /// <param name="time_cursor">Time in seconds of current cursor position. Is 0, if the plug-in is called via plug-in menu.</param>
        /// <param name="time_start">Time in seconds of start of the current selection. Is 0, when no selection exists.</param>
        /// <param name="time_end">Time in seconds of end of the current selection. Is 0, when no selection exists.</param>
        /// <param name="parameter">Additional parameter of the key bindings.</param>
        /// <returns>
        /// Returned signals have to be described by the corresponding Unisens XML element (e.g. signalEntry or eventEntry). UnisensViewer displays the returned signals directly.
        /// </returns>
        public IEnumerable<XElement> Main(XDocument unisensxml, IEnumerable<XElement> selectedsignals, string path, double time_cursor, double time_start, double time_end, string parameter)
        {
            int sample_start = 0;
            int sample_end = 0;
            double sampleValueMin = 0;
            double sampleValueMax = 0;
            string clipboard = string.Empty;
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

                        ////TODO: Read values at position positionSampleStart and positionSampleEnd
                        sampleValueMin = 1;
                        sampleValueMax = 2;
                        clipboard += "unisensViewer(" + i + ").physicalValue = [" + ((sampleValueMin - MeasurementEntry.GetBaseline(xe)) * MeasurementEntry.GetLsbValue(xe)) + ", " + ((sampleValueMax - MeasurementEntry.GetBaseline(xe)) * MeasurementEntry.GetLsbValue(xe)) + "];\n";
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
            //     cursor time    sample index
            // from    00:00:00.123    532
            // to    00:00:01.123    2343
            //
            //     physical value    sample value
            // min    5.4384g    23452
            // max    5.4385g    23453

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
            ////

            return null;
        }
    }
}

