using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace UnisensViewer
{
	public class SelectionStatusVisibilityConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			try
			{
				double start = (double)values[0];
				double end = (double)values[1];

				return start < end ? Visibility.Visible : Visibility.Collapsed;
			}
			catch
			{
				return Visibility.Collapsed;
			}
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
