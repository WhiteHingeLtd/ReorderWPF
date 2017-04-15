namespace ReorderWPF.UserControls
{
    using System.Collections.Generic;
    using System.Windows.Controls;
    using ReorderWPF.Pages;
    using WHLClasses;

    /// <summary>
    /// Interaction logic for Packsizes.xaml
    /// </summary>
    public partial class Packsizes : UserControl
    {
        public static List<DataItemDetails> PacksizesList;

        public Packsizes(SkuCollection children, DataItem sku)
        {
            InitializeComponent();
            PacksizesList = sku.Packsizes;
            PacksizesDataGrid.DataContext = new DataItemDetails();

            foreach (var child in children)
            {
                var item = DataItemDetails.NewDataItemDetails(child);
               // PacksizesList.Add(item);
            }
            this.PacksizesDataGrid.ItemsSource = PacksizesList;
        }
    }
}
