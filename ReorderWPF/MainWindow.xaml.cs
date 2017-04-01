using ReorderWPF.UserControls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using WHLClasses;
using ReorderWPF.CustomControls;
using ReorderWPF;
using ReorderWPF.Pages;

namespace ReorderWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        public EmployeeCollection Data_Employees;
        internal WHLClasses.Authentication.AuthClass User_Employee;
        internal SkuCollection DataSkus;
        internal SkuCollection DataSkusMixDown;
        internal SupplierCollection SupplierCollection;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void UpdateShellColor(object sender, PropertyChangedEventArgs e)
        {
            byte R = Convert.ToByte((Convert.ToInt32(SystemParameters.WindowGlassColor.R) + 510)/3);
            byte G = Convert.ToByte((Convert.ToInt32(SystemParameters.WindowGlassColor.G) + 510) / 3);
            byte B = Convert.ToByte((Convert.ToInt32(SystemParameters.WindowGlassColor.B) + 510) / 3);
            SolidColorBrush BackgroundBrush = new SolidColorBrush(Color.FromRgb(R, G, B));
            MainRibbon.Background = BackgroundBrush;
        }


        private void Window_Initialized(object sender, EventArgs e)
        {
            var Splash = new Splash();
            Splash.HomeRef = this;
            Splash.InitializeComponent();
            Splash.ShowDialog();

        }
        private void Window_Loaded(object sender, EventArgs e)
        {
            NewTab(new SupplierDashboard(this,SupplierCollection));
            UpdateShellColor(null, null);
            SystemParameters.StaticPropertyChanged += UpdateShellColor;
        }
        public void NewTab(ThreadedPage Control)
        {
            PageFrameTab Tab = new PageFrameTab(Control);
            if (Control.SupportsMultipleTabs())
            {
                Tabs.Items.Add(Tab);
                Tab.Focus();
                Tabs.SelectedItem = Tab;
            }
            else
            {
                //Check if the tab has already been opened
                bool Exists = false;
                PageFrameTab ExistingRef = default(PageFrameTab);
                foreach (object Existing in Tabs.Items)
                {
                    try
                    {
                        PageFrameTab TExisting = Existing as PageFrameTab;
                        if (Control.GetType() == TExisting.GetChildType())
                        {
                            Exists = true;
                            ExistingRef = TExisting;
                        }

                    }
                    catch (Exception)
                    {

                    }

                }
                if (!Exists)
                {
                    Tabs.Items.Add(Tab);
                    Tab.Focus();
                    Tabs.SelectedItem = Tab;
                }
                else
                {
                    ExistingRef.Focus();
                    Tabs.SelectedItem = ExistingRef;
                }
            }



        }

        private void RibbonWindow_Closing(object sender, CancelEventArgs e)
        {
            bool fuck = false;
            foreach (TabItem Child in Tabs.Items)
            {
                try
                {
                    
                    ((PageFrameTab)Child).Content.TabClosing(ref fuck);
                    

                }
                catch (Exception)
                {
                }
            }
            e.Cancel = fuck;
        }

        private void RibbonButton_Click(object sender, RoutedEventArgs e)
        {
            NewTab(new ItemChart(this));
        }


        private void RefreshItemDataButton_Click(object sender, RoutedEventArgs e)
        {
            Misc.OperationDialog("Refreshing Item Data",ItemDataRefreshForWorker);
        }

        private void ItemDataRefreshForWorker(object sender, DoWorkEventArgs e)
        {
            GenericDataController loader = new GenericDataController();
            DataSkus = loader.SmartSkuCollLoad();
            DataSkusMixDown = DataSkus.MakeMixdown();
        }
    }

    public class SupplierOrderData
    {
        public Guid OrderGuid;
        public string SupplierCode;
        public string OrderId;
        public DateTime OrderDate;
        public DateTime OrderDelivered;
        public DateTime OrderInvoiced;
        public int LinesOfStock;
        public Single NetValue;
        public string CustomOrderNote;
        public string CustomDeliveryNote;
        public List<Dictionary<WhlSKU, int>> SkuOrderList;



        public enum OrderStates
        {
            Ordered = 0,
            Invoiced = 1,
            Delivered = 2,
            Closed = 3
        }
    }
}
