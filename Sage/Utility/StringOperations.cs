/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.IO.Compression;

namespace Highpoint.Sage.Utility {
	/// <summary>
	/// Summary description for StringOperations.
	/// </summary>
    public static class StringOperations {
	    public static string NormalizeLength(string from, int nSpaces, Justification justification = Justification.Left, char filler = ' ') {
            if (from.Length > nSpaces)
                return from.Substring(0, nSpaces);

            string retval;
            switch (justification) {
                case Justification.Left: {
                        StringBuilder sb = new StringBuilder(from);
                        while (sb.Length <= nSpaces)
                            sb.Append(filler);
                        return sb.ToString();
                    }
                case Justification.Center: {
                        int length = from.Length;
                        int spacesLeft = (nSpaces - length) / 2;
                        int spacesRight = nSpaces - length - spacesLeft;

                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i <= spacesLeft; i++)
                            sb.Append(filler);
                        sb.Append(from);
                        for (int i = 0; i <= spacesRight; i++)
                            sb.Append(filler);
                        retval = sb.ToString();
                        break;
                    }

                case Justification.Right: {
                        StringBuilder sb = new StringBuilder();
                        while (sb.Length <= (nSpaces-from.Length))
                            sb.Append(filler);
                        sb.Append(from);
                        retval = sb.ToString();
                        break;
                    }

                default: {
                        throw new ApplicationException("Unknown text mode, " + justification + " requested.");
                    }
            }
            return retval;
        }

        public enum Justification { Left, Center, Right };

	    public static string Spaces(int n) { return NormalizeLength("", n); }

        /// <summary>
        /// Given IEnumerable of strings such as {"Foo", "Bar", "Baz"} returns a string representation, "Foo, Bar and Baz".
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        /// <returns>the string representation.</returns>
        public static string ToCommasAndAndedList(IEnumerable<string> enumerable) {
            IEnumerator<string> lstEnum = enumerable.GetEnumerator();

            string last = null;
            bool entryMade = false;

            StringBuilder sb = new StringBuilder();
            while (lstEnum.MoveNext()) {
                string nxtToLast = last;
                last = lstEnum.Current;
                if (nxtToLast == null) continue;
                if (entryMade) sb.Append(", ");
                sb.Append(nxtToLast);
                entryMade = true;
            }

            if (entryMade) sb.Append(" and ");
            sb.Append(last);

            return sb.ToString();
        }

        /// <summary>
        /// Given a list of {"Foo", "Bar", "Baz"} returns a string representation, "Foo, Bar and Baz".
        /// </summary>
        /// <param name="alist">The list.</param>
        /// <returns>the string representation.</returns>
        public static string ToCommasAndAndedList(System.Collections.ArrayList alist) {
            // Uses an adapter class built solely for this purpose. See below.
            return ToCommasAndAndedList(new ArrayListToIEnumOfStr(alist));
        }

        /// <summary>
        /// Given an IEnumerable of type T and a converter function from T to string, returns a string representation such as "Foo, Bar and Baz".
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ienum">The IEnumerable of type T.</param>
        /// <param name="converterFunc">The converter function.</param>
        /// <returns>the string representation.</returns>
        public static string ToCommasAndAndedList<T>(IEnumerable<T> ienum, Func<T,string> converterFunc) {
            return ToCommasAndAndedList(ienum.Select(converterFunc));
        }

        /// <summary>
        /// Given a List of type T, where T implements IHasName, returns a string representation such as "Foo, Bar and Baz".
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        /// <returns>the string representation.</returns>
        public static string ToCommasAndAndedListOfNames<T>(List<T> list) where T : SimCore.IHasName {
            return ToCommasAndAndedList(list.ConvertAll(n => n.Name));
        }

        #region Private Support Class
        /// <summary>
        /// A wrapper, used only in this class, to morph an arraylist that I know is going to be converted
        /// to strings, into an IEnumerable of strings - this way the same algo code as others can be used.
        /// </summary>
        private class ArrayListToIEnumOfStr : IEnumerable<string> {
            private readonly System.Collections.ArrayList m_alist;
            public ArrayListToIEnumOfStr(System.Collections.ArrayList alist) { m_alist = alist; }
            public IEnumerator<string> GetEnumerator() { return new EnumOfStr(m_alist); }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return m_alist.GetEnumerator(); }

            private class EnumOfStr : IEnumerator<string> {
                private readonly System.Collections.IEnumerator m_ienum;
                public EnumOfStr(System.Collections.ArrayList alist) { m_ienum = alist.GetEnumerator(); }
                public string Current => m_ienum.Current.ToString();
                object System.Collections.IEnumerator.Current => m_ienum.Current.ToString();
                void IDisposable.Dispose() { }
                public bool MoveNext() { return m_ienum.MoveNext(); }
                public void Reset() { m_ienum.Reset(); }
            }
        }

        #endregion 

        /// <summary>
        /// Returns a unique string in the context of the strings already in the collection. By default, if Dog exists in the list,
        /// and you pass in Dog, it will return Dog:1, then Dog:2 on the next call, and so on. Optionally, you can automatically
        /// update the list, and use a template for creating the new string.
        /// </summary>
        /// <param name="name">The name you want to use.</param>
        /// <param name="existingNames">the list of existing names.</param>
        /// <param name="addToList">if true, automatically updates the list with the new name.</param>
        /// <param name="template">The string format to use. It takes as {0} the name, and as {1}, the index, if any, to use.</param>
        /// <returns>the name you can use - that is unique.</returns>
        public static string UniqueString(string name, List<string> existingNames, bool addToList = false, string template = "{0}:{1}") {
            string tmpName = name;
            int ndx = 0;
            while ( existingNames.Contains(tmpName) ) {
                tmpName = string.Format(template, name, ndx++);
            }

            if ( addToList ) {
                existingNames.Add(tmpName);
            }
            return tmpName;
        }

        public static IComparer<string> StringSorterWithIntegralSuffix = new Pc();

        /// <summary>
        /// Compresses the string.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        public static string Compress(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            var memoryStream = new MemoryStream();
            using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
            {
                gZipStream.Write(buffer, 0, buffer.Length);
            }

            memoryStream.Position = 0;

            var compressedData = new byte[memoryStream.Length];
            memoryStream.Read(compressedData, 0, compressedData.Length);

            var gZipBuffer = new byte[compressedData.Length + 4];
            Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
            return Convert.ToBase64String(gZipBuffer);
        }

        /// <summary>
        /// Decompresses the string.
        /// </summary>
        /// <param name="compressedText">The compressed text.</param>
        /// <returns></returns>
        public static string Decompress(string compressedText)
        {
            byte[] gZipBuffer = Convert.FromBase64String(compressedText);
            using (var memoryStream = new MemoryStream())
            {
                int dataLength = BitConverter.ToInt32(gZipBuffer, 0);
                memoryStream.Write(gZipBuffer, 4, gZipBuffer.Length - 4);

                var buffer = new byte[dataLength];

                memoryStream.Position = 0;
                using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    gZipStream.Read(buffer, 0, buffer.Length);
                }

                return Encoding.UTF8.GetString(buffer);
            }
        }

        private class Pc : IComparer<string> {

            private static string _pattern = @"(\d+)$";

            private static readonly Regex s_regex = new Regex(_pattern, RegexOptions.Compiled);

            public int Compare(string x, string y) {
                Match matchX = s_regex.Match(x);
                Match matchY = s_regex.Match(y);
                int result;
                if ( matchX.Length == 0 || matchY.Length == 0 ) {
                    result = Comparer<string>.Default.Compare(x, y);
                } else {
                    string baseX = x.Substring(0, x.Length - matchX.Length);
                    string baseY = y.Substring(0, y.Length - matchY.Length);

                    result = Comparer<string>.Default.Compare(baseX, baseY);

                    if ( result == 0 ) {
                        int suffixX = int.Parse(matchX.Value);
                        int suffixY = int.Parse(matchY.Value);
                        result = Comparer<int>.Default.Compare(suffixX, suffixY);
                        if ( result == 0 ) {
                            result = Comparer<string>.Default.Compare(matchX.Value, matchY.Value);
                        }
                    }
                }
                return result;
            }
        }

        public class RandomStringGenerator {
            private readonly Random m_random;
            private readonly string m_letters;
            public RandomStringGenerator(int seed, string candidateCharacters) {
                m_letters = candidateCharacters;
                m_random = new Random(seed);
            }

            public RandomStringGenerator() : this((new Random()).Next(), "abcdefghijklmnopqrstuvwxyz") { }

            public string RandomString(int nChars) {
                StringBuilder sb = new StringBuilder(nChars);
                for (int i = 0; i < nChars; i++)
                    sb.Append(m_letters[m_random.Next(m_letters.Length)]);
                return sb.ToString();
            }
        }
    }

}
