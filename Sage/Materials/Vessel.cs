/* This source code licensed under the GNU Affero General Public License */
using System;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Utility;

namespace Highpoint.Sage.Materials.Chemistry
{
    public class Vessel  : IContainer, IModelObject, IResettable {

        #region Private Fields
        private static readonly Guid s_mixture_Guidmask = new Guid("4500F94B-DAE7-4399-B5E3-DC2EAD036ECD");
        private bool m_autoReset;
        private Mixture m_mixture;
        private double m_capacity;
        private double m_initialPressure;
        private double m_pressure;
        private DoubleTracker m_mixtureVolume;
        private DoubleTracker m_mixtureMass;
        #endregion

        public Vessel(IModel model, string name, string description, Guid guid, double capacity, double pressure, bool autoReset) {
            InitializeIdentity(model, name, description, guid);
            m_mixture = new Mixture(model, name + ".Mixture", GuidOps.XOR(guid, s_mixture_Guidmask));
            m_mixtureMass = new DoubleTracker();
            m_mixtureVolume = new DoubleTracker();
            m_mixture.MaterialChanged += new MaterialChangeListener(m_mixture_MaterialChanged);
            m_pressure = m_initialPressure = pressure;
            m_capacity = capacity;
            m_autoReset = autoReset;
            m_model.Starting += new ModelEvent(m_model_Starting);
        }

        /// <summary>
        /// Gets a double tracker that records the initial, minimum, maximum, and final mixture mass.
        /// </summary>
        /// <value>The mixture mass.</value>
        public DoubleTracker MixtureMass { get { return m_mixtureMass; } }

        /// <summary>
        /// Gets a double tracker that records the initial, minimum, maximum, and final mixture volume.
        /// </summary>
        /// <value>The mixture volume.</value>
        public DoubleTracker MixtureVolume { get { return m_mixtureVolume; } }

        #region IContainer Members

        public Mixture Mixture {
            get { return m_mixture; }
        }

        public double Capacity {
            get { return m_capacity; }
        }

        public double Pressure {
            get { return m_pressure; }
        }

        public bool AutoReset { get { return m_autoReset; } set { m_autoReset = value; } }

        #endregion

        #region IResettable Members

        /// <summary>
        /// Performs a reset operation on this instance.
        /// </summary>
        public void Reset() {
            m_mixture.Clear();
            m_pressure = m_initialPressure;
            m_mixtureVolume.Reset();
            m_mixtureMass.Reset();
            m_mixtureMass.Register(m_mixture.Mass);
            m_mixtureVolume.Register(m_mixture.Volume);
        }

        #endregion

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

        private void m_mixture_MaterialChanged(IMaterial material, MaterialChangeType type) {
            m_mixtureMass.Register(m_mixture.Mass);
            m_mixtureVolume.Register(m_mixture.Volume);
        }

        private void m_model_Starting(IModel theModel) {
            if (m_autoReset) {
                Reset();
            }
        }
    }
}
