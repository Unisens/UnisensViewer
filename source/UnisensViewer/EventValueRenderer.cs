using System;
using System.Windows;
using System.Xml.Linq;
using UnisensViewerClrCppLibrary;
using UnisensViewerLibrary;
using System.Collections;
using System.Windows.Media;

namespace UnisensViewer
{
	public class EventValueRenderer : Renderer
	{
		public readonly float Baseline;
		public readonly float Lsbvalue;
		public readonly string Unit;

		//private const int DEFAULT_IMAGEWIDTH = 128;

		private EventValueData eventdata;
		private double samplespersec;
		public int imageheight;
        public int imagewidth;

		public Int32Rect dirtyrect;

		private int channels;

		private SampleD[] sampledata;
        // for VectorRenderSlices
        private Hashtable geometries;
        private Typeface typeface;

        public EventValueRenderer(XElement valueentry, double guisignaldisplaywidth, int imagewidth)
			: base(valueentry)
		{
			this.Baseline = (float)ValueEntry.GetBaseline(valueentry);
			this.Lsbvalue = (float)ValueEntry.GetLsbValue(valueentry);
			this.Unit = ValueEntry.GetUnit(valueentry);

			this.samplespersec = ValueEntry.GetSampleRate(valueentry);
			this.channels = ValueEntry.GetNumChannels(valueentry);

			this.imageheight = (int)guisignaldisplaywidth;
            this.imagewidth = imagewidth;
            this.dirtyrect = new Int32Rect(0, 0, this.imagewidth, this.imageheight);

			this.sampledata = new SampleD[this.channels];

			this.ReOpen();

            // for VectorRenderSlices
            this.geometries = new Hashtable();
            this.typeface = new Typeface(SystemFonts.MessageFontFamily, SystemFonts.MessageFontStyle, SystemFonts.MessageFontWeight, FontStretches.Normal);
		}

		public override int Channels
		{
			get { return this.channels; }
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
			this.eventdata = new EventValueData(SevEntry);
		}

		public override void Close()
		{
			this.eventdata = null;
		}

		public override RenderSlice CreateRenderSlice(int channelnum)
		{
            return new RasterRenderSlice(this, channelnum, ValueEntry.GetChannelName(SevEntry, channelnum), this.imagewidth, this.imageheight, ValueEntry.GetUnit(SevEntry), ValueEntry.GetChannel(SevEntry, channelnum));
        }

        public override void Render(double time, double timestretch, XElement channel)
        {
            int channelNum = 0;
            int numOfChannels = 0;

            // prüfen, ob eine bestimmte RenderSlice oder alle RenderSlice gerendert werden.
            if (channel != null)
            {
                // wenn nur eine bestimmte RS gerendert werden soll, werden die For-Schleife nur einmal ausgeführt.
                channelNum = MeasurementEntry.GetChannelNum(channel);
                numOfChannels = channelNum + 1;
            }
            else
            {
                // Sonst werden alle Kanäle (alle RenderSlice) durch die Schleife neu gerendert.
                channelNum = 0;
                numOfChannels = this.channels;
            }

            for (int c = channelNum; c < numOfChannels; ++c)
            {
                if (RenderSlices[c] != null)
                {
                    ((RasterRenderSlice)RenderSlices[c]).WBmp.Lock();
                }
            }

            double samplesperpixel = this.samplespersec * timestretch / this.imageheight;
            // (kontinuierliche) nummer des samples
            double s = time * this.samplespersec;
            double peakend = s + samplesperpixel;

            if (RendererManager.SampleAndHold)
            {
                this.RenderSampleAndHold(this.imageheight, samplesperpixel, s, peakend, channelNum, numOfChannels);
            }
            if (RendererManager.Point)
            {
                this.RenderPoint(this.imageheight, samplesperpixel, s, peakend, channelNum, numOfChannels);
            }
            if (RendererManager.Linear)
            {
                this.RenderLinear(this.imageheight, samplesperpixel, s, peakend, channelNum, numOfChannels);
            }

            for (int c = channelNum; c < numOfChannels; ++c)
            {
                if (RenderSlices[c] != null)
                {
                    if (((RasterRenderSlice)RenderSlices[c]).WBmp.PixelWidth != this.dirtyrect.Width)
                    {
                        this.dirtyrect.Width = ((RasterRenderSlice)RenderSlices[c]).WBmp.PixelWidth;
                    }
                    ((RasterRenderSlice)RenderSlices[c]).WBmp.AddDirtyRect(this.dirtyrect);
                    ((RasterRenderSlice)RenderSlices[c]).WBmp.Unlock();
                }
            }
        }

        public void RenderSampleAndHold(int pixelheight, double samplesperpixel, double s, double peakend, int channelNum, int numOfChannels)
		{
			// erster event im sichtbaren bereich
			int i = this.eventdata.Search((uint)Math.Ceiling(s));		
			int		o = i * this.channels;

			int		l = this.eventdata.timestamps.Length;
			int a = 0;

			if (i != -1)	
			{
                // "letzter wert" für unten (**) initialisieren
                if (i >= 1)
                {
                    int oprevious = o - this.channels;

                    for (int c = channelNum; c < numOfChannels; ++c)
                    {
                        this.sampledata[c].min = (this.eventdata.values[oprevious + c] - this.Baseline) * this.Lsbvalue;
                        this.sampledata[c].max = (this.eventdata.values[oprevious + c] - this.Baseline) * this.Lsbvalue;
                        this.sampledata[c].value = (this.eventdata.values[oprevious + c] - this.Baseline) * this.Lsbvalue;

                       //++oprevious;
                    }
                }
                else
                {
                    for (int c = channelNum; c < numOfChannels; ++c)
                    {
                        this.sampledata[c].min = 0.0f;
                        this.sampledata[c].max = 0.0f;
                        this.sampledata[c].value = 0.0f;
                    }
                }

                for (int c = channelNum; c < numOfChannels; ++c)
                {
                    if (RenderSlices[c] != null)
                    {
                        ((RasterRenderSlice)RenderSlices[c]).Clear(a, this.imageheight - a);
                        ((RasterRenderSlice)RenderSlices[c]).ClearPoint();
                        ((RasterRenderSlice)RenderSlices[c]).ReInit(this.sampledata[c].value);
                    }
                }

                while (i < l && a < this.imageheight)
                {
                    // erstmal bis zum event vorspulen und pixelspalten mit letztem wert malen (PLOT) (**)
                    while ((int)peakend < this.eventdata.timestamps[i] && a < this.imageheight)
                    {
                        for (int c = channelNum; c < numOfChannels; ++c)
                        {
                            if (RenderSlices[c] != null)
                            {
                                ((RasterRenderSlice)RenderSlices[c]).Plot(a, this.sampledata[c].value);
                            }
                        }

                        peakend += samplesperpixel;
                        ++a;
                    }

                    if (a < this.imageheight)
                    {
                        i = this.PeakSamples(i, (int)peakend);

                        // aggregierte pixelspalte malen (PEAK)
                        for (int c = channelNum; c < numOfChannels; ++c)
                        {
                            this.sampledata[c].value = (this.sampledata[c].value - this.Baseline) * this.Lsbvalue;
                            this.sampledata[c].min = (this.sampledata[c].min - this.Baseline) * this.Lsbvalue;
                            this.sampledata[c].max = (this.sampledata[c].max - this.Baseline) * this.Lsbvalue;

                            if (RenderSlices[c] != null)
                            {
                                // jetzt mit marker
                                ((RasterRenderSlice)RenderSlices[c]).PeakMark(a, this.sampledata[c]);
                            }
                        }

                        peakend += samplesperpixel;
                        ++a;
                    }
                }              
			}			
		}

        public void RenderPoint(int pixelheight, double samplesperpixel, double s, double peakend, int channelNum, int numOfChannels)
        {
            EllipseGeometry myEllipseGeometry = this.GetCircle();
            // erster event im sichtbaren bereich
            int i = this.eventdata.Search((uint)Math.Ceiling(s));
            int o = i * this.channels;

            int l = this.eventdata.timestamps.Length;
            int a = 0;

            if (i != -1)
            {
                // "letzter wert" für unten (**) initialisieren
                if (i >= 1)
                {
                    int oprevious = o - this.channels;

                    for (int c = channelNum; c < numOfChannels; ++c)
                    {
                        this.sampledata[c].min = (this.eventdata.values[oprevious + c] - this.Baseline) * this.Lsbvalue;
                        this.sampledata[c].max = (this.eventdata.values[oprevious + c] - this.Baseline) * this.Lsbvalue;
                        this.sampledata[c].value = (this.eventdata.values[oprevious + c] - this.Baseline) * this.Lsbvalue;

                       //++oprevious;
                    }
                }
                else
                {
                    for (int c = channelNum; c < numOfChannels; ++c)
                    {
                        this.sampledata[c].min = 0.0f;
                        this.sampledata[c].max = 0.0f;
                        this.sampledata[c].value = 0.0f;
                    }
                }

                for (int c = channelNum; c < numOfChannels; ++c)
                {
                    if (RenderSlices[c] != null)
                    {
                        ((RasterRenderSlice)RenderSlices[c]).Clear(a, this.imageheight - a);
                        ((RasterRenderSlice)RenderSlices[c]).ClearPoint();
                        ((RasterRenderSlice)RenderSlices[c]).ReInit(this.sampledata[c].value);
                    }
                }

                while (i < l && a < this.imageheight)
                {
                    // erstmal bis zum event vorspulen und pixelspalten mit letztem wert malen (PLOT) (**)
                    while ((int)peakend < this.eventdata.timestamps[i] && a < this.imageheight)
                    {
                        peakend += samplesperpixel;
                        ++a;
                    }

                    if (a < this.imageheight)
                    {
                        i = this.PeakSamples(i, (int)peakend);

                        // aggregierte pixelspalte malen (PEAK)
                        for (int c = channelNum; c < numOfChannels; ++c)
                        {
                            this.sampledata[c].value = (this.sampledata[c].value - this.Baseline) * this.Lsbvalue;
                            this.sampledata[c].min = (this.sampledata[c].min - this.Baseline) * this.Lsbvalue;
                            this.sampledata[c].max = (this.sampledata[c].max - this.Baseline) * this.Lsbvalue;

                            if (RenderSlices[c] != null)
                            {
                                // now plot Point
                                ((RasterRenderSlice)RenderSlices[c]).PlotPoint(a, this.sampledata[c].value, myEllipseGeometry);
                            }
                        }

                        peakend += samplesperpixel;
                        ++a;
                    }
                }
            }
        }

		public override void UpdateZoomInfo(double time, double timestretch)
		{
			double s = time * this.samplespersec;		// (kontinuierliche) nummer des samples
			int i = this.eventdata.Search((uint)Math.Ceiling(s));		// erster event im sichtbaren bereich

			if (i != -1)
			{
				if (i >= 1)
				{
					--i;
				}

				this.PeakSamples(i, (int)Math.Ceiling((time + timestretch) * this.samplespersec));

				for (int a = 0; a < this.channels; ++a)
				{
					if (RenderSlices[a] != null)
					{
						RenderSlices[a].Zoominfo.PhysicalMin = (this.sampledata[a].min - this.Baseline) * this.Lsbvalue;
						RenderSlices[a].Zoominfo.PhysicalMax = (this.sampledata[a].max - this.Baseline) * this.Lsbvalue;
					}
				}
			}
		}

        public void RenderLinear(int pixelheight, double samplesperpixel, double s, double peakend, int channelNum, int numOfChannels)
        {
            EllipseGeometry myEllipseGeometry = this.GetCircle();
            // erster event im sichtbaren bereich
            int iTimestamps = this.eventdata.Search((uint)Math.Ceiling(s));
            int o = iTimestamps * this.channels;

            int nTimestamps = this.eventdata.timestamps.Length;
            int x = 0;

            if (iTimestamps != -1)
            {
                // "letzter wert" für unten (**) initialisieren
                if (iTimestamps >= 1)
                {
                    int oprevious = o - this.channels;

                    for (int c = channelNum; c < numOfChannels; ++c)
                    {
                        this.sampledata[c].min = (this.eventdata.values[oprevious + c] + this.Baseline) * this.Lsbvalue;
                        this.sampledata[c].max = (this.eventdata.values[oprevious + c] + this.Baseline) * this.Lsbvalue;
                        this.sampledata[c].value = (this.eventdata.values[oprevious + c] + this.Baseline) * this.Lsbvalue;

                        //++oprevious;
                    }
                }
                else
                {
                    for (int c = channelNum; c < numOfChannels; ++c)
                    {
                        this.sampledata[c].min = 0.0f;
                        this.sampledata[c].max = 0.0f;
                        this.sampledata[c].value = 0.0f;
                    }
                }

                for (int c = channelNum; c < numOfChannels; ++c)
                {
                    if (RenderSlices[c] != null)
                    {
                        ((RasterRenderSlice)RenderSlices[c]).Clear(x, this.imageheight - x);
                        ((RasterRenderSlice)RenderSlices[c]).ClearPoint();
                        ((RasterRenderSlice)RenderSlices[c]).ReInit(this.sampledata[c].value);
                    }
                }

                int nValues = 0;
                // last event out of the display
                if (iTimestamps > 0 && iTimestamps < nTimestamps)
                {
                    for (int c = channelNum; c < numOfChannels; ++c)
                    {
                        this.sampledata[c].value = (this.sampledata[c].value + this.Baseline) * this.Lsbvalue;
                        this.sampledata[c].min = (this.sampledata[c].min + this.Baseline) * this.Lsbvalue;
                        this.sampledata[c].max = (this.sampledata[c].max + this.Baseline) * this.Lsbvalue;

                        float lastValue = this.sampledata[c].value;
                        double lastTimeStamp = (double)(this.eventdata.timestamps[iTimestamps - 1]);
                        int lastPixelNumber = (int)((lastTimeStamp - s) / samplesperpixel);

                        if (RenderSlices[c] != null)
                        {
                            ((RasterRenderSlice)RenderSlices[c]).PlotLinear(lastPixelNumber, lastValue, nValues, 0, myEllipseGeometry, c);
                        }                      
                    }
                    nValues++;
                }

                while (iTimestamps < nTimestamps && x < this.imageheight)
                {                  
                    // erstmal bis zum event vorspulen und pixelspalten mit letztem wert malen (PLOT) (**)
                    while ((int)peakend < this.eventdata.timestamps[iTimestamps] && x < this.imageheight)
                    {
                        peakend += samplesperpixel;
                        ++x;
                    }
                    if (x < this.imageheight)
                    {
                        iTimestamps = this.PeakSamples(iTimestamps, (int)peakend);

                        // aggregierte pixelspalte malen (PEAK)
                        for (int c = channelNum; c < numOfChannels; ++c)
                        {
                            this.sampledata[c].value = (this.sampledata[c].value + this.Baseline) * this.Lsbvalue;
                            this.sampledata[c].min = (this.sampledata[c].min + this.Baseline) * this.Lsbvalue;
                            this.sampledata[c].max = (this.sampledata[c].max + this.Baseline) * this.Lsbvalue;

                            if (RenderSlices[c] != null)
                            {
                                ((RasterRenderSlice)RenderSlices[c]).PlotLinear(x, this.sampledata[c].value, nValues, iTimestamps, myEllipseGeometry, c);
                            }
                            
                        }
                        nValues++;
                        peakend += samplesperpixel;
                        ++x;
                    }                 
                }
                // next event out of the display
                if (x == this.imageheight)
                {
                    // letzte Event liegt nicht im Anzeigebereich 
                    if (iTimestamps < nTimestamps)
                    {
                        int nextTimeStamp = (int)(this.eventdata.timestamps[iTimestamps]);
                        for (int c = channelNum; c < numOfChannels; ++c)
                        {
                            this.sampledata[c].value = (this.sampledata[c].value + this.Baseline) * this.Lsbvalue;
                            this.sampledata[c].min = (this.sampledata[c].min + this.Baseline) * this.Lsbvalue;
                            this.sampledata[c].max = (this.sampledata[c].max + this.Baseline) * this.Lsbvalue;

                            //float nextTimeStamp = this.sampledata[iChannel].value;
                            float nextValue = this.eventdata.values[(iTimestamps + 1) * this.channels - this.channels + c];      // (2+1)*2 = 6 values in eventdata
                            int nextPixelNumber = (int)((nextTimeStamp - s) / samplesperpixel);

                            if (RenderSlices[c] != null)
                            {
                                ((RasterRenderSlice)RenderSlices[c]).PlotLinear(nextPixelNumber, nextValue, nValues, iTimestamps + 1, myEllipseGeometry, c);
                            }                          
                        }
                        nValues++;
                    }
                }
            }
        }

		public override SampleInfo GetSampleInfo(int channel, double time, double time_end)
		{
			double s = time * this.samplespersec;		// (kontinuierliche) nummer des samples
			int i = this.eventdata.Search((uint)Math.Floor(s));
			int s_end = (int)Math.Floor(time_end * this.samplespersec);

			if (i != -1)
			{
				this.PeakSamples(i, s_end);

				if (this.sampledata[channel].min == float.MaxValue || this.sampledata[channel].max == float.MinValue)
				{
					if (i > 0)
					{
						this.sampledata[channel].min = this.eventdata.values[((i - 1) * this.channels) + channel];
						this.sampledata[channel].max = this.sampledata[channel].min;
					}
					else
					{
						this.sampledata[channel].min = 0.0f;
						this.sampledata[channel].max = 0.0f;
					}
				}

				return new SampleInfoEventValue(this, this.sampledata[channel].min, this.sampledata[channel].max, time, time_end, (int)s, s_end);
			}

			return null;
		}

		private int PeakSamples(int i, int timestamp_end)
		{
			int nTimestamps = this.eventdata.timestamps.Length;
			int o = i * this.channels;
			float ev;

			for (int c = 0; c < this.channels; ++c)
			{
				this.sampledata[c].min = float.MaxValue;
				this.sampledata[c].max = float.MinValue;
			}

			while (i < nTimestamps && this.eventdata.timestamps[i] <= timestamp_end || i == 0)
			{
				for (int c = 0; c < this.channels; ++c)
				{
					ev = this.eventdata.values[o];
					this.sampledata[c].value = ev;

					if (ev < this.sampledata[c].min)
					{
						this.sampledata[c].min = ev;
					}

					if (ev > this.sampledata[c].max)
					{
						this.sampledata[c].max = ev;
					}

					++o;
				}

				++i;
			}

			return i;
		}

        private EllipseGeometry GetCircle()
        {
            EllipseGeometry myEllipseGeometry = new EllipseGeometry();
            //myEllipseGeometry.Center = new Point(0, 0);
            myEllipseGeometry.RadiusX = 1;
            myEllipseGeometry.RadiusY = 1;
            return myEllipseGeometry;
        }

		#region old code
		/*
	public override void ZoomInto(double time, double timestretch)
	{
		double s = time * samplespersec;		// (kontinuierliche) nummer des samples
		int i = eventdata.Search((uint)Math.Ceiling(s));		// erster event im sichtbaren bereich


		if (i != -1)
		{
			PeakSamples(i, (int)Math.Ceiling((time + timestretch) * samplespersec));


			for (int i = 0; i < channels; ++i)
				if (renderslices[i] != null)
					renderslices[i].ZoomInto((sampledata[i].min - baseline) * lsbvalue, (sampledata[i].max - baseline) * lsbvalue);
		}

		Render(time, timestretch);
	}
	*/
		#endregion
	
    }
}
