using System;
using WHLClasses;

namespace ReorderWPF.Pages
{
    using System.Globalization;

    public class DataItemDetails
    {
        public string Sku { get; set; }

        public int Sales { get; set; }

        public string Retail { get; set; }

        public int Packsize { get; set; }

        public double WeeksLeft { get; set; }

        public string Profit { get; set; }

        public string Margin { get; set; }


        public static DataItemDetails NewDataItemDetails(WhlSKU sku)
        {
            if (sku.SKU.Contains("xxxx"))
            {
                return null;
            }
            var item = new DataItemDetails();
            item.Sku = sku.SKU;
            item.Sales = Convert.ToInt32(sku.SalesData.WeightedAverage);
            item.Packsize = sku.PackSize;
            item.Retail = sku.Price.Retail.ToString("C", CultureInfo.CreateSpecificCulture("en-GB"));
            item.Profit = Math.Round(sku.Price.Profit,2).ToString("C", CultureInfo.CreateSpecificCulture("en-GB"));
            item.Margin = (sku.Price.Profit / sku.Price.Retail).ToString("P",CultureInfo.InvariantCulture);
            if (item.Sales != 0) item.WeeksLeft = Math.Round(Convert.ToDouble(sku.Stock.Level / item.Sales), 1);
            else item.WeeksLeft = 999;
            return item;
        }
    }
}
