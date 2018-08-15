/* This source code licensed under the GNU Affero General Public License */

using System;
using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.ItemBased.Connectors;
using Highpoint.Sage.SimCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Diagnostics;

namespace Highpoint.Sage.ItemBased.Blocks {


    [TestClass]
    public class PortsAndConnectorsTester {
        [TestMethod] public void TestPortBasics(){
            new PortTester().TestSimplePortNetwork();
        }

    }

    [TestClass]
    public class PortTester {
        Model m_model;
        int m_nBlocks = 10;
        SimplePassThroughPortOwner[] m_blocks;
        //SimplePassThroughPortOwner m_finish;
        public PortTester(){
            m_model = new Model();
            m_blocks = new SimplePassThroughPortOwner[m_nBlocks];
        }

        [TestMethod]
        public void TestSimplePortNetwork() {

            for (int i = 0 ; i < m_nBlocks ; i++) {
                m_blocks[i] = new SimplePassThroughPortOwner(m_model, "Block" + i, Guid.NewGuid());
                if (i > 0)
                    ConnectorFactory.Connect(m_blocks[i - 1].Out, m_blocks[i].In);
            }

            m_blocks[0].In.Put("Random string");
            Debug.WriteLine(m_blocks[m_nBlocks - 1].Out.Peek(null));


            Debug.WriteLine(m_blocks[m_nBlocks - 1].Out.Take(null));
        }

        [TestMethod]
        public void TestProxyPorts() {

            SimpleProxyPortOwner sppo = new SimpleProxyPortOwner(m_model, "Proxy", Guid.NewGuid());
            sppo.In.Put("Random string");
            Debug.WriteLine(sppo.Out.Peek(null));

            Debug.WriteLine(sppo.Out.Take(null));
        }
    }

    [TestClass]
    public class ManagementFacadeTester {

        public void TestManagementBasics() {

            //new ManagementFacadeTester().DoAbbreviatedFacadeTest();
            //new ManagementFacadeTester().DoAbbreviatedSimplePushTest();
            //new ManagementFacadeTester().DoAbbreviatedSimplePullTest();
            //new ManagementFacadeTester().DoAbbreviatedInputSideBufferedPullTest();
            //new ManagementFacadeTester().DoAbbreviatedOutputSideBufferedPullTest();
            //new ManagementFacadeTester().DoAbbreviatedSimplePushPullTestWithBuffering();
            //new ManagementFacadeTester().DoAbbreviatedSimplePushPullTestNoOutputBuffering();
            //new ManagementFacadeTester().OneActiveOnePassiveInputDeterminesOneBufferedPassiveOutputTest();
            //new ManagementFacadeTester().DoAbbreviatedForcedPullTest();
            new ManagementFacadeTester().TestOneInOneOutPushPullTransform();
            //new ManagementFacadeTester().TestOnePullValue();
        }

        private IInputPort in0, in1;
        private IOutputPort out0, out1;
        private SimpleOutputPort entryPoint0, entryPoint1;
        private InputPortManager facadeIn0, facadeIn1;
        private OutputPortManager facadeOut0, facadeOut1;

        [TestMethod]
        public void DoAbbreviatedFacadeTest() {

            PortManagementFacade pmf = Setup(out in0, out in1, out out0, out out1, out facadeIn0, out facadeIn1, out facadeOut0, out facadeOut1, out entryPoint0, out entryPoint1);

            facadeOut0.ComputeFunction = new Action(() => { facadeOut0.Buffer = facadeIn0.Value.ToString() + " " + facadeIn1.Value.ToString(); });
            facadeOut1.ComputeFunction = new Action(() => { facadeOut1.Buffer = facadeIn1.Value.ToString() + " " + facadeIn0.Value.ToString(); });

            Console.WriteLine(out0.Take(null) + " taken.");
            Console.WriteLine(out0.Take(null) + " taken.");
            Console.WriteLine(out1.Take(null) + " taken.");
            Console.WriteLine(out1.Take(null) + " taken.");

        }

        [TestMethod]
        public void DoAbbreviatedSimplePushTest() {

            PortManagementFacade pmf = Setup(out in0, out in1, out out0, out out1, out facadeIn0, out facadeIn1, out facadeOut0, out facadeOut1, out entryPoint0, out entryPoint1);

            facadeIn0.WriteAction = InputPortManager.DataWriteAction.Push;
            facadeIn0.ReadSource = InputPortManager.DataReadSource.BufferOrPull;

            facadeOut0.ComputeFunction = new Action(() => { facadeOut0.Buffer = facadeIn0.Value; });
            facadeIn0.SetDependents(facadeOut0);

            entryPoint0.OwnerPut("Data0");
            entryPoint0.OwnerPut("Data1");
            //Console.WriteLine(out0.Take(null) + " taken.");
            //Console.WriteLine(out0.Take(null) + " taken.");
            //Console.WriteLine(out1.Take(null) + " taken.");
            //Console.WriteLine(out1.Take(null) + " taken.");

        }

        [TestMethod]
        public void DoAbbreviatedSimplePullTest() {

            PortManagementFacade pmf = Setup(out in0, out in1, out out0, out out1, out facadeIn0, out facadeIn1, out facadeOut0, out facadeOut1, out entryPoint0, out entryPoint1);

            facadeIn0.WriteAction = InputPortManager.DataWriteAction.StoreAndInvalidate;
            facadeIn0.ReadSource = InputPortManager.DataReadSource.BufferOrPull;
            facadeIn0.DataBufferPersistence = PortManager.BufferPersistence.UntilRead;
            facadeIn0.SetDependents(facadeOut0);

            facadeOut0.ComputeFunction = new Action(() => { facadeOut0.Buffer = facadeIn0.Value; });
            facadeOut0.DataBufferPersistence = PortManager.BufferPersistence.UntilRead;

            Console.WriteLine(out0.Take(null) + " taken.");
            Console.WriteLine(out0.Take(null) + " taken.");

        }

        [TestMethod]
        public void DoAbbreviatedInputSideBufferedPullTest() {
            // Put a value into the input. Read the output several times. Replace the input, read the output twice more.

            PortManagementFacade pmf = Setup(out in0, out in1, out out0, out out1, out facadeIn0, out facadeIn1, out facadeOut0, out facadeOut1, out entryPoint0, out entryPoint1);

            facadeIn0.WriteAction = InputPortManager.DataWriteAction.StoreAndInvalidate;
            facadeIn0.ReadSource = InputPortManager.DataReadSource.BufferOrPull;
            facadeIn0.DataBufferPersistence = PortManager.BufferPersistence.UntilWrite;
            facadeIn0.SetDependents(facadeOut0);

            facadeOut0.ComputeFunction = new Action(() => { facadeOut0.Buffer = facadeIn0.Value; });
            facadeOut0.DataBufferPersistence = PortManager.BufferPersistence.UntilRead;

            entryPoint0.OwnerPut("PushData0");
            Console.WriteLine(out0.Take(null) + " taken.");
            Console.WriteLine(out0.Take(null) + " taken.");
            entryPoint0.OwnerPut("PushData1");
            Console.WriteLine(out0.Take(null) + " taken.");
            //Console.WriteLine(out1.Take(null) + " taken.");
            //Console.WriteLine(out1.Take(null) + " taken.");

        }

        [TestMethod]
        public void DoAbbreviatedOutputSideBufferedPullTest() {
            // Put a value into the input. Read the output several times. Replace the input, read the output twice more.

            PortManagementFacade pmf = Setup(out in0, out in1, out out0, out out1, out facadeIn0, out facadeIn1, out facadeOut0, out facadeOut1, out entryPoint0, out entryPoint1);

            facadeIn0.WriteAction = InputPortManager.DataWriteAction.StoreAndInvalidate;
            facadeIn0.ReadSource = InputPortManager.DataReadSource.Buffer;
            facadeIn0.DataBufferPersistence = PortManager.BufferPersistence.None;
            facadeIn0.SetDependents(facadeOut0);

            facadeOut0.ComputeFunction = new Action(() => { facadeOut0.Buffer = facadeIn0.Value; });
            facadeOut0.DataBufferPersistence = PortManager.BufferPersistence.UntilWrite;

            entryPoint0.OwnerPut("PushData0");
            Console.WriteLine(out0.Take(null) + " taken.");
            Console.WriteLine(out0.Take(null) + " taken.");
            entryPoint0.OwnerPut("PushData1");
            Console.WriteLine(out0.Take(null) + " taken.");
            //Console.WriteLine(out1.Take(null) + " taken.");
            //Console.WriteLine(out1.Take(null) + " taken.");

        }

        [TestMethod]
        public void DoAbbreviatedSimplePushPullTestWithOutputBuffering() {

            PortManagementFacade pmf = Setup(out in0, out in1, out out0, out out1, out facadeIn0, out facadeIn1, out facadeOut0, out facadeOut1, out entryPoint0, out entryPoint1);

            facadeIn0.WriteAction = InputPortManager.DataWriteAction.Push;
            facadeIn0.ReadSource = InputPortManager.DataReadSource.Pull;
            facadeIn0.DataBufferPersistence = PortManager.BufferPersistence.None;
            facadeIn0.SetDependents(facadeOut0);

            facadeOut0.ComputeFunction = new Action(() => { facadeOut0.Buffer = facadeIn0.Value; });
            facadeOut0.DataBufferPersistence = PortManager.BufferPersistence.UntilWrite;

            entryPoint0.OwnerPut("PushData0");
            entryPoint0.OwnerPut("PushData1");
            Console.WriteLine(out0.Take(null) + " taken.");
            Console.WriteLine(out0.Take(null) + " taken.");
            //Console.WriteLine(out1.Take(null) + " taken.");
            //Console.WriteLine(out1.Take(null) + " taken.");

        }

        [TestMethod]
        public void DoAbbreviatedSimplePushPullTestNoOutputBuffering() {

            PortManagementFacade pmf = Setup(out in0, out in1, out out0, out out1, out facadeIn0, out facadeIn1, out facadeOut0, out facadeOut1, out entryPoint0, out entryPoint1);

            facadeIn0.WriteAction = InputPortManager.DataWriteAction.Push;
            facadeIn0.ReadSource = InputPortManager.DataReadSource.BufferOrPull;
            facadeIn0.DataBufferPersistence = PortManager.BufferPersistence.UntilWrite;
            facadeIn0.SetDependents(facadeOut0);

            facadeOut0.ComputeFunction = new Action(() => { facadeOut0.Buffer = facadeIn0.Value; });
            facadeOut0.DataBufferPersistence = PortManager.BufferPersistence.UntilWrite;

            entryPoint0.OwnerPut("PushData0");
            entryPoint0.OwnerPut("PushData1");
            Console.WriteLine(out0.Take(null) + " taken.");
            Console.WriteLine(out0.Take(null) + " taken.");
            entryPoint0.OwnerPut("PushData2");
            Console.WriteLine(out0.Take(null) + " taken.");
            Console.WriteLine(out0.Take(null) + " taken.");

        }

        [TestMethod]
        public void OneActiveOnePassiveInputDeterminesOneBufferedPassiveOutputTest() {

            PortManagementFacade pmf = Setup(out in0, out in1, out out0, out out1, out facadeIn0, out facadeIn1, out facadeOut0, out facadeOut1, out entryPoint0, out entryPoint1);

            // Active input. Writes to this port cause a value to be pushed out the output.
            facadeIn0.WriteAction = InputPortManager.DataWriteAction.StoreAndInvalidate;
            facadeIn0.ReadSource = InputPortManager.DataReadSource.BufferOrPull;
            facadeIn0.DataBufferPersistence = PortManager.BufferPersistence.UntilRead;
            facadeIn0.SetDependents(facadeOut0);

            // Passive input. We pull a value if none is present, and store it in a buffer that remains valid until overwritten.
            facadeIn1.WriteAction = InputPortManager.DataWriteAction.StoreAndInvalidate;
            facadeIn1.ReadSource = InputPortManager.DataReadSource.BufferOrPull;
            facadeIn1.DataBufferPersistence = PortManager.BufferPersistence.UntilWrite;
            facadeIn1.SetDependents(facadeOut0);

            // Pulls from the output cause the buffer value to be reused.
            facadeOut0.ComputeFunction = new Action(() => { facadeOut0.Buffer = ((string)facadeIn0.Value) + ((string)facadeIn1.Value); });
            facadeOut0.DataBufferPersistence = PortManager.BufferPersistence.UntilWrite;

            entryPoint0.OwnerPut("PushData0");
            Console.WriteLine("With data0 not yet provided, and data1 not yet provided, " + out0.Take(null) + " taken.");
            Console.WriteLine("With data0 provided, and data1 not not yet provided, " + out0.Take(null) + " taken.");
            entryPoint0.OwnerPut("PushData1");
            entryPoint0.OwnerPut("PushData2");
            entryPoint1.OwnerPut("EntryPoint1(2)");
            Console.WriteLine(out0.Take(null) + " taken.");

        }

        [TestMethod]
        public void TestOnePullValue() {
            OnePullValue opv = new OnePullValue(null, null, null, Guid.NewGuid());
            opv.ConstValue = 12.345;
            Console.WriteLine(opv.Ports.Outputs[0].Take(null));
            Console.WriteLine(opv.Ports.Outputs[0].Take(null));
            Console.WriteLine(opv.Ports.Outputs[0].Take(null));
        }

        [TestMethod]
        public void TestOneInOneOutPushPullTransform() {
            DLog dlog = new DLog(null, null, null, Guid.NewGuid());

            double i1 = 0.1;
            entryPoint1 = new SimpleOutputPort(null, "", Guid.NewGuid(), null, delegate(IOutputPort iop, object selector) { return i1++; }, null);
            ConnectorFactory.Connect(entryPoint1, dlog.Ports.Inputs[0]);

            dlog.Ports.Outputs[0].PortDataPresented += new PortDataEvent(delegate(object data, IPort where) { Console.WriteLine(string.Format("{0} presented at {1}.", data.ToString(), where.Name)); });


            Console.WriteLine(String.Format("Pushing {0}", i1)); entryPoint1.OwnerPut(i1++);
            Console.WriteLine(String.Format("Pushing {0}", i1)); entryPoint1.OwnerPut(i1++);


            Console.WriteLine(dlog.Ports.Outputs[0].Take(null));
            Console.WriteLine(dlog.Ports.Outputs[0].Take(null));
            Console.WriteLine(dlog.Ports.Outputs[0].Take(null));
        }

        private PortManagementFacade Setup(out IInputPort in0, out IInputPort in1, out IOutputPort out0, out IOutputPort out1, out InputPortManager facadeIn0, out InputPortManager facadeIn1, out OutputPortManager facadeOut0, out OutputPortManager facadeOut1, out SimpleOutputPort entryPoint0, out SimpleOutputPort entryPoint1) {
            ManagementFacadeBlock mfb = new ManagementFacadeBlock();
            out0 = new SimpleOutputPort(null, "Out0", Guid.NewGuid(), mfb, null, null);
            out1 = new SimpleOutputPort(null, "Out1", Guid.NewGuid(), mfb, null, null);
            in0 = new SimpleInputPort(null, "In0", Guid.NewGuid(), mfb, null);
            in1 = new SimpleInputPort(null, "In1", Guid.NewGuid(), mfb, null);

            int i0 = 0;
            entryPoint0 = new SimpleOutputPort(null, "", Guid.NewGuid(), null, delegate(IOutputPort iop, object selector) { return string.Format("Src0 ({0})", i0++); }, null);
            ConnectorFactory.Connect(entryPoint0, in0);

            int i1 = 0;
            entryPoint1 = new SimpleOutputPort(null, "", Guid.NewGuid(), null, delegate(IOutputPort iop, object selector) { return string.Format("Src1 ({0})", i1++); }, null);
            ConnectorFactory.Connect(entryPoint1, in1);

            out0.PortDataPresented += new PortDataEvent(delegate(object data, IPort where) { Console.WriteLine(string.Format("{0} presented at {1}.", data.ToString(), where.Name)); });
            out1.PortDataPresented += new PortDataEvent(delegate(object data, IPort where) { Console.WriteLine(string.Format("{0} presented at {1}.", data.ToString(), where.Name)); });

            PortManagementFacade pmf = new PortManagementFacade(mfb);

            facadeIn0 = pmf.ManagerFor(in0);
            facadeIn1 = pmf.ManagerFor(in1);
            facadeOut0 = pmf.ManagerFor(out0);
            facadeOut1 = pmf.ManagerFor(out1);

            return pmf;
        }
    }

    internal class OnePullValue : ManagementFacadeBlock {

        private SimpleOutputPort m_output = null;
        private OutputPortManager m_opm = null;

        public OnePullValue(IModel model, string name, string description, Guid guid) {

            m_output = new SimpleOutputPort(null, "Output", Guid.NewGuid(), this, null, null);

            PortManagementFacade pmf = new PortManagementFacade(this);

            m_opm = pmf.ManagerFor(m_output);

            m_opm.DataBufferPersistence = PortManager.BufferPersistence.UntilWrite; // Output value is reusable. We never use the compute function.

        }

        public double ConstValue {
            get {
                return (double)m_opm.Buffer;
            }
            set {
                m_opm.Buffer = value;
            }
        }
    }

    /// <summary>
    /// This class assumes that each time a write is done to the input port, computation and a push to the output port
    /// is expected, and each time a pull is done from the output port, a pull from the input port and recomputation of
    /// the value to be presented on the output port is expected.
    /// </summary>
    internal abstract class OneInOneOutPushPullTransform : ManagementFacadeBlock
    {
        private SimpleInputPort m_input = null;
        private SimpleOutputPort m_output = null;

        private InputPortManager m_ipm = null;
        private OutputPortManager m_opm = null;

        public OneInOneOutPushPullTransform(IModel model, string name, string description, Guid guid) {

            m_output = new SimpleOutputPort(null, "Output", Guid.NewGuid(), this, null, null);
            m_input = new SimpleInputPort(null, "Input", Guid.NewGuid(), this, null);

            m_input.PortDataPresented += new PortDataEvent(delegate(object data, IPort where) { Console.WriteLine(data.ToString() + " presented to " + where.Name ); });

            PortManagementFacade pmf = new PortManagementFacade(this);

            m_ipm = pmf.ManagerFor(m_input);
            m_opm = pmf.ManagerFor(m_output);

            m_ipm.WriteAction = InputPortManager.DataWriteAction.Push; // When a value is written into the input buffer, we push the resultant transform out the output port.
            m_ipm.DataBufferPersistence = InputPortManager.BufferPersistence.None; // The input buffer is re-read with every pull.
            m_ipm.ReadSource = InputPortManager.DataReadSource.Pull; // We'll always pull a new value.
            m_ipm.SetDependents(m_opm); // A new value written to ipm impacts opm.

            m_opm.ComputeFunction = new Action(ComputeFuction);
            m_opm.DataBufferPersistence = PortManager.BufferPersistence.None; // Output value is always recomputed.

        }

        public object Input { get { return m_ipm.Value; } }
        public object Output { set { m_opm.Buffer = value; } }

        protected abstract void ComputeFuction();
    }

    class DLog : OneInOneOutPushPullTransform {

        public DLog(IModel model, string name, string description, Guid guid) : base(model, name, description, guid) { }

        protected override void ComputeFuction() {
            Output = Math.Log((double)Input, Math.E);
        }
    }

    class SimplePassThroughPortOwner : IPortOwner, IHasName {
        public IInputPort In { get { return m_in; } }
        private SimpleInputPort m_in;
        public IOutputPort Out { get { return m_out; } }
        private SimpleOutputPort m_out;
        public string Name { get { return m_name; } }
        private object m_buffer;
        private string m_name;

        public SimplePassThroughPortOwner(IModel model, string name, Guid guid){
            m_name = name;
            m_in = new SimpleInputPort(model, "In", Guid.NewGuid(), this, new DataArrivalHandler(OnDataArrived));
            m_out = new SimpleOutputPort(model, "Out", Guid.NewGuid(), this, new DataProvisionHandler(OnDataRequested), null);
        }

        private bool OnDataArrived(object data,IInputPort ip){
            Debug.Write(Name + " was just given data (" + data.ToString() + ")");
            if ( In.Peer != null ) {
                Debug.WriteLine(" by " + ( (IHasName)In.Peer.Owner ).Name);
            } else {
                Debug.WriteLine(" by some non-connected element.");
            }

            m_buffer = data;

            if ( m_out.Peer != null ) {
                return m_out.OwnerPut(data);
            } else {
                return false;
            }
        }

        private object OnDataRequested(IOutputPort op, object selector) {

            Debug.Write(Name + " was just asked for data by ");
            if ( Out.Peer != null ) {
                Debug.WriteLine(((IHasName)Out.Peer.Owner).Name);
            } else {
                Debug.WriteLine("some non-connected element.");
            }
            
            if ( m_in.Peer != null ) {
                Debug.WriteLine("I will ask " + ( (IHasName)In.Peer.Owner ).Name + "...");
                return m_in.OwnerTake(null);
            } else {
                string data = RandomString();
                Debug.WriteLine("Since I have no predecessor, I'll make up some data. How about " + data + "...");
                return data;
            }
        }
        
        private string m_letters = "abcdefghijklmnopqrstuvwxyz";
        private Random m_random = new Random();
		private string RandomString(){
			return RandomString(10);
		}
		private string RandomString(int nChars){
			System.Text.StringBuilder sb = new System.Text.StringBuilder(nChars);
			for ( int i = 0 ; i < nChars ; i++ ) sb.Append(m_letters[m_random.Next(m_letters.Length)]);
			return sb.ToString();
		}


        #region IPortOwner Implementation
        /// <summary>
        /// The PortSet object to which this IPortOwner delegates.
        /// </summary>
        private PortSet m_ports = new PortSet();
        /// <summary>
        /// Registers a port with this IPortOwner
        /// </summary>
        /// <param name="key">The key by which this IPortOwner will know this port.</param>
        /// <param name="port">The port that this IPortOwner will know by this key.</param>
        public void AddPort(IPort port) {m_ports.AddPort(port);}
        
        /// <summary>
        /// Adds a port to this object's port set in the specified role or channel.
        /// </summary>
        /// <param name="channel">The channel - usually "Input" or "Output", sometimes "Control", "Kanban", etc.</param>
        /// <returns>The newly-created port. Can return null if this is not supported.</returns>
        public IPort AddPort(string channel) { return null; /*Implement AddPort(string channel); */}

        /// <summary>
        /// Adds a port to this object's port set in the specified role or channel.
        /// </summary>
        /// <param name="channelTypeName">The channel - usually "Input" or "Output", sometimes "Control", "Kanban", etc.</param>
        /// <param name="guid">The GUID to be assigned to the new port.</param>
        /// <returns>The newly-created port. Can return null if this is not supported.</returns>
        public IPort AddPort(string channelTypeName, Guid guid) { return null; /*Implement AddPort(string channel); */}

        /// <summary>
        /// Gets the names of supported port channels.
        /// </summary>
        /// <value>The supported channels.</value>
        public List<Highpoint.Sage.ItemBased.Ports.IPortChannelInfo> SupportedChannelInfo { get { return GeneralPortChannelInfo.StdInputAndOutput; } }

        /// <summary>
        /// Unregisters a port from this IPortOwner.
        /// </summary>
        /// <param name="key">The key by which the port being unregistered is known.</param>
        public void RemovePort(IPort port){ m_ports.RemovePort(port); }

        /// <summary>
        /// Unregisters all ports that this IPortOwner knows to be its own.
        /// </summary>
        public void ClearPorts(){m_ports.ClearPorts();}

        /// <summary>
        /// The public property that is the PortSet this IPortOwner owns.
        /// </summary>
        public IPortSet Ports { get { return m_ports; } }
        #endregion
    }

    class SimpleProxyPortOwner : IPortOwner {
        private string m_name;
        private SimplePassThroughPortOwner m_sptpo;
        public IInputPort In { get { return m_in; } }
        private IInputPort m_in;
        public IOutputPort Out { get { return m_out; } }
        private IOutputPort m_out;

        public SimpleProxyPortOwner(IModel model, string name, Guid guid) {
            m_name = name;
            m_sptpo = new SimplePassThroughPortOwner(model, name + ".internal", Guid.NewGuid());

            m_in = new InputPortProxy(model, "In", null, Guid.NewGuid(), this, m_sptpo.In);
            m_out = new OutputPortProxy(model, "Out", null, Guid.NewGuid(), this, m_sptpo.Out);
        }

        public string Name { get { return m_name; } }


        #region IPortOwner Members

        public void AddPort(IPort port) {
            throw new Exception("The method or operation is not implemented.");
        }

        /// <summary>
        /// Adds a port to this object's port set in the specified role or channel.
        /// </summary>
        /// <param name="channel">The channel - usually "Input" or "Output", sometimes "Control", "Kanban", etc.</param>
        /// <returns>The newly-created port. Can return null if this is not supported.</returns>
        public IPort AddPort(string channel) { return null; /*Implement AddPort(string channel); */}

        /// <summary>
        /// Adds a port to this object's port set in the specified role or channel.
        /// </summary>
        /// <param name="channelTypeName">The channel - usually "Input" or "Output", sometimes "Control", "Kanban", etc.</param>
        /// <param name="guid">The GUID to be assigned to the new port.</param>
        /// <returns>The newly-created port. Can return null if this is not supported.</returns>
        public IPort AddPort(string channelTypeName, Guid guid) { return null; /*Implement AddPort(string channel); */}

        /// <summary>
        /// Gets the names of supported port channels.
        /// </summary>
        /// <value>The supported channels.</value>
        public List<Highpoint.Sage.ItemBased.Ports.IPortChannelInfo> SupportedChannelInfo { get { return GeneralPortChannelInfo.StdInputAndOutput; } }

        public void RemovePort(IPort port) {
            throw new Exception("The method or operation is not implemented.");
        }

        public void ClearPorts() {
            throw new Exception("The method or operation is not implemented.");
        }

        public IPortSet Ports {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        #endregion
    }

    class ManagementFacadeBlock : IPortOwner {

        PortSet m_ps = new PortSet();

        public void AddPort(IPort port) {
            m_ps.AddPort(port);
        }

        public IPort AddPort(string channelTypeName) {
            throw new NotImplementedException();
        }

        public IPort AddPort(string channelTypeName, Guid guid) {
            throw new NotImplementedException();
        }

        public List<IPortChannelInfo> SupportedChannelInfo {
            get { return GeneralPortChannelInfo.StdInputAndOutput; }
        }

        public void RemovePort(IPort port) {
            m_ps.RemovePort(port);
        }

        public void ClearPorts() {
            m_ps.ClearPorts();
        }

        public IPortSet Ports {
            get { return m_ps; }
        }
    }
}
