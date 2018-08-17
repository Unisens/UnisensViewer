using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using UnisensViewerClrCppLibrary;
using System.Windows;
using System.Diagnostics;

namespace UnisensViewer
{
	public unsafe class RasterRenderSlice : RenderSlice
	{                                       
		private int	xlastplot;
		public int strideUInt32;	
	
		public uint* argb;
        // for Point
        public GeometryGroup geometrygroup;
        public GeometryDrawing geometrydrawing;
        private int xTemp;
        private double[] yTemp;
		
		public RasterRenderSlice(Renderer renderer, int channel, string name, int imagewidth, int imageheight, string unit, XElement unisensnode)
            : base(renderer, channel, name, ColorRelaxed.GetNextColor(), unit, unisensnode)
		{
            this.ImageWidth = imagewidth;
            this.ImageHeight = imageheight;
            this.yTemp = new double[channel+1];
            this.xlastplot = 0;


            Scale = 0.05f;
            Offset = (float)(imagewidth >> 1);
            

            // PixelFormats.Indexed8 wäre super, aber das wird laut Doku konvertiert bevor
            // es ins GPU-RAM geschoben wird, also kein Performancegewinn hier.
            // Wenn man direkten Zugriff auf DirectX hätte, könnte man durchaus eine 8Bit Surface
            // mit ColorKey machen.
            this.WBmp = new WriteableBitmap(imagewidth, this.ImageHeight, 96, 96, PixelFormats.Pbgra32, null);

            this.argb = (uint*)this.WBmp.BackBuffer;
            this.strideUInt32 = this.WBmp.BackBufferStride >> 2;

            this.geometrygroup = new GeometryGroup();
            MatrixTransform mt = new MatrixTransform(0.0, 1.0, -1.0, 0.0, imagewidth, 0.0);
            mt.Freeze();
            this.geometrygroup.Transform = mt;
            GeometryDrawing geometrydrawing = new GeometryDrawing(
                new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)(color >> 24), (byte)(color >> 16), (byte)(color >> 8), (byte)color)),
                null,
                this.geometrygroup);
            geometrydrawing.Pen = new Pen(new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)(color >> 24), (byte)(color >> 16), (byte)(color >> 8), (byte)color)), 1);
            this.geometrydrawing = geometrydrawing;
            DrawingGroup drawinggroup = new DrawingGroup();
            drawinggroup.Children.Add(geometrydrawing);
            Rect rect = new Rect(0.0, 0.0, imagewidth, this.ImageHeight);

            ImageDrawing imagedrawing = new ImageDrawing(WBmp, rect);
            drawinggroup.Children.Add(imagedrawing);
            //this.imagesource = this.WBmp;
            this.imagesource = new DrawingImage(drawinggroup);   
		}

		public WriteableBitmap WBmp { get; set; }

        private int _imageWidth;

		public override int ImageWidth 
        {
            get
            {
                //Trace.WriteLine("Get ImageWidth");

                return _imageWidth;
            }

            set 
            {
               // if (this._imageWidth != value)
                {
                    this._imageWidth = value;

                    OnPropertyChanged("ImageWidth");
                    Trace.WriteLine("OnPropertyChanged(ImageWidth)" + value);
                }
            }
        }

		public int ImageHeight { get; set; } 

		public void Clear(int pixelrow, int pixelheight)
		{
			int o = pixelrow * this.strideUInt32;		// XXX durch ein shift ersetzen oder außen addition
			int len = pixelrow + pixelheight < this.ImageHeight ? pixelheight - pixelrow : this.ImageHeight - pixelrow;

			int pixels = this.strideUInt32 * len;

			for (int a = 0; a < pixels; ++a)
			{
				this.argb[o] = 0;
				++o;
			}
		}

        public void ClearPoint()
        {
            this.Clear(0, ImageHeight);
            this.geometrygroup.Children.Clear();
        }

		// plot/peak verbindet den aktuellen plot mit ner linie mit dem vorherigen.
		// den koordinatenwert des letzten plots wird in xlastplot gespeichert.
		// wird das signal komplett neu gerendert, muss daher xlastplot initialisiert werden.
		public void ReInit(float zamplitude)
		{
            this.xlastplot = (int)((zamplitude - Offset) * Scale);
		}

		public void PlotHardEdges(int pixelrow, float amplitude)
		{
			int x, xstart, xend;

			// stetige plots
            x = (int)((amplitude - this.Offset) * this.Scale);

			if (x <= this.xlastplot)
			{
				xstart = x;
				xend = this.xlastplot;
			}
			else
			{
				xstart = this.xlastplot;
				xend = x;
			}

			this.xlastplot = x;

			this.ClippingAndPixeling(xstart, xend, pixelrow);
		}

		public void Plot(int pixelrow, float amplitude)
		{
			int x, xstart, xend;
			
			// stetige plots
            x = (int)((amplitude - this.Offset) * this.Scale);

			if (x <= this.xlastplot)
			{
				xstart = x;
				xend = this.xlastplot;
			}
			else
			{
				xstart = this.xlastplot;
				xend = x;
			}

			this.xlastplot = x;

			if (xstart != xend)
			{
				xend--;
			}

			this.ClippingAndPixeling(xstart, xend, pixelrow);
		}

        public void PlotPoint(int pixelrow, float amplitude, EllipseGeometry myEllipseGeometry)
        {
            int y;

            // stetige plots
            y = (int)((amplitude - this.Offset) * this.Scale);

            GeometryGroup gg = new GeometryGroup();
            gg.Children.Add(myEllipseGeometry);
            if (((this.ImageWidth - y) >= 1) && ((this.ImageWidth - y) <= this.ImageWidth))
            {
                gg.Transform = new TranslateTransform(pixelrow, this.ImageWidth - y);
                //myEllipseGeometry.Center = new Point(pixelrow, this.ImageWidth - y);
                
            }
            this.geometrygroup.Children.Add(gg);
        }


        public void PlotLinear(int x, float amplitude, int k, int i, EllipseGeometry firstEllipseGeometry, int c)
        {
            // The point (0,0) for the amplitude y and the pixelrow x is the bottom left corner.
            // The point (0,0) for the C# methods is the top left corner.
            double y;
            // stetige plots
            //y = (double)((amplitude - this.Offset + 0.00001) * this.Scale);
            y = (double)((amplitude - this.Offset) * this.Scale);

            EllipseGeometry secondEllipseGeometry = firstEllipseGeometry.Clone();
            LineGeometry myLineGeometry = new LineGeometry();
            GeometryGroup gg = new GeometryGroup();

            myLineGeometry.StartPoint = new Point(this.xTemp, this.ImageWidth - this.yTemp[c]);
            myLineGeometry.EndPoint = new Point(x, this.ImageWidth - y);
            secondEllipseGeometry.Center = myLineGeometry.EndPoint;
            gg.Children.Add(secondEllipseGeometry);
            if (k > 0)
            {
                gg.Children.Add(myLineGeometry);
            }
            this.geometrygroup.Children.Add(gg);
            this.xTemp = x;
            this.yTemp[c] = y;
        }
        //public void PlotLinear(int x, float amplitude, int k, int i, EllipseGeometry firstEllipseGeometry, int c)
        //{
        //    // The point (0,0) for the amplitude y and the pixelrow x is the bottom left corner.
        //    // The point (0,0) for the C# methods is the top left corner.
        //    double y;
        //    // stetige plots
        //    //y = (double)((amplitude - this.Offset + 0.00001) * this.Scale);
        //    y = (double)((amplitude - this.Offset) * this.Scale);

        //    EllipseGeometry secondEllipseGeometry = firstEllipseGeometry.Clone();
        //    LineGeometry myLineGeometry = new LineGeometry();
        //    GeometryGroup gg = new GeometryGroup();
        //    //Point startPoint = new Point();
        //    //Point endPoint = new Point();
        //    double a = Math.Abs(x - this.xTemp);
        //    double b = Math.Abs(y - this.yTemp[c]);

        //    if ((this.yTemp[c] > this.ImageWidth && y > this.ImageWidth) || (this.yTemp[c] < 0 && y < 0) || (this.xTemp < 0 && x < 0) || (this.xTemp > this.ImageHeight && x > this.ImageHeight))
        //    {
        //        // Both points are out of the visible bitmap: Draw nothing
        //    }
        //    else if ((this.yTemp[c] >= 0 && y >= 0) && (this.yTemp[c] <= this.ImageWidth && y <= this.ImageWidth) && (this.xTemp >= 0 && x >= 0) && (this.xTemp <= this.ImageHeight && x <= this.ImageHeight))
        //    {
        //        // Both points are inside the visible bitmap: Draw line
        //        myLineGeometry.StartPoint = new Point(this.xTemp, this.ImageWidth - this.yTemp[c]);
        //        myLineGeometry.EndPoint = new Point(x, this.ImageWidth - y);
        //        // paint the line, if there is mind. 2 Points                  
        //        firstEllipseGeometry.Center = new Point(this.xTemp, this.ImageWidth - this.yTemp[c]);
        //        gg.Children.Add(firstEllipseGeometry);
        //        secondEllipseGeometry.Center = new Point(x, this.ImageWidth - y);
        //        gg.Children.Add(secondEllipseGeometry);
        //        // Draw line, if it exits min. two Points
        //    }
        //    else if (this.xTemp < 0 && x > this.ImageHeight)
        //    {
        //        if (yTemp[c] <= y)
        //        {
        //            myLineGeometry.StartPoint = new Point(0, this.ImageWidth - (Math.Abs(this.xTemp) * b / a) - this.yTemp[c]);
        //            myLineGeometry.EndPoint = new Point(this.ImageHeight, this.ImageWidth - ((this.ImageHeight - this.xTemp) * b / a) - this.yTemp[c]);
        //        }
        //        else
        //        {
        //            myLineGeometry.StartPoint = new Point(0, this.ImageWidth - (x * b / a) - y);
        //            myLineGeometry.EndPoint = new Point(this.ImageHeight, this.ImageWidth - ((x - this.ImageHeight) * b / a) - y);
        //        }
        //    }
        //    else
        //    {
        //        // endpoint within ImageWith
        //        if (y >= 0 && y <= this.ImageWidth)
        //        {
        //            // startpoint ist unten links von Bitmap
        //            if (this.xTemp < 0 && this.yTemp[c] < 0)
        //            {
        //                // Zwischenspunkt liegt auf den horizontal Achse
        //                if ((Math.Abs((double)y / this.yTemp[c])) < (Math.Abs((double)x / this.xTemp)))
        //                {
        //                    myLineGeometry.StartPoint = new Point(x - (a * y / b), this.ImageWidth);
        //                }
        //                // Zwischenspunkt liegt auf den vertikale Achse
        //                if ((Math.Abs((double)y / this.yTemp[c])) > (Math.Abs((double)x / this.xTemp)))
        //                {
        //                    myLineGeometry.StartPoint = new Point(0, this.ImageWidth - y + (b * x / a));
        //                }
        //                myLineGeometry.EndPoint = new Point(x, this.ImageWidth - y);
        //                secondEllipseGeometry.Center = myLineGeometry.EndPoint;
        //                gg.Children.Add(secondEllipseGeometry);
        //            }
        //            // startpoint ist oben links von Bitmap
        //            else if (this.xTemp < 0 && this.yTemp[c] >= this.ImageWidth)
        //            {
        //                // Zwischenspunkt liegt auf den horizontal Achse(obere Achse)
        //                if ((Math.Abs((double)(this.ImageWidth - y) / (this.ImageWidth - this.yTemp[c]))) < (Math.Abs((double)x / this.xTemp)))
        //                {
        //                    myLineGeometry.StartPoint = new Point((a * (this.yTemp[c] - this.ImageWidth) / b - Math.Abs(this.xTemp)), 0);
        //                }
        //                // Zwischenspunkt liegt auf den vertikale Achse 
        //                if ((Math.Abs((double)(this.ImageWidth - y) / (this.ImageWidth - this.yTemp[c]))) > (Math.Abs((double)x / this.xTemp)) || (this.yTemp[c] == this.ImageWidth))
        //                {
        //                    myLineGeometry.StartPoint = new Point(0, b * Math.Abs(this.xTemp) / a - this.yTemp[c] + this.ImageWidth);
        //                }
        //                myLineGeometry.EndPoint = new Point(x, this.ImageWidth - y);
        //                secondEllipseGeometry.Center = myLineGeometry.EndPoint;
        //                gg.Children.Add(secondEllipseGeometry);
        //            }
        //            else
        //            {
        //                // Startpoint ist unten von Bitmap 
        //                if (this.yTemp[c] < 0)
        //                {
        //                    // endpoint innerhalb von visualen Bitmap
        //                    if (x > 0 && x < this.ImageHeight)
        //                    {
        //                        myLineGeometry.EndPoint = new Point(x, this.ImageWidth - y);
        //                        secondEllipseGeometry.Center = myLineGeometry.EndPoint;
        //                        gg.Children.Add(secondEllipseGeometry);
        //                    }
        //                    // endpoint ist rechts von visualen Bitmap
        //                    else if (x > this.ImageWidth)
        //                    {
        //                        myLineGeometry.EndPoint = new Point(this.ImageHeight, this.ImageWidth - ((this.ImageHeight - this.xTemp) * b / a) - this.yTemp[c]);
        //                    }
        //                    if (this.xTemp >= 0)
        //                    {
        //                        myLineGeometry.StartPoint = new Point(this.xTemp + (Math.Abs(this.yTemp[c]) * a / b), this.ImageWidth);
        //                    }
        //                    if (this.xTemp < 0)
        //                    {
        //                        myLineGeometry.StartPoint = new Point((Math.Abs(this.yTemp[c]) * a / b) - Math.Abs(this.xTemp), this.ImageWidth);
        //                    }
        //                }
        //                // Starting point is above of Bitmap/*
        //                else if (this.yTemp[c] > this.ImageWidth)
        //                {
        //                    if (x >= 0 && x <= this.ImageHeight)
        //                    {
        //                        myLineGeometry.EndPoint = new Point(x, this.ImageWidth - y);
        //                        secondEllipseGeometry.Center = myLineGeometry.EndPoint;
        //                        gg.Children.Add(secondEllipseGeometry);
        //                    }
        //                    else if (x > this.ImageWidth)
        //                    {
        //                        myLineGeometry.EndPoint = new Point(this.ImageHeight, this.ImageWidth - ((x - this.ImageHeight) * b / a) - y);
        //                    }
        //                    myLineGeometry.StartPoint = new Point((this.xTemp + (this.yTemp[c] - this.ImageWidth) * a / b), 0);
        //                }
        //                // Starting point is on the left side of Bitmap
        //                else
        //                {
        //                    if (this.xTemp < 0 && yTemp[c] <= y)
        //                    {
        //                        myLineGeometry.StartPoint = new Point(0, this.ImageWidth - (Math.Abs(this.xTemp) * b / a) - this.yTemp[c]);
        //                        myLineGeometry.EndPoint = new Point(x, this.ImageWidth - y);
        //                        secondEllipseGeometry.Center = myLineGeometry.EndPoint;
        //                        gg.Children.Add(secondEllipseGeometry);
        //                    }
        //                    if (this.xTemp < 0 && yTemp[c] > y)
        //                    {
        //                        myLineGeometry.StartPoint = new Point(0, this.ImageWidth - (x * b / a) - y);
        //                        myLineGeometry.EndPoint = new Point(x, this.ImageWidth - y);
        //                        secondEllipseGeometry.Center = myLineGeometry.EndPoint;
        //                        gg.Children.Add(secondEllipseGeometry);
        //                    }
        //                    // the last point is on the right side of Image
        //                    if (x >= this.ImageHeight)
        //                    {
        //                        myLineGeometry.StartPoint = new Point(this.xTemp, this.ImageWidth - this.yTemp[c]);
        //                        if (this.yTemp[c] == y)
        //                        {
        //                            myLineGeometry.EndPoint = new Point(this.ImageHeight, this.ImageWidth - y);  
        //                        }
        //                        if (this.yTemp[c] < y && this.yTemp[c] >= 0)
        //                        {
        //                            myLineGeometry.EndPoint = new Point(this.ImageHeight, this.ImageWidth - ((this.ImageHeight - this.xTemp) * b / a) - this.yTemp[c]);
        //                        }
        //                        if (this.yTemp[c] > y)
        //                        {
        //                            myLineGeometry.EndPoint = new Point(this.ImageHeight, this.ImageWidth - ((x - this.ImageHeight) * b / a) - y);   
        //                        }
        //                    }
        //                }
        //            }
        //            // paint the line, if there is mind. 2 Points
        //        }
        //        // endpoint outside of the ImageWidth
        //        if (y < 0 || y > this.ImageWidth)
        //        {
        //            // endpoint ist oben rechts von visualen Bitmap
        //            if (x > this.ImageHeight && y > this.ImageWidth)
        //            {
        //                // Letzte Punkt liegt auf den vertikale Achse
        //                if ((Math.Abs((double)(this.ImageWidth - y) / (this.ImageWidth - this.yTemp[c]))) < (Math.Abs((double)(x - this.ImageHeight) / (this.ImageHeight - this.xTemp))))
        //                {
        //                    myLineGeometry.EndPoint = new Point(this.ImageHeight, this.ImageWidth - this.yTemp[c] - (this.ImageHeight - this.xTemp) * b / a);
        //                }
        //                // Letzte Punkt liegt auf den horizontale Achse (obere Achse)
        //                if ((Math.Abs((double)(this.ImageWidth - y) / (this.ImageWidth - this.yTemp[c]))) > (Math.Abs((double)(x - this.ImageHeight) / (this.ImageHeight - this.xTemp))))
        //                {
        //                    myLineGeometry.EndPoint = new Point(this.xTemp + a * (this.ImageWidth - this.yTemp[c]) / b, 0);
        //                }
        //                myLineGeometry.StartPoint = new Point(this.xTemp, this.ImageWidth - this.yTemp[c]);
        //                firstEllipseGeometry.Center = myLineGeometry.StartPoint;
        //            }
        //            // endpoint ist unten rechts von visualen Bitmap
        //            else if (x > this.ImageHeight && y < 0)
        //            {
        //                // Letzte Punkt liegt auf den horizontale Achse(untere Achse)
        //                if ((Math.Abs((double)(y / this.yTemp[c]))) > (Math.Abs((double)(x / this.xTemp))))
        //                {
        //                    myLineGeometry.EndPoint = new Point(this.xTemp + a * this.yTemp[c] / b, this.ImageWidth);
        //                }
        //                // Letzte Punkt liegt auf den vertikale Achse 
        //                if ((Math.Abs((double)(y / this.yTemp[c]))) < (Math.Abs((double)(x / this.xTemp))))
        //                {
        //                    myLineGeometry.EndPoint = new Point(this.ImageHeight, this.ImageWidth - this.yTemp[c] + b * (this.ImageHeight - this.xTemp) / a);
        //                }
        //                myLineGeometry.StartPoint = new Point(this.xTemp, this.ImageWidth - this.yTemp[c]);
        //                firstEllipseGeometry.Center = myLineGeometry.StartPoint;
        //            }
        //            // endpoint ist entweder oben oder unten von Bitmap
        //            else
        //            {
        //                // steigende Linie
        //                if (this.yTemp[c] < y)
        //                {
        //                    // endpoint ist oben, startpoint links
        //                    if (this.xTemp < 0 && this.yTemp[c] < this.ImageWidth)
        //                    {
        //                        myLineGeometry.StartPoint = new Point(0, this.ImageWidth - Math.Abs(xTemp) * b / a - this.yTemp[c]);
        //                        myLineGeometry.EndPoint = new Point(x - (y - this.ImageWidth) * a / b, 0);
        //                        myLineGeometry.StartPoint = myLineGeometry.StartPoint;
        //                        myLineGeometry.EndPoint = myLineGeometry.EndPoint;
        //                    }
        //                    // endpoint oben
        //                    else
        //                    {
        //                        // startpoint innerhalb von Bitmap
        //                        if (yTemp[c] >= 0 && yTemp[c] < this.ImageWidth)
        //                        {
        //                            myLineGeometry.StartPoint = new Point(this.xTemp, this.ImageWidth - this.yTemp[c]);
        //                            myLineGeometry.EndPoint = new Point(this.xTemp + a * (this.ImageWidth - this.yTemp[c]) / b, 0);
        //                            firstEllipseGeometry.Center = myLineGeometry.StartPoint;
        //                            gg.Children.Add(firstEllipseGeometry);
        //                        }
        //                        // startpoint ist unten von Bitmap
        //                        else if (yTemp[c] < 0)
        //                        {
        //                            myLineGeometry.StartPoint = new Point(this.xTemp + a * Math.Abs(this.yTemp[c]) / b, this.ImageWidth);
        //                            myLineGeometry.EndPoint = new Point(this.xTemp + a * (this.ImageWidth - this.yTemp[c]) / b, 0);
        //                        }
        //                    }
        //                }
        //                // absteigende Linie
        //                else if (this.yTemp[c] > y)
        //                {
        //                    myLineGeometry.StartPoint = new Point(this.xTemp, this.ImageWidth - this.yTemp[c]);
        //                    myLineGeometry.EndPoint = new Point(x, this.ImageWidth - y);
        //                    firstEllipseGeometry.Center = myLineGeometry.StartPoint;
        //                    gg.Children.Add(firstEllipseGeometry);
        //                    //// startpoint ist links von Bitmap
        //                    //if (this.xTemp < 0 && this.yTemp[c] > 0)
        //                    //{
        //                    //    myLineGeometry.StartPoint = new Point(0, this.ImageWidth - this.yTemp[c] + Math.Abs(xTemp) * b / a);
        //                    //    myLineGeometry.EndPoint = new Point(this.xTemp + a * (this.yTemp[c]) / b, this.ImageWidth);
        //                    //    firstEllipseGeometry.Center = myLineGeometry.StartPoint;
        //                    //    gg.Children.Add(firstEllipseGeometry);
        //                    //}
        //                    //else
        //                    //{
        //                    //    myLineGeometry.StartPoint = new Point(this.xTemp, this.ImageWidth - this.yTemp[c]);
        //                    //    myLineGeometry.EndPoint = new Point(this.xTemp + a * (this.yTemp[c]) / b, this.ImageWidth);
        //                    //    firstEllipseGeometry.Center = myLineGeometry.StartPoint;
        //                    //    gg.Children.Add(firstEllipseGeometry);
        //                    //}
        //                }
        //            }
        //        }
        //        // paint the line, if there is mind. 2 Points            
        //    }
        //    if (k > 0)
        //    {
        //        gg.Children.Add(myLineGeometry);
        //    }
        //    this.geometrygroup.Children.Add(gg);
        //    this.xTemp = x;
        //    this.yTemp[c] = y;
        //}

		public void Peak(int pixelrow, SampleD sampledata)
		{
			int swap, xstart, xend;

    		xstart = (int)((sampledata.max - Offset) * Scale);
            xend = (int)((sampledata.min - Offset) * Scale);

			if (xstart > xend)
			{
				swap = xend;
				xend = xstart;
				xstart = swap;
			}

			// stetige plots
			if (this.xlastplot < xstart)
			{
				xstart = this.xlastplot;
			}

			if (this.xlastplot > xend)
			{
				xend = this.xlastplot;
			}

            this.xlastplot = (int)((sampledata.value - this.Offset) * this.Scale);

			this.ClippingAndPixeling(xstart, xend, pixelrow);
		}

		// das ist genau der gleiche code wie Peak().
		// wird nur in EventValueRenderer benutzt.
		// malte wollte halt noch marker bei den value signalen, damit man sieht wo ein sample ist.
		// die marker werden nur gemalt, wenn kein sprung da ist, bei dem man das neue sample bemerken kann.
		public void PeakMark(int pixelrow, SampleD sampledata)
		{
			int swap, xstart, xend;

            xstart = (int)((sampledata.max - Offset) * Scale);
            xend = (int)((sampledata.min - Offset) * Scale);

			if (xstart > xend)
			{
				swap = xend;
				xend = xstart;
				xstart = swap;
			}

			// stetige plots
			if (this.xlastplot < xstart)
			{
				xstart = this.xlastplot;
			}

			if (this.xlastplot > xend)
			{
				xend = this.xlastplot;
			}

			this.xlastplot = (int)((sampledata.value - this.Offset) * this.Scale);

			// marker für die value signale
			if (xstart == xend)
			{
				xstart -= 3;
				xend += 3;
			}

			this.ClippingAndPixeling(xstart, xend, pixelrow);
		}

		public void Stem(int pixelrow, float amplitude)
		{
			int swap, xstart, xend;

            xstart = (int)((amplitude - Offset) * this.Scale);
			xend = -(int)(this.Offset * this.Scale);

			if (xstart > xend)
			{
				swap = xend;
				xend = xstart;
				xstart = swap;
			}

			this.ClippingAndPixeling(xstart, xend, pixelrow);
		}

		public override void ZoomInto(float min, float max)
		{
			// falls signal umgepolt war (scale negativ), dann auch umgepolt reinzoomen
			if (Math.Abs(max - min) > double.Epsilon)
			{
				if (this.Scale >= 0)
				{
					Scale = (this.ImageWidth - 1) / (max - min);
					Offset = min;
				}
				else
				{
					this.Scale = -(this.ImageWidth - 1) / (max - min);
					this.Offset = max;
				}
			}
			else
			{
				this.Scale = this.ImageWidth - 1;
				this.Offset = min - ((float)(this.ImageWidth >> 1) / this.Scale);
			}
		}

		protected override void UpdateRange(double scale)
		{
			this.Range = Math.Abs(scale) >= 10.0 * double.Epsilon ? this.ImageWidth / scale : 0.0;
		}

		private void ClippingAndPixeling(int xstart, int xend, int pixelrow)
		{
			int i = 0;
			int o = pixelrow * this.strideUInt32;

			if (xstart > this.ImageWidth)
			{
				xstart = this.ImageWidth;
			}

			if (xend >= this.ImageWidth)	
			{
				xend = this.ImageWidth - 1;
			}

			// pixeln
			for (; i < xstart; ++i)
			{
				this.argb[o] = 0;
				++o;
			}

			for (; i <= xend; ++i)
			{
				this.argb[o] = this.color;
				++o;
			}

			for (; i < this.ImageWidth; ++i)
			{
				this.argb[o] = 0;
				++o;
			}
		}
	}
}
