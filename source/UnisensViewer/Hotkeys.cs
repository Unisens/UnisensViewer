using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Xml.Linq;
using UnisensViewerLibrary;

namespace UnisensViewer
{
	public class Hotkeys
	{
		public Hotkeys(XDocument settings, IEnumerable<IDspPlugin1> plugins)
		{
			this.Bindings = new Hashtable();

			if (settings != null)
			{
				XElement hotkeys = settings.Root.Element("Hotkeys");
				if (hotkeys != null)
				{
					IEnumerable<XElement> bs = hotkeys.Elements("Binding");

					foreach (XElement b in bs)
					{
						this.AddBinding(b, plugins);
					}
				}
			}
		}

		public Hashtable Bindings { get; set; }

		private void AddBinding(XElement binding, IEnumerable<IDspPlugin1> plugins)
		{
			IDspPlugin1 plugin = this.ParsePlugin(binding, plugins);
			ModifierKeys modifiers = this.ParseModifiers(binding);
			Key key = this.ParseKey(binding);
			PluginHotkeyBinding.SelectedSignals signals = this.ParseSelectedSignals(binding);

			XAttribute p = binding.Attribute("Parameter");
			string parameter = p != null ? p.Value : null;

			if (plugin != null && key != 0)
			{
				this.Bindings.Add(new HotkeyHashkey(key, modifiers), new PluginHotkeyBinding(plugin, signals, parameter));
			}
		}

		private ModifierKeys ParseModifiers(XElement binding)
		{
			try
			{
				return (ModifierKeys)System.Enum.Parse(typeof(ModifierKeys), binding.Attribute("Modifiers").Value, true);
			}
			catch
			{
				return 0;
			}
		}

		private Key ParseKey(XElement binding)
		{
			try
			{
				return (Key)System.Enum.Parse(typeof(Key), binding.Attribute("Key").Value, true);
			}
			catch
			{
				return 0;
			}
		}

		private PluginHotkeyBinding.SelectedSignals ParseSelectedSignals(XElement binding)
		{
			try
			{
				return (PluginHotkeyBinding.SelectedSignals)System.Enum.Parse(typeof(PluginHotkeyBinding.SelectedSignals), binding.Attribute("SelectedSignals").Value, true);
			}
			catch
			{
				return PluginHotkeyBinding.SelectedSignals.AllOpenFiles;
			}
		}

		private IDspPlugin1 ParsePlugin(XElement binding, IEnumerable<IDspPlugin1> plugins)
		{
			try
			{
				string name = binding.Attribute("Plugin").Value;

				IEnumerable<IDspPlugin1> i = from p in plugins
											where p.Name == name
											select p;

				return i.First();
			}
			catch
			{
				return null;
			}
		}
	}
}
