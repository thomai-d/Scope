using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Scope.MVVM
{
    public class CompareConverter : IValueConverter
    {
        public CompareConverter()
        {
        }

        public CompareConverter(object comparant, object valueIsGreaterResult, object valueIsSmallerResult, object equalityResult)
        {
            this.Comparant = comparant;
            this.ValueIsGreaterResult = valueIsGreaterResult;
            this.ValueIsSmallerResult = valueIsSmallerResult;
            this.EqualityResult = equalityResult;
        }

        public object Comparant { get; set; }

        public object ValueIsGreaterResult { get; set; }

        public object ValueIsSmallerResult { get; set; }

        public object EqualityResult { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var input = value as IComparable;
            if (input == null)
            {
                return Binding.DoNothing;
            }

            var result = input.CompareTo(this.Comparant);
            if (result == 0) return this.EqualityResult;
            if (result > 0) return this.ValueIsGreaterResult;
            return this.ValueIsSmallerResult;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}