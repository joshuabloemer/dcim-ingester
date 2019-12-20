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
        private readonly string VolumeLabel;
        private readonly List<string> FilesToTransfer;
        private readonly long TotalTransferSize;
        private readonly Action<TaskStatus> SetStatus;

        private Thread TransferThread;
        private bool ShouldCancelTransfer = false;
        private int LastFileTransferred = -1;
        private string FirstFileDestination = null;
        private int DuplicateCounter = 0;

        public event EventHandler<PageDismissEventArgs> PageDismissed;

        public TaskPageTransfer(string volumeLabel, List<string> filesToTransfer,
            long totalTransferSize, Action<TaskStatus> setStatus)
        {
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
            for (int i = LastFileTransferred + 1; i < FilesToTransfer.Count; i++)
            {
                string file = FilesToTransfer[i];
                float percentage =
                    ((float)(LastFileTransferred + 1) / FilesToTransfer.Count) * 100;

                // Update interface to reflect progress
                Application.Current.Dispatcher.Invoke(delegate ()
                {
                    LabelCaption.Text =
                        string.Format("Transferring file {0} of {1} ({2}) from {3}",
                        LastFileTransferred + 2, FilesToTransfer.Count,
                        totalSizeString, VolumeLabel);

                    LabelPercentage.Content =
                        string.Format("{0}%", Math.Round(percentage));
                    ProgressBarA.Value = percentage;
                });

                if (!TransferFile(file))
                {
                    // Update interface to reflect failure
                    Application.Current.Dispatcher.Invoke(delegate ()
                    { TransferFilesFailed(); });
                    return;
                }

                LastFileTransferred++;
                if (ShouldCancelTransfer)
                {
                    if (LastFileTransferred == FilesToTransfer.Count - 1)
                    {
                        // Update interface to reflect completion
                        Application.Current.Dispatcher.Invoke(delegate ()
                        {
                            TransferFilesCompleted();
                            ButtonCancel.IsEnabled = true;
                        });
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(delegate ()
                        { TransferFilesCancelled(); });
                        return;
                    }
                }
            }

            // Update interface to reflect completion
            Application.Current.Dispatcher.Invoke(delegate ()
            { TransferFilesCompleted(); });
        }
        private void TransferFilesCompleted()
        {
            SetStatus(TaskStatus.Completed);
            LabelCaption.Text =
                string.Format("Transfer from {0} complete", VolumeLabel);

            ButtonCancel.Content = "Dismiss";
            ButtonView.Visibility = Visibility.Visible;
        }
        private void TransferFilesFailed()
        {
            SetStatus(TaskStatus.Failed);
            LabelCaption.Text =
                string.Format("Transfer from {0} failed", VolumeLabel);

            ButtonCancel.Content = "Dismiss";
            ButtonRetry.Visibility = Visibility.Visible;

            if (LastFileTransferred > -1)
                ButtonView.Visibility = Visibility.Visible;
        }
        private void TransferFilesCancelled()
        {
            SetStatus(TaskStatus.Cancelled);
            LabelCaption.Text =
                string.Format("Transfer from {0} cancelled", VolumeLabel);

            ButtonCancel.Content = "Dismiss";
            ButtonCancel.IsEnabled = true;
            ButtonRetry.Visibility = Visibility.Visible;

            if (LastFileTransferred > -1)
                ButtonView.Visibility = Visibility.Visible;
        }
        private bool TransferFile(string filePath)
        {
            try
            {
                DateTime? timeTaken = GetTimeTaken(filePath);

                // Determine file destination based on time taken
                string newImageDir;
                if (timeTaken != null)
                {
                    newImageDir = Path.Combine(Properties.Settings.Default.Endpoint,
                        string.Format("{0:D4}\\{0:D4}-{1:D2}-{2:D2} -- Untitled",
                        timeTaken?.Year, timeTaken?.Month, timeTaken?.Day));
                }
                else
                {
                    newImageDir = Path.Combine(Properties.Settings.Default.Endpoint,
                        "Unsorted");
                }

                Directory.CreateDirectory(newImageDir);
                string newFilePath =
                    Path.Combine(newImageDir, Path.GetFileName(filePath));

                bool isDuplicate = false;
                int duplicateCounter = 1;

                // Add number to file name if file already exists
                while (File.Exists(newFilePath))
                {
                    newFilePath = Path.Combine(newImageDir,
                        Path.GetFileNameWithoutExtension(filePath) + string.Format(
                            " ({0})", duplicateCounter) + Path.GetExtension(filePath));

                    isDuplicate = true;
                    duplicateCounter++;
                }

                File.Copy(filePath, newFilePath);
                if (Properties.Settings.Default.ShouldDeleteAfter)
                    File.Delete(filePath);

                if (isDuplicate)
                {
                    DuplicateCounter++;
                    Application.Current.Dispatcher.Invoke(delegate ()
                    {
                        LabelSubCaption.Text = string.Format(
                            "{0} duplicate file names (number appended)",
                            DuplicateCounter);
                    });
                }

                if (FirstFileDestination == null)
                    FirstFileDestination = newImageDir;
            }
            catch { return false; }

            return true;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (ButtonCancel.Content.ToString() == "Cancel")
            {
                ButtonCancel.IsEnabled = false;
                ButtonCancel.Content = "Cancelling";
                ShouldCancelTransfer = true;
            }
            else
            {
                PageDismissed?.Invoke(this,
                    new PageDismissEventArgs("IngesterPageTransfer.Dismiss"));
            }
        }
        private void ButtonRetry_Click(object sender, RoutedEventArgs e)
        {
            ShouldCancelTransfer = false;

            ButtonCancel.Content = "Cancel";
            ButtonRetry.Visibility = Visibility.Collapsed;
            ButtonView.Visibility = Visibility.Collapsed;

            // Start the transfer thread again
            Page_Loaded(this, new RoutedEventArgs());
        }
        private void ButtonView_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = FirstFileDestination,
                UseShellExecute = true,
                Verb = "open"
            });
        }
    }
}
