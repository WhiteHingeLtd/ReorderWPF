using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
        public static SkuCollection SupplierSkuCollectionFull = new SkuCollection(true);
        private SkuCollection _supplierSkuCollection = new SkuCollection(true);
        private List<DataItem> SupplierDataList = new List<DataItem>();
        private List<DataItemDetails> CurrentPacksizes = new List<DataItemDetails>();
        private SupplierOrderData CurrentSupplierOrder = new SupplierOrderData();

        private bool LoadLowStock = false;
        private bool LoadSupplierLow = false;
        private bool LoadDiscontinued = false;
        private bool LoadUnlisted = false;
        private bool LoadNoSales = false;
        public SupplierData(MainWindow Main, Supplier SupplierCode)
        {
            InitializeComponent();
            SetMainWindowRef(Main);
            Title = SupplierCode.Name;
            _currentSupplier = SupplierCode;
            SupplierSkuCollectionFull = Main.DataSkus;
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
            SupplierDataList.Clear();
            foreach (WhlSKU sku in _supplierSkuCollection.SearchBySuppName(_currentSupplier.Code))
            {
             DataItem refGridRow = new DataItem();
                foreach (SKUSupplier supp in sku.Suppliers)
                {
                    if (!supp.Primary) continue;
                    refGridRow.SupplierCode = supp.ReOrderCode;
                }
                refGridRow.Sku = sku.ShortSku;
                refGridRow.ItemName = sku.Title.Invoice;
                
                try
                {
                    refGridRow.AverageSales = Int32.Parse(sku.SalesData.EightWeekAverage.ToString());
                    if (LoadNoSales && Int32.Parse(sku.SalesData.EightWeekAverage.ToString()) == 0) continue;
                }
                catch (Exception)
                {
                    refGridRow.AverageSales = 0;
                }          
            SupplierDataList.Add(refGridRow);
            }
            
        }

        private void RenderDataGrid()
        {

            SupplierDataGrid.ItemsSource = SupplierDataList;
        }

        private void RefreshButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            UpdateBooleans();
            Misc.OperationDialog("Preparing " + _currentSupplier.Name, PrepareDataGrid);
            RenderDataGrid();
        }

        private void UpdateBooleans()
        {
            try
            {
                if (LowStockCheck.IsChecked == true) LoadLowStock = true;
                else LoadLowStock = false;
                if (SupplierLowCheck.IsChecked == true) LoadSupplierLow = true;
                else LoadSupplierLow = false;
                if (DiscontCheck.IsChecked == true) LoadDiscontinued = true;
                else LoadDiscontinued = false;
                if (UnlistedCheck.IsChecked == true) LoadUnlisted = true;
                else LoadUnlisted = false;
                if (NoSalesCheck.IsChecked == true)  LoadNoSales = true;
                else LoadNoSales = false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void UpdateCurrentOrderData()
        {
            SkusCurrentOrder.Text = "Skus: " + CurrentSupplierOrder.LinesOfStock.ToString();
            TotalItemsCurrentOrder.Text = "Items: " + CurrentSupplierOrder.SkuOrderList.Sum(v => v.Value);
        }

        private void AddToCurrentOrder(WhlSKU Item, int Quantity)
        {
            if (CurrentSupplierOrder.SkuOrderList.ContainsKey(Item))
            {
                CurrentSupplierOrder.SkuOrderList[Item] += Quantity;
            }
            else
            {
                CurrentSupplierOrder.SkuOrderList.Add(Item, Quantity);
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
        public WhlSKU SkuData { get; set; }

        public SkuCollection Children => SupplierData.SupplierSkuCollectionFull.GatherChildren(SkuData.ShortSku);

        public List<DataItemDetails> Packsizes
        {
            get
            {
                var PacksizeList = new List<DataItemDetails>();
                foreach (WhlSKU Pack in this.Children)
                {
                    var newitem = new DataItemDetails(Pack);
                    PacksizeList.Add(newitem);
                }
                return PacksizeList;
            }
            
        }

    }

    public class DataItemDetails
    {
        public string ShortSku { get; set; }
        public string Packsize { get; set; }
        public Single WeeksLeft { get; set; }

        public DataItemDetails(WhlSKU item)
        {
            
        }
    }
}
