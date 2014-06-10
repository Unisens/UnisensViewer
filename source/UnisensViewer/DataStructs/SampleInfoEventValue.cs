using System;

namespace UnisensViewer
{
	public class SampleInfoEventValue : SampleInfo
	{
		private double samplemin;
		private double samplemax;
		private EventValueRenderer eventvaluerenderer;
        private bool rounding = WindowMain.Rounding;

		public SampleInfoEventValue(EventValueRenderer eventvaluerenderer, double samplemin, double samplemax, double time, double timeend, long sampleoffs, long sampleoffsend)
			: base(time, timeend, sampleoffs, sampleoffsend)
		{
			this.samplemin = samplemin;
			this.samplemax = samplemax;
			this.eventvaluerenderer = eventvaluerenderer;
		}

		public double SampleMin
		{
			get { return this.samplemin; }
		}

		public double SampleMax
		{
			get { return this.samplemax; }
		}

		public double PhysicalMin
		{
			get { return (this.samplemin - this.eventvaluerenderer.Baseline) * this.eventvaluerenderer.Lsbvalue; }
		}

        public string PhysicalMinString
        {
            get
            {
                if (this.rounding)
                {
                    return String.Format("{0,-10:0.0000}", this.PhysicalMin);
                }
                else
                {
                    return this.PhysicalMin.ToString();
                }
            }
        }

		public double PhysicalMax
		{
			get { return (this.samplemax - this.eventvaluerenderer.Baseline) * this.eventvaluerenderer.Lsbvalue; }
		}

        public string PhysicalMaxString
        {
            get
            {
				if (this.rounding)
                {
					return String.Format("{0,-10:0.0000}", this.PhysicalMax);
                }
				else
				{
					return this.PhysicalMax.ToString();
				}
            }
        }

		public string Unit
		{
			get { return this.eventvaluerenderer.Unit; }
		}
	}
}
