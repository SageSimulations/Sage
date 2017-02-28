/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using _Debug = System.Diagnostics.Debug;
#pragma warning disable 1587

/// <summary>
/// There are several key areas of interest in SimCore. They are the Executive, the Model, and the State Machine.
/// <para/>
/// The base engine behind Sage® consists of two orthogonal pieces, the executive and the model. The executive 
/// is a C# class with several supporting classes, that performs event (callback) registration and sequencing. In 
/// essence, through the executive, a model entity may request to receive a callback at a specific time, with a specific 
/// priority, on a specified method, and with a specified object provided to that method on the callback. The entity 
/// may rescind that request at any point before the call, and the method need not be located on the entity requesting 
/// the callback. Further, the entity requesting the callback may select how the callback is to be handled, currently 
/// among three choices:<para/>
/// 1.	<b>Synchronous</b> – the callback is called on the dispatch thread, and upon completion, the next callback is
/// selected based upon scheduled time and priority. This is similar to the “event queue” implementations in Garrido
/// (1993) and Law and Kelton (2000).<para/>
/// 2.	<b>Detachable</b> – the callback is called on a thread from the .Net thread pool, and the dispatch thread then
/// suspends awaiting the completion or suspension of that thread. If the event thread is sus-pended, an event controller
/// is made available to other entities which can be used to resume or abort that thread. This is useful for modeling
/// “intelligent entities” and situations where the developer wants to easily represent a delay or interruption of a process.<para/>
/// 3.	<b>Asynchronous</b> – the callback is called on a thread from the thread pool that is, in essence, fire-and-forget.
/// This is useful when the thread has a long task to perform, such as I/O, or external system interfacing (i.e. data export)
/// and the results of that activity cannot affect the simulation.
/// <code>// public member of Executive class.
/// public long RequestEvent(
/// ExecEventReceiver eer, // user callback
/// DateTime when, 
/// double priority, 
/// object userData, 
/// ExecEventType execEventType){ … }
/// </code>
/// <para/>
/// The Model class provided with Sage® performs containment and coordination between the executive, the model state 
/// machine and model entities such as queues, customers, manufacturing stages, transport hubs, etc.
/// <para/>
/// The model’s state machine is used to control and indicate the state of the model – for example, a model that has
/// states such as design, initialization, warmup, run, cooldown, data analysis, and perhaps pause, would represent
/// each of those states in the state machine. Additionally, the application designer may attach a handler to any specified
/// transition into or out of any given state, or between two specific states. Handlers may be given a sequence number to
/// describe the order in which they are to be executed. Each transition is performed through a two-phase-commit protocol,
/// with a prepare phase permitting registrants to indicate approval or denial of the transition, and a commit or rollback
/// phase completing or canceling the attempted transition.
/// The following code describes the interface that is implemented by a transition handler. User code may implement any of 
/// the three delegates (API signatures) at the top of the listing, and add the callback to the handlers for transition out
/// of, into, or between specified stages.
/// <code>
/// public delegate ITransitionFailureReason PrepareTransitionEvent(Model model);
/// public delegate void CommitTransitionEvent(Model model);
/// public delegate void RollbackTransitionEvent(Model model, IList reasons);
/// 
/// public interface ITransitionHandler {
/// 	event PrepareTransitionEvent Prepare;
/// 	event CommitTransitionEvent Commit;
/// 	event RollbackTransitionEvent Rollback;
/// 	bool IsValidTransition { get; }
/// 
/// 	void AddPrepareEvent(PrepareTransitionEvent pte,double sequence);
/// 	void RemovePrepareEvent(PrepareTransitionEvent pte);
/// 	void AddCommitEvent(CommitTransitionEvent cte,double sequence);
/// 	void RemoveCommitEvent(CommitTransitionEvent cte);
/// 	void AddRollbackEvent(RollbackTransitionEvent rte,double sequence);
/// 	void RemoveRollbackEvent(RollbackTransitionEvent rte);
/// }
/// </code>
/// </summary>
namespace Highpoint.Sage.SimCore {

    /// <summary>
    /// An interface implemented by anything that is known by a name. The name is not necessarily required to be unique.
    /// </summary>
	public interface IHasName { 
		/// <summary>
		/// The user-friendly name for this object.
		/// </summary>
        string Name { get; }	
	}

	/// <summary>
	/// Implemented by any object that is likely to be tracked by the core, or
	/// perhaps a user, framework.
	/// </summary>
    public interface IHasIdentity : IHasName {

		/// <summary>
		/// A description of this object.
		/// </summary>
		string Description { get; }

		/// <summary>
		/// The Guid for this object. Typically required to be unique.
		/// </summary>
        Guid Guid { get; }
    }

    /// <summary>
    /// Implemented by any object that has a dictionary of parameters.
    /// </summary>
	public interface IHasParameters {
        /// <summary>
        /// Gets the parameters dictionary.
        /// </summary>
        /// <value>The parameters.</value>
		IDictionary Parameters { get; }
	}

    /// <summary>
    /// A Comparer that is used to sort implementers of IHasIdentity on their names.
    /// </summary>
    public class HasNameComparer : IComparer {
        #region IComparer Members
        private IComparer m_comparer = Comparer.Default;
        public int Compare(object x, object y) {
            return m_comparer.Compare(( (IHasName)x ).Name, ( (IHasName)y ).Name);
        }
        #endregion
    }

    /// <summary>
    /// A Comparer that is used to sort implementers of IHasIdentity on their names.
    /// </summary>
    public class HasNameComparer<T> : System.Collections.Generic.Comparer<T> where T : IHasName {
        public override int Compare(T x, T y) {
            return Comparer.Default.Compare(x.Name, y.Name);
        }
    }


	/// <summary>
	/// Implemented by an object that 'belongs' to a model, or that needs to know its
	/// model in order to function properly.
	/// </summary>
    public interface IModelObject : IHasIdentity {
		/// <summary>
		/// The model that owns this object, or from which this object gets time, etc. data.
		/// </summary>
        IModel Model { get; }

        /// <summary>
        /// Initializes the fields that feed the properties of this IModelObject identity.
        /// </summary>
        /// <param name="model">The IModelObject's new model value.</param>
        /// <param name="name">The IModelObject's new name value.</param>
        /// <param name="description">The IModelObject's new description value.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
        void InitializeIdentity(IModel model, string name, string description, Guid guid);

        //bool Intrinsic { set; get; }


    }

	#region Sample Implementation of IModelObject
#if NOT_DEFINED
// A recommended implementation to ^C^V.
        #region Implementation of IModelObject
        private string m_name = null;
        private Guid m_guid = Guid.Empty;
        private IModel m_model;
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
		public string Description { [System.Diagnostics.DebuggerStepThrough] get { return ((m_description==null)?("No description for " + m_name):m_description); } }
        
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

#endif

    #endregion

    /// <summary>
    /// A helper class that contains logic for initializing and registering ModelObjects.
    /// </summary>
    public static class IMOHelper {

        /// <summary>
        /// Initializes the specified m_model.
        /// </summary>
        /// <param name="m_model">The m_model field in the IModelObject.</param>
        /// <param name="model">The model to initialize the IModelObject's field with.</param>
        /// <param name="m_name">The m_name field in the IModelObject.</param>
        /// <param name="name">The name to initialize the IModelObject's field with.</param>
        /// <param name="m_description">The m_description field in the IModelObject.</param>
        /// <param name="description">The description to initialize the IModelObject's field with.</param>
        /// <param name="m_guid">The m_guid field in the IModelObject.</param>
        /// <param name="guid">The GUID to initialize the IModelObject's field with.</param>
        public static void Initialize(ref IModel m_model, IModel model, ref string m_name, string name, ref string m_description, string description, ref Guid m_guid, Guid guid) {

            if (m_model == null && m_guid.Equals(Guid.Empty)) {
                m_model = model;
                m_name = name;
                if (description == null || description.Equals("")) {
                    m_description = name;
                } else {
                    m_description = description;
                }
                m_guid = guid;
            } else {
                string identity = "Model=" + ( m_model == null ? "<null>" : ( m_model.Name == null ? m_model.Guid.ToString() : m_model.Name ) ) +
                    ", Name=" + ( m_name == null ? "<null>" : m_name ) + ", Description=" + ( m_description == null ? "<null>" : m_description ) +
                    ", Guid=" + m_guid;

                throw new ApplicationException("Cannot call InitializeIdentity(...) on an IModelObject that is already initialized. " +
                    "The IModelobject's Identity is:\r\n[" + identity + "].");
            }
        }

        /// <summary>
        /// Registers the IModelObject with the model by adding the IModelObject to the IModel's ModelObjectDictionary.
        /// </summary>
        /// <param name="imo">The IModelObject.</param>
        public static void RegisterWithModel(IModelObject imo) {
            if (!( imo.Guid.Equals(Guid.Empty) ) && ( imo.Model != null )) {
                imo.Model.AddModelObject(imo);
            }
        }

        /// <summary>
        /// Registers the provided IModelObject, keyed on its Guid, with the model, replacing any existing one with the new one, if so indicated.
        /// </summary>
        /// <param name="imo">The imo.</param>
        /// <param name="replaceOk">if set to <c>true</c> [replace OK].</param>
        public static void RegisterWithModel(IModelObject imo, bool replaceOk) {
            if (!( imo.Guid.Equals(Guid.Empty) ) && ( imo.Model != null )) {
                if (imo.Model.ModelObjects.Contains(imo.Guid)) {
                    if (!imo.Model.ModelObjects[imo.Guid].Equals(imo)) {
                        imo.Model.ModelObjects.Remove(imo.Guid);
                        imo.Model.AddModelObject(imo);
                    }
                } else {
                    imo.Model.AddModelObject(imo);
                }
            }
        }
    }
}
