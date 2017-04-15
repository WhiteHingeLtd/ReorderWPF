using System;
using System.Collections.Generic;
using WHLClasses;

namespace ReorderWPF
{
    public class SupplierOrderData
    {
        public Guid OrderGuid;
        public string SupplierCode = "";
        public string OrderId = "";
        public DateTime OrderDate;
        public DateTime OrderDelivered;
        public DateTime OrderInvoiced;
        public int LinesOfStock = 0;
        public Single NetValue = 0;
        public string CustomOrderNote = "";
        public string CustomDeliveryNote = "";
        public Dictionary<WhlSKU, int> SkuOrderList;
        public int OrderState = 0;
        public enum OrderStates
        {
            Ordered = 0,
            Invoiced = 1,
            Delivered = 2,
            Closed = 3
        }

        public SupplierOrderData(string Supplier)
        {

        }
        public SupplierOrderData(Supplier Supplier)
        {
            SupplierCode = Supplier.Code;
            OrderGuid = new Guid();
            OrderDate = DateTime.Now;
            LinesOfStock = 0;
            NetValue = 0;
            CustomDeliveryNote = "";
        }

        public SupplierOrderData(bool cancel = true)
        {
            
        }

}
}
