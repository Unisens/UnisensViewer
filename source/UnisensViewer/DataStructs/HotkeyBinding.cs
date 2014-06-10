using System.Collections.Generic;
using System.Windows.Input;
using System.Xml.Linq;
using UnisensViewerLibrary;

namespace UnisensViewer
{
	public class PluginHotkeyBinding
	{
        public PluginHotkeyBinding(IDspPlugin1 plugin, SelectedSignals signals, string parm)
        {
            this.Plugin = plugin;
            this.Signals = signals;
            this.Parameter = parm;
        }

        public enum SelectedSignals
        {
            AllOpenFiles,
            StackFiles,
            StackChannels,
            StackSelectedFiles,
            StackSelectedChannels,
            AllSignalEntries,
            AllEventEntries,
            AllValuesEntries,
            Dialog,
            All
        }

        public IDspPlugin1 Plugin { get; set; }

        public SelectedSignals Signals { get; set; }

        public string Parameter { get; set; }
	}
}
