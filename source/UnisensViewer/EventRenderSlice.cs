using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;

namespace UnisensViewer
{
	public class EventRenderSlice : RasterRenderSlice
	{
        //private GeometryGroup	geometrygroup;
        //public GeometryDrawing geometrydrawing;
		private double					lasttextextent;

        public EventRenderSlice(Renderer renderer, int channel, string name, int imagewidth, int imageheight, string unit, XElement unisensnode)
			: base(renderer, channel, name, imagewidth, imageheight, unit, unisensnode)
		{
			Rect rect = new Rect(0.0, 0.0, imagewidth, imageheight);

			ImageDrawing imagedrawing = new ImageDrawing(WBmp, rect);

			this.geometrygroup = new GeometryGroup();
			MatrixTransform mt = new MatrixTransform(0.0, 1.0, -1.0, 0.0, imagewidth, 0.0);		// die -90° rotation vom signalstacker wieder rückgängig machen
			mt.Freeze();
			this.geometrygroup.Transform = mt;
			GeometryDrawing geometrydrawing = new GeometryDrawing(
				new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)(color >> 24), (byte)(color >> 16), (byte)(color >> 8), (byte)color)),
				null,
				this.geometrygroup);
            this.geometrydrawing = geometrydrawing;
			DrawingGroup drawinggroup = new DrawingGroup();
			drawinggroup.Children.Add(imagedrawing);
			drawinggroup.Children.Add(geometrydrawing);

			drawinggroup.ClipGeometry = new RectangleGeometry(rect);

			this.imagesource = new DrawingImage(drawinggroup);
		}

		public void Print(int pixelrow, Geometry geometry)
		{
			double x = pixelrow;

			// falls ganz nah rangezoomt wird, dann ein paar textstrings
			// auslassen, so dass nichts übereinandergemalt wird
			if (x >= this.lasttextextent)
			{
				this.lasttextextent = x + geometry.Bounds.Width + 30.0;	// die textstrings sollen auch noch mindestens 5 pixel auseinander sein

				GeometryGroup gg = new GeometryGroup();
				gg.Children.Add(geometry);

				if (Scale >= 0.0f)
				{
                    gg.Transform = new TranslateTransform(x, (Offset * Scale) + ImageWidth);
				}
				else
				{
                    gg.Transform = new TranslateTransform(x, ImageWidth + (Offset * Scale) - geometry.Bounds.Bottom - geometry.Bounds.Y);
				}

				this.geometrygroup.Children.Add(gg);
			}
		}

		public void Clear()
		{
			this.lasttextextent = 0.0;

			this.Clear(0, ImageHeight);
			this.geometrygroup.Children.Clear();
		}

		public override void ZoomInto(float min, float max)
		{
			/*
			Scale = (float)imagewidth / 10.0f;
			Offset = 20.0f;
			*/
			Scale = (float)ImageWidth / 10.0f;
			Offset = -1.5f;
		}

		#region old_code
		/*
		public void Print(int pixelrow, string text)
		{
			FormattedText ft = new FormattedText(text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, SystemFonts.MessageFontSize, Brushes.White);

			geometrygroup.Children.Add(ft.BuildGeometry(new Point(pixelrow, 0)));
		}
		*/
		#endregion
	}
}
