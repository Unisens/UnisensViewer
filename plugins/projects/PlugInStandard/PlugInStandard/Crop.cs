using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Xml.Linq;
using UnisensViewerClrCppLibrary;
using UnisensViewerLibrary;
using System.Windows.Media.Imaging;
using System.Reflection;




namespace PlugInStandard
{
    /// <summary>
    /// This class provides two plug-ins for trimming measurement entries (signals, events, 
    /// values). The CROP method crops the entries to the selected area -- both the beginning
    /// and the ending can be trimmed in one step. DELETE deletes the selected area with one
    /// restriction: The selection has to be either at the beginning of the signal or at the 
    /// end.
    /// </summary>
    [Export(typeof(IDspPlugin))]
    public class Delete : IDspPlugin
    {
        public BitmapImage OrganizationIcon
        {
            get
            {
                Assembly myAssembly = Assembly.GetExecutingAssembly();
                Stream myStream = myAssembly.GetManifestResourceStream("PlugInStandard.OrganizationIcon_FZI.png");

                BitmapImage bImg = new BitmapImage();
                bImg.BeginInit();
                bImg.StreamSource = myStream;
                bImg.EndInit();

                return bImg;
            }
        }


        public string Name
        {
            get { return "Delete"; }
        }

        public string Description
        {
            get { return "Delete signals in the selected time interval."; }
        }

        public string Help
        {
            get { return "http://www.unisens.org"; }
        }

        public string Group
        {
            get { return "Misc"; }
        }

        public BitmapImage LargeRibbonIcon
        {
            get
            {
                Assembly myAssembly = Assembly.GetExecutingAssembly();
                Stream myStream = myAssembly.GetManifestResourceStream("PlugInStandard.LargeIcon_misc_delete.png");

                BitmapImage bImg = new BitmapImage();
                bImg.BeginInit();
                bImg.StreamSource = myStream;
                bImg.EndInit();

                return bImg;
            }
        }

        public BitmapImage SmallRibbonIcon
        {
            get
            {
                Assembly myAssembly = Assembly.GetExecutingAssembly();
                Stream myStream = myAssembly.GetManifestResourceStream("PlugInStandard.SmallIcon_misc_delete.png");

                BitmapImage bImg = new BitmapImage();
                bImg.BeginInit();
                bImg.StreamSource = myStream;
                bImg.EndInit();

                return bImg;
            }
        }

        public string CopyrightInfo
        {
            get { return "2009-2010 FZI Forschungszentrum Informatik"; }
        }

        public IEnumerable<XElement> Main(XDocument unisensxml, IEnumerable<XElement> selectedsignals, double time_cursor, double time_start, double time_end, string parameter)
        {
            if (time_start >= time_end)
                return null;

            double length = Crop.GetSignalLength(unisensxml, selectedsignals);

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
                return null;
            }
		

            if (MessageBox.Show("Es werden alle Messdaten unwiderruflich gelöscht, die innerhalb des markierten Bereichs liegen!", "Delete", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel) == MessageBoxResult.Cancel)
                return null;

            // If you want to delete more than 10 % of the signal, an extra warning is given.
            if (time_end - time_start < length * 0.9)
            {
                if (MessageBox.Show("Sind Sie Sich sicher, dass Sie mehr als 10% der Daten löschen wollen?", "Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.No)
                    return null;
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
                                Crop.CropBinary(xe, time_start, time_end); 
                                break;

                            case FileFormat.Csv: 
                                Crop.CropCsv(xe, time_start, time_end); 
                                break;
                        }

                        break;
                }
            }


            return null;
        }
    }


    [Export(typeof(IDspPlugin))]
    public class Crop : IDspPlugin
    {
        public BitmapImage OrganizationIcon
        {
            get { return new BitmapImage(new Uri(@"Images\OrganizationIcon_FZI.png", UriKind.Relative)); }
        }

        public string Name
        {
            get { return "Crop"; }
        }

        public string Description
        {
            get { return "Crops signals to the selected time interval."; }
        }

        public string Help
        {
            get { return "http://www.unisens.org"; }
        }

        public string Group
        {
            get { return "Misc"; }
        }

        public BitmapImage LargeRibbonIcon
        {
            get { return new BitmapImage(new Uri(@"Images\LargeIcon_misc_crop.png", UriKind.Relative)); }
        }

        public BitmapImage SmallRibbonIcon
        {
            get { return new BitmapImage(new Uri(@"Images\SamllIcon_misc_crop.png", UriKind.Relative)); }
        }

        public string CopyrightInfo
        {
            get { return "2009-2010 FZI Forschungszentrum Informatik"; }
        }

        public IEnumerable<XElement> Main(XDocument unisensxml, IEnumerable<XElement> selectedsignals, double time_cursor, double time_start, double time_end, string parameter)
        {
            if (time_start >= time_end)
                return null;

            if (MessageBox.Show("Es werden alle Messdaten unwiderruflich gelöscht, die außerhalb des markierten Bereichs liegen!", "Crop", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel) == MessageBoxResult.Cancel)
                return null;

            // If you want to delete more than 10 % of the signal, an extra warning is given.
            double length = Crop.GetSignalLength(unisensxml, selectedsignals);
            if (time_end - time_start < length * 0.9)
            {
                if (MessageBox.Show("Sind Sie Sich sicher, dass Sie mehr als 10% der Daten löschen wollen?", "Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.No)
                    return null;
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
                            case FileFormat.Bin: CropBinary(xe, time_start, time_end); break;
                            case FileFormat.Csv: CropCsv(xe, time_start, time_end); break;
                        }

                        break;
                }
            }


            return null;
        }





        public static unsafe void CropBinary(XElement signalentry, double time_start, double time_end)
        {
            StreamDataType datatype = SignalEntry.GetBinDataType(signalentry);
            int channels = SignalEntry.GetNumChannels(signalentry);
            int samplestructsize = channels * (int)SignalEntry.GetDataTypeBytes(datatype);
            double samplespersec = SignalEntry.GetSampleRate(signalentry);



            MapFile mapfile = new MapFile(SignalEntry.GetId(signalentry), false);

            // Hier Dateigröße und Anzahl der Samples nochmal ausrechnen.
            // Kann ja sein, dass die Datei am Ende ein bissel kaputt ist.
            // (Datei wird dann auf ganze Sampleanzahl abgeschnitten)
            Int64 filesamples = mapfile.filesize / samplestructsize;
            Int64 filesize = filesamples * samplestructsize;


            Int64 sample_start = (Int64)(time_start * samplespersec);
            Int64 sample_end = (Int64)(time_end * samplespersec);


            if (sample_start >= filesamples)
                return;


            if (sample_end > filesamples)
                sample_end = filesamples;


            Int64 srcoffs = sample_start * samplestructsize;
            Int64 size = (Int64)((sample_end - sample_start) * samplestructsize);


            // Sampledaten nach vorne kopieren
            if (sample_start > 0)
                mapfile.MemMove(0, (int)srcoffs, (int)size);	// XXX memmove gibts gibts nur als 32bit version??


            mapfile.Dispose();
            mapfile = null;


            // Datei hinten abschneiden (truncate)
            if (size < filesize)
            {
                FileStream fs = new FileStream(SignalEntry.GetId(signalentry), FileMode.Open, FileAccess.Write);
                fs.SetLength(size);
                fs.Close();
                fs.Dispose();
                fs = null;
            }
        }

        public static double GetSignalLength(XDocument unisensxml, IEnumerable<XElement> selectedsignals)
        {
            double length = 0;

            try
            {
                XElement xUnisens = unisensxml.Root;
                if (xUnisens != null)
                {
                    length = double.Parse(xUnisens.Attribute("duration").Value);
                }
            }
            catch (Exception e)
            {
                foreach (XElement xe in selectedsignals)
                {
                    // no DURATION attribut existent -> calculate duration (in seconds) with length of signal entry
                    switch (xe.Name.LocalName)
                    {
                        case "signalEntry":
                            // length = length_of_file / (bytes_per_sample * number_of_channels * sample_rate)
                            length = new FileInfo(SignalEntry.GetId(xe)).Length /
                                (SignalEntry.GetDataTypeBytes(SignalEntry.GetBinDataType(xe)) *
                                MeasurementEntry.GetNumChannels(xe) *
                                MeasurementEntry.GetSampleRate(xe));
                            break;
                    }

                    // Exit the foreach loop when length is calculated
                    if (length > 0)
                        break;
                }
            }

            return length;
        }


        /// <summary>
        /// crops CSV files (only eventEntry or valuesEntry)
        /// </summary>
        /// <param name="signalentry"></param>
        /// <param name="time_start"></param>
        /// <param name="time_end"></param>
        public static void CropCsv(XElement signalentry, double time_start, double time_end)
        {
            double samplespersec = SignalEntry.GetSampleRate(signalentry);
            string filename = SignalEntry.GetId(signalentry);
            char delim = Entry.GetCsvFileFormatSeparator(signalentry);
            int sample_start = (int)(time_start * samplespersec);
            int sample_end = (int)(time_end * samplespersec);


            StreamReader csv_in = new StreamReader(filename);
            StreamWriter csv_out = new StreamWriter(filename + ".tmp");


            // Alle Ereignisse nach Zeitstempel filtern, in temporäre Datei schreiben

            string line;
            while ((line = csv_in.ReadLine()) != null)
            {
                int a = line.IndexOf(delim);
                if (a == -1)
                    break;

                string timestamp = line.Substring(0, a);
                string rest = line.Substring(a);

                int ts = int.Parse(timestamp, CultureInfo.InvariantCulture.NumberFormat);


                if (ts > sample_end)
                    break;

                if (ts >= sample_start)
                    csv_out.WriteLine((ts - sample_start) + rest);		// Zeitstempel korrigieren
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
    }

}