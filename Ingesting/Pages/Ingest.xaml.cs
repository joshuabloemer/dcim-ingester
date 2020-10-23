using DCIMIngester.Routines;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace DCIMIngester.Ingesting.Pages
{
    public partial class Ingest : Page
    {
        private readonly IngestTask IngestTask;

        private Thread IngestThread;
        private bool ShouldCancelIngest = false;

        private int LastFileIngested = -1;
        private string FirstFileDestination = null;
        private int DuplicateCounter = 0;
        private int UnsortedCounter = 0;

        public event EventHandler<PageDismissedEventArgs> Dismissed;


        public Ingest(IngestTask ingestTask)
        {
            IngestTask = ingestTask;
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            IngestThread = new Thread(() => IngestFiles());
            IngestThread.Start();
        }

        private void IngestFiles()
        {
            for (int i = LastFileIngested + 1; i < IngestTask.Context.FilesToIngest.Count; i++)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (IngestTask.Context.VolumeLabel == "")
                    {
                        LabelCaption.Text = string.Format("Transferring file {0} of {1} from unnamed volume",
                            i + 1, IngestTask.Context.FilesToIngest.Count);
                    }
                    else
                    {
                        LabelCaption.Text = string.Format("Transferring file {0} of {1} from '{2}'", i + 1,
                            IngestTask.Context.FilesToIngest.Count, IngestTask.Context.VolumeLabel);
                    }

                    float percentage = (float)i / IngestTask.Context.FilesToIngest.Count * 100;
                    LabelPercentage.Content = string.Format("{0}%", Math.Round(percentage));
                    ProgressBarProgress.Value = percentage;

                    LabelSubCaption.Text = string.Format(
                        "{0} renamed duplicates, {1} ingested to 'Unsorted' folder", DuplicateCounter, UnsortedCounter);
                });

                if (!IngestFile(IngestTask.Context.FilesToIngest.ElementAt(i)))
                {
                    Application.Current.Dispatcher.Invoke(() => IngestFailed());
                    return;
                }

                LastFileIngested++;
                if (ShouldCancelIngest)
                {
                    // Task is cancelled only if the file we just copied was not the final file
                    if (LastFileIngested < IngestTask.Context.FilesToIngest.Count - 1)
                    {
                        Application.Current.Dispatcher.Invoke(() => IngestCancelled());
                        return;
                    }
                }
            }

            Application.Current.Dispatcher.Invoke(() => IngestCompleted());
        }
        private bool IngestFile(string filePath)
        {
            try
            {
                DateTime? timeTaken = Helpers.GetTimeTaken(filePath);

                string destinationDir;
                bool isUnsorted = false;

                // Determine file destination based on time taken and selected subfolder organisation
                if (timeTaken != null)
                {
                    switch (Properties.Settings.Default.Subfolders)
                    {
                        case 0:
                            {
                                destinationDir = Path.Combine(Properties.Settings.Default.Endpoint, string.Format(
                                    "{0:D4}\\{1:D2}\\{2:D2}", timeTaken?.Year, timeTaken?.Month, timeTaken?.Day));
                                break;
                            }
                        case 1:
                            {
                                destinationDir = Path.Combine(Properties.Settings.Default.Endpoint, string.Format(
                                    "{0:D4}\\{0:D4}-{1:D2}-{2:D2}", timeTaken?.Year, timeTaken?.Month, timeTaken?.Day));
                                break;
                            }
                        case 2:
                            {
                                destinationDir = Path.Combine(Properties.Settings.Default.Endpoint, string.Format(
                                    "{0:D4}-{1:D2}-{2:D2}", timeTaken?.Year, timeTaken?.Month, timeTaken?.Day));
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

                destinationDir = Helpers.CreateDirectory(destinationDir);
                if (Helpers.CopyFile(filePath, destinationDir))
                    DuplicateCounter++;

                if (isUnsorted) UnsortedCounter++;
                if (FirstFileDestination == null)
                    FirstFileDestination = destinationDir;

                if (Properties.Settings.Default.ShouldDeleteAfter)
                    File.Delete(filePath);
            }
            catch { return false; }

            return true;
        }

        private void IngestCompleted()
        {
            IngestTask.Status = IngestTask.TaskStatus.Completed;

            if (IngestTask.Context.VolumeLabel == "")
                LabelCaption.Text = string.Format("Ingest from unnamed volume complete");
            else LabelCaption.Text = string.Format("Ingest from '{0}' complete", IngestTask.Context.VolumeLabel);

            LabelPercentage.Content = "100%";
            ProgressBarProgress.Value = 100;

            LabelSubCaption.Text = string.Format(
                "{0} renamed duplicates, {1} ingested to 'Unsorted' folder", DuplicateCounter, UnsortedCounter);

            ButtonCancel.Content = "Dismiss";
            ButtonCancel.IsEnabled = true;
            ButtonView.Visibility = Visibility.Visible;
        }
        private void IngestFailed()
        {
            IngestTask.Status = IngestTask.TaskStatus.Failed;

            if (IngestTask.Context.VolumeLabel == "")
                LabelCaption.Text = string.Format("Ingest from unnamed volume failed");
            else LabelCaption.Text = string.Format("Ingest from '{0}' failed", IngestTask.Context.VolumeLabel);

            ButtonCancel.Content = "Dismiss";
            ButtonCancel.IsEnabled = true;
            ButtonRetry.Visibility = Visibility.Visible;

            LabelSubCaption.Text = string.Format(
                "{0} renamed duplicates, {1} ingested to 'Unsorted' folder", DuplicateCounter, UnsortedCounter);

            if (LastFileIngested > -1)
                ButtonView.Visibility = Visibility.Visible;
        }
        private void IngestCancelled()
        {
            IngestTask.Status = IngestTask.TaskStatus.Cancelled;

            if (IngestTask.Context.VolumeLabel == "")
                LabelCaption.Text = string.Format("Ingest from unnamed volume cancelled");
            else LabelCaption.Text = string.Format("Ingest from '{0}' cancelled", IngestTask.Context.VolumeLabel);

            ButtonCancel.Content = "Dismiss";
            ButtonCancel.IsEnabled = true;
            ButtonRetry.Visibility = Visibility.Visible;

            LabelSubCaption.Text = string.Format(
                "{0} renamed duplicates, {1} ingested to 'Unsorted' folder", DuplicateCounter, UnsortedCounter);

            if (LastFileIngested > -1)
                ButtonView.Visibility = Visibility.Visible;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (ButtonCancel.Content.ToString() == "Cancel")
            {
                ButtonCancel.IsEnabled = false;
                ButtonCancel.Content = "Cancelling";
                ShouldCancelIngest = true;
            }
            else Dismissed?.Invoke(this, new PageDismissedEventArgs(PageDismissedEventArgs.PageDismissAction.IngestDismiss));
        }
        private void ButtonRetry_Click(object sender, RoutedEventArgs e)
        {
            ShouldCancelIngest = false;

            ButtonCancel.Content = "Cancel";
            ButtonRetry.Visibility = Visibility.Collapsed;
            ButtonView.Visibility = Visibility.Collapsed;

            Page_Loaded(this, new RoutedEventArgs()); // Restart the ingest process
        }
        private void ButtonView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open the destination directory of the first file transferred
                ProcessStartInfo psi = new ProcessStartInfo(FirstFileDestination);
                psi.UseShellExecute = true;
                psi.Verb = "open";
                Process.Start(psi);
            }
            catch { }
        }
    }
}
