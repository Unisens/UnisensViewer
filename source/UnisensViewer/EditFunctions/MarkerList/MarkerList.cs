using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using org.unisens;
using System.IO;
using UnisensViewerLibrary;
using System.Globalization;

namespace UnisensViewer
{
    public class MarkerList
    {
        public static IEnumerable<XElement> setMarkerList(string path)
        {
            List<XElement> returnElementList = new List<XElement>();
            char delim =';';
            List<Event> events = new List<Event>();
            long samplestamp;
            string type;
            string comment;
            string lines = null;
            string entryId = null;
            string textfeld = null;
            string comments = null;
            double sampleRate = 0;
            XDocument unisensxml = UnisensXmlFileManager.CurrentUnisensInstance.Xdocument;
            DialogMarkerList dialogMarkerList = new DialogMarkerList();
            dialogMarkerList.Topmost = true;
            
            if (dialogMarkerList.ShowDialog() != (DialogMarkerList.markerlist))
            {
                entryId = DialogMarkerList.entryId;
                textfeld = DialogMarkerList.textfeld;
                comments = DialogMarkerList.comment;
                sampleRate = DialogMarkerList.sampleRate;
            }
            path = path.Substring(0, path.Length - 11);
            StreamWriter myWriter = File.CreateText(path + entryId);
            myWriter.WriteLine(textfeld);
            myWriter.Close();
            //while ((lines = textfeld.ReadLine()) != null)
            //{
            //    samplestamp = Timestamp(delim, lines);
            //    type = Type(delim, lines);
            //    comment = Comment(delim, lines);
            //    events.Add(new Event(samplestamp, type, comment));
            //}

            
            //// Open the Unisens dataset and read essential parameters.
            //UnisensFactory factory = UnisensFactoryBuilder.createFactory();
            //org.unisens.Unisens unisens = factory.createUnisens(path);
            //org.unisens.EventEntry evententry = (org.unisens.EventEntry)unisens.createEventEntry(entryId, sampleRate);
            //evententry.append(events);

            XElement entryElement = new XElement("{http://www.unisens.org/unisens2.0}eventEntry",
                                        new XAttribute("id", entryId),
                                        new XAttribute("sampleRate", sampleRate),
                                        new XElement("{http://www.unisens.org/unisens2.0}csvFileFormat",
                                            new XAttribute("decimalSeparator", "."), new XAttribute("separator", ";")));
            unisensxml.Root.Add(entryElement);
            returnElementList.Add(entryElement);
            return returnElementList;
        }

        private static long Timestamp(char delim, string lines)
        {
            long timestamp = 0;
            int a = lines.IndexOf(delim);
            timestamp = long.Parse(lines.Substring(0, a), CultureInfo.InvariantCulture.NumberFormat);
            return timestamp;
        }

        private static string Type(char delim, string lines)
        {
            string type = null;
            int a = lines.IndexOf(delim);
            type = lines.Substring(a,lines.Length - a);
            return type;
        }

        private static string Comment(char delim, string lines)
        {
            string comment = null;
            int a = lines.IndexOf(delim);
            int b = lines.IndexOf(delim, a);
            comment = lines.Substring(b);
            return comment;
        }
    }
}
