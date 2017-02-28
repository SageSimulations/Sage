/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Utility {
	using System;
	using _Debug = System.Diagnostics.Debug;
	using System.Collections;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Highpoint.Sage.SimCore;

    /// <summary>
    /// Summary description for TupleTester.
	/// </summary>
	[TestClass]
	public class TupleTester {
		private ITupleSpace m_tsut; // ExchangeUnderTest.
		private ArrayList m_results;
		private IExecutive m_exec;
		private DateTime m_t;
		private ExecEventReceiver m_postIt;
		private ExecEventReceiver m_readIt;
		private ExecEventReceiver m_takeIt;
		private ExecEventReceiver m_blockingPostIt;
		private ExecEventReceiver m_blockingReadIt;
		private ExecEventReceiver m_blockingTakeIt;
        private ExecEventReceiver m_blockTilGone;
		private string k1 = "Object 1";

		public TupleTester() {
			m_exec = ExecFactory.Instance.CreateExecutive();
			m_t = new DateTime(2004,07,15,05,23,14);
			m_postIt = new ExecEventReceiver(PostTuple);
			m_readIt = new ExecEventReceiver(ReadTuple);
			m_takeIt = new ExecEventReceiver(TakeTuple);
			m_blockingPostIt = new ExecEventReceiver(BlockingPostTuple);
			m_blockingReadIt = new ExecEventReceiver(BlockingReadTuple);
			m_blockingTakeIt = new ExecEventReceiver(BlockingTakeTuple);
            m_blockTilGone = new ExecEventReceiver(BlockTilGoneTuple);
		}

		[TestInitialize] 
		public void Init() {}
		[TestCleanup]
		public void destroy() {
			_Debug.WriteLine( "Done." );
		}

		[TestMethod]
		public void TestTupleBasics(){
			string[] expected = new string[]{"632254657940000000:RT1:" + HashCode(k1), "632254657940000000:RT2b:" + HashCode(k1), "632254657940000000:PT1:" + HashCode(k1), "632254657940000000:PT2:" + HashCode(k1), "632254657940000000:TT1:" + HashCode(k1), "632254657940000000:TT2a:" + HashCode(k1)};
			InitTest();
			RequestEvent(XActType.Read,k1,0);
			RequestEvent(XActType.Post,k1,0);
			RequestEvent(XActType.Take,k1,0);
			m_exec.Start();
			EvaluateTest(expected,false);
		}

        private string HashCode(string k1) {
            return k1.GetHashCode().ToString();
        }

		[TestMethod]
		public void TestRead(){
            string[] expected = new string[] { "632254657940000000:PT1:" + HashCode(k1), "632254657940000000:PT2:" + HashCode(k1), "632254659740000000:RT1:" + HashCode(k1), "632254659740000000:RT2a:" + HashCode(k1) };
			InitTest();

			RequestEvent(XActType.Post,k1,0);
			RequestEvent(XActType.Read,k1,3);
			m_exec.Start();
			EvaluateTest(expected,false);
		}
		[TestMethod]
		public void TestTake(){
            string[] expected = new string[] { "632254657940000000:PT1:" + HashCode(k1), "632254657940000000:PT2:" + HashCode(k1), "632254659740000000:TT1:" + HashCode(k1), "632254659740000000:TT2a:" + HashCode(k1) };
			InitTest();
			RequestEvent(XActType.Post,k1,0);
			RequestEvent(XActType.Take,k1,3);
			m_exec.Start();
			EvaluateTest(expected,false);
		}
		[TestMethod]
		public void TestBlockingPost(){
			string[] expected = new string[]{"632254657940000000:BPT1:" + HashCode(k1), "632254657940000000:RT1:" + HashCode(k1), "632254657940000000:RT2a:" + HashCode(k1), "632254657940000000:RT1:" + HashCode(k1), "632254657940000000:RT2a:" + HashCode(k1), "632254657940000000:TT1:" + HashCode(k1), "632254657940000000:TT2a:" + HashCode(k1), "632254657940000000:BPT2:" + HashCode(k1)};
			InitTest();

			RequestEvent(XActType.BlockingPost,k1,0);
			RequestEvent(XActType.Read,k1,0);
			RequestEvent(XActType.Read,k1,0);
			RequestEvent(XActType.Take,k1,0);
			m_exec.Start();
			EvaluateTest(expected,false);
		}

		[TestMethod]
		public void TestBlockingRead(){
			string[] expected = new string[]{"632254657940000000:RT1:" + HashCode(k1), "632254657940000000:RT2b:" + HashCode(k1), "632254657940000000:BRT1:" + HashCode(k1), "632254657940000000:PT1:" + HashCode(k1), "632254657940000000:PT2:" + HashCode(k1), "632254657940000000:BRT2:" + HashCode(k1), "632254657940000000:TT1:" + HashCode(k1), "632254657940000000:TT2a:" + HashCode(k1)};
			InitTest();
			RequestEvent(XActType.Read,k1,0);
			RequestEvent(XActType.BlockingRead,k1,0);
			RequestEvent(XActType.Post,k1,0);
			RequestEvent(XActType.Take,k1,0);
			m_exec.Start();
			EvaluateTest(expected,false);
		}

		[TestMethod]
		public void TestBlockingTake(){
			string[] expected = new string[]{"632254657940000000:RT1:" + HashCode(k1), "632254657940000000:RT2b:" + HashCode(k1), "632254657940000000:BTT1:" + HashCode(k1), "632254657940000000:PT1:" + HashCode(k1), "632254657940000000:PT2:" + HashCode(k1), "632254657940000000:BTT2:" + HashCode(k1), "632254657940000000:RT1:" + HashCode(k1), "632254657940000000:RT2b:" + HashCode(k1)};
			InitTest();
			RequestEvent(XActType.Read,k1,0);
			RequestEvent(XActType.BlockingTake,k1,0);
			RequestEvent(XActType.Post,k1,0);
			RequestEvent(XActType.Read,k1,0);
			m_exec.Start();
			EvaluateTest(expected,false);
		}

        [TestMethod]
        public void TestBlock() {
            string[] expected = new string[] { "632254657940000000:PT1:" + HashCode(k1), "632254657940000000:PT2:" + HashCode(k1), "632254658540000000:WTG1:" + HashCode(k1), "632254659140000000:RT1:" + HashCode(k1), "632254659140000000:RT2a:" + HashCode(k1), "632254659740000000:TT1:" + HashCode(k1), "632254659740000000:TT2a:" + HashCode(k1), "632254659740000000:WTG2:" + HashCode(k1) };
            InitTest();

            RequestEvent(XActType.Post, k1, 0);
            RequestEvent(XActType.BlockTilGone, k1, 1);
            RequestEvent(XActType.Read, k1, 2);
            RequestEvent(XActType.Take, k1, 3);
            m_exec.Start();
            EvaluateTest(expected, false);
        }


		private enum XActType { Post, Read, Take, BlockingPost, BlockingRead, BlockingTake, BlockTilGone };

		private void RequestEvent(XActType xactType, string key, int when){
			ExecEventReceiver eer;
			switch ( xactType ) {
				case XActType.Post: { eer = m_postIt; break; }
				case XActType.Read: { eer = m_readIt; break; }
				case XActType.Take: { eer = m_takeIt; break; }
				case XActType.BlockingPost: { eer = m_blockingPostIt; break; }
				case XActType.BlockingRead: { eer = m_blockingReadIt; break; }
                case XActType.BlockingTake: { eer = m_blockingTakeIt; break; }
                case XActType.BlockTilGone: { eer = m_blockTilGone; break; }
                default: { eer = null; break; /*throw new ApplicationException("Unknown XActType.");*/ }
			}
			m_exec.RequestEvent(eer,m_t+TimeSpan.FromMinutes(when),0,key,ExecEventType.Detachable);
		}

		private void InitTest(){
			m_tsut = new Exchange(m_exec);
			m_results = new ArrayList();
			m_exec.Reset();
		}

		private void EvaluateTest(string[] expected, bool benchmark){
			if ( benchmark ) {
				Console.WriteLine("Expected result representation of the latest test run is:");
				string resultString = "";
				resultString += "new string[]{\"";
				for ( int i = 0 ; i < m_results.Count ; i++ ) {
					resultString += m_results[i];
					if ( i < (m_results.Count-1) ) {
						resultString += "\",\"";
					} else {
						resultString += "\"";
					}
				}
				resultString += "};";
				_Debug.WriteLine(resultString);
				//System.Windows.Forms.Clipboard.SetDataObject(resultString);
			} else {
				string msg = "Incorrect number of elements in \"Expected\" results.";
					if (expected.Length!=m_results.Count) _Debug.Assert(false,msg);
				for ( int i = 0 ; i < m_results.Count ; i++ ) {
					if (!expected[i].Equals(m_results[i])) {
						msg = "Argument mismatch in element " + i + " of the expected test results.";
                        _Debug.Assert(false,msg);
					}
				}
			}
		}

		private void PostTuple(IExecutive exec, object key){
			m_results.Add(""+exec.Now.Ticks+":PT1:"+key.GetHashCode());
			Console.WriteLine( exec.Now + " : Posting Tuple w/ prikey of " + key + ".");
			m_tsut.Post(key,DataFromKey(key),false);
			m_results.Add(""+exec.Now.Ticks+":PT2:"+key.GetHashCode());
			Console.WriteLine( exec.Now + " : Done posting Tuple w/ prikey of " + key + ".");
		}

		private void ReadTuple(IExecutive exec, object key){
			m_results.Add(""+exec.Now.Ticks+":RT1:"+key.GetHashCode());
			Console.Write( exec.Now + " : Reading Tuple w/ prikey of " + key + ".");
			ITuple tuple = m_tsut.Read(key,false);
			object data = (tuple==null?null:tuple.Data);
			if ( data != null ) {
				Console.WriteLine(" Tuple data = " + data + ".");
				m_results.Add(""+exec.Now.Ticks+":RT2a:"+key.GetHashCode());
			} else {
				Console.WriteLine(" " + key + " is an unknown priKey.");
				m_results.Add(""+exec.Now.Ticks+":RT2b:"+key.GetHashCode());
			}
		}

		private void TakeTuple(IExecutive exec, object key){
			Console.Write( exec.Now + " : Taking Tuple w/ prikey of " + key + ".");
			m_results.Add(""+exec.Now.Ticks+":TT1:"+key.GetHashCode());
			ITuple tuple = m_tsut.Take(key,false);
			object data = (tuple==null?null:tuple.Data);
			if ( data != null ) {
				Console.WriteLine(" Tuple data = " + data + ".");
				m_results.Add(""+exec.Now.Ticks+":TT2a:"+key.GetHashCode());
			} else {
				Console.WriteLine(" " + key + " is an unknown priKey.");
				m_results.Add(""+exec.Now.Ticks+":TT2b:"+key.GetHashCode());
			}
		}

		private void BlockingPostTuple(IExecutive exec, object key){
			Console.WriteLine( exec.Now + " : Starting blocking post of Tuple w/ prikey of " + key + ".");
			m_results.Add(""+exec.Now.Ticks+":BPT1:"+key.GetHashCode());
			m_tsut.Post(key,DataFromKey(key),true);
			Console.WriteLine( exec.Now + " : Done with blocking post of tuple data with key = " + key + ".");
			m_results.Add(""+exec.Now.Ticks+":BPT2:"+key.GetHashCode());
		}

		private void BlockingReadTuple(IExecutive exec, object key){
			Console.WriteLine( exec.Now + " : Starting blocking read of Tuple w/ prikey of " + key + ".");
			m_results.Add(""+exec.Now.Ticks+":BRT1:"+key.GetHashCode());
			object data = m_tsut.Read(key,true).Data;
			Console.WriteLine( exec.Now + " : Done with blocking post of tuple data = " + data + ".");
			m_results.Add(""+exec.Now.Ticks+":BRT2:"+key.GetHashCode());
		}

        private void BlockingTakeTuple(IExecutive exec, object key) {
            Console.WriteLine(exec.Now + " : Starting blocking take of Tuple w/ prikey of " + key + ".");
            m_results.Add("" + exec.Now.Ticks + ":BTT1:" + key.GetHashCode());
            object data = m_tsut.Take(key, true).Data;
            Console.WriteLine(exec.Now + " : Done with blocking take of tuple data = " + data + ".");
            m_results.Add("" + exec.Now.Ticks + ":BTT2:" + key.GetHashCode());
        }

        private void BlockTilGoneTuple(IExecutive exec, object key) {
            Console.WriteLine(exec.Now + " : Starting wait for departure of Tuple w/ prikey of " + key + ".");
            m_results.Add("" + exec.Now.Ticks + ":WTG1:" + key.GetHashCode());
            m_tsut.BlockWhilePresent(key);
            Console.WriteLine(exec.Now + " : Done with wait for departure of Tuple w/ prikey of " + key + ".");
            m_results.Add("" + exec.Now.Ticks + ":WTG2:" + key.GetHashCode());
        }

        private string DataFromKey(object key) { return "Data:" + key.ToString(); }
	}
}
