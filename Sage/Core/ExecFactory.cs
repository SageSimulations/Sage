/* This source code licensed under the GNU Affero General Public License */
//Comment this out to time-bound the ability to obtain and run an executive.
#define TIME_BOUNDED

using System;
using System.CodeDom;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;
#if LICENSING_ENABLED
using Highpoint.Sage.Licensing;
#endif // LICENSING_ENABLED

using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.SimCore {

    public enum ExecType
    {
        /// <summary>
        /// The full featured executive is multi-threaded, and supports the full API.
        /// </summary>
        FullFeatured,
        /// <summary>
        /// The single threaded executive obtains highest performance through a limited feature set.
        /// </summary>
        SingleThreaded,
        /// <summary>
        /// This single threaded executive also offers rollback capability for parallel simulation.
        /// </summary>
        SingleThreadedWithRollback
    }

    /// <summary>
    /// ExecFactory produces instances of objects that implement IExecutive.
    /// </summary>
    public class ExecFactory {

#if LICENSING_ENABLED
        private static bool _licenseChecked = true;
#endif // LICENSING_ENABLED
        private static readonly object s_lock = new object();
        private static volatile ExecFactory _instance;

        private ExecFactory() {
#if LICENSING_ENABLED
#if TIME_BOUNDED
            DateTime expiry = new DateTime(2016, 12, 31);
#else
            DateTime expiry = DateTime.MaxValue;
#endif // TIME_BOUNDED

            if (!_licenseChecked || DateTime.Now > expiry ) {
                if (Debugger.IsAttached) {
                    if (!LicenseManager.Check()) {
                        MessageBox.Show("Sage® Simulation and Modeling Library license is invalid.", "Licensing Error");
                    }
                }

                try {
                    LicenseManager.ReportUsage( );
                } catch (WebException) {
                    Console.WriteLine( "Failure to report usage." );
                } catch (Exception) {
                    Console.WriteLine( "Failure to report usage." );
                }

                _licenseChecked = true;
            }

#if TIME_BOUNDED
            Console.WriteLine("Trial license expiration, {0}.", expiry);
#endif // TIME_BOUNDED
#endif // LICENSING_ENABLED


        }

        /// <summary>
        /// Provides a reference to the one ExecFactory in the current Application Context.
        /// </summary>
        public static ExecFactory Instance {
            get {

                lock (s_lock) { if (_instance == null) _instance = new ExecFactory(); }
                return _instance;
            }
        }

        /// <summary>
        /// Creates a copy of the default type of executive.
        /// </summary>
        /// <returns>An instance of the default executive type.</returns>
        public IExecutive CreateExecutive() {
            return CreateExecutive(Guid.NewGuid());
        }

        /// <summary>
        /// Creates an executive of the built-in type specified by the provided enumeration.
        /// </summary>
        /// <param name="execType">Type of the executive.</param>
        /// <returns>IExecutive.</returns>
        public IExecutive CreateExecutive(ExecType execType)
        {
            return CreateExecutive(execType, Guid.NewGuid());
        }

        /// <summary>
        /// Creates an executive of the built-in type specified by the provided enumeration.
        /// </summary>
        /// <param name="execType">Type of the execute.</param>
        /// <param name="guid">The unique identifier.</param>
        /// <returns>IExecutive.</returns>
        public IExecutive CreateExecutive(ExecType execType, Guid guid)
        {
            IExecutive exec = null;
            switch(execType)
            {
                case ExecType.FullFeatured:
                    exec = CreateExecutive(typeof (Highpoint.Sage.SimCore.Executive).FullName, guid);
                    break;
                case ExecType.SingleThreaded:
                    exec = CreateExecutive(typeof(Highpoint.Sage.SimCore.ExecutiveFastLight).FullName, guid);
                    break;
                case ExecType.SingleThreadedWithRollback:
                    exec = CreateExecutive(typeof(Highpoint.Sage.SimCore.ExecutiveFastLight).FullName, guid);
                    if (exec != null)
                    {
                        ((ExecutiveFastLight)exec).SupportsRollback = true;
                    }
                    break;
                default:
                    throw new ApplicationException("Attempt to create an instance of an unsupported executive (" + execType + ").");
            }
            return exec;
        }

        public IExecutive CreateExecutive(string typeName, Guid guid) {

            Type type = Type.GetType(typeName);
            if ( type == null ) throw new ApplicationException("Attempt to create an executive of type \"" + typeName + "\", but that type does not exist.");
            BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            //Binder binder = null;
            //ParameterModifier[] modifiers = null;

            ConstructorInfo ci = type.GetConstructor(bindingAttr, null, new[] { typeof(Guid) }, null);
            object objExec = ci.Invoke(new object[] { guid });
            IExecutive exec = (IExecutive)objExec;

            return exec;

        }


        private string m_requiredType;
        /// <summary>
        /// Creates a copy of the default type of executive.
        /// </summary>
        /// <param name="execGuid">The guid to be assigned to the new executive.</param>
        /// <returns>A copy of the default executive.</returns>
        public IExecutive CreateExecutive(Guid execGuid) {
            if (m_requiredType == null) {
                m_requiredType = "Highpoint.Sage.SimCore.Executive, Sage";
                NameValueCollection nvc = (NameValueCollection)ConfigurationManager.GetSection("Sage");
                if (nvc == null) {
                    _Debug.WriteLine("Warning - <Sage> section missing from config file for " + Process.GetCurrentProcess().ProcessName);
                } else if (nvc["ExecutiveType"] != null) {
                    m_requiredType = nvc["ExecutiveType"];
                }
            }
            return CreateExecutive(m_requiredType, execGuid);
        }
    }
}