/* This source code licensed under the GNU Affero General Public License */
#region COMMENTED OUT WHILE WORKFLOW PARAMETERS ARE COMMENTED OUT.
#if NOT_DEFINED

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Highpoint.Sage.Utility;
using Highpoint.Sage.Workflow;
using System.IO;
using System.Text;
using Highpoint.Sage.ItemBased;
using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.SimCore;
using System.Reflection;

namespace Highpoint.Sage.Workflow {

    /// <summary>
    /// Summary description for zTestWorkflowTokens.
    /// </summary>
    [TestClass]
    public class zTestWorkflowBlockInteractions {

        #region MSTest Goo
        [TestInitialize]
        public void Init() {
        }
        [TestCleanup]
        public void destroy() {
            Debug.WriteLine("Done.");
        }
        #endregion

        private static StringBuilder m_sb = null;
        private static StringWriter m_sw = null;
        private static int m_deNum = 0;

        public zTestWorkflowBlockInteractions() { }

        [TestMethod]
        public void TestTokenBlockInteractions_Basic() {

            MyWorkflow mw = new MyWorkflow(null,"Workflow#1",Guid.NewGuid());


            MyWorkflowCase wfCase = new MyWorkflowCase("Case 1");
            wfCase.RootToken.TokenStateChangingTo += new Token.TokenStateChange(RootToken_TokenStateChangingTo);
            wfCase.CaseCompleted += new WorkflowCaseEvent(wfCase_CaseCompleted);

            mw.AcceptCase(wfCase);

        }

        [TestMethod]
        public void TestTokenBlockInteractions_Ext1() {
            Model model = new Model("TestTokenBlockInteractions_Ext1", Guid.NewGuid());
            MyWorkflow mw = new MyWorkflow(model, "Workflow#2", Guid.NewGuid());

            MyWorkflowCase wfCase = new MyWorkflowCase("Case 1");
            wfCase.RootToken.TokenStateChangingTo += new Token.TokenStateChange(RootToken_TokenStateChangingTo);
            wfCase.CaseCompleted += new WorkflowCaseEvent(wfCase_CaseCompleted);

            model.Starting += new ModelEvent(
                delegate(IModel m) {
                    m.Executive.RequestEvent(new ExecEventReceiver(delegate(IExecutive exec, object data) { mw.AcceptCase(wfCase); }), m.Executive.Now, 0.0, null);
                });
            model.Start();

        }

        [TestMethod]
        public void TestParameters() {
            Parameters parameters = new Parameters(GroupingCriterion.Parentage, ProcessingCadence.FirstToken, ProcessingTiming.UponReset,
                                                 BlockTargets.ArrivalPort, BlockScope.ThisTokenGroup, BlockDuration.Reset, TransitionTo.Complete, 
                                                 TransitionWhen.UponReceipt,ResetCadence.OnLastSiblingNotRunning);
            parameters.Normalize();
            string s = parameters.AbbrString;
        }

        [TestMethod]
        public void TestRangeOfParameters() {
            ConstructorInfo[] cia = typeof(Parameters).GetConstructors();
            Debug.Assert(cia.Length == 1);
            ParameterInfo[] pia = cia[0].GetParameters();
            Stack<Type> types = new Stack<Type>();
            foreach (ParameterInfo pi in pia) {
                types.Push(pi.ParameterType);
            }

            IEnumerator<List<object>> argsets = Permute(types,null);
            while (argsets.MoveNext()) {
                Console.WriteLine(argsets.Current);
            }

            //Type[] enumTypes = new Type[] { typeof(GroupingCriterion), typeof(ProcessingCadence), typeof(ProcessingTiming), typeof(BlockTargets), typeof(BlockScope), typeof(BlockDuration, TransitionTo, TransitionWhen, ResetCadence };
            Parameters parameters = new Parameters(GroupingCriterion.Parentage, ProcessingCadence.FirstToken, ProcessingTiming.UponReset,
                                                 BlockTargets.ArrivalPort, BlockScope.ThisTokenGroup, BlockDuration.Reset, TransitionTo.Complete, 
                                                 TransitionWhen.UponReceipt,ResetCadence.OnLastSiblingNotRunning);
            parameters.Normalize();
            string s = parameters.AbbrString;
        }

        private IEnumerator<List<object>> Permute(Stack<Type> enums, List<object> working) {
            if (working == null) {
                working = new List<object>();
            }
            if (enums.Count > 0) {
                Type _enum = enums.Pop();
                foreach (object ob in Enum.GetValues(_enum)) {
                    working.Add(ob);
                    Permute(enums, working);
                    working.Remove(ob);
                }
                enums.Push(_enum);
            } else {
                yield return working;
            }
        }

        void model_Starting(IModel theModel) {
            throw new Exception("The method or operation is not implemented.");
        }

        void RootToken_TokenStateChangingTo(Token token, Token.TokenState state) {
            Console.WriteLine(token.State.ToString() + "->" + state.ToString());
        }

        void wfCase_CaseCompleted(IWorkflowCase wfCase, IWorkflow workflow) {
            Console.WriteLine("Case completed.");
        }

        abstract class Workflow : IWorkflow {

            #region IWorkflow Members

            public abstract bool AcceptCase(IWorkflowCase wfCase);

            public abstract List<IWorkflowElement> RootElements {get;}

            #endregion

        }
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        class MyWorkflow : Workflow {

            List<IWorkflowElement> m_RootElements = new List<IWorkflowElement>();
            IWorkflowElement m_rootBlock = null;

            public MyWorkflow( IModel model, string name, Guid guid) {

                Parameters parameters = new Parameters(GroupingCriterion.Parentage, ProcessingCadence.FirstToken, ProcessingTiming.UponReset,
                     BlockTargets.ArrivalPort, BlockScope.ThisTokenGroup, BlockDuration.Reset, TransitionTo.Complete, TransitionWhen.UponReceipt,
                     ResetCadence.OnLastSiblingNotRunning);

                //m_rootBlock = (IWorkflowElement)wfeType.GetConstructor(new Type[] { }).Invoke(new object[] { });
                m_rootBlock = new FilterBlock(model,name+".Root",Guid.NewGuid(), 3, parameters);
                
                m_RootElements.Add(m_rootBlock);
                m_rootBlock.Ports["PrimaryOutput"].PortDataPresented += new PortDataEvent(MyWorkflow_PortDataPresented);
            }

            void MyWorkflow_PortDataPresented(object data, IPort where) {
                Token token = (Token)data;
                token.Complete();
            }

            public override bool AcceptCase(IWorkflowCase wfCase) {

                wfCase.RootToken.Enter(m_RootElements[0],null);
                wfCase.RootToken.Start();

                List<Token> tokens = new List<Token>();
                for (int i = 0 ; i < 3 ; i++ ) {
                    tokens.Add(wfCase.RootToken.Spawn());
                }

                for (int i = 0 ; i < 3 ; i++) {
                    ( (IInputPort)m_rootBlock.Ports.Inputs[i] ).Put(tokens[i]);
                }

                //return ( (IInputPort)m_rootBlock.Ports.Inputs[0] ).Put(tokens[0]);
                return true;
            }

            public override List<IWorkflowElement> RootElements {
                get { return m_RootElements; }
            }

        }
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        class MyWorkflowElement : IWorkflowElement {

            private PortSet m_portSet = null;

            public MyWorkflowElement() {
                m_portSet = new PortSet(true);
                m_portSet.AddPort(new SimpleInputPort(null, "PrimaryInput", Guid.NewGuid(), this, new DataArrivalHandler(TokenArrived)));
                m_portSet.AddPort(new SimpleOutputPort(null, "PrimaryOutput", Guid.NewGuid(), this, new DataProvisionHandler(TakeHandler), new DataProvisionHandler(PeekHandler)));

            }

            private bool TokenArrived(object obj, IInputPort onPort) {
                Token token = (Token)obj;
                Console.WriteLine("Recieved case " + token.WorkflowCase.Name + ", " + token.Name + "[" + token.State + "] on port " + onPort.Name + ".");
                token.Enter(this, onPort);
                token.Start();
                Start(token);
                token.Stop();
                token.Exit();

                ( (SimpleOutputPort)m_portSet["PrimaryOutput"] ).OwnerPut(token);

                return true;
            }

            private void Start(Token token) {
                Console.WriteLine("Performing work of this element with " + token.WorkflowCase.Name + ".");
            }

            // Not supporting pull.
            private object TakeHandler(IOutputPort whichPortPulled, object selector) { return null; }
            private object PeekHandler(IOutputPort whichPortPulled, object selector) { return null; }

            #region IPortOwner Members

            public void AddPort(IPort port) {
                m_portSet.AddPort(port);
            }

            public IPort AddPort(string channelTypeName) {
                throw new Exception("The method or operation is not implemented.");
            }

            public IPort AddPort(string channelTypeName, Guid guid) {
                throw new Exception("The method or operation is not implemented.");
            }

            public List<IPortChannelInfo> SupportedChannelInfo {
                get { return GeneralPortChannelInfo.StdInputAndOutput; }
            }

            public void RemovePort(IPort port) {
                m_portSet.RemovePort(port);
            }

            public void ClearPorts() {
                m_portSet.ClearPorts();
            }

            public IPortSet Ports {
                get { return m_portSet; }
            }

            #endregion

            #region IModelObject Members

            public IModel Model {
                get { throw new Exception("The method or operation is not implemented."); }
            }

            public void InitializeIdentity(IModel model, string name, string description, Guid guid) {
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
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        class MyWorkflowCase : IWorkflowCase {

            private Token m_rootToken = null;
            private IWorkflow m_workflow = null;

            public MyWorkflowCase(string name) {
                m_name = name;
                m_rootToken = new Token(this);
                m_rootToken.TokenStateChangedFrom += new Token.TokenStateChange(m_rootToken_TokenStateChangedFrom);
            }

            void m_rootToken_TokenStateChangedFrom(Token token, Token.TokenState state) {
                if (token.State.Equals(Token.TokenState.Completed) || token.State.Equals(Token.TokenState.Completed)) {
                    if (CaseCompleted != null) {
                        CaseCompleted(this, m_workflow);
                    }
                }
            }

            #region IWorkflowCase Members

            public event WorkflowCaseEvent CaseCompleted;

            public Token RootToken {
                [DebuggerStepThrough]
                get { return m_rootToken; }
            }

            #endregion

            #region Implementation of IModelObject
            private string m_name = null;
            private Guid m_guid = Guid.Empty;
            private IModel m_model = null;
            private string m_description = null;

            /// <summary>
            /// The IModel to which this object belongs.
            /// </summary>
            /// <value>The object's Model.</value>
            public IModel Model { [System.Diagnostics.DebuggerStepThrough] get { return m_model; } }

            /// <summary>
            /// The name by which this object is known. Typically not required to be unique in a pan-model context.
            /// </summary>
            /// <value>The object's name.</value>
            public string Name { [System.Diagnostics.DebuggerStepThrough]get { return m_name; } }

            /// <summary>
            /// The description for this object. Typically used for human-readable representations.
            /// </summary>
            /// <value>The object's description.</value>
            public string Description { [System.Diagnostics.DebuggerStepThrough] get { return ( ( m_description == null ) ? ( "No description for " + m_name ) : m_description ); } }

            /// <summary>
            /// The Guid for this object. Typically required to be unique in a pan-model context.
            /// </summary>
            /// <value>The object's Guid.</value>
            public Guid Guid { [System.Diagnostics.DebuggerStepThrough] get { return m_guid; } }

            /// <summary>
            /// Initializes the fields that feed the properties of this IModelObject identity.
            /// </summary>
            /// <param name="model">The IModelObject's new model value.</param>
            /// <param name="name">The IModelObject's new name value.</param>
            /// <param name="description">The IModelObject's new description value.</param>
            /// <param name="guid">The IModelObject's new GUID value.</param>
            public void InitializeIdentity(IModel model, string name, string description, Guid guid) {
                IMOHelper.Initialize(ref m_model, model, ref m_name, name, ref m_description, description, ref m_guid, guid);
            }
            #endregion
        }

        //public delegate IWorkflowElement ElementDiscriminator(IWorkflowCase wfCase);
        //public delegate IInputPort InputPortDiscriminator(IWorkflowCase wfCase);
        //public delegate IOutputPort OutputPortDiscriminator(IWorkflowCase wfCase);

    }
}
#endif
#endregion