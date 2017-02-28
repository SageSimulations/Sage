/* This source code licensed under the GNU Affero General Public License */

using System;
using _Debug = System.Diagnostics.Debug;
using System.Collections;


namespace Highpoint.Sage.SimCore {

	/// <summary>
	/// Implemented by a method that is to be called when an warning is added to, or removed from the model.
	/// </summary>
	public delegate void WarningEvent(IModelWarning modelWarning);
	/// <summary>
	/// Implemented by a method that is to be called when an error is added to, or removed from the model.
	/// </summary>
	public delegate void ErrorEvent(IModelError modelError);

	/// <summary>
	/// The base class from which models are built or derived. Models provide a state machine,
	/// error and warning management, task processor management (for Task Graphs) and access
	/// and control functions to the Executive that is running the simulation embodied in this
	/// model.
	/// </summary>
    public interface IModel : IHasIdentity, IHasParameters, IModelObject, IDisposable {

        /// <summary>
        /// Gets the random seed in use by this model.
        /// </summary>
        /// <value>The random seed in use by this model.</value>
		ulong RandomSeed { get; }

        /// <summary>
        /// Gets the random server.
        /// </summary>
        /// <value>The random server.</value>
		Randoms.RandomServer RandomServer { get ; }

		#region >>> Manage Model Objects <<<
		/// <summary>
		/// A dictionary of currently live IModelObjects. An IModelObject that is garbage-
		/// collected is automatically removed from this collection. Note that the object
		/// is not necessarily removed at the time of last release, but at the time of
		/// garbage collection. Code can call Remove(...) to explicitly remove the object.
		/// </summary>
		ModelObjectDictionary ModelObjects { get; }

        /// <summary>
        /// Adds a model object to this model's ModelObjects collection.
        /// </summary>
        /// <param name="modelObject">The model object.</param>
		void AddModelObject(IModelObject modelObject);

		#endregion

		/// <summary>
        /// The ModelConfig is an object that holds the contents of the Sage section of the
		/// app.config file.
		/// </summary>
		ModelConfig ModelConfig { get; }

        /// <summary>
        /// Gets the executive controller that governs the rate-throttling and frame-rendering event frequency of this model.
        /// </summary>
        /// <value>The executive controller.</value>
        ExecController ExecutiveController { get; set; }

		/// <summary>
		/// Provides access to the executive being used by this model.
		/// </summary>
		IExecutive Executive { get; }

		#region >>> Error and Warning Management <<<

		/// <summary>
		/// An collection of all of the warnings currently applicable to this model.
		/// </summary>
        ICollection Warnings { get; }
		
		/// <summary>
		/// Adds a warning to this model, e.g. a 'GenericModelWarning'...
		/// </summary>
		/// <param name="theWarning">The warning to be added.</param>
        void AddWarning(IModelWarning theWarning);

        /// <summary>
        /// Returns true if this model has any active warnings.
        /// </summary>
        /// <returns>Returns true if this model has any active warnings - otherwise, false.</returns>
		bool HasWarnings();

        /// <summary>
        /// Clears all of the warnings applicable to this model.
        /// </summary>
		void ClearAllWarnings();

        /// <summary>
        /// Fired when an error happens in (is added to) a model.
        /// </summary>
        event ErrorEvent ErrorHappened;

		/// <summary>
		/// Fired when an error is removed from a model.
		/// </summary>
		event ErrorEvent ErrorCleared;

        #region ErrorHandlers
		/// <summary>
		/// Enables a user/developer to add an error handler to the model in real time,
		/// (e.g. during a simulation run) and ensures that that handler is called for
		/// any errors currently in existence in the model.
		/// </summary>
		/// <param name="theErrorHandler">The error handler delegate that is to receive notification of the error events.</param>
        void AddErrorHandler(IErrorHandler theErrorHandler);

		/// <summary>
		/// Removes an error handler from the model.
		/// </summary>
		/// <param name="theErrorHandler">The error handler to be removed from the model.</param>
        void RemoveErrorHandler(IErrorHandler theErrorHandler);
        #endregion

		/// <summary>
		/// An enumeration over all of the errors in the model.
		/// </summary>
        ICollection Errors { get; }

        /// <summary>
        /// Adds an error to the model, and iterates over all of the error handlers,
        /// allowing each in turn to respond to the error. As soon as any errorHandler
        /// indicates that it has HANDLED the error (by returning true from 'HandleError'),
        /// the error is cleared, and further handlers are not called.
        /// </summary>
        /// <param name="theError">The error that is to be added to the model's error list.</param>
        /// <returns>True if the error was successfully added to the model, false if it was cleared by a handler.</returns>
		bool AddError(IModelError theError);

		/// <summary>
		/// Removes the error from the model's collection of errors.
		/// </summary>
		/// <param name="theError">The error to be removed from the model.</param>
        void RemoveError(IModelError theError);

        /// <summary>
        /// Removes all errors.
        /// </summary>
        void ClearAllErrors();

        /// <summary>
        /// Removes all errors whose target is the specified object.
        /// </summary>
        /// <param name="target">The object for whom all errors are to be removed.</param>
		void ClearAllErrorsFor(object target);

		/// <summary>
		/// Returns true if the model has errors.
		/// </summary>
		/// <returns>true if the model has errors.</returns>
        bool HasErrors();

		/// <summary>
		/// Provides a string that summarizes all of the errors currently active in this model.
		/// </summary>
        string ErrorSummary { get; }

        #endregion

        #region >>> State Management <<<

		/// <summary>
		/// Provides access to the state machine being used by this model. While the state machine
		/// can be set, too, this is an advanced feature, and should not be done unless the developer
		/// is sure what they are doing.
		/// </summary>
        StateMachine StateMachine {get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is running.
        /// </summary>
        /// <value><c>true</c> if this instance is running; otherwise, <c>false</c>.</value>
        bool IsRunning { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is paused.
        /// </summary>
        /// <value><c>true</c> if this instance is paused; otherwise, <c>false</c>.</value>
        bool IsPaused { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is completed.
        /// </summary>
        /// <value><c>true</c> if this instance is completed; otherwise, <c>false</c>.</value>
        bool IsCompleted { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is ready to be started.
        /// </summary>
        /// <value><c>true</c> if this instance is ready to be started; otherwise, <c>false</c>.</value>
        bool IsReady { get; set; }

        /// <summary>
        /// Starts the model.
        /// </summary>
        void Start();

        /// <summary>
        /// Pauses execution of this model after completion of the running callback of the current event.
        /// </summary>
        void Pause();

        /// <summary>
        /// Resumes execution of this model. Ignored if the model is not already paused.
        /// </summary>
        void Resume();

		/// <summary>
		/// Aborts the model.
		/// </summary>
        void Abort();

        /// <summary>
        /// Resets the model. First resets the executive, then fires the Model.Resetting event.
        /// </summary>
        void Reset();
        
		/// <summary>
		/// Fired when the model has been commanded to start.
		/// </summary>
		event ModelEvent Starting;

        /// <summary>
        /// Fired when the model has been commanded to stop.
        /// </summary>
        event ModelEvent Stopping;

        /// <summary>
        /// Fired when the model has been commanded to reset.
        /// </summary>
        event ModelEvent Resetting;

        /// <summary>
		/// Fired when the model has completed.
		/// </summary>
		event ModelEvent Completed;
        #endregion

        /// <summary>
        /// Adds the specified service with the provided name.
        /// </summary>
        /// <typeparam name="T">The type of service we are adding.</typeparam>
        /// <param name="service">The service.</param>
        /// <param name="name">The name.</param>
        void AddService<T>(T service, string name = null) where T : IModelService;

        /// <summary>
        /// Gets the service of the specified type, and known by the provided name.
        /// </summary>
        /// <typeparam name="T">The type of service we are looking for.</typeparam>
        /// <param name="identifier">The identifier.</param>
        /// <returns>T.</returns>
        T GetService<T>(string identifier = null) where T : IModelService;
    }
}