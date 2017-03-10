using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Scope.MVVM
{
    public class IfEmptyConverter : IValueConverter
    {
        public object EmptyValue { get; set; }

        public object NonEmptyValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            /* Note for developers:
			 * string.Empty is implicitly supported, since it enumerates to a zero-item-list of chars.
			 * Nice. */

            var enumerable = value as IEnumerable;
            var isListEmpty = value == null || (enumerable != null && !enumerable.GetEnumerator().MoveNext());

            return isListEmpty
                ? this.EmptyValue
                : this.NonEmptyValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}