using System;
using System.Collections.Generic;
using WHLClasses;

namespace ReorderWPF
{
    public class ReorderSupplier
    {
        public string Code;
        public string FullName;
        public SkuCollection Children;
        public double MinimumOrder;
        public int LeadDays;
        public bool CartonDiscount;
        public double ReorderAtPercentage;
        public DateTime LastOrder;
        public Guid LastOrderGuid = Guid.Empty;

        public ReorderSupplier(Supplier CurrentSupplier, SkuCollection FullSkuCollection)
        {
            LastOrderGuid = Guid.Empty;
            var Query = SQLServer.MSSelectDataDictionary("SELECT TOP 1 * from whldata.reorder_supplierdata WHERE SupplierCode='" + CurrentSupplier.Code + "';");
            foreach (Dictionary<string, object> result in Query)
            {
                Code = result["SupplierCode"].ToString();
                FullName = result["SupplierFullName"].ToString();
                Children = FullSkuCollection.SearchBySuppName(Code);
                try
                {
                    MinimumOrder = Convert.ToDouble(result["MinimumOrder"]);
                }
                catch (NullReferenceException)
                {
                    MinimumOrder = 0;                   
                }
                int CheckForDiscount = Convert.ToInt32(result["CartonDiscount"]);
                if (CheckForDiscount == 1) CartonDiscount = true;
                else CartonDiscount = false;
                ReorderAtPercentage = Convert.ToDouble(result["ReorderPercentage"]);
                if (result["LastOrder"] != DBNull.Value) LastOrder = DateTime.Parse(result["LastOrder"].ToString());
                if (result["LastOrderGuid"] != DBNull.Value) LastOrderGuid = new Guid(result["LastOrderGuid"].ToString());
            }
        }
    }

}
