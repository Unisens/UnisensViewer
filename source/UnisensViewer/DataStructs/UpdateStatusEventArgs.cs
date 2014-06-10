using System.Windows;

namespace UnisensViewer
{
	public class UpdateStatusEventArgs : RoutedEventArgs
	{
		public readonly SampleInfo SampleInfo;

		public UpdateStatusEventArgs(SampleInfo sampleInfo)
			: base(WindowMain.UpdateStatusEvent)
		{
			this.SampleInfo = sampleInfo;
		}
	}
}
