using System;
using System.Windows;
using System.Xml.Linq;
using UnisensViewerClrCppLibrary;
using UnisensViewerLibrary;

namespace UnisensViewer
{
    /// <summary>
    /// Implements the Renderer for all signal entries.
    /// </summary>
    public unsafe class StreamRenderer : Renderer
    {
        public readonly float Baseline;
        public readonly float Lsbvalue;
        public readonly string Unit;

        protected const double SINC_USESAMPLES = 10.0;

        //private const int DEFAULT_IMAGEWIDTH = 128;
        public int imagewidth;

        private StreamDataType datatype;
        private int channels;
        private int samplestructsize;
        private byte* mapbase;
        private byte* mapend;

        private SampleD[] physicaldata;

        private CsvFile csvfile;
        private MapFile mapfile;
        private SampleReader samplereader;

        private long filesamples;
        private long filesize;		// mit ganzzahliger anzahl samplestructs

        private double samplespersec;

        public int imageheight;

        public Int32Rect dirtyrect;

        public StreamRenderer(XElement signalentry, double guisignaldisplaywidth, int imagewidth)
            : base(signalentry)
        {
            this.channels = SignalEntry.GetNumChannels(signalentry);

            switch (SignalEntry.GetFileFormat(signalentry))
            {
                case FileFormat.Bin:
                    this.datatype = SignalEntry.GetBinDataType(signalentry);
                    this.samplestructsize = this.channels * (int)SignalEntry.GetDataTypeBytes(this.datatype);
                    break;

                case FileFormat.Csv:
                    this.datatype = StreamDataType.Ieee754_Single;
                    this.samplestructsize = this.channels * (int)SignalEntry.GetDataTypeBytes(this.datatype);
                    break;

                default:
                    throw new Exception("Dateiformat noch nicht unterstützt.");
            }

            this.Baseline = (float)SignalEntry.GetBaseline(signalentry);
            this.Lsbvalue = (float)SignalEntry.GetLsbValue(signalentry);
            this.Unit = SignalEntry.GetUnit(signalentry);
            this.samplespersec = SignalEntry.GetSampleRate(signalentry);

            this.imageheight = (int)guisignaldisplaywidth;
            this.imagewidth = imagewidth;
            //this.imageheight = 100;
            this.dirtyrect = new Int32Rect(0, 0, this.imagewidth, this.imageheight);

            this.physicaldata = new SampleD[this.channels];

            this.ReOpen();
        }

        public override int Channels
        {
            get { return this.channels; }
        }

        public override double TimeMax
        {
            get { return (double)this.filesamples / this.samplespersec; }
        }

        public override void ReOpen()
        {
            switch (SignalEntry.GetFileFormat(this.SevEntry))
            {
                case FileFormat.Bin:
                    this.mapfile = new MapFile(SignalEntry.GetId(this.SevEntry), true);

                    // hier nochmal ausrechnen, falls die datei am ende n bissel kaputt ist...
                    // (datei wird dann auf ganze sampleanzahl abgeschnitten)
                    this.filesamples = this.mapfile.filesize / this.samplestructsize;
                    this.filesize = this.filesamples * this.samplestructsize;
                    this.mapbase = (byte*)this.mapfile.mapbase;
                    this.mapend = (byte*)this.mapbase + this.filesize;

                    this.samplereader = SampleReaderFactory.GetSampleReader(this.mapbase, this.mapend, this.datatype, this.channels);
                    break;

                case FileFormat.Csv:
                    this.csvfile = new CsvFile(SignalEntry.GetId(this.SevEntry), this.channels, Entry.GetCsvFileFormatSeparator(this.SevEntry), Entry.GetCsvFileDecimalSeparator(this.SevEntry));

                    this.filesamples = this.csvfile.samples;
                    this.filesize = this.filesamples * this.samplestructsize;
                    this.mapbase = (byte*)this.csvfile.data;
                    this.mapend = (byte*)this.mapbase + this.filesize;

                    this.samplereader = SampleReaderFactory.GetSampleReader(this.mapbase, this.mapend, this.datatype, this.channels);
                    break;
            }
        }

        public override void Close()
        {
            this.samplereader = null;

            if (this.mapfile != null)
            {
                this.mapfile.Dispose();
                this.mapfile = null;
            }

            if (this.csvfile != null)
            {
                this.csvfile.Dispose();
                this.csvfile = null;
            }
        }

        /// <summary>
        /// Creates a new RenderSlice.
        /// </summary>
        /// <param name="channelnum">The number of the channel is needed for getting the channel name.</param>
        /// <returns></returns>
        public override RenderSlice CreateRenderSlice(int channelnum)
        {
            return new RasterRenderSlice(this, channelnum, SignalEntry.GetChannelName(this.SevEntry, channelnum), imagewidth, this.imageheight, SignalEntry.GetUnit(this.SevEntry), SignalEntry.GetChannel(this.SevEntry, channelnum));
        }

        /// <summary>
        /// This function renders a single Renderer Object with all render slices (when channel is null) or only a specific render slice or a renderer (when channel is not null).
        /// </summary>
        /// <param name="time">time in seconds at the beginning of the signal pane</param>
        /// <param name="timestretch">length of the signal pane</param>
        /// <param name="channel">specific channel</param>
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

            // Locks all RenderSlice's bitmaps
            for (int a = channelNum; a < numOfChannels; ++a)
            {
                if (this.RenderSlices[a] != null)
                {
                    ((RasterRenderSlice)this.RenderSlices[a]).WBmp.Lock();
                }
            }

            double samplesperpixel = this.samplespersec * timestretch / this.imageheight;

            // Perform signal interpolation, compression or just render the signal.
            if (samplesperpixel < 1)
            {
                if (RendererManager.SincInterpolation)
                {
                    this.RenderSinc(0, this.imageheight, time * this.samplespersec, samplesperpixel, channelNum, numOfChannels);
                }
                else
                {
                    this.RenderSampleAndHold(0, this.imageheight, time * this.samplespersec, samplesperpixel, channelNum, numOfChannels);
                }
            }
            else
            {
                if (RendererManager.PeakSamples)
                {
                    this.RenderPeak(0, this.imageheight, time * this.samplespersec, samplesperpixel, channelNum, numOfChannels);
                }
                else
                {
                    this.RenderSkip(0, this.imageheight, time * this.samplespersec, samplesperpixel, channelNum, numOfChannels);
                }
            }

            // Change parts of the old bitmap with the new rendered bitmap.
            for (int a = channelNum; a < numOfChannels; ++a)
            {
                if (RenderSlices[a] != null)
                {
                    if (((RasterRenderSlice)this.RenderSlices[a]).WBmp.PixelWidth != this.dirtyrect.Width)
                    {
                        this.dirtyrect.Width = ((RasterRenderSlice)this.RenderSlices[a]).WBmp.PixelWidth;
                    }
                    ((RasterRenderSlice)this.RenderSlices[a]).WBmp.AddDirtyRect(this.dirtyrect);
                    ((RasterRenderSlice)this.RenderSlices[a]).WBmp.Unlock();
                }
            }
        }

        /// <summary>
        /// Updates the Zoominfo of a RenderSlice. 
        /// </summary>
        /// <param name="time">time in seconds at the beginning of the signal pane</param>
        /// <param name="timestretch">length of the signal pane</param>
        public override void UpdateZoomInfo(double time, double timestretch)
        {
            double soffs = time * this.samplespersec;

            long soffsint = (long)soffs;
            byte* mapaddr = (soffsint * this.samplestructsize) + this.mapbase;

            if (soffsint < this.filesamples)
            {
                double soffs_end = Math.Ceiling((time + timestretch) * this.samplespersec);
                long soffsint_end = (long)soffs_end;
                byte* mapaddr_end;

                if (soffsint_end < this.filesamples)
                {
                    mapaddr_end = (soffsint_end * this.samplestructsize) + this.mapbase;
                }
                else
                {
                    mapaddr_end = ((this.filesamples - 1) * this.samplestructsize) + this.mapbase;
                }

                this.samplereader.Peak(mapaddr, mapaddr_end + this.samplestructsize);

                for (int a = 0; a < this.channels; ++a)
                {
                    if (this.RenderSlices[a] != null)
                    {
                        this.RenderSlices[a].Zoominfo.PhysicalMin = (this.samplereader.data[a].min - this.Baseline) * this.Lsbvalue;
                        this.RenderSlices[a].Zoominfo.PhysicalMax = (this.samplereader.data[a].max - this.Baseline) * this.Lsbvalue;
                    }
                }
            }
        }

        public override SampleInfo GetSampleInfo(int channel, double time, double time_end)
        {
            double soffs = time * this.samplespersec;
            long soffsint = (long)soffs;
            byte* mapaddr = (soffsint * this.samplestructsize) + this.mapbase;

            if (soffsint < this.filesamples)
            {
                double soffs_end = time_end * this.samplespersec;
                long soffsint_end = (long)soffs_end;
                byte* mapaddr_end;

                if (soffsint_end < this.filesamples)
                {
                    mapaddr_end = (soffsint_end * this.samplestructsize) + this.mapbase;
                }
                else
                {
                    mapaddr_end = ((this.filesamples - 1) * this.samplestructsize) + this.mapbase;
                }

                this.samplereader.Peak(mapaddr, mapaddr_end + this.samplestructsize);

                return new SampleInfoStream(this, this.samplereader.data[channel].min, this.samplereader.data[channel].max, time, time_end, soffsint, soffsint_end);
            }
            else
            {
                return null;
            }
        }

        private unsafe void RenderPeak(int pixelrow, int pixelheight, double sampleoffs, double samplesperpixel, int channelNum, int numOfChannels)
        {
            long sampleoffsint;			// muss 64bit sein, wegen dateien > 4gb (double sampleoffs)
            byte* mapaddr;					// je nach system 32/64bit

            sampleoffsint = (long)sampleoffs;								// cast == FLOOR für positive zahlen
            mapaddr = (sampleoffsint * this.samplestructsize) + this.mapbase;

            // rasterrenderslice-xlastsample mit erstem samplewert initialisiern
            if (sampleoffsint < this.filesamples)
            {
                this.samplereader.Peak(mapaddr, mapaddr + this.samplestructsize);
                for (int a = channelNum; a < numOfChannels; ++a)
                {
                    if (RenderSlices[a] != null)
                    {
                        ((RasterRenderSlice)this.RenderSlices[a]).ReInit((this.samplereader.data[a].value - this.Baseline) * this.Lsbvalue);
                    }
                }
            }

            while (pixelheight > 0 && sampleoffsint < this.filesamples)
            {
                byte* ma = mapaddr;

                // vom schleifenende vorgezogen:
                // (brauche begrenzung zum nächsten sample bzw mapaddr,
                // damit der samplepeaker weiß, wann er aufhören soll)
                sampleoffs += samplesperpixel;
                sampleoffsint = (long)sampleoffs;							// cast == FLOOR für positive zahlen
                mapaddr = (sampleoffsint * this.samplestructsize) + this.mapbase;

                this.samplereader.Peak(ma, mapaddr);

                for (int a = channelNum; a < numOfChannels; ++a)
                {
                    if (RenderSlices[a] != null)
                    {
                        this.physicaldata[a].value = (this.samplereader.data[a].value - this.Baseline) * this.Lsbvalue;
                        this.physicaldata[a].min = (this.samplereader.data[a].min - this.Baseline) * this.Lsbvalue;
                        this.physicaldata[a].max = (this.samplereader.data[a].max - this.Baseline) * this.Lsbvalue;

                        ((RasterRenderSlice)this.RenderSlices[a]).Peak(pixelrow, this.physicaldata[a]);
                    }
                }

                --pixelheight;
                ++pixelrow;
            }

            // das ende löschen (nicht: nullen plotten)
            if (sampleoffsint >= this.filesamples)
            {
                for (int a = channelNum; a < numOfChannels; ++a)
                {
                    if (RenderSlices[a] != null)
                    {
                        ((RasterRenderSlice)this.RenderSlices[a]).Clear(pixelrow, pixelheight);
                    }
                }
            }
        }

        private unsafe void RenderSkip(int pixelrow, int pixelheight, double sampleoffs, double samplesperpixel, int channelNum, int numOfChannels)
        {
            long sampleoffsint;			// muss 64bit sein, wegen dateien > 4gb (double sampleoffs)
            byte* mapaddr;					// je nach system 32/64bit

            sampleoffsint = (long)sampleoffs;								// cast == FLOOR für positive zahlen
            mapaddr = (sampleoffsint * this.samplestructsize) + this.mapbase;

            // rasterrenderslice-xlastsample mit erstem samplewert initialisiern
            if (sampleoffsint < this.filesamples)
            {
                this.samplereader.Read(mapaddr);
                for (int a = channelNum; a < numOfChannels; ++a)
                {
                    if (RenderSlices[a] != null)
                    {
                        ((RasterRenderSlice)RenderSlices[a]).ReInit((this.samplereader.data[a].value - this.Baseline) * this.Lsbvalue);
                    }
                }
            }

            while (pixelheight > 0 && sampleoffsint < this.filesamples)
            {
                this.samplereader.Read(mapaddr);

                for (int a = channelNum; a < numOfChannels; ++a)
                {
                    if (RenderSlices[a] != null)
                    {
                        ((RasterRenderSlice)RenderSlices[a]).Plot(pixelrow, (this.samplereader.data[a].value - this.Baseline) * this.Lsbvalue);
                    }
                }

                sampleoffs += samplesperpixel;
                sampleoffsint = (long)sampleoffs;							// cast == FLOOR für positive zahlen
                mapaddr = (sampleoffsint * this.samplestructsize) + this.mapbase;

                --pixelheight;
                ++pixelrow;
            }

            // das ende löschen (nicht: nullen plotten)
            if (sampleoffsint >= this.filesamples)
            {
                for (int a = channelNum; a < numOfChannels; ++a)
                {
                    if (this.RenderSlices[a] != null)
                    {
                        ((RasterRenderSlice)this.RenderSlices[a]).Clear(pixelrow, pixelheight);
                    }
                }
            }
        }

        private unsafe void RenderSinc(int pixelrow, int pixelheight, double sampleoffs, double samplesperpixel, int channelNum, int numOfChannels)
        {
            long sampleoffsint;			// muss 64bit sein, wegen dateien > 4gb (double sampleoffs)
            byte* mapaddr;					// je nach system 32/64bit

            sampleoffsint = (long)sampleoffs;								// cast == FLOOR für positive zahlen
            mapaddr = (sampleoffsint * this.samplestructsize) + this.mapbase;

            // rasterrenderslice-xlastsample mit erstem samplewert initialisiern
            if (sampleoffsint < this.filesamples)
            {
                this.samplereader.Peak(mapaddr, mapaddr + this.samplestructsize);
                for (int a = channelNum; a < numOfChannels; ++a)
                {
                    if (this.RenderSlices[a] != null)
                    {
                        ((RasterRenderSlice)RenderSlices[a]).ReInit((this.samplereader.data[a].value - this.Baseline) * this.Lsbvalue);
                    }
                }
            }

            while (pixelheight > 0 && sampleoffsint < this.filesamples)
            {
                this.samplereader.Sinc(mapaddr, sampleoffs);

                for (int a = channelNum; a < numOfChannels; ++a)
                {
                    if (RenderSlices[a] != null)
                    {
                        ((RasterRenderSlice)this.RenderSlices[a]).Plot(pixelrow, (this.samplereader.data[a].value - this.Baseline) * this.Lsbvalue);
                    }
                }

                sampleoffs += samplesperpixel;
                sampleoffsint = (long)sampleoffs;							// cast == FLOOR für positive zahlen
                mapaddr = (sampleoffsint * this.samplestructsize) + this.mapbase;

                --pixelheight;
                ++pixelrow;
            }

            // das ende löschen (nicht: nullen plotten)
            if (sampleoffsint >= this.filesamples)
            {
                for (int a = channelNum; a < numOfChannels; ++a)
                {
                    if (RenderSlices[a] != null)
                    {
                        ((RasterRenderSlice)this.RenderSlices[a]).Clear(pixelrow, pixelheight);
                    }
                }
            }
        }

        private unsafe void RenderSampleAndHold(int pixelrow, int pixelheight, double sampleoffs, double samplesperpixel, int channelNum, int numOfChannels)
        {
            long sampleoffsint;			// muss 64bit sein, wegen dateien > 4gb (double sampleoffs)
            byte* mapaddr;					// je nach system 32/64bit

            sampleoffsint = (long)sampleoffs;								// cast == FLOOR für positive zahlen
            mapaddr = (sampleoffsint * this.samplestructsize) + this.mapbase;

            // rasterrenderslice-xlastsample mit erstem samplewert initialisiern
            if (sampleoffsint < this.filesamples)
            {
                this.samplereader.Read(mapaddr);
                for (int a = channelNum; a < numOfChannels; ++a)
                {
                    if (RenderSlices[a] != null)
                    {
                        ((RasterRenderSlice)this.RenderSlices[a]).ReInit((this.samplereader.data[a].value - this.Baseline) * this.Lsbvalue);
                    }
                }
            }

            while (pixelheight > 0 && sampleoffsint < this.filesamples)
            {
                this.samplereader.Read(mapaddr);

                for (int a = channelNum; a < numOfChannels; ++a)
                {
                    if (RenderSlices[a] != null)
                    {
                        ((RasterRenderSlice)this.RenderSlices[a]).PlotHardEdges(pixelrow, (this.samplereader.data[a].value - this.Baseline) * this.Lsbvalue);
                    }
                }

                sampleoffs += samplesperpixel;
                sampleoffsint = (long)sampleoffs;							// cast == FLOOR für positive zahlen
                mapaddr = (sampleoffsint * this.samplestructsize) + this.mapbase;

                --pixelheight;
                ++pixelrow;
            }

            // das ende löschen (nicht: nullen plotten)
            if (sampleoffsint >= this.filesamples)
            {
                for (int a = channelNum; a < numOfChannels; ++a)
                {
                    if (RenderSlices[a] != null)
                    {
                        ((RasterRenderSlice)this.RenderSlices[a]).Clear(pixelrow, pixelheight);
                    }
                }
            }
        }
    }
    #region old code
    /*
	public override void ZoomInto(double time, double timestretch)
	{
		double	soffs		= time * samplespersec;

		Int64	soffsint	= (Int64)soffs;
		byte*	mapaddr		= soffsint * samplestructsize + mapbase;



		if (soffsint < filesamples)
		{
			double	soffs_end		= Math.Ceiling((time + timestretch) * samplespersec);
			Int64	soffsint_end	= (Int64)soffs_end;
			byte*	mapaddr_end;


			if (soffsint_end < filesamples)
				mapaddr_end = soffsint_end * samplestructsize + mapbase;
			else
				mapaddr_end = (filesamples - 1) * samplestructsize + mapbase;



			samplereader.Peak(mapaddr, mapaddr_end + samplestructsize);


			for (int a = 0; a < channels; ++a)
			{
				if (renderslices[a] != null)
				{
					physicaldata[a].value	= (samplereader.data[a].value - baseline) * lsbvalue;
					physicaldata[a].min		= (samplereader.data[a].min - baseline) * lsbvalue;
					physicaldata[a].max		= (samplereader.data[a].max - baseline) * lsbvalue;

					renderslices[a].ZoomInto(physicaldata[a].min, physicaldata[a].max);
				}
			}
		}


		Render(time, timestretch);
	}
	*/
    #endregion;
}
