using System;

namespace UnisensViewer
{
	public class SampleInfoEventString : SampleInfo
	{
		private string description;

		public SampleInfoEventString(string description, double time, double timeend, long sampleoffs, long sampleoffsend)
			: base(time, timeend, sampleoffs, sampleoffsend)
		{
			this.description = description;
		}

		public string Description
		{
			get { return this.description; }
		}
	}
}
