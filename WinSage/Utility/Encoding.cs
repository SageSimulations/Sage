/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Text;

namespace Highpoint.Sage.Utility {
    /// <summary>
    /// Non-standard base64 encoder retained for compatibility with legacy data.
    /// </summary>
    [Obsolete("Use System.Convert.FromBase64String and System.Convert.ToBase64String unless you explicitly require this custom format")]
    public class Base64Encoder {
        private static readonly byte[] s_decoding = new byte[] {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 62, 0, 0, 0, 63, 52, 53,
            54, 55, 56, 57, 58, 59, 60, 61, 0, 0, 0, 0, 0, 0,
            0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14,
            15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 0, 0, 0,
            0, 0, 0, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36,
            37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49,
            50, 51
        };

        public static byte[] Decode(string value) {
            byte[] chars = Encoding.ASCII.GetBytes(value);
            int nDummyBytes = chars[0] - '0';
            byte[] decoded = new byte[(int)((chars.Length - 1) * (3.0 / 4.0)) - nDummyBytes];

            long charIndex = 1;
            long bufferIndex = 0;

            while (charIndex <= chars.Length - 4) {
                byte b0 = s_decoding[chars[charIndex++]];
                byte b1 = s_decoding[chars[charIndex++]];
                byte b2 = s_decoding[chars[charIndex++]];
                byte b3 = s_decoding[chars[charIndex++]];

                long buffer = b3;
                buffer <<= 6;
                buffer += b2;
                buffer <<= 6;
                buffer += b1;
                buffer <<= 6;
                buffer += b0;

                if (bufferIndex + 2 < decoded.Length) decoded[bufferIndex + 2] = (byte)(buffer & 0xFF);
                buffer >>= 8;
                if (bufferIndex + 1 < decoded.Length) decoded[bufferIndex + 1] = (byte)(buffer & 0xFF);
                buffer >>= 8;
                if (bufferIndex < decoded.Length) decoded[bufferIndex] = (byte)(buffer & 0xFF);
                bufferIndex += 3;
            }

            return decoded;
        }
    }
}
