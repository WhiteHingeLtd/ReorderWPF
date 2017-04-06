using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WHLClasses;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using OxyPlot;
using OxyPlot.Axes;
using ReorderWPF.CustomControls;
using OxyPlot.Wpf;
using LineSeries = OxyPlot.Series.LineSeries;

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

        private ConcurrentBag<WhlSKU> ListOfUnloadedSkus = new ConcurrentBag<WhlSKU>();
        internal ObservableCollection<DataItemDetails> CurrentSelectedPacksizes = new ObservableCollection<DataItemDetails>();

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
            LoadSupplierData();

            SupplierNameBlock.Text = SupplierCode.Name;
            Stopwatch asd = new Stopwatch();
            asd.Start();
            Misc.OperationDialog("Preparing " + SupplierCode.Name,PrepareDataGrid);
            asd.Stop();
            Console.WriteLine(asd.ElapsedMilliseconds.ToString());
            RenderDataGrid();
        }

        private void LoadSupplierData()
        {
            
        }

        internal override void TabClosing(ref bool cancel)
        {
            cancel = false;

        }

        internal void PrepareDataGrid(object sender, DoWorkEventArgs e)
        {
            SupplierDataList.Clear();
            var Worker = sender as BackgroundWorker;
            var CurrentColl = _supplierSkuCollection.SearchBySuppName(_currentSupplier.Code);
            var SupplierDataBag = new ConcurrentBag<DataItem>();
            Parallel.ForEach(CurrentColl, (sku) =>
            {
                if (Int32.Parse(sku.SalesData.EightWeekAverage.ToString()) == 0 && LoadNoSales != true)
                {
                    ListOfUnloadedSkus.Add(sku);
                    return;
                }
                
                
                
                var newItem = DataItem.DataItemNew(sku);
                
                SupplierDataBag.Add(newItem);
                Worker.ReportProgress(SupplierDataBag.Count / CurrentColl.Count);
            });
            SupplierDataList.AddRange(SupplierDataBag);
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
        public static PlotModel LoadChartData(WhlSKU Sku)
        {
            if (Sku != null)
            {

                PlotModel PlotArea = new PlotModel();
                var endDate = DateTime.Now.ToOADate();
                var startDate = DateTime.Now.AddMonths(-6).ToOADate();
                var BottomAxis = new OxyPlot.Axes.DateTimeAxis();
                BottomAxis.Position = AxisPosition.Bottom;
                BottomAxis.Maximum = Convert.ToDouble(endDate);

                BottomAxis.AbsoluteMaximum = Convert.ToDouble(endDate);
                BottomAxis.Title = "Date";
                BottomAxis.StringFormat = "dd/M";
                BottomAxis.MinorIntervalType = DateTimeIntervalType.Days;

                var leftAxis = new OxyPlot.Axes.LinearAxis();
                leftAxis.Position = AxisPosition.Left;
                leftAxis.Minimum = 0;
                leftAxis.AbsoluteMinimum = 0;
                leftAxis.Title = "Sales";
                var rightAxis = new OxyPlot.Axes.LinearAxis();
                rightAxis.Position = AxisPosition.Right;
                rightAxis.Minimum = 0;
                rightAxis.AbsoluteMinimum = 0;
                rightAxis.Maximum = 5000;
                rightAxis.Title = "Stock";

                var query = @"SELECT a.shortSku, a.stockDate, a.Stocklevel, a.StockMinimum, b.maintotal 
                          FROM whldata.stock_history as a
                            LEFT JOIN(SELECT /*top (999999999999)*/ a.orderdate, a.shortsku, sum(a.total)as ""maintotal"" FROM
                            (SELECT /*top (999999999999)*/ orderdate, sku, SUBSTRING(sku, 0, 8) as ""shortsku"", sum(salequantity) as ""sales"", CAST(SUBSTRING(sku, 8, 4) as unsigned int) as ""packsize"", sum(salequantity * CAST(SUBSTRING(sku, 8, 4) as unsigned int)) as 'total'
                             FROM whldata.newsales_raw
                             WHERE sku LIKE '" + Sku.ShortSku + @"%'
                             group by sku, orderDate
                             order by orderdate) as a
                            GROUP BY orderdate, shortsku
                            ORDER BY orderDate) as b
                            on b.shortsku = SUBSTRING(a.shortSku, 0, 8) AND b.orderDate = a.stockDate
                            WHERE a.shortsku = '" + Sku.SKU + @"'
                            ORDER BY StockDate ASC";
                var QueryDict = MySQL.SelectData(query) as ArrayList;
                List<DataPoint> StockHistoryPoints = new List<DataPoint>();
                List<DataPoint> SalesHistoryPoints = new List<DataPoint>();
                List<DataPoint> StockHistoryPoints2 = new List<DataPoint>();
                List<DataPoint> SalesHistoryPoints2 = new List<DataPoint>();


                LineSeries SalesSeries = new LineSeries();
                LineSeries StockSeries = new LineSeries();

                OxyPlot.Series.AreaSeries StockAreaSeries = new OxyPlot.Series.AreaSeries();
                OxyPlot.Series.AreaSeries SalesAreaSeries = new OxyPlot.Series.AreaSeries();
                var MaxStock = 0;
                var MaxSales = 0;
                try
                {
                    BottomAxis.AbsoluteMinimum =
                        Convert.ToDouble(DateTime.Parse((QueryDict[0] as ArrayList)[1].ToString()).ToOADate());
                    BottomAxis.Minimum = Convert.ToDouble(DateTime.Parse((QueryDict[0] as ArrayList)[1].ToString())
                        .ToOADate());
                }
                catch (Exception)
                {
                    BottomAxis.AbsoluteMinimum = Convert.ToDouble(startDate);
                    BottomAxis.Minimum = Convert.ToDouble(startDate);
                }

                foreach (ArrayList Result in QueryDict)
                {
                    Double StockTotal;
                    StockTotal = Convert.ToDouble(Int32.Parse(Result[2].ToString()));


                    Double SalesTotal;
                    try
                    {
                        if (MaxStock < Int32.Parse(Result[2].ToString()) + Int32.Parse(Result[3].ToString()))
                            MaxStock = Int32.Parse(Result[2].ToString()) + Int32.Parse(Result[3].ToString());
                        if (DBNull.Value != Result[4])
                        {
                            if (MaxSales < Int32.Parse(Result[4].ToString()))
                                MaxSales = Int32.Parse(Result[4].ToString());
                        }

                    }
                    catch (Exception)
                    {

                    }
                    try
                    {
                        SalesTotal = Convert.ToDouble(Int32.Parse(Result[4].ToString()));
                    }
                    catch (Exception)
                    {
                        Console.WriteLine();
                        SalesTotal = Convert.ToDouble(0);
                    }
                    var Date = Convert.ToDouble(DateTime.Parse(Result[1].ToString()).ToOADate());
                    var StockHistoryPoint = new DataPoint(Date, StockTotal);
                    var SaleHistoryPoint = new DataPoint(Date, SalesTotal);
                    var StockHistoryPoint2 = new DataPoint(Date, 0);
                    SalesHistoryPoints.Add(SaleHistoryPoint);
                    StockHistoryPoints.Add(StockHistoryPoint);

                    SalesHistoryPoints2.Add(StockHistoryPoint2);
                    StockHistoryPoints2.Add(StockHistoryPoint2);
                }

                SalesSeries.Points.AddRange(SalesHistoryPoints);
                StockSeries.Points.AddRange(StockHistoryPoints);


                rightAxis.Key = "StockKey";
                SalesSeries.YAxisKey = leftAxis.Key;
                SalesSeries.CanTrackerInterpolatePoints = false;
                SalesSeries.Color = OxyColor.FromRgb(237, 125, 49);
                SalesSeries.Title = "Sales History";
                StockSeries.YAxisKey = rightAxis.Key;
                StockSeries.CanTrackerInterpolatePoints = false;

                StockAreaSeries.Points.AddRange(StockHistoryPoints);
                StockAreaSeries.YAxisKey = rightAxis.Key;
                StockAreaSeries.CanTrackerInterpolatePoints = false;
                StockAreaSeries.Fill = OxyColor.FromRgb(176, 195, 230);
                StockAreaSeries.Color = OxyColor.FromRgb(138, 167, 218);
                StockAreaSeries.Color2 = OxyColor.FromRgb(138, 167, 218);
                StockAreaSeries.Points2.AddRange(StockHistoryPoints2);
                //StockAreaSeries.ConstantY2 = 0;
                StockAreaSeries.Title = "Stock History Area";

                SalesAreaSeries.Points.AddRange(SalesHistoryPoints);
                SalesAreaSeries.CanTrackerInterpolatePoints = false;
                SalesAreaSeries.Fill = OxyColor.FromArgb(140, 237, 125, 49);
                SalesAreaSeries.Color = OxyColor.FromArgb(255, 138, 167, 218);
                SalesAreaSeries.Color2 = OxyColor.FromRgb(138, 167, 218);
                SalesAreaSeries.Points2.AddRange(StockHistoryPoints2);
                //StockAreaSeries.ConstantY2 = 0;
                SalesAreaSeries.Title = "Sales History Area";


                PlotArea.Series.Add(StockAreaSeries);
                PlotArea.Series.Add(SalesAreaSeries);


                if (MaxSales == 0)
                {
                    leftAxis.AbsoluteMaximum = 1;
                    rightAxis.AbsoluteMaximum += 10;
                    leftAxis.Title = "No sales";
                }
                if (MaxSales > 0)
                {
                    leftAxis.AbsoluteMaximum = MaxSales * 1.15;
                    leftAxis.Maximum = MaxSales * 1.1;
                    rightAxis.Maximum = MaxStock * 1.1;
                    rightAxis.AbsoluteMaximum = MaxStock * 1.15;
                }
                leftAxis.IsZoomEnabled = false;
                leftAxis.AbsoluteMaximum = MaxSales;
                rightAxis.AbsoluteMaximum = MaxStock;
                PlotArea.Axes.Add(BottomAxis);
                PlotArea.Axes.Add(leftAxis);
                PlotArea.Axes.Add(rightAxis);

                PlotArea.Title = Sku.ShortSku + " Sales/Stock History";

                return PlotArea;
            }
            else return null;
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
                PlotView Model1 = FindVisualChild<PlotView>(asd);
                try
                {
                    Model1.Model = (e.Row.Item as DataItem).SalesGraph;
                }
                catch (Exception exception)
                {
                    Model1.Visibility = Visibility.Collapsed;
                    Console.WriteLine(exception);
                }
                
                CurrentSelectedPacksizes.Clear();
                foreach (DataItemDetails Pack in ((e.Row.Item) as DataItem).Packsizes)
                {
                    CurrentSelectedPacksizes.Add(Pack);
                }



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
        public float NetOrderPrice { get; set; }
        public WhlSKU SkuData { get; set; }
        public PlotModel SalesGraph { get; set; }

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
            NewItem.SalesGraph = LoadChartData(sku);
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
        public static PlotModel LoadChartData(WhlSKU Sku)
        {
            if (Sku != null)
            {

                PlotModel PlotArea = new PlotModel();
                var endDate = DateTime.Now.ToOADate();
                var startDate = DateTime.Now.AddMonths(-6).ToOADate();
                var BottomAxis = new OxyPlot.Axes.DateTimeAxis();
                BottomAxis.Position = AxisPosition.Bottom;
                BottomAxis.Maximum = Convert.ToDouble(endDate);

                BottomAxis.AbsoluteMaximum = Convert.ToDouble(endDate);
                BottomAxis.Title = "Date";
                BottomAxis.StringFormat = "dd/M";
                BottomAxis.MinorIntervalType = DateTimeIntervalType.Days;

                var leftAxis = new OxyPlot.Axes.LinearAxis();
                leftAxis.Position = AxisPosition.Left;
                leftAxis.Minimum = 0;
                leftAxis.AbsoluteMinimum = 0;
                leftAxis.Title = "Sales";
                var rightAxis = new OxyPlot.Axes.LinearAxis();
                rightAxis.Position = AxisPosition.Right;
                rightAxis.Minimum = 0;
                rightAxis.AbsoluteMinimum = 0;
                rightAxis.Maximum = 5000;
                rightAxis.Title = "Stock";

                var query = @"SELECT a.shortSku, a.stockDate, a.Stocklevel, a.StockMinimum, b.maintotal 
                          FROM whldata.stock_history as a
                            LEFT JOIN(SELECT top (999999999999) a.orderdate, a.shortsku, sum(a.total)as ""maintotal"" FROM
                            (SELECT top (999999999999) orderdate, sku, SUBSTRING(sku, 0, 8) as ""shortsku"", sum(salequantity) as ""sales"", CAST(SUBSTRING(sku, 8, 4) as /*unsigned*/ int) as ""packsize"", sum(salequantity * CAST(SUBSTRING(sku, 8, 4) as /*unsigned*/ int)) as 'total'
                             FROM whldata.newsales_raw
                             WHERE sku LIKE '" + Sku.ShortSku + @"%'
                             group by sku, orderDate
                             order by orderdate) as a
                            GROUP BY orderdate, shortsku
                            ORDER BY orderDate) as b
                            on b.shortsku = SUBSTRING(a.shortSku, 0, 8) AND b.orderDate = a.stockDate
                            WHERE a.shortsku = '" + Sku.SKU + @"'
                            ORDER BY StockDate ASC";
                var QueryDict = MSSQLPublic.SelectData(query) as ArrayList;
                List<DataPoint> StockHistoryPoints = new List<DataPoint>();
                List<DataPoint> SalesHistoryPoints = new List<DataPoint>();
                List<DataPoint> StockHistoryPoints2 = new List<DataPoint>();
                List<DataPoint> SalesHistoryPoints2 = new List<DataPoint>();


                LineSeries SalesSeries = new LineSeries();
                LineSeries StockSeries = new LineSeries();

                OxyPlot.Series.AreaSeries StockAreaSeries = new OxyPlot.Series.AreaSeries();
                OxyPlot.Series.AreaSeries SalesAreaSeries = new OxyPlot.Series.AreaSeries();
                var MaxStock = 0;
                var MaxSales = 0;
                try
                {
                    BottomAxis.AbsoluteMinimum =
                        Convert.ToDouble(DateTime.Parse((QueryDict[0] as ArrayList)[1].ToString()).ToOADate());
                    BottomAxis.Minimum = Convert.ToDouble(DateTime.Parse((QueryDict[0] as ArrayList)[1].ToString())
                        .ToOADate());
                }
                catch (Exception)
                {
                    BottomAxis.AbsoluteMinimum = Convert.ToDouble(startDate);
                    BottomAxis.Minimum = Convert.ToDouble(startDate);
                }

                foreach (ArrayList Result in QueryDict)
                {
                    Double StockTotal;
                    StockTotal = Convert.ToDouble(Int32.Parse(Result[2].ToString()));


                    Double SalesTotal;
                    try
                    {
                        if (MaxStock < Int32.Parse(Result[2].ToString()) + Int32.Parse(Result[3].ToString()))
                            MaxStock = Int32.Parse(Result[2].ToString()) + Int32.Parse(Result[3].ToString());
                        if (DBNull.Value != Result[4])
                        {
                            if (MaxSales < Int32.Parse(Result[4].ToString()))
                                MaxSales = Int32.Parse(Result[4].ToString());
                        }

                    }
                    catch (Exception)
                    {

                    }
                    if (Result[4] == System.DBNull.Value) SalesTotal = Convert.ToDouble(0);
                    else SalesTotal = Convert.ToDouble(Int32.Parse(Result[4].ToString()));

                    
                    var Date = Convert.ToDouble(DateTime.Parse(Result[1].ToString()).ToOADate());
                    var StockHistoryPoint = new DataPoint(Date, StockTotal);
                    var SaleHistoryPoint = new DataPoint(Date, SalesTotal);
                    var StockHistoryPoint2 = new DataPoint(Date, 0);
                    SalesHistoryPoints.Add(SaleHistoryPoint);
                    StockHistoryPoints.Add(StockHistoryPoint);

                    SalesHistoryPoints2.Add(StockHistoryPoint2);
                    StockHistoryPoints2.Add(StockHistoryPoint2);
                }

                SalesSeries.Points.AddRange(SalesHistoryPoints);
                StockSeries.Points.AddRange(StockHistoryPoints);


                rightAxis.Key = "StockKey";
                SalesSeries.YAxisKey = leftAxis.Key;
                SalesSeries.CanTrackerInterpolatePoints = false;
                SalesSeries.Color = OxyColor.FromRgb(237, 125, 49);
                SalesSeries.Title = "Sales History";
                StockSeries.YAxisKey = rightAxis.Key;
                StockSeries.CanTrackerInterpolatePoints = false;

                StockAreaSeries.Points.AddRange(StockHistoryPoints);
                StockAreaSeries.YAxisKey = rightAxis.Key;
                StockAreaSeries.CanTrackerInterpolatePoints = false;
                StockAreaSeries.Fill = OxyColor.FromRgb(176, 195, 230);
                StockAreaSeries.Color = OxyColor.FromRgb(138, 167, 218);
                StockAreaSeries.Color2 = OxyColor.FromRgb(138, 167, 218);
                StockAreaSeries.Points2.AddRange(StockHistoryPoints2);
                //StockAreaSeries.ConstantY2 = 0;
                StockAreaSeries.Title = "Stock History Area";

                SalesAreaSeries.Points.AddRange(SalesHistoryPoints);
                SalesAreaSeries.CanTrackerInterpolatePoints = false;
                SalesAreaSeries.Fill = OxyColor.FromArgb(140, 237, 125, 49);
                SalesAreaSeries.Color = OxyColor.FromArgb(255, 138, 167, 218);
                SalesAreaSeries.Color2 = OxyColor.FromRgb(138, 167, 218);
                SalesAreaSeries.Points2.AddRange(StockHistoryPoints2);
                //StockAreaSeries.ConstantY2 = 0;
                SalesAreaSeries.Title = "Sales History Area";


                PlotArea.Series.Add(StockAreaSeries);
                PlotArea.Series.Add(SalesAreaSeries);


                if (MaxSales == 0)
                {
                    leftAxis.AbsoluteMaximum = 1;
                    rightAxis.AbsoluteMaximum += 10;
                    leftAxis.Title = "No sales";
                }
                if (MaxSales > 0)
                {
                    leftAxis.AbsoluteMaximum = MaxSales * 1.15;
                    leftAxis.Maximum = MaxSales * 1.1;
                    rightAxis.Maximum = MaxStock * 1.1;
                    rightAxis.AbsoluteMaximum = MaxStock * 1.15;
                }
                leftAxis.IsZoomEnabled = false;
                leftAxis.AbsoluteMaximum = MaxSales;
                rightAxis.AbsoluteMaximum = MaxStock;
                PlotArea.Axes.Add(BottomAxis);
                PlotArea.Axes.Add(leftAxis);
                PlotArea.Axes.Add(rightAxis);

                PlotArea.Title = Sku.ShortSku + " Sales/Stock History";

                return PlotArea;
            }
            else return null;
        }

    }

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
            Item.Sales = Convert.ToInt32(sku.SalesData.EightWeekAverage);
            Item.Packsize = sku.PackSize.ToString();
            if (Item.Sales != 0) Item.WeeksLeft = Math.Round(Convert.ToDouble(sku.Stock.Level / Item.Sales), 1);
            else Item.WeeksLeft = 999;
            return Item;
        }
    }
}
