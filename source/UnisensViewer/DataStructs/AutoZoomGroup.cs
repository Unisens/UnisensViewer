using System.Collections.Generic;

namespace UnisensViewer
{
    public struct AutoZoomGroup
	{
		public IEnumerable<RenderSlice> RsList;
		public double ScaleFactor;

		public AutoZoomGroup(IEnumerable<RenderSlice> rsList, double scaleFactor)
		{
			this.RsList = rsList;
			this.ScaleFactor = scaleFactor;
		}
	}
}
