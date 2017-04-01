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
using System.Windows.Shapes;
using WHLClasses;
namespace ReorderWPF.Dialogs
{
    /// <summary>
    /// Interaction logic for SearchOptionsDialog.xaml
    /// </summary>
    public partial class SearchOptionsDialog : Window
    {
        internal SkuCollection SearchCollection = new SkuCollection(true);
        public SearchOptionsDialog(SkuCollection SearchColl)
        {
            InitializeComponent();
            SearchCollection = SearchColl;
            foreach (WhlSKU sku in SearchCollection)
            {
                if (sku.SKU.Contains("xxxx")) continue;
                SkuBox.Items.Add(sku.Title.Label);

            }
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
