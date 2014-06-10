using System;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace UnisensViewer
{
	[ValueConversion(typeof(string), typeof(Brush))]
	public class InfoBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value != null)
			{
				TextBlock t = new TextBlock(new Italic(new Run((string)value)));

				VisualBrush vb = new VisualBrush(t);
				vb.AlignmentX = AlignmentX.Left;
				vb.Stretch = Stretch.None;
				vb.Opacity = 0.5;

				return vb;
			}
			else
			{
				return Binding.DoNothing;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}

