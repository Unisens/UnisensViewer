using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace UnisensViewer
{
	[ValueConversion(typeof(UInt32), typeof(Brush))]
	public class UInt32BrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			uint c = (uint)value;
			return new SolidColorBrush(Color.FromArgb((byte)(c >> 24), (byte)(c >> 16), (byte)(c >> 8), (byte)c));
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
