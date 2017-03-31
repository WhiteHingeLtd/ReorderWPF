using System;
using System.Collections.Generic;
using System.ComponentModel;
using WHLClasses;
using System.Windows.Controls;
using System.Windows.Data;
using ReorderWPF.CustomControls;

namespace ReorderWPF.Pages
{
    /// <summary>
    /// Interaction logic for SupplierData.xaml
    /// </summary>
    public partial class SupplierData : ThreadedPage
    {
        private Supplier _currentSupplier;
        private SkuCollection _supplierSkuCollectionfull = new SkuCollection(true);
        private SkuCollection _supplierSkuCollection = new SkuCollection(true);
        private List<DataItem> SupplierDataList = new List<DataItem>();
        public SupplierData(MainWindow Main, Supplier SupplierCode)
        {
            InitializeComponent();
            SetMainWindowRef(Main);
            Title = SupplierCode.Name;
            _currentSupplier = SupplierCode;
            _supplierSkuCollectionfull = Main.DataSkus;
            _supplierSkuCollection = Main.DataSkusMixDown;
            Misc.OperationDialog("Preparing " + SupplierCode.Name,PrepareDataGrid);
            RenderDataGrid();
        }
        internal override void TabClosing(ref bool cancel)
        {
            cancel = false;

        }

        internal void PrepareDataGrid(object sender, DoWorkEventArgs e)
        {
            
            foreach (WhlSKU sku in _supplierSkuCollection.SearchBySuppName(_currentSupplier.Code))
            {
             DataItem refGridRow = new DataItem();
                refGridRow.Sku = sku.ShortSku;
                refGridRow.ItemName = sku.Title.Invoice;
                try
                {
                    refGridRow.AverageSales = Int32.Parse(sku.SalesData.EightWeekAverage.ToString());
                }
                catch (Exception)
                {
                    refGridRow.AverageSales = 0;
                }
                
                foreach (SKUSupplier supp in sku.Suppliers)
                {
                    if (!supp.Primary) continue;
                    refGridRow.SupplierCode = supp.ReOrderCode;
                }


            SupplierDataList.Add(refGridRow);
            }
            
        }

        private void RenderDataGrid()
        {
            DataGridTextColumn SkuColumn = new DataGridTextColumn();
            SkuColumn.Header = "SKU";
            SkuColumn.Binding = new Binding("Sku");
            SupplierDataGrid.Columns.Add(SkuColumn);
            DataGridTextColumn ItemNameColumn = new DataGridTextColumn();
            ItemNameColumn.Header = "Item Name";
            ItemNameColumn.Binding = new Binding("ItemName");
            SupplierDataGrid.Columns.Add(ItemNameColumn);
            DataGridTextColumn WeeksRemainingColumn = new DataGridTextColumn();
            WeeksRemainingColumn.Header = "Weeks Remaining";
            WeeksRemainingColumn.Binding = new Binding("WeeksRemaining");
            SupplierDataGrid.Columns.Add(WeeksRemainingColumn);
            DataGridTextColumn SupplierCodeColumn = new DataGridTextColumn();
            SupplierCodeColumn.Header = "Supplier Code";
            SupplierCodeColumn.Binding = new Binding("SupplierCode");
            SupplierDataGrid.Columns.Add(SupplierCodeColumn);
            DataGridTextColumn AverageSalesColumn = new DataGridTextColumn();
            AverageSalesColumn.Header = "Avg Sales";
            AverageSalesColumn.Binding = new Binding("AverageSales");
            SupplierDataGrid.Columns.Add(AverageSalesColumn);
            DataGridTextColumn StockLevelColumn = new DataGridTextColumn();
            StockLevelColumn.Header = "Stock";
            StockLevelColumn.Binding = new Binding("StockLevel");
            SupplierDataGrid.Columns.Add(StockLevelColumn);
            DataGridTextColumn RecommendedSalesColumn = new DataGridTextColumn();
            RecommendedSalesColumn.Header = "Rec.";
            RecommendedSalesColumn.Binding = new Binding("RecommendedToOrder");
            SupplierDataGrid.Columns.Add(RecommendedSalesColumn);
            DataGridTextColumn InnerSalesColumn = new DataGridTextColumn();
            InnerSalesColumn.Header = "Inner";
            InnerSalesColumn.Binding = new Binding("InnerCarton");
            SupplierDataGrid.Columns.Add(InnerSalesColumn);
            DataGridTextColumn OuterSalesColumn = new DataGridTextColumn();
            OuterSalesColumn.Header = "Outer";
            OuterSalesColumn.Binding = new Binding("OuterCarton");
            SupplierDataGrid.Columns.Add(OuterSalesColumn);
            DataGridTextColumn NumOnOrderSalesColumn = new DataGridTextColumn();
            NumOnOrderSalesColumn.Header = "On Order";
            NumOnOrderSalesColumn.Binding = new Binding("NumOnOrder");
            SupplierDataGrid.Columns.Add(NumOnOrderSalesColumn);
            DataGridTextColumn NetOrderPriceSalesColumn = new DataGridTextColumn();
            NetOrderPriceSalesColumn.Header = "Net Price";
            NetOrderPriceSalesColumn.Binding = new Binding("NetOrderPrice");
            SupplierDataGrid.Columns.Add(NetOrderPriceSalesColumn);

            foreach (DataItem Item in SupplierDataList)
            {
                SupplierDataGrid.Items.Add(Item);
            }


        }
    }

    public class DataItem
    {
        public string Sku { get; set; }
        public string ItemName { get; set; }
        public Single WeeksRemaining { get; set; }
        public string SupplierCode { get; set; }
        public int AverageSales { get; set; }
        public int StockLevel { get; set; }
        public int RecommendedToOrder { get; set; }
        public int InnerCarton { get; set; }
        public int OuterCarton { get; set; }
        public int NumOnOrder { get; set; }
        public Single NetOrderPrice { get; set; }
    

    }

}
