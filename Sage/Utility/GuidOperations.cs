/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using Highpoint.Sage.SimCore;

namespace Highpoint.Sage.Utility {

    public static class GuidOps {

        private static System.Security.Cryptography.HashAlgorithm _hash;
        private static readonly object s_lock = new object();

        public static Guid Mask = Guid.Empty;

        public static Guid FromString(string src) {
            char[] ca = Convert.ToBase64String(Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(src))).ToCharArray();
            byte[] ba = new byte[16];
            for (int i = 0 ; i < ca.Length ; i++) {
                ba[i%16] = (byte)(ca[i]);
            }
            return new Guid(ba);
        }

        private static System.Security.Cryptography.HashAlgorithm Hash {
            get {
                if (_hash == null) {
                    lock (s_lock){
                        if (_hash == null) {
                            _hash = new System.Security.Cryptography.SHA1Managed();
                        }
                    }
                }
                return _hash;
            }
        }

        // ReSharper disable once InconsistentNaming
        public static Guid XOR(Guid a, Guid b) {
            byte[] aBytes = a.ToByteArray();
            byte[] bBytes = b.ToByteArray();
            byte[] cBytes = new byte[16];
            for (int i = 0; i < aBytes.Length; i++) {
                cBytes[i] = (byte)(aBytes[i] ^ bBytes[i]);
            }
            Guid c = new Guid(cBytes);
            return c;
        }

        public static Guid Increment(Guid a) {
            byte[] ba = a.ToByteArray();
            for (int i = ba.Length - 1; i >= 0; i--) {
                ba[i]++;
                if (ba[i] != 0)
                    break;
            }
            return new Guid(ba);
        }

        public static Guid Decrement(Guid a) {
            byte[] ba = a.ToByteArray();
            for (int i = ba.Length - 1; i >= 0; i--) {
                ba[i]--;
                if (ba[i] != 0xFF)
                    break;
            }
            return new Guid(ba);
        }

        public static Guid Add(Guid a, int n) {
            byte[] ba = a.ToByteArray();
            for (int h = 0; h < n; h++) {
                for (int i = ba.Length - 1; i >= 0; i--) {
                    ba[i]++;
                    if (ba[i] != 0)
                        break;
                }
            }
            return new Guid(ba);
        }

        /// <summary>
        /// Returns an integer that represents guid a minus guid b.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns></returns>
        public static int Subtract(Guid a, Guid b) {
            int n = 0;
            while (!a.Equals(b)) {
                b = Increment(b);
                n++;
            }
            return n;
        }

        public static Guid Subtract(Guid a, int n) {
            byte[] ba = a.ToByteArray();
            for (int h = 0; h < n; h++) {
                for (int i = ba.Length - 1; i >= 0; i--) {
                    ba[i]--;
                    if (ba[i] != 0xFF)
                        break;
                }
            }
            return new Guid(ba);
        }

        public static int Compare(Guid a, Guid b) {
            byte[] ba = a.ToByteArray();
            byte[] bb = b.ToByteArray();

            int comp = 0;
            for (int i = ba.Length - 1; i >= 0; i--) {
                comp = Comparer.Default.Compare(ba[i], bb[i]);
                if (comp != 0)
                    return comp;
            }
            return comp;
        }

        /// <summary>
        /// A comparer used for sorting on Guids. Note - Guids are sorted by binary order, which is not the same as
        /// display order for a number of reasons. If you want Guids in &quot;Visually&quot; sorted order, use the
        /// AsStringGuidComparer.
        /// </summary>
        public class GuidComparer : System.Collections.Generic.IComparer<Guid> {
            /// <summary>
            /// Compares the specified Guid a with the specified Guid b by binary values.
            /// </summary>
            /// <param name="a">One Guid.</param>
            /// <param name="b">The other Guid.</param>
            /// <returns>-1, 0 or 1, depending on the relationship between a &amp; b.</returns>
            public int Compare(Guid a, Guid b) { return GuidOps.Compare(a, b); }
        }

        /// <summary>
        /// A comparer use for sorting implementers of IHasIdentity on their Guids.
        /// </summary>
        /// <typeparam name="T">The actual type of the elements being sorted. Must implement IHasIdentity</typeparam>
        public class HasIdentityByGuidComparer<T> : System.Collections.Generic.IComparer<T> where T : IHasIdentity {
            /// <summary>
            /// Compares the specified a with the specified b by binary values.
            /// </summary>
            /// <param name="a">One IHasIdentity implementer.</param>
            /// <param name="b">The other IHasIdentity implementer.</param>
            /// <returns>-1, 0 or 1, depending on the relationship between a &amp; b.</returns>
            public int Compare(T a, T b) { return GuidOps.Compare(a.Guid, b.Guid); }
        }

        /// <summary>
        /// A comparer used for sorting on Guids. Note - Guids are sorted by visual order, which is not the same as
        /// binary order for a number of reasons. If you want Guids in &quot;Binary&quot; sorted order, use the
        /// GuidComparer.
        /// </summary>
        public class AsStringGuidComparer : System.Collections.Generic.IComparer<Guid> {
            /// <summary>
            /// Compares the specified Guid a with the specified Guid b by string representations.
            /// </summary>
            /// <param name="a">One Guid.</param>
            /// <param name="b">The other Guid.</param>
            /// <returns>-1, 0 or 1, depending on the relationship between a &amp; b.</returns>
            public int Compare(Guid a, Guid b) {
                return Comparer.Default.Compare(a.ToString(), b.ToString());
            }
        }

        /// <summary>
        /// Compares two guids as strings.
        /// </summary>
        /// <param name="a">One Guid.</param>
        /// <param name="b">The other Guid.</param>
        /// <returns>-1, 0 or 1, depending on the relationship between a &amp; b.</returns>
        public static int CompareAsString(Guid a, Guid b) {
            return Comparer.Default.Compare(a.ToString(), b.ToString());
        }

        //private static byte[] GuidByteSequence { get; } = {0, 15, 14, 13, 12, 11, 10, 9, 7, 8, 5, 6, 1, 2, 3, 4};
        //private static byte[] _masks = new byte[] { 1, 2, 4, 8, 16, 32, 64, 128 };

        /// <summary>
        /// Rotates the specified guid by the number of bits. Negative rotation is right-rotation. Be aware that it is the
        /// byte-array that is rotated, not the textual representation of the Guid, so the textual look of the rotated guids
        /// may be in a surprising order.
        /// </summary>
        /// <param name="a">The guid to rotate.</param>
        /// <param name="n">The number of places left to rotate it.</param>
        /// <returns></returns>
        public static Guid Rotate(Guid a, int n) {

            while (n < 0)
                n += 128;

            byte[] srcba = a.ToByteArray();
            byte[] dstba = new byte[16];

            int nBitsToShift = n % 8;
            int nBytesToShift = n >> 3;
            for (int j = 0; j < 16; j++) {
                int tgtNdx = j + nBytesToShift;
                if (tgtNdx > 15)
                    tgtNdx -= 16;

                //dstba[tgtNdx] = srcba[guidByteSequence[j]];
                dstba[tgtNdx] = srcba[j];
            }

            uint overflow = 0;
            for (int j = 0; j < 16; j++) {
                uint tmp = (uint)(dstba[j] << nBitsToShift);
                dstba[j] = (byte)(tmp + overflow);
                overflow = tmp >> 8;
                if (overflow < 0) overflow = 0;
            }
            dstba[0] += (byte)overflow;

            return new Guid(dstba);
        }

        //public static string guidMaskWODelimiters = @"[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}";

        // TODO: Move this into test suite.
#if TESTING
        private static void TestRotate() {
            Guid tstGuid = Increment(Guid.Empty);
            for (int i = -128; i < 128; i++) {
                Console.WriteLine(Rotate(tstGuid, i));
            }
            tstGuid = new Guid("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFE");
            for (int i = -128; i < 128; i++) {
                Console.WriteLine(Rotate(tstGuid, i));
            }
            Console.ReadLine();
        }

        private static void TestRotate2() {
            Guid mask = Guid.NewGuid();
            Guid tstGuid = Increment(Guid.Empty);
            //GuidGenerator gg = new GuidGenerator(Increment(Guid.Empty), new Guid("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFE"), 1);
            GuidGenerator gg = new GuidGenerator(new Guid("EFB07753-8492-445d-B9EF-DA7460A9832D"), new Guid("6A4179D9-890F-4612-96EF-1BB0F87153E2"), 1);
            System.Collections.Generic.List<Guid> guids = new System.Collections.Generic.List<Guid>();
            for (int i = 0; i < 100000; i++) {
                Guid g = gg.Next();
                Console.WriteLine(g);
                System.Diagnostics.Debug.Assert(!guids.Contains(g));
                guids.Add(g);
            }

            gg.Reset();
            for (int i = 0; i < 100000; i++) {
                Guid g = gg.Next();
                Console.WriteLine(g);
                System.Diagnostics.Debug.Assert(guids.Contains(g));
            }
            Console.ReadLine();
        }
#endif
    }
}
