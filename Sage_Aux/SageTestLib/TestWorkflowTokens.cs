/* This source code licensed under the GNU Affero General Public License */
#if WORKFLOW_INCLUDED

using Trace = System.Diagnostics.Debug;

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Highpoint.Sage.Utility;
using System.IO;
using System.Text; 


using Highpoint.Sage.Workflow;

namespace Highpoint.Sage.Workflow {

    /// <summary>
    /// Summary description for zTestWorkflowTokens.
    /// </summary>
    [TestClass]
    public class zTestWorkflowTokens {

        class DummyElement : IWorkflowElement {
            private int m_deNum;
            public DummyElement(int num) { m_deNum = num; }
            public override string ToString() { return "Dummy Element " + m_deNum; }

            #region IPortOwner Members

            public void AddPort(Highpoint.Sage.ItemBased.Ports.IPort port) {
                throw new Exception("The method or operation is not implemented.");
            }

            public Highpoint.Sage.ItemBased.Ports.IPort AddPort(string channelTypeName) {
                throw new Exception("The method or operation is not implemented.");
            }

            public Highpoint.Sage.ItemBased.Ports.IPort AddPort(string channelTypeName, Guid guid) {
                throw new Exception("The method or operation is not implemented.");
            }

            public List<Highpoint.Sage.ItemBased.Ports.IPortChannelInfo> SupportedChannelInfo {
                get { throw new Exception("The method or operation is not implemented."); }
            }

            public void RemovePort(Highpoint.Sage.ItemBased.Ports.IPort port) {
                throw new Exception("The method or operation is not implemented.");
            }

            public void ClearPorts() {
                throw new Exception("The method or operation is not implemented.");
            }

            public Highpoint.Sage.ItemBased.Ports.IPortSet Ports {
                get { throw new Exception("The method or operation is not implemented."); }
            }

            #endregion

            #region IModelObject Members

            public Highpoint.Sage.SimCore.IModel Model {
                get { throw new Exception("The method or operation is not implemented."); }
            }

            public void InitializeIdentity(Highpoint.Sage.SimCore.IModel model, string name, string description, Guid guid) {
                throw new Exception("The method or operation is not implemented.");
            }

            #endregion

            #region IHasIdentity Members

            public string Description {
                get { throw new Exception("The method or operation is not implemented."); }
            }

            public Guid Guid {
                get { throw new Exception("The method or operation is not implemented."); }
            }

            #endregion

            #region IHasName Members

            public string Name {
                get { throw new Exception("The method or operation is not implemented."); }
            }

            #endregion
        }

        class DummyCase : IWorkflowCase {
            #region IModelObject Members

            public Highpoint.Sage.SimCore.IModel Model {
                get { throw new Exception("The method or operation is not implemented."); }
            }

            public void InitializeIdentity(Highpoint.Sage.SimCore.IModel model, string name, string description, Guid guid) {
                throw new Exception("The method or operation is not implemented.");
            }

            #endregion

            #region IHasIdentity Members

            public string Description {
                get { throw new Exception("The method or operation is not implemented."); }
            }

            public Guid Guid {
                get { throw new Exception("The method or operation is not implemented."); }
            }

            #endregion

            #region IHasName Members

            public string Name {
                get { throw new Exception("The method or operation is not implemented."); }
            }

            #endregion

            #region IWorkflowCase Members

#pragma warning disable 67 // Ignore it if this event is not used. It's a framework, and this event may be for clients.
            public event WorkflowCaseEvent CaseCompleted;
#pragma warning restore 67

            public Token RootToken {
                get { throw new Exception("The method or operation is not implemented."); }
            }

            #endregion
        }

        delegate void Action();

        #region MSTest Goo
        [TestInitialize]
        public void Init() {
        }
        [TestCleanup]
        public void destroy() {
            Trace.WriteLine("Done.");
        }
        #endregion

        private static StringBuilder m_sb = null;
        private static StringWriter m_sw = null;
        private static int m_deNum = 0;

        public zTestWorkflowTokens() { }

        [TestMethod]
        public void TestTokenStateTransitions_Basic() {

            Token t;
            m_sb = new StringBuilder();
            m_sw = new StringWriter(m_sb);
            m_deNum = 0;

            t = NewToken();
            Debug.Assert(!TryToEnter(t, null));

            m_sw.WriteLine("---------------------------------------------------");
            t = NewToken();
            Debug.Assert(TryToEnter(t, NewDummyElement()));
            Debug.Assert(!TryToChange(t, new Action(t.Complete)));

            m_sw.WriteLine("---------------------------------------------------");
            t = NewToken();
            Debug.Assert(!TryToExit(t));

            m_sw.WriteLine("---------------------------------------------------");
            t = NewToken();
            Debug.Assert(TryToEnter(t, NewDummyElement()));
            Debug.Assert(TryToExit(t));

            m_sw.WriteLine("---------------------------------------------------");
            t = NewToken();
            Debug.Assert(TryToEnter(t, NewDummyElement()));
            Debug.Assert(TryToChange(t, new Action(t.Start)));
            Debug.Assert(TryToChange(t, new Action(t.Stop)));
            Debug.Assert(TryToExit(t));
            Debug.Assert(TryToEnter(t, NewDummyElement()));
            Debug.Assert(TryToChange(t, new Action(t.Start)));
            Debug.Assert(TryToChange(t, new Action(t.Stop)));
            Debug.Assert(TryToExit(t));
            Debug.Assert(TryToChange(t, new Action(t.Complete)));

            m_sw.WriteLine("---------------------------------------------------");
            t = NewToken();
            Debug.Assert(TryToEnter(t, NewDummyElement()));
            Debug.Assert(TryToChange(t, new Action(t.Start)));
            Debug.Assert(!TryToChange(t, new Action(t.Complete)));
            Debug.Assert(TryToChange(t, new Action(t.Terminate)));

            m_sw.WriteLine("---------------------------------------------------");
            t = NewToken();
            Debug.Assert(TryToEnter(t, NewDummyElement()));
            Debug.Assert(TryToChange(t, new Action(t.Start)));
            Debug.Assert(TryToChange(t, new Action(t.Terminate)));
            Debug.Assert(!TryToChange(t, new Action(t.Complete)));

            m_sw.WriteLine("---------------------------------------------------");
            t = NewToken();
            Debug.Assert(TryToEnter(t, NewDummyElement()));
            Debug.Assert(TryToChange(t, new Action(t.Terminate)));

            m_sw.WriteLine("---------------------------------------------------");
            t = NewToken();
            Debug.Assert(TryToEnter(t, NewDummyElement()));
            Debug.Assert(!TryToChange(t, new Action(t.Complete)));

            m_sw.WriteLine("---------------------------------------------------");
            t = NewToken();
            Debug.Assert(TryToEnter(t, NewDummyElement()));
            Debug.Assert(TryToChange(t, new Action(t.Start)));
            Debug.Assert(!TryToChange(t, new Action(t.Start)));

            Debug.Assert(m_sb.ToString().Equals(m_stbExpected_TestTokenStateTransitions_Basic));
        }

        [TestMethod]
        public void TestTokenPropagation_Basic() {
            Token t;
            m_sb = new StringBuilder();
            m_sw = new StringWriter(m_sb);
            m_deNum = 0;

            t = NewToken();
            Debug.Assert(TryToEnter(t, NewDummyElement()));
            Debug.Assert(TryToChange(t, new Action(t.Start)));

            List<Token> kids = new List<Token>();
            for (int i = 0 ; i < 4 ; i++) {
                Token k = t.Spawn();
                k.TokenStateChangedFrom += new Token.TokenStateChange(t_TokenStateChangedFrom);
                k.TokenStateChangingTo += new Token.TokenStateChange(t_TokenStateChangingTo);
                kids.Add(k);
            }

            kids.ForEach(delegate(Token k) {
                k.Enter(NewDummyElement(),null);
                Debug.Assert(!TryToChange(t, new Action(t.Stop)));
                k.Start();
                Debug.Assert(!TryToChange(t, new Action(t.Stop)));
                k.Stop();
                Debug.Assert(!TryToChange(t, new Action(t.Stop)));
                k.Exit();
                Debug.Assert(!TryToChange(t, new Action(t.Stop)));
                if (kids.IndexOf(k) % 2 == 0)
                    k.Complete();
                else
                    k.Terminate();
            }
                );

            kids.ForEach(delegate(Token k) {
                Debug.Assert(!TryToChange(t, new Action(t.Stop)));
                t.SubsumeChild(k);
            }
            );

            Debug.Assert(TryToChange(t, new Action(t.Stop)));

            Debug.Assert(m_sb.ToString().Equals(m_stbExpected_TestTokenPropagation_Basic));

        }

        #region Support Methods

        Token NewToken() {
            Token t = new Token(new DummyCase());
            t.TokenStateChangedFrom += new Token.TokenStateChange(t_TokenStateChangedFrom);
            t.TokenStateChangingTo += new Token.TokenStateChange(t_TokenStateChangingTo);
            return t;
        }

        void t_TokenStateChangingTo(Token token, Token.TokenState state) {
            m_sw.Write("\t" + token.State + " -> " + state + "? ==> ");
        }

        void t_TokenStateChangedFrom(Token token, Token.TokenState state) {
            m_sw.WriteLine( state + " -> " + token.State + "!");
        }

        DummyElement NewDummyElement() {
            DummyElement de = new DummyElement(m_deNum++);
            return de;
        }

        private static bool TryToEnter(Token t, DummyElement element) {
            m_sw.WriteLine(t.State.ToString() + "\t: Enter(" + element + ")");
            //m_sw.WriteLine("\r\nAttempting to call Enter(" + element + ") with a token in the " + t.State.ToString() + " state.");
            bool result = true;
            try {
                t.Enter(element, null);
            } catch (Token.IllegalOperationException ioe) {
                m_sw.WriteLine("\tFAIL : " + ioe.Message);
                result = false;
            } finally {
                m_sw.WriteLine("\tToken in " + t.State.ToString() + ", held by " + ( t.CurrentHolder == null ? "<null>" : t.CurrentHolder.ToString() ) + ".");
            }
            return result;
        }


        private static bool TryToChange(Token t, Action p) {
            m_sw.WriteLine(t.State.ToString() + "\t: " + p.Method.Name + "()");
            //m_sw.WriteLine("\r\nAttempting to call " + p.Method.Name + " with a token in the " + t.State.ToString() + " state.");
            bool result = true;
            try {
                p();
            } catch (Token.IllegalOperationException ioe) {
                m_sw.WriteLine("\tFAIL : " + ioe.Message);
                result = false;
            } finally {
                m_sw.WriteLine("\tToken in " + t.State.ToString() + ", held by " + ( t.CurrentHolder == null ? "<null>" : t.CurrentHolder.ToString() ) + ".");
            }
            return result;
        }

        private static bool TryToExit(Token t) {
            m_sw.WriteLine(t.State.ToString() + "\t: Exit()");
            bool result = true;
            try {
                t.Exit();
            } catch (Token.IllegalOperationException ioe) {
                m_sw.WriteLine("\tFAIL : " + ioe.Message);
                result = false;
            } finally {
                m_sw.WriteLine("\tToken in " + t.State.ToString() + ", held by " + ( t.CurrentHolder == null ? "<null>" : t.CurrentHolder.ToString() ) + ".");
            }
            return result;
        }

        #endregion

        #region Expected Values

        private string m_stbExpected_TestTokenStateTransitions_Basic = 
@"Idle	: Enter()
	FAIL : Attempt to call Enter(null) - this is illegal.
	Token in Idle, held by <null>.
---------------------------------------------------
Idle	: Enter(Dummy Element 0)
	Token in Idle, held by Dummy Element 0.
Idle	: Complete()
	FAIL : Caller tried to call Complete() on a token held by Dummy Element 0 that is in the Idle state. This is illegal.
	Token in Idle, held by Dummy Element 0.
---------------------------------------------------
Idle	: Exit()
	FAIL : Attempt to call Exit() on a token that is not currently held by any workflow element.
	Token in Idle, held by <null>.
---------------------------------------------------
Idle	: Enter(Dummy Element 1)
	Token in Idle, held by Dummy Element 1.
Idle	: Exit()
	Token in Idle, held by <null>.
---------------------------------------------------
Idle	: Enter(Dummy Element 2)
	Token in Idle, held by Dummy Element 2.
Idle	: Start()
	Idle -> Running? ==> Idle -> Running!
	Token in Running, held by Dummy Element 2.
Running	: Stop()
	Running -> Idle? ==> Running -> Idle!
	Token in Idle, held by Dummy Element 2.
Idle	: Exit()
	Token in Idle, held by <null>.
Idle	: Enter(Dummy Element 3)
	Token in Idle, held by Dummy Element 3.
Idle	: Start()
	Idle -> Running? ==> Idle -> Running!
	Token in Running, held by Dummy Element 3.
Running	: Stop()
	Running -> Idle? ==> Running -> Idle!
	Token in Idle, held by Dummy Element 3.
Idle	: Exit()
	Token in Idle, held by <null>.
Idle	: Complete()
	Idle -> Completed? ==> Idle -> Completed!
	Token in Completed, held by <null>.
---------------------------------------------------
Idle	: Enter(Dummy Element 4)
	Token in Idle, held by Dummy Element 4.
Idle	: Start()
	Idle -> Running? ==> Idle -> Running!
	Token in Running, held by Dummy Element 4.
Running	: Complete()
	FAIL : Caller tried to call Complete() on a token held by Dummy Element 4 that is in the Running state. This is illegal.
	Token in Running, held by Dummy Element 4.
Running	: Abort()
	Running -> Aborted? ==> Running -> Aborted!
	Token in Aborted, held by Dummy Element 4.
---------------------------------------------------
Idle	: Enter(Dummy Element 5)
	Token in Idle, held by Dummy Element 5.
Idle	: Start()
	Idle -> Running? ==> Idle -> Running!
	Token in Running, held by Dummy Element 5.
Running	: Abort()
	Running -> Aborted? ==> Running -> Aborted!
	Token in Aborted, held by Dummy Element 5.
Aborted	: Complete()
	FAIL : Caller tried to call Complete() on a token held by Dummy Element 5 that is in the Aborted state. This is illegal.
	Token in Aborted, held by Dummy Element 5.
---------------------------------------------------
Idle	: Enter(Dummy Element 6)
	Token in Idle, held by Dummy Element 6.
Idle	: Abort()
	Idle -> Aborted? ==> Idle -> Aborted!
	Token in Aborted, held by Dummy Element 6.
---------------------------------------------------
Idle	: Enter(Dummy Element 7)
	Token in Idle, held by Dummy Element 7.
Idle	: Complete()
	FAIL : Caller tried to call Complete() on a token held by Dummy Element 7 that is in the Idle state. This is illegal.
	Token in Idle, held by Dummy Element 7.
---------------------------------------------------
Idle	: Enter(Dummy Element 8)
	Token in Idle, held by Dummy Element 8.
Idle	: Start()
	Idle -> Running? ==> Idle -> Running!
	Token in Running, held by Dummy Element 8.
Running	: Start()
	FAIL : Caller tried to call Start() on a token held by Dummy Element 8 that is in the Running state. This is illegal.
	Token in Running, held by Dummy Element 8.
";

        
        private string m_stbExpected_TestTokenPropagation_Basic =
@"Idle	: Enter(Dummy Element 0)
	Token in Idle, held by Dummy Element 0.
Idle	: Start()
	Idle -> Running? ==> Idle -> Running!
	Token in Running, held by Dummy Element 0.
	Running -> Parenting? ==> Running -> Parenting!
Parenting	: Stop()
	FAIL : Caller tried to call Stop() on a token held by Dummy Element 0 that is in the Parenting state. This is illegal.
	Token in Parenting, held by Dummy Element 0.
	Idle -> Running? ==> Idle -> Running!
Parenting	: Stop()
	FAIL : Caller tried to call Stop() on a token held by Dummy Element 0 that is in the Parenting state. This is illegal.
	Token in Parenting, held by Dummy Element 0.
	Running -> Idle? ==> Running -> Idle!
Parenting	: Stop()
	FAIL : Caller tried to call Stop() on a token held by Dummy Element 0 that is in the Parenting state. This is illegal.
	Token in Parenting, held by Dummy Element 0.
Parenting	: Stop()
	FAIL : Caller tried to call Stop() on a token held by Dummy Element 0 that is in the Parenting state. This is illegal.
	Token in Parenting, held by Dummy Element 0.
	Idle -> Completed? ==> Idle -> Completed!
Parenting	: Stop()
	FAIL : Caller tried to call Stop() on a token held by Dummy Element 0 that is in the Parenting state. This is illegal.
	Token in Parenting, held by Dummy Element 0.
	Idle -> Running? ==> Idle -> Running!
Parenting	: Stop()
	FAIL : Caller tried to call Stop() on a token held by Dummy Element 0 that is in the Parenting state. This is illegal.
	Token in Parenting, held by Dummy Element 0.
	Running -> Idle? ==> Running -> Idle!
Parenting	: Stop()
	FAIL : Caller tried to call Stop() on a token held by Dummy Element 0 that is in the Parenting state. This is illegal.
	Token in Parenting, held by Dummy Element 0.
Parenting	: Stop()
	FAIL : Caller tried to call Stop() on a token held by Dummy Element 0 that is in the Parenting state. This is illegal.
	Token in Parenting, held by Dummy Element 0.
	Idle -> Aborted? ==> Idle -> Aborted!
Parenting	: Stop()
	FAIL : Caller tried to call Stop() on a token held by Dummy Element 0 that is in the Parenting state. This is illegal.
	Token in Parenting, held by Dummy Element 0.
	Idle -> Running? ==> Idle -> Running!
Parenting	: Stop()
	FAIL : Caller tried to call Stop() on a token held by Dummy Element 0 that is in the Parenting state. This is illegal.
	Token in Parenting, held by Dummy Element 0.
	Running -> Idle? ==> Running -> Idle!
Parenting	: Stop()
	FAIL : Caller tried to call Stop() on a token held by Dummy Element 0 that is in the Parenting state. This is illegal.
	Token in Parenting, held by Dummy Element 0.
Parenting	: Stop()
	FAIL : Caller tried to call Stop() on a token held by Dummy Element 0 that is in the Parenting state. This is illegal.
	Token in Parenting, held by Dummy Element 0.
	Idle -> Completed? ==> Idle -> Completed!
Parenting	: Stop()
	FAIL : Caller tried to call Stop() on a token held by Dummy Element 0 that is in the Parenting state. This is illegal.
	Token in Parenting, held by Dummy Element 0.
	Idle -> Running? ==> Idle -> Running!
Parenting	: Stop()
	FAIL : Caller tried to call Stop() on a token held by Dummy Element 0 that is in the Parenting state. This is illegal.
	Token in Parenting, held by Dummy Element 0.
	Running -> Idle? ==> Running -> Idle!
Parenting	: Stop()
	FAIL : Caller tried to call Stop() on a token held by Dummy Element 0 that is in the Parenting state. This is illegal.
	Token in Parenting, held by Dummy Element 0.
Parenting	: Stop()
	FAIL : Caller tried to call Stop() on a token held by Dummy Element 0 that is in the Parenting state. This is illegal.
	Token in Parenting, held by Dummy Element 0.
	Idle -> Aborted? ==> Idle -> Aborted!
Parenting	: Stop()
	FAIL : Caller tried to call Stop() on a token held by Dummy Element 0 that is in the Parenting state. This is illegal.
	Token in Parenting, held by Dummy Element 0.
	Completed -> Subsumed? ==> Completed -> Subsumed!
Parenting	: Stop()
	FAIL : Caller tried to call Stop() on a token held by Dummy Element 0 that is in the Parenting state. This is illegal.
	Token in Parenting, held by Dummy Element 0.
	Aborted -> Subsumed? ==> Aborted -> Subsumed!
Parenting	: Stop()
	FAIL : Caller tried to call Stop() on a token held by Dummy Element 0 that is in the Parenting state. This is illegal.
	Token in Parenting, held by Dummy Element 0.
	Completed -> Subsumed? ==> Completed -> Subsumed!
Parenting	: Stop()
	FAIL : Caller tried to call Stop() on a token held by Dummy Element 0 that is in the Parenting state. This is illegal.
	Token in Parenting, held by Dummy Element 0.
	Aborted -> Subsumed? ==> Aborted -> Subsumed!
	Parenting -> Running? ==> Parenting -> Running!
Running	: Stop()
	Running -> Idle? ==> Running -> Idle!
	Token in Idle, held by Dummy Element 0.
";

        #endregion
    }
}

#endif