using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using UnisensViewerClrCppLibrary;
using UnisensViewerLibrary;

namespace UnisensViewer
{    
	/// <summary>
	/// This class provides two plug-ins for trimming measurement entries (signals, events, 
	/// values). The CROP method crops the entries to the selected area -- both the beginning
	/// and the ending can be trimmed in one step. DELETE deletes the selected area with one
	/// restriction: The selection has to be either at the beginning of the signal or at the 
	/// end.
	/// </summary>
    public class Crop
    {
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
		public static bool CropSelection(double time_start, double time_end)
		{
			XDocument unisensxml = UnisensXmlFileManager.CurrentUnisensInstance.Xdocument;
			IEnumerable<XElement> selectedsignals = from XElement xe in unisensxml.Root.Elements()
                                                    where xe.Name.LocalName == "signalEntry" || xe.Name.LocalName == "eventEntry" || xe.Name.LocalName == "valuesEntry" ||  xe.Name.LocalName == "customEntry"
													select xe;
            foreach (XElement xe in selectedsignals)
            {
                if (xe.Name.LocalName == "customEntry")
                {
                    if (MessageBox.Show("Im Datensatz sind customEntries enthalten. Wenn die Crop-Funktion weiter ausgeführt werden soll, werden diese customEntries nicht mitbearbeitet!", "Crop", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel) == MessageBoxResult.Cancel)
                    {
                        return false;
                    }
                }
            }
            
			if (time_start >= time_end)
			{
				return false;
			}

			if (MessageBox.Show("Es werden alle Messdaten unwiderruflich gelöscht, die außerhalb des markierten Bereichs liegen!", "Crop", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel) == MessageBoxResult.Cancel)
			{
				return false;
			}

			return cropSelection(time_start, time_end, unisensxml, selectedsignals);
		}

		public static bool TrimSelection(double time_start, double time_end)
		{
			XDocument unisensxml = UnisensXmlFileManager.CurrentUnisensInstance.Xdocument;
			IEnumerable<XElement> selectedsignals = from XElement xe in unisensxml.Root.Elements()
                                                    where xe.Name.LocalName == "signalEntry" || xe.Name.LocalName == "eventEntry" || xe.Name.LocalName == "valuesEntry" || xe.Name.LocalName == "customEntry"
													select xe;
            foreach (XElement xe in selectedsignals)
            {
                if (xe.Name.LocalName == "customEntry")
                {
                    if (MessageBox.Show("Im Datensatz sind customEntries enthalten. Wenn die Delete-Funktion weiter ausgeführt werden soll, werden diese customEntries nicht mitbearbeitet!", "Delete", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel) == MessageBoxResult.Cancel)
                    {
                        return false;
                    }
                }
            }

			if (time_start >= time_end)
			{
				return false;
			}

			double length = GetSignalLength(unisensxml, selectedsignals);

			if (time_start < length && time_end > length)
			{
				// cut end
				time_end = time_start;
				time_start = 0;
			}
			else if (time_start == 0 && time_end < length)
			{
				// cut start
				time_start = time_end;
				time_end = length;
			}
			else
			{
				// invalid selection
				MessageBox.Show("Es können nur Messdaten am Anfang oder am Ende einer Datei gelöscht werden!", "Delete", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}

			if (MessageBox.Show("Es werden alle Messdaten unwiderruflich gelöscht, die innerhalb des markierten Bereichs liegen!", "Delete", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel) == MessageBoxResult.Cancel)
			{
				return false;
			}

			// Ruft crop mit den invertierten auswahlbereich auf.
			return cropSelection(time_start, time_end, unisensxml, selectedsignals);
		}

		private static bool cropSelection(double time_start, double time_end, XDocument unisensxml, IEnumerable<XElement> selectedsignals)
		{
			// If you want to delete more than 10 % of the signal, an extra warning is given.
			double length = Crop.GetSignalLength(unisensxml, selectedsignals);
			if (time_end - time_start < length * 0.9)
			{
				if (MessageBox.Show("Sind Sie Sich sicher, dass Sie mehr als 10% der Daten löschen wollen?", "Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.No)
				{
					return false;
				}
			}

			// Renderer schließen
			foreach (XElement xe in selectedsignals)
			{
				RendererManager.CloseRenderer(xe);
			}

			foreach (XElement xe in selectedsignals)
			{
				switch (xe.Name.LocalName)
				{
					case "signalEntry":
					case "eventEntry":
					case "valuesEntry":

						switch (MeasurementEntry.GetFileFormat(xe))
						{
							case FileFormat.Bin:
								CropBinary(xe, time_start, time_end);
								break;
							case FileFormat.Csv:
								CropCsv(xe, time_start, time_end);
								break;
						}

						break;
				}
			}

			// Renderer aktivieren
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

			// If duration attribute is known: Recalculate duration attribute
			ResetDuration(unisensxml, time_end - time_start);
			ResetTimestampStart(unisensxml, time_start);
			RendererManager.UpdateTimeMax();

			return true;
		}

        /// <summary>
        /// Crops the binary.
        /// </summary>
        /// <param name="signalentry">The signalentry.</param>
        /// <param name="time_start">The time_start.</param>
        /// <param name="time_end">The time_end.</param>
        private static unsafe void CropBinary(XElement signalentry, double time_start, double time_end)
        {
            String fileName = SignalEntry.GetId(signalentry);
            StreamDataType datatype = SignalEntry.GetBinDataType(signalentry);
            int channels = SignalEntry.GetNumChannels(signalentry);
            int samplestructsize = channels * (int)SignalEntry.GetDataTypeBytes(datatype);
            double samplespersec = SignalEntry.GetSampleRate(signalentry);

            FileInfo fileInfo = new FileInfo(fileName);
            // Hier Dateigröße und Anzahl der Samples nochmal ausrechnen.
            // Kann ja sein, dass die Datei am Ende ein bissel kaputt ist.
            // (Datei wird dann auf ganze Sampleanzahl abgeschnitten)
            long filesamples = fileInfo.Length / samplestructsize;
            long filesize = filesamples * samplestructsize;
            long sample_start = (long)(time_start * samplespersec);
            long sample_end = (long)(time_end * samplespersec);

            if (sample_start >= filesamples)
            {
                return;
            }

            if (sample_end > filesamples)
            {
                sample_end = filesamples;
            }

            long size = (long)((sample_end - sample_start) * samplestructsize);

            // Datei vorne abschneiden
            if (sample_start > 0)
            {
                long srcoffs = sample_start * samplestructsize;
                try //Try it first with fast memory mapping
                {
                    MapFile mapfile = new MapFile(fileName, false);
                    mapfile.MemMove(0, (int)srcoffs, (int)size); // XXX memmove gibts gibts nur als 32bit version??
                    mapfile.Dispose();
                    mapfile = null;
                }
                catch (Exception) //If file is too large for memory mapping do it manually
                {
                    FileStream inFile = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                    FileStream outFile = new FileStream(fileName + ".tmp", FileMode.OpenOrCreate, FileAccess.Write);
                    int blocksize = 10240;

                    inFile.Seek(srcoffs, SeekOrigin.Begin);
                    while (true)
                    {
                        byte[] buffer = new byte[blocksize];
                        int data = inFile.Read(buffer, 0, blocksize);
                        if (data <= 0)
                            break; //
                        outFile.Write(buffer, 0, data);
                        if (data < blocksize)
                            break;
                    }
                    inFile.Close();
                    outFile.Close();
                    File.Delete(fileName);
                    File.Move(fileName + ".tmp", fileName);
                }
            }

            // Datei hinten abschneiden (truncate)
            if (size < filesize)
            {
                FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Write);
                fs.SetLength(size);
                fs.Close();
                fs.Dispose();
                fs = null;
            }
        }

        /// <summary>
        /// Gets the length of the signal.
        /// </summary>
        /// <param name="unisensxml">The unisensxml.</param>
        /// <param name="selectedsignals">The selectedsignals.</param>
        /// <returns>length of the signal</returns>
        private static double GetSignalLength(XDocument unisensxml, IEnumerable<XElement> selectedsignals)
        {
            double length = -1;

            try
            {
                XElement unisens = unisensxml.Root;
                if (unisens != null)
                {
                    XAttribute duration = unisens.Attribute("duration");
                    length = double.Parse(duration.Value, System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            catch (Exception)
            {
                foreach (XElement xe in selectedsignals)
                {
                    // no DURATION attribut existent -> calculate duration (in seconds) with length of signal entry
                    switch (xe.Name.LocalName)
                    {
                        case "signalEntry":
                        case "valuesEntry":
                        case "customEntry":
                            // length = length_of_file / (bytes_per_sample * number_of_channels * sample_rate)
                            if (SignalEntry.GetFileFormat(xe) == FileFormat.Bin)
                            {
                                length = new FileInfo(SignalEntry.GetId(xe)).Length /
                                    (SignalEntry.GetDataTypeBytes(SignalEntry.GetBinDataType(xe)) *
                                    MeasurementEntry.GetNumChannels(xe) *
                                    MeasurementEntry.GetSampleRate(xe));
                            }

                            break;
                    }

                    // Exit the foreach loop when length is calculated
                    if (length > 0)
                    {
                        break;
                    }
                }
            }

            if (length < 0)
            {
                throw new Exception("Cannot cut a file without duration information");
            }

            return length;
        }

        /// <summary>
        /// Crops the CSV.
        /// </summary>
        /// <param name="signalentry">The signalentry.</param>
        /// <param name="time_start">The time_start.</param>
        /// <param name="time_end">The time_end.</param>
        private static void CropCsv(XElement signalentry, double time_start, double time_end)
        {
            double samplespersec = SignalEntry.GetSampleRate(signalentry);
            string filename = SignalEntry.GetId(signalentry);
            char delim = Entry.GetCsvFileFormatSeparator(signalentry);
            int sample_start = (int)(time_start * samplespersec);
            int sample_end = (int)(time_end * samplespersec);
            StreamReader csv_in = new StreamReader(filename);
            StreamWriter csv_out = new StreamWriter(filename + ".tmp");

            if (signalentry.Name.LocalName == "signalEntry")
            {
                string line;

                // Skip first lines (from 0 to sample_start)
                for (int i = 0; i < sample_start; i++)
                {
                    line = csv_in.ReadLine();
                    if (line == null)
                    {
                        break;
                    }
                }

                // Write lines from sample_start to sample_end
                for (int i = sample_start; i < sample_end; i++)
                {
                    line = csv_in.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    csv_out.WriteLine(line);
                }
            }
            else
            {
                // Alle Ereignisse nach Zeitstempel filtern, in temporäre Datei schreiben
                string line;
                while ((line = csv_in.ReadLine()) != null)
                {
                    int a = line.IndexOf(delim);
                    if (a == -1)
                    {
                        break;
                    }

                    string timestamp = line.Substring(0, a);
                    string rest = line.Substring(a);

                    int ts = int.Parse(timestamp, CultureInfo.InvariantCulture.NumberFormat);

                    if (ts > sample_end)
                    {
                        break;
                    }

                    if (ts >= sample_start)
                    {
                        csv_out.WriteLine((ts - sample_start) + rest); // Zeitstempel korrigieren
                    }
                }
            }

            csv_in.Close();
            csv_in.Dispose();
            csv_in = null;

            csv_out.Close();
            csv_out.Dispose();
            csv_out = null;

            // ursprüngliche Datei löschen und durch temporäre Datei ersetzen
            File.Delete(filename);
            File.Move(filename + ".tmp", filename);
        }

        /// <summary>
        /// Resets the duration.
        /// </summary>
        /// <param name="unisensxml">The unisensxml.</param>
        /// <param name="newDuration">The new duration.</param>
        private static void ResetDuration(XDocument unisensxml, double newDuration)
        {
            try
            {
                XElement unisens = unisensxml.Root;
                if (unisens != null)
                {
                    XAttribute duration = unisens.Attribute("duration");
                    double length = double.Parse(duration.Value, System.Globalization.CultureInfo.InvariantCulture);

                    if (length > newDuration)
                    {
                        unisens.SetAttributeValue("duration", newDuration);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Resets the timestamp start.
        /// </summary>
        /// <param name="unisensxml">The unisensxml.</param>
        /// <param name="offset">The offset.</param>
        private static void ResetTimestampStart(XDocument unisensxml, double offset)
        {
            if (offset == 0)
            {
                return;
            }

            string dateStr = "????-??-??T??:??:??.???";

            try
            {
                XElement unisens = unisensxml.Root;
                if (unisens != null)
                {
                    // Change timestampStart
                    string timestampStart = Unisens.getTimestampStart(unisensxml);
                    DateTime timeStampStart = Convert.ToDateTime(timestampStart);
                    DateTime dt = timeStampStart.AddSeconds(offset);
                    dateStr = String.Format("{0:yyyy-MM-ddTHH:mm:ss.fff}", dt);
                    
                    unisens.SetAttributeValue("timestampStart", dateStr);

                    // Add old timestampStart to comment
                    string comment = unisens.Attribute("comment").Value;
                    comment = comment.TrimEnd();
                    if (comment.Length > 0 && comment[comment.Length - 1] != '.')
                    {
                        comment += ".";
                    }

                    if (comment.Length > 0)
                    {
                        comment += " ";
                    }

                    comment += "Original timestampStart before crop/delete: " + timestampStart;

                    unisens.SetAttributeValue("comment", comment);

					// Reset the current TimeStamp of the file Manager
					UnisensXmlFileManager.TimeStampStart = dateStr;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Cannot write new timestampStart (" + dateStr + ")!", "unisens.xml error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        
    }
}