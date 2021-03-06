﻿namespace ReorderWPF.Pages
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using OxyPlot.Wpf;
    using ReorderWPF.CustomControls;
    using ReorderWPF.UserControls;

    using WHLClasses;

    /// <summary>
    /// Interaction logic for SupplierData.xaml
    /// </summary>
    public partial class SupplierData : ThreadedPage
    {
        private Supplier _currentSupplier;
        public static SkuCollection SupplierSkuCollectionFull = new SkuCollection(true);
        private SkuCollection _supplierSkuCollection = new SkuCollection(true);
        private List<DataItem> SupplierDataList = new List<DataItem>();
        private List<DataItemDetails> _currentPacksizes = new List<DataItemDetails>();
        private SupplierOrderData CurrentSupplierOrder = new SupplierOrderData();
        private DataItem CurrentSelectedItem = new DataItem();
        private ReorderSupplier CurrentReorderSupplier = null;

        private ConcurrentBag<WhlSKU> ListOfUnloadedSkus = new ConcurrentBag<WhlSKU>();
        internal ObservableCollection<DataItemDetails> CurrentSelectedPacksizes = new ObservableCollection<DataItemDetails>();

        private bool LoadLowStock = false;
        private bool LoadSupplierLow = false;
        private bool LoadDiscontinued = false;
        private bool LoadUnlisted = false;
        private bool LoadNoSales = true;
        private bool LoadPrimaryOnly = true;
        public SupplierData(MainWindow Main, Supplier SupplierCode)
        {
            InitializeComponent();
            SetMainWindowRef(Main);
            Title = SupplierCode.Name;
            _currentSupplier = SupplierCode;
            SupplierSkuCollectionFull = Main.DataSkus;
            _supplierSkuCollection = Main.DataSkusMixDown;
            LoadSupplierData();
            UpdateBooleans();
            SupplierNameBlock.Text = SupplierCode.Name;
            Stopwatch asd = new Stopwatch();
            asd.Start();
            Misc.OperationDialog("Preparing " + SupplierCode.Name,PrepareDataGrid);
            asd.Stop();
            Console.WriteLine(asd.ElapsedMilliseconds.ToString());
            RenderDataGrid();
        }

        private void LoadSupplierData()
        {
            CurrentReorderSupplier = new ReorderSupplier(_currentSupplier, SupplierSkuCollectionFull);
            if (CurrentReorderSupplier.LastOrderGuid == Guid.Empty)
            {
                LastOrderDateBlock.Text = "";
                ShowLastOrderButton.IsEnabled = false;
            }
            else
            {
                LastOrderDateBlock.Text = CurrentReorderSupplier.LastOrder.ToShortDateString(); 
                ShowLastOrderButton.IsEnabled = true;
            }
            
            LeadDaysTextBox.Text = CurrentReorderSupplier.LeadDays.ToString();
            if (CurrentReorderSupplier.CartonDiscount) CartonDiscount.Text = "Yes";
            else CartonDiscount.Text = "No";
            ReorderPercentageTextBox.Text = Convert.ToString((CurrentReorderSupplier.ReorderAtPercentage) * 100) + "%";

        }

        internal override void TabClosing(ref bool cancel)
        {
            cancel = false;

        }

        internal void PrepareDataGrid(object sender, DoWorkEventArgs e)
        {
            SupplierDataList.Clear();
            var Worker = sender as BackgroundWorker;
            var CurrentColl = _supplierSkuCollection.SearchBySuppName(_currentSupplier.Code).ExcludeStatus("Dead");
            var SupplierDataBag = new ConcurrentBag<DataItem>();
            var parallelopts = new ParallelOptions {MaxDegreeOfParallelism = 4};
            Parallel.ForEach(CurrentColl, parallelopts,(sku) =>
            {
                if (int.Parse(sku.SalesData.EightWeekAverage.ToString()) == 0 && LoadNoSales != true)
                {
                    ListOfUnloadedSkus.Add(sku);
                    return;
                }
                if (sku.GetPrimarySupplier().Name != _currentSupplier.Code && LoadPrimaryOnly) return;

                var newItem = DataItem.DataItemNew(sku);
                
                SupplierDataBag.Add(newItem);
                Worker.ReportProgress((SupplierDataBag.Count / CurrentColl.Count)*100);
            });
            SupplierDataList.AddRange(SupplierDataBag);
        }

        private void RenderDataGrid()
        {

            SupplierDataGrid.ItemsSource = SupplierDataList;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateBooleans();
            Misc.OperationDialog("Preparing " + _currentSupplier.Name, PrepareDataGrid);
            RenderDataGrid();
        }

        private void UpdateBooleans()
        {
            try
            {
               
                if (LowStockCheck.IsChecked == true) LoadLowStock = true;
                else LoadLowStock = false;

                if (SupplierLowCheck.IsChecked == true) LoadSupplierLow = true;
                else LoadSupplierLow = false;
                if (DiscontCheck.IsChecked == true) LoadDiscontinued = true;
                else LoadDiscontinued = false;
                if (UnlistedCheck.IsChecked == true) LoadUnlisted = true;
                else LoadUnlisted = false;
                if (NoSalesCheck.IsChecked == true)  LoadNoSales = true;
                else LoadNoSales = false;
                if (PrimaryOnlyCheck.IsChecked == true) LoadPrimaryOnly = true;
                else LoadPrimaryOnly = false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void UpdateCurrentOrderData()
        {
            SkusCurrentOrder.Text = "Skus: " + CurrentSupplierOrder.LinesOfStock.ToString();
            TotalItemsCurrentOrder.Text = "Items: " + CurrentSupplierOrder.SkuOrderList.Sum(v => v.Value);
        }

        private void AddToCurrentOrder(WhlSKU Item, int Quantity)
        {
            if (CurrentSupplierOrder.SkuOrderList.ContainsKey(Item))
            {
                CurrentSupplierOrder.SkuOrderList[Item] += Quantity;
            }
            else
            {
                CurrentSupplierOrder.SkuOrderList.Add(Item, Quantity);
            }
        }

        private void SupplierDataGrid_RowDetailsVisibilityChanged(object sender, DataGridRowDetailsEventArgs e)
        {
            if (CurrentSelectedItem != e.Row.Item)
            {
                CurrentSelectedItem = e.Row.Item as DataItem;
                var asd = e.DetailsElement as Grid;
                var packsize = FindVisualChild<StackPanel>(asd);
                var DeliveryNoteBox = FindVisualChild<TextBox>(asd);

                var Model1 = FindVisualChild<PlotView>(asd);
                try
                {
                    Model1.Model = (e.Row.Item as DataItem).SalesGraph;
                    DeliveryNoteBox.Text = (e.Row.Item as DataItem).SkuData.DeliveryNote;

                }
                catch (Exception exception)
                {
                    Model1.Visibility = Visibility.Collapsed;
                    Console.WriteLine(exception);
                }
                
                CurrentSelectedPacksizes.Clear();
                packsize.Children.Add(new Packsizes(CurrentSelectedItem.Children, CurrentSelectedItem));

                // foreach (DataItemDetails Pack in (e.Row.Item as DataItem).Packsizes)
                // {
                //    CurrentSelectedPacksizes.Add(Pack);
                // }
            }
        }

        public static childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }
        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            //get parent item
            var ParentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (ParentObject == null) return null;

            //check if the parent matches the type we're looking for
            T parent = ParentObject as T;
            if (parent != null) return parent;
            else return FindParent<T>(ParentObject); //Intentional Recursive method
        }

        public TextBlock SelectSiblingTextBlockByName(DependencyObject Sibling, string Name)
        {
            var Parent = VisualTreeHelper.GetParent(Sibling) as Grid;
            bool IsCorrect = false;
            try
            {
                while (!IsCorrect)
                {
                    var TextBlock = FindVisualChild<TextBlock>(Parent);
                    if (TextBlock == null) throw new NullReferenceException(); // reached the end of the tree

                    if (TextBlock.Name == Name)
                    {
                        IsCorrect = true;
                        return TextBlock;
                    }
                }
               
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
            return null;
        }
        private void SaveDeliveryNotesButton_Click_1(object sender, RoutedEventArgs e)
        {
            var NoteBoxGrid = VisualTreeHelper.GetParent(sender as Button);
            var tBox = FindVisualChild<TextBox>(NoteBoxGrid);
            // var NotesToSave = SupplierDataGrid.RowDetailsTemplate.FindName("DeliveryNoteRtf", FindParent<RowDetailsTemplate>(sender as Button)) as TextBox;
            CurrentSelectedItem.SkuData.DeliveryNote = tBox.Text;
            CurrentSelectedItem.SkuData.SetDeliveryNote(tBox.Text, MainWindowRef.UserEmployee.AuthenticatedUser);
        }

        private void AddToOrderButton_Click(object sender, RoutedEventArgs e)
        {
            var OrderQuantityGrid = VisualTreeHelper.GetParent(sender as Button);
            var tBox = FindVisualChild<TextBox>(OrderQuantityGrid);
        }
    }
}
