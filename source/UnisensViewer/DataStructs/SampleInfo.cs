using System;

namespace UnisensViewer
{
	public abstract class SampleInfo
	{
		private double time;
		private double timeend;
		private long sampleoffs;
		private long sampleoffsend;

		public SampleInfo(double time, double timeend, long sampleoffs, long sampleoffsend)
		{
			this.time = time;
			this.timeend = timeend;
			this.sampleoffs = sampleoffs;
			this.sampleoffsend = sampleoffsend;
		}

		public double Time
		{
			get { return this.time; }
		}

		public double TimeEnd
		{
			get { return this.timeend; }
		}

		public long SampleOffs
		{
			get { return this.sampleoffs; }
		}

		public long SampleOffsEnd
		{
			get { return this.sampleoffsend; }
		}

        public string TimeStampStart
        {
            get
            {
                string dateStr = null;
                if (UnisensXmlFileManager.TimeStampStart != null)
                {
                    DateTime timestampstart = Convert.ToDateTime(UnisensXmlFileManager.TimeStampStart);
                    DateTime dt = timestampstart.AddSeconds(Time);
                    string a = String.Format("{0:mm}", dt);
                    string b = String.Format("{0:ss}", dt);
                    string c = String.Format("{0:fff}", dt);
                    dateStr = dt.Hour + ":" + a + ":" + b + "." + c + " Uhr";
                }
                else
                {
                    dateStr = "No entry";
                }

                return dateStr;
            }
        }

        public string DateStampStart
        {
            get
            {
                string dateStr = null;
                if (UnisensXmlFileManager.TimeStampStart != null)
                {
                    DateTime timestampstart = Convert.ToDateTime(UnisensXmlFileManager.TimeStampStart);
                    DateTime dt = timestampstart.AddSeconds(Time);
                    dateStr = String.Format("{0:dddd, dd. MMMM yyyy }", dt);
                }
                else
                {
                    dateStr = "No entry";
                }

                return dateStr;
            }
        }
	}
}

