namespace ReorderWPF
{
    using System;
    using System.ComponentModel;
    using WHLClasses;

    public class ReorderDataLoader
    {
        public SupplierOrderData LoadSupplierOrderData(Guid orderGuid)
        {
            var returnable = new SupplierOrderData();
            var orderData = MSSQLPublic.SelectDataDictionary("SELECT * from whldata.reorder_orders WHERE orderguid='" + orderGuid.ToString() + "';");
            return returnable;
        }

        public void SaveCurrentOrderToDatabase(object sender, DoWorkEventArgs e)
        {
            if (sender.GetType() != typeof(SupplierOrderData))
            {
                throw new NullReferenceException();
            }
            
                var currentOrder = sender as SupplierOrderData;
            if (currentOrder == null)
            {
                throw new NullReferenceException();
            }

                MSSQLPublic.insertUpdate("DELETE FROM whldata.reorder_orders WHERE OrderGUID = '" + currentOrder.OrderGuid.ToString() + "'");
                MSSQLPublic.insertUpdate(
                    @"INSERT INTO whldata.reorder_orders (OrderGUID, SupplierCode, CustomOrderID, OrderDate, OrderDelivered, OrderInvoiced, LinesOfStock, NetValue, CustomOrderNote, CustomDeliveryNote,OrderState)
                                    VALUES ('" + currentOrder.OrderGuid + "','" + currentOrder.SupplierCode + "','" +
                    currentOrder.OrderId + "','" + currentOrder.OrderDate.ToString() + "','" +
                    currentOrder.OrderDelivered.ToString() + "','" + currentOrder.OrderInvoiced.ToString() + "','" +
                    currentOrder.LinesOfStock.ToString() + "'," +
                    "'" + currentOrder.NetValue.ToString() + "','" + currentOrder.CustomOrderNote + "','" +
                    currentOrder.CustomDeliveryNote + "','" + currentOrder.OrderState.ToString() + "';");
            
        }
    }
}
