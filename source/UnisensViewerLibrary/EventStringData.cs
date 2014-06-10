using System.IO;
using System.Globalization;
using System.Xml.Linq;
using System;
using System.Windows.Forms;



namespace UnisensViewerLibrary
{


	public class EventStringData : EventData
	{
		public string[] strings;

		public string[] comments;



		public EventStringData(XElement evententry)
			: base(evententry, 1)
		{
		}





        public override void LoadCsv(string filepath, int channels, char delimiter, char decimalSeparator)
		{
			// die quick-and-dirty-implementierung hier ist eher für kleinere dateien.
			// wer megabytes an event-daten hat sollte die eh besser binär speichern.

			//char[]	delim = { ';' };
			char[]	delim = { delimiter };
			int		iLine, nLines;


            string[] lines;
            try
            {
                lines = File.ReadAllLines(filepath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ein Entry ist nicht vorhanden oder bereits in einem anderen Programm geöffnet:\n" + ex.Message);
                throw new ArgumentNullException();
            }
			nLines = lines.Length;
			timestamps = new uint[nLines];
			strings = new string[nLines];
			comments = new string[nLines];

			for (iLine = 0; iLine < nLines; ++iLine)
			{
				string[] data = lines[iLine].Split(delim);

                if (data.Length > 0 && data[0].Length > 0)
                {
                    timestamps[iLine] = uint.Parse(data[0], CultureInfo.InvariantCulture.NumberFormat);
                }
                else
                {
                    // No timestamp, skip this line
                    continue;
                }

                if (data.Length > 1)
                {
                    strings[iLine] = data[1];
                }

				if (data.Length > 2)
				{
					comments[iLine] = data[2];
				}
			}
		}

	}


}
