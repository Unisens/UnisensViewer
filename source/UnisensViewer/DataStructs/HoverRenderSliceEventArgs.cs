using System.Windows;

namespace UnisensViewer
{
	public class HoverRenderSliceEventArgs : RoutedEventArgs
	{
		private RenderSlice renderslice;

		public HoverRenderSliceEventArgs(RenderSlice renderslice)
			: base(SignalViewerControl.HoverRenderSliceEvent)
		{
			this.renderslice = renderslice;
		}

		public RenderSlice RenderSlice
		{
			get { return this.renderslice; }
		}
	}
}
