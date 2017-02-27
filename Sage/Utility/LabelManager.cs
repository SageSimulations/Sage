/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;
using System.Threading;

namespace Highpoint.Sage.Utility {

    /// <summary>
    /// Implemented by an object that is capable of managing context-specific labels.
    /// </summary>
    public interface IHasLabel {

        /// <summary>
        /// Gets or sets the label in the currently-selected context, or if none has been selected, then according to the default context.
        /// </summary>
        /// <value>The label.</value>
        string Label { get; set; }

        /// <summary>
        /// Sets the label in the context indicated by the provided context, or if null or String.Empty has been selected, then in the default context.
        /// </summary>
        /// <param name="label">The label.</param>
        /// <param name="context">The context - use null or string.Empty for the default context.</param>
        void SetLabel(string label, string context);

        /// <summary>
        /// Gets the label from the context indicated by the provided context, or if null or String.Empty has been selected, then from the default context.
        /// </summary>
        /// <param name="context">The context - use null or string.Empty for the default context.</param>
        string GetLabel(string context);
    }
    
    /// <summary>
    /// A manager to which a class that implements a Label can delegate.
    /// </summary>
    public class LabelManager : IHasLabel {

        private static readonly LocalDataStoreSlot s_ldss;
        /// <summary>
        /// The name of the Thread-Local-Storage data slot in which the current context key is stored.
        /// </summary>
        public static readonly string CONTEXTSLOTNAME = "SageLabelContext";

        /// <summary>
        /// The name of the default contents of the Thread-Local-Storage data slot in which the current context key is stored.
        /// </summary>
        public static readonly string DEFAULT_CHANNEL = "SageDefaultLabelContext";

        private readonly Dictionary<string, string> m_labels;

        /// <summary>
        /// Initializes the <see cref="T:LabelManager"/> class by creating a Thread-Local-Storage data slot for the key.
        /// </summary>
        static LabelManager() {
            s_ldss = Thread.AllocateNamedDataSlot(CONTEXTSLOTNAME);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:LabelManager"/> class.
        /// </summary>
        public LabelManager() {
            m_labels = new Dictionary<string, string> {[DEFAULT_CHANNEL] = String.Empty};
        }

        /// <summary>
        /// Sets the label context for all unspecified requests in this thread.
        /// </summary>
        /// <param name="context">The context.</param>
        public static void SetContext(string context) {
            LocalDataStoreSlot ldss = Thread.GetNamedDataSlot(CONTEXTSLOTNAME);
            Thread.SetData(ldss,context);
        }

        #region IHasLabel Members

        /// <summary>
        /// Gets or sets the label in the currently-selected context, or if none has been selected, then according to the default context.
        /// </summary>
        /// <value>The label.</value>
        public string Label {
            get {
                if (m_labels.ContainsKey(Key)) {
                    return m_labels[Key];
                } else {
                    return string.Empty;
                }
            }
            set {
                if (m_labels.ContainsKey(Key)) {
                    m_labels[Key] = value;
                } else {
                    m_labels.Add(Key, value);
                }
            }
        }

        /// <summary>
        /// Sets the label in the context indicated by the provided context, or if null or String.Empty has been selected, then in the default context.
        /// </summary>
        /// <param name="label">The label.</param>
        /// <param name="context">The context - use null or string.Empty for the default context.</param>
        public void SetLabel(string label, string context) {
            if (context == null || context.Equals(string.Empty)) {
                context = DEFAULT_CHANNEL;
            }
            m_labels[context] = label;
        }

        /// <summary>
        /// Gets the label from the context indicated by the provided context, or if null or String.Empty has been selected, then from the default context.
        /// </summary>
        /// <param name="context">The context - use null or string.Empty for the default context.</param>
        /// <returns></returns>
        public string GetLabel(string context) {
            if (context == null || context.Equals(string.Empty)) {
                context = DEFAULT_CHANNEL;
            }
            return m_labels[context];
        }

        #endregion

        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <value>The key.</value>
        private string Key => (string)Thread.GetData(s_ldss) ?? DEFAULT_CHANNEL;
    }
}