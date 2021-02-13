using DcimIngester.Ingesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace DcimIngester.Controls
{
    public partial class IngestItem : UserControl
    {
        private readonly IngestWork work;
        private readonly IngestTask task;

        public Guid VolumeID => work.VolumeID;
        public IngestTaskStatus Status => task.Status;

        private string? firstFileDest = null;

        private int sortedCount = 0;
        private int unsortedCount = 0;
        private int renamedCount = 0;

        public event EventHandler? Dismissed;

        public IngestItem(IngestWork work)
        {
            InitializeComponent();
            this.work = work;

            task = new IngestTask(work);
            task.PreFileIngested += Task_PreFileIngested;
            task.PostFileIngested += Task_PostFileIngested;
        }


        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            string volumeLabel = work.VolumeLabel == "" ? "unnamed" : work.VolumeLabel;

            LabelPromptCaption.Text = string.Format(
                "{0} ({1}) contains {2} files ({3}). Do you want to ingest them?",
                work.VolumeLetter, volumeLabel, work.FilesToIngest.Count,
                Utilities.FormatBytes(work.TotalIngestSize));

            CheckBoxPromptDelete.IsChecked = Properties.Settings.Default.ShouldDeleteAfter;
        }

        private void ButtonPromptYes_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ShouldDeleteAfter =
                (bool)CheckBoxPromptDelete.IsChecked!;
            Properties.Settings.Default.Save();

            GridPrompt.Visibility = Visibility.Collapsed;
            GridIngest.Visibility = Visibility.Visible;

            Ingest();
        }
        private void ButtonPromptNo_Click(object sender, RoutedEventArgs e)
        {
            Dismissed?.Invoke(this, new EventArgs());
        }

        private async void Ingest()
        {
            ButtonIngestCancel.Content = "Cancel";
            ButtonIngestRetry.Visibility = Visibility.Collapsed;
            ButtonIngestOpen.Visibility = Visibility.Collapsed;

            try
            {
                bool result = await task.IngestAsync();

                if (result)
                    LabelIngestCaption.Text = string.Format("Ingest from {0} complete", work.VolumeLabel);
                else LabelIngestCaption.Text = string.Format("Ingest from {0} cancelled", work.VolumeLabel);
            }
            catch
            {
                LabelIngestCaption.Text = string.Format("Ingest from {0} failed", work.VolumeLetter);
                ButtonIngestRetry.Visibility = Visibility.Visible;
            }

            ButtonIngestCancel.Content = "Dismiss";
            ButtonIngestCancel.IsEnabled = true;

            if (firstFileDest != null)
                ButtonIngestOpen.Visibility = Visibility.Visible;
        }
        private void Task_PreFileIngested(object? sender, PreFileIngestedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LabelIngestCaption.Text = string.Format("Transferring file {0} of {1} from {2}",
                    e.FileNumber, work.FilesToIngest.Count, work.VolumeLetter);
            });
        }
        private void Task_PostFileIngested(object? sender, PostFileIngestedEventArgs e)
        {
            if (firstFileDest == null)
                firstFileDest = e.FilePath;

            if (e.IsUnsorted)
                unsortedCount++;
            else sortedCount++;

            if (e.IsRenamed)
                renamedCount++;

            double percentage = ((double)e.FileNumber / work.FilesToIngest.Count) * 100;

            Application.Current.Dispatcher.Invoke(() =>
            {
                LabelIngestPercentage.Content = string.Format("{0}%", Math.Round(percentage));
                ProgressBarIngest.Value = percentage;

                LabelIngestSubCaption.Text = string.Format(
                    "{0} sorted, {1} unsorted, {2} renamed", sortedCount, unsortedCount, renamedCount);
            });
        }

        private void ButtonIngestCancel_Click(object sender, RoutedEventArgs e)
        {
            if (task.Status == IngestTaskStatus.Ingesting)
            {
                ButtonIngestCancel.IsEnabled = false;
                ButtonIngestCancel.Content = "Cancelling";
                task.AbortIngest();
            }
            else Dismissed?.Invoke(this, new EventArgs());
        }
        private void ButtonIngestRetry_Click(object sender, RoutedEventArgs e)
        {
            Ingest();
        }
        private void ButtonIngestOpen_Click(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo psi = new ProcessStartInfo(
                new FileInfo(firstFileDest!).DirectoryName!);

            psi.Verb = "open";
            psi.UseShellExecute = true;

            try
            {
                Process.Start(psi);
            }
            catch { }
        }
    }
}
