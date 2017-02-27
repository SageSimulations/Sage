/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Scheduling {

	/// <summary>
	/// Declares the property that is being changed in a TimePeriodAdjustment. By
	/// examining the TimeAdjustmentMode of the target time period, the user can
	/// determine what changed/will change.
	/// </summary>
	public enum TimePeriodPart { 
        /// <summary>
        /// The start time is being changed. 
        /// </summary>
        StartTime, 
        /// <summary>
        /// The duration is being changed.
        /// </summary>
        Duration, 
        /// <summary>
        /// The end time is being changed.
        /// </summary>
        EndTime 
    }
	
}
