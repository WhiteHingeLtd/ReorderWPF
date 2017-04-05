using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using WHLClasses;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
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
        private DataItem CurrentSelectedItem = new DataItem();
        internal List<DataItemDetails> CurrentSelectedPacksizes = new List<DataItemDetails>();

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
                var newItem = DataItem.DataItemNew(sku);
            SupplierDataList.Add(newItem);
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

        private void SupplierDataGrid_RowDetailsVisibilityChanged(object sender, DataGridRowDetailsEventArgs e)
        {
            if (CurrentSelectedItem != e.Row.Item)
            {
                Grid asd = e.DetailsElement as Grid;
                DataGrid asd1 = FindVisualChild<DataGrid>(asd);
                asd1.DataContext = CurrentSelectedPacksizes;
                CurrentSelectedPacksizes = (e.Row.Item as DataItem).Packsizes;
                CurrentSelectedItem = e.Row.Item as DataItem;
            }

        }
        public static childItem FindVisualChild<childItem>(DependencyObject obj)
            where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }
    }

    public class DataItem
    {
        public string Sku { get; set; }
        public string ItemName { get; set; }
        public double WeeksRemaining { get; set; }
        public string SupplierCode { get; set; }
        public int AverageSales { get; set; }
        public int StockLevel { get; set; }
        public int RecommendedToOrder { get; set; }
        public int InnerCarton { get; set; }
        public int OuterCarton { get; set; }
        public int NumOnOrder { get; set; }
        public Single NetOrderPrice { get; set; }
        public WhlSKU SkuData { get; set; }

        public string Locations
        {
            get
            {
                var returnstring = "Locations :";
                foreach(SKULocation loc in SkuData.Locations)
                {
                    returnstring += loc.LocationText + ", ";
                }
                returnstring = returnstring.TrimEnd();
                returnstring = returnstring.Remove(returnstring.Length - 1);
                return returnstring;
            }
            
        }

        public SkuCollection Children => SupplierData.SupplierSkuCollectionFull.GatherChildren(SkuData.ShortSku);

        public static DataItem DataItemNew(WhlSKU sku)
        {
            var NewItem = new DataItem();
            NewItem.ItemName = sku.Title.Invoice;
            NewItem.Sku = sku.ShortSku;
            NewItem.SkuData = sku;
            NewItem.StockLevel = sku.Stock.Level;
            
            try
            {
                NewItem.AverageSales = Int32.Parse(sku.SalesData.EightWeekAverage.ToString());               
            }
            catch (Exception)
            {
                NewItem.AverageSales = 0;
            }
            if (NewItem.AverageSales != 0)
            {
                NewItem.WeeksRemaining = Math.Round(Convert.ToDouble(NewItem.StockLevel / NewItem.AverageSales),1);
            }
            else
            {
                NewItem.WeeksRemaining = 999;
            }
           
            foreach (SKUSupplier supp in sku.Suppliers)
            {
                if (!supp.Primary) continue;
                NewItem.SupplierCode = supp.ReOrderCode;
            }

            return NewItem;
        }

        public List<DataItemDetails> Packsizes
        {
            get
            {
                var PacksizeList = new List<DataItemDetails>();
                foreach (WhlSKU Pack in this.Children)
                {
                    DataItemDetails newitem = DataItemDetails.NewDataItemDetails(Pack);
                    PacksizeList.Add(newitem);
                }
                return PacksizeList;
            }
            
        }

    }

    public class DataItemDetails
    {
        public string ShortSku { get; set; }
        public int Sales { get; set; }
        public Single Retail { get; set; }
        public string Packsize { get; set; }
        public double WeeksLeft { get; set; }

        public static DataItemDetails NewDataItemDetails(WhlSKU sku)
        {
            var Item = new DataItemDetails();
            Item.ShortSku = sku.ShortSku;
            Item.Sales = Convert.ToInt32(sku.SalesData.EightWeekAverage);
            Item.Packsize = sku.PackSize.ToString();
            if (Item.Sales != 0) Item.WeeksLeft = Math.Round(Convert.ToDouble(sku.Stock.Level / Item.Sales), 1);
            else Item.WeeksLeft = 999;
            return Item;
        }
    }
}
