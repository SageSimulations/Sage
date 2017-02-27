/* This source code licensed under the GNU Affero General Public License */
#if INCLUDE_WIP
using System;
using System.Diagnostics;
using System.Collections;
using Trace = System.Diagnostics.Debug;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Utility;

namespace Highpoint.Sage.Scheduling {

	/// <summary>
	/// An object that implements this interfaces has aspects that relate to time
	/// periods. For example, a task may have two time period aspects, one responding
	/// to the MyTask.PlannedTimePeriod key, and one responding to the
	/// myTask.ObservedTimePeriod key.
	/// </summary>
	public interface IHasTimePeriodAspects {
		/// <summary>
		/// Returns the ITimePeriod that characterizes the given aspect of the
		/// implementing object.
		/// </summary>
		/// <param name="aspectKey">the key object that identifies the time period aspect.</param>
		/// <returns>The identified Time Period Aspect.</returns>
		ITimePeriod GetTimePeriodAspect(object aspectKey);
	}
}
#endif