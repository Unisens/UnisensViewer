using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;

namespace UnisensViewer
{
	class ExpertmodeToVisibility : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (((bool)value) == true)
			{
				return System.Windows.Visibility.Visible;
			}
			else
			{
				return System.Windows.Visibility.Collapsed;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (((System.Windows.Visibility)value) == System.Windows.Visibility.Visible)
			{
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}
