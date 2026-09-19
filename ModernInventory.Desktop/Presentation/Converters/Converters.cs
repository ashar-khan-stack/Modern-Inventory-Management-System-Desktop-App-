using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ModernInventory.Desktop.Presentation.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolVal = false;
            if (value is bool b)
            {
                boolVal = b;
            }
            else if (value is string s)
            {
                boolVal = !string.IsNullOrWhiteSpace(s);
            }
            else if (value is int i)
            {
                boolVal = i > 0;
            }
            else if (value != null)
            {
                boolVal = true;
            }

            if (Invert) boolVal = !boolVal;
            return boolVal ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility vis)
            {
                return vis == Visibility.Visible;
            }
            return false;
        }
    }

    public class CurrencyFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decVal)
            {
                return $"PKR {decVal:N2}";
            }
            return "PKR 0.00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return 0.00m;
        }
    }

    public class StringNullOrEmptyToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isEmpty = string.IsNullOrWhiteSpace(value as string);
            if (Invert) isEmpty = !isEmpty;
            return isEmpty ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EqualityToBooleanConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isEqual = false;
            if (value == null && parameter == null)
            {
                isEqual = true;
            }
            else if (value != null && parameter != null)
            {
                isEqual = string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            if (Invert) isEqual = !isEqual;
            return isEqual;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CollectionEmptyToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isEmpty = true;
            if (value is System.Collections.ICollection col)
            {
                isEmpty = col.Count == 0;
            }
            else if (value is int count)
            {
                isEmpty = count == 0;
            }
            else if (value != null)
            {
                isEmpty = false;
            }

            if (Invert) isEmpty = !isEmpty;
            return isEmpty ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
