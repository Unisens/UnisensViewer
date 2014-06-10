using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace UnisensViewerLibrary
{
    public class Unisens
    {
        public static string getTimestampStart(XDocument unisensxml)
        {
            XElement xUnisens = unisensxml.Root;
            XAttribute timestampStart = xUnisens.Attribute("timestampStart");
            if (timestampStart != null)
            {
                string timestampstart = xUnisens.Attribute("timestampStart").Value;
                return timestampstart;
            }
            else return null;
        }

        //public static string getTimestampNow(XDocument unisensxml, double seconds)
        //{
        //    string timestampStart = getTimestampStart(unisensxml);
        //    DateTime TimeStampStart = Convert.ToDateTime(timestampStart);
        //    DateTime dt = TimeStampStart.AddSeconds(seconds);
        //    string a = String.Format("{0:mm}", dt);
        //    string b = String.Format("{0:ss}", dt);
        //    string c = String.Format("{0:fff}", dt);
        //    String dateStr = dt.Hour + ":" + a + ":" + b + "." + c + " Uhr";
        //    return dateStr;
        //}

        public static DateTime getTimestampNow(XDocument unisensxml, double seconds)
        {
            string timestampStart = getTimestampStart(unisensxml);
            DateTime TimeStampStart = Convert.ToDateTime(timestampStart);
            DateTime dt = TimeStampStart.AddSeconds(seconds);
            return dt;
        }

        public static double getTimeMillisecond(XDocument unisensxml)
        {
            string timestampStart = getTimestampStart(unisensxml);
            DateTime TimeStampStart = Convert.ToDateTime(timestampStart);
            double milisecond = TimeStampStart.Millisecond;
            return milisecond;
        }

        public static XElement getElement(string entryId)
        {
            XDocument doc = XDocument.Load("unisens.xml");
            XElement xUnisens = doc.Root;
            IEnumerable<XElement> xelements = doc.Root.Elements("{http://www.unisens.org/unisens2.0}signalEntry");
            XElement Element = null;

            foreach (XElement el in xelements)
            {
                if (el.Attribute("id").Value == entryId)
                {
                    Element = el;
                }
            }
            return Element;
        }
   
    }
}
