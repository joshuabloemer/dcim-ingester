using DCIMIngester.Routines;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using static DCIMIngester.Routines.Helpers;

namespace DCIMIngester.IngesterTaskPages
{
    public partial class TaskPageStart : Page
    {
        private readonly string VolumeLabel;
        private readonly List<string> FilesToTransfer;
        private readonly long TotalTransferSize;

        public event EventHandler<PageDismissEventArgs> PageDismissed;

        public TaskPageStart(string volumeLabel, List<string> filesToTransfer,
            long totalTransferSize)
        {
            VolumeLabel = volumeLabel;
            FilesToTransfer = filesToTransfer;
            TotalTransferSize = totalTransferSize;

            InitializeComponent();
        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LabelCaption.Text = string.Format("DCIM device {0} contains {1} media "
                + "files ({2}). Do you want to transfer them?", VolumeLabel,
                FilesToTransfer.Count, FormatBytes(TotalTransferSize));

            CheckBoxDelete.IsChecked = Properties.Settings.Default.ShouldDeleteAfter;
        }

        private void ButtonYes_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings
                .Default.ShouldDeleteAfter = (bool)CheckBoxDelete.IsChecked;
            Properties.Settings.Default.Save();

            PageDismissed?.Invoke(this,
                new PageDismissEventArgs("IngesterPageStart.Transfer"));
        }
        private void ButtonNo_Click(object sender, RoutedEventArgs e)
        {
            PageDismissed?.Invoke(this,
                new PageDismissEventArgs("IngesterPageStart.Cancel"));
        }
    }
}
