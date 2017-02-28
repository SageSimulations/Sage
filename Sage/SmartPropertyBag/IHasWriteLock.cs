/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Diagnostics;
using _Debug = System.Diagnostics.Debug;
using System.Collections;
// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.SimCore {

    /// <summary>
    /// Implemented by an object that can be set to read-only.
    /// </summary>
    public interface IHasWriteLock {

        /// <summary>
        /// Gets a value indicating whether this instance is currently writable.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is writable; otherwise, <c>false</c>.
        /// </value>
        bool IsWritable{ get; }
    }
    
    /// <summary>
    /// Fires when an object that can be made read-only, changes its writability status.
    /// </summary>
    /// <param name="newWritableState">true if the object is now writable.</param>
    public delegate void WritabilityChangeEvent(bool newWritableState);

    /// <summary>
    /// A class that manages the details of nestable write locking - that is, a parent that is write-locked implies that its children are thereby also write-locked.
    /// </summary>
    public class WriteLock : IHasWriteLock
    {

        #region Private Fields

        private bool m_writable;
        private readonly ArrayList m_children;
        private string m_whereApplied;
        private static readonly bool s_locationTracingEnabled = Diagnostics.DiagnosticAids.Diagnostics("WriteLockTracing");

        #endregion 

        /// <summary>
        /// Creates a new instance of the <see cref="T:WriteLock"/> class.
        /// </summary>
        /// <param name="initiallyWritable">if set to <c>true</c> [initially writable].</param>
        public WriteLock(bool initiallyWritable){
            m_writable = initiallyWritable;
            m_children = new ArrayList();
			if ( !s_locationTracingEnabled ) m_whereApplied = s_tracing_Off_Msg;
        }

        /// <summary>
        /// Fires when the object that this lock is overseeing, changes its writability status.
        /// </summary>
        public event WritabilityChangeEvent WritabilityChanged;

        /// <summary>
        /// Gets a value indicating whether this instance is currently writable.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is writable; otherwise, <c>false</c>.
        /// </value>
        public bool IsWritable => m_writable;

        /// <summary>
        /// Sets the value indicating whether this instance is currently writable.
        /// </summary>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        public void SetWritable(bool writable){
            if ( writable == m_writable ) return;
			if ( s_locationTracingEnabled ) {
				if ( writable ) {
					m_whereApplied = null;
				} else {
					StackTrace st = new StackTrace(true);
					m_whereApplied = "";
					for ( int i = st.FrameCount-1 ; i >0 ; i-- ) { 
					    m_whereApplied += st.GetFrame(i).ToString();
					}
				}
			}
            m_writable = writable;
            WritabilityChanged?.Invoke(m_writable);
            foreach ( var obj in m_children ) {
                ((WriteLock)obj).SetWritable(writable);
            }
        }
        /// <summary>
        /// Gets the location in a hierarchy of write-locked objects where the write-lock was applied.
        /// </summary>
        /// <value>The where applied.</value>
        public string WhereApplied => m_whereApplied;

        /// <summary>
        /// Adds a dependent child object to this WriteLock.
        /// </summary>
        /// <param name="child">The child.</param>
        public void AddChild(WriteLock child){m_children.Add(child);}
        /// <summary>
        /// Removes a dependent child object from this WriteLock.
        /// </summary>
        /// <param name="child">The child.</param>
        public void RemoveChild(WriteLock child){m_children.Remove(child);}
        /// <summary>
        /// Clears the children from this WriteLock.
        /// </summary>
        public void ClearChildren() { m_children.Clear(); }

		private static readonly string s_tracing_Off_Msg = 
			@"[WriteLock tracing is off. Turn it on with an entry in the AppConfig file diagnostics section that looks like\r\n		<add key=""WriteLockTracing""					value=""true"" />";

    }

    /// <summary>
    /// Thrown when someone tries to change a value that is write-locked.
    /// </summary>
    public class WriteProtectionViolationException : Exception {


        #region Private Fields

        #endregion 

        /// <summary>
        /// Creates a new instance of the <see cref="T:WriteProtectionViolationException"/> class.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="msg">The message.</param>
        public WriteProtectionViolationException(object target, string msg) : base(msg){
            Target = target;
        }
        /// <summary>
        /// Creates a new instance of the <see cref="T:WriteProtectionViolationException"/> class.
        /// </summary>
        /// <param name="target">The target - the object that received the attempt to change its value.</param>
        /// <param name="writeLock">The write lock that is watching that target.</param>
        public WriteProtectionViolationException(object target, WriteLock writeLock) : 
            base("Attempted write protection violation in " + target + (writeLock.WhereApplied!=null?". WriteLock applied at : \r\n" + writeLock.WhereApplied:".")){
            Target = target;
        }
        /// <summary>
        /// Creates a new instance of the <see cref="T:WriteProtectionViolationException"/> class.
        /// </summary>
        /// <param name="target">The target - the object that received the attempt to change its value.</param>
        public WriteProtectionViolationException(object target) : base("Attempted write protection violation in " + target){
            Target = target;
        }
        /// <summary>
        /// Gets the target - the object that received the attempt to change its value.
        /// </summary>
        /// <value>The target.</value>
        public object Target { get; }
    }
}