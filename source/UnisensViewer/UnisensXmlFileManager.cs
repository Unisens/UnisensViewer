using System;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Xml.Schema;
using System.Collections.Generic;

namespace UnisensViewer
{
	public class UnisensXmlFileManager
	{

		public delegate void FileEventHandler(object sender, bool successfull);
		public event FileEventHandler FileLoaded;
		public event FileEventHandler FileClosed;

		private static UnisensXmlFileManager currentUnisensInstance = new UnisensXmlFileManager();

		private XmlSchemaSet xmlschemaset;

		private List<object> validationmessages;
		private string xsdfilepath;
		
		private bool isDirty;

		private UnisensXmlFileManager()
		{
		}

		public static UnisensXmlFileManager CurrentUnisensInstance
		{
			get
			{
				return currentUnisensInstance;
			}
		}

		public static UnisensXmlFileManager CreateNewInstance
		{
			get
			{
				return new UnisensXmlFileManager();
			}
		}

		public XDocument Xdocument { get; set; }

		public string XmlFilePath { get; set; }

		public static string TimeStampStart { get; set; }

		public bool IsDirty
		{
			get
			{
				return this.isDirty;
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

		public static XDocument FixNamespace(XDocument source, XNamespace xNamespace)
		{
			foreach (XElement xElement in source.Descendants())
			{
				// First make sure that the xmlns-attribute is changed
				xElement.SetAttributeValue("xmlns", xNamespace.NamespaceName);

				// Then also prefix the name of the element with the namespace
				xElement.Name = xNamespace + xElement.Name.LocalName;
			}

			return source;
		}

		public void New()
		{
			LoadXsd(Folders.UnisensViewer + "unisens.xsd");
			LoadXmlParseString("<unisens xmlns=\"http://www.unisens.org/unisens2.0\"/>");
			this.isDirty = false;
		}

		public bool LoadXml(string xmlfilepath)
		{
			this.isDirty = false;
			
			try
			{
				this.Xdocument = XDocument.Load(xmlfilepath);

				// Hotfix if the namespace is missing in the Document
				if (String.Compare(this.Xdocument.Root.Name.Namespace.ToString(), "http://www.unisens.org/unisens2.0", true) != 0)
				{
					this.Xdocument = FixNamespace(this.Xdocument, "http://www.unisens.org/unisens2.0");
				}

                this.XmlFilePath = xmlfilepath;

                this.Xdocument.Changed += new EventHandler<XObjectChangeEventArgs>(this.XDocument_Changed);
                this.Validate();

                

                raiseFileLoaded(true);
			}
			catch (Exception)
			{
				this.Xdocument = null;
				this.XmlFilePath = null;

				raiseFileLoaded(false);
				return false;
			}

			return true;
		}

		public void LoadXmlParseString(string xmlstring)
		{
			this.isDirty = true;
			this.XmlFilePath = null;
			this.Xdocument = XDocument.Parse(xmlstring);
			
			this.Xdocument.Changed += new EventHandler<XObjectChangeEventArgs>(this.XDocument_Changed);
			this.Validate();

			raiseFileLoaded(true);
		}

		private void raiseFileLoaded(bool successfull)
		{
			if (this.FileLoaded != null)
			{
				this.FileLoaded(this, successfull);
			}
		}

		public void LoadXsd(string xsdfilepath)
		{
			try
			{
				this.xmlschemaset = new XmlSchemaSet();

				this.xmlschemaset.Add(null, xsdfilepath);
				this.xmlschemaset.Compile();

				this.xsdfilepath = xsdfilepath;
			}
			catch (Exception)
			{
				this.xmlschemaset = null;
				this.xsdfilepath = null;
			}
		}

		public void Save()
		{
			this.Xdocument.Save(this.XmlFilePath);

			this.isDirty = false;
		}

		public void SaveAs(string xmlfilepath)
		{
			this.Xdocument.Save(xmlfilepath);

			this.isDirty = false;
			this.XmlFilePath = xmlfilepath;
		}

		public void Close()
		{
            SessionSettings.Instance.Update();
            SessionSettings.Instance.WriteObject(); 

			this.Xdocument = null;
			this.XmlFilePath = null;

			if (this.FileClosed != null)
			{
				this.FileClosed(this, true);
			}
		}

		private void XDocument_Changed(object sender, XObjectChangeEventArgs e)
		{
			this.Validate();
			// TODO textblock_xmlcode.Text = treeview.SelectedItem != null ? treeview.SelectedItem.ToString() : null;
			this.isDirty = true;
		}

		private void Validate()
		{
			if (this.xmlschemaset != null)
			{
				this.validationmessages = new List<object>();

				// unter manchen umständen* wirft die validierung trotzdem eine exception
				//
				// * 1. in einem child-element ein nicht-schema-deklariertes attribut erstellen.
				//      die validierung beschwert sich, aber es gibt noch keine exception.
				//   2. in dem parent-element nochmal das gleiche nicht-schema-deklariertes attribut erstellen.
				//      jetzt wirds dem xml-parser zuviel...
				//
				// * wenn man bei einem signalEntry einen XText child-node erzeugt und anschließend wieder löscht
				try
				{
					this.Xdocument.Validate(this.xmlschemaset, this.ValidationCallback, true);
				}
				catch (Exception e)
				{
					this.validationmessages.Add(e);
					////MessageBox.Show(e.Message, "Validierung", MessageBoxButton.OK, MessageBoxImage.Error);
				}

				// TODO expander_validation.Content = this.validationmessages.Count == 0 ? null : this.validationmessages;
			}
		}

		private void ValidationCallback(object sender, ValidationEventArgs args)
		{
			this.validationmessages.Add(args);
		}
		
		public void Load(string xmlfilepath)
		{
			if (!File.Exists(xmlfilepath))
			{
				return;
			}

			Environment.CurrentDirectory = System.IO.Path.GetDirectoryName(xmlfilepath);
            if (LoadXml(xmlfilepath) == true)
            {
                // TimeStampStart auslesen und speichern
                TimeStampStart = null;
                XElement xunisens = this.Xdocument.Root;
                if (xunisens != null)
                {
                    XAttribute timestampStart = xunisens.Attribute("timestampStart");
                    if (timestampStart != null)
                    {
                        TimeStampStart = timestampStart.Value;
                    }
                }
            }else{
                //LoadXmlParseString("<unisens xmlns=\"http://www.unisens.org/unisens2.0\"/>"); //Makes file dirty and asks user if he want so save file
            }
		}

		public void AddFile(string filepath)
		{
			if (XmlFilePath == null)
			{
				// es wurde noch keine unisens.xml geladen oder erzeugt/gespeichert (außer "New();")
				Environment.CurrentDirectory = System.IO.Path.GetDirectoryName(filepath);
				XmlFilePath = System.IO.Path.Combine(Environment.CurrentDirectory, "unisens.xml");
			}

			string unisensdir = System.IO.Path.GetDirectoryName(XmlFilePath);
			string filedir = System.IO.Path.GetDirectoryName(filepath);

			// die datei muss im gleichen verzeichnis sein wie die unisens.xml
			if (string.Compare(unisensdir, filedir, StringComparison.OrdinalIgnoreCase) == 0)
			{
				string filename = System.IO.Path.GetFileName(filepath);

				switch (System.IO.Path.GetExtension(filename).ToLowerInvariant())
				{
					case ".bin":
						this.AddFile_bin(filename);
						break;

					case ".csv":
						this.AddFile_csv(filename);
						break;

					case ".xml":
						this.AddFile_xml(filename, filepath);
						break;

					case ".jpg":
					case ".bmp":
					case ".tif":
					case ".tiff":
					case ".png":
					case ".gif":
						this.AddFile_picture(filename);
						break;
				}
			}
		}

		public object CreateContentViewer_context(XElement selectedelement)
		{
			XAttribute schemaurl = selectedelement.Attribute("schemaUrl");
			string xsdName;

			if (schemaurl != null)
			{
				xsdName = schemaurl.Value;
			}
			else
			{
				xsdName = "context.xsd";
			}

			XmlControl xmlcontrol = new XmlControl(xsdName);

			if (xmlcontrol.LoadXml("context.xml") == false)
			{
				xmlcontrol.LoadXmlParseString("<context xmlns=\"" + xmlcontrol.SchemaNameSpace + "\"/>");
			}

			return xmlcontrol;
		}

		public object CreateContentViewer_customEntry(XElement selectedelement)
		{
			XAttribute id = selectedelement.Attribute("id");
			string id_fullpath = id != null ? System.IO.Path.GetFullPath(id.Value) : null;

			XAttribute type = selectedelement.Attribute("type");
			string type_tolower = type != null ? type.Value.ToLowerInvariant() : null;

			switch (type_tolower)
			{
				case "picture":
					Image image = new Image();
					image.SnapsToDevicePixels = true;

					try
					{
						BitmapImage bmp = new BitmapImage(new Uri(id_fullpath));
						image.Source = bmp;

						return image;
					}
					catch (Exception e)
					{
						return e.Message;
					}

				default:
					return "Unterstützte Werte für \"type\": picture";
			}
		}

		private void AddFile_bin(string filename)
		{
			XElement signalentry =
				new XElement(
					"{http://www.unisens.org/unisens2.0}signalEntry",
					new XAttribute("id", filename),
					new XElement("{http://www.unisens.org/unisens2.0}binFileFormat", new XAttribute("endianess", "LITTLE")));

			Xdocument.Root.Add(signalentry);
		}

		private void AddFile_csv(string filename)
		{
			XElement evententry =
				new XElement(
					"{http://www.unisens.org/unisens2.0}eventEntry",
					new XAttribute("id", filename),
					new XElement("{http://www.unisens.org/unisens2.0}csvFileFormat", new XAttribute("decimalSeparator", "."), new XAttribute("separator", ";")));

			Xdocument.Root.Add(evententry);
		}

		private void AddFile_xml(string filename, string filepath)
		{
			switch (filename.ToLowerInvariant())
			{
				case "context.xml":
					Xdocument.Root.Add(new XElement("{http://www.unisens.org/unisens2.0}context"));
					break;
				case "unisens.xml":
					this.Load(filepath);
					break;
				default:
					// custom, signal, event, value?....
					break;
			}
		}

		private void AddFile_picture(string filename)
		{
			XElement customentry =
				new XElement(
					"{http://www.unisens.org/unisens2.0}customEntry",
					new XAttribute("id", filename),
					new XAttribute("type", "picture"),
					new XElement("{http://www.unisens.org/unisens2.0}customFileFormat", new XAttribute("fileFormatName", "custom")));

			Xdocument.Root.Add(customentry);
		}
	}
}
