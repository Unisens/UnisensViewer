using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;



namespace UnisensViewerLibrary
{




	public class MeasurementEntry : Entry
	{




		public static int GetNumChannels(XElement measuremententry)
		{
			int ret;

			IEnumerable<XElement> channels = measuremententry.Elements("{http://www.unisens.org/unisens2.0}channel");

			if ((ret = channels.Count()) > 0)
				return ret;
			else
				throw new Exception("Keine Kanäle zum Anzeigen.");
		}





		public static int GetChannelNum(XElement channel)
		{
			XElement mentry = channel.Parent;

			if (mentry != null)
			{
				IEnumerable<XElement> es = mentry.Elements("{http://www.unisens.org/unisens2.0}channel");

				int a = 0;

				foreach (XElement e in es)
				{
					if (e == channel)
						return a;

					++a;
				}
			}

			return -1;
		}





		public static UnisensDataType GetDataType(XElement measuremententry)
		{
			UnisensDataType		udt;
			XAttribute			datatype = measuremententry.Attribute("dataType");


			switch (datatype.Value.ToLowerInvariant())
			{
				case "double":	udt = UnisensDataType.Double;	break;
				case "float":	udt = UnisensDataType.Float;	break;
				case "uint8":	udt = UnisensDataType.UInt8;	break;
				case "int8":	udt = UnisensDataType.Int8;		break;
				case "uint16":	udt = UnisensDataType.UInt16;	break;
				case "int16":	udt = UnisensDataType.Int16;	break;
				case "uint32":	udt = UnisensDataType.UInt32;	break;
				case "int32":	udt = UnisensDataType.Int32;	break;
				case "uint64":	udt = UnisensDataType.UInt64;	break;
				case "int64":	udt = UnisensDataType.Int64;	break;
				default:		udt = UnisensDataType.Invalid;	break;
			}

			return udt;
		}





		public static double GetSampleRate(XElement measuremententry)
		{
			XAttribute samplerate = measuremententry.Attribute("sampleRate");
            if (samplerate == null) throw new Exception("Samplerate muss gesetzt sein.");

			double s = double.Parse(samplerate.Value, CultureInfo.InvariantCulture.NumberFormat);

			if (s > 0)
				return s;
			else if (s < 0)
				return -1.0 / s;	// negative sampleraten werden als bruch interpretiert
			else
				throw new Exception("Samplerate darf nicht 0 sein.");
		}





		public static XElement GetChannel(XElement measuremententry, int channelnum)
		{
			IEnumerable<XElement> channels = measuremententry.Elements("{http://www.unisens.org/unisens2.0}channel");
			return channels.ElementAt(channelnum);
		}





		public static string GetChannelName(XElement measuremententry, int channelnum)
		{
			IEnumerable<XElement> channels = measuremententry.Elements("{http://www.unisens.org/unisens2.0}channel");
			XElement ch = channels.ElementAt(channelnum);

			XAttribute name = ch.Attribute("name");
			XAttribute id = measuremententry.Attribute("id");

			// id ist != null, sonst hätte das Objekt gar nicht erzeugt werden können (throw im Constructor)

			if (name != null)
				return id.Value + " (" + name.Value + ")";
			else
				return id.Value + " (#" + channelnum + ")";
		}





		public static string GetUnit(XElement measuremententry)
		{
			XAttribute unit = measuremententry.Attribute("unit");

			if (unit != null)
				return unit.Value;
			else
				return null;
		}





		public static double GetLsbValue(XElement measuremententry)
		{
			try
			{
				XAttribute lsbvalue = measuremententry.Attribute("lsbValue");
				return double.Parse(lsbvalue.Value, CultureInfo.InvariantCulture.NumberFormat);
			}
			catch
			{
				return 1.0;
			}
		}





		public static double GetBaseline(XElement measuremententry)
		{

				XAttribute baseline = measuremententry.Attribute("baseline");
                return baseline != null ? double.Parse(baseline.Value, CultureInfo.InvariantCulture.NumberFormat) : 0.0;

		}

	}


}
