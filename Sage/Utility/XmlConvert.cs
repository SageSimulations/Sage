/* This source code licensed under the GNU Affero General Public License */

using System.IO;
using System.Xml;

namespace Highpoint.Sage.Utility {

    /// <summary>
    /// Class XmlTransform converts xml to non-xml and vice versa by changing angle-brackets to ampersand-l-t's, etc, and vice versa.
    /// </summary>
    public class XmlTransform {

        /// <summary>
        /// Xmlifies the specified source string - converts angle-brackets to ampersand-l-t's, etc.
        /// </summary>
        /// <param name="src">The SRC.</param>
        /// <returns></returns>
        public static string Xmlify(string src) {
            StringWriter sw = new StringWriter();
            XmlTextWriter writer = new XmlTextWriter(sw);
            writer.WriteString(src);
            return sw.ToString();
        }

        /// <summary>
        /// Converse of Xmlify - converts ampersand-l-t's, etc., to angle-brackets, etc.
        /// </summary>
        /// <param name="src">The SRC.</param>
        /// <returns></returns>
        public static string DeXmlify(string src) {
            try {
                XmlTextReader reader = new XmlTextReader("<foo>" + src + "</foo>", XmlNodeType.Element, null);
                reader.Read();
                reader.Read();
                return reader.Value;
            } catch (XmlException) {
                return src;
            }
        }
	}
}