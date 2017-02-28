/* This source code licensed under the GNU Affero General Public License */

using System;
using _Debug = System.Diagnostics.Debug;
using System.Collections;

namespace Highpoint.Sage.SimCore {

    /// <summary>
    /// This interface is implemented by an object that provides notification of some event.
    /// </summary>
	public interface INotification {
		/// <summary>
		/// The name of the notification.
		/// </summary>
		string Name{ get; }
		/// <summary>
		/// A descriptive text that describes what happened.
		/// </summary>
		string Narrative{ get; }
		/// <summary>
		/// Target is the place that the notification occurred.
		/// </summary>
		object Target{ get; }
		/// <summary>
		/// Subject is the thing that (probably) caused the notification.
		/// </summary>
		object Subject{ get; }
	}

	/// <summary>
	/// This interface is implemented by an object that serves to indicate that an
	/// error has occurred in the model.
	/// </summary>
	public interface IModelError : INotification {
//		/// <summary>
//		/// The name of the error.
//		/// </summary>
//		string Name{ get; }
//		/// <summary>
//		/// A descriptive text that describes what happened.
//		/// </summary>
//		string Narrative{ get; }
//		/// <summary>
//		/// Target is the place that the error occurred.
//		/// </summary>
//		object Target{ get; }
//		/// <summary>
//		/// Subject is the thing that (probably) caused the error.
//		/// </summary>
//		object Subject{ get; }

		/// <summary>
		/// An exception that may have been caught in the detection of this error.
		/// </summary>
		Exception InnerException { get; }

        /// <summary>
        /// Gets a value indicating whether this error should be automatically cleared at the start of a simulation.
        /// </summary>
        /// <value><c>true</c> if [auto clear]; otherwise, <c>false</c>.</value>
        bool AutoClear { get; }
	}

	/// <summary>
	/// This interface is implemented by an object that serves to indicate that a
	/// warning has occurred in the model.
	/// </summary>
	public interface IModelWarning : INotification {
//		/// <summary>
//		/// The name of the warning.
//		/// </summary>
//		string Name{ get; }
//		/// <summary>
//		/// A descriptive text that describes what happened.
//		/// </summary>
//		string Narrative{ get; }
//		/// <summary>
//		/// Target is the place that the warning occurred.
//		/// </summary>
//		object Target{ get; }
//		/// <summary>
//		/// Subject is the thing that (probably) caused the warning.
//		/// </summary>
//		object Subject{ get; }
	}

	/// <summary>
	/// Implemented by an object that is able to handle (and perhaps resolve) an error.
	/// </summary>
	public interface IErrorHandler {
		/// <summary>
		/// Called when an individual error occurs, and gives the error handler an opportunity to
		/// resolve the error.
		/// </summary>
		/// <param name="modelError">The error that just occurred.</param>
		/// <returns>true if the error was handled.</returns>
		bool HandleError(IModelError modelError);
		/// <summary>
		/// Called to give the error handler an opportunity to handle all currently-existent errors
		/// in one fell swoop. This is typically called immediately prior to attempting a requested
		/// state transition, and if, after attempting resolution, any errors remain, the requested
		/// transition is made to fail.
		/// </summary>
		/// <param name="modelErrors">An IEnumerable that contains the errors to be handled.</param>
		void HandleErrors(IEnumerable modelErrors);
	}

	/// <summary>
	/// A basic implementation of IModelError. 
	/// </summary>
	public class GenericModelError : IModelError {
		private object m_target;
		private string m_name;
		private string m_narrative;
		private object m_subject = null;
		private Exception m_innerException;
        private bool m_autoClear = false;

		/// <summary>
		/// Creates an instance of a basic implementation of IModelError.
		/// </summary>
		/// <param name="name">The short name of the error.</param>
		/// <param name="narrative">A longer narrative of the error.</param>
		/// <param name="target">The target of the error - where the error happened.</param>
		/// <param name="subject">The subject of the error - who probably caused it.</param>
		public GenericModelError(string name, string narrative, object target, object subject)
		:this(name,narrative,target,subject,null){}

		/// <summary>
		/// Creates an instance of a basic implementation of IModelError.
		/// </summary>
		/// <param name="name">The short name of the error.</param>
		/// <param name="narrative">A longer narrative of the error.</param>
		/// <param name="target">The target of the error - where the error happened.</param>
		/// <param name="subject">The subject of the error - who probably caused it.</param>
		/// <param name="innerException">An exception that may have been caught in the detection of this error.</param>
		public GenericModelError(string name, string narrative, object target, object subject, Exception innerException){ 
			m_name      = name;
			m_narrative = narrative;
			m_target    = target;
			m_subject   = subject;
			m_innerException = innerException;
		}

		#region Implementation of IModelError
		/// <summary>
		/// The short name of the error.
		/// </summary>
		public string Name { get { return m_name; } }
		/// <summary>
		/// A longer narrative of the error.
		/// </summary>
		public string Narrative { get { return m_narrative; } }
		/// <summary>
		/// The target of the error - where the error happened.
		/// </summary>
		public object Target { get { return m_target; } }
		/// <summary>
		/// The subject of the error - who probably caused it.
		/// </summary>
		public object Subject { get { return m_subject; } }
		/// <summary>
		/// The exception, if any, that generated this ModelError.
		/// </summary>
		public Exception InnerException { get { return m_innerException; } }

        /// <summary>
        /// Gets a value indicating whether this error should be automatically cleared at the start of a simulation.
        /// </summary>
        /// <value><c>true</c> if [auto clear]; otherwise, <c>false</c>.</value>
        public bool AutoClear { get { return m_autoClear; } }

		#endregion

		public override string ToString() {
			string innerExString = (m_innerException==null?"":" Inner exception = " + m_innerException + ".");
			string subjectString = m_subject == null ? "<NoSubject>" : "Subject = " + m_subject;
			return m_name + ": " + m_narrative + " (" + subjectString + ", Target = " + m_target + "." + innerExString;
		}

	}

	/// <summary>
	/// A basic implementation of IModelWarning. 
	/// </summary>
	public class GenericModelWarning : IModelWarning {
		private object m_target;
		private string m_name;
		private string m_narrative;
		private object m_subject = null;

		/// <summary>
		/// Creates an instance of a basic implementation of IModelWarning.
		/// </summary>
		/// <param name="name">The short name of the warning.</param>
		/// <param name="narrative">A longer narrative of the warning.</param>
		/// <param name="target">The target of the warning - where the warning happened.</param>
		/// <param name="subject">The subject of the warning - who probably caused it.</param>
		public GenericModelWarning(string name, string narrative, object target, object subject){ 
			m_name = name;
			m_narrative = narrative;
			m_target = target;
			m_subject   = subject;
		}

		#region Implementation of IModelWarning
		/// <summary>
		/// The short name of the warning.
		/// </summary>
		public string Name { get { return m_name; } }
		/// <summary>
		/// A longer narrative of the warning.
		/// </summary>
		public string Narrative { get { return m_narrative; } }
		/// <summary>
		/// The target of the warning - where the warning happened.
		/// </summary>
		public object Target { get { return m_target; } }
		/// <summary>
		/// The subject of the warning - who probably caused it.
		/// </summary>
		public object Subject { get { return m_subject; } }
		#endregion

		public override string ToString() {
			string subjectString = m_subject == null ? "<NoSubject>" : "Subject = " + m_subject;
			return m_name + ": " + m_narrative + " (" + subjectString + ", Target = " + m_target + ".";
		}

	}
	
	/// <summary>
	/// An error that is registered as a result of an exception having been thrown, unhandled, by the model.
	/// </summary>
	public class ModelExceptionError : GenericModelError {
		private Exception m_exception;
		/// <summary>
		/// Creates a ModelExceptionError around a thrown exception.
		/// </summary>
		/// <param name="ex">The exception that caused this error.</param>
		public ModelExceptionError(Exception ex):base("Model Exception Error",ex.Message,null,null){
			m_exception = ex;
		}

		/// <summary>
		/// The exception that caused this error.
		/// </summary>
		public Exception BaseException { get { return m_exception; } }

	}


}

