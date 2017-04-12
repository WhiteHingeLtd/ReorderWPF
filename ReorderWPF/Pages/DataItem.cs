using System;
using System.Collections;
using System.Collections.Generic;
using WHLClasses;
using OxyPlot;
using OxyPlot.Axes;
using LineSeries = OxyPlot.Series.LineSeries;

namespace ReorderWPF.Pages
{
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
            NewItem.SkuData = sku;
            
            try
            {
                foreach (WhlSKU Child in NewItem.Children)
                {
                    NewItem.AverageSales += Int32.Parse(Child.SalesData.WeightedAverage.ToString()) * Child.PackSize;
                }        
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
            try
            {
                NewItem.SalesGraph = LoadChartData(sku);
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine(e);
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
        public static PlotModel LoadChartData(WhlSKU Sku)
        {
            if (Sku != null)
            {

                PlotModel PlotArea = new PlotModel();
                var endDate = DateTime.Now.ToOADate();
                var startDate = DateTime.Now.AddMonths(-6).ToOADate();
                var BottomAxis = new OxyPlot.Axes.DateTimeAxis
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
                             WHERE sku LIKE '" + Sku.ShortSku + @"%'
                             group by sku, orderDate
                             order by orderdate) as a
                            GROUP BY orderdate, shortsku
                            ORDER BY orderDate) as b
                            on b.shortsku = SUBSTRING(a.shortSku, 0, 8) AND b.orderDate = a.stockDate
                            WHERE a.shortsku = '" + Sku.SKU + @"'
                            ORDER BY StockDate ASC";
                var QueryDict = MSSQLPublic.SelectData(query) as ArrayList;
                if (QueryDict == null) throw new NullReferenceException();
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
                if (QueryDict.Count != 0)
                {
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
                }
                else
                {
                    var QueryDict2 = MSSQLPublic.SelectData("SELECT StockLevel,StockMinimum,StockDate from whldata.stock_history WHERE sku="+Sku.ShortSku+"';") as ArrayList;
                    foreach (ArrayList result in QueryDict2)
                    {
                        if (MaxStock < Int32.Parse(result[0].ToString()) + Int32.Parse(result[1].ToString()))
                            MaxStock = Int32.Parse(result[0].ToString()) + Int32.Parse(result[1].ToString());
                        Double StockTotal;
                        StockTotal = Convert.ToDouble(Int32.Parse(result[0].ToString()));
                        var Date = Convert.ToDouble(DateTime.Parse(result[2].ToString()).ToOADate());
                        var StockHistoryPoint = new DataPoint(Date, StockTotal);
                        var StockHistoryPoint2 = new DataPoint(Date, 0);
                        StockHistoryPoints.Add(StockHistoryPoint);
                        StockHistoryPoints2.Add(StockHistoryPoint2);
                    }

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
                    leftAxis.AbsoluteMaximum = (MaxSales * 1.15) + 10;
                    leftAxis.Maximum = (MaxSales * 1.1) + 10;
                    rightAxis.Maximum = MaxStock * 1.1;
                    rightAxis.AbsoluteMaximum = MaxStock * 1.15;
                }
                //leftAxis.IsZoomEnabled = false;
                //leftAxis.AbsoluteMaximum = MaxSales;
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
}
