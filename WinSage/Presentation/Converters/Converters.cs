/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Highpoint.Sage.Presentation.Converters {
    public class BooleanToVisibleVisibility : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            Visibility visibility = Visibility.Visible; // Default fallback
            try {
                bool visible = bool.Parse(value.ToString());
                visibility = visible ? Visibility.Collapsed : Visibility.Visible;
            } catch (Exception) {
            }
            return visibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value;
        }
    }

    public class BooleanToCollapsedVisibility : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            Visibility visibility = Visibility.Visible;
            try {
                bool visible = bool.Parse(value.ToString());
                visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            } catch (Exception) {
            }
            return visibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value;
        }
    }

    public class NotMatchStringToCollapsedVisibility : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            string test = parameter as string;
            Visibility visibility = Visibility.Collapsed;
            try {
                string strValue = value as string;
                if (strValue != null && strValue.Equals(test)) {
                    visibility = Visibility.Visible;
                }
            } catch (Exception) {
            }
            return visibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value;
        }
    }

    public class InvertBoolean : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            try {
                bool boolVal = bool.Parse(value.ToString());
                return !boolVal;
            } catch (Exception) {
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return Convert(value, targetType, parameter, culture);
        }
    }
}
