using System;
using System.Collections.Generic;
using System.Deployment.Internal;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using WHLClasses;

namespace ReorderWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public EmployeeCollection Data_Employees;
        internal WHLClasses.Authentication.AuthClass User_Employee;
        internal SkuCollection Data_Skus;
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
        }
    }
}
