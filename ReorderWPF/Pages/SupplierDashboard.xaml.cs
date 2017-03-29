using ReorderWPF.CustomControls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
using ReorderWPF.UserControls;
using WHLClasses;

namespace ReorderWPF.Pages
{
    /// <summary>
    /// Interaction logic for SupplierDashboard.xaml
    /// </summary>
    public partial class SupplierDashboard : ThreadedPage
    {
        public SupplierDashboard(SupplierCollection supps)
        {
            InitializeComponent();

            foreach (Supplier Supp in supps)
            {
                var refcontrol = new SupplierControl();
                try
                {
                    var suppcount = MSSQLPublic.SelectData("SELECT COUNT(*) FROM whldata.sku_supplierdata WHERE SupplierName ='" + Supp.Code + "';") as ArrayList;
                    var DiscontCount = MSSQLPublic.SelectData("SELECT COUNT(*) FROM whldata.sku_supplierdata WHERE SupplierName ='" +Supp.Code + "' AND isDiscontinued='True' ;") as ArrayList;
                    refcontrol.FullSupplierName.Text = Supp.Name;
                    refcontrol.SupplierCode.Text = Supp.Code;
                    refcontrol.TotalLines.Text = (suppcount[0] as ArrayList)[0].ToString();
                    refcontrol.Discontinued.Text = (DiscontCount[0] as ArrayList)[0].ToString();
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
        internal override void TabClosing(ref bool cancel)
        {
            MessageBoxResult asd = MessageBox.Show("asd","asd",MessageBoxButton.YesNo);
            if (asd == MessageBoxResult.Yes)
            {
                MessageBox.Show("cancelled");
                cancel = true;
            }
            else
            {
                MessageBox.Show("not cancelled, app is rip");
            }
        
        }
    }
}
