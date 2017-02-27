/* This source code licensed under the GNU Affero General Public License */

using System.Collections;

namespace Highpoint.Sage.Resources {

	/// <summary>
	/// An object that can be used by a ResourceManager to permit or deny individual
	/// resource aquisition reqests.
	/// </summary>
	public interface IAccessRegulator {
		/// <summary>
		/// Returns true if the given subject can be acquired using the presented key.
		/// </summary>
		/// <param name="subject">The resource whose acquisition is being queried.</param>
		/// <param name="usingKey">The key that is to be presented by the prospective acquirer.</param>
		/// <returns>True if the acquire will be allowed, false if not.</returns>
		bool CanAcquire(object subject, object usingKey);
	}

	/// <summary>
	/// An object that manages multiple access regulators. They are managed in stacks,
	/// with one stack for each specific resource under management, and one stack for
	/// any requests for resources that do not have specific regulators assigned to them.
	/// </summary>
	public interface IAccessManager : IAccessRegulator {
		/// <summary>
		/// Pushes an access regulator onto the stack that is associated with a particular resource, or
		/// the default stack, if no resource is specified.
		/// </summary>
		/// <param name="accReg">Access Regulator to be pushed.</param>
		/// <param name="subject">The resource to which this regulator is to apply, or null, if it applies to all of them.</param>
		void PushAccessRegulator(IAccessRegulator accReg, IResource subject);
		/// <summary>
		/// Pops the top access regulator from the stack associated with the specified resource, or from the
		/// default stack if subject is set as null.
		/// </summary>
		/// <param name="subject">The resource to be regulated, or null if all are to be regulated.</param>
		/// <returns>The AccessRegulator being popped, or null, if the stack was empty.</returns>
		IAccessRegulator PopAccessRegulator(IResource subject);
	}

	/// <summary>
	/// A SimpleAccessManager is made a part of the resource acquisition protocol that is
	/// embodied in all resource managers. When a resource manager is aware of an access
	/// manager, it asks that access manager if any resource request is grantable
	/// before it even allows the resource request to score the available resources. Therefore,
	/// an access manager uses Access Regulators to prevent resource requests from being granted
	/// in certain cases.
	/// <para></para>
	/// A SimpleAccessManager manages a single AccessRegulator that it applies across all
	/// resources that are presented to it, or it manages a stack of AccessRegulators that
	/// are applied to specified resources.
	/// <para></para>
	/// NOTE: If an AccessManager has a default regulator as well as resource-specific ones, the
	/// resource-specific ones take precedence.
	/// </summary>
	public class SimpleAccessManager : IAccessManager {

		#region Private Fields

		private readonly Hashtable m_monitoredObjects;
		private readonly bool m_autoDeleteEmptyStacks;
		private Stack m_defaultAccessRegulators;

		#endregion 

		/// <summary>
		/// Creates an access manager that removes resource-specific stacks of regulators once they
		/// are empty.
		/// </summary>
		public SimpleAccessManager():this(true){}
		/// <summary>
		/// See the default ctor - this ctor allows the developer to decide if they want to remove any
		/// stack that is assigned to a specific resource once it is empty. One might set this arg to
		/// false if there will be many adds &amp; removes of regulators, and it is expected that the stack
		/// will empty and refill often.
		/// </summary>
		/// <param name="autoDeleteEmptyStacks">True if you want the SimpleAccessManager to perform clean up.</param>
		public SimpleAccessManager(bool autoDeleteEmptyStacks){
			m_autoDeleteEmptyStacks = autoDeleteEmptyStacks;
			m_monitoredObjects = new Hashtable();
			m_defaultAccessRegulators = new Stack();
		}

		/// <summary>
		/// Pushes an access regulator onto the stack that is associated with a particular resource, or
		/// the default stack, if no resource is specified.
		/// </summary>
		/// <param name="accReg">Access Regulator to be pushed.</param>
		/// <param name="subject">The resource to which this regulator is to apply, or null, if it applies to all of them.</param>
		public void PushAccessRegulator(IAccessRegulator accReg, IResource subject){
			if ( subject == null ) {
				if ( m_defaultAccessRegulators == null ) m_defaultAccessRegulators = new Stack();
				m_defaultAccessRegulators.Push(accReg);
			} else {
				Stack stack = (Stack)m_monitoredObjects[subject];
				if ( stack == null ) {
					stack = new Stack();
					m_monitoredObjects.Add(subject,stack);
				}
				stack.Push(accReg);
			}
		}

		/// <summary>
		/// Pops the top access regulator from the stack associated with the specified resource, or from the
		/// default stack if subject is set as null.
		/// </summary>
		/// <param name="subject">The resource to be regulated, or null if all are to be regulated.</param>
		/// <returns>The AccessRegulator being popped, or null, if the stack was empty.</returns>
		public IAccessRegulator PopAccessRegulator(IResource subject){
			IAccessRegulator retval = null;
			if ( subject == null ) {
				retval = (IAccessRegulator)m_defaultAccessRegulators.Pop();
				if ( m_defaultAccessRegulators.Count==0 && m_autoDeleteEmptyStacks ) m_defaultAccessRegulators = null;
			} else {
				Stack stack = (Stack)m_monitoredObjects[subject];
				if ( stack != null ) {
					retval = (IAccessRegulator)stack.Pop();
					if ( m_autoDeleteEmptyStacks && stack.Count == 0 )m_monitoredObjects.Remove(subject);
				}
			}
			return retval;
		}

		/// <summary>
		/// Returns true if the given subject can be acquired using the presented key.
		/// </summary>
		/// <param name="subject">The resource whose acquisition is being queried.</param>
		/// <param name="usingKey">The key that is to be presented by the prospective acquirer.</param>
		/// <returns>True if the acquire will be allowed, false if not.</returns>
		public bool CanAcquire(object subject, object usingKey){
			Stack myStack = (Stack)m_monitoredObjects[subject];
			if ( myStack != null ) {
				IAccessRegulator iar = (IAccessRegulator)myStack.Peek();
				return ( iar == null || iar.CanAcquire(subject,usingKey));
			} else {
				if ( m_defaultAccessRegulators == null || m_defaultAccessRegulators.Count == 0 ) return true;
				IAccessRegulator iar = (IAccessRegulator)m_defaultAccessRegulators.Peek();
				return iar.CanAcquire(subject,usingKey);
			}
		}
	}

	/// <summary>
	/// Grants access to the requestor if the subject is null or matches
	/// the requested subject, and the stored key matches the provided key
	/// via the .Equals(...) operator.
	/// </summary>
	public class SingleKeyAccessRegulator : IAccessRegulator {

		#region Private Fields

		private readonly object m_key;
		private readonly object m_subject;

		#endregion 

        /// <summary>
        /// Creates a new instance of the <see cref="T:SingleKeyAccessRegulator"/> class.
        /// </summary>
        /// <param name="subject">The subject.</param>
        /// <param name="key">The key.</param>
		public SingleKeyAccessRegulator(object subject, object key){
			m_subject = subject;
			m_key = key;
		}

        /// <summary>
        /// Returns true if the given subject can be acquired using the presented key.
        /// </summary>
        /// <param name="subject">The resource whose acquisition is being queried.</param>
        /// <param name="usingKey">The key that is to be presented by the prospective acquirer.</param>
        /// <returns>
        /// True if the acquire will be allowed, false if not.
        /// </returns>
		public bool CanAcquire(object subject, object usingKey){
			return ( ( m_subject == null || m_subject.Equals(subject) || subject.Equals(m_subject) ) && m_key.Equals(usingKey) );
		}
	}

	/// <summary>
	/// An access regulator that maintains a list of keys, the presentation
	/// of an object with a .Equals(...) match to any one of which will result
	/// in an allowed acquisition.
	/// </summary>
	public class MultiKeyAccessRegulator : IAccessRegulator {

		#region Private Fields

		private readonly ArrayList m_keys;
		private readonly object m_subject;

		#endregion 

        /// <summary>
        /// Creates a new instance of the <see cref="T:MultiKeyAccessRegulator"/> class.
        /// </summary>
        /// <param name="subject">The subject that the caller wiches to acquire.</param>
        /// <param name="keys">The keys that the caller is presenting, in hopes of an acquisition.</param>
		public MultiKeyAccessRegulator(object subject, ArrayList keys){
			m_subject = subject;
			m_keys = keys;
		}

        /// <summary>
        /// Returns true if the given subject can be acquired using the presented key.
        /// </summary>
        /// <param name="subject">The resource whose acquisition is being queried.</param>
        /// <param name="usingKey">The key that is to be presented by the prospective acquirer.</param>
        /// <returns>
        /// True if the acquire will be allowed, false if not.
        /// </returns>
		public bool CanAcquire(object subject, object usingKey){
			return ( (m_subject.Equals(subject) || subject.Equals(m_subject)) && m_keys.Contains(usingKey) );
		}
	}
}
