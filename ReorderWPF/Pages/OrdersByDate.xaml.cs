using OxyPlot;
using OxyPlot.Axes;
using ReorderWPF.CustomControls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using ReorderWPF.Dialogs;
using WHLClasses;
using LineSeries = OxyPlot.Series.LineSeries;

namespace ReorderWPF.Pages
{
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

        private void LoadByDate_Click(object sender, RoutedEventArgs e)
        {
            Misc.OperationDialog("Loading", LoadDate);
            PlotPlot.Model = PlotGlobal;
        }

        private void LoadByTime_Click(object sender, RoutedEventArgs e)
        {

        }

        private void LoadDate(object sender, DoWorkEventArgs e)
        {
            var QueryDict = MSSQLPublic.SelectDataDictionary(@"SELECT 

            SUBSTRING(a.OrderDateTime, 12, 5) as TimeOrdered,
            Count(a.OrderDateTime) as Orders
            FROM(SELECT Max(OrderDateTime) as OrderDateTime FROM whldata.newsales_raw GROUP BY OrderID) as a
                GROUP BY SUBSTRING(a.OrderDateTime, 12, 5)
            Order By SUBSTRING(a.OrderDateTime, 12, 5) asc");
            var PlotArea = new PlotModel();

            var ListOfPlots = new List<DataPoint>();
            var BottomAxis = new TimeSpanAxis();
            BottomAxis.Position = AxisPosition.Bottom;
            BottomAxis.Title = "Time";
            BottomAxis.StringFormat = "hh:mm";
            var leftAxis = new LinearAxis();
            leftAxis.Position = AxisPosition.Left;
            leftAxis.Minimum = 0;
            leftAxis.AbsoluteMinimum = 0;
            leftAxis.Title = "Sales";
            PlotArea.Axes.Add(leftAxis);
            PlotArea.Axes.Add(BottomAxis);
            foreach (var result in QueryDict)
            {
                var Time = 0.0;
                var Orders = 0.0;
                try
                {
                    Time = TimeSpan.Parse(result["TimeOrdered"].ToString()).TotalSeconds;
                    Orders = Convert.ToDouble(result["Orders"]);
                }
                catch (FormatException FormatEx)
                {
                    Console.WriteLine(FormatEx);
                }

                var PointOfData = new DataPoint(Time, Orders);
                ListOfPlots.Add(PointOfData);
            }
            var OrderSeries = new LineSeries();
            OrderSeries.Points.AddRange(ListOfPlots);
            OrderSeries.CanTrackerInterpolatePoints = false;
            PlotArea.Series.Add(OrderSeries);
            PlotGlobal = PlotArea;
        }
        internal override void TabClosing(ref bool cancel)
        {
            cancel = false;

        }
    }
}
