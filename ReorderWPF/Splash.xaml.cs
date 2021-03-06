﻿
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using WHLClasses;
using WHLClasses.Authentication;

namespace ReorderWPF
{
    /// <summary>
    /// Interaction logic for Splash.xaml
    /// </summary>
    public partial class Splash : Window
    {
        private BackgroundWorker Worker = new BackgroundWorker();
        internal MainWindow HomeRef = null;
        internal void SplashLoad()
        {
            Worker.DoWork += SplashProxy;
            Worker.ProgressChanged += ProxyTextUpdate;
            Worker.RunWorkerCompleted += ProxyFinished;
            Worker.WorkerReportsProgress = true;
            Worker.RunWorkerAsync();
        }

        private void LoadAssemblies(Assembly Assembly)
        {
            foreach (AssemblyName name in Assembly.GetReferencedAssemblies())
            {
                Worker.ReportProgress(0, "Loading " + name.Name.ToString());
                Thread.Sleep(25);
                if (!AppDomain.CurrentDomain.GetAssemblies().Any(x => x.FullName == name.FullName))
                {
                    LoadAssemblies(Assembly.Load(name));
                    Console.WriteLine(name.FullName);
                }

            }
        }

        internal void SplashProxy(object sender, DoWorkEventArgs e)
        {
            Worker.ReportProgress(0, "Loading Assemblies");
            LoadAssemblies(this.GetType().Assembly);
            GenericDataController loader = new GenericDataController();
            Worker.ReportProgress(0, "Loading Item Data");
            HomeRef.DataSkus = loader.SmartSkuCollLoad();
            Worker.ReportProgress(0,"Making Mixdown");
            HomeRef.DataSkusMixDown = HomeRef.DataSkus.MakeMixdown();
            Worker.ReportProgress(0, "Loading Employee Data");
            HomeRef.DataEmployees = new EmployeeCollection();
            Worker.ReportProgress(0, "Loading Supplier Collection");
            HomeRef.SupplierCollection = new SupplierCollection();
            HomeRef.SupplierCollection.SortItemsBySupplier(null,null,HomeRef.DataSkus);
            foreach (Supplier Supp in HomeRef.SupplierCollection)
            {
                Worker.ReportProgress(0,"Mixing " + Supp.Name);
                Supp.MakeMixdown();
            }
            Thread.Sleep(20);
            Worker.ReportProgress(0, "Preparing Authentication");
            HomeRef.UserEmployee = new AuthClass();


        }

        private void UpdateText(string NewText)
        {
            LoadingStatsText.Text += Environment.NewLine + NewText;
        }

        internal void ProxyTextUpdate(object sender, ProgressChangedEventArgs e)
        {
            UpdateText(e.UserState.ToString());
        }

        internal void ProxyFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            UpdateText("Requesting User Login");
            try
            {
                UpdateText("Logged in: " + HomeRef.UserEmployee.AuthenticatedUser.FullName);
            }
            catch (NullReferenceException)
            {
                HomeRef.UserEmployee = new AuthClass();
                UpdateText("Logged in: " + HomeRef.UserEmployee.AuthenticatedUser.FullName);
            }
            finally
            {
                
                Close();
            }
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SplashLoad();
        }
    }
}
