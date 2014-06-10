using System;
using System.IO;
using System.Globalization;
using System.Windows;
using System.Xml.Linq;
using System.Windows.Forms;


namespace UnisensViewerLibrary
{


	public class EventValueData : EventData
	{
		public float[]		values;





		public EventValueData(XElement valueentry)
			: base(valueentry, ValueEntry.GetNumChannels(valueentry))
		{
		}





        public override void LoadCsv(string filepath, int channels, char delimiter, char decimalSeparator)
		{
			// die quick-and-dirty-implementierung hier ist eher für kleinere dateien.
			// wer megabytes an event-daten hat sollte die eh besser binär speichern.

			//char[]	delim = { ';' };
			char[] delim = { delimiter };
			int i, numberofLines, c, o;
            string[] lines;
            try
            {
                lines = File.ReadAllLines(filepath);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Das Entry \"" + filepath + "\" ist nicht vorhanden oder bereits in einem anderen Programm geöffnet."); 
                throw new ArgumentNullException();
            }
            numberofLines = lines.Length;
            // Datei ist leer
            if (numberofLines == 0)
            {
                //MessageBox.Show("Das Entry \"" + filepath + "\" ist leer");
                //throw new ArgumentNullException();
            }
            timestamps = new uint[numberofLines];          
            values = new float[numberofLines * channels];

            CultureInfo ci = new CultureInfo("en-US", false);
            NumberFormatInfo nfi = ci.NumberFormat;
            nfi.NumberDecimalSeparator = ".";
            nfi.NumberGroupSeparator = "";

            o = 0;
            for (i = 0; i < numberofLines; ++i)
            {
                string[] data = lines[i].Split(delim);
                try
                {
                    timestamps[i] = uint.Parse(data[0], nfi);
                }
                catch
                {
                    MessageBox.Show("Das Entry \"" + filepath + "\" hat falsche Format");
                }
                for (c = 1; c <= channels; ++c)
                    values[o++] = float.Parse(data[c].Replace(decimalSeparator.ToString(), "."), nfi);
            }        
		}
	}
}
