/* This source code licensed under the GNU Affero General Public License */
#if NODELETE
using System;
using System.Xml;

namespace Highpoint.Sage.Utility {
/// <summary>
/// This class is made necessary by a _bug in the .NET libraries (version 1.1)
/// whereby a TimeSpan of less than 1 second serializes to a string as &quot;P&quot;,
/// and then throws an exception upon being converted back to a TimeSpan. For a demo
/// of the _bug (to see if it's been fixed), see if this code throws an exception.
/// <code>
/// class Class1 {
///		static void Main(string[] args) {
///			foreach ( long tix in new long[]{0x1d7f90,0x11d7f90,0x111d7f90,0xf0ff00ff} ) {
///				TimeSpan ts = TimeSpan.FromTicks(tix);
///				string strRep = System.Xml.XmlConvert.ToString(ts);
///				TimeSpan andBack = System.Xml.XmlConvert.ToTimeSpan(strRep);
///				Console.WriteLine(tix + " : " + strRep + " : " + andBack);
///			}
///			Console.ReadLine();
///		}
///	}
///
/// </code>
/// </summary>
	public class XmlConvert : System.Xml.XmlConvert {

        /// <summary>
        /// Converts a timespan to a string containing the number of ticks in that timespan.
        /// </summary>
        /// <param name="ts">The timespan.</param>
        /// <returns>a string containing the number of ticks in that timespan.</returns>
		public static new string ToString(TimeSpan ts) {
			return ts.Ticks.ToString();
		}

        /// <summary>
        /// Xmlifies the specified source string - converts angle-brackets to ampersand-l-t's, etc.
        /// </summary>
        /// <param name="src">The SRC.</param>
        /// <returns></returns>
        public static string Xmlify(string src) {
            System.IO.StringWriter sw = new System.IO.StringWriter();
            System.Xml.XmlTextWriter writer = new System.Xml.XmlTextWriter(sw);
            writer.WriteString(src);
            return sw.ToString();
        }

        public static string deXmlify(string src)
        {
                return DeXmlify(src); // TODO: Remove this.
        }

        /// <summary>
        /// Converse of Xmlify - converts ampersand-l-t's, etc., to angle-brackets, etc.
        /// </summary>
        /// <param name="src">The SRC.</param>
        /// <returns></returns>
        public static string DeXmlify(string src) {
            try {
                System.Xml.XmlTextReader reader = new XmlTextReader("<foo>" + src + "</foo>", System.Xml.XmlNodeType.Element, null);
                reader.Read();
                reader.Read();
                return reader.Value;
            } catch (XmlException) {
                return src;
            }
        }

        /// <summary>
        /// Converts the given string to a time span. It first tries to convert as though the string were a number of ticks,
        /// and if that fails, tries to do so as if the string were a standard XmlConvert-encoded string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The timeSpan</returns>
        public static new TimeSpan ToTimeSpan(string s) {
            try {
                long ticks = 0;
                if (long.TryParse(s, out ticks)) {
                    return TimeSpan.FromTicks(long.Parse(s));
                } else {
                    return System.Xml.XmlConvert.ToTimeSpan(s);
                }
            } catch (System.FormatException fe2) {
                throw new ApplicationException("Bad format for string \"" + s + "\", which was supposed to be a TimeSpan.", fe2);
            }
        }

        /// <summary>
        /// Converts the object to a string by the best available rule.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <returns>A string that best representsthe object.</returns>
		public static string ToString(object o) {
			// TODO: Add support for other types
			switch(o.GetType().ToString()) {
				case "System.Guid":
					return System.Xml.XmlConvert.ToString( (Guid)o );
				case "System.TimeSpan":
					return System.Xml.XmlConvert.ToString( ((TimeSpan)o).Ticks );
				case "System.Double":
					return System.Xml.XmlConvert.ToString( (double)o );
				case "System.DateTime":
                    return System.Xml.XmlConvert.ToString((DateTime)o, XmlDateTimeSerializationMode.RoundtripKind);
				case "System.Boolean":
					return System.Xml.XmlConvert.ToString( (System.Boolean)o );
				case "System.Int64":
					return System.Xml.XmlConvert.ToString( (System.Int64)o );
				case "System.Int32":
					return System.Xml.XmlConvert.ToString( (System.Int32)o );
				default:
					return o.ToString();
			} // end switch
		} // end CreateString

	}
}
#endif