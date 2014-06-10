using System;

namespace UnisensViewer
{
	public class SampleInfoStream : SampleInfo
	{
		private double samplemin;
		private double samplemax;
		private StreamRenderer streamrenderer;
		private bool rounding = WindowMain.Rounding;

		public SampleInfoStream(StreamRenderer streamrenderer, double samplemin, double samplemax, double time, double timeend, long sampleoffs, long sampleoffsend)
			: base(time, timeend, sampleoffs, sampleoffsend)
		{
			this.samplemin = samplemin;
			this.samplemax = samplemax;
			this.streamrenderer = streamrenderer;
		}
		
		public double SampleMin
		{
			get { return this.samplemin; }
		}

		public double SampleMax
		{
			get { return this.samplemax; }
		}

        // wenn die Funtion "Physical value rounding" aktiv ist, 
        // dann wird der Physikalischer Wert abgerundet mit Methode String.Math("{0,10:0.0000})
		public double PhysicalMin
		{
			get { return (this.samplemin - this.streamrenderer.Baseline) * this.streamrenderer.Lsbvalue; }
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
			get { return (this.samplemax - this.streamrenderer.Baseline) * this.streamrenderer.Lsbvalue; }
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
			get { return this.streamrenderer.Unit; }
		}
	}
}
