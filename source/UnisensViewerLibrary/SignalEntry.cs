using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;
using UnisensViewerClrCppLibrary;

namespace UnisensViewerLibrary
{
	public class SignalEntry : MeasurementEntry
	{
		public static StreamDataType GetBinDataType(XElement signalentry)
		{
			bool			littleendian;
			StreamDataType	sdt;

			XElement binfileformat = signalentry.Element("{http://www.unisens.org/unisens2.0}binFileFormat");

			XAttribute endianess = binfileformat.Attribute("endianess");
			if (endianess != null)
				littleendian = endianess.Value.ToLowerInvariant() == "little";
			else
				littleendian = true;

			XAttribute datatype = signalentry.Attribute("dataType");
			if (datatype != null)
			{
				switch (datatype.Value.ToLowerInvariant())
				{
					case "quad": sdt = StreamDataType.Ieee754_Quad; break;
					case "half": sdt = StreamDataType.Ieee754r_Half; break;
					case "double": sdt = StreamDataType.Ieee754_Double; break;
					case "float": sdt = StreamDataType.Ieee754_Single; break;
					case "uint8": sdt = StreamDataType.Intel_UInt8; break;
					case "int8": sdt = StreamDataType.Intel_Int8; break;
					case "uint16": sdt = littleendian ? StreamDataType.Intel_UInt16 : StreamDataType.Motorola_UInt16; break;
					case "int16": sdt = littleendian ? StreamDataType.Intel_Int16 : StreamDataType.Motorola_Int16; break;
					case "uint32": sdt = littleendian ? StreamDataType.Intel_UInt32 : StreamDataType.Motorola_UInt32; break;
					case "int32": sdt = littleendian ? StreamDataType.Intel_Int32 : StreamDataType.Motorola_Int32; break;
					case "uint64": sdt = littleendian ? StreamDataType.Intel_UInt64 : StreamDataType.Invalid; break;
					case "int64": sdt = littleendian ? StreamDataType.Intel_Int64 : StreamDataType.Invalid; break;
					default:
						throw new Exception("Attribut dataType ungültig.");
						//sdt = StreamDataType.Invalid; break;
				}
			}
			else
			{
				XAttribute adcresolution = signalentry.Attribute("adcResolution");
				if (adcresolution == null)
					throw new Exception("Attribut dataType oder adcResolution muss vorhanden sein.");

				int bits = int.Parse(adcresolution.Value, CultureInfo.InvariantCulture.NumberFormat);

				if (bits > 64)
					throw new Exception("Attribut adcResolution ungültig.");
				else if (bits > 32)
					sdt = StreamDataType.Intel_Int64;
				else if (bits > 16)
					sdt = StreamDataType.Intel_Int32;
				else if (bits > 8)
					sdt = StreamDataType.Intel_Int16;
				else
					sdt = StreamDataType.Intel_Int8;
			}

			return sdt;
		}

		public static uint GetDataTypeBytes(StreamDataType d)
		{
			switch (d)
			{
				case StreamDataType.Intel_Int8:
				case StreamDataType.Intel_UInt8:
					return 1;

				case StreamDataType.Intel_Int16:
				case StreamDataType.Intel_UInt16:
				case StreamDataType.Motorola_Int16:
				case StreamDataType.Motorola_UInt16:
				case StreamDataType.Ieee754r_Half:
					return 2;

				case StreamDataType.Intel_Int32:
				case StreamDataType.Intel_UInt32:
				case StreamDataType.Motorola_Int32:
				case StreamDataType.Motorola_UInt32:
				case StreamDataType.Ieee754_Single:
					return 4;

				case StreamDataType.Intel_Int64:
				case StreamDataType.Intel_UInt64:
				case StreamDataType.Ieee754_Double:
					return 8;


				case StreamDataType.Ieee754_Quad:
					return 16;

				default:
					return 0;
			}
		}

	}


}
