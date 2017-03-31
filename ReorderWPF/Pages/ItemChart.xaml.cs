using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;
using ReorderWPF.CustomControls;
using WHLClasses;
using DateTimeAxis = OxyPlot.Wpf.DateTimeAxis;
using LineSeries = OxyPlot.Series.LineSeries;

namespace ReorderWPF.Pages
{
    /// <summary>
    /// Interaction logic for ItemChart.xaml
    /// </summary>
    public partial class ItemChart : ThreadedPage
    {
        private SkuCollection skus = new SkuCollection(true);
        private WhlSKU _selectedSku = new WhlSKU();
        private PlotModel PlotGlobal = new PlotModel();
        private bool UseAreaForChart = false;
        public ItemChart(MainWindow Main, WhlSKU SelectedSku = null)
        {
            skus = Main.DataSkus;
            if (SelectedSku != null)
            {
                _selectedSku = SelectedSku;
            }
            
            SetMainWindowRef(Main);
            InitializeComponent();
            LoadChartData(SelectedSku);
        }

        internal override void TabClosing(ref bool cancel)
        {
            cancel = false;
            

        }

        private void LoadChartData(WhlSKU Sku)
        {
            if (Sku != null)
            {
                PlotModel PlotArea = new PlotModel();
                var endDate = DateTime.Now.ToOADate();
                var startDate = DateTime.Now.AddMonths(-6).ToOADate();
                var BottomAxis = new OxyPlot.Axes.DateTimeAxis();
                BottomAxis.Position = AxisPosition.Bottom;
                BottomAxis.Minimum = Convert.ToDouble(startDate);
                BottomAxis.Maximum = Convert.ToDouble(endDate);

                BottomAxis.AbsoluteMaximum = Convert.ToDouble(endDate);
                BottomAxis.Title = "Date";
                BottomAxis.StringFormat = "dd/M";
                BottomAxis.MinorIntervalType = DateTimeIntervalType.Days;

                var leftAxis = new OxyPlot.Axes.LinearAxis();
                leftAxis.Position = AxisPosition.Left;
                leftAxis.Minimum = 0;
                leftAxis.AbsoluteMinimum = 0;
                leftAxis.Maximum = 1000;
                leftAxis.Title = "Sales";
                var rightAxis = new OxyPlot.Axes.LinearAxis();
                rightAxis.Position = AxisPosition.Right;
                rightAxis.Minimum = 0;
                rightAxis.AbsoluteMinimum = 0;
                rightAxis.Maximum = 3000;
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
                    BottomAxis.AbsoluteMinimum = Convert.ToDouble(DateTime.Parse((QueryDict[0] as ArrayList)[1].ToString()).ToOADate());
                }
                catch (Exception)
                {
                    BottomAxis.AbsoluteMinimum = Convert.ToDouble(startDate);
                }
                #region It's fucked
                //ConcurrentBag<DataPoint> ConcurrentStockHistoryPoints = new ConcurrentBag<DataPoint>();
                //ConcurrentBag<DataPoint> ConcurrentSalesHistoryPoints = new ConcurrentBag<DataPoint>();
                //ConcurrentBag<DataPoint> ConcurrentStockHistoryPoints2 = new ConcurrentBag<DataPoint>();
                //ConcurrentBag<DataPoint> ConcurrentSalesHistoryPoints2 = new ConcurrentBag<DataPoint>();
                //Parallel.ForEach(QueryDict.Cast<ArrayList>(), (result) =>
                //    {
                //        var StockTotal =
                //        Convert.ToDouble(Int32.Parse(result[2].ToString()) + Int32.Parse(result[3].ToString()));
                //        Double SalesTotal;
                //        try
                //        {
                //            if (MaxStock < Int32.Parse(result[2].ToString()) + Int32.Parse(result[3].ToString()))
                //                MaxStock = Int32.Parse(result[2].ToString()) + Int32.Parse(result[3].ToString());
                //            if (DBNull.Value != result[4])
                //            {
                //                if (MaxSales < Int32.Parse(result[4].ToString())) MaxSales = Int32.Parse(result[4].ToString());
                //            }

                //        }
                //        catch (Exception)
                //        {

                //        }
                //        try
                //        {
                //            SalesTotal = Convert.ToDouble(Int32.Parse(result[4].ToString()));
                //        }
                //        catch (Exception)
                //        {
                //            Console.WriteLine();
                //            SalesTotal = Convert.ToDouble(0);
                //        }
                //        var Date = Convert.ToDouble(DateTime.Parse(result[1].ToString()).ToOADate());
                //        var StockHistoryPoint = new DataPoint(Date, StockTotal);
                //        var SaleHistoryPoint = new DataPoint(Date, SalesTotal);
                //        var StockHistoryPoint2 = new DataPoint(Date, 0);
                //        ConcurrentSalesHistoryPoints.Add(SaleHistoryPoint);
                //        ConcurrentStockHistoryPoints.Add(StockHistoryPoint);

                //        ConcurrentSalesHistoryPoints2.Add(StockHistoryPoint2);
                //        ConcurrentStockHistoryPoints2.Add(StockHistoryPoint2);
                //    }
                //);
                //SalesHistoryPoints.AddRange(ConcurrentSalesHistoryPoints);
                //SalesHistoryPoints2.AddRange(ConcurrentSalesHistoryPoints2);
                //StockHistoryPoints.AddRange(ConcurrentStockHistoryPoints);
                //StockHistoryPoints2.AddRange(ConcurrentStockHistoryPoints2);
                #endregion
                foreach (ArrayList Result in QueryDict)
                {
                    var StockTotal =
                        Convert.ToDouble(Int32.Parse(Result[2].ToString()) + Int32.Parse(Result[3].ToString()));
                    Double SalesTotal;
                    try
                    {
                        if (MaxStock < Int32.Parse(Result[2].ToString()) + Int32.Parse(Result[3].ToString()))
                            MaxStock = Int32.Parse(Result[2].ToString()) + Int32.Parse(Result[3].ToString());
                        if (DBNull.Value != Result[4])
                        {
                            if (MaxSales < Int32.Parse(Result[4].ToString())) MaxSales = Int32.Parse(Result[4].ToString());
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
                SalesSeries.Color = OxyColor.FromRgb(237,125,49);
                SalesSeries.Title = "Sales History";                
                StockSeries.YAxisKey = rightAxis.Key;
                StockSeries.CanTrackerInterpolatePoints = false;
                if (UseAreaForChart)
                {
                    StockAreaSeries.Points.AddRange(StockHistoryPoints);
                    StockAreaSeries.YAxisKey = rightAxis.Key;
                    StockAreaSeries.CanTrackerInterpolatePoints = false;
                    StockAreaSeries.Fill = OxyColor.FromRgb(176,195,230); 
                    StockAreaSeries.Color = OxyColor.FromRgb(138, 167, 218);
                    StockAreaSeries.Color2 = OxyColor.FromRgb(138, 167, 218);
                    StockAreaSeries.Points2.AddRange(StockHistoryPoints2);
                    //StockAreaSeries.ConstantY2 = 0;
                    StockAreaSeries.Title = "Stock History Area";

                    SalesAreaSeries.Points.AddRange(SalesHistoryPoints);
                    SalesAreaSeries.CanTrackerInterpolatePoints = false;
                    SalesAreaSeries.Fill = OxyColor.FromArgb(140,237, 125, 49);
                    SalesAreaSeries.Color = OxyColor.FromArgb(255,138, 167, 218);
                    SalesAreaSeries.Color2 = OxyColor.FromRgb(138, 167, 218);
                    SalesAreaSeries.Points2.AddRange(StockHistoryPoints2);
                    //StockAreaSeries.ConstantY2 = 0;
                    SalesAreaSeries.Title = "Sales History Area";
                    
                    
                    PlotArea.Series.Add(StockAreaSeries);
                    PlotArea.Series.Add(SalesAreaSeries);
                }
                else
                {
                    StockSeries.Color = OxyColors.DarkGreen;
                    StockSeries.Title = "Stock History";
                    PlotArea.Series.Add(StockSeries);

                    PlotArea.Series.Add(SalesSeries);

                }


                leftAxis.AbsoluteMaximum = MaxSales;
                rightAxis.AbsoluteMaximum = MaxStock;
                PlotArea.Axes.Add(BottomAxis);
                PlotArea.Axes.Add(leftAxis);
                PlotArea.Axes.Add(rightAxis);
                if (MaxSales == 0)
                {
                    leftAxis.AbsoluteMaximum = 1;
                    rightAxis.AbsoluteMaximum += 10;
                    leftAxis.Title = "No sales";
                }
                
                

                
                //MessageBox.Show(QueryDict.Count.ToString());
                PlotArea.Title = Sku.ShortSku + " Sales/Stock History";
                PlotGlobal = PlotArea;
            }
        }

        private void LoadGraphButton_Click(object sender, RoutedEventArgs e)
        {
            if (SkuBox.Text.Length == 7 || SkuBox.Text.Length == 11)
            {
                var SkuText = SkuBox.Text.Substring(0, 7) + "0001";
                _selectedSku = skus.SearchSKUSReturningSingleSku(SkuText);
                ItemTitle.Text = _selectedSku.Title.Invoice;
                this.Title = _selectedSku.ShortSku;
                if (UseAreaCheck.IsChecked == true) UseAreaForChart = true;
                else UseAreaForChart = false;
                Misc.OperationDialog("Loading Chart", LoadChartWorker);
                PlotPlot.Model = PlotGlobal;
                SkuBox.Text = "";
            }
        }
        private void LoadChartWorker (object sender, DoWorkEventArgs e)
        {
            LoadChartData(_selectedSku);
        }

        private void SkuBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoadGraphButton_Click(null,null);
            }
        }
    }
}
