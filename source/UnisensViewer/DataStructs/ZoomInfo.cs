using System;

namespace UnisensViewer
{
    public struct ZoomInfo
	{
		public float PhysicalMin;
		public float PhysicalMax;

        public ZoomInfo(float physicalMin, float physicalMax)
		{
			this.PhysicalMin = physicalMin;
			this.PhysicalMax = physicalMax;
		}
	}
}