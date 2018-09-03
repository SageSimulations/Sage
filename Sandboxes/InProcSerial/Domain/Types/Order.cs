namespace ModelRunner
{
    internal struct Order
    {
        private static int m_onCursor = 0;
        public Order(int sku, int quantity = 1)
        {
            SKU = sku;
            Quantity = quantity;
            OrderNumber = m_onCursor++;
        }
        public int SKU { get; }
        public int Quantity { get; }
        public int OrderNumber { get; private set; }
    }
}