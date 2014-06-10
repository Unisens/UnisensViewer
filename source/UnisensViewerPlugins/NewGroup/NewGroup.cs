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
    using org.unisens;

    [Export(typeof(IDspPlugin1))]
    class NewGroup : IDspPlugin1
    {
        private bool isEnable = false;
        #region RibbonConfig
        /// <summary>
        /// Gets full name of the plug-in.
        /// </summary>
        public string Name
        {
            get { return "Create Group"; }
        }

        /// <summary>
        /// Gets short description of the plug-in.
        /// </summary>
        public string Description
        {
            get
            {
                return "Creates a new group with the selected entries";
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
            get { return new BitmapImage(new Uri(@"Images\SmallIcon_group.png", UriKind.Relative)); }
        }

        /// <summary>
        /// Gets large image for the ribbon menu.
        /// </summary>
        public BitmapImage LargeRibbonIcon
        {
            get { return new BitmapImage(new Uri(@"Images\LargeIcon_group.png", UriKind.Relative)); }
        }

        /// <summary>
        /// Gets copyright information for the tooltip footer.
        /// </summary>
        public string CopyrightInfo
        {
            get { return "(c) 2011 FZI Forschungszentrum Informatik"; }
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
        #endregion

        public IEnumerable<XElement> Main(XDocument unisensxml, IEnumerable<XElement> selectedsignals, string path, double time_cursor, double time_start, double time_end, string parameter)
        {
            List<XElement> returnElementList = new List<XElement>();
            string groupId = null;
            DialogNewGroup dialogNewGroup = new DialogNewGroup();
            dialogNewGroup.Topmost = true;
            if (dialogNewGroup.ShowDialog() != (DialogNewGroup.newgroup))
            {
                groupId = DialogNewGroup.idOfTheGroup;
            }
            path = path.Substring(0, path.Length - 11);
            UnisensFactory factory = UnisensFactoryBuilder.createFactory();
            org.unisens.Unisens unisens = factory.createUnisens(path);
            org.unisens.Group group = (org.unisens.Group)unisens.createGroup(groupId);
            XElement groupElement = new XElement("{http://www.unisens.org/unisens2.0}group",
                                        new XAttribute("id", groupId)
                                        );
            foreach (XElement xe in selectedsignals)
            {
                XElement xelement = new XElement("{http://www.unisens.org/unisens2.0}groupEntry",
                                        new XAttribute("ref", xe.Attribute("id").Value));
                groupElement.Add(xelement);
            }

            returnElementList.Add(groupElement);
            unisensxml.Root.Add(groupElement);
            unisens.closeAll();
            return returnElementList;
        }
    }
}

