using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Xml.Linq;

/*
 * Dieses Control dient zum Editieren von Mixed-Text-Content eines Elements.
 * Falls das Element schon (mehrere) XText-Childs besitzt, werden sie in
 * einem ItemsControl zum Editieren bereitgestellt.
 * 
 * Falls das Element noch kein XText Child besitzt, wird eine TextBox
 * bereitgestellt, in dem ein neues XText child erstellt werden kann.
 * Dieses wird jedoch nur bei Bedarf erzeugt und zum XML-Baum hinzugefügt.
 * (Bleibt die TextBox leer, wird der XML-Baum nicht verändert).
 * 
 */
namespace UnisensViewer
{
	public partial class XmlEditMixedTextControl : UserControl
	{
		private XText			textboxxnode;

		public XmlEditMixedTextControl()
		{
			InitializeComponent();

			this.Loaded += new RoutedEventHandler(this.XmlEditMixedTextControl_Loaded);
		}

		private void XmlEditMixedTextControl_Loaded(object sender, RoutedEventArgs e)
		{
			XElement xelement = (XElement)DataContext;

			IEnumerable<XText> textnodes = from n in xelement.Nodes()
										   where n is XText
										   select n as XText;

			List<XText> textnodeslist = textnodes.ToList();

			if (textnodeslist.Count >= 2)		
			{
				// Mixed text content
				ItemsControl itemscontrol = new ItemsControl();
				itemscontrol.ItemsSource = textnodeslist;
				contentpresenter.Content = itemscontrol;
			}
			else								
			{
				// "normaler" text content
				TextBox textbox = new TextBox();

				if (textnodeslist.Count == 1)
				{
					this.textboxxnode = textnodeslist.First();
					textbox.Text = this.textboxxnode.Value;
				}
				else
				{
					this.textboxxnode = null;
				}

				textbox.TextWrapping = TextWrapping.Wrap;
				textbox.ToolTip = "Text content of this element";
				contentpresenter.Content = textbox;
				textbox.LostFocus += new RoutedEventHandler(this.Textbox_LostFocus);
			}
		}

		private void Textbox_LostFocus(object sender, RoutedEventArgs e)
		{
			TextBox textbox = sender as TextBox;

			// evtl einen neuen XText erstellen oder vom parent löschen
			if (textbox.Text.Length > 0)
			{
				if (this.textboxxnode == null)
				{
					XElement xelement = (XElement)DataContext;
					this.textboxxnode = new XText(textbox.Text);
					xelement.Add(this.textboxxnode);
				}
				else
				{
					this.textboxxnode.Value = textbox.Text;
				}
			}
			else
			{
				if (this.textboxxnode != null)
				{
					this.textboxxnode.Remove();
					this.textboxxnode = null;
				}
			}
		}
	}
}
