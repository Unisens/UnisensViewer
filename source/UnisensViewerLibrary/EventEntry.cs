using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;




namespace UnisensViewerLibrary
{


	public class EventEntry : Entry
	{

		/*
		public static string GetCsvSeparator(XElement evententry)
		{
			XAttribute id = evententry.Attribute("id");
			return id.Value;
		}
		 */





		public static double GetSampleRate(XElement evententry)
		{
			return MeasurementEntry.GetSampleRate(evententry);
		}



	}


}
