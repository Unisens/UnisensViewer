using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;

namespace UnisensViewerLibrary
{
	public enum FileFormat
	{
		Invalid,
		Bin,
		Xml,
		Csv,
		Custom
	}

	public class Entry
	{
		public static string GetId(XElement entry)
		{
			XAttribute id = entry.Attribute("id");
			return id.Value;
		}

		public static FileFormat GetFileFormat(XElement entry)
		{
			if (entry.Element("{http://www.unisens.org/unisens2.0}binFileFormat") != null)
				return FileFormat.Bin;
			else if (entry.Element("{http://www.unisens.org/unisens2.0}xmlFileFormat") != null)
				return FileFormat.Xml;
			else if (entry.Element("{http://www.unisens.org/unisens2.0}csvFileFormat") != null)
				return FileFormat.Csv;
			else if (entry.Element("{http://www.unisens.org/unisens2.0}customFileFormat") != null)
				return FileFormat.Custom;
			else
				return FileFormat.Invalid;
		}

        public static char GetCsvFileFormatSeparator(XElement entry)
        {
            try
            {
                XElement csvfileformat = entry.Element("{http://www.unisens.org/unisens2.0}csvFileFormat");
                XAttribute separator = csvfileformat.Attribute("separator");

                string s = separator.Value.Trim();

                if (string.Compare(s, @"\t", true) == 0)
                    return '\t';
                else
                    return s[0];
            }
            catch
            {
                return ';';
            }
        }

        public static char GetCsvFileDecimalSeparator(XElement entry)
        {
            try
            {
                XElement csvfileformat = entry.Element("{http://www.unisens.org/unisens2.0}csvFileFormat");
                XAttribute separator = csvfileformat.Attribute("decimalSeparator");

                string s = separator.Value.Trim();

                if (string.Compare(s, @"\t", true) == 0)
                    return '\t';
                else
                    return s[0];
            }
            catch
            {
                return CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
            }
        }
	}
}
