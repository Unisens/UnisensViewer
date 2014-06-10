using System;
using System.Globalization;
using System.Windows.Data;

namespace UnisensViewer
{
	public class SelectionStatusConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			try
			{
				double start = (double)values[0];
				double end = (double)values[1];

				TimeSpan ts = TimeSpan.FromSeconds(start);
				TimeSpan te = TimeSpan.FromSeconds(end);

				if (start < end)
				{
					return "[" + ts.ToString(@"hh\:mm\:ss\.fff") + ", " + te.ToString(@"hh\:mm\:ss\.fff") + "]";
				}
				else
				{
					return string.Empty;
				}
			}
			catch
			{
				return string.Empty;
			}
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
