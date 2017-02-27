/* This source code licensed under the GNU Affero General Public License */


#if NODELETE

namespace Highpoint.Sage.Utility {
	/// <summary>
	/// Base 64 Encoding/Decoding. Can use System.Convert to do this - but this implementation is not
	/// compatible, since the string lengths don't necessarily match.
	/// </summary>
    [Obsolete("Please use \"System.Convert.FromBase64String(string s)\" and System.Convert.ToBase64String(byte[] ba);\"")]
	public class Base64Encoder {

		private static readonly byte[] s_decoding = new byte[]{
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

	    public static byte[] Decode(string _string) {
			byte[] chars = System.Text.Encoding.ASCII.GetBytes(_string);
			int nDummyBytes = chars[0] - '0';
			byte[] myDecoding = new byte[((int)((chars.Length-1)*(3.0/4.0))) - nDummyBytes];

			// take 4 characters at a time and make 3 bytes from them.
			long cc = 1;
			long bc = 0;

		    while ( cc <= chars.Length-4 ) {
				byte b0 = s_decoding[chars[cc++]];
                byte b1 = s_decoding[chars[cc++]];
                byte b2 = s_decoding[chars[cc++]];
                byte b3 = s_decoding[chars[cc++]];
			
				long bfr = b3; bfr<<=6;
				bfr += b2; bfr<<=6;
				bfr += b1; bfr<<=6;
				bfr += b0;
 
				if ( (bc+2) < myDecoding.Length ) myDecoding[bc+2] = (byte)(bfr&0xFF); bfr>>=8;
				if ( (bc+1) < myDecoding.Length ) myDecoding[bc+1] = (byte)(bfr&0xFF); bfr>>=8;
				if (  bc    < myDecoding.Length ) myDecoding[bc] = (byte)(bfr&0xFF);
				bc+=3;
			}


			return myDecoding;
		}
	}
}
#endif