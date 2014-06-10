


namespace UnisensViewerLibrary
{


	static class Motorola
	{
		// hier aufpassen mit den casts, wann sign-extended wird!!
		// zur info: für >> gibts zwei assembler befehle, den jeweils richtigen muss man erzwingen bzw. man muss
		// aufpassen, dass der compiler einem nicht die bits versaut...

		public static float Convert_Int16(ushort rawint16)
		{
			return (float)((short)((rawint16 << 8) | (rawint16 >> 8)));
		}

		public static float Convert_UInt16(ushort rawuint16)
		{
			return (float)((ushort)((rawuint16 << 8) | (rawuint16 >> 8)));
		}

		public static float Convert_Int32(uint rawint32)
		{
			return (float)((int)((rawint32 << 24) | (rawint32 >> 24) | (rawint32 << 8 & 0x00ff0000) | (rawint32 >> 8 & 0x0000ff00)));
		}

		public static float Convert_UInt32(uint rawuint32)
		{
			return (float)((uint)((rawuint32 << 24) | (rawuint32 >> 24) | (rawuint32 << 8 & 0x00ff0000) | (rawuint32 >> 8 & 0x0000ff00)));
		}
	}


}
