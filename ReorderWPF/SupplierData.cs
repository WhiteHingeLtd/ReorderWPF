using System;
using WHLClasses;

namespace ReorderWPF
{
    public class SupplierData
    {
        public string SupplierName;
        public string SupplierCode;
        public int Lines;
        public int Discontinued;
        public int Oversold;
        public int LowStock;
        public Single LowPercentage;
        public DateTime LastOrder;
        public DateTime DateDue;
        public SkuCollection ListOfSkus;

        public SupplierData(string code,SkuCollection FullCollection)
        {
            SupplierCode = code;
            ListOfSkus = FullCollection.SearchBySuppName(code);
            Lines = ListOfSkus.Count;
            LowPercentage = LowStock / Lines;
        }
    }
}
