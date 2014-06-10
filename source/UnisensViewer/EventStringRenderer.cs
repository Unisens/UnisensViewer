using System;
using System.Collections;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using UnisensViewerLibrary;

namespace UnisensViewer
{
	public class EventStringRenderer : Renderer
	{
		//private const int DEFAULT_IMAGEWIDTH = 128;

		private EventStringData eventdata;
		private double samplespersec;
		public int imageheight;
        public int imagewidth;

		public Int32Rect dirtyrect;

		private Hashtable geometries;
		private Typeface typeface;

		public EventStringRenderer(XElement evententry, double guisignaldisplaywidth, int imagewidth)
			: base(evententry)
		{
			this.geometries = new Hashtable();
			this.typeface = new Typeface(SystemFonts.MessageFontFamily, SystemFonts.MessageFontStyle, SystemFonts.MessageFontWeight, FontStretches.Normal);

			this.imageheight = (int)guisignaldisplaywidth;
            this.imagewidth = imagewidth;
			this.dirtyrect = new Int32Rect(0, 0, this.imagewidth, this.imageheight);

			this.samplespersec = EventEntry.GetSampleRate(evententry);

			this.ReOpen();
		}

		public override int Channels
		{
			get { return 1; }
		}

		public override double TimeMax
		{
			get
			{
				if (this.eventdata.timestamps.Length > 0)
				{
					return (double)this.eventdata.timestamps[this.eventdata.timestamps.Length - 1] / this.samplespersec;
				}
				else
				{
					return 0.0;
				}
			}
		}

		public override void ReOpen()
		{
			this.eventdata = new EventStringData(SevEntry);		
		}

		public override void Close()
		{
			this.eventdata = null;
		}

        public override RenderSlice CreateRenderSlice(int channelnum)
		{
            return new EventRenderSlice(this, channelnum, EventEntry.GetId(SevEntry), this.imagewidth, this.imageheight, "event", SevEntry);
		}

		public override void Render(double time, double timestretch, XElement channel)
		{
			((EventRenderSlice)RenderSlices[0]).WBmp.Lock();

			double samplesperpixel = this.samplespersec * timestretch / this.imageheight;

			double s = time * this.samplespersec;		// (kontinuierliche) nummer des samples
			double peakend = s + samplesperpixel;

			int i = this.eventdata.Search((uint)Math.Ceiling(s));		// erster event im sichtbaren bereich
			int l = this.eventdata.timestamps.Length;

			int		a = 0;

			((EventRenderSlice)RenderSlices[0]).Clear();

			if (i != -1)	
			{
				// events gefunden
				while (i < l && a < this.imageheight)
				{
                    // erstmal bis zum event vorspulen
					while ((int)peakend < this.eventdata.timestamps[i] && a < this.imageheight)
					{
						peakend += samplesperpixel;
						++a;
					}

					if (a < this.imageheight)
                    {
                        // nur den nächsten event [i] für die ausgabe nehmen
						((EventRenderSlice)this.RenderSlices[0]).Stem(a, 1.0f);

						if (this.eventdata.strings[i] != null)
						{
							((EventRenderSlice)this.RenderSlices[0]).Print(a, this.GetStringGeometry(this.eventdata.strings[i]));
						}

                        // alle andern, die auch auf die gleiche spalte fallen überspringen
                        // (die linie wurde schon gemalt, braucht nicht nochmal gemalt werden, = kompression)
                        int peakend_int = (int)peakend;
                        while (i < l && this.eventdata.timestamps[i] <= peakend_int)
                        {
                            ++i;
                        }

                        peakend += samplesperpixel;
                        ++a;
                    }
				}
			}

            if (((RasterRenderSlice)RenderSlices[0]).WBmp.PixelWidth != this.dirtyrect.Width)
            {
                this.dirtyrect.Width = ((RasterRenderSlice)RenderSlices[0]).WBmp.PixelWidth;
            }
			((EventRenderSlice)RenderSlices[0]).WBmp.AddDirtyRect(this.dirtyrect);
			((EventRenderSlice)RenderSlices[0]).WBmp.Unlock();
		}

		public override void UpdateZoomInfo(double time, double timestretch)
		{
		}

		public override SampleInfo GetSampleInfo(int channel, double time, double time_end)
		{
			// timestamp
			uint ts = (uint)Math.Floor(time * this.samplespersec);
			uint tsEnd = (uint)Math.Floor(time_end * this.samplespersec);

			int i = this.eventdata.Search(ts);

			if (i != -1)
			{
				if (this.eventdata.timestamps[i] == ts || this.eventdata.timestamps[i] <= tsEnd)
				{
					return new SampleInfoEventString("Typ: " + this.eventdata.strings[i] + "\nKommentar: " + this.eventdata.comments[i], time, time_end, ts, tsEnd);
				}
				else if (i > 0)
				{
					return new SampleInfoEventString("Typ: " + this.eventdata.strings[i - 1] + "\nKommentar: " + this.eventdata.comments[i - 1], time, time_end, ts, tsEnd);
				}
				else
				{
					return new SampleInfoEventString(null, time, time_end, ts, tsEnd);
				}
			}

			return null;
		}

		private Geometry GetStringGeometry(string s)
		{
			Geometry g;
			int hash = s.GetHashCode();

			g = (Geometry)this.geometries[hash];

			if (g != null)
			{
				return g;
			}
			else
			{
				// geometrie für diesen text erzeugen
				FormattedText ft = new FormattedText(s, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, this.typeface, SystemFonts.MessageFontSize, Brushes.White);

				g = ft.BuildGeometry(new Point());
				g.Freeze();

				this.geometries[hash] = g;
				return g;
			}
		}

		#region old_code
		/*
		public override void ZoomInto(double time, double timestretch)
		{
			//renderslice.ZoomInto(0.0f, 1.0f);
			((EventRenderSlice)renderslices[0]).Scale = (float)DEFAULT_IMAGEWIDTH / 10.0f;
			((EventRenderSlice)renderslices[0]).Offset = -1.0f;

			Render(time, timestretch);
		}
		*/
		#endregion
	}
}
