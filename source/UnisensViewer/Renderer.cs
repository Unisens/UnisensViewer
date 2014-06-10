using System;
using System.Xml.Linq;

namespace UnisensViewer
{
	public abstract class Renderer
	{
		private EventHandler					rskillhandler;

		public Renderer(XElement sev_entry)
		{
			this.SevEntry = sev_entry;
			this.rskillhandler = new EventHandler(this.RendersliceKill);
		}

		public event EventHandler Kill;

		public abstract int				Channels { get; }

		public abstract double			TimeMax { get; }

		public XElement SevEntry { get; set; }	
	
		public RenderSlice[] RenderSlices { get; set; }

		public abstract void			Render(double time, double timestretch, XElement channel);

		public abstract void			UpdateZoomInfo(double time, double timestretch);

		public abstract void			Close();

		public abstract void			ReOpen();
		
		public abstract RenderSlice CreateRenderSlice(int channelnum);
		
		public abstract SampleInfo GetSampleInfo(int channel, double time, double time_end);

		public RenderSlice GetRenderSlice(int channelnum)
		{
			if (channelnum >= this.Channels)
			{
				throw new Exception("Ungültiger Kanal. Eventuell wurde die unisens.xml modifiziert, während bereits ein Renderer existierte. Schließe alle Ansichten der Datei, um den alten Renderer zu zerstören und versuchs nochmal.");
			}

			if (this.RenderSlices == null)
			{
				this.RenderSlices = new RenderSlice[this.Channels];
			}

			if (this.RenderSlices[channelnum] == null)
			{
				this.RenderSlices[channelnum] = this.CreateRenderSlice(channelnum);
				this.RenderSlices[channelnum].Kill += this.rskillhandler;
			}

			return this.RenderSlices[channelnum];
		}

		public void RaiseKill()
		{
			for (int a = 0; a < this.Channels; ++a)
			{
				if (this.RenderSlices[a] != null)
				{
					this.RenderSlices[a].RaiseKill();
				}
			}
		}

		private void RendersliceKill(object sender, EventArgs e)
		{
			RenderSlice rs = (RenderSlice)sender;

			rs.Kill -= this.rskillhandler;

			int numslices = 0;

			// referenz auf das renderslice löschen
			// falls überhaupt keine renderslices mehr aktiv, dann den ganzen renderer löschen
			for (int a = 0, b = this.Channels; a < b; ++a)
			{
				if (this.RenderSlices[a] == rs)
				{
					this.RenderSlices[a] = null;
				}

				if (this.RenderSlices[a] != null)
				{
					++numslices;
				}
			}

			if (numslices == 0)
			{
				this.Kill(this, null);
				// Free Memory
				this.Close();
			}
		}
	}
}
