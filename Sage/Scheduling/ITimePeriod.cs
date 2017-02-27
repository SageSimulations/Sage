/* This source code licensed under the GNU Affero General Public License */

using System;
using Highpoint.Sage.SimCore;

namespace Highpoint.Sage.Scheduling {

    /// <summary>
    /// Implemented in a method that is to be called after an observable.
    /// Do not respond to this notification by changing the whoChanged object, and be aware that
    /// it is not legal to update U/I elements on any but the thread on which they were created.
    /// </summary>
    public delegate void ObservableChangeHandler(object whoChanged, object whatChanged, object howChanged);

    /// <summary>
    /// IObservable is implemented by an object that is capable of notifying others of its changes.
    /// </summary>
    public interface IObservable {
        /// <summary>
        /// ObservableChangeHandler is an event that is fired after an object changes.
        /// </summary>
        event ObservableChangeHandler ChangeEvent;
    }

    public interface ITimePeriodBase {
        /// <summary>
        /// Reads and writes the Start Time. Modification of other parameters is according to
        /// the AdjustmentMode (which defaults to FixedDuration.)
        /// </summary>
        DateTime StartTime { get; set; }

        /// <summary>
        /// Reads and writes the End Time.
        /// </summary>
        DateTime EndTime { get; set; }

        /// <summary>
        /// Reads and writes the Duration.
        /// </summary>
        TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Implemented by any object that embodies a time period.
    /// </summary>
    public interface ITimePeriod : ITimePeriodBase, IHasIdentity, IObservable {

        /// <summary>
        /// Gets the subject of this time period - for example, the task for which this time period represents the start, duration and end times.
        /// </summary>
        /// <value>The subject.</value>
        ISupportsCorrelation Subject { get; set; }

        /// <summary>
        /// Gets the modifier for the subject - the context for which the time period 
        /// relates to the subject. This might be an iteration count, a key indicating plan
        /// or actual, or some other similar value.
        /// </summary>
        /// <value>The modifier.</value>
        object Modifier { get; set; }

		/// <summary>
		/// Sets the start time to an indeterminate value.
		/// </summary>
		void ClearStartTime();

		/// <summary>
		/// Sets the end time to an indeterminate time.
		/// </summary>
		void ClearEndTime();

		/// <summary>
		/// Sets the duration to an indeterminate time.
		/// </summary>
		void ClearDuration();

		/// <summary>
		/// Determines what inferences are to be made about the other two settings when one
		/// of the settings (start, duration, finish times) is changed.
		/// </summary>
		TimeAdjustmentMode AdjustmentMode { get; set; }

		/// <summary>
		/// Pushes the current time period adjustment mode onto a stack, substituting a provided mode. This
		/// must be paired with a corresponding Pop operation.
		/// </summary>
		/// <param name="tam">The time period adjustment mode that is to temporarily take the place of the current one.</param>
		void PushAdjustmentMode(TimeAdjustmentMode tam);

		/// <summary>
		/// Pops the previous time period adjustment mode off a stack, and sets this Time Period's adjustment mode to that value.
		/// </summary>
		/// <returns>The newly-popped time period adjustment mode.</returns>
		TimeAdjustmentMode PopAdjustmentMode();

		/// <summary>
		/// The milestone that represents the starting of this time period.
		/// </summary>
		IMilestone StartMilestone { get; }

		/// <summary>
		/// The milestone that represents the ending point of this time period.
		/// </summary>
		IMilestone EndMilestone { get; }

		/// <summary>
		/// True if the time period has a determinate start time.
		/// </summary>
		bool HasStartTime { get; }

		/// <summary>
		/// True if the time period has a determinate end time.
		/// </summary>
		bool HasEndTime { get; }

		/// <summary>
		/// True if the time period has a determinate duration.
		/// </summary>
		bool HasDuration { get; }

		/// <summary>
		/// Adds a relationship between this time period and some other time period. Shorthand for actually creating the
		/// relationship and its reciprocal, setting each as the other's reciprocal, and adding them to the appropriate
		/// milestones.
		/// </summary>
		/// <param name="relationship">The dependent relationship.</param>
		/// <param name="otherTimePeriod">Describes the nature of the relationship.</param>
		void AddRelationship(TimePeriod.Relationship relationship, ITimePeriod otherTimePeriod );

        /// <summary>
        /// Adds a relationship between this time period and some other time period. Shorthand for actually creating the
        /// relationship and its reciprocal, setting each as the other's reciprocal, and adding them to the appropriate
        /// milestones.
        /// </summary>
        /// <param name="relationship">The relationship.</param>
        /// <param name="otherTimePeriod">The other time period.</param>
        void RemoveRelationship(TimePeriod.Relationship relationship, ITimePeriod otherTimePeriod);

    }

    public interface ISupportsCorrelation : IHasIdentity {
        Guid ParentGuid { get; }
        int InstanceCount { get; }
    }
}
