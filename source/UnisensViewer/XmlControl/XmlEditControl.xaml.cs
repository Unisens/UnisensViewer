using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Xml.Linq;
using System.Xml.Schema;

namespace UnisensViewer
{
	public partial class XmlEditControl : UserControl
	{
		private ObservableCollection<XmlDocumentSchemaAttribute>	attributes;
		private XElement											xelement;

		public XmlEditControl(XElement xelement)
		{
			InitializeComponent();

			this.xelement = xelement;
			DataContext = xelement;

			this.attributes = this.GetPossibleAttributes(xelement);

			this.textblock_currentelement.IsEnabled = xelement.Parent != null;	// das root element nicht veränderbar machen
			this.textblock_currentelement.ItemsSource = this.GetPossibleChilds(xelement.Parent);
			this.textblock_currentelement.Text = xelement.Name.LocalName;

			this.combobox_childelement.ItemsSource = this.GetPossibleChilds(xelement);
			this.itemscontrol_attributes.ItemsSource = this.attributes;
		}

		private ObservableCollection<XmlDocumentSchemaAttribute> GetPossibleAttributes(XElement xe)
		{
			ObservableCollection<XmlDocumentSchemaAttribute> a = new ObservableCollection<XmlDocumentSchemaAttribute>();

			IXmlSchemaInfo ixsi = xe.GetSchemaInfo();

			if (ixsi != null)
			{
				XmlSchemaType xst = ixsi.SchemaType;

				if (xst is XmlSchemaComplexType)
				{
					XmlSchemaComplexType xsct = (XmlSchemaComplexType)xst;

					// alle im schema spezifizierten attribute zum editieren hinzufügen
					foreach (XmlSchemaAttribute xsa in xsct.AttributeUses.Values)
					{
						a.Add(new XmlDocumentSchemaAttribute(a, xe, xsa));
					}

					// alle attribute im dokument, die nicht im schema spezifiziert sind hinzufügen
					foreach (XAttribute xa in xe.Attributes())
					{
						IXmlSchemaInfo i = xa.GetSchemaInfo();

						if (i == null || i.SchemaAttribute == null)
						{
							if (xa.Name.Namespace == string.Empty && xa.Name.LocalName != "xmlns")
							{
								a.Add(new XmlDocumentSchemaAttribute(a, xe, xa.Name));
							}
						}
					}
				}
			}
			else	
			{	// kein schema geladen, alles aus dem dokument
				foreach (XAttribute xa in xe.Attributes())
				{
					if (xa.Name.Namespace == string.Empty && xa.Name.LocalName != "xmlns")
					{
						a.Add(new XmlDocumentSchemaAttribute(a, xe, xa.Name));
					}
				}
			}

			return a;
		}

		private	List<string> GetPossibleChilds(XElement xe)
		{
			List<string> l = new List<string>();

			if (xe != null)
			{
				IXmlSchemaInfo ixsi = xe.GetSchemaInfo();

				if (ixsi != null)
				{
					XmlSchemaType xst = ixsi.SchemaType;

					if (xst is XmlSchemaComplexType)
					{
						this.WalkSchemaParticle(l, ((XmlSchemaComplexType)xst).ContentTypeParticle);
					}
				}
			}

			return l;
		}

		private void WalkSchemaParticle(List<string> list, XmlSchemaParticle particle)
		{
			if (particle is XmlSchemaElement)
			{
				list.Add(((XmlSchemaElement)particle).Name);
			}
			else if (particle is XmlSchemaGroupBase)
			{
				XmlSchemaGroupBase xsgb = particle as XmlSchemaGroupBase;

				foreach (XmlSchemaParticle p in xsgb.Items)
				{
					this.WalkSchemaParticle(list, p);
				}
			}
		}
		
		private void Button_addattribute_OnClick(object sender, RoutedEventArgs args)
		{
			if (textbox_newattribute.Text.Length > 0)
			{
				string attrname = textbox_newattribute.Text;
				
				// erstmal schaun, ob das nicht schon in der oberfläche da ist
				foreach (XmlDocumentSchemaAttribute dsa in this.attributes)
				{
					if (dsa.AttributeName == attrname)
					{
						textbox_newattribute.Clear();
						return;
					}
				}

				try
				{
					// probieren, ob attrname gültig ist
					XAttribute xa = new XAttribute(attrname, string.Empty);
					this.attributes.Add(new XmlDocumentSchemaAttribute(this.attributes, this.xelement, attrname));
				}
				catch (Exception e)
				{
					MessageBox.Show(e.Message, "Attribut Name", MessageBoxButton.OK, MessageBoxImage.Error);
				}

				textbox_newattribute.Clear();
			}
		}

		private void Button_addelement_OnClick(object sender, RoutedEventArgs args)
		{
			if (combobox_childelement.Text.Length > 0)
			{
				try
				{
					this.xelement.Add(new XElement(this.xelement.Name.Namespace + combobox_childelement.Text));
				}
				catch (Exception e)
				{
					MessageBox.Show(e.Message, "Element", MessageBoxButton.OK, MessageBoxImage.Error);
				}

				this.combobox_childelement.Text = string.Empty;
			}
		}

		private void Button_deleteelement_OnClick(object sender, RoutedEventArgs args)
		{
			if (this.xelement.Parent != null)
			{
				XAttribute id = this.xelement.Attribute("id");

				if (id != null)
				{
					string filename = id.Value;

					// Configure the message box to be displayed
					string messageBoxText = "Möchten Sie die Datei " + filename + " auch auf der Festplatte löschen?";
					string caption = "Eintrag " + filename + " löschen";
					MessageBoxButton button = MessageBoxButton.YesNoCancel;
					MessageBoxImage icon = MessageBoxImage.Question;

					// Display message box
					MessageBoxResult result = MessageBox.Show(messageBoxText, caption, button, icon);

					// Process message box results
					switch (result)
					{
						case MessageBoxResult.Yes:
							// Datei auf der festplatte löschen
							string path = System.IO.Path.Combine(Environment.CurrentDirectory, filename);
							File.Delete(path);
							break;
						case MessageBoxResult.No:
							// Nur im XML löschen
							break;
						case MessageBoxResult.Cancel:
							// Abbrechen
							return;
					}
				}

				// Aus dem XML entfernen
				this.xelement.Remove();
			}
		}

		private void Textblock_currentelement_LostFocus(object sender, RoutedEventArgs args)
		{
			if (this.textblock_currentelement.Text != this.xelement.Name.LocalName)
			{
				// also das hier geht zwar:

				////xelement.Name = xelement.Name.Namespace + textblock_currentelement.Text;

				// aber der treeview wird nicht aktualisiert. laut doku ist xelement.Name nur lesbar,
				// die implementierung ist aber sowohl lesbar als auch beschreibbar.
				// vermutlich ist da im framework noch eine baustelle.
				try
				{	// kann sein, dass der name ungültige zeichen enthält...
					XElement newxe = new XElement(this.xelement.Name.Namespace + this.textblock_currentelement.Text, this.xelement.Nodes());
					newxe.Add(this.xelement.Attributes());
					this.xelement.ReplaceWith(newxe);
				}
				catch (Exception e)
				{
					this.textblock_currentelement.Text = this.xelement.Name.LocalName;
					MessageBox.Show(e.Message, "Element Name", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}
	}
}
