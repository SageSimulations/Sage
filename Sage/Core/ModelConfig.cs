/* This source code licensed under the GNU Affero General Public License */

using System.Diagnostics;
using Trace = System.Diagnostics.Debug;
using System.Collections.Specialized;


namespace Highpoint.Sage.SimCore {

    /// <summary>
    /// Class ModelConfig is a collection of initialization parameters that is intended to be available to the model in the app.config file.
    /// </summary>
    public class ModelConfig {
		NameValueCollection m_nvc;
        public ModelConfig() : this("Sage") { }

        public ModelConfig(string sectionName) {
            m_nvc = (NameValueCollection)System.Configuration.ConfigurationManager.GetSection(sectionName);
            if (m_nvc == null) {
                // TODO: Add this to an Errors & Warnings collection instead of dumping it to Trace.
                Trace.WriteLine(string.Format("Warning - <{0}> section missing from config file for {1}.", sectionName, Process.GetCurrentProcess().ProcessName));
            }
        }

        public string GetSimpleParameter(string key) {
			string retval = null;
			if ( m_nvc != null ) retval = m_nvc[key];
			if ( retval == null ) {
				// TODO: Add this to an Errors & Warnings collection instead of dumping it to Trace.
				Trace.WriteLine("Application requested unfound parameter associated with key " + key + " in the app.config file.");
			}
			return retval;
		}
	}
}