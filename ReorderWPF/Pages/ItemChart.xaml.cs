namespace ReorderWPF.Pages
{
    #region Usings

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Input;

    using OxyPlot;
    using OxyPlot.Axes;
    using OxyPlot.Series;

    using ReorderWPF.CustomControls;
    using ReorderWPF.Dialogs;

    using WHLClasses;

    #endregion

    /// <summary>
    ///     Interaction logic for ItemChart.xaml
    ///     This is just fucked formatting wise. Don't touch it
    /// </summary>
    public partial class ItemChart : ThreadedPage
    {
        private SkuCollection skus = new SkuCollection();

        private SkuCollection mixdownCollection = new SkuCollection();

        private WhlSKU globalSelectedSku = new WhlSKU();

        private PlotModel plotGlobal;

        private bool useAreaForChart;

        private bool ignoreStockMinimums;

        public ItemChart(MainWindow mainWindowRef, WhlSKU selectedSku = null)
        {
            skus = mainWindowRef.DataSkus;
            mixdownCollection = mainWindowRef.DataSkusMixDown;
            if (selectedSku != null)
            {
                this.globalSelectedSku = selectedSku;
            }

            SetMainWindowRef(mainWindowRef);
            InitializeComponent();
            LoadChartData(selectedSku);
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
                var bottomAxis = new DateTimeAxis
                                     {
                                         Position = AxisPosition.Bottom,
                                         Maximum = Convert.ToDouble(endDate),
                                         AbsoluteMaximum = Convert.ToDouble(endDate),
                                         Title = "Date",
                                         StringFormat = "dd/M",
                                         MinorIntervalType = DateTimeIntervalType.Days
                                     };

                var leftAxis = new LinearAxis
                                   {
                                       Position = AxisPosition.Left,
                                       Minimum = 0,
                                       AbsoluteMinimum = 0,
                                       Title = "Sales"
                                   };
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
                var queryDict = MySQL.SelectData(query) as ArrayList;
                var stockHistoryPoints = new List<DataPoint>();
                var salesHistoryPoints = new List<DataPoint>();
                var stockHistoryPoints2 = new List<DataPoint>();
                var salesHistoryPoints2 = new List<DataPoint>();

                var salesSeries = new LineSeries();
                var stockSeries = new LineSeries();

                OxyPlot.Series.AreaSeries StockAreaSeries = new OxyPlot.Series.AreaSeries();
                OxyPlot.Series.AreaSeries SalesAreaSeries = new OxyPlot.Series.AreaSeries();
                var MaxStock = 0;
                var MaxSales = 0;
                try
                {
                    bottomAxis.AbsoluteMinimum =
                        Convert.ToDouble(DateTime.Parse((queryDict[0] as ArrayList)[1].ToString()).ToOADate());
                    bottomAxis.Minimum = Convert.ToDouble(
                        DateTime.Parse((queryDict[0] as ArrayList)[1].ToString()).ToOADate());
                }
                catch (Exception)
                {
                    bottomAxis.AbsoluteMinimum = Convert.ToDouble(startDate);
                    bottomAxis.Minimum = Convert.ToDouble(startDate);
                }

                foreach (ArrayList result in queryDict)
                {
                    double StockTotal;
                    if (ignoreStockMinimums)
                    {
                        StockTotal = Convert.ToDouble(Int32.Parse(result[2].ToString()));
                    }
                    else
                    {
                        StockTotal = Convert.ToDouble(
                            Int32.Parse(result[2].ToString()) + Int32.Parse(result[3].ToString()));
                    }
                    Double SalesTotal;
                    try
                    {
                        if (MaxStock < Int32.Parse(result[2].ToString()) + Int32.Parse(result[3].ToString()))
                            MaxStock = Int32.Parse(result[2].ToString()) + Int32.Parse(result[3].ToString());
                        if (DBNull.Value != result[4])
                        {
                            if (MaxSales < Int32.Parse(result[4].ToString()))
                                MaxSales = Int32.Parse(result[4].ToString());
                        }
                    }
                    catch (Exception)
                    {
                    }
                    try
                    {
                        SalesTotal = Convert.ToDouble(Int32.Parse(result[4].ToString()));
                    }
                    catch (Exception)
                    {
                        Console.WriteLine();
                        SalesTotal = Convert.ToDouble(0);
                    }
                    var Date = Convert.ToDouble(DateTime.Parse(result[1].ToString()).ToOADate());
                    var StockHistoryPoint = new DataPoint(Date, StockTotal);
                    var SaleHistoryPoint = new DataPoint(Date, SalesTotal);
                    var StockHistoryPoint2 = new DataPoint(Date, 0);
                    salesHistoryPoints.Add(SaleHistoryPoint);
                    stockHistoryPoints.Add(StockHistoryPoint);

                    salesHistoryPoints2.Add(StockHistoryPoint2);
                    stockHistoryPoints2.Add(StockHistoryPoint2);
                }

                salesSeries.Points.AddRange(salesHistoryPoints);
                stockSeries.Points.AddRange(stockHistoryPoints);

                rightAxis.Key = "StockKey";
                salesSeries.YAxisKey = leftAxis.Key;
                salesSeries.CanTrackerInterpolatePoints = false;
                salesSeries.Color = OxyColor.FromRgb(237, 125, 49);
                salesSeries.Title = "Sales History";
                stockSeries.YAxisKey = rightAxis.Key;
                stockSeries.CanTrackerInterpolatePoints = false;
                if (this.useAreaForChart)
                {
                    StockAreaSeries.Points.AddRange(stockHistoryPoints);
                    StockAreaSeries.YAxisKey = rightAxis.Key;
                    StockAreaSeries.CanTrackerInterpolatePoints = false;
                    StockAreaSeries.Fill = OxyColor.FromRgb(176, 195, 230);
                    StockAreaSeries.Color = OxyColor.FromRgb(138, 167, 218);
                    StockAreaSeries.Color2 = OxyColor.FromRgb(138, 167, 218);
                    StockAreaSeries.Points2.AddRange(stockHistoryPoints2);
                    //StockAreaSeries.ConstantY2 = 0;
                    StockAreaSeries.Title = "Stock History Area";

                    SalesAreaSeries.Points.AddRange(salesHistoryPoints);
                    SalesAreaSeries.CanTrackerInterpolatePoints = false;
                    SalesAreaSeries.Fill = OxyColor.FromArgb(140, 237, 125, 49);
                    SalesAreaSeries.Color = OxyColor.FromArgb(255, 138, 167, 218);
                    SalesAreaSeries.Color2 = OxyColor.FromRgb(138, 167, 218);
                    SalesAreaSeries.Points2.AddRange(stockHistoryPoints2);
                    //StockAreaSeries.ConstantY2 = 0;
                    SalesAreaSeries.Title = "Sales History Area";

                    PlotArea.Series.Add(StockAreaSeries);
                    PlotArea.Series.Add(SalesAreaSeries);
                }
                else
                {
                    stockSeries.Color = OxyColors.DarkGreen;
                    stockSeries.Title = "Stock History";
                    PlotArea.Series.Add(stockSeries);

                    PlotArea.Series.Add(salesSeries);
                }
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
                PlotArea.Axes.Add(bottomAxis);
                PlotArea.Axes.Add(leftAxis);
                PlotArea.Axes.Add(rightAxis);

                PlotArea.Title = Sku.ShortSku + " Sales/Stock History";

                this.plotGlobal = PlotArea;
            }
        }

        private void LoadGraphButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.globalSelectedSku = null;
                if (SkuBox.Text.Length == 7 || SkuBox.Text.Length == 13)
                {
                    var SkuText = SkuBox.Text.Substring(0, 7) + "0001";
                    this.globalSelectedSku = skus.SearchSKUSReturningSingleSku(SkuText);
                }
                else
                {
                    var SearchColl = new SkuCollection(true);
                    var SuppCodeTest = SkuBox.Text;
                    SearchColl = mixdownCollection.SearchSKUS(SuppCodeTest, true);
                    if (SearchColl.Count == 1) this.globalSelectedSku = SearchColl[0];
                    else
                    {
                        var Dialog = new SearchOptionsDialog(SearchColl);
                        Dialog.ShowDialog();
                    }
                }
                if (this.globalSelectedSku == null) throw new Exception("Unrecognised String");
                ItemTitle.Text = this.globalSelectedSku.Title.Invoice;
                if (UseAreaCheck.IsChecked == true) this.useAreaForChart = true;
                else this.useAreaForChart = false;

                if (IgnoreStockMinimumCheck.IsChecked == true) this.ignoreStockMinimums = true;
                else this.ignoreStockMinimums = false;
                Misc.OperationDialog("Loading Chart", LoadChartWorker);
                PlotPlot.Model = this.plotGlobal;
            }
            catch (Exception)
            {
            }
        }

        private void LoadChartWorker(object sender, DoWorkEventArgs e)
        {
            LoadChartData(this.globalSelectedSku);
        }

        private void SkuBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoadGraphButton_Click(null, null);
            }
        }
    }
}