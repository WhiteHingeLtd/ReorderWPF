

namespace ReorderWPF.Pages
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using OxyPlot;
    using OxyPlot.Axes;
    using ReorderWPF.CustomControls;
    using WHLClasses;
    using LineSeries = OxyPlot.Series.LineSeries;

    /// <summary>
    /// Interaction logic for OrdersByDate.xaml
    /// </summary>
    public partial class OrdersByDate : ThreadedPage
    {
        public PlotModel PlotGlobal;

        public OrdersByDate()
        {
            InitializeComponent();
        }

        internal override void TabClosing(ref bool cancel)
        {
            cancel = false;
        }

        private void LoadByDateClick(object sender, RoutedEventArgs e)
        {
            Misc.OperationDialog("Loading", LoadDate);
            PlotPlot.Model = PlotGlobal;
        }

        private void LoadDate(object sender, DoWorkEventArgs e)
        {
            var queryDict = SQLServer.MSSelectDataDictionary(@"SELECT 
            SUBSTRING(a.OrderDateTime, 12, 5) as TimeOrdered,
            Count(a.OrderDateTime) as Orders
            FROM(SELECT Max(OrderDateTime) as OrderDateTime FROM whldata.newsales_raw GROUP BY OrderID) as a
                GROUP BY SUBSTRING(a.OrderDateTime, 12, 5)
            Order By SUBSTRING(a.OrderDateTime, 12, 5) asc");

            var plotArea = new PlotModel();

            var listOfPlots
                = new List<DataPoint>();
            var bottomAxis = new TimeSpanAxis
                                 {
                                     Position = AxisPosition.Bottom,
                                     Title = "Time",
                                     StringFormat = "hh:mm"
                                 };
            var leftAxis = new LinearAxis();
            leftAxis.Position = AxisPosition.Left;
            leftAxis.Minimum = 0;
            leftAxis.AbsoluteMinimum = 0;
            leftAxis.Title = "Sales";
            plotArea.Axes.Add(leftAxis);
            plotArea.Axes.Add(bottomAxis);
            foreach (var result in queryDict)
            {
                var time = 0.0;
                var orders = 0.0;
                try
                {
                    time = TimeSpan.Parse(result["TimeOrdered"].ToString()).TotalSeconds;
                    orders = Convert.ToDouble(result["Orders"]);
                }
                catch (FormatException formatEx)
                {
                    Console.WriteLine(formatEx);
                }

                var pointOfData = new DataPoint(time, orders);
                listOfPlots.Add(pointOfData);
            }
            var orderSeries = new LineSeries();
            orderSeries.Points.AddRange(listOfPlots);
            orderSeries.CanTrackerInterpolatePoints = false;
            plotArea.Series.Add(orderSeries);
            PlotGlobal = plotArea;
        }


    }
}
