/* This source code licensed under the GNU Affero General Public License */

// ReSharper disable UnusedParameter.Global
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedMethodReturnValue.Global
namespace Highpoint.Sage.Utility {

    // TODO: Obsolete the Tuple implementation and get it to use the new .NET Tuple class if possible.
    /// <summary>
    /// Interface ITuple is a key/data pair. It is used only in the TupleSpace implementation
    /// </summary>
    public interface ITuple {
		/// <summary>
		/// TODO: All objects in a tuple must be able to be a key. This is a simplified use case where the zeroth element is the only key.
		/// </summary>
		object Key { get; }
		/// <summary>
		/// TODO: All objects in a tuple must be able to be a key. This is a simplified use case where the 1st element is the only data (though it may be a list.)
		/// </summary>
		object Data { get; }
        /// <summary>
        /// Called when a Tuple is posted to a TupleSpace.
        /// </summary>
        /// <param name="ts">The ts.</param>
        void OnPosted(ITupleSpace ts);
        /// <summary>
        /// Called when a Tuple is read from a TupleSpace.
        /// </summary>
        /// <param name="ts">The ts.</param>
        void OnRead(ITupleSpace ts);
        /// <summary>
        /// Called when a Tuple is taken from a TupleSpace.
        /// </summary>
        /// <param name="ts">The ts.</param>
        void OnTaken(ITupleSpace ts);
	}


    /// <summary>
    /// Describes a participant's role in a handoff through a TupleSpace.
    /// </summary>
    public enum TsOpType {
        /// <summary>
        /// The participant will place the token into the common area.
        /// </summary>
        Post,
        /// <summary>
        /// The participant will read the token, and leave it in the common area.
        /// </summary>
        Read,
        /// <summary>
        /// The participant will take the token from the common area.
        /// </summary>
        Take
    }

	public delegate void TupleEvent(ITupleSpace space, ITuple tuple);


    /// <summary>
    /// Interface ITupleSpace describes a simple tupleSpace, perhaps better thought of as a whiteboard or exchange. Things are posted (added), read, and removed according to a provided key value.
    /// </summary>
    public interface ITupleSpace {
        /// <summary>
        /// Gets a value indicating whether this TupleSpace permits multiple Tuples to be posted under the same key. Currently, this will be false.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this TupleSpace permits multiple Tuples to be posted under the same key; otherwise, <c>false</c>.
        /// </value>
		bool PermitsDuplicateKeys { get; }
        /// <summary>
        /// Posts the specified tuple. If blocking is true, this call blocks 
        /// the caller's thread until the Tuple is taken from the space by another caller.
        /// </summary>
        /// <param name="tuple">The tuple.</param>
        /// <param name="blocking">if set to <c>true</c> this call blocks 
        /// the caller's thread until the Tuple is taken from the space by another caller..</param>
		void Post(ITuple tuple, bool blocking);
        /// <summary>
        /// Posts a Tuple with the specified key and data. If blocking is true, this call blocks 
        /// the caller's thread until the Tuple is taken from the space by another caller.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="data">The data.</param>
        /// <param name="blocking">if set to <c>true</c> [blocking].</param>
		void Post(object key, object data, bool blocking);
        /// <summary>
        /// Reads the specified key, returning null if it is not present in the TupleSpace.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="blocking">if set to <c>true</c> [blocking].</param>
        /// <returns>The Tuple stored under the specified key</returns>
		ITuple Read(object key, bool blocking);
        /// <summary>
        /// Takes the Tuple entered under the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="blocking">if set to <c>true</c> the calling thread will not return until a Tuple has been found with the
        /// specified key value.</param>
        /// <returns>The Tuple stored under the specified key</returns>
        ITuple Take(object key, bool blocking);
        /// <summary>
        /// Blocks the calling thread until the specified key is not in the TupleSpace.
        /// </summary>
        /// <param name="key">The key.</param>
        void BlockWhilePresent(object key);

        /// <summary>
        /// Fires when a Tuple has been posted.
        /// </summary>
		event TupleEvent TuplePosted;
        /// <summary>
        /// Fires when a Tuple has been read.
        /// </summary>
        event TupleEvent TupleRead;
        /// <summary>
        /// Fires when a Tuple has been taken.
        /// </summary>
        event TupleEvent TupleTaken;
	}
}