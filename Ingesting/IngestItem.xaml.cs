using DcimIngester.Ingesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using static DcimIngester.Utilities;

namespace DcimIngester.Controls
{
    /// <summary>
    /// Represents the user interface for an ingest operation.
    /// </summary>
    public partial class IngestItem : UserControl
    {
        /// <summary>
        /// The ingest task for the item to execute.
        /// </summary>
        private readonly IngestTask task;

        /// <summary>
        /// Gets the letter of the volume to ingest from.
        /// </summary>
        public char VolumeLetter => task.Work.VolumeLetter;

        /// <summary>
        /// Gets the status of the ingest operation.
        /// </summary>
        public IngestTaskStatus Status => task.Status;

        /// <summary>
        /// The directory that the first file was ingested to.
        /// </summary>
        private string? firstIngestDir = null;

        /// <summary>
        /// The number of ingested files that were sorted into directories by date taken.
        /// </summary>
        private int sortedCount = 0;

        /// <summary>
        /// The number of ingested files that were sorted into an "unsorted" folder.
        /// </summary>
        private int unsortedCount = 0;

        /// <summary>
        /// The number of ingested files that were renamed to avoid a duplicate file name.
        /// </summary>
        private int renamedCount = 0;

        /// <summary>
        /// Occurs when the user dismisses the item.
        /// </summary>
        public event EventHandler? Dismissed;


        /// <summary>
        /// Initialises a new instance of the <see cref="IngestItem"/> class.
        /// </summary>
        /// <param name="work">The work to do when the ingest operation is executed.</param>
        public IngestItem(IngestWork work)
        {
            InitializeComponent();

            task = new IngestTask(work);
            task.PreFileIngested += Task_PreFileIngested;
            task.PostFileIngested += Task_PostFileIngested;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            string label = task.Work.VolumeLabel.Length == 0 ? "unnamed" : task.Work.VolumeLabel;

            LabelPromptCaption.Text = string.Format(
                "{0}: ({1}) contains {2} files ({3}). Do you want to ingest them?",
                task.Work.VolumeLetter, label, task.Work.FilesToIngest.Count,
                FormatBytes(task.Work.TotalIngestSize));

            CheckBoxPromptDelete.IsChecked = Properties.Settings.Default.DeleteAfterIngest;
        }

        private void ButtonPromptYes_Click(object sender, RoutedEventArgs e)
        {
            task.DestinationDirectory = Properties.Settings.Default.DestDirectory;
            task.DestinationStructure = (DestStructure)Properties.Settings.Default.DestStructure;
            task.DeleteAfterIngest = (bool)CheckBoxPromptDelete.IsChecked!;

            Properties.Settings.Default.DeleteAfterIngest = task.DeleteAfterIngest;
            Properties.Settings.Default.Save();

            GridPrompt.Visibility = Visibility.Collapsed;
            GridIngest.Visibility = Visibility.Visible;

            Ingest();
        }

        private void ButtonPromptNo_Click(object sender, RoutedEventArgs e)
        {
            Dismissed?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Executes the ingest operation, making sure the UI is updated appropriately.
        /// </summary>
        private async void Ingest()
        {
            ButtonIngestCancel.Content = "Cancel";
            ButtonIngestRetry.Visibility = Visibility.Collapsed;
            ButtonIngestOpen.Visibility = Visibility.Collapsed;

            string label = task.Work.VolumeLabel.Length == 0 ? "unnamed" : task.Work.VolumeLabel;

            try
            {
                bool result = await task.IngestAsync();

                if (result)
                {
                    LabelIngestCaption.Text = string.Format(
                        "Ingest from {0}: ({1}) complete", task.Work.VolumeLetter, label);
                }
                else
                {
                    LabelIngestCaption.Text = string.Format(
                        "Ingest from {0}: ({1}) cancelled", task.Work.VolumeLetter, label);
                }
            }
            catch
            {
                LabelIngestCaption.Text = string.Format(
                    "Ingest from {0}: ({1}) failed", task.Work.VolumeLetter, label);
                ButtonIngestRetry.Visibility = Visibility.Visible;
            }

            ButtonIngestCancel.Content = "Dismiss";
            ButtonIngestCancel.IsEnabled = true;

            if (firstIngestDir != null)
                ButtonIngestOpen.Visibility = Visibility.Visible;
        }

        private void Task_PreFileIngested(object? sender, PreFileIngestedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                string label = task.Work.VolumeLabel.Length == 0 ? "unnamed" : task.Work.VolumeLabel;

                LabelIngestCaption.Text = string.Format("Transferring file {0} of {1} from {2}: ({3})",
                    e.FileNumber + 1, task.Work.FilesToIngest.Count, task.Work.VolumeLetter, label);
            });
        }

        private void Task_PostFileIngested(object? sender, PostFileIngestedEventArgs e)
        {
            if (firstIngestDir == null)
                firstIngestDir = Path.GetDirectoryName(e.NewFilePath);

            if (e.IsUnsorted)
                unsortedCount++;
            else sortedCount++;

            if (e.IsRenamed)
                renamedCount++;

            double percentage = ((double)(e.FileNumber + 1) / task.Work.FilesToIngest.Count) * 100;

            Application.Current.Dispatcher.Invoke(() =>
            {
                LabelIngestPercent.Content = string.Format("{0}%", Math.Round(percentage));
                ProgressBar1.Value = percentage;

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
            ProcessStartInfo psi = new(firstIngestDir!)
            {
                Verb = "open",
                UseShellExecute = true
            };

            try
            {
                Process.Start(psi);
            }
            catch { }
        }
    }
}
