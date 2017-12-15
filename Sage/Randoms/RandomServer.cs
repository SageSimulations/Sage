/* This source code licensed under the GNU Affero General Public License */
using System;
using Trace = System.Diagnostics.Debug;
using System.Threading;
// ReSharper disable ClassNeverInstantiated.Global

namespace Highpoint.Sage.Randoms {

    /// <summary>
    /// Class GlobalRandomServer is a singleton RandomServer that exists and can be obtained from anywhere in a process space. See 
    /// RandomServer for details.
    /// </summary>
    public class GlobalRandomServer
    {
        #region Private Fields
        private static readonly object s_lock = new object();
        private static volatile RandomServer _instance;
        private static ulong _seed = (ulong)DateTime.Now.Ticks;
        private static int _bufferSize;
        private static int _globalRandomChannelBufferSize;
        private static IRandomChannel _globalRandomChannel;
        private static ulong _globalRandomChannelSeed;
        #endregion

        /// <summary>
        /// Sets the random seed for the global random server. This is a super-seed 
        /// which is used to seed any channels not otherwise explicitly seeded that 
        /// are obtained from the Global Random Server.
        /// </summary>
        /// <param name="seed">The seed.</param>
        /// <exception cref="ApplicationException">Calls to GlobalRandomServer.SetSeed(long newSeed) must be performed before any call to GlobalRandomServer.Instance.</exception>
        public static void SetSeed(ulong seed)
        {
            if (_instance == null)
            {
                _seed = seed;
            }
            else
            {
                throw new ApplicationException("Calls to GlobalRandomServer.SetSeed(long newSeed) must be performed before any call to GlobalRandomServer.Instance.");
            }
        }

        /// <summary>
        /// Sets the size of the buffer for each of the double-buffer sides. 
        /// Generation is done into one buffer on a worker thread while 
        /// service is taken from the other.
        /// </summary>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <exception cref="ApplicationException">Calls to GlobalRandomServer.SetBufferSize(int bufferSize) must be performed before any call to GlobalRandomServer.Instance.</exception>
        public static void SetBufferSize(int bufferSize)
        {
            if (_instance == null)
            {
                _bufferSize = bufferSize;
            }
            else
            {
                throw new ApplicationException("Calls to GlobalRandomServer.SetBufferSize(int bufferSize) must be performed before any call to GlobalRandomServer.Instance.");
            }
        }

        /// <summary>
        /// Sets the seed for the GlobalRandomChannel. The seed must be set before the first call to use the GlobalRandomChannel.
        /// </summary>
        /// <param name="seed">the GlobalRandomChannel seed</param>
        /// <exception cref="ApplicationException">Calls to GlobalRandomServer.SetBufferSize(int bufferSize) must be performed before any call to GlobalRandomServer.Instance.</exception>
        public static void SetGlobalRandomChannelSeed(ulong seed)
        {
            if (_globalRandomChannel == null)
            {
                _globalRandomChannelSeed = seed;
            }
            else
            {
                throw new ApplicationException("Calls to GlobalRandomServer.SetGlobalRandomChannelSeed(ulong seed) must be performed before any call to GlobalRandomServer.GlobalRandomChannel.");
            }
        }

        /// <summary>
        /// Sets the size of the buffer for each of the double-buffer 
        /// sides of the GlobalRandomChannel. Generation is done into 
        /// one buffer on a worker thread while service is taken from 
        /// the other.
        /// </summary>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <exception cref="ApplicationException">Calls to GlobalRandomServer.SetGlobalRandomChannelBufferSize(int bufferSize) must be performed before any call to GlobalRandomServer.SetGlobalRandomChannelBufferSize.</exception>
        public static void SetGlobalRandomChannelBufferSize(int bufferSize)
        {
            if (_globalRandomChannel == null)
            {
                _globalRandomChannelBufferSize = bufferSize;
            }
            else
            {
                throw new ApplicationException("Calls to GlobalRandomServer.SetGlobalRandomChannelBufferSize(int bufferSize) must be performed before any call to GlobalRandomServer.GlobalRandomChannel.");
            }
        }

        /// <summary>
        /// Gets the global random channel.
        /// </summary>
        /// <value>The global random channel.</value>
        public static IRandomChannel GlobalRandomChannel => _globalRandomChannel ??
                                                            (_globalRandomChannel = Instance.GetRandomChannel(_globalRandomChannelSeed, _globalRandomChannelBufferSize));

        /// <summary>
        /// Gets the singleton instance of the global random server.
        /// </summary>
        /// <value>The instance.</value>
        public static RandomServer Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (s_lock)
                    {
                        if (_instance == null) _instance = new RandomServer(_seed, _bufferSize);
                    }
                }
                return _instance;
            }
        }
    }

	/// <summary>
	/// The random server serves channels from which fixed sequences of pseudo-random
	/// numbers can be drawn. Each channel is serviced by a Mersenne Twister Random
	/// Number Generator. The server can be created with or without a default buffer
	/// size, and thereafter, each channel can be created with or without a specified
	/// buffer size. If a channel's buffer size is not specified, it utilizes a buffer
	/// of the default size.
	/// <p></p>
	/// <b>Buffering Scheme:</b><p></p>
	/// Each channel, if it has a non-zero buffer size, utilizes a double buffering
	/// scheme where the PRNG is filling the back buffer in a producer thread while
	/// the model is using the front buffer in its consumer thread. Due to the Windows
	/// threading model, this results in better PRNG performance even when the consumer
	/// thread is processor-bound, at least over the longer-haul (more than a few tens
	/// of consumptions.) It will really shine when there is more than one processor
	///  in the system. You can play around with buffer size, and the effect probably
	///  varies somewhat with RNG consumption rate, but you might start with a number
	///  somewhere around 100. This will be the subject of study, some day...<p></p>
	///  <b>Note: </b>If using buffering, the model/consumer must call Dispose() on
	///  the Random Channel once it is finished with that Random Channel.
	/// <p></p>If there is a zero buffer size specified, the consumer always goes
	/// straight to the PRNG to get its next value. This option may be slightly faster
	/// in cases where the machine is running threads that are higher than user priority,
	/// and usually starving the system, but in our tests, it ran at about half the speed.
	/// In this case, there is no explicit need to call Dispose() on the Random Channel.<p></p>
	/// <b>Coming Enhancements:</b><p></p>
	/// Two, mainly. First, using a single thread per RandomServer, rather than per RandomChannel.
	/// And second, making it so that you don't have to call Dispose() any more.
	/// </summary>
	public class RandomServer {

        #region Private Fields
        private readonly MersenneTwisterFast m_seedGenerator;
        private readonly int m_defaultBufferSize;
        #endregion

        /// <summary>
        /// Creates a RandomServer with a specified hyperSeed and default buffer size.
        /// </summary>
        /// <param name="hyperSeed">This is the seed that will initialize the PRNG that
        /// provides seeds for RandomChannels that do not have a specified seed. This is
        /// a way of having an entire model's sequence be repeatable without having to
        /// hard code all of the RC's seed values.</param>
        /// <param name="defaultBufferSize">The buffer size that will be applied to channels
        /// that do not have an explicit buffer size specified. This provides a good way
        /// to switch the entire model's buffering scheme on or off at one location.</param>
        // ReSharper disable once UnusedParameter.Local
        public RandomServer(ulong hyperSeed, int defaultBufferSize = 0){           
            m_defaultBufferSize = 0;// defaultBufferSize;
			m_seedGenerator = new MersenneTwisterFast();
			m_seedGenerator.Initialize(hyperSeed);
		}

		/// <summary>
		/// Creates a RandomServer with a zero buffer size (and therefore single-threaded
		/// RandomChannels), and a hyperSeed that is based on the time of day.
		/// </summary>
		public RandomServer():this((ulong)DateTime.Now.Ticks){}

		/// <summary>
		/// Gets a RandomChannel with a specified seed and buffer size.
		/// </summary>
		/// <param name="seed">The seed value for the PRNG behind this channel.</param>
		/// <param name="bufferSize">The buffer size for this channel. Non-zero enables double-buffering.</param>
		/// <returns>The random channel from which random numbers may be obtained in a repeatable sequence.</returns>
		public IRandomChannel GetRandomChannel(ulong seed, int bufferSize){
			if ( bufferSize == 0 ) {
				return new RandomChannel(seed);
			} else {
				return new BufferedRandomChannel(seed,bufferSize);
			}
		}

		/// <summary>
		/// Gets a RandomChannel with a specified seed and buffer size.
		/// </summary>
		/// <param name="initArray">An array of unsigned longs that will be used to initialize the PRNG behind this channel.</param>
		/// <param name="bufferSize">The buffer size for this channel. Non-zero enables double-buffering.</param>
		/// <returns>The random channel from which random numbers may be obtained in a repeatable sequence.</returns>
		// ReSharper disable once UnusedParameter.Global
		public IRandomChannel GetRandomChannel(ulong[] initArray, int bufferSize = 0){
            if (bufferSize == 0)
            {
                return new RandomChannel(initArray);
            }
            else
            {
                return new BufferedRandomChannel(initArray, bufferSize);
            }
        }

		/// <summary>
		/// Gets a RandomChannel with a seed and buffer size provided by the RandomServer.
		/// </summary>
		public IRandomChannel GetRandomChannel(){
			return GetRandomChannel(m_seedGenerator.genrand_int32(),m_defaultBufferSize);
		}
	}

	/// <summary>
	/// Implemented by an object that can serve random numbers, similarly to the Math.Random() PRNG.
	/// </summary>
	public interface IRandomChannel : IDisposable {

		/// <summary>
		/// Produces the next pseudo random integer. Ranges from int.MinValue to int.MaxValue.
		/// </summary>
		/// <returns>The next pseudo random integer.</returns>
		int Next();

		/// <summary>
		/// Produces the next pseudo random integer. Ranges from int.MinValue to the argument maxValue.
		/// </summary>
		/// <param name="maxValue">The maximum value served by the PRNG, exclusive.</param>
		/// <returns>The next pseudo random integer in the range [minValue,maxValue).</returns>
		int Next(int maxValue);

		/// <summary>
		/// Produces the next pseudo random integer. Ranges from the argument minValue to the argument maxValue.
		/// </summary>
		/// <param name="minValue">The minimum value served by the PRNG, inclusive.</param>
		/// <param name="maxValue">The maximum value served by the PRNG, exclusive.</param>
		/// <returns>The next pseudo random integer in the range [minValue,maxValue).</returns>
		int Next(int minValue, int maxValue);

        /// <summary>
        /// Returns a random double between 0 (inclusive) and 1 (exclusive).
        /// </summary>
        /// <returns>The next random double in the range [0,1).</returns>
        double NextDouble();

        /// <summary>
        /// Returns a random double on the range [min,max), unless min == max,
        /// in which case it returns min.
        /// </summary>
        /// <returns>The next random double in the range [min,max).</returns>
        double NextDouble(double min, double max);

        /// <summary>
		/// Fills an array with random bytes.
		/// </summary>
		/// <param name="bytes"></param>
		void NextBytes(byte[] bytes);
	}

	internal delegate ulong ULongGetter();

	internal class RandomChannel : IRandomChannel {

		protected ULongGetter GetNextULong;
		protected readonly MersenneTwisterFast Mtf;
 
		public RandomChannel(ulong seed){
			Mtf = new MersenneTwisterFast();
			Mtf.Initialize(seed);
			GetNextULong = Mtf.genrand_int32;			
		}

		public RandomChannel(ulong[] initArray){
			Mtf = new MersenneTwisterFast();
			Mtf.Initialize(initArray);
			GetNextULong = Mtf.genrand_int32;			
		}

		~RandomChannel(){ Dispose(); }

		#region IRandomChannel Members

		public int Next() {
			int retval = (int)GetNextULong();
			return retval;
		}

		public int Next(int maxValue) {
			return ((int)GetNextULong())%maxValue;
		}

		public int Next(int minValue, int maxValue) {
			return (int)(minValue + ((uint)GetNextULong())%(maxValue-minValue));
		}

		/// <summary>
		/// [0,1)
		/// </summary>
		/// <returns></returns>
		public double NextDouble() {
			return GetNextULong()*(1.0/4294967296.0); 
			/* divided by 2^32 */
		}

        public double NextDouble(double min, double max) {
            //System.Diagnostics.Debug.Assert(min >= 0.0 && max <= 1.0 && min <= max);
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (min == max) return min;

            double x = GetNextULong()*(1.0/4294967296.0);
            x *= (max - min);
            return min + x;
        }


		public void NextBytes(byte[] bytes) {
			if ( bytes == null || bytes.Length == 0 ) return;
            int byteNum = bytes.Length;
			while( true ) {
				unchecked {
					ulong number = GetNextULong();
					bytes[byteNum] = (byte)(number&0xFF);
					number >>= 8;
					if ( byteNum-- == 0 ) break;
					bytes[byteNum] = (byte)(number&0xFF);
					number >>= 8;
					if ( byteNum-- == 0 ) break;
					bytes[byteNum] = (byte)(number&0xFF);
					number >>= 8;
					if ( byteNum-- == 0 ) break;
					bytes[byteNum] = (byte)(number&0xFF);
					//number >> 8;
					if ( byteNum-- == 0 ) break;
				}
			}
		}

		#endregion
	
		public virtual void Dispose() {}

	}

	internal class BufferedRandomChannel : RandomChannel {

        #region Private Fields
        private static readonly int s_min_Buffer_Size = 10;
        private readonly object m_lockObject = new object();
        private Thread m_bufferThread;
        private int m_bufferSize;
        private ulong[] m_bufferA;
        private ulong[] m_bufferB;
        private ulong[] m_inUse;
        private int m_nextInUseCell;
        private ulong[] m_beingFilled;
        private int m_nFills;
        private int m_nExpectedFills = 1; 
        #endregion

        public BufferedRandomChannel(ulong seed, int bufferSize):base(seed){
			GetNextULong = BufferedGetNextULong;
			Init(bufferSize);
		}

		public BufferedRandomChannel(ulong[] initArray, int bufferSize):base(initArray){
			Init(bufferSize);
			GetNextULong = BufferedGetNextULong;
		}

		~BufferedRandomChannel(){ Dispose(); }

		private void Init(int bufferSize){
			m_bufferSize = bufferSize>s_min_Buffer_Size?bufferSize:s_min_Buffer_Size;
			m_bufferA = new ulong[bufferSize];
			m_bufferB = new ulong[bufferSize];
			m_beingFilled = m_bufferA;
			for ( int i = 0 ; i < m_bufferSize ; i++ ) m_bufferA[i] = Mtf.genrand_int32();
			m_inUse = m_bufferA;
			m_beingFilled = m_bufferB;
			m_bufferThread = new Thread(FillBuffer);
			m_bufferThread.Start();
			// Must wait for the buffer thread to lock on the lockObject.
		    while (!m_bufferThread.ThreadState.Equals(ThreadState.WaitSleepJoin)) {/* spinwait for the buffer thread to lock on the lockObject.*/ }
		    m_bufferThread.IsBackground = true; // Allow thread termination.
		}

		private ulong BufferedGetNextULong(){
			unchecked {
				if ( m_nextInUseCell == m_bufferSize ) {
					SwapBuffers();
				}
				return m_inUse[m_nextInUseCell++];
			}
		}

		private void SwapBuffers(){
			lock ( m_lockObject ) {
				// The next few lines make sure that we can't come back and swap buffers
				// before the producer thread has had a chance to refill the back buffer.
				while ( m_nFills != m_nExpectedFills ){
					Monitor.Pulse(m_lockObject);
					Monitor.Wait(m_lockObject);
				}
				m_nExpectedFills = m_nFills+1;
				ulong[] tmp = m_inUse;
				m_inUse = m_beingFilled;
				m_beingFilled = tmp;
				m_nextInUseCell = 0;
				Monitor.Pulse(m_lockObject);
			}
		}


		private void FillBuffer(){
            try {
                unchecked {
                    lock (m_lockObject) {
                        while (true) {
                            for (int i = 0 ; i < m_bufferSize ; i++)
                                m_beingFilled[i] = Mtf.genrand_int32();
                            m_nFills++; // Mark this iteration complete.
                            Monitor.Pulse(m_lockObject); // In case SwapBuffers is waiting.
                            Monitor.Wait(m_lockObject);
                        }
                    }
                }
            } catch (ThreadAbortException) {
                Thread.ResetAbort();
            }
		}

		#region IDisposable Members
		public override void Dispose() {
			try
			{
			    m_bufferThread?.Abort();
			}
			catch(ThreadStateException){}
		}

		#endregion
	}
}