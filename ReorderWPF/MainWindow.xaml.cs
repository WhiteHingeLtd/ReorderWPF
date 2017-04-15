namespace ReorderWPF
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Ribbon;
    using System.Windows.Media;
    using ReorderWPF.CustomControls;
    using ReorderWPF.Pages;   
    using WHLClasses;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        public static Employee AuthdEmployee;
        public EmployeeCollection DataEmployees;
        public WHLClasses.Authentication.AuthClass UserEmployee;      
        public SkuCollection DataSkus;
        internal SkuCollection DataSkusMixDown;
        internal SupplierCollection SupplierCollection;

        public MainWindow()
        {
            this.InitializeComponent();
            AuthdEmployee = this.UserEmployee.AuthenticatedUser;
        }

        /// <summary>
        /// Creates a new Tab based on a ThreadedPage Control.
        /// </summary>
        /// <param name="refControl">The threaded page that you want to create. Must have an xaml and associated Code behind and impletement TabClosing</param>
        public void NewTab(ThreadedPage refControl)
        {
            var tab = new PageFrameTab(refControl);
            if (refControl.SupportsMultipleTabs())
            {
                Tabs.Items.Add(tab);
                tab.Focus();
                Tabs.SelectedItem = tab;
            }
            else
            {
                // Check if the tab has already been opened
                var exists = false;
                var existingRef = default(PageFrameTab);
                foreach (object Existing in Tabs.Items)
                {
                    try
                    {
                        var frameTab = Existing as PageFrameTab;
                        if (frameTab == null)
                        {
                            throw new NullReferenceException();
                        }

                        if (refControl.GetType() == frameTab.GetChildType())
                        {
                            exists = true;
                            existingRef = frameTab;
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                }
                if (!exists)
                {
                    Tabs.Items.Add(tab);
                    tab.Focus();
                    Tabs.SelectedItem = tab;
                }
                else
                {
                    existingRef.Focus();
                    Tabs.SelectedItem = existingRef;
                }
            }



        }

        private void UpdateShellColor(object sender, PropertyChangedEventArgs e)
        {
            var redByte = Convert.ToByte((Convert.ToInt32(SystemParameters.WindowGlassColor.R) + 510) / 3);
            var greenByte = Convert.ToByte((Convert.ToInt32(SystemParameters.WindowGlassColor.G) + 510) / 3);
            var blueByte = Convert.ToByte((Convert.ToInt32(SystemParameters.WindowGlassColor.B) + 510) / 3);
            var backgroundBrush = new SolidColorBrush(Color.FromRgb(redByte, greenByte, blueByte));
            MainRibbon.Background = backgroundBrush;
        }


        private void WindowInitialized(object sender, EventArgs e)
        {
            var newSplash = new Splash
                             {
                                 HomeRef = this
                             };
            newSplash.InitializeComponent();
            newSplash.ShowDialog();

        }

        private void WindowLoaded(object sender, EventArgs e)
        {
            NewTab(new SupplierDashboard(this,SupplierCollection));
            UpdateShellColor(null, null);
            SystemParameters.StaticPropertyChanged += UpdateShellColor;
        }

        private void RibbonWindowClosing(object sender, CancelEventArgs e)
        {
            bool fuck = false;
            foreach (TabItem child in Tabs.Items)
            {
                try
                {                    
                    ((PageFrameTab)child).Content.TabClosing(ref fuck);                   
                }
                catch (Exception)
                {
                }

            }

            e.Cancel = fuck;
        }

        private void ItemChartClick(object sender, RoutedEventArgs e)
        {
            NewTab(new ItemChart(this));
        }


        private void RefreshItemDataButtonClick(object sender, RoutedEventArgs e)
        {
            Misc.OperationDialog("Refreshing Item Data", ItemDataRefreshForWorker);
        }

        private void ItemDataRefreshForWorker(object sender, DoWorkEventArgs e)
        {
            var loader = new GenericDataController();
            DataSkus = loader.SmartSkuCollLoad();
            DataSkusMixDown = DataSkus.MakeMixdown();
        }

        private void OrdersByDateClick(object sender, RoutedEventArgs e)
        {
            NewTab(new OrdersByDate());
        }
    }
}
