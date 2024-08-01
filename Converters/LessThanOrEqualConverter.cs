using System;
using System.Globalization;
using System.Windows.Data;

namespace PetkusApplication.Converters
{
    public class LessThanOrEqualConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is int kolicina && values[1] is int minKolicina)
            {
                return kolicina <= minKolicina;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
