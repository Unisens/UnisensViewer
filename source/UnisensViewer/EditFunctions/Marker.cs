using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using Microsoft.Win32;
using UnisensViewerLibrary;
namespace UnisensViewer
{
    public enum MarkerOperation{ Set, Delete, Truncate }
    public class Marker
    {
        private static char delim;
        public static IEnumerable<XElement> setMarker(IEnumerable<XElement> selectedsignals, double time_cursor, string markerSymbol)
        {
            return main(selectedsignals, time_cursor, 0, 0, markerSymbol, MarkerOperation.Set);
        }

        public static void deleteMarker(IEnumerable<XElement> selectedsignals, double time_start, double time_end)
        {
            main(selectedsignals, 0, time_start, time_end, "M", MarkerOperation.Delete);
        }

        public static void truncateMarker(IEnumerable<XElement> selectedsignals, double time_cursor)
        {
            main(selectedsignals, time_cursor, 0, 0, "M", MarkerOperation.Truncate);
        }

        /// - Hotkey muss mit SelectedSignals="StackFiles" konfiguriert sein!
        ///    => selectedsignals enthält alle Signale im Stapel
        /// - parameter == "truncate" für truncate-Funktion
        /// <summary>
        /// Main function for plug-ins, called by UnisensViewer.
        /// </summary>
        /// <param name="unisensxml">unisens.xml file.</param>
        /// <param name="selectedsignals">All information from unisens.xml of the selected signals.</param>
        /// <param name="path">Path of the current unisens.xml file.</param>
        /// <param name="time_cursor">Time in seconds of current cursor position. Is 0, if the plug-in is called via plug-in menu.</param>
        /// <param name="time_start">Time in seconds of start of the current selection. Is 0, when no selection exists.</param>
        /// <param name="time_end">Time in seconds of end of the current selection. Is 0, when no selection exists.</param>
        /// <param name="parameter">Additional parameter of the key bindings.</param>
        /// <returns>
        /// Returned signals have to be described by the corresponding Unisens XML element (e.g. signalEntry or eventEntry). UnisensViewer displays the returned signals directly.
        /// </returns>
        private static IEnumerable<XElement> main(IEnumerable<XElement> selectedsignals, double time_cursor, double time_start, double time_end, string markerSymbol, MarkerOperation operation)
        {
            XDocument unisensxml = UnisensXmlFileManager.CurrentUnisensInstance.Xdocument;

            //// Marker-Datei im Signal-Stapel suchen.
            //// Falls keine vorhanden:
            ////  - eindeutigen Dateinamen für Marker-Datei generieren
            ////  - in Unisens-Metadatei einen neuen eventEntry mit Marker-Dateinamen erzeugen
            XElement evententry = FindMarkerEntry(selectedsignals);
            List<XElement> ret = null;

            if (evententry == null)
            {
                if (operation == MarkerOperation.Truncate || operation == MarkerOperation.Delete)
                {
                    return null;
                }

                evententry = CreateMarkerEntry(unisensxml, GetMaxSampleRate(selectedsignals));

                if (evententry == null)
                {
                    return null;
                }

                ret = new List<XElement>();
                ret.Add(evententry);
            }

            // kein Auswahl
            if (time_start == 0 & time_end == 0)
            {
                time_start = time_end = time_cursor;
            }

            string markerfile = evententry.Attribute("id").Value;
            double samplespersec = EventEntry.GetSampleRate(evententry);
            delim = EventEntry.GetCsvFileFormatSeparator(evententry);
            int sample_cursor = (int)(time_cursor * samplespersec);
            int sample_start = (int)(time_start * samplespersec);
            int sample_end = (int)(time_end * samplespersec);

            string mark = delim + markerSymbol;

            if (!File.Exists(markerfile))
            {
                FileStream fs = File.Create(markerfile);
                fs.Close();
            }

            StreamReader csv_in = new StreamReader(markerfile);
            StreamWriter csv_out = new StreamWriter(markerfile + ".tmp");

            Boolean success = false;
            switch (operation)
            {
                case MarkerOperation.Set:
                    success = Set(csv_in, csv_out, sample_cursor, mark);
                    break;
                case MarkerOperation.Delete:
                    success = Delete(csv_in, csv_out, sample_start, sample_end);
                    break;
                case MarkerOperation.Truncate:
                    success = Truncate(csv_in, csv_out, sample_cursor);
                    break;
            }

            csv_in.Close();
            csv_in.Dispose();
            csv_out.Close();
            csv_out.Dispose();

            if (success)
            {
                // Die ursprüngliche Marker-Datei durch temporäre Datei ersetzen.
                File.Delete(markerfile);
                File.Move(markerfile + ".tmp", markerfile);
            }
            else
            {
                // Die temporäre Marker-Datei löschen
                File.Delete(markerfile + ".tmp");
            }

            // Rendere wieder aktivieren
            foreach (XElement xe in selectedsignals)
            {
                try
                {
                    RendererManager.ReOpenRenderer(xe);
                }
                catch
                {
                    RendererManager.KillRenderer(xe);
                }
            }

            // UnisensViewer den neuen eventEntry (falls einer erzeugt wurde) zum Stapel hinzufügen lassen
            return ret;
        }

        private static string CopyUntilTimeStamp(StreamReader csv_in, StreamWriter csv_out, int targetTimeStamp)
        {
            String line = null;
            while ((line = csv_in.ReadLine()) != null)
            {
                if (LineTimeStampIsLowerThan(line, targetTimeStamp))
                {
                    csv_out.WriteLine(line);
                }
                else
                {
                    break;
                }
            }
            return line;
        }

        private static void CopyRest(StreamReader csv_in, StreamWriter csv_out)
        {
            String line = null;
            while ((line = csv_in.ReadLine()) != null && line != "")
            {
                csv_out.WriteLine(line);
            }
        }

        private static bool Set(StreamReader csv_in, StreamWriter csv_out, int sample_cursor, string mark)
        {
            String lineAfterTimestamp = CopyUntilTimeStamp(csv_in, csv_out, sample_cursor);
            
            csv_out.WriteLine(sample_cursor + mark);

            if (lineAfterTimestamp != null && lineAfterTimestamp != "") csv_out.WriteLine(lineAfterTimestamp);

            CopyRest(csv_in, csv_out);

            return true;
        }

        private static bool Delete(StreamReader csv_in, StreamWriter csv_out, int sample_start, int sample_end)
        {
            String lineAfterTimestamp = CopyUntilTimeStamp(csv_in, csv_out, sample_start);

            if (LineTimeStampIsHigherThan(lineAfterTimestamp, sample_end) || lineAfterTimestamp == "")
            {
                System.Windows.MessageBox.Show("Kein Marker im markierten Bereich zum löschen!", "Marker", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
                return false;
            }
            else
            {
                int linesToDelete = 1;

                String currentLine;
                while ((currentLine = csv_in.ReadLine()) != null && currentLine != "")
                {
                    if (LineTimeStampIsLowerThan(currentLine, sample_end))
                    {
                        linesToDelete++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (linesToDelete > 1)
                {
                    if (System.Windows.MessageBox.Show("Wollen Sie wirklich " + linesToDelete + " Marker im ausgewählten Intervall löschen?", "Marker", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.OK) != MessageBoxResult.OK)
                    {
                        return false;
                    }
                }
                if (currentLine != null && currentLine != "") csv_out.WriteLine(currentLine);
                CopyRest(csv_in, csv_out);
            }

            return true;
        }


        private static Boolean Truncate(StreamReader csv_in, StreamWriter csv_out, int sample_cursor)
        {
            String lineAfterTimestamp = CopyUntilTimeStamp(csv_in, csv_out, sample_cursor);

            if (!LineTimeStampIsHigherThan(lineAfterTimestamp, sample_cursor) || lineAfterTimestamp == "")
            {
                System.Windows.MessageBox.Show("Kein Marker nach der aktuellen Cursorposition gefunden!", "Marker", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
                return false;
            }
            else
            {
                int linesToDelete = 1;

                String currentLine;
                while ((currentLine = csv_in.ReadLine()) != null && currentLine != "")
                {
                    if (LineTimeStampIsHigherThan(lineAfterTimestamp, sample_cursor))
                    {
                        linesToDelete++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (linesToDelete > 1)
                {
                    if (System.Windows.MessageBox.Show("Wollen Sie wirklich " + linesToDelete + " Marker nach der aktuellen Cursorposition löschen?", "Marker", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.OK) != MessageBoxResult.OK)
                    {
                        return false;
                    }
                }
                if (currentLine != null && currentLine != "") csv_out.WriteLine(currentLine);
                CopyRest(csv_in, csv_out);
            }

            return true;
        }

        private static Boolean LineTimeStampIsLowerThan(string line, int stamp)
        {
            if (line != null)
            {
                if (line.IndexOf(delim) > 0)
                {
                    int timeStamp = int.Parse(line.Substring(0, line.IndexOf(delim)), CultureInfo.InvariantCulture.NumberFormat);
                    if (timeStamp < stamp)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static Boolean LineTimeStampIsHigherThan(string line, int stamp)
        {
            if (line != null)
            {
                if (line.IndexOf(delim) > 0)
                {
                    int timeStamp = int.Parse(line.Substring(0, line.IndexOf(delim)), CultureInfo.InvariantCulture.NumberFormat);
                    if (timeStamp > stamp)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Finds the marker entry.
        /// </summary>
        /// <param name="selectedsignals">The selectedsignals.</param>
        /// <returns>Marker Entry</returns>
        private static XElement FindMarkerEntry(IEnumerable<XElement> selectedsignals)
        {
            foreach (XElement xe in selectedsignals)
            {
                XAttribute id = xe.Attribute("id");

                // if (id != null && id.Value.IndexOf("marker_") == 0)
                if (id != null && Path.GetExtension(id.Value) == ".csv" && xe.Name.LocalName == "eventEntry")
                {
                    return xe;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the max sample rate.
        /// </summary>
        /// <param name="selectedsignals">The selectedsignals.</param>
        /// <returns>Sample rate</returns>
        private static double GetMaxSampleRate(IEnumerable<XElement> selectedsignals)
        {
            double max = 0.0;

            foreach (XElement xe in selectedsignals)
            {
                double sr = MeasurementEntry.GetSampleRate(xe);

                if (sr > max)
                {
                    max = sr;
                }
            }

            return max;
        }

        /// <summary>
        /// Creates the marker entry.
        /// </summary>
        /// <param name="unisensxml">The unisensxml.</param>
        /// <param name="samplerate">The samplerate.</param>
        /// <returns>Marker Entry</returns>
        private static XElement
            CreateMarkerEntry(XDocument unisensxml, double samplerate)
        {
            string markerfile = null;
            SaveFileDialog markerfileDialog;
            XElement evententry = null;

            markerfileDialog = new SaveFileDialog();

            markerfileDialog.Filter = "csv files (*.csv)|*.csv|All files (*.*)|*.*";
            markerfileDialog.AddExtension = true;
            markerfileDialog.DefaultExt = ".csv";
            markerfileDialog.CheckFileExists = false;
            markerfileDialog.OverwritePrompt = false;
            markerfileDialog.InitialDirectory = Environment.CurrentDirectory;



            if (markerfileDialog.ShowDialog() == true)
            {
                markerfile = markerfileDialog.FileName;

                if (Path.GetDirectoryName(markerfile).Equals(Path.GetDirectoryName(UnisensXmlFileManager.CurrentUnisensInstance.XmlFilePath)))
                {
                    markerfile = Path.GetFileName(markerfile);

                    // Check, if file exists
                    if (File.Exists(markerfile))
                    {
                        foreach (XElement xe in unisensxml.Root.Elements())
                        {
                            if (xe.Name.LocalName == "eventEntry" && Entry.GetId(xe) == markerfile)
                            {
                                evententry = xe;
                            }
                        }
                    }

                    // create entry, if necessary
                    XName xname = "{http://www.unisens.org/unisens2.0}eventEntry";
                    object id = new XAttribute("id", markerfile);
                    object sampleRate = new XAttribute("sampleRate", samplerate);
                    object csvFileFormat = new XElement(
                                            "{http://www.unisens.org/unisens2.0}csvFileFormat",
                                            new XAttribute("decimalSeparator", "."),
                                            new XAttribute("separator", ";"));
                    if (evententry == null)
                    {
                        evententry = new XElement(xname, id, sampleRate, csvFileFormat);
                        unisensxml.Root.Add(evententry);
                    }
                    ////if (evententry == null)
                    ////{
                    ////    evententry = new XElement(
                    ////                    "{http://www.unisens.org/unisens2.0}eventEntry",
                    ////                    new XAttribute("id", markerfile),
                    ////                    new XAttribute("sampleRate", samplerate),
                    ////                    new XElement(
                    ////                        "{http://www.unisens.org/unisens2.0}csvFileFormat",
                    ////                        new XAttribute("decimalSeparator", "."),
                    ////                        new XAttribute("separator", ";")));

                    ////    unisensxml.Root.Add(evententry);
                    ////}
                }
                else
                {
                    // Cannot save marker file outside your dataset!
                    if (System.Windows.MessageBox.Show("Cannot save marker file outside your dataset!", "Marker", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK) == MessageBoxResult.OK)
                    {
                        evententry = CreateMarkerEntry(unisensxml, samplerate);
                    }
                }
            }

            return evententry;
        }
    }
}
