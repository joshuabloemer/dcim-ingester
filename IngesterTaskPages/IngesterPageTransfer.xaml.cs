using dcim_ingester.Routines;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using static dcim_ingester.Routines.Helpers;

namespace dcim_ingester.IngesterTaskPages
{
    public partial class IngesterPageTransfer : Page
    {
        private Guid Volume;
        private bool DeleteAfter;
        private List<string> FilesToTransfer;
        private long TotalTransferSize;

        private Thread TransferThread;
        private int TransferCount = 0;

        public event EventHandler<PageDismissEventArgs> OnPageDismiss;

        public IngesterPageTransfer(Guid volume,
            bool deleteAfter, List<string> filesToTransfer, long totalTransferSize)
        {
            Volume = volume;
            DeleteAfter = deleteAfter;
            FilesToTransfer = filesToTransfer;
            TotalTransferSize = totalTransferSize;

            InitializeComponent();
        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            TransferThread = new Thread(delegate () { TransferFiles(); });
            TransferThread.Start();
        }

        private void TransferFiles()
        {
            string totalSizeString = FormatBytes(TotalTransferSize);

            foreach (string file in FilesToTransfer)
            {
                float percentage
                    = ((float)(TransferCount) / FilesToTransfer.Count) * 100;

                Application.Current.Dispatcher.Invoke(delegate ()
                {
                    TextBlockTransferring.Text = string.Format(
                        "Transferring file {0} of {1} ({2})", TransferCount + 1,
                        FilesToTransfer.Count, totalSizeString);

                    TextBlockPercentage.Text
                        = string.Format("{0}%", Math.Round(percentage));
                    ProgressBarA.Value = percentage;
                });

                try
                {
                    // Transfer the file

                    try
                    {
                        // if (DeleteAfter) Delete file
                    }
                    catch (Exception)
                    {

                    }
                }
                catch (Exception)
                {
                    TextBlockTransferring.Text = string.Format(
                        "Transfer from {0} failed", GetVolumeLabel(Volume));

                    // ButtonRetry.Content = "Dismiss";
                }

                TransferCount += 1;
                Thread.Sleep(25);
            }

            Application.Current.Dispatcher.Invoke(delegate ()
            { TransferComplete(false); });
        }
        private void TransferComplete(bool cancelled)
        {
            if (!cancelled)
            {
                TextBlockTransferring.Text = string.Format(
                    "Transfer from {0} completed", GetVolumeLabel(Volume));
            }
            else
            {
                TextBlockTransferring.Text = string.Format(
                    "Transfer from {0} cancelled", GetVolumeLabel(Volume));
            }

            ButtonCancel.Content = "Dismiss";
            ButtonView.Visibility = Visibility.Visible;
            ButtonEject.Visibility = Visibility.Visible;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (ButtonCancel.Content.ToString() == "Cancel")
            {
                TransferThread.Abort();
                TransferComplete(true);
            }
            else
            {
                OnPageDismiss?.Invoke(this, new
                    PageDismissEventArgs("IngesterPageTransfer.Dismiss"));
            }
        }
        private void ButtonView_Click(object sender, RoutedEventArgs e)
        {
            OnPageDismiss?.Invoke(this, new
                PageDismissEventArgs("IngesterPageTransfer.Dismiss"));
        }
        private void ButtonEject_Click(object sender, RoutedEventArgs e)
        {
            OnPageDismiss?.Invoke(this, new
                PageDismissEventArgs("IngesterPageTransfer.Dismiss"));
        }
    }
}
