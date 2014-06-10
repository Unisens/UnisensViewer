using System;
using System.Globalization;
using System.Windows.Data;
using UnisensViewer.Translations;

namespace UnisensViewer
{
	public class SelectionDurationConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			try
			{
				double start = (double)values[0];
				double end = (double)values[1];
                // 86400 second is equivalent to 1 day
                int date = (int)(((double)values[1] - (double)values[0]) / 86400);
				TimeSpan t = TimeSpan.FromSeconds(end - start);
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

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	} 

    public class SelectionFrequencyConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                double start = (double)values[0];
                double end = (double)values[1];

                double f = 1 / (end - start);
                return f.ToString("0.##");
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
    public class SelectionBpmConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                double start = (double)values[0];
                double end = (double)values[1];

                double f = 60 / (end - start);
                return f.ToString("0");
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
