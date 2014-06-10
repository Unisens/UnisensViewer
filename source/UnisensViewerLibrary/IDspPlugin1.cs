namespace UnisensViewerLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Xml.Linq;

    /// <summary>
    /// Interface definition for UnisensViewer plug-ins.
    /// </summary>
    public interface IDspPlugin1
    {
        /// <summary>
        /// Gets full name of the plug-in.
        /// </summary>
        string Name
        {
            get;
        }

        /// <summary>
        /// Gets short description of the plug-in.
        /// </summary>
        string Description
        {
            get;
        }

        /// <summary>
        /// Gets URL to a website with help information (e.g. http://www.unisens.org) or name of a help document contained in the Plugin folder.
        /// </summary>
        string Help
        {
            get;
        }

        /// <summary>
        /// Gets the group name of the corresponding group in the ribbon menu.
        /// </summary>
        string Group
        {
            get;
        }

        /// <summary>
        ///  Gets organization icon for the tooltip footer.
        /// </summary>
        BitmapImage OrganizationIcon
        {
            get;
        }

        /// <summary>
        /// Gets copyright information for the tooltip footer.
        /// </summary>
        string CopyrightInfo
        {
            get;
        }

        /// <summary>
        /// Gets large image for the ribbon menu.
        /// </summary>
        BitmapImage LargeRibbonIcon
        {
            get;
        }

        /// <summary>
        /// Gets small image for the ribbon menu.
        /// </summary>
        BitmapImage SmallRibbonIcon
        {
            get;
        }
        
        /// <summary>
        /// Gets the supported content classes.
        /// </summary>
        string[] SupportedContentClasses
        {
            get;
        }

        bool IsEnable
        {
            get;
            set;
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
        IEnumerable<XElement> Main(XDocument unisensxml, IEnumerable<XElement> selectedsignals, string path, double time_cursor, double time_start, double time_end, string parameter);
    }
}

