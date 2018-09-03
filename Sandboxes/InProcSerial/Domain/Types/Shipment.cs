using System.Collections.Generic;
using System.Linq;

namespace ModelRunner
{
    internal class Shipment : List<Order>
    {
        private static int m_snCursor = 0;

        public Shipment()
        {
            ShipmentNumber = m_snCursor++;
        }
        public int ShipmentNumber { get; private set; }
        public double NetVolume(SKU[] skus)
        {
            double retval = 0.0;
            retval = this.Aggregate(retval, (a, b) => a + (b.Quantity * skus[b.SKU].Volume));
            return retval;
        }
    }
}