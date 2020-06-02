using DCIMIngester.Routines;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using static DCIMIngester.Ingester.IngesterTask;
using static DCIMIngester.Routines.Helpers;

namespace DCIMIngester.Ingester
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
        private int UnsortedCounter = 0;

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
                    if (VolumeLabel == "")
                    {
                        LabelCaption.Text =
                            string.Format("Transferring file {0} of {1} from unnamed volume",
                            LastFileTransferred + 2, FilesToTransfer.Count);
                    }
                    else
                    {
                        LabelCaption.Text =
                            string.Format("Transferring file {0} of {1} from '{2}'",
                            LastFileTransferred + 2, FilesToTransfer.Count,
                            VolumeLabel);
                    }

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

            if (VolumeLabel == "")
                LabelCaption.Text = string.Format("Transfer from unnamed volume complete");
            else LabelCaption.Text = string.Format("Transfer from '{0}' complete", VolumeLabel);

            LabelPercentage.Content = "100%";
            ProgressBarA.Value = 100;

            LabelSubCaption.Text = string.Format(
                "{0} renamed duplicates, {1} transferred to 'Unsorted' folder", DuplicateCounter,
                UnsortedCounter);

            ButtonCancel.Content = "Dismiss";
            ButtonCancel.IsEnabled = true;
            ButtonView.Visibility = Visibility.Visible;
        }
        private void TransferFilesFailed()
        {
            SetStatus(TaskStatus.Failed);

            if (VolumeLabel == "")
                LabelCaption.Text = string.Format("Transfer from unnamed volume failed");
            else LabelCaption.Text = string.Format("Transfer from '{0}' failed", VolumeLabel);

            ButtonCancel.Content = "Dismiss";
            ButtonCancel.IsEnabled = true;
            ButtonRetry.Visibility = Visibility.Visible;

            LabelSubCaption.Text = string.Format(
                "{0} renamed duplicates, {1} transferred to 'Unsorted' folder", DuplicateCounter,
                UnsortedCounter);

            if (LastFileTransferred > -1)
                ButtonView.Visibility = Visibility.Visible;
        }
        private void TransferFilesCancelled()
        {
            SetStatus(TaskStatus.Cancelled);

            if (VolumeLabel == "")
                LabelCaption.Text = string.Format("Transfer from unnamed volume cancelled");
            else LabelCaption.Text = string.Format("Transfer from '{0}' cancelled", VolumeLabel);

            ButtonCancel.Content = "Dismiss";
            ButtonCancel.IsEnabled = true;
            ButtonRetry.Visibility = Visibility.Visible;

            LabelSubCaption.Text = string.Format(
                "{0} renamed duplicates, {1} transferred to 'Unsorted' folder", DuplicateCounter,
                UnsortedCounter);

            if (LastFileTransferred > -1)
                ButtonView.Visibility = Visibility.Visible;
        }
        private bool TransferFile(string filePath)
        {
            try
            {
                DateTime? timeTaken = GetTimeTaken(filePath);

                string destinationDir = "";
                bool isUnsorted = false;

                // Determine file destination based on time taken and selected subfolder organisation
                if (timeTaken != null)
                {
                    switch (Properties.Settings.Default.Subfolders)
                    {
                        case 0:
                            {
                                destinationDir = Path.Combine(Properties.Settings.Default.Endpoint,
                                    string.Format("{0:D4}\\{1:D2}\\{2:D2}",
                                    timeTaken?.Year, timeTaken?.Month, timeTaken?.Day));
                                break;
                            }
                        case 1:
                            {
                                destinationDir = Path.Combine(Properties.Settings.Default.Endpoint,
                                    string.Format("{0:D4}\\{0:D4}-{1:D2}-{2:D2}",
                                    timeTaken?.Year, timeTaken?.Month, timeTaken?.Day));
                                break;
                            }
                        case 2:
                            {
                                destinationDir = Path.Combine(Properties.Settings.Default.Endpoint,
                                    string.Format("{0:D4}-{1:D2}-{2:D2}",
                                    timeTaken?.Year, timeTaken?.Month, timeTaken?.Day));
                                break;
                            }
                        default: return false;
                    }
                }
                else
                {
                    isUnsorted = true;
                    destinationDir = Path.Combine(Properties.Settings.Default.Endpoint, "Unsorted");
                }

                destinationDir = CreateDirectory(destinationDir);
                if (CopyFile(filePath, destinationDir))
                    DuplicateCounter++;

                if (isUnsorted) UnsortedCounter++;
                if (FirstFileDestination == null)
                    FirstFileDestination = destinationDir;

                if (Properties.Settings.Default.ShouldDeleteAfter)
                    File.Delete(filePath);

                // Display duplicate and unsorted counter as the transfer progresses
                if (DuplicateCounter > 0 || UnsortedCounter > 0)
                {
                    Application.Current.Dispatcher.Invoke(delegate ()
                    {
                        LabelSubCaption.Text = string.Format(
                            "{0} renamed duplicates, {1} transferred to 'Unsorted' folder",
                            DuplicateCounter, UnsortedCounter);
                    });
                }
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
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = FirstFileDestination,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch { }
        }
    }
}
