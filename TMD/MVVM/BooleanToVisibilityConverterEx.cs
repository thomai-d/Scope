using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace TMD.MVVM
{
    public sealed class BooleanToVisibilityConverterEx : IValueConverter
    {
        public BooleanToVisibilityConverterEx()
        {
            this.HiddenVisibility = Visibility.Collapsed;
        }

        public Visibility HiddenVisibility { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var flag = false;

            if (value is bool)
            {
                flag = (bool)value;
            }

            if (value is long)
            {
                flag = System.Convert.ToBoolean(value);
            }

            if (parameter != null)
            {
                if (parameter is bool && (bool)parameter)
                    flag = !flag;

                if (parameter is string && bool.Parse((string)parameter))
                    flag = !flag;
            }

            if (flag)
            {
                return Visibility.Visible;
            }

            return this.HiddenVisibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var back = value is Visibility && ((Visibility)value == Visibility.Visible);
            if (parameter != null)
            {
                if (bool.Parse((string)parameter))
                {
                    back = !back;
                }
            }

            return back;
        }
    }
}