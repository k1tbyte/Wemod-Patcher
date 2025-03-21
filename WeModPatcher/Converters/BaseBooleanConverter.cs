using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace WeModPatcher.Converters
{
    public abstract class BaseBooleanConverter<T> : IValueConverter
    {
        protected BaseBooleanConverter(T trueValue, T falseValue)
        {
            True = trueValue;
            False = falseValue;
        }

        protected T True { get; set; }
        protected T False { get; set; }

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case null:
                    return False;
                case bool booleanValue:
                    return booleanValue ? True : False;
            }

            if (!(value is int intValue))
            {
                return True;
            }
                
            switch (parameter)
            {
                case null:
                    return intValue == 0 ? False : True;
                case int param:
                    return intValue > param ? True : False;
                default:
                    //Because object not null
                    return True;
            }
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is T t && EqualityComparer<T>.Default.Equals(t, True);
        }
    }
}