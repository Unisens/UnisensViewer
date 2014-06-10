using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace UnisensViewer
{
	public class UnisensXmlDataTemplateSelector : DataTemplateSelector
	{
		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			XElement xe = (XElement)item;
            Application app = Application.Current;

			switch (xe.Name.LocalName)
			{
				case "valuesEntry":
				case "eventEntry":
				case "signalEntry": return app.FindResource("unisensxml_id") as DataTemplate;
				case "channel": return app.FindResource("unisensxml_channel") as DataTemplate;
				case "context": return app.FindResource("unisensxml_context") as DataTemplate;
				case "customEntry": return app.FindResource("unisensxml_customEntry") as DataTemplate;
				case "group": return app.FindResource("unisensxml_group") as DataTemplate;
				case "groupEntry": return app.FindResource("unisensxml_groupEntry") as DataTemplate;
                case "customAttribute": return app.FindResource("unisensxml_customAttribute") as DataTemplate;
                case "unisens": return app.FindResource("unisensxml_root") as DataTemplate;
                default: return app.FindResource("unisensxml_default") as DataTemplate;
			}
		}
	}
}
