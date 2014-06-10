using System;
using System.Windows.Data;
using UnisensViewer.Translations;

namespace UnisensViewer
{
	public class StatusTimeConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			try
			{
				TimeSpan t = TimeSpan.FromSeconds((double)value);
                // 86400 second is equivalent to 1 day
                int date = (int)((double)value / 86400);
                if (date == 0)
                {
                    return t.ToString(@"hh\:mm\:ss\.fff");
                }
                else if (date == 1)
                {
                    return (date.ToString() + " " + Translations.Translations.Tag + " und " + t.ToString(@"hh\:mm\:ss\.fff"));
                }
                else
                {
                    return (date.ToString() + " " + Translations.Translations.Tage + " und " + t.ToString(@"hh\:mm\:ss\.fff"));
                }
			}
			catch
			{
				return string.Empty;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
