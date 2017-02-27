/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
#pragma warning disable 1587

/// <summary>
/// Scaling is an operation that transforms an object or set of objects, typically a
/// Material Transfer Request.
/// This transformation may occur in one or many dimensions, and some
/// dimensions may not be transformed equally. The entity doing the scaling
/// would like not to have to remember the original scale, but would like
/// to be able to rescale to the original size by setting scale to unity.
/// Scaling is seen as a monolithic event - that is, the scaling operation
/// applies to the entire entity at once, and the entity now exists at the
/// new scale, without having to remember its own "unity" scale.<p></p><p></p>
/// The scaling architecture allows the developer to decorate objects with
/// other object that apply scale to them.<p></p>
/// Any object that implements IScalable may be scaled by attaching a
/// scaling engine. The scaling engine must then be connected to the
/// scalable object through a scaling adapter.<p></p> 
/// When a scaling operation is desired on an object, call that object's
/// scaling engine, and set a new scale.
/// </summary>
namespace Highpoint.Sage.Mathematics.Scaling {

    /// <summary>
    /// This interface is implemented by any object that can be scaled.
    /// </summary>
    public interface IScalable {
        /// <summary>
        /// Called to command a rescaling operation where the scalable object is
        /// rescaled directly to a given scale. +1.0 sets the scale to it's original
        /// value. +2.0 sets the scale to twice its original value, +0.5 sets the
        /// scale to half of its original value.
        /// </summary>
        /// <param name="newScale">The new scale for the IScalable.</param>
        void Rescale(double newScale);

    }

    /// <summary>
    /// An object that is able to apply scaling to another object. It can
    /// also subsequently remove that scaling from the same object. 
    /// </summary>
    public interface IScalingEngine {
        /// <summary>
        /// The combined, aggregate scale of all of the subjects of this scaling engine
        /// compared to their original scale.
        /// </summary>
        double AggregateScale { get; set; }

        /// <summary>
        /// Rescales the implementer by the provided factor.
        /// </summary>
        /// <param name="byFactor">The factor.</param>
        void Rescale(double byFactor);
    }

    /// <summary>
    /// An engine that is capable of performing groupwise rescaling of a set of <see cref="Highpoint.Sage.Mathematics.Scaling.IScalable"/>s.
    /// </summary>
    public class ScalingEngine : IScalingEngine
    {
        #region Private fields.
        private readonly IEnumerable m_subjectEnumerator;
        private double m_aggregate;
        #endregion

        /// <summary>
        /// Creates a new instance of the <see cref="T:ScalingEngine" /> class.
        /// </summary>
        /// <param name="scalables">The scalables.</param>
        public ScalingEngine(IEnumerable scalables){
            m_subjectEnumerator = scalables;
            m_aggregate = 1;
        }

        /// <summary>
        /// The scale to be applied to the target object. Cannot scale by a factor of zero.
        /// </summary>
        public void Rescale(double byFactor){
            // TODO: Figure out a scaling rollback strategy if somebody pukes on scaling.
            m_aggregate*=byFactor;
            foreach ( IScalable scalable in m_subjectEnumerator ) {
                scalable.Rescale(m_aggregate);
            }
        }

        /// <summary>
        /// The combined, aggregate scale of all of the subjects of this scaling engine
        /// compared to their original scale.
        /// </summary>
        public double AggregateScale { 
            get{
                return m_aggregate;
            }
            set{
                Rescale(1.0/m_aggregate); // TODO: Think about this. In non-linear cases this is probably necessary.
                Rescale(value);
            }
        }    
    }

    /// <summary>
    /// Implemented by an object that has a double value that can be scaled.
    /// </summary>
    public interface IDoubleScalingAdapter : IScalable {
        /// <summary>
        /// Gets the current value of the data that is scaled by this adapter.
        /// </summary>
        /// <value>The current value of the data that is scaled by this adapter.</value>
        double CurrentValue { get; }

        /// <summary>
        /// Gets the value of the data that is scaled by this adapter when scale is 1.0.
        /// </summary>
        /// <value>The full scale value.</value>
        double FullScaleValue { get; }

        /// <summary>
        /// Clones this IDoubleScalingAdapter.
        /// </summary>
        /// <returns>The closed instance.</returns>
		IDoubleScalingAdapter Clone();
    }

    /// <summary>
    /// Implemented by an object that has a TimeSpan value that can be scaled.
    /// </summary>
    public interface ITimeSpanScalingAdapter : IScalable {
        /// <summary>
        /// Gets the current value of the data that is scaled by this adapter.
        /// </summary>
        /// <value>The current value of the data that is scaled by this adapter.</value>
        TimeSpan CurrentValue { get; }

        /// <summary>
        /// Gets the value of the data that is scaled by this adapter when scale is 1.0.
        /// </summary>
        TimeSpan FullScaleValue { get; }

        /// <summary>
        /// Clones this ITimeSpanScalingAdapter.
        /// </summary>
        /// <returns>The cloned instance.</returns>
        ITimeSpanScalingAdapter Clone();
    }

    /// <summary>
    /// A class that manages linear scaling of a double. If linearity is 2.0, for example, 
    /// a rescaling of 2.0 quadruples the underlying value, and a rescaling of 0.5 quarters
    /// the underlying value.
    /// </summary>
    public class DoubleLinearScalingAdapter : IDoubleScalingAdapter
    {

        #region Private Fields

        private readonly double m_originalValue;
        private readonly double m_linearity;
        private double m_currentValue;

        #endregion 

        /// <summary>
        /// Creates a new instance of the <see cref="T:DoubleLinearScalingAdapter"/> class.
        /// </summary>
        /// <param name="originalValue">The original value of the underlying data.</param>
        /// <param name="linearity">The linearity.</param>
        public DoubleLinearScalingAdapter(double originalValue, double linearity){
            m_currentValue = m_originalValue = originalValue;
            m_linearity = linearity;
        }

        /// <summary>
        /// Rescales the underlying data by the specified aggregate scale, taking this DoubleLinearScalingAdapter's linearity into account.
        /// </summary>
        /// <param name="aggregateScale">The aggregate scale.</param>
        public void Rescale(double aggregateScale){
            m_currentValue = m_originalValue*(1-(m_linearity*(1-aggregateScale)));
        }

        /// <summary>
        /// Gets the current value of the data that is scaled by this adapter.
        /// </summary>
        /// <value>The current value of the data that is scaled by this adapter.</value>
        public double CurrentValue => m_currentValue;

        /// <summary>
        /// Gets the value of the data that is scaled by this adapter when scale is 1.0.
        /// </summary>
        /// <value>The full scale value.</value>
        public double FullScaleValue => m_originalValue;

        /// <summary>
        /// Clones this IDoubleScalingAdapter.
        /// </summary>
        /// <returns>The closed instance.</returns>
		public IDoubleScalingAdapter Clone(){
			return new DoubleLinearScalingAdapter(m_originalValue,m_linearity);
		}

        /// <summary>
        /// Gets the original value of the underlying data.
        /// </summary>
        /// <value>The original value.</value>
		public double OriginalValue => m_originalValue;

        /// <summary>
        /// Gets the linearity of this DoubleLinearScalingAdapter.
        /// </summary>
        /// <value>The linearity.</value>
		public double Linearity => m_linearity;
    }

    /// <summary>
    /// A class that manages linear scaling of a TimeSpan. If linearity is 2.0, for example, 
    /// a rescaling of 2.0 quadruples the underlying value, and a rescaling of 0.5 quarters
    /// the underlying value. Slope of the scaling line.
    /// </summary>
    public class TimeSpanLinearScalingAdapter : ITimeSpanScalingAdapter {

        #region Private Fields
        private readonly double m_linearity;
        private TimeSpan m_originalValue;
        private TimeSpan m_currentValue;

        #endregion 

        /// <summary>
        /// Creates a new instance of the <see cref="T:TimeSpanLinearScalingAdapter"/> class.
        /// </summary>
        /// <param name="originalValue">The original value of this TimeSpanLinearScalingAdapter's underlying data.</param>
        /// <param name="linearity">The linearity.</param>
        public TimeSpanLinearScalingAdapter(TimeSpan originalValue, double linearity){
            m_currentValue = m_originalValue = originalValue;
            m_linearity = linearity;
        }

        /// <summary>
        /// Rescales the underlying data by the specified aggregate scale, taking this TimeSpanLinearScalingAdapter's linearity into account.
        /// </summary>
        /// <param name="aggregateScale">The aggregate scale.</param>
        public void Rescale(double aggregateScale){
            double factor = (1-(m_linearity*(1-aggregateScale)));
            m_currentValue = TimeSpan.FromTicks((long)(m_originalValue.Ticks*factor));
        }

        /// <summary>
        /// Gets the current value of the data that is scaled by this adapter.
        /// </summary>
        /// <value>The current value of the data that is scaled by this adapter.</value>
        public TimeSpan CurrentValue => m_currentValue;

        /// <summary>
        /// Gets the value of the data that is scaled by this adapter when scale is 1.0.
        /// </summary>
        /// <value></value>
        public TimeSpan FullScaleValue => m_originalValue;

        /// <summary>
        /// Clones this TimeSpanLinearScalingAdapter.
        /// </summary>
        /// <returns>The cloned instance.</returns>
		public ITimeSpanScalingAdapter Clone(){
			return new TimeSpanLinearScalingAdapter(m_originalValue,m_linearity);
		}

        /// <summary>
        /// Gets the original value of the data that is scaled by this adapter.
        /// </summary>
        /// <value>The original value of the data that is scaled by this adapter.</value>
		public TimeSpan OriginalValue => m_originalValue;

        /// <summary>
        /// Gets the linearity of this TimeSpanLinearScalingAdapter.
        /// </summary>
        /// <value>The linearity.</value>
		public double Linearity => m_linearity;
    }
}