using ReorderWPF.CustomControls;
using System;
using System.Collections;
using System.Windows;
using ReorderWPF.UserControls;
using WHLClasses;

namespace ReorderWPF.Pages
{
    /// <summary>
    /// Interaction logic for SupplierDashboard.xaml
    /// </summary>
    public partial class SupplierDashboard : ThreadedPage
    {
        public SupplierDashboard(MainWindow Main, SupplierCollection supps)
        {
            InitializeComponent();
            SetMainWindowRef(Main);
            foreach (Supplier Supp in supps)
            {
                var refcontrol = new SupplierControl();
                try
                {
                    var suppcount = MSSQLPublic.SelectData("SELECT COUNT(*) FROM whldata.sku_supplierdata WHERE SupplierName ='" + Supp.Code + "';") as ArrayList;
                    var DiscontCount = MSSQLPublic.SelectData("SELECT COUNT(*) FROM whldata.sku_supplierdata WHERE SupplierName ='" +Supp.Code + "' AND isDiscontinued='True' ;") as ArrayList;
                    refcontrol.FullSupplierName.Text = Supp.Name;
                    refcontrol.SupplierCode.Text = Supp.Code;
                    refcontrol.TotalLines.Text = (suppcount[0] as ArrayList)[0].ToString() + " Lines";
                    refcontrol.Discontinued.Text = (DiscontCount[0] as ArrayList)[0].ToString() + " Discontinued";
                    refcontrol.SupplierCodeInternal = Supp.Code;
                    refcontrol.SupplierInternal = Supp;
                    refcontrol.MouseUp += Refcontrol_MouseUp;
                    refcontrol.TouchUp += Refcontrol_TouchUp;
                }
                catch (InvalidCastException)
                {
                    continue;
                }
                catch (NullReferenceException)
                {
                    continue;
                }
                finally
                {
                    SupplierStackPanel.Children.Add(refcontrol);
                }
                

            }
        }

        private void Refcontrol_TouchUp(object sender, System.Windows.Input.TouchEventArgs e)
        {
            var Ctrl = sender as SupplierControl;
            CreateNewTab(Ctrl);

        }

        private void Refcontrol_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var Ctrl = sender as SupplierControl;
            CreateNewTab(Ctrl);
        }

        internal override void TabClosing(ref bool cancel)
        {
            MessageBoxResult asd = MessageBox.Show("Are you sure","Close Application",MessageBoxButton.YesNo);
            if (asd == MessageBoxResult.No)
            {
                cancel = true;
            }
            else
            {
                cancel = false;
            }
        
        }

        private void CreateNewTab(SupplierControl sender)
        {
            MainWindowRef.NewTab(new SupplierData(MainWindowRef, sender.SupplierInternal));
        }
    }
}
