using System;
using WHLClasses;

namespace ReorderWPF.Pages
{
    public class DataItemDetails
    {
        public string ShortSku { get; set; }
        public int Sales { get; set; }
        public float Retail { get; set; }
        public string Packsize { get; set; }
        public double WeeksLeft { get; set; }

        public static DataItemDetails NewDataItemDetails(WhlSKU sku)
        {
            var Item = new DataItemDetails();
            Item.ShortSku = sku.ShortSku;
            Item.Sales = Convert.ToInt32(sku.SalesData.WeightedAverage);
            Item.Packsize = sku.PackSize.ToString();
            if (Item.Sales != 0) Item.WeeksLeft = Math.Round(Convert.ToDouble(sku.Stock.Level / Item.Sales), 1);
            else Item.WeeksLeft = 999;
            return Item;
        }
    }
}
