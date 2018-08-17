using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using System.Xml.Schema;

namespace UnisensViewer
{
	public partial class XmlControl : UserControl
	{
		public static readonly DependencyProperty XmlFilePathProperty = DependencyProperty.Register("XmlFilePath", typeof(string), typeof(XmlControl));

		protected UnisensXmlFileManager fileManager;

		private XmlSchemaSet xmlschemaset;
		
		private List<object>				validationmessages;
		private string 						xsdfilepath;

		public XmlControl()
		{
			fileManager = UnisensXmlFileManager.CurrentUnisensInstance;
			fileManager.FileLoaded += new UnisensXmlFileManager.FileEventHandler(fileManager_FileLoaded);
			fileManager.FileClosed += new UnisensXmlFileManager.FileEventHandler(fileManager_FileClosed);

			InitializeComponent();
		}

		public XmlControl(string xsdfilepath)
		{
			fileManager = UnisensXmlFileManager.CreateNewInstance;
			fileManager.FileLoaded += new UnisensXmlFileManager.FileEventHandler(fileManager_FileLoaded);
			fileManager.FileClosed += new UnisensXmlFileManager.FileEventHandler(fileManager_FileClosed);

			fileManager.LoadXsd(xsdfilepath);

			InitializeComponent();
		}

		protected void fileManager_FileLoaded(object sender, bool successfull)
		{
			this.IsEnabled = true;
			this.SelectXElement(null);

			if (successfull)
			{
				this.treeview.ItemContainerGenerator.StatusChanged += new EventHandler(this.ItemContainerGenerator_StatusChanged);
				this.treeview.ItemsSource = fileManager.Xdocument.Elements();
			}
		}

		public string SchemaNameSpace
		{
			get
			{
				if (this.xmlschemaset != null)
				{
					foreach (XmlSchemaElement xse in this.xmlschemaset.GlobalElements.Values)
					{
						return xse.QualifiedName.Namespace;
					}
				}

				return null;
			}
		}

		public bool LoadXml(string xmlfilepath)
		{
			return fileManager.LoadXml(xmlfilepath);
		}

		public void LoadXmlParseString(string xmlstring)
		{
			fileManager.LoadXmlParseString(xmlstring);
		}

		private void fileManager_FileClosed(object sender, bool successfull)
		{
			if (successfull)
			{
				treeview.ItemsSource = null;
				this.SelectXElement(null);
				expander_validation.Content = null;

				IsEnabled = false;
			}
		}

		protected virtual object CreateContentViewer(XElement selectedelement)
		{
			return null;
		}

		private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
		{
			this.ExpandFirstLevel();
		}

		private void ExpandFirstLevel()
		{
			ItemContainerGenerator icg = treeview.ItemContainerGenerator;

            if (treeview.Items.Count > 0)
			{
                TreeViewItem tvi = icg.ContainerFromItem(treeview.Items[0]) as TreeViewItem;
                if (tvi != null)
                {
                	tvi.IsExpanded = true;
                }
			}
		}

		private void Treeview_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			this.SelectXElement(e.NewValue as XElement);
		}

		private void SelectXElement(XElement selected)
		{
			if (selected != null)
			{
				textblock_xmlcode.Text = selected.ToString();
				contentpresenter_editcontrol.Content = new XmlEditControl(selected);
				expander_content.Content = this.CreateContentViewer(selected);
			}
			else
			{
				textblock_xmlcode.Text = null;
				contentpresenter_editcontrol.Content = null;
				expander_content.Content = null;
			}
		}
	}
}
