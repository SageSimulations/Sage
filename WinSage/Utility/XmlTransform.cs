/* This source code licensed under the GNU Affero General Public License */

using System;
using System.IO;
using System.Xml;

namespace Highpoint.Sage.Utility {
    /// <summary>
    /// Provides encoding helpers that work around historic framework issues.
    /// </summary>
    public class XmlConvert : System.Xml.XmlConvert {
        public static new string ToString(TimeSpan ts) {
            return ts.Ticks.ToString();
        }

        public static string Xmlify(string src) {
            using StringWriter sw = new StringWriter();
            using XmlTextWriter writer = new XmlTextWriter(sw);
            writer.WriteString(src);
            return sw.ToString();
        }

        public static string DeXmlify(string src) {
            try {
                using XmlTextReader reader = new XmlTextReader("<foo>" + src + "</foo>", XmlNodeType.Element, null);
                reader.Read();
                reader.Read();
                return reader.Value;
            } catch (XmlException) {
                return src;
            }
        }

        public static new TimeSpan ToTimeSpan(string s) {
            try {
                if (long.TryParse(s, out long ticks)) {
                    return TimeSpan.FromTicks(ticks);
                }
                return System.Xml.XmlConvert.ToTimeSpan(s);
            } catch (FormatException fe2) {
                throw new ApplicationException("Bad format for string \"" + s + "\"; expected TimeSpan.", fe2);
            }
        }

        public static string ToString(object o) {
            switch (o) {
                case Guid guid:
                    return System.Xml.XmlConvert.ToString(guid);
                case TimeSpan ts:
                    return System.Xml.XmlConvert.ToString(ts.Ticks);
                case double d:
                    return System.Xml.XmlConvert.ToString(d);
                case DateTime dt:
                    return System.Xml.XmlConvert.ToString(dt, XmlDateTimeSerializationMode.RoundtripKind);
                case bool b:
                    return System.Xml.XmlConvert.ToString(b);
                case long l:
                    return System.Xml.XmlConvert.ToString(l);
                case int i:
                    return System.Xml.XmlConvert.ToString(i);
                default:
                    return o.ToString();
            }
        }
    }
}
