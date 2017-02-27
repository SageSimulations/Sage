/* This source code licensed under the GNU Affero General Public License */
#if NODELETE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;

namespace Highpoint.Sage.Presentation.Converters {
    public class BooleanToVisibleVisibility : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            Visibility visibility = Visibility.Visible; // Have to choose something, in case of an exception.
            try {
                bool visible = bool.Parse(value.ToString());
                if (visible) {
                    visibility = Visibility.Collapsed;
                } else {
                    visibility = Visibility.Visible;
                }
            } catch (Exception) {
            }
            return visibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            return value;
        }
    }

    public class BooleanToCollapsedVisibility : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            Visibility visibility = Visibility.Visible; // Have to choose something, in case of an exception.
            try {
                bool visible = bool.Parse(value.ToString());
                if (visible) {
                    visibility = Visibility.Visible;
                } else {
                    visibility = Visibility.Collapsed;
                }
            } catch (Exception) {
            }
            return visibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            return value;
        }
    }

    public class NotMatchStringToCollapsedVisibility : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            string test = parameter as string;
            Visibility visibility = Visibility.Collapsed; // Have to choose something, in case of an exception.
            try {
                string strValue = value as string;
                if (strValue.Equals(test)) {
                    visibility = Visibility.Visible;
                } else {
                    visibility = Visibility.Collapsed;
                }
            } catch (Exception) {
            }
            return visibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            return value;
        }
    }

    public class InvertBoolean : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            try {
                bool boolVal = bool.Parse(value.ToString());
                return !boolVal;
            } catch (Exception) {
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            return Convert(value, targetType, parameter, culture);
        }
    }
}
#endif