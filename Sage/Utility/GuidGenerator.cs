/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.Utility {

    /// <summary>
    /// Generates a pseudorandom stream of Guids. Make sure that the maskGuid and
    /// seedGuids are 'sufficiently chaotic'. This generator is best used for testing.
    /// It is modeled after a linear feedback shift register. http://en.wikipedia.org/wiki/LFSR
    /// </summary>
    public class GuidGenerator {
        private readonly Guid m_seedGuid;
        private readonly Guid m_maskGuid;
        private readonly int m_rotateBits;
        private Guid m_current;
        private bool m_passThrough;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:GuidGenerator"/> class.
        /// </summary>
        /// <param name="seedGuid">The seed GUID - the starting register value.</param>
        /// <param name="maskGuid">The mask GUID - the polynomial.</param>
        /// <param name="rotateBits">The number of bits to rotate the register by.</param>
        public GuidGenerator(Guid seedGuid, Guid maskGuid, int rotateBits) {
            m_rotateBits = rotateBits;
            m_seedGuid = seedGuid;
            m_maskGuid = maskGuid;
            Reset();
        }

        /// <summary>
        /// Gets the next guid from this Guid Generator.
        /// </summary>
        /// <returns></returns>
        public Guid Next() {
            if (!m_passThrough) {
                m_current = GuidOps.Increment(m_current);
                m_current = GuidOps.XOR(m_maskGuid, m_current);
                m_current = GuidOps.Rotate(m_current, m_rotateBits);
                return m_current;
            } else {
                return Guid.NewGuid();
            }
        }

        /// <summary>
        /// Resets this Guid Generator to its initial settings.
        /// </summary>
        public void Reset() {
            m_current = m_seedGuid;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:GuidGenerator"/> is passthrough, meaning
        /// that if it is passthrough, it will simply generate a new Guid from Guid.NewGuid(); with every call
        /// to Next();
        /// </summary>
        /// <value><c>true</c> if passthrough; otherwise, <c>false</c>.</value>
        public bool Passthrough {
            set {
                m_passThrough = value;
            }
            get {
                return m_passThrough;
            }
        }
    }
}
