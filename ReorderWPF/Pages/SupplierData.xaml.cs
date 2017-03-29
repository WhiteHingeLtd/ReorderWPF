using System;
using WHLClasses;
using System.Windows.Controls;
using ReorderWPF.CustomControls;

namespace ReorderWPF.Pages
{
    /// <summary>
    /// Interaction logic for SupplierData.xaml
    /// </summary>
    public partial class SupplierData : ThreadedPage
    {
        public SupplierData(MainWindow Main, Supplier SupplierCode)
        {
            InitializeComponent();
            SetMainWindowRef(Main);
            Title = SupplierCode.Name;
        }
        internal override void TabClosing(ref bool cancel)
        {
            cancel = false;

        }

    }
}
