/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Graphs.PFC {
    public class PfcUnitInfo : IPfcUnitInfo {

        #region Private Fields
        private string m_name;
        private int m_sequenceNumber;
        #endregion 

        #region Constructors
        public PfcUnitInfo(string name, int sequenceNumber) {
            m_name = name;
            m_sequenceNumber = sequenceNumber;
        }
        #endregion 

        #region IPfcUnit Members

        public string Name {
            get {
                return m_name;
            }
            set {
                m_name = value;
            }
        }

        public int SequenceNumber {
            get {
                return m_sequenceNumber;
            }
            set {
                m_sequenceNumber = value;
            }
        }

        #endregion
    }
}
