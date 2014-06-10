//--------------------------------------------------------------------------------------------
// <copyright file="Measurement.cs" company="FZI Forschungszentrum Informatik">
// Copyright 2011 FZI Forschungszentrum Informatik, movisens GmbH
// Giau Pham (pham@fzi.de)
// </copyright>
//--------------------------------------------------------------------------------------------
namespace UnisensViewerPack1
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Windows.Forms;
    using System.Windows.Media.Imaging;
    using System.Xml.Linq;
    using UnisensViewerClrCppLibrary;
    using UnisensViewerLibrary;
    using org.unisens;

    /// <summary>
    /// This Plugin show the information of the entry(time_start, time_end, daten,...) in the selected intervall.
    /// </summary>
    [Export(typeof(IDspPlugin1))]
    public class Measurement : IDspPlugin1
    {
        private bool isEnable = false;
        /// <summary>
        /// time_start, it will be used by the dialog for showing
        /// </summary>
        private static double pluginTimeStart;

        /// <summary>
        /// time_end, it will be used by the dialog for showing
        /// </summary>
        private static double pluginTimeEnd;

        /// <summary>
        /// time_lenght(intervall) for showing in dialog
        /// </summary>
        private static double pluginTimeLength;

        /// <summary>
        /// Collection of selected signals
        /// </summary>
        private static IEnumerable<XElement> selectedSignals;

        /// <summary>
        /// the document unisens.xml
        /// </summary>
        private static XDocument unisensXml;

        private static org.unisens.SignalEntry entry;

        /// <summary>
        /// Gets the plugin time start.
        /// </summary>
        public static double PluginTimeStart
        {
            get
            {
                return pluginTimeStart;
            }
        }

        /// <summary>
        /// Gets the plugin time end.
        /// </summary>
        public static double PluginTimeEnd
        {
            get
            {
                return pluginTimeEnd;
            }
        }

        /// <summary>
        /// Gets the length of the plugin time.
        /// </summary>
        /// <value>
        /// The length of the plugin time.
        /// </value>
        public static double PluginTimeLength
        {
            get
            {
                return pluginTimeLength;
            }
        }

        /// <summary>
        /// Gets the selected signals.
        /// </summary>
        public static IEnumerable<XElement> SelectedSignals
        {
            get
            {
                return selectedSignals;
            }
        }

        public static org.unisens.SignalEntry Entry
        {
            get
            {
                return entry;
            }
        }

        /// <summary>
        /// Gets the unisens XML.
        /// </summary>
        public static XDocument UnisensXml
        {
            get
            {
                return unisensXml;
            }
        }

        /// <summary>
        /// Gets full name of the plug-in.
        /// </summary>
        public string Name
        {
            get { return "Measurement"; }
        }

        /// <summary>
        /// Gets the supported content classes.
        /// </summary>
        public string[] SupportedContentClasses
        {
            get { return null; }
        }

        /// <summary>
        /// Gets organization icon for the tooltip footer.
        /// </summary>
        public BitmapImage OrganizationIcon
        {
            get { return new BitmapImage(new Uri(@"Images\OrganizationIcon_FZI.png", UriKind.Relative)); }
        }

        /// <summary>
        /// Gets short description of the plug-in.
        /// </summary>
        public string Description
        {
            get { return "show max and min physical value and different of them in the interval"; }
        }

        /// <summary>
        /// Gets URL to a website with help information (e.g. http://www.unisens.org) or name of a help document contained in the Plugin folder.
        /// </summary>
        public string Help
        {
            get { return "http://www.unisens.org"; }
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
            get { return new BitmapImage(new Uri(@"Images\SmallIcon_misc_measurement.png", UriKind.Relative)); }
        }

        /// <summary>
        /// Gets large image for the ribbon menu.
        /// </summary>
        public BitmapImage LargeRibbonIcon
        {
            get { return new BitmapImage(new Uri(@"Images\SmallIcon_misc_measurement.png", UriKind.Relative)); }
        }

        /// <summary>
        /// Gets copyright information for the tooltip footer.
        /// </summary>
        public string CopyrightInfo
        {
            get { return "Produktname: UnisensViewer\nProduktversion: 1.0.242.0\nCopyright: 2009-2010 FZI Forschungszentrum Informatik"; }
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
            //// When time_cursor is used (context menu or hot key), read data from cursor position. Otherwise read data from selection.
            //if (time_cursor != 0)
            //{
            //    time_start = time_cursor;
            //    time_end = time_cursor;
            //}
            List<string> entryIds = GetEntryList(selectedsignals, "SignalEntry");
            if (entryIds.Count == 0)
            {
                MessageBox.Show("The selected entry is no signalEntry. "
                                , "Wrong signalEntry",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            else if (entryIds.Count > 1)
            {
                if (MessageBox.Show("More than one matching entry was selected. I use \n" +
                                entryIds[0] + " for further processing.",
                                "SignalEntry overkill",
                                MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.Cancel)
                {
                    return null;
                }
            }
            path = path.Substring(0, path.Length - 11);
            UnisensFactory factory = UnisensFactoryBuilder.createFactory();
            org.unisens.Unisens unisens = factory.createUnisens(path);
            entry = (org.unisens.SignalEntry)unisens.getEntry(entryIds[0]);
            unisensXml = unisensxml;
            selectedSignals = selectedsignals;
            pluginTimeStart = time_start;
            pluginTimeEnd = time_end;
            pluginTimeLength = time_end - time_start;
            DialogMeasurement dlgMeasurement = new DialogMeasurement();
            dlgMeasurement.Topmost = true;
            dlgMeasurement.Show();

            return null;
        }

        public static List<string> GetEntryList(IEnumerable<XElement> selectedSignals, string entryType)
        {
            List<string> entryIds = new List<string>();

            foreach (XElement element in selectedSignals)
            {
                XAttribute id = element.Attribute("id");

                if (id == null)
                {
                    break;
                }


                // Check entry type.
                if (entryType != "*" && string.Compare(element.Name.LocalName, entryType, true) != 0)
                {
                    break;
                }

                entryIds.Add(id.Value);
            }

            return entryIds;
        }
    }   
}