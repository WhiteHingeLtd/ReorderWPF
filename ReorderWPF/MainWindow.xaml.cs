using ReorderWPF.UserControls;
using System;
using System.Collections;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.ComponentModel;
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
        internal SkuCollection Data_Skus;
        internal SupplierCollection SupplierCollection;

        public MainWindow()
        {
            InitializeComponent();
        }
       
        private void Window_Initialized(object sender, EventArgs e)
        {
            var Splash = new Splash();
            Splash.HomeRef = this;
            Splash.InitializeComponent();
            Splash.ShowDialog();
            //SupplierStackPanel.Visibility = Visibility.Visible;
            foreach (Supplier Supp in SupplierCollection)
            {
                var suppcount = MSSQLPublic.SelectData("SELECT COUNT(*) FROM whldata.sku_supplierdata WHERE SupplierName ='" + Supp.Code + "';") as ArrayList;
                var DiscontCount = MSSQLPublic.SelectData("SELECT COUNT(*) FROM whldata.sku_supplierdata WHERE SupplierName ='" + Supp.Code + "' AND isDiscontinued='True' ;") as ArrayList;
                var refcontrol = new SupplierControl();
                refcontrol.FullSupplierName.Text = Supp.Name;
                refcontrol.SupplierCode.Text = Supp.Code;
                refcontrol.TotalLines.Text = ((suppcount[0]) as ArrayList)[0].ToString();
                refcontrol.Discontinued.Text = ((DiscontCount[0]) as ArrayList)[0].ToString();
               // SupplierStackPanel.Children.Add(refcontrol);

            }
        }
        private void Window_Loaded(object sender, EventArgs e)
        {
            NewTab(new SupplierDashboard(SupplierCollection));
        }
        private void NewTab(ThreadedPage Control)
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
    }
}
