using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Xml.Linq;
using UnisensViewerLibrary;
using System.Windows;
using System.Globalization;

namespace UnisensViewer
{
    public class Artifacts
    {
        private static char delim;
        public static IEnumerable<XElement> setArtifact(double time_start, double time_end, string artifactSymbol_Start, string artifactSymbol_End)
        {
            XDocument unisensxml = UnisensXmlFileManager.CurrentUnisensInstance.Xdocument;
            IEnumerable<XElement> selectedsignals = from XElement xe in unisensxml.Root.Elements()
                                                    where xe.Name.LocalName == "signalEntry" || xe.Name.LocalName == "eventEntry" || xe.Name.LocalName == "valuesEntry"
                                                    select xe;
            XElement evententry = FindArtifactsEntry(selectedsignals);
            List<XElement> ret = null;
            if (evententry == null)
            { // kein EventEntry vorhanden, erstelle eine neue EventEntry
                MessageBox.Show("Es wird ein neues Artefakt-Entry mit der ID '" +  Properties.Settings.Default.ArtifactEntryId + "' erstellt. ");
                evententry = CreateArtifactsEntry(unisensxml, GetMaxSampleRate(selectedsignals));
                ret = new List<XElement>();
                ret.Add(evententry);
            }

            // Renderer schließen
            foreach (XElement xe in selectedsignals)
            {
                RendererManager.CloseRenderer(xe);
            }
            string comment = null;
            string artifactsfile = evententry.Attribute("id").Value;
            double samplespersec = EventEntry.GetSampleRate(evententry);
            delim = EventEntry.GetCsvFileFormatSeparator(evententry);
            int sample_start = (int)(time_start * samplespersec);
            int sample_end = (int)(time_end * samplespersec);

            // Artefakt-Kommentare sind erstmal deaktiviert
            //DialogsArtifacts dialogsArtifact = new DialogsArtifacts();
            //dialogsArtifact.Topmost = true;

            //if (dialogsArtifact.ShowDialog() != (DialogsArtifacts.artifact))
            //{
            //    comment = DialogsArtifacts.artifact_comment;
            //}

            string artifactStart = delim + artifactSymbol_Start + ";" + comment;
            string artifactEnd = delim + artifactSymbol_End + ";" + comment;

            StreamWriter csv_out = null;
            StreamReader csv_in = null;

            try
            {
                // Temporäre Datei erzeugen, um Marker an entsprechender Stelle einfügen zu können.
                csv_in = new StreamReader(artifactsfile);
                csv_out = new StreamWriter(artifactsfile + ".tmp");
            }
            catch
            {
                return null;
            }

            string s1 = csv_in.ReadLine();
            string s2 = csv_in.ReadLine();

            // implements for reading and writting the data
            bool aldready = false;

            while (s1 !=null && s2 != null )
            {
                int a = s1.IndexOf(delim);
                int b = s2.IndexOf(delim);
                if (a == -1 || b == -1)
                {
                    break;
                }

                int x1 = int.Parse(s1.Substring(0, a), CultureInfo.InvariantCulture.NumberFormat);
                int x2 = int.Parse(s2.Substring(0, b), CultureInfo.InvariantCulture.NumberFormat);

                if (sample_end < x1)
                {
                    if (aldready == false)
                    {
                        csv_out.WriteLine(sample_start + artifactStart);
                        csv_out.WriteLine(sample_end + artifactEnd);
                        aldready = true;
                    }                    
                }
                else if ((x1<sample_start && sample_start<x2) || (x1<sample_end && sample_end<x2) || (sample_start<x1 && x2<sample_end))
                {
                    MessageBox.Show("Der ausgewählte Bereich hat eine Überlappung. Bitte wählen Sie einen anderen Bereich!");
                    aldready = true;
                }

                csv_out.WriteLine(s1);
                csv_out.WriteLine(s2);
                s1 = csv_in.ReadLine();
                s2 = csv_in.ReadLine();
            }

            if (aldready == false) // SAMPLESTAMP_START;(artifact; noch nicht abgelegt
            {
                csv_out.WriteLine(sample_start + artifactStart);
                csv_out.WriteLine(sample_end + artifactEnd);
                aldready = true;
            }

            //while ((line = csv_in.ReadLine()) != null)
            //{
            //    int a = line.IndexOf(delim);
            //    if (a == -1)
            //    {
            //        break;
            //    }

            //    int timeStamp = int.Parse(line.Substring(0, a), CultureInfo.InvariantCulture.NumberFormat);
            //    if (timeStamp < sample_start)
            //    {
            //        csv_out.WriteLine(line);
            //    }
            //    else
            //    {
            //        if (start == false)
            //        {
            //            csv_out.WriteLine(sample_start + artifactStart);
            //            start = true; // write SAMPLESTAMP_START;(artifact already
            //        }

            //        if (timeStamp < sample_end)
            //        {
            //            csv_out.WriteLine(line);
            //        }
            //        else
            //        {
            //            if (end == false)
            //            {
            //                csv_out.WriteLine(sample_end + artifactEnd);
            //                end = true;
            //            }
            //            csv_out.WriteLine(line);
            //        }
            //    }
            //}

            //if (start == false) // SAMPLESTAMP_START;(artifact; noch nicht abgelegt
            //{
            //    csv_out.WriteLine(sample_start + artifactStart);
            //    start = true;
            //}
            //if (end == false)
            //{
            //    csv_out.WriteLine(sample_end + artifactEnd);
            //    end = true;
            //}

            csv_in.Close();
            csv_in.Dispose();
            csv_in = null;
            File.Delete(artifactsfile);

            // Die ursprüngliche Marker-Datei (oben schon gelöscht) durch temporäre Datei ersetzen.
            csv_out.Close();
            csv_out.Dispose();
            csv_out = null;
            File.Move(artifactsfile + ".tmp", artifactsfile);

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

            // UnisensViewer das neue eventEntry (falls eins erzeugt wurde) zum Stapel hinzufügen lassen
            return ret;
        }


        private static XElement FindArtifactsEntry(IEnumerable<XElement> selectedsignals)
        {          
            foreach (XElement xe in selectedsignals)
            {
                XAttribute id = xe.Attribute("id");
                if (id.Value == Properties.Settings.Default.ArtifactEntryId)
                {
                    return xe;
                }
            }
            return null;
        }

        private static XElement CreateArtifactsEntry(XDocument unisensxml, double samplerate)
        {
            

            XElement evententry = new XElement("{http://www.unisens.org/unisens2.0}eventEntry",
                                        new XAttribute("id", "artifact.csv"),
                                        new XAttribute("sampleRate", samplerate),
                                        new XElement("{http://www.unisens.org/unisens2.0}csvFileFormat",
                                            new XAttribute("decimalSeparator", "."), new XAttribute("separator", ";")));
            unisensxml.Root.Add(evententry);
            StreamWriter SW = File.CreateText("artifact.csv");
            SW.Close();
            return evententry;
        }

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
    }
}
