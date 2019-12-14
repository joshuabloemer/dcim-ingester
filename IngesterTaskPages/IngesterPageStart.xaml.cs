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
        private Guid VolumeID;
        private List<string> FilesToTransfer;
        private long TotalSize;

        public event EventHandler<PageDismissEventArgs> OnPageDismiss;

        public IngesterPageStart(
            Guid volumeId, List<string> filesToTransfer, long totalSize)
        {
            VolumeID = volumeId;
            FilesToTransfer = filesToTransfer;
            TotalSize = totalSize;

            InitializeComponent();
        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LabelA.Text = string.Format("DCIM device {0} contains {1} files ({2}). "
                + "Do you want to transfer them?", GetVolumeLabel(VolumeID),
                FilesToTransfer.Count, FormatBytes(TotalSize));
            CheckBoxDelete.IsChecked = Properties.Settings.Default.DeleteAfter;
        }

        private void ButtonYes_Click(object sender, RoutedEventArgs e)
        {
            PageDismissEventArgs eventArgs
                = new PageDismissEventArgs("IngesterPageStart.Yes");

            if (CheckBoxDelete.IsChecked == true)
            {
                Properties.Settings.Default.DeleteAfter = true;
                Properties.Settings.Default.Save();
                eventArgs.Extra = "delete";
            }

            OnPageDismiss?.Invoke(this, eventArgs);
        }
        private void ButtonNo_Click(object sender, RoutedEventArgs e)
        {
            OnPageDismiss?.Invoke(
                this, new PageDismissEventArgs("IngesterPageStart.No"));
        }
    }
}
