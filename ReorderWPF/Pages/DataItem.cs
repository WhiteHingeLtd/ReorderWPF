namespace ReorderWPF.Pages
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using WHLClasses;
    using OxyPlot;
    using OxyPlot.Axes;
    using LineSeries = OxyPlot.Series.LineSeries;
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
                foreach (var loc in SkuData.Locations)
                {
                    returnstring += loc.LocationText + ", ";
                }
                returnstring = returnstring.TrimEnd();
                returnstring = returnstring.Remove(returnstring.Length - 1);
                return returnstring;
            }
            
        }

        public SkuCollection Children => SupplierData.SupplierSkuCollectionFull.GatherChildren(SkuData.ShortSku);

        public List<DataItemDetails> Packsizes
        {
            get
            {
                var PacksizeList = new List<DataItemDetails>();
                foreach (WhlSKU Pack in this.Children)
                {
                    DataItemDetails newitem = DataItemDetails.NewDataItemDetails(Pack);
                    if (newitem == null) continue;
                    PacksizeList.Add(newitem);
                }
                return PacksizeList;
            }

        }

        public static DataItem DataItemNew(WhlSKU sku)
        {
            var newItem = new DataItem
                              {
                                  ItemName = sku.Title.Invoice,
                                  Sku = sku.ShortSku,
                                  SkuData = sku,
                                  StockLevel = sku.Stock.Level
                              };        
            try
            {
                foreach (var child in newItem.Children)
                {
                    newItem.AverageSales += int.Parse(child.SalesData.WeightedAverage.ToString()) * child.PackSize;
                }        
            }
            catch (Exception)
            {
                newItem.AverageSales = 0;
            }
            if (newItem.AverageSales != 0)
            {
                newItem.WeeksRemaining = Math.Round(Convert.ToDouble(newItem.StockLevel / newItem.AverageSales),1);
            }
            else
            {
                newItem.WeeksRemaining = 999;
            }
           
            foreach (SKUSupplier supp in sku.Suppliers)
            {
                if (!supp.Primary)
                {
                    continue;
                }

                newItem.SupplierCode = supp.ReOrderCode;
            }
            try
            {
                newItem.SalesGraph = LoadChartData(sku);
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine(e);
            }
            
            return newItem;
        }


        public static PlotModel LoadChartData(WhlSKU paraSku)
        {
            if (paraSku != null)
            {

                var plotArea = new PlotModel();
                var endDate = DateTime.Now.ToOADate();
                var startDate = DateTime.Now.AddMonths(-6).ToOADate();
                var bottomAxis =
                    new OxyPlot.Axes.DateTimeAxis
                        {
                            Position = AxisPosition.Bottom,
                            Maximum = Convert.ToDouble(endDate),
                            AbsoluteMaximum = Convert.ToDouble(endDate),
                            Title = "Date",
                            StringFormat = "dd/M",
                            MinorIntervalType = DateTimeIntervalType.Days
                        };


                var leftAxis = new OxyPlot.Axes.LinearAxis
                                   {
                                       Position = AxisPosition.Left,
                                       Minimum = 0,
                                       AbsoluteMinimum = 0,
                                       Title = "Sales"
                                   };
                var rightAxis = new OxyPlot.Axes.LinearAxis
                                    {
                                        Position = AxisPosition.Right,
                                        Minimum = 0,
                                        AbsoluteMinimum = 0,
                                        Maximum = 5000,
                                        Title = "Stock"
                                    };


                var query = @"SELECT a.shortSku, a.stockDate, a.Stocklevel, a.StockMinimum, b.maintotal 
                          FROM whldata.stock_history as a
                            LEFT JOIN(SELECT top (999999999999) a.orderdate, a.shortsku, sum(a.total)as ""maintotal"" FROM
                            (SELECT top (999999999999) orderdate, sku, SUBSTRING(sku, 0, 8) as ""shortsku"", sum(salequantity) as ""sales"", CAST(SUBSTRING(sku, 8, 4) as /*unsigned*/ int) as ""packsize"", sum(salequantity * CAST(SUBSTRING(sku, 8, 4) as /*unsigned*/ int)) as 'total'
                             FROM whldata.newsales_raw
                             WHERE sku LIKE '" + paraSku.ShortSku + @"%'
                             group by sku, orderDate
                             order by orderdate) as a
                            GROUP BY orderdate, shortsku
                            ORDER BY orderDate) as b
                            on b.shortsku = SUBSTRING(a.shortSku, 0, 8) AND b.orderDate = a.stockDate
                            WHERE a.shortsku = '" + paraSku.SKU + @"'
                            ORDER BY StockDate ASC";
                var queryDict = SQLServer.MSSelectDataDictionary(query);
                if (queryDict == null)
                {
                    throw new NullReferenceException();
                }

                var stockHistoryPoints = new List<DataPoint>();
                var salesHistoryPoints = new List<DataPoint>();
                var stockHistoryFillPoints = new List<DataPoint>();
                var salesHistoryFillPoints = new List<DataPoint>();


                var salesSeries = new LineSeries();
                var stockSeries = new LineSeries();

                OxyPlot.Series.AreaSeries stockAreaSeries = new OxyPlot.Series.AreaSeries();
                OxyPlot.Series.AreaSeries salesAreaSeries = new OxyPlot.Series.AreaSeries();
                var maxStock = 0;
                var maxSales = 0;
                if (queryDict.Count != 0)
                {
                    try
                    {
                        bottomAxis.AbsoluteMinimum =
                            Convert.ToDouble(DateTime.Parse((queryDict[0])["stockDate"].ToString()).ToOADate());
                        bottomAxis.Minimum =
                            Convert.ToDouble(DateTime.Parse((queryDict[0])["stockDate"].ToString()).ToOADate());
                    }
                    catch (Exception)
                    {
                        bottomAxis.AbsoluteMinimum = Convert.ToDouble(startDate);
                        bottomAxis.Minimum = Convert.ToDouble(startDate);
                    }

                    foreach (var result in queryDict)
                    {
                        var stockTotal = Convert.ToDouble(int.Parse(result["Stocklevel"].ToString()));
                        double salesTotal;

                        try
                        {
                            if (maxStock < int.Parse(result["Stocklevel"].ToString()) + int.Parse(result["StockMinimum"].ToString()))
                            {
                                maxStock = int.Parse(result["Stocklevel"].ToString()) + int.Parse(result["StockMinimum"].ToString());
                            }
                            if (DBNull.Value != result["maintotal"])
                            {
                                if (maxSales < int.Parse(result["maintotal"].ToString()))
                                {
                                    maxSales = int.Parse(result["maintotal"].ToString());
                                }
                            }

                        }
                        catch (Exception)
                        {

                        }
                        salesTotal = Convert.ToDouble(result["maintotal"] == DBNull.Value ? 0 : int.Parse(result["maintotal"].ToString()));


                        var date = Convert.ToDouble(DateTime.Parse(result["stockDate"].ToString()).ToOADate());
                        var stockHistoryPoint = new DataPoint(date, stockTotal);
                        var saleHistoryPoint = new DataPoint(date, salesTotal);
                        var stockHistoryPoint2 = new DataPoint(date, 0);
                        salesHistoryPoints.Add(saleHistoryPoint);
                        stockHistoryPoints.Add(stockHistoryPoint);

                        salesHistoryFillPoints.Add(stockHistoryPoint2);
                        stockHistoryFillPoints.Add(stockHistoryPoint2);
                    }
                }
                else
                {
                    var queryDict2 = SQLServer.SelectData(
                                         "SELECT StockLevel,StockMinimum,StockDate from whldata.stock_history WHERE sku="
                                         + paraSku.ShortSku + "';") as ArrayList;
                    if (queryDict2 != null)
                    {
                        foreach (ArrayList result in queryDict2)
                        {
                            if (maxStock < int.Parse(result[0].ToString()) + int.Parse(result[1].ToString()))
                            {
                                maxStock = int.Parse(result[0].ToString()) + int.Parse(result[1].ToString());
                            }

                            var stockLevel = Convert.ToDouble(int.Parse(result[0].ToString()));
                            var date = Convert.ToDouble(DateTime.Parse(result[2].ToString()).ToOADate());
                            var stockHistoryPoint = new DataPoint(date, stockLevel);
                            var stockHistoryPoint2 = new DataPoint(date, 0);
                            stockHistoryPoints.Add(stockHistoryPoint);
                            stockHistoryFillPoints.Add(stockHistoryPoint2);
                        }
                    }
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

                stockAreaSeries.Points.AddRange(stockHistoryPoints);
                stockAreaSeries.YAxisKey = rightAxis.Key;
                stockAreaSeries.CanTrackerInterpolatePoints = false;
                stockAreaSeries.Fill = OxyColor.FromRgb(176, 195, 230);
                stockAreaSeries.Color = OxyColor.FromRgb(138, 167, 218);
                stockAreaSeries.Color2 = OxyColor.FromRgb(138, 167, 218);
                stockAreaSeries.Points2.AddRange(stockHistoryFillPoints);

                stockAreaSeries.Title = "Stock History Area";

                salesAreaSeries.Points.AddRange(salesHistoryPoints);
                salesAreaSeries.CanTrackerInterpolatePoints = false;
                salesAreaSeries.Fill = OxyColor.FromArgb(140, 237, 125, 49);
                salesAreaSeries.Color = OxyColor.FromArgb(255, 138, 167, 218);
                salesAreaSeries.Color2 = OxyColor.FromRgb(138, 167, 218);
                salesAreaSeries.Points2.AddRange(stockHistoryFillPoints);

                salesAreaSeries.Title = "Sales History Area";


                plotArea.Series.Add(stockAreaSeries);
                plotArea.Series.Add(salesAreaSeries);


                if (maxSales == 0)
                {
                    leftAxis.AbsoluteMaximum = 1;
                    rightAxis.AbsoluteMaximum += 10;
                    leftAxis.Title = "No sales";
                }
                if (maxSales > 0)
                {
                    leftAxis.AbsoluteMaximum = (maxSales * 1.15) + 10;
                    leftAxis.Maximum = (maxSales * 1.1) + 10;
                    rightAxis.Maximum = maxStock * 1.1;
                    rightAxis.AbsoluteMaximum = maxStock * 1.15;
                }

                rightAxis.AbsoluteMaximum = maxStock;
                plotArea.Axes.Add(bottomAxis);
                plotArea.Axes.Add(leftAxis);
                plotArea.Axes.Add(rightAxis);

                plotArea.Title = paraSku.ShortSku + " Sales/Stock History";

                return plotArea;
            }
            else
            {
                return null;
            }
        }

    }
}
