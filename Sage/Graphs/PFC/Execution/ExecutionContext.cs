/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;
using System.Text;
using Highpoint.Sage.Utility;
using System.Collections;
using Highpoint.Sage.Scheduling;
using System.Diagnostics;

namespace Highpoint.Sage.Graphs.PFC.Execution {

    public interface IFacility { } // Don't know what this is, yet, but it provides access to resources and other running models & ec's.

    /// <summary>
    /// Class PfcExecutionContext holds all of the information necessary to track one execution through a PFC. The PFC governs structure,
    /// the PfcExecutionContext governs process-instance-specific data.
    /// </summary>
    /// <seealso cref="Highpoint.Sage.Utility.ExecutionContext" />
    /// <seealso cref="Highpoint.Sage.Scheduling.ISupportsCorrelation" />
    public class PfcExecutionContext : ExecutionContext, ISupportsCorrelation {

        #region Private Fields
        private int m_instanceCount = 0;
        private IProcedureFunctionChart m_pfc;
        private IPfcStepNode m_step;
        private ITimePeriod m_timePeriod;
        private static readonly Guid s_time_Period_Mask = new Guid("8aeaf15a-f138-4739-b815-6db516107103");
        private static readonly bool s_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("PfcExecutionContext");
        #endregion Private Fields

        #region Constructors
        public PfcExecutionContext(IProcedureFunctionChart pfc, string name, string description, Guid guid, PfcExecutionContext parent)
            : base(pfc.Model, name, description, guid, parent) {

            if (s_diagnostics) {
                string parentName = ( parent == null ? "<null>" : parent.Name );
                Console.WriteLine("Creating PFCEC \"" + name + "\" under PFCEC \"" + parentName + "\" For parent " + pfc.Name + " and numbered " + guid);
            }

            m_pfc = pfc;
            m_step = null;
            m_timePeriod = new TimePeriodEnvelope(name, GuidOps.XOR(guid, s_time_Period_Mask));
            m_timePeriod.Subject = this;
            if (parent != null) {
                ( (TimePeriodEnvelope)parent.TimePeriod ).AddTimePeriod(m_timePeriod);
            }
            m_timePeriod.ChangeEvent += new ObservableChangeHandler(m_timePeriod_ChangeEvent);
        }

        public PfcExecutionContext(IPfcStepNode stepNode, string name, string description, Guid guid, PfcExecutionContext parent)
            : base(stepNode.Parent.Model, name, description, guid, parent) {

            if (s_diagnostics) {
                Console.WriteLine("Creating PfcEC \"" + name + "\" under PfcEC \"" + parent.Name + "\" For parent " + stepNode.Name + " and numbered " + guid);
            }

            m_pfc = stepNode.Parent;
            m_step = stepNode;
            if (stepNode.Actions.Count == 0) {
                m_timePeriod = new TimePeriod(name, GuidOps.XOR(guid, s_time_Period_Mask), TimeAdjustmentMode.InferDuration);
                m_timePeriod.Subject = this;
                ( (TimePeriodEnvelope)parent.TimePeriod ).AddTimePeriod(m_timePeriod);
            } else {
                m_timePeriod = new TimePeriodEnvelope(name, GuidOps.XOR(guid, s_time_Period_Mask));
                m_timePeriod.Subject = this;
                ( (TimePeriodEnvelope)parent.TimePeriod ).AddTimePeriod(m_timePeriod);
            }
            m_timePeriod.ChangeEvent += new ObservableChangeHandler(m_timePeriod_ChangeEvent);
        }
        #endregion Constructors

        private void m_timePeriod_ChangeEvent(object whoChanged, object whatChanged, object howChanged) {
            if (TimePeriodChange != null)
                TimePeriodChange((ITimePeriod)whoChanged, (TimePeriod.ChangeType)whatChanged);
        }

        public event TimePeriodChange TimePeriodChange;

        public IProcedureFunctionChart PFC { [DebuggerStepThrough] get { return m_pfc; } }
        public IPfcStepNode Step { [DebuggerStepThrough] get { return m_step; } }
        public bool IsStepCentric { [DebuggerStepThrough] get { return m_step != null; } }
        public ITimePeriod TimePeriod { [DebuggerStepThrough] get { return m_timePeriod; } }

        public IEnumerable<IPfcStepNode> ChildSteps {
            get {
                foreach (object obj in Values) {
                    StepStateMachine ssm = obj as StepStateMachine;
                    if (ssm != null) {
                        yield return ssm.MyStep;
                    }
                }
            }
        }

        public IEnumerable<StepStateMachine> ChildStepStateMachines {
            get {
                foreach (object obj in Keys) {
                    StepStateMachine ssm = obj as StepStateMachine;
                    if (ssm != null) {
                        yield return ssm;
                    }
                }
            }
        }

#region To Various String Representations
        public string ToXmlString(bool deep) {
            StringBuilder sb = new StringBuilder();
            _toXmlString(sb, deep);
            return sb.ToString();
        }

        public override string ToString() {
            return ( IsStepCentric ? "Step" : "PFC" ) + " Exec Ctx : " + Name;
        }

        private void _toXmlString(StringBuilder sb, bool deep) {
            sb.Append(@"<PfcExecutionContext pfcName=""" + m_pfc.Name + @""">");
            foreach (DictionaryEntry de in this) {
                sb.Append("<Entry><Key>" + de.Key + "</Key><Value>" + de.Value + "</Value></Entry>");
            }
            if (deep) {
                sb.Append("<Children>");
                foreach (PfcExecutionContext child in Children) {
                    child._toXmlString(sb, deep);
                }
                sb.Append("</Children>");
            }
            sb.Append("</PfcExecutionContext>");
        }
#endregion To Various String Representations

#region ISupportsCorrelation Members

        /// <summary>
        /// Gets the parent of this tree node for the purpose of correlation. This will be its nominal parent, too.
        /// </summary>
        /// <value>The parent.</value>
        public Guid ParentGuid {
            get { return Parent.Payload.Guid; }
        }

        public int InstanceCount {
            get {
                return m_instanceCount;
            }
            set {
                m_instanceCount = value;
            }
        }
#endregion

    }
}
