using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using UnisensViewerLibrary;

namespace UnisensViewer
{
	public partial class DialogPlugin : Window
	{
		private IEnumerable<CheckListXElement>	signals;
		private IEnumerable<XElement> groups;
		private IEnumerable<XElement> selectedsignals;

		public DialogPlugin()
		{
			InitializeComponent();

			Loaded += new RoutedEventHandler(this.DialogPlugin_Loaded);
		}

		public IDspPlugin1 DspPlugin
		{
			get;
			set;
		}

		public XDocument UnisensXml
		{
			get;
			set;
		}

		public IEnumerable<XElement> SelectedSignals
		{
			get { return this.selectedsignals; }
		}

		private void DialogPlugin_Loaded(object sender, RoutedEventArgs e)
		{
			textblock_plugin.Text = this.DspPlugin.Name;
			textblock_description.Text = this.DspPlugin.Description;

			IEnumerable<CheckListXElement> s = from XElement xe in this.UnisensXml.Root.Elements()
												where xe.Name.LocalName == "signalEntry" || xe.Name.LocalName == "eventEntry" || xe.Name.LocalName == "valuesEntry"
												select new CheckListXElement(xe);

			IEnumerable<XElement> g = from XElement xe in this.UnisensXml.Root.Elements()
									  where xe.Name.LocalName == "group"
									  select xe;

			this.signals = s.ToList();
			this.groups = g.ToList();

			listbox_signals.ItemsSource = this.signals;
			listbox_groups.ItemsSource = this.groups;
		}

		private void Button_Click_select(object sender, RoutedEventArgs e)
		{
			Button b = (Button)sender;
			XElement xegroup = (XElement)b.DataContext;
			IEnumerable<string> grouprefs = from xe in xegroup.Elements()
											select xe.Attribute("ref").Value;

			foreach (CheckListXElement clxe in this.signals)
			{
				if (grouprefs.Contains(clxe.Xe.Attribute("id").Value))
				{
					clxe.IsChecked = true;
				}
			}
		}

		private void Button_Click_deselect(object sender, RoutedEventArgs e)
		{
			Button b = (Button)sender;
			XElement xegroup = (XElement)b.DataContext;
			IEnumerable<string> grouprefs = from xe in xegroup.Elements()
											select xe.Attribute("ref").Value;

			foreach (CheckListXElement c in this.signals)
			{
				if (grouprefs.Contains(c.Xe.Attribute("id").Value))
				{
					c.IsChecked = false;
				}
			}
		}

		private void Button_Click_select_all(object sender, RoutedEventArgs e)
		{
			foreach (CheckListXElement c in this.signals)
			{
				c.IsChecked = true;
			}
		}

		private void Button_Click_deselect_all(object sender, RoutedEventArgs e)
		{
			foreach (CheckListXElement c in this.signals)
			{
				c.IsChecked = false;
			}
		}

		private void Button_Click_Ok(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

			this.selectedsignals = from s in this.signals
                              where s.IsChecked
                              select s.Xe;

            Close();
        }

        private void Button_Click_Help(object sender, RoutedEventArgs rea)
        {
			string helpPath = this.DspPlugin.Help;

            if (helpPath.IndexOf("http://") != 0)
            {
				helpPath = Folders.UnisensViewer + @"\Plugins\" + this.DspPlugin.Help;
            }

            System.Diagnostics.Process.Start(helpPath);
        }
	}
}