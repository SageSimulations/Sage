/* This source code licensed under the GNU Affero General Public License */
namespace Highpoint.Sage.Scheduling {

	/// <summary>
	/// Three ways of adjusting start, duration and end parameters on a time
	/// period. These three parameters are all dependent on each other, so
	/// when adjusting one of them, this enum tells us which of the other two
	/// should be held constant.
	/// </summary>
	public enum TimeAdjustmentMode { 
		/// <summary>
		/// No inferences are made. Responsibility for the coherent management and use
		/// of the three underlying data points lies with the user.
		/// </summary>
		None,
		/// <summary>
		/// Fixed start implies that if either the duration or finish time are modified,
		/// the other of these two will be adjusted to ensure that the three data points
		/// are consistent, but the start time does not change.
		/// </summary>
		FixedStart, 
		/// <summary>
		/// Fixed duration implies that if either the start or finish time are modified,
		/// the other of these two will be adjusted to ensure that the three data points
		/// are consistent, but the duration does not change.
		/// </summary>
		FixedDuration, 
		/// <summary>
		/// Fixed end implies that if either the start or duration time are modified,
		/// the other of these two will be adjusted to ensure that the three data points
		/// are consistent, but the end time does not change.
		/// </summary>
		FixedEnd,
		/// <summary>
		/// InferStartTime implies that either duration or end time may be set, and the
		/// start time will be inferred from the duration and end times. Setting of the
		/// start time while in this mode is not legal. 
		/// </summary>
		InferStartTime,
		/// <summary>
		/// InferDuration implies that either start time or end time may be set, and the
		/// duration will be inferred from the start and end times. Setting of the
		/// duration while in this mode is not legal. 
		/// </summary>
		InferDuration,
		/// <summary>
		/// InferEndTime implies that either start time or duration may be set, and the
		/// end time will be inferred from the start time and duration. Setting of the
		/// end time while in this mode is not legal.
		/// </summary>
		InferEndTime,
		/// <summary>
		/// Prohibits any adjustment of the time period.
		/// </summary>
		Locked
	}
}
