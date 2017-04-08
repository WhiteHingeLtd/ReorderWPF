using System;
using System.ComponentModel;
using WHLClasses;

namespace ReorderWPF
{
    public class ReorderDataLoader
    {
        public SupplierOrderData LoadSupplierOrderData(Guid orderGuid)
        {
            var returnable = new SupplierOrderData();
            var OrderData = MSSQLPublic.SelectDataDictionary("SELECT * from whldata.reorder_orders WHERE orderguid='" + orderGuid.ToString() + "';");
            return returnable;
        }
        public void SaveCurrentOrderToDatabase(object sender, DoWorkEventArgs e)
        {
            if (sender.GetType() != typeof(SupplierOrderData)) throw new NullReferenceException();
            
                var CurrentOrder = sender as SupplierOrderData;
                if (CurrentOrder == null) throw new NullReferenceException();
                MSSQLPublic.insertUpdate("DELETE FROM whldata.reorder_orders WHERE OrderGUID = '" + CurrentOrder.OrderGuid.ToString() + "'");
                MSSQLPublic.insertUpdate(
                    @"INSERT INTO whldata.reorder_orders (OrderGUID, SupplierCode, CustomOrderID, OrderDate, OrderDelivered, OrderInvoiced, LinesOfStock, NetValue, CustomOrderNote, CustomDeliveryNote,OrderState)
                                    VALUES ('" + CurrentOrder.OrderGuid + "','" + CurrentOrder.SupplierCode + "','" +
                    CurrentOrder.OrderId + "','" + CurrentOrder.OrderDate.ToString() + "','" +
                    CurrentOrder.OrderDelivered.ToString() + "','" + CurrentOrder.OrderInvoiced.ToString() + "','" +
                    CurrentOrder.LinesOfStock.ToString() + "'," +
                    "'" + CurrentOrder.NetValue.ToString() + "','" + CurrentOrder.CustomOrderNote + "','" +
                    CurrentOrder.CustomDeliveryNote + "','" + CurrentOrder.OrderState.ToString() + "';");
            
        }
    }
}
