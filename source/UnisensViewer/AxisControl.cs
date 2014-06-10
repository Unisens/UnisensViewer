using System;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using UnisensViewerLibrary;

namespace UnisensViewer
{
	public class AxisControl : FrameworkElement
	{
		public static readonly DependencyProperty	BaseUnitProperty		= DependencyProperty.Register("BaseUnit", typeof(string), typeof(AxisControl));
		public static readonly DependencyProperty	RangeProperty			= DependencyProperty.Register("Range", typeof(double), typeof(AxisControl), new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback(VisibleRangePropertyChangedCallback)));
		public static readonly DependencyProperty	OffsetProperty			= DependencyProperty.Register("Offset", typeof(double), typeof(AxisControl), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
		public static readonly DependencyProperty	ForegroundProperty		= DependencyProperty.Register("Foreground", typeof(Brush), typeof(AxisControl), new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback(ForegroundPropertyChangedCallback)));

		private Scale				scale;
		private Typeface			typeface;
		private Pen					pen;
        private string              timeFormat = "Duration";

		public AxisControl()
		{
			this.typeface = new Typeface(SystemFonts.MessageFontFamily, SystemFonts.MessageFontStyle, SystemFonts.MessageFontWeight, FontStretches.Normal);

			Height = 20.0;
			ClipToBounds = true;

			this.Loaded += new RoutedEventHandler(this.AxisControl_Loaded);
		}

		public string BaseUnit
		{
			get { return (string)GetValue(BaseUnitProperty); }
			set { SetValue(BaseUnitProperty, value); }
		}

		public double Range
		{
			get { return (double)GetValue(RangeProperty); }
			set { SetValue(RangeProperty, value); }
		}

		public double Offset
		{
			get { return (double)GetValue(OffsetProperty); }
			set { SetValue(OffsetProperty, value); }
		}

		public Brush Foreground
		{
			get { return (Brush)GetValue(ForegroundProperty); }
			set { SetValue(ForegroundProperty, value); }
		}

        // alte OnRender
        //protected override void OnRender(DrawingContext drawingContext)
        //{
        //    if (this.scale != null)	
        //    {	// ist irgendwann bei der initialisierung null (onrender vor loaded event)
        //        Brush fbrush = this.Foreground;

        //        // ** verschiebung um 0.5:
        //        //    WPF zeichnet die linien zwischen die pixel und nicht auf die pixel.
        //        //    die signaldarstellungen sind aber auf den pixel.
        //        double width = ActualWidth + 0.5;	// **

        //        double dipsperdegree = Math.Abs(this.ActualWidth / this.scale.NormalizedRange);
        //        double offs = this.Offset * Math.Abs(this.scale.NormalizedRange) / this.scale.Range;	// VisibleRange darf nicht 0 sein!

        //        double o = Math.Ceiling(offs);
        //        double x = ((o - offs) * dipsperdegree) + 0.5;	// **

        //        string s;
        //        double step;

        //        double lasttextextent = 0.0;

        //        if (this.scale.PrettyUnit == null)
        //        {
        //            if (!string.IsNullOrEmpty(this.scale.NormalizedFactors))
        //            {
        //                s = "\u00b7" + this.scale.NormalizedFactors + " " + this.scale.NormalizedUnit;
        //            }
        //            else
        //            {
        //                s = " " + this.scale.NormalizedUnit;
        //            }

        //            step = 1.0;
        //        }
        //        else
        //        {
        //            s = " " + this.scale.PrettyUnit;
        //            step = this.scale.PrettyFactor;
        //            o *= this.scale.PrettyFactor;
        //        }

        //        if (dipsperdegree >= 2.0)	
        //        {	// zu fein für linien
        //            while (x < width)		
        //            {
        //                drawingContext.DrawLine(this.pen, new Point(x, 10.0), new Point(x, 15.0));

        //                if (x >= lasttextextent)	
        //                {	// evtl. ein paar zahlen auslassen (je nach zoom)
        //                    FormattedText ft = new FormattedText(o.ToString() + s, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, this.typeface, SystemFonts.MessageFontSize, fbrush);
        //                    drawingContext.DrawText(ft, new Point(x + 1.0, 0.0));
        //                    lasttextextent = x + ft.Width + 120.0;	// mindestens 30 pixel abstand
        //                }

        //                x += dipsperdegree;
        //                o += step;
        //            }
        //        }
        //        else
        //        {
        //            // wenigstens text hinschreiben, sonst sieht man gar nix
        //            while (x < width)	
        //            {
        //                if (x >= lasttextextent)	
        //                {	// evtl. ein paar zahlen auslassen (je nach zoom)
        //                    FormattedText ft = new FormattedText(o.ToString() + s, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, this.typeface, SystemFonts.MessageFontSize, fbrush);
        //                    drawingContext.DrawText(ft, new Point(x + 1.0, 0.0));
        //                    lasttextextent = x + ft.Width + 120.0;	// mindestens 30 pixel abstand
        //                }

        //                x += dipsperdegree;
        //                o += step;
        //            }
        //        }
				
        //        /////////////////////////////////
        //        if (this.scale.DownTicks > 0.0)
        //        {
        //            double dipsperdegreeDown = dipsperdegree / this.scale.DownTicks;
        //            if (dipsperdegreeDown >= 2.0)
        //            {
        //                double offsDown = offs * this.scale.DownTicks;
        //                x = ((Math.Ceiling(offsDown) - offsDown) * dipsperdegreeDown) + 0.5;		// **

        //                while (x < width)	
        //                {
        //                    drawingContext.DrawLine(this.pen, new Point(x, 15.0), new Point(x, 20.0));
        //                    x += dipsperdegreeDown;
        //                }
        //            }
        //        }

        //        ////////////////////////////////////////////////
        //        if (this.scale.UpTicks > 0.0)
        //        {
        //            double dipsperdegreeUp = dipsperdegree * this.scale.UpTicks;
        //            if (dipsperdegreeUp >= 2.0)
        //            {
        //                double offs_up = offs / this.scale.UpTicks;
        //                x = ((Math.Ceiling(offs_up) - offs_up) * dipsperdegreeUp) + 0.5;	// **

        //                while (x < width)	
        //                {
        //                    drawingContext.DrawLine(this.pen, new Point(x, 5.0), new Point(x, 10.0));
        //                    x += dipsperdegreeUp;
        //                }
        //            }
        //        }
        //    }
        //}

        // neue OnRender mit Anzeigen der absoluten Uhrzeit
        protected override void OnRender(DrawingContext drawingContext)
        {    
            if (this.scale != null)
            {	// ist irgendwann bei der initialisierung null (onrender vor loaded event)
                Brush fbrush = this.Foreground;

                // ** verschiebung um 0.5:
                //    WPF zeichnet die linien zwischen die pixel und nicht auf die pixel.
                //    die signaldarstellungen sind aber auf den pixel.
                double width = ActualWidth + 0.5;	// **

                double dipsperdegree = Math.Abs(this.ActualWidth / this.scale.NormalizedRange);
                double offs = this.Offset * Math.Abs(this.scale.NormalizedRange) / this.scale.Range;	// VisibleRange darf nicht 0 sein!

                double o = Math.Ceiling(offs);
                double x = ((o - offs) * dipsperdegree) + 0.5;	// **

                string s;
                double step;

                double lasttextextent = 0.0;

                if (this.scale.PrettyUnit == null)
                {
                   if (!string.IsNullOrEmpty(this.scale.NormalizedFactors))
                   {
                       s = "\u00b7" + this.scale.NormalizedFactors + " " + this.scale.NormalizedUnit;
                   }
                   else
                   {
                       s = " " + this.scale.NormalizedUnit;
                   }

                   step = 1.0;
                }
                else
                {
                    s = " " + this.scale.PrettyUnit;
                    step = this.scale.PrettyFactor;
                    o *= this.scale.PrettyFactor;
                }
                // die Nummer von Pixel pixelToAdd, an die die Uhrzeit bewegt werden soll
                int pixelToAdd = AddPixel(this.scale.Range, this.ActualWidth, this.scale.NormalizedUnit, this.scale.PrettyUnit, this.scale.PrettyFactor);
                string absoluteTime = null;
                if (dipsperdegree >= 2.0)
                {	// zu fein für linien
                    while (x < width)
                    {                       
                        FormattedText ft = null;
                        // Uhrzeit zum Anzeigen
                        absoluteTime = ToSecondConvert(this.scale.NormalizedUnit, this.scale.PrettyUnit, o, this.scale.PrettyFactor);
                        if (x >= lasttextextent)
                        {	// evtl. ein paar zahlen auslassen (je nach zoom)
                            if (timeFormat == "Duration")
                            {
                                ft = new FormattedText(o.ToString() + s, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, this.typeface, SystemFonts.MessageFontSize, fbrush);
                                drawingContext.DrawLine(this.pen, new Point(x, 10.0), new Point(x, 15.0));
                                drawingContext.DrawText(ft, new Point(x + 1.0, 0.0));
                                lasttextextent = x + ft.Width + 30.0;	// mindestens 30 pixel abstand
                            }
                            else
                            {
                                if (absoluteTime != null)
                                {
                                    ft = new FormattedText(absoluteTime, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, this.typeface, SystemFonts.MessageFontSize, fbrush);
                                    drawingContext.DrawLine(this.pen, new Point(x + pixelToAdd, 10.0), new Point(x + pixelToAdd, 15.0));
                                    //drawingContext.DrawLine(this.pen, new Point(x + pixelToAdd, 10.0), new Point(x + pixelToAdd, 20.0));
                                    drawingContext.DrawText(ft, new Point(x + 1.0 + pixelToAdd, 0.0));
                                    lasttextextent = x + ft.Width + 30.0;	// mindestens 30 pixel abstand
                                } 
                            }          
                        }

                        x += dipsperdegree;
                        o += step;
                    }
                }
                else
                {
                    // wenigstens text hinschreiben, sonst sieht man gar nix
                    while (x < width)
                    {
                        FormattedText ft = null;
                        // Uhrzeit zum Anzeigen
                        absoluteTime = ToSecondConvert(this.scale.NormalizedUnit, this.scale.PrettyUnit, o, this.scale.PrettyFactor);
                        if (x >= lasttextextent)
                        {	// evtl. ein paar zahlen auslassen (je nach zoom)
                            if (timeFormat == "Duration")
                            {
                                ft = new FormattedText(o.ToString() + s, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, this.typeface, SystemFonts.MessageFontSize, fbrush);
                                drawingContext.DrawLine(this.pen, new Point(x, 10.0), new Point(x, 15.0));
                                drawingContext.DrawText(ft, new Point(x + 1.0, 0.0));
                                lasttextextent = x + ft.Width + 30.0;	// mindestens 30 pixel abstand
                            }
                            else
                            {
                                if (absoluteTime != null)
                                {
                                    ft = new FormattedText(absoluteTime, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, this.typeface, SystemFonts.MessageFontSize, fbrush);
                                    drawingContext.DrawText(ft, new Point(x + 1.0 + pixelToAdd, 0.0));
                                    lasttextextent = x + ft.Width + 30.0;	// mindestens 30 pixel abstand
                                } 
                            }
                        }

                        x += dipsperdegree;
                        o += step;
                    }
                }

                /////////////////////////////////
                if (this.scale.DownTicks > 0.0)
                {
                    double dipsperdegreeDown = dipsperdegree / this.scale.DownTicks;
                    if (dipsperdegreeDown >= 2.0)
                    {
                        double offsDown = offs * this.scale.DownTicks;
                        x = ((Math.Ceiling(offsDown) - offsDown) * dipsperdegreeDown) + 0.5;		// **

                        if (timeFormat == "Absolute")
                        {
                            double x1 = x + pixelToAdd + 0.5;
                            while (x1 > 0)
                            {
                                drawingContext.DrawLine(this.pen, new Point(x1, 15.0), new Point(x1, 20.0));
                                x1 -= dipsperdegreeDown;
                            }
                        }

                        while (x < width)
                        {
                            if (timeFormat == "Duration")
                            {
                                drawingContext.DrawLine(this.pen, new Point(x, 15.0), new Point(x, 20.0));                                
                            }
                            else
                            {
                                drawingContext.DrawLine(this.pen, new Point(x + pixelToAdd, 15.0), new Point(x + pixelToAdd, 20.0));
                            }
                            x += dipsperdegreeDown;
                        }
                    }
                }

                ////////////////////////////////////////////////
                if (this.scale.UpTicks > 0.0)
                {
                    double dipsperdegreeUp = dipsperdegree * this.scale.UpTicks;
                    if (dipsperdegreeUp >= 2.0)
                    {
                        double offs_up = offs / this.scale.UpTicks;
                        x = ((Math.Ceiling(offs_up) - offs_up) * dipsperdegreeUp) + 0.5;	// **

                        while (x < width)
                        {
                            if (timeFormat == "Duration")
                            {
                                drawingContext.DrawLine(this.pen, new Point(x, 5.0), new Point(x, 10.0));
                            }
                            else
                            {
                                drawingContext.DrawLine(this.pen, new Point(x + pixelToAdd, 5.0), new Point(x + pixelToAdd, 10.0));
                            }
                            x += dipsperdegreeUp;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// berechnet der Pixelanzahl, die verschieben werden soll
        /// </summary>
        /// <param name="Range">Range der Pane</param>
        /// <param name="ActualWidth">Die Breite der Anzeigenbereich in Pixel</param>
        /// <param name="NormalizedUnit">Units von der pane (ms, s, min, hrs, d)</param>
        /// <param name="PrettyUnit">Units von der Zoom-pane, wenn die Unit von der pane als s ist</param>
        /// <param name="PrettyFactor">der Faktor von der Zoom-pane (10 ms oder 100 ms)</param>
        /// <returns></returns>
        private int AddPixel(double Range, double ActualWidth, string NormalizedUnit, string PrettyUnit, double PrettyFactor)
        {
            XDocument unisensxml = UnisensXmlFileManager.CurrentUnisensInstance.Xdocument;
            int pixelToAdd = 0;
            double milisecond = 0;
            double second = 0;
            double minuten = 0;
            double hours = 0;
            if (unisensxml != null)
            {
                DateTime dateTime = Unisens.getTimestampNow(unisensxml, 0);
                if (dateTime.Millisecond > 0)
                {
                    milisecond = (double)(1000 - dateTime.Millisecond) / 1000;
                }

                if (dateTime.Second > 0)
                {
                    if (milisecond != 0)
                    {
                        second = 60 - dateTime.Second - 1;
                    }
                    else
                    {
                        second = 60 - dateTime.Second;
                    }
                }

                if (dateTime.Minute > 0)
                {
                    if (second != 0)
                    {
                        minuten = 60 - dateTime.Minute - 1;
                    }
                    else
                    {
                        minuten = 60 - dateTime.Minute;
                    }
                }

                if (dateTime.Hour > 0)
                {
                    if (minuten != 0)
                    {
                        hours = 24 - dateTime.Hour - 1;
                    }
                    else
                    {
                        hours = 24 - dateTime.Hour;
                    }
                }
                switch (NormalizedUnit)
                {
                    case "ms":
                        pixelToAdd = 0;
                        break;
                    case "min":
                        pixelToAdd = (int)(ActualWidth / Range * (milisecond + second));
                        break;
                    case "hrs":
                        pixelToAdd = (int)(ActualWidth / Range * (milisecond + second + minuten * 60));
                        break;
                    case "d":
                        pixelToAdd = (int)(ActualWidth / Range * (milisecond + second + minuten * 60 + hours * 3600));
                        break;
                    default:
                        if (PrettyUnit == "ms")
                        {
                            if (milisecond != 0)
                            {
                                if (PrettyFactor == 100)
                                {
                                    milisecond = (Math.Truncate(((1 - milisecond) * 1000 + PrettyFactor) / PrettyFactor)) / 10;
                                }
                                else if (PrettyFactor == 10)
                                {
                                    milisecond = (Math.Truncate(((1 - milisecond) * 1000 + PrettyFactor) / PrettyFactor)) / 100;
                                }
                            }
                            pixelToAdd = (int)(ActualWidth * (milisecond - (double)dateTime.Millisecond/1000) / Range);
                        }
                        else
                        {
                            pixelToAdd = (int)(ActualWidth / Range * milisecond);
                        }
                        break;
                }
            }
            return pixelToAdd;
        }
        /// <summary>
        /// Umwandlung die Dauer der Messung in die genaue Uhrzeit
        /// </summary>
        /// <param name="NormalizedUnit">Units von der pane (ms, s, min, hrs, d)</param>
        /// <param name="PrettyUnit">Units von der Zoom-pane, wenn die Unit von der pane als s ist</param>
        /// <param name="o">Schritt der Zeichnung</param>
        /// <param name="PrettyFactor">der Faktor von der Zoom-pane (10 ms oder 100 ms)</param>
        /// <returns></returns>
        private static string ToSecondConvert(string NormalizedUnit, string PrettyUnit, double o, double PrettyFactor)
        {
            XDocument unisensxml = UnisensXmlFileManager.CurrentUnisensInstance.Xdocument;
            string absoluteTime = null;
            DateTime dateTime = DateTime.Now;
            double second = 0;
            double minuten = 0;
            double hours = 0;
            double milisecond = 0;


            if (unisensxml != null)
            {
                //milisecond = Unisens.getTimeMillisecond(unisensxml);
                dateTime = Unisens.getTimestampNow(unisensxml, 0);
                if (dateTime.Millisecond > 0)
                {
                    milisecond = (double)(1000 - dateTime.Millisecond) / 1000;
                }

                if (dateTime.Second > 0)
                {
                    if (milisecond != 0)
                    {
                        second = 60 - dateTime.Second - 1;
                    }
                    else
                    {
                        second = 60 - dateTime.Second;
                    }
                }

                if (dateTime.Minute > 0)
                {
                    if (second != 0)
                    {
                        minuten = 60 - dateTime.Minute - 1;
                    }
                    else
                    {
                        minuten = 60 - dateTime.Minute;
                    }
                }

                if (dateTime.Hour > 0)
                {
                    if (minuten != 0)
                    {
                        hours = 24 - dateTime.Hour - 1;
                    }
                    else
                    {
                        hours = 24 - dateTime.Hour;
                    }
                }
                
                switch (NormalizedUnit)
                {
                    case "ms":
                        dateTime = Unisens.getTimestampNow(unisensxml, o/1000);
                        absoluteTime = dateTime.ToLongTimeString() + "." + String.Format("{0:fff}", dateTime);
                        break;
                    case "min":
                        dateTime = Unisens.getTimestampNow(unisensxml, o * 60 + second + milisecond);
                        absoluteTime = dateTime.ToShortTimeString();
                        break;
                    case "hrs":                          
                        dateTime = Unisens.getTimestampNow(unisensxml, o * 3600 + minuten * 60 + second + milisecond);                    
                        absoluteTime = dateTime.ToShortTimeString() + " (" + dateTime.ToShortDateString() + ")";
                        break;
                    case "d":
                        dateTime = Unisens.getTimestampNow(unisensxml, o * 86400 + hours * 3600 + minuten * 60 +second + milisecond);
                        absoluteTime = dateTime.ToShortTimeString() + " (" + dateTime.ToShortDateString() + ")";
                        break;
                    default:
                        if (PrettyUnit == "ms")
                        {
                            if (PrettyFactor == 100)
                            {
                                if (milisecond != 0)
                                {
                                    // 12:23:13.581       (1-milisecond)*1000 = (1 - 0,849)*1000 = 581 
                                    milisecond = (Math.Truncate(((1-milisecond)*1000 + PrettyFactor) / PrettyFactor)) / 10;
                                }                        
                            }
                            else if (PrettyFactor == 10)
                            {
                                if (milisecond != 0)
                                {
                                    milisecond = (Math.Truncate(((1-milisecond)*1000 + PrettyFactor) / PrettyFactor)) / 100;
                                }
                            }
                            else
                            {
                                milisecond = 0;
                            }
                            dateTime = Unisens.getTimestampNow(unisensxml, o / 1000 + (milisecond - (double)dateTime.Millisecond/1000));
                            absoluteTime = dateTime.ToLongTimeString() + "." + String.Format("{0:fff}", dateTime); 
                        }
                        else
                        {                          
                            // Ex: 12:23:13.581  --> 12:23:14.000     "0 + (1 - 0,581) = 0,419"
                            dateTime = Unisens.getTimestampNow(unisensxml, o + milisecond);
                            absoluteTime = dateTime.ToLongTimeString();
                        }
                        break;
                }   
            }
            return absoluteTime;
        }

		private static void VisibleRangePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			AxisControl ac = (AxisControl)d;

			if (ac.scale != null)
			{
				ac.scale.Range = (double)e.NewValue;
			} 
		}

		private static void ForegroundPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			AxisControl ac = (AxisControl)d;
			ac.pen = new Pen(ac.Foreground, 1.0);
		}

		private void AxisControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (this.BaseUnit == "event")
			{
				this.Visibility = System.Windows.Visibility.Collapsed;
			}

			if (this.BaseUnit == string.Empty)
			{
				this.BaseUnit = "?";
			}

			this.scale = Units.GetScale(this.BaseUnit);
			this.scale.Range = this.Range;

			this.pen = new Pen(this.Foreground, 1.0);

			InvalidateVisual();
		}

        internal void AbsolutTime_Check()
        {
            timeFormat = "Absolute";
            InvalidateVisual();
        }

        internal void DurationMessung_Check()
        {
            timeFormat = "Duration";
            InvalidateVisual();
        }

        //internal void AbsolutDauerTime_Check()
        //{
        //    timeFormat = "Beides";
        //    InvalidateVisual();
        //}
    }
}

