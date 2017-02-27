/* This source code licensed under the GNU Affero General Public License */

using Trace = System.Diagnostics.Debug;
using System.Text;
using System.Collections;
using Highpoint.Sage.ItemBased.Ports;

namespace Highpoint.Sage.ItemBased {

    class SimplePortActivityLogger {
        private StringBuilder m_contents = new StringBuilder();
        private ArrayList m_arrayList = new ArrayList();
        private IPort m_port;
        public SimplePortActivityLogger(IPort port){
            m_port = port;
            port.PortDataPresented+=new PortDataEvent(PortDataPresented);
            port.PortDataAccepted+=new PortDataEvent(PortDataAccepted);
            port.PortDataRejected+=new PortDataEvent(PortDataRejected);
        }

        public SimplePortActivityLogger(IPortSet portSet){
            foreach ( IPort port in portSet ) {
                port.PortDataPresented+=new PortDataEvent(PortDataPresented);
                port.PortDataAccepted+=new PortDataEvent(PortDataAccepted);
                port.PortDataRejected+=new PortDataEvent(PortDataRejected);
            }
        }

        void PortDataPresented(object data, IPort port){
            m_contents.Append("A port on " + port.Owner + " was presented with " + data + "\r\n");
        }

        void PortDataAccepted(object data, IPort port){
            m_contents.Append("A port on " + port.Owner + " accepted " + data + "\r\n");
        }

        void PortDataRejected(object data, IPort port){
            m_contents.Append("A port on " + port.Owner + " rejected " + data + "\r\n");
        }

        public string Contents { get { return m_contents.ToString(); } }
    }
}