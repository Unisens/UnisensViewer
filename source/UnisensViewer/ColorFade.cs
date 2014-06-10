using System;

namespace UnisensViewer
{
	// Erster Versuch, automatisch Farben zu generieren.
	// Vorgegebene Standard-Farben sind doch auf Dauer langweilig...
	public static class ColorFade
	{
		private static uint[] lut = new uint[1536];
		private static Random rand = new Random();

		static ColorFade()
		{
			uint i, r, g, b;

			i = 0;

			r = 255;
			g = 0;
			b = 0;

			for (i = 0; i < 256; ++i)
			{
				lut[i] = (r << 8 | g++) << 8 | b | 0xff000000;
			}

			g = 255;

			for (; i < 512; ++i)
			{
				lut[i] = (r-- << 8 | g) << 8 | b | 0xff000000;
			}

			r = 0;

			for (; i < 768; ++i)
			{
				lut[i] = (r << 8 | g) << 8 | b++ | 0xff000000;
			}

			b = 255;

			for (; i < 1024; ++i)
			{
				lut[i] = (r << 8 | g--) << 8 | b | 0xff000000;
			}

			g = 0;

			for (; i < 1280; ++i)
			{
				lut[i] = (r++ << 8 | g) << 8 | b | 0xff000000;
			}

			r = 255;

			for (; i < 1536; ++i)
			{
				lut[i] = (r << 8 | g) << 8 | b-- | 0xff000000;
			}
		}

		public static uint GetNextColor()
		{
			return lut[rand.Next(1536)];
		}
	}
}
