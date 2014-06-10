using System.Windows;
using System.Windows.Controls;

namespace UnisensViewer
{
	public partial class XmlEditAttributeControl : UserControl
	{
		public XmlEditAttributeControl()
		{
			InitializeComponent();
	
			this.Loaded += new RoutedEventHandler(this.UserControl1_Loaded);
		}

		private void UserControl1_Loaded(object sender, RoutedEventArgs e)
		{
			XmlDocumentSchemaAttribute dsa = (XmlDocumentSchemaAttribute)DataContext;

			this.textbox_attribute.Text = dsa.Value;
		
			// textbox_attribute.TextChanged
			this.textbox_attribute.LostFocus += new RoutedEventHandler(this.Textbox_attribute_LostFocus);
		}

		private void Textbox_attribute_LostFocus(object sender, RoutedEventArgs args)
		{
			XmlDocumentSchemaAttribute dsa = (XmlDocumentSchemaAttribute)DataContext;

			dsa.Value = textbox_attribute.Text;
		}

		private void Button_deleteattribute_OnClick(object sender, RoutedEventArgs args)
		{
			XmlDocumentSchemaAttribute dsa = (XmlDocumentSchemaAttribute)DataContext;

			dsa.Remove();
			textbox_attribute.Clear();
		}
	}
}