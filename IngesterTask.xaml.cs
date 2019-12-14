using dcim_ingester.IngesterTaskPages;
using dcim_ingester.Routines;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using static dcim_ingester.Routines.Helpers;

namespace dcim_ingester
{
    public partial class IngesterTask : UserControl
    {
        public Guid VolumeID { get; private set; }
        public TaskStatus Status { get; private set; }

        private List<string> FilesToTransfer = new List<string>();
        private long TotalSize = 0;

        public event EventHandler<TaskDismissEventArgs> OnTaskDismiss;

        public IngesterTask(Guid volumeId)
        {
            VolumeID = volumeId;
            Status = TaskStatus.Waiting;

            InitializeComponent();
        }


        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            IngesterPageStart startPage = new IngesterPageStart(
                VolumeID, FilesToTransfer, TotalSize);
            startPage.OnPageDismiss += IngesterPage_OnPageDismiss;
            FrameA.Navigate(startPage);
        }

        private void IngesterPage_OnPageDismiss(object sender, PageDismissEventArgs e)
        {
            switch (e.DismissMessage)
            {
                case "IngesterPageStart.Yes":
                    Status = TaskStatus.Transferring;
                    bool deleteAfter = e.Extra == "delete" ? true : false;

                    IngesterPageTransfer transferPage = new IngesterPageTransfer(
                        VolumeID, deleteAfter, FilesToTransfer, TotalSize);
                    transferPage.OnPageDismiss += IngesterPage_OnPageDismiss;
                    FrameA.Navigate(transferPage);
                    break;

                case "IngesterPageStart.No":
                    OnTaskDismiss?.Invoke(this, new TaskDismissEventArgs(this));
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


        public void ComputeTransferList()
        {
            string[] directories = Directory.GetDirectories(
                Path.Combine(GetVolumeLetter(VolumeID), "DCIM"));
            if (directories.Length == 0) return;

            foreach (string directory in directories)
            {
                // Ignore directory names not conforming to DCF spec
                if (!Regex.IsMatch(Path.GetFileName(
                    directory), "^[0-9]{3}[0-9a-zA-Z]{5}$")) continue;

                string[] files = Directory.GetFiles(directory);

                foreach (string file in files)
                {
                    // Ignore file names not conforming to DCF spec
                    if (!Regex.IsMatch(Path.GetFileNameWithoutExtension(
                        file), "^[0-9a-zA-Z_]{4}[0-9]{4}$")) continue;

                    TotalSize += new FileInfo(file).Length;
                    FilesToTransfer.Add(file);
                }
            }
        }
    }
}
