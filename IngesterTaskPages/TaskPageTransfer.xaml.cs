using DCIMIngester.Routines;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using static DCIMIngester.Routines.Helpers;
using static DCIMIngester.Routines.IngesterTask;

namespace DCIMIngester.IngesterTaskPages
{
    public partial class TaskPageTransfer : Page
    {
        private string VolumeLetter;
        private string VolumeLabel;
        private List<string> FilesToTransfer;
        private long TotalTransferSize;
        private Action<TaskStatus> SetStatus;

        private Thread TransferThread;
        private int TransferCount = 0;
        private string DirectoryToView = null;

        public event EventHandler<PageDismissEventArgs> PageDismissed;

        public TaskPageTransfer(string volumeLetter, string volumeLabel,
            List<string> filesToTransfer, long totalTransferSize,
            Action<TaskStatus> setStatus)
        {
            VolumeLetter = volumeLetter;
            VolumeLabel = volumeLabel;
            FilesToTransfer = filesToTransfer;
            TotalTransferSize = totalTransferSize;
            SetStatus = setStatus;

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
                    = ((float)TransferCount / FilesToTransfer.Count) * 100;

                // Update interface to reflect progress
                Application.Current.Dispatcher.Invoke(delegate ()
                {
                    LabelCaption.Text = string.Format(
                        "Transferring file {0} of {1} ({2}) from {3}",
                        TransferCount + 1, FilesToTransfer.Count,
                        totalSizeString, VolumeLabel);

                    LabelPercentage.Content
                        = string.Format("{0}%", Math.Round(percentage));
                    ProgressBarA.Value = percentage;
                });

                if (!TransferFile(file))
                {
                    // Update interface to reflect failure
                    Application.Current.Dispatcher.Invoke(delegate ()
                    {
                        SetStatus(TaskStatus.Failed);
                        LabelCaption.Text = string.Format(
                            "Transfer from {0} failed", VolumeLabel);

                        ButtonCancel.Content = "Dismiss";
                        if (DirectoryToView != null)
                            ButtonView.Visibility = Visibility.Visible;
                    });

                    return;
                }

                TransferCount++;
            }

            // Update interface to reflect completion
            Application.Current.Dispatcher.Invoke(delegate ()
            {
                SetStatus(TaskStatus.Completed);
                LabelCaption.Text = string.Format(
                    "Transfer from {0} complete", VolumeLabel);

                ButtonCancel.Content = "Dismiss";
                ButtonView.Visibility = Visibility.Visible;
            });

            if (Properties.Settings.Default.ShouldEjectAfter)
                EjectVolume();
        }
        private bool TransferFile(string filePath)
        {
            DateTime? timeTaken = null;
            try { timeTaken = GetTimeTaken(filePath); }
            catch { return false; }

            // Copy file to directory based on the time taken
            if (timeTaken != null)
            {
                string imageDir = Path.Combine(Properties.Settings.Default.Endpoint,
                    string.Format("{0:D4}\\{0:D4}-{1:D2}-{2:D2} -- Untitled",
                    timeTaken?.Year, timeTaken?.Month, timeTaken?.Day));

                try
                {
                    Directory.CreateDirectory(imageDir);
                    //File.Copy(filePath, imageDir);

                    if (DirectoryToView == null)
                        DirectoryToView = imageDir;

                    //if (Properties.Settings.Default.ShouldDeleteAfter)
                    //    File.Delete(filePath);
                }
                catch { return false; }
            }
            else
            {
                // Copy files without time taken to different folder
                string imageDir = Path.Combine(Properties.Settings.Default.Endpoint,
                    "Unsorted", Path.GetFileName(filePath));

                try
                {
                    Directory.CreateDirectory(imageDir);
                    //File.Copy(filePath, imageDir);

                    if (DirectoryToView == null)
                        DirectoryToView = imageDir;

                    //if (Properties.Settings.Default.ShouldDeleteAfter)
                    //    File.Delete(filePath);
                }
                catch { return false; }
            }

            return true;
        }
        private void EjectVolume()
        {

        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (ButtonCancel.Content.ToString() == "Cancel")
            {
                TransferThread.Abort();
                SetStatus(TaskStatus.Failed);

                // Update interface to reflect cancellation
                LabelCaption.Text = string.Format(
                    "Transfer from {0} cancelled", VolumeLabel);

                ButtonCancel.Content = "Dismiss";
                if (DirectoryToView != null)
                    ButtonView.Visibility = Visibility.Visible;
            }
            else
            {
                PageDismissed?.Invoke(this, new
                    PageDismissEventArgs("IngesterPageTransfer.Dismiss"));
            }
        }
        private void ButtonView_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = DirectoryToView,
                UseShellExecute = true,
                Verb = "open"
            });

            PageDismissed?.Invoke(this, new
                PageDismissEventArgs("IngesterPageTransfer.Dismiss"));
        }
    }
}
