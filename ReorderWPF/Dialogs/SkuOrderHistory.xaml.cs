using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using WHLClasses;

namespace ReorderWPF.Dialogs
{
    /// <summary>
    /// Interaction logic for SkuOrderHistory.xaml
    /// </summary>
    public partial class SkuOrderHistory : Window
    {
        public List<SkuOrderHistoryClass> CurrentOrderHistory = new List<SkuOrderHistoryClass>();
        public WhlSKU CurrentSku = null;
        public SkuOrderHistory(WhlSKU sku)
        {
            InitializeComponent();
            CurrentSku = sku;
            WHLClasses.Misc.OperationDialog("Loading", LoadData);

        }

        public void LoadData(object sender, DoWorkEventArgs e)
        {
            var query = MSSQLPublic.SelectDataDictionary("SELECT * from whldata.reorder_orderitems WHERE SKU like'" + CurrentSku.ShortSku + "%';");
            foreach (Dictionary<string, object> result in query)
            {
                var OrderHistory = SkuOrderHistoryClass.MakeNewClass(result);
                CurrentOrderHistory.Add(OrderHistory);
            }
        }
    }

    public class SkuOrderHistoryClass
    {
        public string ShortSku { get; set; }
        public DateTime OrderDate { get; set;}
        public string Supplier { get; set; }
        public int AmountOrder { get; set; }
        public int AmountRecieved { get; set; }
        public DateTime RecievedDate { get; set; }
        public double LeadDays { get; set; }

        public static SkuOrderHistoryClass MakeNewClass(Dictionary<string, object> result)
        {
            var leadDaysCalc = (DateTime.Parse(result["RecievedDate"].ToString()) - DateTime.Parse(result["OrderDate"].ToString())).TotalDays;
            var returnable = new SkuOrderHistoryClass
            {
                ShortSku = result["ShortSku"].ToString(),
                OrderDate = DateTime.Parse(result["OrderDate"].ToString()),
                Supplier = result["Supplier"].ToString(),
                AmountOrder = int.Parse(result["AmountOrdered"].ToString()),
                AmountRecieved = int.Parse(result["AmountRecieved"].ToString()),
                RecievedDate = DateTime.Parse(result["RecievedDate"].ToString()),
                LeadDays = leadDaysCalc
            };
            return returnable;
        }
    }
}
