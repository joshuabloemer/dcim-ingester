using dcim_ingester.IngesterTaskPages;
using System;
using System.Windows;
using System.Windows.Controls;
using static dcim_ingester.Helpers;

namespace dcim_ingester
{
    public partial class IngesterTask : UserControl
    {
        public Guid VolumeID { get; private set; }
        public TaskStatus Status = TaskStatus.None;

        public enum TaskStatus { None, Waiting, Transferring, Completed, Failed };

        public IngesterTask(Guid volumeId)
        {
            InitializeComponent();
            VolumeID = volumeId;
        }


        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            IngesterPageStart startPage = new IngesterPageStart(GetVolumeLabel(VolumeID));
            startPage.OnPageDismiss += IngesterPageStart_OnPageDismiss;
            FrameA.Navigate(startPage);
        }

        private void IngesterPageStart_OnPageDismiss(object sender, PageDismissEventArgs e)
        {
            switch (e.DismissMessage)
            {
                case "IngesterPageStart.Yes":
                    FrameA.Navigate(new IngesterPageTransfer());
                    break;

                case "IngesterPageStart.No":
                    Environment.Exit(0);
                    break;

                case "IngesterPageTransfer.Complete":
                    break;

                case "IngesterPageTransfer.Fail":
                    break;

                case "IngesterPageComplete.Dismiss":
                    break;

                case "IngesterPageComplete.Explore":
                    break;

                case "IngesterPageFail.Dismiss":
                    break;
            }
        }
    }
}
