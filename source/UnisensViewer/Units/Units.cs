using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using System.ComponentModel;
using System.Windows;

namespace UnisensViewer
{
    public static class Units
	{
		static Units()
		{
			XElement xunits = null;

            if (DesignerProperties.GetIsInDesignMode(new FrameworkElement())) return; //If in Design mode do not load Units

			try
			{
				XDocument xdoc = XDocument.Load(Folders.UnisensViewer + "settings.xml");
				
				XElement xsettings = xdoc.Root;

				if (xsettings != null)
				{
					xunits = xsettings.Element("Units");
				}
			}
			catch (Exception ex)
			{
                throw new Exception("Error loading Units", ex);
			}

			InitUnits(xunits);
		}

		public static List<object[]> Scales { get; set; }

		public static Hashtable UInitIndex { get; set; }

		public static void InitUnits(XElement xunits)
		{
			Scales = new List<object[]>();
			UInitIndex = new Hashtable();

			if (xunits != null)
			{
				IEnumerable<XElement> xscales = xunits.Elements("Scale");

				foreach (XElement xs in xscales)
				{
					try
					{
						object[] s = BuildScale(xs);

						Scales.Add(s);
						IndexScale(s);
					}
					catch (Exception)
					{
					}
				}
			}
		}
	
		public static Scale GetScale(string unit)
		{
            // Wenn keine Einheit angegeben ist oder die Einheit Leerzeichen enthält, wird eine Default-Skalierung angenommen.
            if (unit == null || UInitIndex == null)
            {
                // da muss ein zeichen drin stehen! (kein leerzeichen, das ist zum trennen von synonymen einheiten. zur not kann auch 255 als unsichtbares zeichen genommen werden (falls das noch so ist wie früher...).
                return new Scale(BuildDefaultScale("?"), "?");	
            }

            unit = unit.Replace(' ', '_');

			object[] s = (object[])UInitIndex[unit];

			if (s != null)
			{
				return new Scale(s, unit);
			}
			else
			{
				return new Scale(BuildDefaultScale(unit), unit);
			}
		}

		private static object[] BuildDefaultScale(string unit)
		{
			return new object[] { 10.0, unit, 10.0 };
		}
			
		private static object[] BuildScale(XElement xscale)
		{
			IEnumerable<XElement>	xes	= xscale.Elements();
			object[]				s	= new object[xes.Count()];
			int						a   = 0;

			foreach (XElement xe in xes)		
			{
				switch (xe.Name.LocalName)
				{
					case "Unit":
						s[a] = xe.Value.Trim();
						++a;
						break;

					case "Factor":
						s[a] = double.Parse(xe.Value, CultureInfo.InvariantCulture.NumberFormat);
						++a;
						break;

					default: 
						// man kann auch weitermachen, aber dann müsste man die länge von s[] anpassen.
						throw new Exception("Ungültiges Element in <Scale>.");
				}
			}

			return s;
		}

		private static void IndexScale(object[] s)
		{
			// ** units werden nicht gleich in der schleife oben indiziert, da ja eine
			//    exception auftreten kann und man das wieder rückgängig machen müsste.	
			foreach (object o in s)
			{
				if (o is string)
				{
					// der string o kann jetzt mehrere units enthalten, die durch whitespaces getrennt sind,
					// d.h. diese müssen alle auch indiziert werden.
					// (trennung der einfachheit halber durch whitespaces statt kommas. removeemptyentries ist praktisch.)
					string[] units = ((string)o).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

					foreach (string u in units)
                    {
                        UInitIndex[u] = s;
                    }
				}
			}
		}
	}
}
