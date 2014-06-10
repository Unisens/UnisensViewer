using System.Collections.ObjectModel;
using UnisensViewerLibrary;

namespace UnisensViewer
{
	public class ExecPluginCommandParameter
	{
		public readonly IDspPlugin1							Plugin;
		////public readonly IEnumerable<XElement>	selectedsignals;
		public readonly ObservableCollection<RenderSlice>	Stack;
		public readonly double								TimeCursor;
		public readonly string								Parameter;

		public ExecPluginCommandParameter(IDspPlugin1 plugin, ObservableCollection<RenderSlice> stack, double time_cursor, string parameter)
		{
			this.Plugin = plugin;
			this.Stack = stack;
			////this.selectedsignals = selectedsignals;
			this.TimeCursor = time_cursor;
			this.Parameter = parameter;
		}
	}
}
