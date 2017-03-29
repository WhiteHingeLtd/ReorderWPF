using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Forms;


namespace ReorderWPF.CustomControls
{
    public abstract class ThreadedPage : Page
    {

        internal virtual bool SupportsMultipleTabs()
        {
            return true;
        }

        internal DispatcherTimer Timer;
        internal BackgroundWorker Worker;
        TextBlock Status;
        TextBlock ClockBlock;

        internal ThreadedPage()
        {

            // Add any initialization after the InitializeComponent() call.
            Timer = new DispatcherTimer(new TimeSpan(0, 0, 0, 1), DispatcherPriority.Normal, TimerTick, base.Dispatcher);
            SetUpWorker();
        }

        private void SetUpWorker()
        {
            Worker = new BackgroundWorker();
            Worker.WorkerReportsProgress = true;
            Worker.DoWork += WorkerHandler;
            Worker.ProgressChanged += WorkerProgress;
        }

        public void TimerTick(object sender, EventArgs e)
        {
            if (ClockBlock == null)
                ClockBlock = this.Template.FindName("ClockBlock", this) as TextBlock;
            if ((ClockBlock != null))
                ClockBlock.Text = DateTime.Now.ToString("HH:mm:ss");

        }

        internal void UpdateStatus(string NewStatus)
        {
            if (Status == null)
                Status = this.Template.FindName("StatusBlock", this) as TextBlock;

            Status.Text = NewStatus;
            //StatusText..text = NewStatus
        }


        bool WorkerRunning = false;
        internal void ProcessInBackground(Action Process)
        {
            WorkerRunning = false;
            Worker.RunWorkerAsync(Process);
            while (Worker.IsBusy | WorkerRunning)
            {
                Application.DoEvents();
                Thread.Sleep(16);
            }
        }

        private void WorkerHandler(object sender, DoWorkEventArgs e)
        {
            ((Action)e.Argument).Invoke();
            WorkerRunning = false;
        }

        internal virtual void WorkerProgress(object sender, ProgressChangedEventArgs e)
        {
            UpdateStatus(e.UserState.ToString());
        }

        internal abstract void TabClosing(ref bool Cancel);

    }
}
