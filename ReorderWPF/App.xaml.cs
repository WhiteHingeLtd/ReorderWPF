using System.Windows;
using System.Windows.Threading;
namespace ReorderWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception.Message == "\'EditItem\' is not allowed for this view.")
            {
                e.Handled = true;
            }
            else
            {
                WHLClasses.Reporting.ErrorReporting.ReportException(e.Exception, false);
                e.Handled = true;
            }

        }

    }
}
