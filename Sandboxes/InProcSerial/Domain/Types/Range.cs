using System;
using System.Xml.Serialization;

namespace ModelRunner
{
    /// <summary>
    /// Struct Range contains a high and a low value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Range<T>
    {
        public Range(T min, T max)
        {
            Min = min;
            Max = max;
        } 
        public T Min { get; set; }
        public T Max { get; set; }
    }

    public struct TimeSpanRange
    {
        public TimeSpanRange(TimeSpan min, TimeSpan max)
        {
            Min = min;
            Max = max;
        }
        [XmlElement(Type = typeof(Highpoint.Sage.Utility.XmlTimeSpan))]
        public TimeSpan Min { get; set; }
        [XmlElement(Type = typeof(Highpoint.Sage.Utility.XmlTimeSpan))]
        public TimeSpan Max { get; set; }
        
    }
}