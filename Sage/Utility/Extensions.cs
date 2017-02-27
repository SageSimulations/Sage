/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.Utility {
    public static class Extensions {

        /// <summary>
        /// Performs a bitwise XOR operation of this Guid and the other Guid.
        /// </summary>
        /// <param name="guid">The unique identifier.</param>
        /// <param name="otherGuid">The other unique identifier.</param>
        /// <returns>Guid.</returns>
        [System.Diagnostics.DebuggerStepThrough]
        // ReSharper disable once InconsistentNaming
        public static Guid XOR(this Guid guid, Guid otherGuid) {
            return GuidOps.XOR(guid, otherGuid);
        }

        /// <summary>
        /// Bytewise increments the specified unique identifier by the amount specified in the second Guid.
        /// </summary>
        /// <param name="guid">The unique identifier.</param>
        /// <returns>Guid.</returns>
        [System.Diagnostics.DebuggerStepThrough]
        public static Guid Increment(this Guid guid) {
            return GuidOps.Increment(guid);
        }

        /// <summary>
        /// Bytewise adds the two Guids together.
        /// </summary>
        /// <param name="guid">The unique identifier.</param>
        /// <param name="value">The value.</param>
        /// <returns>Guid.</returns>
        [System.Diagnostics.DebuggerStepThrough]
        public static Guid Add(this Guid guid, int value) {
            return GuidOps.Add(guid, value);
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// Performs a bitwise XOR operation of this byte array and the other byte array.
        /// </summary>
        /// <param name="thisBa">this byte array.</param>
        /// <param name="thatBa">that byte array.</param>
        /// <returns>System.Byte[].</returns>
        /// <exception cref="ArgumentException">Cannot XOR two byte arrays of unequal length.</exception>
        public static byte[] XOR(this byte[] thisBa, byte[] thatBa) {
            if (thisBa.LongLength != thatBa.LongLength) {
                throw new ArgumentException("Cannot XOR two byte arrays of unequal length.");
            }
            long baLength = thisBa.Length;
            byte[] retval = new byte[baLength];
            for (int i = 0; i < baLength; i++) {
                retval[i] = (byte)(thisBa[i] ^ thatBa[i]);
            }
            return retval;
        }

        /// <summary>
        /// Rotates the specified byte by the rot count of bits. Left rot is positive.
        /// </summary>
        /// <param name="thisB">The specified byte.</param>
        /// <param name="rotCount">The rot count.</param>
        public static byte Rotate(this byte thisB, int rotCount) {
            rotCount = rotCount % 8;
            if (rotCount < 0) rotCount += 8; // Right 3 is the same as left 5.
            int tmp = thisB << rotCount;
            return (byte)((tmp & 0xFF) + (tmp >> 8));
        }

        /// <summary>
        /// Rotates the specified byte array by the rot count of bits. Left rot is positive.
        /// </summary>
        /// <param name="thisBa">The this BA.</param>
        /// <param name="rotCount">The rot count.</param>
        public static void Rotate(this byte[] thisBa, long rotCount) {
            long length = thisBa.LongLength;
            rotCount = rotCount % length;
            byte[] tmp = new byte[thisBa.LongLength];
            long bitsRot = rotCount % 8;
            long bytesRot = (long)((rotCount - bitsRot) / 8.0);
            for (long i = 0; i < thisBa.LongLength; i++) {
                long targetByte = i + bytesRot;
                if (targetByte < 0) targetByte += length;
                if (targetByte > length) targetByte -= length;
                tmp[targetByte] = thisBa[i].Rotate((int)bitsRot);
            }
            for (int i = 0; i < thisBa.LongLength; i++) {
                thisBa[i] = tmp[i];
            }
        }
    }
}
