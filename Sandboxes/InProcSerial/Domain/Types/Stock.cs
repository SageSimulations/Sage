namespace ModelRunner
{
    internal struct Stock
    {
        private readonly double m_unitVolume;
        private int m_quantity;
        private int m_onOrder;

        public Stock(double unitVolume, int quantity=0)
        {
            m_unitVolume = unitVolume; // TODO: Reference UnitVolume at the FC level only.
            m_quantity = quantity;
            NetVolume = m_unitVolume * m_quantity;
            //WatchMe = false;
            m_onOrder = 0;
        }

        public int Quantity
        {
            get { return m_quantity; }
            set { m_quantity = value;
                //if ( WatchMe && m_quantity == 9 ) System.Diagnostics.Debugger.Break();
                NetVolume = m_unitVolume*m_quantity;
            }
        }

        public int OnOrder {
            get { return m_onOrder; }
            set { m_onOrder = value; }// if (WatchMe && m_onOrder == 0) System.Diagnostics.Debugger.Break(); }
        }

        public double NetVolume { get; private set; }

        //public bool WatchMe { get; set; }
    
    }
}