using ReorderWPF.UserControls;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.ComponentModel;
using System.Linq;
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
        public SkuCollection DataSkus;
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
        public string SupplierCode = "";
        public string OrderId = "";
        public DateTime OrderDate;
        public DateTime OrderDelivered;
        public DateTime OrderInvoiced;
        public int LinesOfStock = 0;
        public Single NetValue = 0;
        public string CustomOrderNote = "";
        public string CustomDeliveryNote = "";
        public Dictionary<WhlSKU, int> SkuOrderList;
        public int OrderState = 0;
        public enum OrderStates
        {
            Ordered = 0,
            Invoiced = 1,
            Delivered = 2,
            Closed = 3
        }

        public SupplierOrderData(string Supplier)
        {

        }
        public SupplierOrderData(Supplier Supplier)
        {
            SupplierCode = Supplier.Code;
            OrderGuid = new Guid();
            OrderDate = DateTime.Now;
            LinesOfStock = 0;
            NetValue = 0;
            CustomDeliveryNote = "";
        }

        public SupplierOrderData(bool cancel = true)
        {
            
        }

}

    public class SupplierData
    {
        public string SupplierName;
        public string SupplierCode;
        public int Lines;
        public int Discontinued;
        public int Oversold;
        public int LowStock;
        public Single LowPercentage;
        public DateTime LastOrder;
        public DateTime DateDue;
        public SkuCollection ListOfSkus;

        public SupplierData(string code,SkuCollection FullCollection)
        {
            SupplierCode = code;
            ListOfSkus = FullCollection.SearchBySuppName(code);
            Lines = ListOfSkus.Count;
            LowPercentage = LowStock / Lines;
        }
    }

    public class ReorderDataLoader
    {
        public SupplierOrderData LoadSupplierOrderData(Guid orderGuid)
        {
            var returnable = new SupplierOrderData();
            var OrderData = MSSQLPublic.SelectDataDictionary("SELECT * from whldata.reorder_orders WHERE orderguid='" + orderGuid.ToString() + "';");
            return returnable;
        }
        public void SaveCurrentOrderToDatabase(object sender, DoWorkEventArgs e)
        {
            if (sender.GetType() == typeof(SupplierOrderData))
            {
                var CurrentOrder = sender as SupplierOrderData;
                if (CurrentOrder == null) throw new NullReferenceException();
                MSSQLPublic.insertUpdate("DELETE FROM whldata.reorder_orders WHERE OrderGUID = '" + CurrentOrder.OrderGuid.ToString() + "'");
                MSSQLPublic.insertUpdate(
                    @"INSERT INTO whldata.reorder_orders (OrderGUID, SupplierCode, CustomOrderID, OrderDate, OrderDelivered, OrderInvoiced, LinesOfStock, NetValue, CustomOrderNote, CustomDeliveryNote,OrderState)
                                    VALUES ('" + CurrentOrder.OrderGuid + "','" + CurrentOrder.SupplierCode + "','" +
                    CurrentOrder.OrderId + "','" + CurrentOrder.OrderDate.ToString() + "','" +
                    CurrentOrder.OrderDelivered.ToString() + "','" + CurrentOrder.OrderInvoiced.ToString() + "','" +
                    CurrentOrder.LinesOfStock.ToString() + "'," +
                    "'" + CurrentOrder.NetValue.ToString() + "','" + CurrentOrder.CustomOrderNote + "','" +
                    CurrentOrder.CustomDeliveryNote + "','" + CurrentOrder.OrderState.ToString() + "';");
            }
        }
    }

    public class ReorderSupplier
    {
        public string Code;
        public string FullName;
        public SkuCollection Children;
        public double MinimumOrder;
        public int LeadDays;
        public bool CartonDiscount;
        public double ReorderAtPercentage;
        public DateTime LastOrder;
        public Guid LastOrderGuid;

        public ReorderSupplier(Supplier CurrentSupplier, SkuCollection FullSkuCollection)
        {
            var Query = MSSQLPublic.SelectDataDictionary("SELECT TOP 1 * from whldata.reorder_supplierdata WHERE SupplierCode='" + CurrentSupplier.Code + "';");
            foreach (Dictionary<string, object> result in Query)
            {
                Code = result["SupplierCode"].ToString();
                FullName = result["SupplierFullName"].ToString();
                Children = FullSkuCollection.SearchBySuppName(Code);
                try
                {
                    MinimumOrder = Convert.ToDouble(result["MinimumOrder"]);
                }
                catch (NullReferenceException)
                {
                    MinimumOrder = 0;                   
                }
                int CheckForDiscount = Convert.ToInt32(result["CartonDiscount"]);
                if (CheckForDiscount == 1) CartonDiscount = true;
                else CartonDiscount = false;
                ReorderAtPercentage = Convert.ToDouble(result["ReorderPercentage"]);
                if (result["LastOrder"] != DBNull.Value) LastOrder = DateTime.Parse(result["LastOrder"].ToString());
                if (result["LastOrderGuid"] != DBNull.Value) LastOrderGuid = new Guid(result["LastOrderGuid"].ToString());
            }
        }
    }

}
