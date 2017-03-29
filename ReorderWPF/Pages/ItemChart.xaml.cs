using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OxyPlot.Axes;
using ReorderWPF.CustomControls;
using WHLClasses;
using DateTimeAxis = OxyPlot.Wpf.DateTimeAxis;

namespace ReorderWPF.Pages
{
    /// <summary>
    /// Interaction logic for ItemChart.xaml
    /// </summary>
    public partial class ItemChart : ThreadedPage
    {
        public ItemChart()
        {
            InitializeComponent();
        }

        internal override void TabClosing(ref bool cancel)
        {
            cancel = false;
            

        }

        private void LoadChartData(WhlSKU Sku = null)
        {
            var endDate = DateTime.Now;
            var startDate = DateTime.Now.AddMonths(-6);
            var BottomAxis = new DateTimeAxis();
            BottomAxis.Position = AxisPosition.Bottom;
            BottomAxis.Minimum = startDate.ToDouble(); 
            PlotArea.Axes.Add(new DateTimeAxis());
        }

    }
}
