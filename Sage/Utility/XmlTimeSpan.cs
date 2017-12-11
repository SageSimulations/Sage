using System;

namespace Highpoint.Sage.Utility
{
    /// <summary>
    /// Class XmlTimeSpan. Unbelievable that TimeSpan STILL isn't xml serializable.
    /// (What am I missing? Why isn't it?)
    /// To use in serializing a timespan, add this attribute to the property. 
    /// [XmlElement(Type = typeof(Highpoint.Sage.Utility.XmlTimeSpan))]
    /// </summary>
    public class XmlTimeSpan
    {
        private const long m_ticksPerMs = TimeSpan.TicksPerMillisecond;

        private TimeSpan m_value = TimeSpan.Zero;

        public XmlTimeSpan() { }
        public XmlTimeSpan(TimeSpan source) { m_value = source; }

        public static implicit operator TimeSpan? (XmlTimeSpan o)
        {
            return o?.m_value;
        }

        public static implicit operator XmlTimeSpan(TimeSpan? o)
        {
            return o == null ? null : new XmlTimeSpan(o.Value);
        }

        public static implicit operator TimeSpan(XmlTimeSpan o)
        {
            return o?.m_value ?? default(TimeSpan);
        }

        public static implicit operator XmlTimeSpan(TimeSpan o)
        {
            return o == default(TimeSpan) ? null : new XmlTimeSpan(o);
        }

        [System.Xml.Serialization.XmlText]
        public long Default
        {
            get { return m_value.Ticks / m_ticksPerMs; }
            set { m_value = new TimeSpan(value * m_ticksPerMs); }
        }
    }
}
