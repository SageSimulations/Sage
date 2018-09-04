/* This source code licensed under the GNU Affero General Public License */
using System.Collections.Generic;

namespace Highpoint.Sage.Utility
{

    /// <summary>
    /// Class UniqueNameGenerator creates unique names. 
    /// When asked for GetNextName("Dog") the first time, it produces "Dog:0".
    /// When asked for GetNextName("Dog") the second time, it produces "Dog:1".
    /// When asked for GetNextName("Cat") the first time, it produces "Cat:0".
    /// And so on.
    /// It is intended when automatically creating, say, 100 objects of type Restaurant,
    /// one would call myUniqueNameGenerator.GetNextName(typeof(Restaurant).Name, 3, false)
    /// to create Restaurant:001, Restaurant:002, Restaurant:003, etc.
    /// </summary>
    public class UniqueNameGenerator
    {
        private readonly Dictionary<string,UniqueNameData> m_uniqueNameData;

        /// <summary>
        /// Initializes a new instance of the <see cref="UniqueNameGenerator"/> class.
        /// </summary>
        public UniqueNameGenerator()
        {
            m_uniqueNameData = new Dictionary<string, UniqueNameData>();
        }

        /// <summary>
        /// Gets the next name for the provided seed, to the specified number 
        /// of places, with the index either zero-based or one-based, 
        /// depending on the value of 'zeroBased.'
        /// </summary>
        /// <param name="seed">The seed.</param>
        /// <param name="nPlaces">The number of places for the increasing index. It's up to the user to ensure this is adequate.</param>
        /// <param name="zeroBased">if set to <c>true</c> the index will be zero based. Ignored on all but the first call for a particular seed.</param>
        /// <returns>System.String.</returns>
        public string GetNextName(string seed, int nPlaces, bool zeroBased = false)
        {
            string key = seed + nPlaces;
            if (!m_uniqueNameData.ContainsKey(key))
            {
                m_uniqueNameData.Add(key, new UniqueNameData(nPlaces, zeroBased));
            }
            UniqueNameData und = m_uniqueNameData[key];
            return seed + und.NextSuffix();
        }

        private class UniqueNameData
        {
            private int m_nextIndex;
            private readonly string m_formatString;

            public UniqueNameData(int nPlaces, bool zeroBased)
            {
                m_formatString = string.Format("{{0:D{0}}}", nPlaces);
                m_nextIndex = zeroBased?0:1;
            }

            public string NextSuffix()
            {
                return string.Format(m_formatString, m_nextIndex++);
            }
        }
    }
}