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
        private Guid VolumeID;
        private bool DeleteAfter;
        private List<string> FilesToTransfer;
        private long TotalSize;

        public event EventHandler<PageDismissEventArgs> OnPageDismiss;

        public IngesterPageTransfer(Guid volumeId,
            bool deleteAfter, List<string> filesToTransfer, long totalSize)
        {
            VolumeID = volumeId;
            DeleteAfter = deleteAfter;
            FilesToTransfer = filesToTransfer;
            TotalSize = totalSize;

            InitializeComponent();
        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            new Thread(delegate () { TransferFiles(); }).Start();
        }

        private void TransferFiles()
        {
            string totalSizeString = FormatBytes(TotalSize);

            for (int i = 0; i < FilesToTransfer.Count; i++)
            {
                float percentage
                    = ((float)(i + 1) / FilesToTransfer.Count) * 100;
                Application.Current.Dispatcher.Invoke(delegate ()
                {
                    TextBlockTransferring.Text = string.Format(
                        "Transferring file {0} of {1} ({2})", i + 1,
                        FilesToTransfer.Count, totalSizeString);

                    TextBlockPercentage.Text = string.Format("{0}%",
                        Math.Round(percentage));
                    ProgressBarA.Value = percentage;

                    // Transfer the file
                    // if (DeleteAfter) Delete file
                });

                Thread.Sleep(25);
            }

            Application.Current.Dispatcher.Invoke(delegate ()
            {
                TextBlockTransferring.Text = string.Format(
                    "Transferred {0} files ({1}) from {2}", FilesToTransfer.Count,
                    totalSizeString, GetVolumeLabel(VolumeID));
            });
        }
    }
}
