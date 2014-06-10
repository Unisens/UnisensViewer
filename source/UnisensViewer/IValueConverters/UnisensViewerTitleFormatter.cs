using System;
using System.Windows.Data;

namespace UnisensViewer
{
	[ValueConversion(typeof(string), typeof(string))]
	public class UnisensViewerTitleFormatter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null)
			{
				return "UnisensViewer";
			}
			else
			{
				return (string)value + " - UnisensViewer";
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}

