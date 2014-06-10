using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnisensViewer
{
	// Die Farben aus dem ColorFade sind doch ein bissel stressig bzw. beißen sich mit dem
	// grauen Hintergrund (TFTs sind halt so ne Sache...).
	public static class ColorRelaxed
	{
		private static double h;

		static ColorRelaxed()
		{
			Random r = new Random();
			h = r.NextDouble() * 360.0;
		}

		public static uint GetNextColor()
		{
			double rd, gd, bd;

			HsvToRgb(h, 0.5, 1.0, out rd, out gd, out bd);

			h += 42.0;

			uint r = (uint)(rd * 255.0);
			uint g = (uint)(gd * 255.0);
			uint b = (uint)(bd * 255.0);

			return (r << 8 | g) << 8 | b | 0xff000000;
		}

		private static void HsvToRgb(double h360, double s, double v, out double r, out double g, out double b)
		{
			double h = h360 / 60.0;
			double i = Math.Floor(h);
			double f = h - i;

			double m = v * (1.0 - s);
			double k = v * (1.0 - ((1.0 - f) * s));
			double n = v * (1.0 - (s * f));

			switch ((int)i % 6)
			{
				case 0:
					r = v;
					g = k;
					b = m;
					return;
				case 1:
					r = n;
					g = v;
					b = m;
					return;
				case 2:
					r = m;
					g = v;
					b = k;
					return;
				case 3:
					r = m;
					g = n;
					b = v;
					return;
				case 4:
					r = k;
					g = m;
					b = v;
					return;
				case 5:
				default:
					r = v;
					g = m;
					b = n;
					return;
			}
		}
	}
}
