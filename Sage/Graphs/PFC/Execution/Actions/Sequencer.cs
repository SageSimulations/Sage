/* This source code licensed under the GNU Affero General Public License */
using System;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Utility;

namespace Highpoint.Sage.Graphs.PFC.Execution.Actions {

    /// <summary>
    /// When a sequencer's Precondition is set as the precondition of a pfcStep, it will watch the PFCExecutionContext
    /// at it's level minus the rootHeight, and will not grant the step permission to start running until another
    /// the Sequencer with the same sequencer key, and one-less index has already granted its step permission to run.
    /// After the predecessor's permission is granted, the sequencer puts a key tailored for its successor into an
    /// exchange on which the successor is already waiting. Only when the successor receives that key will it grant
    /// itself permission to run.
    /// </summary>
    public class Sequencer {

        private Guid m_sequenceKey;
        private Guid m_myKey;
        private int m_myIndex;
        private int m_rootHeight;
        private IDetachableEventController m_idec = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="Sequencer"/> class.
        /// </summary>
        /// <param name="sequencerKey">The sequencer key.</param>
        /// <param name="myIndex">The creator's index.</param>
        /// <param name="rootHeight">Height of the root.</param>
        public Sequencer(Guid sequencerKey, int myIndex, int rootHeight){
            m_sequenceKey = sequencerKey;
            m_myIndex = myIndex;
            m_myKey = GuidOps.Add(sequencerKey, myIndex);
            m_rootHeight = rootHeight;
        }

        public Sequencer(Guid sequencerKey, int myIndex)
            : this(sequencerKey, myIndex, 0) {}

        public PfcAction Precondition {
            get {
                return new PfcAction(GetPermissionToStart);
            }
        }

        protected void GetPermissionToStart(PfcExecutionContext myPfcec, StepStateMachine ssm) {

            PfcExecutionContext root = myPfcec;
            int ascents = m_rootHeight;
            while (ascents > 0) {
                root = (PfcExecutionContext)myPfcec.Parent.Payload;
                ascents--;
            }
            
            Exchange exchange = null;
            if (m_myIndex == 0) {
                //Console.WriteLine(myPfcec.Name + " is creating an exchange and injecting it into pfcec " + root.Name + " under key " + m_sequenceKey);
                exchange = new Exchange(myPfcec.Model.Executive);
                root.Add(m_sequenceKey, exchange);
            } else {
                //Console.WriteLine(myPfcec.Name + " is looking for an exchange in pfcec " + root.Name + " under key " + m_sequenceKey);
                DictionaryChange dc = new DictionaryChange(myPfcec_EntryAdded);
                while (true) {
                    exchange = (Exchange)root[m_sequenceKey];
                    if (exchange == null) {
                        root.EntryAdded += dc;
                        m_idec = myPfcec.Model.Executive.CurrentEventController;
                        m_idec.Suspend();
                    } else {
                        root.EntryAdded -= dc;
                        break;
                    }
                }
                exchange.Take(m_myKey, true); // Only indices 1,2, ... take (and wait?). Index 0 only posts.
                //Console.WriteLine(myPfcec.Name + " got the key I was looking for!");
            }
            Guid nextGuysKey = GuidOps.Increment(m_myKey);
            exchange.Post(nextGuysKey, nextGuysKey, false);
            //Console.WriteLine(myPfcec.Name + " posted the key the next guy is looking for!");
        }

        void myPfcec_EntryAdded(object key, object value) {
            if (key.Equals(m_sequenceKey)) {
                m_idec.Resume();
            }
        }
    }
}
