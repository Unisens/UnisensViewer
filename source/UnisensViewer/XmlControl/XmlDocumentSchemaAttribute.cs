using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace UnisensViewer
{
	// - ein attribut kann im schema spezifiziert sein, aber nicht im dokument vorkommen (optionales attribut)
	// - ein attribut kann im dokument vorkommen, aber nicht im schema (any-attribute)
	// - sowohl im dokument als auch im schema

	// für die oberfläche wird eine List<DocumentSchemaAttribute> erstellt,
	// auf der dann einzelne XmlEditAttributeControls sitzen
	public class XmlDocumentSchemaAttribute
	{
		private XElement element;
		private XmlSchemaAttribute xsa;
		private ObservableCollection<XmlDocumentSchemaAttribute> parentlist;

		private XName xname;
		private string annotation;
		private string useinfo;

		public XmlDocumentSchemaAttribute(ObservableCollection<XmlDocumentSchemaAttribute> parentlist, XElement element, XName xname)
		{
			this.parentlist = parentlist;
			this.element = element;
			this.xname = xname;
		}

		public XmlDocumentSchemaAttribute(ObservableCollection<XmlDocumentSchemaAttribute> parentlist, XElement element, XmlSchemaAttribute xsa)
		{
			this.parentlist = parentlist;
			this.element = element;
			this.xsa = xsa;
			this.xname = xsa.Name;

			this.annotation = CompileAnnotations(this.xsa);
			this.useinfo = BuildUseInfo(this.xsa);
		}

		public string AttributeName
		{
			get { return this.xname.LocalName; }
		}

		public string Annotation
		{
			get { return this.annotation; }
		}

		public string Value
		{
			get
			{
				XAttribute documentattribute = this.element.Attribute(this.xname);

				if (documentattribute != null)
				{
					return documentattribute.Value;
				}
				else
				{
					return null;
				}
			}

			set
			{
				this.element.SetAttributeValue(this.xname, value.Length != 0 ? value : null);
			}
		}

		public string UseInfo
		{
			get { return this.useinfo; }
		}

		public void Remove()
		{
			this.element.SetAttributeValue(this.xname, null);

			// wenn kein im schema spezifiziertes attribut, dann aus der gui rauslöschen
			if (this.xsa == null)
			{
				this.parentlist.Remove(this);
			}
		}

		private static string CompileAnnotations(XmlSchemaAttribute attribute)
		{
			if (attribute.Annotation != null)
			{
				System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"\s+");
				StringBuilder sb = new StringBuilder(256);

				foreach (XmlSchemaObject o in attribute.Annotation.Items)
				{
					// XmlSchemaAppInfo oder XmlSchemaDocumentation
					if (o is XmlSchemaDocumentation)
					{
						foreach (XmlNode n in ((XmlSchemaDocumentation)o).Markup)
						{
							// falls in den annotations der text mit elementen gemarkupt ist:
							// .OuterXml   gibt den text mit markup zurück
							// .InnerText  gibt nur den konkatenierten text zurück

							// sb.Append(regex.Replace(n.OuterXml, " ").Trim());
							sb.Append(regex.Replace(n.InnerText, " ").Trim());

							sb.Append(" ");
						}

						if (sb.Length > 0)
						{
							sb.Length = sb.Length - 1;
						}
					}

					sb.AppendLine();
				}

				if (sb.Length > 0)
				{
					sb.Length = sb.Length - Environment.NewLine.Length;
				}

				return sb.ToString();
			}
			else
			{
				return null;
			}
		}

		private static string BuildUseInfo(XmlSchemaAttribute xsa)
		{
			StringBuilder sb = new StringBuilder(64);

			switch (xsa.Use)
			{
				case XmlSchemaUse.Prohibited: 
					sb.Append("prohibited"); 
					break;
				case XmlSchemaUse.Required: 
					sb.Append("required"); 
					break;
				case XmlSchemaUse.Optional: 
					sb.Append("optional"); 
					break;
			}

			if (xsa.DefaultValue != null)
			{
				sb.AppendFormat("{0}default={1}", sb.Length > 0 ? ", " : null, xsa.DefaultValue);
			}

			if (xsa.FixedValue != null)
			{
				sb.AppendFormat("{0}fixed={1}", sb.Length > 0 ? ", " : null, xsa.FixedValue);
			}

			return sb.ToString();
		}
	}
}
