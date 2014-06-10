using System;
using System.Xml.Linq;

namespace UnisensViewerLibrary
{
	public abstract class EventData
	{
		public uint[]		timestamps;		// samplenummern, mit samplerate umrechnen in zeit

		public EventData(XElement entry, int channels)
		{
			string		filepath	= System.IO.Path.GetFullPath(Entry.GetId(entry));
			FileFormat	fileformat	= Entry.GetFileFormat(entry);

			switch (fileformat)
			{
				case FileFormat.Csv:
                    LoadCsv(filepath, channels, Entry.GetCsvFileFormatSeparator(entry), Entry.GetCsvFileDecimalSeparator(entry));
					break;

				default:
					throw new Exception("Im Moment nur CSV unterstützt.");
			}
		}

        public abstract void LoadCsv(string filepath, int channels, char delimiter, char decimalSeparator);

		public int Search(uint timestamp) 
		{
            try
            {
                if (timestamp > timestamps[timestamps.Length - 1])
                {
                    return -1;
                }
            }
            catch (Exception e)
            {
                return -1;
            }
			if (timestamps.Length > 0)
			{
				int index = Search(timestamp, 0, timestamps.Length - 1);

				// falls timestamp > alle timestamps, wird das letzte element zurückgeliefert
                if (timestamp <= timestamps[index])
                    return index;
                else
                    return timestamps.Length - 1; //Fix für das Problem #416
			}
            
			return -1;
		}

        // Sucht den timestamp in timstamps.
        // return: index des timestamps oder des nächst höheren
        int Search(uint timestamp, int left, int right)
        {
            if (left == right)
                return left;

            int middle = (left + right) / 2;

            if (timestamp <= timestamps[middle])
                return Search(timestamp, left, middle);
            else
                return Search(timestamp, middle + 1, right);
        }
	}
}
