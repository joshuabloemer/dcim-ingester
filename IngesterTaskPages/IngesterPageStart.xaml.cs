using dcim_ingester.Routines;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using static dcim_ingester.Routines.Helpers;

namespace dcim_ingester.IngesterTaskPages
{
    public partial class IngesterPageStart : Page
    {
        private Guid Volume;
        private List<string> FilesToTransfer;
        private long TotalTransferSize;

        public event EventHandler<PageDismissEventArgs> PageDismissed;

        public IngesterPageStart(
            Guid volumeId, List<string> filesToTransfer, long totalTransferSize)
        {
            Volume = volumeId;
            FilesToTransfer = filesToTransfer;
            TotalTransferSize = totalTransferSize;

            InitializeComponent();
        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LabelA.Text = string.Format("DCIM device {0} contains {1} media files "
                + "({2}). Do you want to transfer them?", GetVolumeLabel(Volume),
                FilesToTransfer.Count, FormatBytes(TotalTransferSize));
            CheckBoxDelete.IsChecked = Properties.Settings.Default.DeleteAfter;
        }

        private void ButtonYes_Click(object sender, RoutedEventArgs e)
        {
            PageDismissEventArgs eventArgs
                = new PageDismissEventArgs("IngesterPageStart.Transfer");

            if (CheckBoxDelete.IsChecked == true)
            {
                Properties.Settings.Default.DeleteAfter = true;
                Properties.Settings.Default.Save();
                eventArgs.Extra = "delete";
            }

            PageDismissed?.Invoke(this, eventArgs);
        }
        private void ButtonNo_Click(object sender, RoutedEventArgs e)
        {
            PageDismissed?.Invoke(this,
                new PageDismissEventArgs("IngesterPageStart.Cancel"));
        }
    }
}
