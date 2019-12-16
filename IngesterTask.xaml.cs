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
        public Guid Volume { get; private set; }
        public TaskStatus Status { get; private set; }

        private List<string> filesToTransfer = new List<string>();
        public IReadOnlyCollection<string> FilesToTransfer
        {
            get { return filesToTransfer.AsReadOnly(); }
        }

        private long TotalTransferSize = 0;

        public event EventHandler<TaskDismissEventArgs> TaskDismissed;

        public IngesterTask(Guid volume)
        {
            Volume = volume;
            Status = TaskStatus.Waiting;
        }


        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            IngesterPageStart startPage = new IngesterPageStart(
                Volume, filesToTransfer, TotalTransferSize);
            startPage.PageDismissed += IngesterPage_PageDismissed;
            FrameA.Navigate(startPage);
        }

        private void IngesterPage_PageDismissed(object sender, PageDismissEventArgs e)
        {
            switch (e.DismissMessage)
            {
                case "IngesterPageStart.Transfer":
                    Status = TaskStatus.Transferring;
                    bool deleteAfter = e.Extra == "delete" ? true : false;

                    IngesterPageTransfer transferPage = new IngesterPageTransfer(
                        Volume, deleteAfter, filesToTransfer, TotalTransferSize);
                    transferPage.PageDismissed += IngesterPage_PageDismissed;
                    FrameA.Navigate(transferPage);
                    break;

                case "IngesterPageStart.Cancel":
                case "IngesterPageTransfer.Dismiss":
                    TaskDismissed?.Invoke(this, new TaskDismissEventArgs(this));
                    break;
            }
        }

        public void ComputeTransferList()
        {
            try
            {
                string[] directories = Directory.GetDirectories(
                    Path.Combine(GetVolumeLetter(Volume), "DCIM"));
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

                        string extension = Path.GetExtension(file).ToLower();

                        // Only include files with supported extension
                        if (extension == ".jpg" || extension == ".jpeg"
                            || extension == ".cr2")
                        {
                            TotalTransferSize += new FileInfo(file).Length;
                            filesToTransfer.Add(file);
                        }
                    }
                }
            }
            catch
            {
                filesToTransfer.Clear();
                TotalTransferSize = 0;
            }
        }
    }
}
