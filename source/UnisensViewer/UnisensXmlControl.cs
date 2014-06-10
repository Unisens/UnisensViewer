using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace UnisensViewer
{
    public class UnisensXmlControl : XmlControl
	{
		public UnisensXmlControl()
		{
			fileManager = UnisensXmlFileManager.CurrentUnisensInstance;
			fileManager.FileLoaded += new UnisensXmlFileManager.FileEventHandler(fileManager_FileLoaded);
			
			// treeview.Height = 250;
			button_save.Visibility = Visibility.Collapsed;
			expander_content.Header = "Unisens Content";

			treeview.AllowDrop = true;
			treeview.Drop += new DragEventHandler(this.Treeview_Drop);

			treeview.ItemTemplate = null;
			treeview.ItemTemplateSelector = new UnisensXmlDataTemplateSelector();
		}

        #region Drag & Drop
        
		public void DropDirectory(string path)
		{
			string[] files = System.IO.Directory.GetFiles(path);

			// schauen ob das ein unisens datensatz ist.
			// falls eine unisens.xml vorhanden, diese laden
			if (files.Contains("unisens.xml", new FileNameEqualityComparer()))
			{
				fileManager.Load(System.IO.Path.Combine(path, "unisens.xml"));
			}
			else
			{
				foreach (string f in files)
				{
					this.DropFile(f);
				}
			}
		}

		public void DropFile(string filepath)
		{
			fileManager.AddFile(filepath);
		}

		protected override object CreateContentViewer(XElement selectedelement)
		{
			switch (selectedelement.Name.LocalName)
			{
				case "signalEntry":
					// vielleicht ne kleine vorschau anzeigen?...
					return null;

				case "customEntry":
					return fileManager.CreateContentViewer_customEntry(selectedelement);

				case "context":
					return fileManager.CreateContentViewer_context(selectedelement);

				default:
					return null;
			}
		}

		private void Treeview_Drop(object sender, DragEventArgs e)
		{
			// This is for dropped file or directory links.
			string[] paths = e.Data.GetData(DataFormats.FileDrop) as string[];

			// This is for dropped file or directory names.
			if (paths == null)
			{
                String text = e.Data.GetData(DataFormats.Text) as string;
                if (text != null)
                {
                    paths = text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                }
			}

            if (paths != null)
            {
                foreach (string p in paths)
                {
                    if (System.IO.Directory.Exists(p))
                    {
                        this.DropDirectory(p);
                        break;
                    }
                    else if (System.IO.File.Exists(p))
                    {
                        this.DropFile(p);
                    }
                }
            }
		}
        #endregion       
	}
}
